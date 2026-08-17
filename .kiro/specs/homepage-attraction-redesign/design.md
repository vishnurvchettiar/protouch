# Design Document — Homepage Attraction Redesign

## Feature: `homepage-attraction-redesign`
**Project:** ProTouch Physio – Slimming Clinic (Blazor Server .NET 9)
**Aesthetic mandate:** Professional physiotherapy and slimming clinic — clinically credible and medically trustworthy. **ProTouch Physio – Slimming Clinic: physiotherapy and clinical slimming treatment only.**

---

## Overview

This design adds dynamic animation, two new sections (Social Proof, Floating Sticky Bar), and visual polish upgrades to the existing public homepage (`/`). All implementation is split across three files:

| File | Role |
|---|---|
| `ClinicApp/Components/Pages/Home.razor` | Markup, C# data models, `IJSRuntime` wiring, disposal |
| `ClinicApp/wwwroot/site.js` | All animation and observer logic (vanilla JS) |
| `ClinicApp/wwwroot/app.css` | All CSS additions scoped to `.wellness-page` |

**Strict constraint:** No existing CSS rules are modified or removed. Every new rule is an additive block under `.wellness-page`. The `.dashboard-theme` and admin panel styles are never touched.

---

## Architecture

```
Browser
  │
  ├── PublicLayout.razor  (unchanged — provides layout shell)
  │
  └── Home.razor  (IAsyncDisposable)
        ├── Markup
        │     ├── Hero section (5-slide sub-carousel, typewriter h1)
        │     ├── Guarantee Strip (duplicated ticker list)
        │     ├── Approach section
        │     ├── Services section (service-card-home with micro-interactions)
        │     ├── Why ProTouch (care-panel)
        │     ├── Social Proof section  ← NEW
        │     ├── Programs section
        │     ├── Gallery Carousel (18 slides, cross-fade)
        │     ├── Contact section (urgency label + dual CTAs)
        │     └── Floating Sticky Bar  ← NEW
        │
        ├── @code block
        │     ├── StatCounter record
        │     ├── Testimonial record
        │     ├── socialProofStats : List<StatCounter>
        │     ├── testimonials : List<Testimonial>
        │     ├── IJSRuntime JS  (injected)
        │     ├── OnAfterRenderAsync (firstRender gate → 5× InvokeVoidAsync)
        │     └── DisposeAsync → InvokeVoidAsync("destroyHomepageAnimations")
        │
        └── site.js  (window.* exports)
              ├── initTypewriter(selector, delay)
              ├── initStatCounters(selector, duration)
              ├── initScrollReveal()
              ├── initGuaranteeTicker()
              ├── initStickyBar(heroSelector, barSelector)
              └── destroyHomepageAnimations()
```

---

## Components and Interfaces

### 1. Hero Section — On-Load Animations

#### `initTypewriter(selector, minDelay)`

```js
window.initTypewriter = function(selector, minDelay = 40) {
  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  const el = document.querySelector(selector);
  if (!el) return;
  if (reduced) { /* leave text fully visible, no change */ return; }
  const fullText = el.textContent;
  el.textContent = '';
  let i = 0;
  const tick = () => {
    if (i < fullText.length) {
      el.textContent += fullText[i++];
      setTimeout(tick, minDelay);
    }
  };
  setTimeout(tick, minDelay);
};
```

- Called once from `OnAfterRenderAsync` with `selector: ".wellness-page .hero-content h1"` and `minDelay: 40`.
- Under `prefers-reduced-motion` the function returns immediately; the full headline is already rendered by Blazor.

#### `initStatCounters(containerSelector, duration)`

```js
window.initStatCounters = function(containerSelector, duration = 1200) {
  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  document.querySelectorAll(containerSelector + ' [data-count-target]').forEach(el => {
    const target = parseFloat(el.dataset.countTarget);
    if (reduced) { el.textContent = el.dataset.countDisplay ?? target; return; }
    // easeOutCubic: t => 1 - Math.pow(1 - t, 3)
    const start = performance.now();
    const step = (now) => {
      const t = Math.min((now - start) / duration, 1);
      const eased = 1 - Math.pow(1 - t, 3);
      el.textContent = Math.round(eased * target);
      if (t < 1) requestAnimationFrame(step);
      else el.textContent = el.dataset.countDisplay ?? target; // snap to exact display value
    };
    requestAnimationFrame(step);
  });
};
```

- `data-count-target` holds the numeric end value; `data-count-display` holds the formatted label (e.g. `"500+"`).
- Triggered by `initScrollReveal`'s IntersectionObserver when the social proof section enters the viewport, not directly at page load. `initStatCounters` is registered as the callback for `.social-proof-section` element.

---

### 2. Scroll-Reveal System

#### `initScrollReveal()`

```js
window.initScrollReveal = (function() {
  let _observer = null;
  return function() {
    if (_observer) return;                          // idempotency guard
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const els = document.querySelectorAll('[data-reveal]');
    if (reduced) {
      els.forEach(el => el.classList.add('revealed'));
      return;
    }
    _observer = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          entry.target.classList.add('revealed');
          // If it's the social proof section, also kick off counters
          if (entry.target.classList.contains('social-proof-section')) {
            window.initStatCounters('.social-proof-section', 1200);
          }
          _observer.unobserve(entry.target);       // fire once only
        }
      });
    }, { threshold: 0.15 });
    els.forEach(el => _observer.observe(el));
  };
})();
```

**`data-reveal` placement in `Home.razor`:**

| Element | Selector / class |
|---|---|
| Approach section | `.split-intro` |
| Each service card | `.service-card-home` |
| Care panel | `.care-panel` |
| Each care-reason item | `.care-reasons > div` |
| Social Proof section | `.social-proof-section` |
| Each stat card | `.stat-card-sp` |
| Each testimonial | `.testimonial-card` |
| Programs section | `.programs` |
| Gallery carousel wrapper | `.carousel-section-wrap` |
| Contact section | `.contact-section` |

**Excluded:** The Floating Sticky Bar (`.sticky-booking-bar`) — never receives `data-reveal`.

**CSS for reveal transitions** (additive, under `.wellness-page`):

```css
.wellness-page [data-reveal] {
  opacity: 0;
  transform: translateY(24px);
  transition: opacity 500ms ease-out, transform 500ms ease-out;
}
.wellness-page [data-reveal].revealed {
  opacity: 1;
  transform: translateY(0);
}
@media (prefers-reduced-motion: reduce) {
  .wellness-page [data-reveal] { opacity: 1; transform: none; transition: none; }
}
```

---

### 3. Social Proof Section

#### C# Data Model (`Home.razor` `@code`)

```csharp
private record StatCounter(
    string CountDisplay,   // e.g. "500+"   — displayed after animation
    double CountTarget,    // e.g. 500      — numeric animation end value
    string Label           // e.g. "Patients Served"
);

private record Testimonial(
    string Quote,          // short outcome statement, no patient name
    string RoleLabel       // e.g. "Weight Loss Patient"
);

private List<StatCounter> socialProofStats = new()
{
    new("4–8 kg",  8, "Weight Loss / Month"),
    new("2–5 in",  5, "Lost Per Session"),
    new("500+",  500, "Patients Served"),
    new("10+",    10, "Years of Experience"),
};

private List<Testimonial> testimonials = new()
{
    new("\"Lost 6 kg in the first month without any fasting or medicine — the physiotherapy approach made all the difference.\"",
        "Weight Loss Patient"),
    new("\"My knee pain reduced significantly within two weeks of targeted rehabilitation. Finally able to walk without discomfort.\"",
        "Physiotherapy Patient"),
};
```

**Design notes:**
- No patient names, no photographs, no identifiable information — **clinic credibility is built on outcomes and expertise, not personal stories**. This is a medical facility, not a social platform.
- Stats reflect the clinic's documented outcomes from existing service card copy.
- Role labels are generic treatment categories only.

#### Markup Skeleton

```razor
<!-- Social Proof Section -->
<section class="wellness-section social-proof-section" data-reveal aria-labelledby="sp-heading">
    <p class="section-kicker">CLINIC OUTCOMES</p>
    <h2 id="sp-heading">Results backed by <em>science and care.</em></h2>

    <div class="sp-stats-grid">
        @foreach (var stat in socialProofStats)
        {
            <div class="stat-card-sp" data-reveal>
                <span class="sp-count"
                      data-count-target="@stat.CountTarget"
                      data-count-display="@stat.CountDisplay">@stat.CountDisplay</span>
                <span class="sp-label">@stat.Label</span>
            </div>
        }
    </div>

    <div class="sp-testimonials">
        @foreach (var t in testimonials)
        {
            <blockquote class="testimonial-card" data-reveal>
                <p>@t.Quote</p>
                <footer>— @t.RoleLabel</footer>
            </blockquote>
        }
    </div>
</section>
```

**CSS** (additive under `.wellness-page`):

```css
.wellness-page .social-proof-section {
    background: #f5f9f6;
    border-top: 1px solid #d8ead0;
    border-bottom: 1px solid #d8ead0;
    max-width: none;
    padding-inline: max(30px, calc((100vw - 1180px) / 2));
}
.wellness-page .sp-stats-grid {
    display: flex;
    justify-content: center;
    gap: 32px;
    flex-wrap: wrap;
    margin: 2.5rem 0 3rem;
}
.wellness-page .stat-card-sp {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 8px;
    min-width: 140px;
    text-align: center;
}
.wellness-page .sp-count {
    font-family: 'Playfair Display', Georgia, serif;
    font-size: clamp(2.4rem, 5vw, 3.6rem);
    font-weight: 800;
    color: #008d36;
    line-height: 1;
    display: block;
}
.wellness-page .sp-label {
    font-size: 0.82rem;
    font-weight: 700;
    color: #5a5260;
    text-transform: uppercase;
    letter-spacing: 0.1em;
    display: block;
}
.wellness-page .sp-testimonials {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 24px;
    margin-top: 2rem;
}
.wellness-page .testimonial-card {
    background: #fff;
    border-left: 4px solid #008d36;
    border-radius: 0 12px 12px 0;
    padding: 24px 28px;
    margin: 0;
}
.wellness-page .testimonial-card p {
    font-size: 1rem;
    line-height: 1.65;
    color: #3d3640;
    font-style: italic;
    margin-bottom: 12px;
}
.wellness-page .testimonial-card footer {
    font-size: 0.8rem;
    font-weight: 800;
    color: #008d36;
    letter-spacing: 0.04em;
    text-transform: uppercase;
}
/* Responsive: stat grid collapses to 2×2 at 520px */
@media (max-width: 520px) {
    .wellness-page .sp-stats-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 20px;
        justify-items: center;
    }
    .wellness-page .sp-testimonials {
        grid-template-columns: 1fr;
    }
}
```

---

### 4. Floating Sticky Bar

#### `initStickyBar(heroSelector, barSelector)`

```js
window.initStickyBar = (function() {
  let _observer = null;
  return function(heroSelector, barSelector) {
    if (_observer) return;                         // idempotency guard
    const hero = document.querySelector(heroSelector);
    const bar  = document.querySelector(barSelector);
    if (!hero || !bar) return;
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduced) {
      bar.style.transition = 'none';
    }
    _observer = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        const visible = !entry.isIntersecting;    // bar visible when hero is NOT in viewport
        bar.classList.toggle('sticky-bar--visible', visible);
        // Keep wellness-page padding-bottom in sync with bar height
        const page = document.querySelector('.wellness-page');
        if (page) {
          page.style.paddingBottom = visible ? bar.offsetHeight + 'px' : '0';
        }
      });
    }, { threshold: 0, rootMargin: '0px' });
    _observer.observe(hero);
  };
})();
```

- `heroSelector`: `".wellness-hero"`
- `barSelector`: `".sticky-booking-bar"`
- The observer fires when the hero's last pixel leaves the viewport (`threshold: 0`), making the transition accurate.

#### CSS

```css
/* Floating Sticky Bar — base state (hidden, off-screen) */
.wellness-page .sticky-booking-bar {
    position: fixed;
    bottom: 0;
    left: 0;
    width: 100%;
    z-index: 200;
    background: #1a4d2e;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 16px;
    padding: 14px 24px;
    opacity: 0;
    transform: translateY(100%);
    transition: opacity 300ms ease, transform 300ms ease;
    pointer-events: none;
}
/* Visible state toggled by JS */
.wellness-page .sticky-booking-bar.sticky-bar--visible {
    opacity: 1;
    transform: translateY(0);
    pointer-events: auto;
}
/* Both CTAs at equal visual weight */
.wellness-page .sticky-bar-btn {
    display: inline-flex;
    align-items: center;
    gap: 8px;
    padding: 12px 24px;
    border-radius: 4px;
    font-size: 0.9rem;
    font-weight: 800;
    text-decoration: none;
    transition: background 0.2s ease;
    min-width: 160px;
    justify-content: center;
}
.wellness-page .sticky-bar-btn--book {
    background: #008d36;
    color: #fff !important;
}
.wellness-page .sticky-bar-btn--book:hover { background: #006d2a; }
.wellness-page .sticky-bar-btn--wa {
    background: #25d366;
    color: #fff !important;
}
.wellness-page .sticky-bar-btn--wa:hover { background: #1ebe59; }
/* Reduced-motion: no transition */
@media (prefers-reduced-motion: reduce) {
    .wellness-page .sticky-booking-bar {
        transition: none;
    }
}
/* Narrow viewport: stack vertically if < 380px */
@media (max-width: 380px) {
    .wellness-page .sticky-booking-bar { flex-direction: column; gap: 8px; padding: 12px 16px; }
    .wellness-page .sticky-bar-btn { min-width: 0; width: 100%; }
}
```

#### Markup Skeleton

```razor
<!-- Floating Sticky Bar — outside <main>, inside .wellness-page -->
<div class="sticky-booking-bar" role="complementary" aria-label="Quick booking bar">
    <a class="sticky-bar-btn sticky-bar-btn--book"
       href="/book"
       aria-label="Book an online appointment at ProTouch Clinic">
        📅 Book Online
    </a>
    <a class="sticky-bar-btn sticky-bar-btn--wa"
       href="https://wa.me/919028027888"
       target="_blank"
       rel="noopener"
       aria-label="Contact ProTouch Clinic via WhatsApp">
        💬 WhatsApp Us
    </a>
</div>
```

---

### 5. Guarantee Strip Animated Ticker

#### `initGuaranteeTicker()`

```js
window.initGuaranteeTicker = function() {
  // The Blazor markup already duplicates the guarantee list for a seamless loop.
  // Under prefers-reduced-motion the CSS animation is already suppressed via media query.
  // This function is a no-op at present but is registered on window for idempotency
  // compliance and future extensibility (e.g. pause on hover).
  const ticker = document.querySelector('.wellness-page .guarantee-ticker-track');
  if (!ticker) return;
  const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  if (reduced) {
    ticker.style.animationPlayState = 'paused';
  }
};
```

#### Markup — Duplicated Content for Seamless Loop

```razor
<!-- Guarantee Strip — replaced with ticker implementation -->
<section class="guarantee-strip guarantee-strip--ticker" aria-label="Our care promise">
    <div class="guarantee-ticker-track">
        @foreach (var g in guarantees) { <span class="guarantee-item">✓ @g</span> }
        @* Duplicate for seamless loop *@
        @foreach (var g in guarantees) { <span class="guarantee-item" aria-hidden="true">✓ @g</span> }
    </div>
</section>
```

#### CSS

```css
/* Ticker override — keeps background:#008d36, color:white from existing rule */
.wellness-page .guarantee-strip--ticker {
    overflow: hidden;
    padding: 0;
    display: block;
    flex-wrap: unset;
    justify-content: unset;
    gap: unset;
}
.wellness-page .guarantee-ticker-track {
    display: flex;
    width: max-content;
    animation: ticker-scroll 18s linear infinite;
}
.wellness-page .guarantee-item {
    padding: 18px 38px;
    font-weight: 800;
    font-size: 0.88rem;
    white-space: nowrap;
    color: #fff;
}
@keyframes ticker-scroll {
    from { transform: translateX(0); }
    to   { transform: translateX(-50%); }
}
@media (prefers-reduced-motion: reduce) {
    .wellness-page .guarantee-ticker-track {
        animation: none;
        width: auto;
        flex-wrap: wrap;
        justify-content: center;
    }
    .wellness-page .guarantee-item[aria-hidden="true"] {
        display: none;  /* hide duplicate content when static */
    }
}
```

---

### 6. Service Cards — Hover Micro-Interactions

Additive CSS only. Existing `.service-card-home:hover` rule is **supplemented** (not replaced) with the upgraded values:

```css
/* Upgraded hover lift — replaces the existing shallow hover stub */
.wellness-page .service-card-home {
    transition: transform 220ms ease-out, box-shadow 220ms ease-out;
}
.wellness-page .service-card-link:hover .service-card-home,
.wellness-page .service-card-link:focus-visible .service-card-home {
    transform: translateY(-6px);
    box-shadow: 0 16px 48px rgba(0, 0, 0, 0.12);
}
.wellness-page .svc-icon-wrap {
    transition: transform 220ms ease-out;
}
.wellness-page .service-card-link:hover .svc-icon-wrap,
.wellness-page .service-card-link:focus-visible .svc-icon-wrap {
    transform: scale(1.12) rotate(6deg);
}
/* Reduced-motion: box-shadow only, no transform */
@media (prefers-reduced-motion: reduce) {
    .wellness-page .service-card-link:hover .service-card-home,
    .wellness-page .service-card-link:focus-visible .service-card-home {
        transform: none;
        box-shadow: 0 16px 48px rgba(0, 0, 0, 0.12);
    }
    .wellness-page .service-card-link:hover .svc-icon-wrap,
    .wellness-page .service-card-link:focus-visible .svc-icon-wrap {
        transform: none;
    }
}
```

---

### 7. Gallery Carousel — Visual Polish

Additive CSS upgrades. The C# Blazor timer logic (`_carouselTimer`, `JumpToSlide`) is unchanged.

```css
/* Cross-fade transition upgrade: 600ms */
.wellness-page .carousel-slide {
    transition: opacity 600ms ease;
}
/* Green-tinted shadow frame */
.wellness-page .carousel-section {
    box-shadow: 0 16px 56px rgba(0, 141, 54, 0.15);
}
/* Active dot: 1.4× scale, #008d36 fill */
.wellness-page .carousel-dot.active {
    background: #008d36;
    transform: scale(1.4);
    border-color: #008d36;
}
/* Minimum tappable target for dots */
.wellness-page .carousel-dot {
    min-width: 24px;
    min-height: 24px;
    padding: 7px;
    box-sizing: content-box;
}
/* Reduced-motion: instant slide switch */
@media (prefers-reduced-motion: reduce) {
    .wellness-page .carousel-slide {
        transition: none;
    }
}
```

---

### 8. Contact Section — Urgency and Prominence

The existing `.contact-section` block uses `background: #7c1b68` (purple). Per Requirement 8.3 the redesign upgrades it to `background: #1a4d2e` (deep clinical green). This is already set in the latest app.css block. The urgency label and dual CTA pattern are added to `Home.razor`:

```razor
<section id="contact" class="contact-section">
    <div>
        <p class="section-kicker">VISIT PROTOUCH CLINIC</p>
        <h2>Let's take the first step together.</h2>
        <!-- Urgency label — above CTAs -->
        <p class="contact-urgency">Take the first step today — consultations available now.</p>
        <div class="contact-cta-group" style="margin-top:1.5rem; display:flex; gap:12px; flex-wrap:wrap;">
            <a class="wellness-primary" href="/book">📅 Book Online →</a>
            <a class="wellness-secondary contact-wa-cta"
               href="https://wa.me/919028027888"
               target="_blank"
               rel="noopener">💬 WhatsApp Us</a>
        </div>
    </div>
    <div class="contact-details">
        <!-- existing contact detail links — unchanged -->
    </div>
</section>
```

Additive CSS:
```css
.wellness-page .contact-urgency {
    font-size: 1.05rem;
    font-weight: 700;
    color: #ffeb00;
    margin-top: 12px;
    line-height: 1.5;
}
.wellness-page .contact-wa-cta {
    color: #fff !important;
    border-color: rgba(255,255,255,0.5);
}
.wellness-page .contact-wa-cta:hover {
    background: rgba(255,255,255,0.08);
    color: #fff !important;
}
```

---

### 9. Blazor Integration — `OnAfterRenderAsync` + `IAsyncDisposable`

```csharp
@inject IJSRuntime JS
@implements IAsyncDisposable

// ...

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        await JS.InvokeVoidAsync("initTypewriter",
            ".wellness-page .hero-content h1", 40);
        await JS.InvokeVoidAsync("initScrollReveal");
        await JS.InvokeVoidAsync("initGuaranteeTicker");
        await JS.InvokeVoidAsync("initStickyBar",
            ".wellness-hero", ".sticky-booking-bar");
        // Note: initStatCounters is called internally by initScrollReveal
        // when the social-proof-section enters the viewport — not called directly here.
    }
}

public async ValueTask DisposeAsync()
{
    try { await JS.InvokeVoidAsync("destroyHomepageAnimations"); } catch { }
    try { _carouselTimer?.Dispose(); } catch { }
    try { _heroTimer?.Dispose();    } catch { }
}
```

**`IDisposable` note:** The existing `@implements IDisposable` directive is replaced with `@implements IAsyncDisposable`. The synchronous `Dispose()` method is removed; cleanup moves to `DisposeAsync()`.

#### `destroyHomepageAnimations()`

```js
window.destroyHomepageAnimations = function() {
  // Disconnect all observers
  if (window._scrollRevealObserver) {
    window._scrollRevealObserver.disconnect();
    window._scrollRevealObserver = null;
  }
  if (window._stickyBarObserver) {
    window._stickyBarObserver.disconnect();
    window._stickyBarObserver = null;
  }
  // Reset sticky bar state
  const bar = document.querySelector('.sticky-booking-bar');
  if (bar) bar.classList.remove('sticky-bar--visible');
  const page = document.querySelector('.wellness-page');
  if (page) page.style.paddingBottom = '';
};
```

> **Implementation note:** `initScrollReveal` and `initStickyBar` must expose their observer instances on `window._scrollRevealObserver` and `window._stickyBarObserver` respectively so `destroyHomepageAnimations` can disconnect them. The IIFE closure pattern above stores the observer in `_observer`; an additional `window._scrollRevealObserver = _observer` assignment is needed inside each init function after creation.

---

### 10. Typography Upgrades

The `@import url(...)` for Playfair Display is already present in `app.css`. The additive CSS block that applies it to `.wellness-page` headings is already in the file. The only new rule needed is ensuring `h3` inside new sections is also covered:

```css
.wellness-page h1,
.wellness-page h2,
.wellness-page h3 {
    font-family: 'Playfair Display', Georgia, serif;
}
.wellness-page .section-kicker {
    text-transform: uppercase;
    letter-spacing: 0.18em;
    font-weight: 900;
    color: #008d36;
    font-size: 0.72rem;
    font-family: 'Outfit', sans-serif;
}
```

---

## Data Models Summary

```csharp
// Hero sub-carousel (existing)
private List<string> heroImages;          // 5 image paths
private int activeHeroIndex;
private Timer? _heroTimer;

// Gallery carousel (existing)
private record CarouselSlide(string ImagePath, string Caption);
private List<CarouselSlide> carouselSlides;   // 18 slides
private int activeSlideIndex;
private Timer? _carouselTimer;

// Service cards (existing)
private record ServiceCard(string Id, string Number, string Icon, string Name,
    string Summary, string[] Bullets, string AccentColor, string IconBg);
private List<ServiceCard> serviceCards;

// Social Proof (NEW)
private record StatCounter(string CountDisplay, double CountTarget, string Label);
private record Testimonial(string Quote, string RoleLabel);
private List<StatCounter> socialProofStats;   // 4 counters
private List<Testimonial> testimonials;        // ≥ 2 items, no patient names/photos

// Guarantee phrases (existing)
private readonly string[] guarantees;          // 5 phrases
```

---

## Error Handling

| Scenario | Handling |
|---|---|
| Hero image fails to load | `onerror="this.style.display='none'"` — existing pattern retained |
| `site.js` function not found | `try/catch` wraps all `InvokeVoidAsync` calls in `DisposeAsync`; init calls in `OnAfterRenderAsync` are not wrapped (failure surfaces as Blazor error boundary) |
| `initScrollReveal` called before DOM ready | Blazor calls it in `OnAfterRenderAsync`, guaranteeing DOM is present |
| IntersectionObserver not supported | Feature-detect: `if (!('IntersectionObserver' in window)) { /* fallback: add .revealed to all data-reveal elements */ }` |
| JS interop after component disposed | `DisposeAsync` `try/catch` swallows `JSDisconnectedException` |

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: JS Init Functions Called Exactly Once Per Page Lifecycle

*For any* number of `OnAfterRenderAsync` invocations on `Home.razor`, each of `initTypewriter`, `initScrollReveal`, `initGuaranteeTicker`, and `initStickyBar` SHALL be invoked via `IJSRuntime` exactly once (when `firstRender == true`), never on subsequent renders.

**Validates: Requirements 2.4, 4.6, 10.2**

---

### Property 2: Stat Counter Animation Reaches Exact Target Value

*For any* `StatCounter` record where `CountTarget` is a positive number, after the roll-up animation completes (duration ≥ 1200 ms), the text content of the corresponding `[data-count-target]` element SHALL equal the `CountDisplay` string exactly — no rounding error, no truncation.

**Validates: Requirements 1.3, 3.2, 3.3**

---

### Property 3: Sticky Bar Visibility Is the Inverse of Hero Viewport Presence

*For any* scroll position, when the `IntersectionObserver` callback fires for `.wellness-hero`:
- If `entry.isIntersecting == true` (hero partially or fully in viewport) → `.sticky-booking-bar` SHALL NOT have class `sticky-bar--visible`
- If `entry.isIntersecting == false` (hero fully scrolled past) → `.sticky-booking-bar` SHALL have class `sticky-bar--visible`

The two states are mutually exclusive and exhaustive.

**Validates: Requirements 4.2, 4.3**

---

### Property 4: Sticky Bar Bottom Padding Equals Bar Height

*For any* computed height `H` of `.sticky-booking-bar` when `sticky-bar--visible` is set, the `paddingBottom` of `.wellness-page` SHALL equal `H` pixels, ensuring no content is permanently obscured by the bar.

**Validates: Requirement 4.9**

---

### Property 5: Reduced-Motion Check Precedes Animation Setup in All `site.js` Functions

*For any* call to `initTypewriter`, `initStatCounters`, `initScrollReveal`, `initGuaranteeTicker`, or `initStickyBar`, the `window.matchMedia('(prefers-reduced-motion: reduce)').matches` check SHALL execute before any `IntersectionObserver` is created, any `setTimeout`/`requestAnimationFrame` is scheduled, or any CSS animation class is toggled.

**Validates: Requirements 1.4, 2.5, 3.6, 4.7, 5.4, 10.4**

---

### Property 6: CSS Selector Scope Isolation — No Bleed to `.dashboard-theme`

*For any* CSS rule added by this feature in `app.css`, the rule's selector SHALL begin with `.wellness-page`, `.auth-page`, or a public layout class. No new rule SHALL target `.dashboard-theme`, bare element selectors (e.g. `h1`, `body`), or global utility classes that would affect the admin portal.

**Validates: Requirement 9.6**

---

### Property 7: `initScrollReveal` Idempotency — One Observer Regardless of Call Count

*For any* number of calls N ≥ 1 to `initScrollReveal()`, the total number of active `IntersectionObserver` instances managing `[data-reveal]` elements SHALL be exactly 1. The IIFE closure guard (`if (_observer) return`) enforces this invariant.

**Validates: Requirements 2.3, 10.5**
