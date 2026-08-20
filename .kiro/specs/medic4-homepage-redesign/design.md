# Technical Design Document

## Feature: medic4-homepage-redesign

---

## Overview

This document describes the technical design for replacing the current dark "aesthetic clinic" homepage (`Home.razor`) with a light, professional medical layout inspired by the BeMedic4 template ([themes.muffingroup.com/be/medic4](https://themes.muffingroup.com/be/medic4/)). The redesign:

- Replaces the current `ac-*` CSS class system with a new `pt-*` (ProTouch) class system
- Introduces a dedicated scoped stylesheet `home-medic4.css` for all new page styles
- Adds non-blocking Google Fonts loading for Manrope and Instrument Sans in `App.razor`
- Restructures `Home.razor` markup into eight new sections matching the BeMedic4 page layout
- Preserves **all** existing `@code` C# logic, JS interop calls, and `site.js` functions without modification

### Scope Boundaries

| In Scope | Out of Scope |
|---|---|
| `Home.razor` markup (HTML/Razor template only) | `Home.razor` `@code` block — preserved verbatim |
| `home-medic4.css` — new dedicated stylesheet | `site.js` — no new JS; all existing functions reused |
| `App.razor` — font `<link>` tags added to `<head>` | `app.css` — dark tokens remain; new stylesheet uses `body.pt-homepage` scope to isolate |
| `PublicLayout.razor` — adds `pt-homepage` class to body (or handled via Home.razor wrapper) | Admin/dashboard pages — unaffected |

### Design Philosophy

The BeMedic4 template achieves medical credibility through four visual principles that this design adopts:

1. **Light surface, green accents** — off-white backgrounds with `#008d36` ProTouch green replace the current dark-glass aesthetic
2. **Typographic hierarchy** — Instrument Sans for headings creates editorial authority; Manrope for body text provides clean readability
3. **Trust-signal placement** — ISO certification kicker, stat counters, and specialist credentials appear at strategic conversion points
4. **Motion restraint** — animations are purposeful (scroll reveal, hero crossfade, stat counters) rather than decorative

---

## Architecture

### File Change Summary

```
ClinicApp/
├── Components/
│   ├── App.razor                          ← ADD: font preconnect + stylesheet links
│   ├── Pages/
│   │   └── Home.razor                     ← CHANGE: markup only; @code preserved
│   └── Layout/
│       └── PublicLayout.razor             ← MINOR CHANGE: add pt-homepage class to wrapper
└── wwwroot/
    ├── app.css                            ← MINOR CHANGE: add body.pt-homepage override block
    └── home-medic4.css                    ← NEW: all pt-* section styles
```

### CSS Isolation Strategy

The existing `app.css` contains a dark-themed global design system (`--bg-primary: #0b0c16`, dark backgrounds, teal/blue accent palette). Rather than removing it (which would break the admin/dashboard pages), all new ProTouch homepage styles are scoped under:

```css
body.pt-homepage { … }
/* or equivalently */
.pt-page { … }
```

`app.css` receives a minimal override block at the end that resets `html, body` background and colour **only** when `body.pt-homepage` is present. `PublicLayout.razor` adds the `pt-homepage` class to the `<body>` element via `OnAfterRenderAsync` JS call, or via a `<HeadContent>` body-class injection.

**Rationale:** This avoids touching the admin UI and keeps all new styles in one auditable file.

### JS Architecture

No new JavaScript is written. The existing `site.js` functions are fully reused with updated selector arguments where CSS class names change from `ac-*` to `pt-*`:

| `OnAfterRenderAsync` call | Selector argument change |
|---|---|
| `initStickyHeader` | `"#ac-header"` → `"#pt-header"` |
| `initActiveNavHighlight` | reads `.ac-nav a[href^="#"]` → `.pt-nav a[href^="#"]` (requires `site.js` update OR keep `ac-nav` class on new element) |
| `initTestiObserver` | reads `.ac-testimonials` → `.pt-testimonials` |
| `initStatCounters` | called as `'.pt-dark-stats'` from `initScrollReveal` |
| `scheduleDeferredInit_home` | unchanged |
| `initGalleryLightbox` | unchanged (DotNetRef-based, no selector) |

> **Decision:** To avoid any `site.js` changes, the new HTML will retain backward-compatible data/class hooks (e.g., `ac-testimonials`, `ac-nav`, `social-proof-section`) as **secondary classes** on the new `pt-*` elements. This means zero JS changes.

---

## Components and Interfaces

### Section Map

The page is structured as a single `Home.razor` containing all sections inline (matching the current pattern). No new Blazor components are extracted. The markup root changes from `<div class="ac-page">` to `<div class="pt-page">`.

```
pt-page
├── pt-header (id="pt-header")          [Req 3]
├── pt-mobile-nav                        [Req 3]
├── pt-hero (id="top")                   [Req 4]
├── pt-booking-banner                    [Req 5]
├── pt-body-parts (id="body-parts")     [Req 6]
├── pt-services (id="services")         [Req 7]
├── pt-how-it-works                      [Req 8]
├── pt-dark-cta                          [Req 9]
├── pt-specialists (id="team")          [Req 10]
├── pt-bmi (id="bmi-estimator")         [Req 11 — preserved]
├── pt-gallery (id="gallery")           [Req 12 — preserved]
├── pt-testimonials                      [Req 13 — preserved]
├── pt-faq                               [Req 14 — preserved]
├── pt-feedback (id="feedback")         [Req 15 — preserved]
├── pt-contact (id="contact")           [Req 16 — preserved]
├── pt-footer                            [Req 16]
└── pt-floating-cta                      [Req 17]
```

### Section HTML Blueprints

#### 1. Header (`pt-header`)

```html
<header class="pt-header" id="pt-header">
  <!-- Logo -->
  <a href="#top" class="pt-logo">
    <img src="/images/logo.webp" alt="ProTouch Physio" />
  </a>

  <!-- Desktop nav — retains ac-nav class for JS compat -->
  <nav class="pt-nav ac-nav" role="navigation" aria-label="Main navigation">
    <a href="#services">Services</a>
    <a href="#body-parts">Treatments</a>
    <a href="#team">Team</a>
    <a href="#feedback">Feedback</a>
    <a href="#contact">Contact</a>
  </nav>

  <!-- CTA button -->
  <div class="pt-header-actions">
    <a class="pt-btn-primary" href="/book">Book a Visit</a>
  </div>

  <!-- Hamburger -->
  <button class="pt-hamburger" type="button"
          @onclick="ToggleMobileMenu"
          aria-label="Toggle navigation menu"
          aria-expanded="@_mobileMenuOpen">
    <span></span><span></span><span></span>
  </button>
</header>

<!-- Mobile nav overlay -->
<div class="pt-mobile-nav @(_mobileMenuOpen ? "pt-mobile-nav--open" : "")">
  <a href="#services" @onclick="ToggleMobileMenu">Services</a>
  <a href="#body-parts" @onclick="ToggleMobileMenu">Treatments</a>
  <a href="#team" @onclick="ToggleMobileMenu">Team</a>
  <a href="#feedback" @onclick="ToggleMobileMenu">Feedback</a>
  <a href="#contact" @onclick="ToggleMobileMenu">Contact</a>
  <a class="pt-btn-primary" href="/book" @onclick="ToggleMobileMenu">Book a Visit</a>
</div>
```

**State transitions (CSS only):**
- Default: `background: transparent`, `color: var(--pt-white)` — overlays hero
- `.pt-header--scrolled` (added by `initStickyHeader` JS): `background: var(--pt-white)`, `color: var(--pt-heading)`, `box-shadow: 0 2px 20px rgba(0,0,0,0.08)`
- Transition: `background-color 200ms ease, color 200ms ease`

#### 2. Hero Section (`pt-hero`)

```html
<section class="pt-hero" id="top">
  <!-- Left: text panel -->
  <div class="pt-hero-left">
    <p class="pt-kicker">✦ ISO 9001:2015 CERTIFIED CLINIC</p>
    <h1 class="pt-hero-h1">Take care of your health</h1>
    <p class="pt-hero-sub">
      Physiotherapy-led slimming, rehabilitation and wellness plans by
      Dr. Manjari — created for lasting results, not temporary fixes.
    </p>
    <div class="pt-hero-stats">
      <div class="pt-stat-pill">
        <span class="pt-stat-num">99%</span>
        <span class="pt-stat-label">Cured</span>
      </div>
      <span class="pt-stat-divider"></span>
      <div class="pt-stat-pill">
        <span class="pt-stat-num">4–8 kg</span>
        <span class="pt-stat-label">/ Month</span>
      </div>
    </div>
    <div class="pt-hero-actions">
      <a class="pt-btn-primary" href="/book">Book a Visit</a>
      <a class="pt-hero-link" href="#services">Our Services →</a>
    </div>
  </div>

  <!-- Right: CSS crossfade slideshow (existing logic preserved) -->
  <div class="pt-hero-right">
    @for (var i = 0; i < heroImages.Count; i++) {
      var idx = i;
      <div class="pt-hero-slide @(idx == 0 ? "pt-hero-slide--1" : idx == 1 ? "pt-hero-slide--2" : "pt-hero-slide--3")">
        <picture>
          <source srcset="@Path.ChangeExtension(heroImages[idx], ".webp")" type="image/webp" />
          <img src="@heroImages[idx]" alt="ProTouch Clinic"
               loading="@(idx == 0 ? "eager" : "lazy")"
               fetchpriority="@(idx == 0 ? "high" : "auto")" />
        </picture>
      </div>
    }
    <div class="pt-hero-overlay-strip"></div>
  </div>
</section>
```

**CSS crossfade:** Three `.pt-hero-slide` elements use `@keyframes ptHeroCrossfade` — each slide has `animation-delay: 0s / 5s / 10s` cycling at `15s` total. `@media (prefers-reduced-motion: reduce)` pauses the animation on slides 2 and 3.

#### 3. Booking Banner (`pt-booking-banner`)

```html
<section class="pt-booking-banner" aria-label="Booking call to action">
  <p class="pt-banner-text">Ready to start your wellness journey?</p>
  <a class="pt-btn-inverse" href="/book">Book a Visit Now →</a>
</section>
```

#### 4. Body-Part Treatments Section (`pt-body-parts`)

```html
<section class="pt-body-parts" id="body-parts" data-reveal>
  <div class="pt-body-parts-inner">
    <p class="pt-kicker">TREATMENTS BY AREA</p>
    <h2>We can help with any part of you</h2>

    <!-- Tab row — ARIA tablist pattern -->
    <div class="pt-bp-tabs" role="tablist" aria-label="Body area treatments">
      @foreach (var tab in bodyPartTabs) {
        <button class="pt-bp-tab @(activeBodyPart == tab.Id ? "pt-bp-tab--active" : "")"
                type="button"
                role="tab"
                id="tab-@tab.Id"
                aria-selected="@(activeBodyPart == tab.Id ? "true" : "false")"
                aria-controls="panel-@tab.Id"
                @onclick="() => SetBodyPart(tab.Id)">
          @tab.Label
        </button>
      }
    </div>

    <!-- Panel content -->
    @{
      var activeTab = bodyPartTabs.FirstOrDefault(t => t.Id == activeBodyPart) ?? bodyPartTabs[0];
    }
    <div class="pt-bp-panel"
         id="panel-@activeTab.Id"
         role="tabpanel"
         aria-labelledby="tab-@activeTab.Id">
      <div class="pt-bp-panel-left">
        <h3>@activeTab.Label</h3>
        <p>@activeTab.Description</p>
        <a class="pt-btn-primary" href="/book">Book Treatment →</a>
      </div>
      <div class="pt-bp-panel-right" aria-hidden="true">
        <img src="/images/clinic-gallery/gallery-27.jpeg"
             alt="@activeTab.Label treatment"
             loading="lazy" />
      </div>
    </div>
  </div>
</section>
```

#### 5. Services Section (`pt-services`)

```html
<section class="pt-services" id="services" data-reveal>
  <div class="pt-services-header">
    <p class="pt-kicker">WHAT WE OFFER</p>
    <h2>Our Services</h2>
    <p class="pt-services-intro">
      Expert-led programmes for weight loss, physiotherapy, yoga and
      specialised care — all under one roof, no fasting, no surgery.
    </p>
  </div>

  <!-- Horizontally scrollable card row -->
  <div class="pt-services-row">
    @{var isFirst = true;}
    @foreach (var svc in serviceCards) {
      <div class="pt-svc-card" style="--svc-accent: @svc.AccentColor">
        <div class="pt-svc-card-img">
          <img src="@GetServiceImage(svc)" alt="@svc.Name"
               loading="@(isFirst ? "eager" : "lazy")"
               fetchpriority="@(isFirst ? "high" : "auto")" />
        </div>
        <div class="pt-svc-card-body">
          <h3>@svc.Name</h3>
          <p>@svc.Summary</p>
          <a class="pt-svc-link" href="/services/@svc.Id">View & Book →</a>
        </div>
      </div>
      isFirst = false;
    }
  </div>
</section>
```

#### 6. How It Works Section (`pt-how-it-works`)

```html
<section class="pt-how-it-works" data-reveal>
  <div class="pt-how-inner">
    <p class="pt-kicker">HOW IT WORKS</p>
    <h2>Our team of specialists is at your service</h2>

    <div class="pt-steps">
      @foreach (var step in howItWorksSteps) {
        <div class="pt-step">
          <span class="pt-step-num" aria-hidden="true">@step.Number</span>
          <h3 class="pt-step-title">@step.Title</h3>
          <p class="pt-step-desc">@step.Description</p>
        </div>
      }
    </div>

    <blockquote class="pt-pull-quote">
      <p>"The best care for lasting results!"</p>
      <footer>— ISO 9001:2015 Certified Clinic</footer>
    </blockquote>
  </div>
</section>
```

#### 7. Dark CTA Section with Stats (`pt-dark-cta`)

```html
<section class="pt-dark-cta" data-reveal>
  <div class="pt-dark-cta-inner">
    <div class="pt-dark-cta-copy">
      <h2>Take care of your health anytime, anywhere</h2>
      <p>
        Available for in-clinic and teleconsultation appointments.
        Dr. Manjari's team is ready to build your personalised plan today.
      </p>
      <a class="pt-btn-primary" href="/book">Book a Visit Now →</a>
    </div>

    <!-- Animated counters — retains social-proof-section + ac-stats for JS compat -->
    <div class="pt-dark-stats social-proof-section ac-stats" data-reveal>
      @foreach (var stat in socialProofStats) {
        <div class="pt-dark-stat">
          <span class="pt-dark-stat-num"
                data-count-target="@stat.CountTarget"
                data-count-display="@stat.CountDisplay">@stat.CountDisplay</span>
          <span class="pt-dark-stat-label">@stat.Label</span>
        </div>
      }
    </div>
  </div>
</section>
```

> **JS Note:** `initStatCounters` is triggered by `initScrollReveal` when the element with class `social-proof-section` enters the viewport. Retaining that secondary class preserves the behavior with zero JS changes.

#### 8. Specialists Section (`pt-specialists`)

```html
<section class="pt-specialists" id="team" data-reveal>
  <div class="pt-specialists-inner">
    <p class="pt-kicker">MEET THE TEAM</p>
    <h2>Our team of specialists is at your service</h2>

    <div class="pt-specialists-grid">
      <!-- Primary: Dr. Manjari -->
      <div class="pt-specialist-card pt-specialist-card--primary">
        <div class="pt-specialist-photo">
          <img src="/images/clinic-gallery/gallery-27.jpeg"
               alt="Dr. Manjari — Physiotherapist & Slimming Expert"
               loading="lazy" />
        </div>
        <div class="pt-specialist-body">
          <h3>Dr. Manjari</h3>
          <p class="pt-specialist-title">Physiotherapist & Slimming Expert</p>
          <ul class="pt-specialist-creds">
            @foreach (var cred in doctorCredentials) {
              <li>@cred</li>
            }
          </ul>
          <a class="pt-btn-primary" href="/book">Book Consultation →</a>
        </div>
      </div>

      <!-- Supporting practitioners (placeholders) -->
      @foreach (var practitioner in supportingPractitioners) {
        <div class="pt-specialist-card">
          <div class="pt-specialist-photo pt-specialist-photo--placeholder">
            <span aria-hidden="true">👤</span>
          </div>
          <div class="pt-specialist-body">
            <h3>@practitioner.Title</h3>
            <a class="pt-link" href="#contact">Learn More →</a>
          </div>
        </div>
      }
    </div>
  </div>
</section>
```

---

## Data Models

All data models are unchanged from the current `Home.razor @code` block. The design introduces two new data structures to support the body-part tabs and how-it-works steps, both initialized in `OnInitialized`.

### New: `BodyPartTab` Record

```csharp
private record BodyPartTab(string Id, string Label, string Description);
```

Initialized in `OnInitialized`:
```csharp
bodyPartTabs = new List<BodyPartTab>
{
    new("neck-shoulder",  "Neck & Shoulder",
        "Physiotherapy for cervical spondylitis, neck pain, frozen shoulder and upper-limb rehabilitation"),
    new("back-spine",     "Back & Spine",
        "Treatment for lumbar spondylitis, sciatica, slipped disc and post-operative spinal care"),
    new("knee-leg",       "Knee & Leg",
        "Knee pain, arthritis, ligament rehabilitation and sports injury recovery"),
    new("arm-elbow",      "Arm & Elbow",
        "Tennis elbow, golfer's elbow, repetitive strain and fracture rehabilitation"),
    new("abdomen-core",   "Abdomen & Core",
        "Core strengthening, diastasis recti, post-natal rehabilitation and inch-loss programmes"),
    new("full-body",      "Full Body Wellness",
        "Weight loss, inch loss, yoga therapy, Russian nature cure and overall body contouring"),
};
activeBodyPart = "neck-shoulder";
```

### New: `HowItWorksStep` Record

```csharp
private record HowItWorksStep(string Number, string Title, string Description);
```

Initialized in `OnInitialized`:
```csharp
howItWorksSteps = new List<HowItWorksStep>
{
    new("01", "Book a Consultation",
        "Choose your preferred service and submit a booking request online or by calling 96735 30031. "
      + "Our team will confirm your appointment within 24 hours."),
    new("02", "Personalised Assessment",
        "Dr. Manjari conducts a thorough assessment of your health profile, goals and medical history. "
      + "A tailored treatment plan is created specifically for you."),
    new("03", "Start Your Journey",
        "Begin your programme with expert guidance, track your progress and achieve lasting results "
      + "— no fasting, no medicine, no surgery."),
};
```

### New: `SupportingPractitioner` Record

```csharp
private record SupportingPractitioner(string Title);
```

Initialized in `OnInitialized`:
```csharp
supportingPractitioners = new List<SupportingPractitioner>
{
    new("Senior Physiotherapist"),
    new("Yoga Therapist"),
    new("Diet & Nutrition Consultant"),
};
```

### New: State Fields

```csharp
private string activeBodyPart = "neck-shoulder";
private List<BodyPartTab> bodyPartTabs = new();
private List<HowItWorksStep> howItWorksSteps = new();
private List<SupportingPractitioner> supportingPractitioners = new();
private List<string> doctorCredentials = new();

private void SetBodyPart(string id) => activeBodyPart = id;
```

`doctorCredentials` is populated in `OnInitialized` with the existing six bullet strings from the current Doctor section.

### Preserved Data Models (Unchanged)

All existing records and state fields remain:
- `ServiceCard`, `StatCounter`, `Testimonial`, `FaqItem` records
- `heroImages`, `serviceCards`, `socialProofStats`, `testimonials`, `faqItems`, `guarantees` lists
- All BMI state fields (`_heightCm`, `_weightKg`, `_bmiGoal`, etc.)
- All gallery state fields (`_galleryVisible`, `_isLightboxOpen`, etc.)
- All feedback state fields (`_feedbackRating`, `_feedbackComment`, etc.)

### CSS Design Token System

All CSS custom properties are defined on `.pt-page` (scoped to the homepage wrapper, not `:root`, to avoid conflicting with the existing dark `app.css` global tokens):

```css
.pt-page {
  /* Brand colours */
  --pt-primary:   #008d36;
  --pt-mint:      #B8E5D2;
  --pt-surface:   #EDF4F2;
  --pt-cta-bg:    #DAFFE7;
  --pt-dark:      #0d2b1e;
  --pt-text:      #737373;
  --pt-heading:   #1a1a1a;
  --pt-white:     #ffffff;

  /* Typography */
  --pt-font-body:    'Manrope', system-ui, -apple-system, sans-serif;
  --pt-font-heading: 'Instrument Sans', Georgia, serif;

  /* Type scale */
  --pt-text-sm:   0.875rem;
  --pt-text-base: 1rem;
  --pt-text-lg:   1.125rem;
  --pt-text-xl:   1.25rem;
  --pt-text-2xl:  1.5rem;
  --pt-text-3xl:  2rem;
  --pt-text-4xl:  2.75rem;
  --pt-text-5xl:  3.5rem;

  /* Spacing */
  --pt-section-v: 5rem;       /* vertical section padding desktop */
  --pt-section-v-mob: 3rem;   /* vertical section padding mobile */

  /* Shadows */
  --pt-shadow-card: 0 4px 24px rgba(0, 0, 0, 0.07);
  --pt-shadow-hover: 0 8px 32px rgba(0, 141, 54, 0.15);

  /* Transitions */
  --pt-trans-fast: 150ms ease;
  --pt-trans-med:  200ms ease;

  /* Reset dark globals from app.css for this scope */
  background: var(--pt-white);
  color: var(--pt-text);
  font-family: var(--pt-font-body);
}
```

#### Typography Application

```css
/* Headings */
.pt-page h1, .pt-page h2, .pt-page h3 {
  font-family: var(--pt-font-heading);
  color: var(--pt-heading);
  letter-spacing: -0.02em;
}

/* Body */
.pt-page p, .pt-page li, .pt-page label, .pt-page span {
  font-family: var(--pt-font-body);
}

/* Links reset (overrides dark app.css teal link colour) */
.pt-page a {
  color: inherit;
  text-shadow: none;
}
.pt-page a:hover {
  color: var(--pt-primary);
  text-shadow: none;
}
```

---

## CSS Architecture — `home-medic4.css`

### File Structure

```css
/* ── 0. Design tokens (scoped to .pt-page) ── */
/* ── 1. Global resets within .pt-page ── */
/* ── 2. Utility classes (.pt-kicker, .pt-btn-primary, etc.) ── */
/* ── 3. Header (.pt-header) ── */
/* ── 4. Hero (.pt-hero) ── */
/* ── 5. Booking Banner (.pt-booking-banner) ── */
/* ── 6. Body Parts (.pt-body-parts) ── */
/* ── 7. Services (.pt-services) ── */
/* ── 8. How It Works (.pt-how-it-works) ── */
/* ── 9. Dark CTA (.pt-dark-cta) ── */
/* ── 10. Specialists (.pt-specialists) ── */
/* ── 11. BMI Widget (.pt-bmi) ── */
/* ── 12. Gallery (.pt-gallery) ── */
/* ── 13. Testimonials (.pt-testimonials) ── */
/* ── 14. FAQ (.pt-faq) ── */
/* ── 15. Feedback (.pt-feedback) ── */
/* ── 16. Contact + Footer ── */
/* ── 17. Floating CTA ── */
/* ── 18. Scroll Reveal animation ── */
/* ── 19. Back to top ── */
/* ── 20. Responsive breakpoints ── */
```

### Responsive Breakpoints

```css
/* Tablet */
@media (max-width: 1024px) { … }

/* Mobile */
@media (max-width: 768px) { … }

/* Small mobile */
@media (max-width: 480px) { … }
```

### Key Layout Patterns

#### Header (transparent → solid on scroll)

```css
.pt-header {
  position: fixed;
  top: 0; left: 0; right: 0;
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem 2rem;
  background: transparent;
  color: var(--pt-white);
  transition: background-color var(--pt-trans-med), color var(--pt-trans-med),
              box-shadow var(--pt-trans-med);
}
.pt-header.ac-header--scrolled {   /* class added by initStickyHeader JS */
  background: var(--pt-white);
  color: var(--pt-heading);
  box-shadow: 0 2px 20px rgba(0,0,0,0.08);
}
```

> **Note:** `initStickyHeader` in `site.js` adds `.ac-header--scrolled` (hardcoded in the JS). The HTML element id changes to `#pt-header` but the JS-applied class name stays `.ac-header--scrolled` — no JS change needed, the CSS simply styles both classes.

#### Hero Layout (split 50/50 desktop → stacked mobile)

```css
.pt-hero {
  min-height: 100vh;
  display: grid;
  grid-template-columns: 1fr 1fr;
  overflow: hidden;
  padding-top: 80px; /* header height */
}
.pt-hero-left {
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding: 4rem 3rem 4rem 6rem;
  background: var(--pt-white);
}
.pt-hero-right {
  position: relative;
  overflow: hidden;
}

@media (max-width: 768px) {
  .pt-hero {
    grid-template-columns: 1fr;
    min-height: 75vh;
  }
  .pt-hero-right {
    max-height: 40vh;
  }
  /* stack: text above image */
  .pt-hero-left { order: 1; padding: 2rem; }
  .pt-hero-right { order: 2; }
}
```

#### Hero Crossfade (CSS-only, 3 slides)

```css
.pt-hero-slide {
  position: absolute;
  inset: 0;
  opacity: 0;
}
.pt-hero-slide img {
  width: 100%; height: 100%;
  object-fit: cover;
}

@keyframes ptHeroCrossfade {
  0%, 28%   { opacity: 1; }
  33%, 95%  { opacity: 0; }
  100%      { opacity: 0; }
}

.pt-hero-slide--1 { animation: ptHeroCrossfade 15s infinite 0s; }
.pt-hero-slide--2 { animation: ptHeroCrossfade 15s infinite 5s; }
.pt-hero-slide--3 { animation: ptHeroCrossfade 15s infinite 10s; }

@media (prefers-reduced-motion: reduce) {
  .pt-hero-slide--1 { opacity: 1; animation: none; }
  .pt-hero-slide--2, .pt-hero-slide--3 { display: none; }
}
```

#### Services Row (horizontal scroll desktop, stack mobile)

```css
.pt-services-row {
  display: flex;
  gap: 1.5rem;
  overflow-x: auto;
  scroll-snap-type: x mandatory;
  -webkit-overflow-scrolling: touch;
  padding-bottom: 1rem; /* scrollbar clearance */
}
.pt-svc-card {
  flex: 0 0 280px;
  scroll-snap-align: start;
  border-radius: 12px;
  box-shadow: var(--pt-shadow-card);
  transition: box-shadow var(--pt-trans-med), transform var(--pt-trans-med);
}
.pt-svc-card:hover {
  box-shadow: var(--pt-shadow-hover);
  transform: translateY(-4px);
}

@media (max-width: 768px) {
  .pt-services-row {
    flex-direction: column;
    overflow-x: visible;
  }
  .pt-svc-card { flex: none; width: 100%; }
}
```

#### Scroll Reveal

```css
[data-reveal] {
  opacity: 0;
  transform: translateY(24px);
  transition: opacity 0.5s ease, transform 0.5s ease;
}
[data-reveal].revealed {
  opacity: 1;
  transform: none;
}
@media (prefers-reduced-motion: reduce) {
  [data-reveal] { opacity: 1; transform: none; }
}
```

### `App.razor` Font Loading Changes

The following is added to `<head>` in `App.razor`, immediately before the existing `<HeadOutlet />`:

```html
<!-- Non-blocking Google Fonts: Manrope + Instrument Sans (Req 2, 18) -->
<link rel="preconnect" href="https://fonts.googleapis.com" />
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
<link rel="stylesheet"
      href="https://fonts.googleapis.com/css2?family=Manrope:wght@400;500;600&family=Instrument+Sans:wght@400;600;700&display=swap"
      media="print"
      onload="this.media='all'" />
<noscript>
  <link rel="stylesheet"
        href="https://fonts.googleapis.com/css2?family=Manrope:wght@400;500;600&family=Instrument+Sans:wght@400;600;700&display=swap" />
</noscript>
<!-- Home-page BeMedic4 redesign styles -->
<link rel="stylesheet" href="@Assets["home-medic4.css"]" />
```

The `media="print"` → `onload="this.media='all'"` pattern loads the font CSS non-blocking, preventing render-blocking. The `<noscript>` fallback ensures fonts load for users with JS disabled. The `display=swap` parameter in the URL instructs Google Fonts to set `font-display: swap` on the generated `@font-face` declarations, preventing invisible text (FOIT) during load. ([Source: pagespeedchecklist.com](https://pagespeedchecklist.com/asynchronous-google-fonts))

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: All CSS design tokens are defined on `.pt-page`

*For any* rendered ProTouch homepage, the `.pt-page` element SHALL have all eight required color custom property values (`--pt-primary`, `--pt-mint`, `--pt-surface`, `--pt-cta-bg`, `--pt-dark`, `--pt-text`, `--pt-heading`, `--pt-white`) resolvable via `getComputedStyle`.

**Validates: Requirements 1.1, 1.2, 1.3**

---

### Property 2: No hard-coded hex colours in markup

*For any* element within `.pt-page`, if the element has an inline `style` attribute, that attribute SHALL NOT contain a literal hex colour value (matching pattern `#[0-9a-fA-F]{3,6}`) except for service card accent values passed via C# `--svc-accent` CSS variable declarations.

**Validates: Requirements 1.3**

---

### Property 3: All required section IDs are present

*For any* rendered homepage document, the following element IDs SHALL be present in the DOM: `pt-header`, `top`, `services`, `body-parts`, `team`, `bmi-estimator`, `gallery`, `feedback`, `contact`.

**Validates: Requirements 3.1, 4.1, 5.1, 6.1, 7.1, 10.1, 11.1, 12.1, 15.1, 16.1**

---

### Property 4: Body-part tab selection renders correct panel

*For any* tab identifier in the set `{neck-shoulder, back-spine, knee-leg, arm-elbow, abdomen-core, full-body}`, when `SetBodyPart(id)` is called, the resulting rendered panel SHALL:
- contain an element with `aria-labelledby="tab-{id}"`
- display the description text corresponding to that tab's `BodyPartTab.Description` value

**Validates: Requirements 6.3**

---

### Property 5: ARIA tab attributes are consistent

*For any* rendered body-parts section with `n` tabs, exactly **one** tab button SHALL have `aria-selected="true"`, and the remaining `n-1` tab buttons SHALL have `aria-selected="false"`. The `aria-selected="true"` button SHALL match the value of `activeBodyPart`.

**Validates: Requirements 6.2, 19.6**

---

### Property 6: Feedback star selection round-trip

*For any* star rating value `s` in `{1, 2, 3, 4, 5}`, after calling `SetFeedbackStar(s)`, the rendered star row SHALL have exactly `s` buttons with class `ac-star--lit` (or the equivalent `pt-star--lit`) and `5 - s` buttons without it.

**Validates: Requirements 15.4**

---

### Property 7: BMI computation is deterministic and produces valid category

*For any* height `h` in `[100, 250]` cm and weight `w` in `[20, 300]` kg, calling `ComputeBmi` with those values SHALL:
- produce a `_bmiValue` equal to `Math.Round(w / (h/100)^2, 1)`
- set `_bmiCategory` to exactly one of `{"Underweight", "Normal", "Overweight", "Obese"}`
- set `_bmiColor` to the value corresponding to that category in the mapping table

**Validates: Requirements 11.3, 11.7**

---

### Property 8: Gallery load-more grows visible count by 12 per activation

*For any* initial visible count `v` less than 60, calling `LoadMoreGallery()` SHALL increase `_galleryVisible` by exactly 12, or set it to 60 if `v + 12 > 60`.

**Validates: Requirements 12.4**

---

### Property 9: Testimonial carousel index is always in bounds

*For any* testimonial list of length `n`, calling `NextTesti()` or `PrevTesti()` any number of times SHALL always result in `activeTestiIndex` in the range `[0, n-1]`.

**Validates: Requirements 13.1, 13.2**

---

### Property 10: FAQ accordion toggle is a consistent toggle

*For any* FAQ item index `i` in `[0, n-1]` and any current open index `openFaq`, calling `ToggleFaq(i)` SHALL produce:
- `openFaq == -1` if `i == openFaq` before the call (closes the open item)
- `openFaq == i` if `i != openFaq` before the call (opens the new item and implicitly closes the previous one)

*For any* two distinct indices `i` and `j`, calling `ToggleFaq(i)` then `ToggleFaq(j)` SHALL result in `openFaq == j` (single-open invariant: opening a new item always closes the previous).

**Validates: Requirements 14.1, 14.2**

---

## Error Handling

### Font Loading Failure (Requirement 2.4)

If Google Fonts fails to load (network error, DNS failure, content blocker), the page remains readable because:
1. The CSS `--pt-font-body` fallback stack (`system-ui, -apple-system, sans-serif`) is always available
2. The CSS `--pt-font-heading` fallback stack (`Georgia, serif`) is always available
3. Font loading uses `media="print"` / `onload` — a network failure simply means `this.media='all'` is never executed, so the font stylesheet never loads; the browser renders with system fonts from the start

**No error UI or toast needed** — this is a silent graceful degradation.

### JS Interop Failures (Requirement 20.4)

All `OnAfterRenderAsync` JS calls are wrapped in `try { … } catch { }`. If `initStickyHeader`, `initScrollReveal`, etc. throw (e.g., the DOM element not found because of a render race), the exception is suppressed and the page continues to function statically. The transparent header simply does not become opaque; scroll reveals do not animate; stat counters show their initial display values.

### BMI Validation Errors (Requirements 11.4–11.6)

The `ComputeBmi()` method validates height, weight, and goal before computing. Validation errors are displayed inline via `_heightError`, `_weightError`, and `_goalError` string fields rendered as `<span class="pt-field-error">`. The BMI result panel is not shown (`_bmiValue` remains `null`) until all inputs are valid.

### Gallery Lightbox Boundary (Requirement 12.3)

`LightboxNext()` and `LightboxPrev()` use modular arithmetic: `(_selectedLightboxIndex + 1) % _galleryVisible`. The index never goes out of bounds. The ESC key handler in `site.js` calls the `[JSInvokable] CloseLightbox()` method which is wrapped in a try/catch in JS.

### Feedback Submission Errors (Requirement 15)

If `ClinicService.SaveFeedbackAsync` throws, the catch block sets `_feedbackError = "Unable to save your feedback. Please try again."` and does not show the success message. The `_feedbackSubmitting` flag is reset to `false` in the `finally` block ensuring the button re-enables.

---

## Testing Strategy

### PBT Applicability Assessment

This feature is primarily a **markup and CSS redesign** of a Blazor server-side page. The `@code` block contains pure C# logic (BMI computation, gallery pagination, testimonial carousel, FAQ toggle, feedback validation) that is well-suited to property-based testing. UI rendering and CSS layout are **not** appropriate for PBT — those are tested with snapshot/visual regression approaches.

**PBT IS appropriate for:** BMI computation, gallery pagination, testimonial index bounds, tab ARIA consistency, feedback star state, CSS token presence (via DOM assertions).

**PBT IS NOT appropriate for:** CSS layout/responsive breakpoints (use visual regression), font loading (use integration/smoke test), `App.razor` HTML structure (use snapshot test).

### Dual Testing Approach

#### Property-Based Tests (using `FsCheck` — F# compatible with xUnit in `ClinicApp.Tests`)

Each property test runs a minimum of **100 iterations** via FsCheck's default generator. Tests are tagged with:
`// Feature: medic4-homepage-redesign, Property {N}: {property_text}`

| Test | Property Validated | PBT Pattern |
|---|---|---|
| `CssTokensPresent_ForAnyRenderedPage` | Property 1 | Smoke / integration assertion |
| `NoHardcodedHexInMarkup` | Property 2 | Invariant |
| `BodyPartTabSelection_RendersCorrectPanel` | Property 4 | Round-trip (set → get) |
| `AriaTabAttributes_ExactlyOneSelected` | Property 5 | Invariant |
| `FeedbackStarSelection_CorrectLitCount` | Property 6 | Invariant (count) |
| `BmiComputation_DeterministicAndValidCategory` | Property 7 | Metamorphic + Invariant |
| `GalleryLoadMore_GrowsByTwelve` | Property 8 | Invariant (arithmetic) |
| `TestimonialIndex_AlwaysInBounds` | Property 9 | Invariant (modular arithmetic) |
| `FaqToggle_ConsistentToggleBehavior` | Property 10 | Invariant (toggle) |

#### Unit Tests (xUnit, example-based)

- `ComputeBmi_HeightOutOfRange_ShowsError` — validates Requirement 11.4
- `ComputeBmi_WeightOutOfRange_ShowsError` — validates Requirement 11.5
- `ComputeBmi_NoGoal_ShowsError` — validates Requirement 11.6
- `SubmitFeedback_NoRating_ShowsError` — validates Requirement 15.3
- `SubmitFeedback_WithRating_CallsService` — validates Requirement 15.2
- `LoadMoreGallery_AtCeiling_CapsAt60` — validates Requirement 12.4 edge case
- `ToggleFaq_SameIndex_Closes` — validates Requirement 14.2 accordion behaviour
- `ToggleFaq_DifferentIndex_SwitchesOpen` — validates Requirement 14.2

#### Snapshot / Visual Regression Tests

- Render `Home.razor` with Blazor test renderer (bUnit) and assert:
  - `<h1>` count equals 1 (Requirement 19.2)
  - Section IDs present: `services`, `body-parts`, `team`, `gallery`, `feedback`, `contact` (Property 3)
  - Tab buttons have `role="tab"` and are inside a `role="tablist"` container (Requirement 19.6)
  - Step number spans have `aria-hidden="true"` (Requirement 19.8)
  - Lightbox `role="dialog"` and `aria-modal="true"` present (Requirement 19.3)

#### Integration / Smoke Tests

- **Font loading:** Page loads in browser, Lighthouse audit confirms no render-blocking font resources
- **Sticky header:** Scroll test confirms `.ac-header--scrolled` class added after 80px scroll
- **Stat counters:** DarkCTA section enters viewport, counters animate from 0 to target
- **Reduced-motion:** `prefers-reduced-motion: reduce` set → hero shows only first image, counters show final values immediately

### Property Test Configuration

```csharp
// FsCheck default: 100 iterations per test
// Custom config for heavier tests:
var config = new FsCheck.Configuration { MaxNbOfTest = 500 };
```

Tag format used in each test:
```csharp
// Feature: medic4-homepage-redesign, Property 7: BMI computation is deterministic
```
