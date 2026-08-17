# Implementation Plan: Homepage Attraction Redesign

## Overview

Add dynamic animations, two new sections (Social Proof, Floating Sticky Bar), and visual polish to the ProTouch Physio public homepage. All changes target exactly three files: `site.js` (6 JS functions), `Home.razor` (markup/wiring), and `app.css` (additive CSS). The aesthetic is a professional physiotherapy and slimming clinic — clinically credible, focused on physiotherapy and clinical slimming treatment. No existing CSS rules are modified; every new rule is additive and scoped to `.wellness-page`.

---

## Tasks

- [ ] 1. Add all six `site.js` animation functions
  - [ ] 1.1 Implement `initTypewriter(selector, minDelay)` in `site.js`
    - Export on `window`; read full text content, clear element, re-append char-by-char via `setTimeout` at `minDelay` ms intervals
    - Add `prefers-reduced-motion` guard at top — return immediately if reduce is set (text stays fully visible)
    - _Requirements: 1.1, 1.2, 1.4, 10.1, 10.4_

  - [ ] 1.2 Implement `initStatCounters(containerSelector, duration)` in `site.js`
    - Export on `window`; query all `[data-count-target]` descendants inside `containerSelector`
    - Animate from 0 → `data-count-target` over `duration` ms using `requestAnimationFrame` + easeOutCubic (`1 - Math.pow(1-t, 3)`)
    - On animation end, snap text to `data-count-display` to guarantee exact formatted value (no rounding residue)
    - Add `prefers-reduced-motion` guard — skip to final `data-count-display` immediately if reduce is set
    - _Requirements: 1.3, 3.3, 3.6, 10.1, 10.4_

  - [ ] 1.3 Implement `initScrollReveal()` as an IIFE-closure in `site.js`
    - Export on `window`; use closure variable `_observer` initialised to `null` as idempotency guard (`if (_observer) return`)
    - Add `prefers-reduced-motion` guard — if reduce is set, add `.revealed` to all `[data-reveal]` elements immediately and return without creating an observer
    - Create one `IntersectionObserver` with `threshold: 0.15`; on intersection add `.revealed` and `unobserve`
    - When `.social-proof-section` enters viewport, also call `window.initStatCounters('.social-proof-section', 1200)`
    - Store observer on `window._scrollRevealObserver` after creation (required for `destroyHomepageAnimations`)
    - Add `IntersectionObserver` feature-detect fallback: if not supported, add `.revealed` to all `[data-reveal]` elements immediately
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7, 3.3, 3.6, 10.1, 10.4, 10.5, 10.7_

  - [ ] 1.4 Implement `initGuaranteeTicker()` in `site.js`
    - Export on `window`; query `.wellness-page .guarantee-ticker-track`
    - If `prefers-reduced-motion` reduce is detected, set `ticker.style.animationPlayState = 'paused'`
    - Function is otherwise a no-op (CSS handles the animation); register it for idempotency compliance
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 10.1, 10.4_

  - [ ] 1.5 Implement `initStickyBar(heroSelector, barSelector)` as an IIFE-closure in `site.js`
    - Export on `window`; use closure variable `_observer` initialised to `null` as idempotency guard (`if (_observer) return`)
    - Resolve `hero` and `bar` via `document.querySelector`; return early if either is missing
    - Add `prefers-reduced-motion` guard — if reduce is set, set `bar.style.transition = 'none'` before creating observer
    - Create one `IntersectionObserver` with `threshold: 0, rootMargin: '0px'`; toggle class `sticky-bar--visible` on bar when `entry.isIntersecting` is false (hero fully scrolled past)
    - When bar becomes visible, set `document.querySelector('.wellness-page').style.paddingBottom = bar.offsetHeight + 'px'`; when hidden, reset to `''`
    - Store observer on `window._stickyBarObserver` after creation (required for `destroyHomepageAnimations`)
    - _Requirements: 4.1, 4.2, 4.3, 4.6, 4.7, 4.9, 10.1, 10.4, 10.5_

  - [ ] 1.6 Implement `destroyHomepageAnimations()` in `site.js`
    - Export on `window`; disconnect and null `window._scrollRevealObserver` and `window._stickyBarObserver` if they exist
    - Remove `sticky-bar--visible` class from `.sticky-booking-bar` and reset `.wellness-page` `paddingBottom` to `''`
    - _Requirements: 10.3_

- [ ] 2. Update `Home.razor` — Blazor wiring and new markup
  - [ ] 2.1 Replace `@implements IDisposable` with `@implements IAsyncDisposable` and add `@inject IJSRuntime JS` directive; convert `Dispose()` to `DisposeAsync()` calling `destroyHomepageAnimations` in a `try/catch`; also dispose `_carouselTimer` and `_heroTimer` inside `DisposeAsync`; change `OnAfterRender` to `OnAfterRenderAsync` and add the 4 `InvokeVoidAsync` calls gated by `if (firstRender)`: `initTypewriter(".wellness-page .hero-content h1", 40)`, `initScrollReveal()`, `initGuaranteeTicker()`, `initStickyBar(".wellness-hero", ".sticky-booking-bar")`
    - Note: `initStatCounters` is NOT called here — it is triggered internally by `initScrollReveal` when `.social-proof-section` enters the viewport
    - _Requirements: 10.2, 10.3, 10.6_

  - [ ] 2.2 Add `data-reveal` attributes to all required elements in `Home.razor`
    - Add `data-reveal` to: `.split-intro` (Approach section), each `.service-card-home` wrapper `<a>`, `.care-panel`, each `.care-reasons > div`, `.programs`, `.carousel-section-wrap` wrapper, and the contact `<section>`
    - Do NOT add `data-reveal` to `.sticky-booking-bar` or the `.wellness-hero` hero section
    - _Requirements: 2.1, 2.7_

  - [ ] 2.3 Replace the Guarantee Strip with the ticker markup in `Home.razor`
    - Change `<section class="guarantee-strip">` to `<section class="guarantee-strip guarantee-strip--ticker" aria-label="Our care promise">`
    - Wrap all `<span>` items in `<div class="guarantee-ticker-track">`, rendering the `guarantees` list twice — second pass with `aria-hidden="true"` on each span for the seamless loop duplicate
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 11.5_

  - [ ] 2.4 Add the Social Proof section to `Home.razor`
    - Insert between the `.care-panel` section and the `.programs` section
    - Add the `StatCounter` and `Testimonial` records to `@code` plus `socialProofStats` (4 items: "4–8 kg"/8/"Weight Loss / Month", "2–5 in"/5/"Lost Per Session", "500+"/500/"Patients Served", "10+"/10/"Years of Experience") and `testimonials` (2 items, no patient names or photos)
    - Markup: `<section class="wellness-section social-proof-section" data-reveal aria-labelledby="sp-heading">` with `.sp-stats-grid` (4 × `.stat-card-sp` each carrying `data-count-target` and `data-count-display` attrs and `data-reveal`) and `.sp-testimonials` (2 × `<blockquote class="testimonial-card" data-reveal>`)
    - Testimonial quotes must reflect clinical outcomes only (physiotherapy / weight loss) — no identifiable patient details
    - _Requirements: 3.1, 3.2, 3.4, 3.5, 3.7, 3.8_

  - [ ] 2.5 Add the Floating Sticky Bar markup to `Home.razor`
    - Place `<div class="sticky-booking-bar" role="complementary" aria-label="Quick booking bar">` just before the closing `</div>` of `.wellness-page` (outside `<main>`)
    - Include two `<a>` tags: `.sticky-bar-btn.sticky-bar-btn--book` (href="/book", descriptive aria-label) and `.sticky-bar-btn.sticky-bar-btn--wa` (href="https://wa.me/919028027888", target="_blank", rel="noopener", descriptive aria-label)
    - _Requirements: 4.1, 4.4, 4.5, 4.8_

  - [ ] 2.6 Upgrade the Contact section markup in `Home.razor`
    - Add `<p class="contact-urgency">Take the first step today — consultations available now.</p>` above the CTA group
    - Add a WhatsApp CTA `<a class="wellness-secondary contact-wa-cta" href="https://wa.me/919028027888" target="_blank" rel="noopener">💬 WhatsApp Us</a>` alongside the existing Book Online CTA inside a `.contact-cta-group` flex div
    - _Requirements: 8.1, 8.2, 8.4, 8.5_

- [ ] 3. Add all additive CSS blocks to `app.css`
  - [ ] 3.1 Add scroll-reveal base and revealed-state rules to `app.css`
    - Append under `.wellness-page` scope: `[data-reveal]` rule (`opacity: 0; transform: translateY(24px); transition: opacity 500ms ease-out, transform 500ms ease-out`) and `.revealed` rule (`opacity: 1; transform: translateY(0)`)
    - Add `@media (prefers-reduced-motion: reduce)` override: `[data-reveal] { opacity: 1; transform: none; transition: none; }`
    - _Requirements: 2.2, 2.6, 2.7_

  - [ ] 3.2 Add Social Proof section CSS to `app.css`
    - Append under `.wellness-page` scope: `.social-proof-section` (full-bleed strip, `background: #f5f9f6`, border-top/bottom `#d8ead0`, `padding-inline: max(30px, calc((100vw - 1180px) / 2))`), `.sp-stats-grid` (flex, centered, `gap: 32px`, `flex-wrap: wrap`), `.stat-card-sp` (column flex, `min-width: 140px`, text-centered), `.sp-count` (Playfair Display, `clamp(2.4rem,5vw,3.6rem)`, `#008d36`), `.sp-label` (0.82rem, uppercase, `letter-spacing: 0.1em`, `#5a5260`)
    - Add `.sp-testimonials` grid (2 columns, `gap: 24px`) and `.testimonial-card` (white bg, `border-left: 4px solid #008d36`, border-radius `0 12px 12px 0`, italic quote, `.testimonial-card footer` in `#008d36` uppercase)
    - Add `@media (max-width: 520px)` override: `.sp-stats-grid` becomes `grid-template-columns: 1fr 1fr`; `.sp-testimonials` becomes single column
    - _Requirements: 3.7, 3.8, 9.1, 9.4, 9.5, 9.6_

  - [ ] 3.3 Add Floating Sticky Bar CSS to `app.css`
    - Append under `.wellness-page` scope: `.sticky-booking-bar` base state (`position: fixed; bottom: 0; left: 0; width: 100%; z-index: 200; background: #1a4d2e; opacity: 0; transform: translateY(100%); transition: opacity 300ms ease, transform 300ms ease; pointer-events: none; display: flex; align-items: center; justify-content: center; gap: 16px; padding: 14px 24px`)
    - Add `.sticky-bar--visible` modifier (`opacity: 1; transform: translateY(0); pointer-events: auto`)
    - Add `.sticky-bar-btn` base (inline-flex, `padding: 12px 24px`, `min-width: 160px`, centered), `.sticky-bar-btn--book` (`background: #008d36`), `.sticky-bar-btn--wa` (`background: #25d366`)
    - Add `@media (prefers-reduced-motion: reduce)` override: `transition: none` on `.sticky-booking-bar`
    - Add `@media (max-width: 380px)` override: flex-direction column, CTAs full-width
    - _Requirements: 4.1, 4.2, 4.3, 4.7, 4.9, 11.2_

  - [ ] 3.4 Add Guarantee Strip ticker CSS to `app.css`
    - Append under `.wellness-page` scope: `.guarantee-strip--ticker` (`overflow: hidden; padding: 0; display: block`), `.guarantee-ticker-track` (`display: flex; width: max-content; animation: ticker-scroll 18s linear infinite`), `.guarantee-item` (`padding: 18px 38px; font-weight: 800; font-size: 0.88rem; white-space: nowrap; color: #fff`)
    - Add `@keyframes ticker-scroll` (`from: translateX(0)` → `to: translateX(-50%)`)
    - Add `@media (prefers-reduced-motion: reduce)` override: `animation: none; width: auto; flex-wrap: wrap; justify-content: center` on `.guarantee-ticker-track`; `display: none` on `.guarantee-item[aria-hidden="true"]`
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

  - [ ] 3.5 Add service card hover micro-interaction CSS to `app.css`
    - Append under `.wellness-page` scope: `.service-card-home` transition rule (`transition: transform 220ms ease-out, box-shadow 220ms ease-out`)
    - Add `.service-card-link:hover .service-card-home, .service-card-link:focus-visible .service-card-home` (`transform: translateY(-6px); box-shadow: 0 16px 48px rgba(0,0,0,0.12)`)
    - Add `.svc-icon-wrap` transition and `.service-card-link:hover .svc-icon-wrap, .service-card-link:focus-visible .svc-icon-wrap` (`transform: scale(1.12) rotate(6deg)`)
    - Add `@media (prefers-reduced-motion: reduce)` override: no transform on hover, box-shadow only; no icon rotation
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

  - [ ] 3.6 Add gallery carousel visual polish CSS to `app.css`
    - Append under `.wellness-page` scope: `.carousel-slide` transition upgrade (`transition: opacity 600ms ease`), `.carousel-section` green shadow (`box-shadow: 0 16px 56px rgba(0,141,54,0.15)`), `.carousel-dot.active` scale-up (`transform: scale(1.4); background: #008d36; border-color: #008d36`), `.carousel-dot` minimum tap target (`min-width: 24px; min-height: 24px; padding: 7px; box-sizing: content-box`)
    - Add `@media (prefers-reduced-motion: reduce)` override: `transition: none` on `.carousel-slide`
    - _Requirements: 7.3, 7.4, 7.5, 7.6, 11.7_

  - [ ] 3.7 Add contact section upgrade CSS and typography rules to `app.css`
    - Append under `.wellness-page` scope: `.contact-urgency` (`font-size: 1.05rem; font-weight: 700; color: #ffeb00; margin-top: 12px; line-height: 1.5`), `.contact-wa-cta` (`color: #fff !important; border-color: rgba(255,255,255,0.5)`), `.contact-wa-cta:hover` (`background: rgba(255,255,255,0.08)`)
    - Append typography catch-all: `.wellness-page h1, .wellness-page h2, .wellness-page h3 { font-family: 'Playfair Display', Georgia, serif }` and `.wellness-page .section-kicker` override ensuring `font-family: 'Outfit', sans-serif` (prevents Playfair bleed onto kicker labels)
    - Verify (no edit needed) that `.contact-section { background: #1a4d2e }` already exists in `app.css` — this rule is already present and must NOT be duplicated
    - _Requirements: 8.1, 8.2, 8.3, 9.1, 9.2, 9.4, 9.6_

- [ ] 4. Checkpoint — Verify all implementation is wired together correctly
  - Ensure all tests pass, ask the user if questions arise.
  - Manually verify: (a) `destroyHomepageAnimations` disconnects both observers, (b) `data-reveal` is absent from `.sticky-booking-bar` and `.wellness-hero`, (c) `initStatCounters` is not called directly in `OnAfterRenderAsync`, (d) no CSS rule targets bare element selectors or `.dashboard-theme`

- [ ] 5. Write integration tests for all correctness properties
  - [ ]* 5.1 Write a Playwright or bUnit test that confirms `initTypewriter`, `initScrollReveal`, `initGuaranteeTicker`, and `initStickyBar` are each called exactly once per page lifecycle regardless of subsequent `StateHasChanged` calls
    - **Property 1: JS Init Functions Called Exactly Once Per Page Lifecycle**
    - **Validates: Requirements 2.4, 4.6, 10.2**

  - [ ]* 5.2 Write a JS unit test (Jest or inline QUnit) that calls `initStatCounters` against a DOM fixture and asserts that after the animation duration each `[data-count-target]` element's text content equals its `data-count-display` exactly
    - **Property 2: Stat Counter Animation Reaches Exact Target Value**
    - **Validates: Requirements 1.3, 3.2, 3.3**

  - [ ]* 5.3 Write a JS unit test that simulates `IntersectionObserver` callback events for `.wellness-hero` and asserts that `.sticky-booking-bar` carries `sticky-bar--visible` when `isIntersecting == false` and does not carry it when `isIntersecting == true`
    - **Property 3: Sticky Bar Visibility Is the Inverse of Hero Viewport Presence**
    - **Validates: Requirements 4.2, 4.3**

  - [ ]* 5.4 Write a JS unit test that checks that after the sticky bar becomes visible, `document.querySelector('.wellness-page').style.paddingBottom` equals `bar.offsetHeight + 'px'`
    - **Property 4: Sticky Bar Bottom Padding Equals Bar Height**
    - **Validates: Requirement 4.9**

  - [ ]* 5.5 Write unit tests for each `site.js` function asserting that the `prefers-reduced-motion` check runs before any observer is created, setTimeout is called, or CSS class is toggled
    - **Property 5: Reduced-Motion Check Precedes Animation Setup in All `site.js` Functions**
    - **Validates: Requirements 1.4, 2.5, 3.6, 4.7, 5.4, 10.4**

  - [ ]* 5.6 Write a CSS audit test (or manual review checklist) asserting no selector added by this feature starts with anything other than `.wellness-page`, `.auth-page`, or `@keyframes`
    - **Property 6: CSS Selector Scope Isolation — No Bleed to `.dashboard-theme`**
    - **Validates: Requirement 9.6**

  - [ ]* 5.7 Write a JS unit test that calls `initScrollReveal()` N times (N ≥ 3) and asserts that exactly one `IntersectionObserver` instance exists (via spy/mock on the `IntersectionObserver` constructor)
    - **Property 7: `initScrollReveal` Idempotency — One Observer Regardless of Call Count**
    - **Validates: Requirements 2.3, 10.5**

---

## Notes

- Tasks marked with `*` are optional testing sub-tasks and can be skipped for a faster MVP
- All CSS changes are **additive only** — no existing rules in `app.css` are modified or removed
- The `.dashboard-theme` and admin panel styles must never be touched
- `initStatCounters` is not called from `OnAfterRenderAsync`; it is triggered by the scroll reveal observer when `.social-proof-section` enters the viewport
- The `IDisposable` directive in `Home.razor` is replaced by `IAsyncDisposable` — the synchronous `Dispose()` method is removed entirely
- Clinic content (testimonials, stat labels) must use clinical/physiotherapy language — no aesthetic-only or unrelated framing
- Observer instances must be stored on `window._scrollRevealObserver` and `window._stickyBarObserver` for cleanup

---

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.4", "1.6"] },
    { "id": 1, "tasks": ["1.3", "1.5"] },
    { "id": 2, "tasks": ["2.1", "3.1", "3.2", "3.3", "3.4", "3.5", "3.6", "3.7"] },
    { "id": 3, "tasks": ["2.2", "2.3", "2.4", "2.5", "2.6"] },
    { "id": 4, "tasks": ["5.1", "5.2", "5.3", "5.4", "5.5", "5.6", "5.7"] }
  ]
}
```
