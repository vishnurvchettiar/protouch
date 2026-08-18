# Implementation Plan: Aesthetic Clinic Redesign

## Overview

This plan converts the ProTouch Physio - Slimming Clinic homepage into a luxury wellness brand experience, implementing 11 new page sections across three files: `Home.razor`, `app.css`, and `site.js`. The approach builds incrementally — design tokens and layout first, then section by section, then interactive JS enhancements, finishing with responsive polish. All existing admin, booking, and service-detail code is left completely untouched.

---

## Tasks

- [ ] 1. Establish new CSS design tokens and `.ac-page` scope in `app.css`
  - Add the `.ac-page { --ac-* }` custom property block with all new design tokens (colours, typography, shadows, transitions) as specified in the design document's "Design Tokens" section
  - Ensure all new rules are scoped under `.ac-page` and do NOT modify any existing `.wellness-page`, `.glass-panel`, `.btn-clinic`, or admin/dashboard selectors
  - Add base resets and font family declarations for `.ac-page *`
  - _Requirements: 1.1, 1.2, 1.3, 1.4_

- [ ] 2. Build the `ac-header` sticky navigation and core page shell in `Home.razor`
  - Replace the current `Home.razor` content with the new `<div class="ac-page">` wrapper
  - Implement `<header class="ac-header" id="ac-header">` with logo (`<a href="#top" class="ac-logo">`), nav links (Services, About, Gallery, Contact), Login button, and Call Now button
  - Wire up `@implements IAsyncDisposable` and declare all new `@code` fields: `_heroTimer`, `_testiTimer`, `activeHeroIndex`, `activeTestiIndex`, `activeTab`, `openFaq`, `heroImages`, `testimonials`, `faqItems`, `beforeAfterPairs`, `serviceCards`, `socialProofStats`, `guarantees`
  - Populate all static data lists in `OnInitialized()`
  - _Requirements: 2.1, 2.5, 14.1_

  - [ ] 2.1 Add `ac-header` CSS to `app.css`
    - Add `.ac-header`, `.ac-logo`, `.ac-nav`, `.ac-header-actions`, `.ac-login-btn`, `.ac-call-btn`, `.ac-header--scrolled` styles
    - Header starts transparent; `.ac-header--scrolled` applies white background with shadow
    - _Requirements: 2.1, 2.2, 2.3_

  - [ ] 2.2 Add `initStickyHeader` to `site.js`
    - Implement `window.initStickyHeader = function(headerSelector)` using a scroll event listener
    - Toggle `.ac-header--scrolled` when `window.scrollY > 80`
    - Store listener as `window._stickyHeaderHandler`
    - _Requirements: 2.3, 2.4, 2.6_

- [ ] 3. Implement the dark/light split hero section
  - Add the `<section class="ac-hero" id="top">` markup to `Home.razor` with left dark panel and right light panel
  - Left panel: `ac-kicker`, `ac-hero-h1`, `ac-hero-sub`, `ac-hero-actions` (Book + WhatsApp CTAs), `ac-trust-row` with ISO badge, star rating (4.9/5.0), patient count (500+)
  - Right panel: `@for` loop over `heroImages` rendering `<img>` tags with `.active` class on `activeHeroIndex`; `ac-hero-badge` card for Dr. Manjari
  - All hero images: `loading="eager"` on index 0, `loading="lazy"` on rest; `onerror="this.style.display='none'"`
  - _Requirements: 3.1, 3.2, 3.5, 3.6_

  - [ ] 3.1 Add `ac-hero` CSS to `app.css`
    - Two-column grid; left panel `background: var(--ac-dark-hero)`, right panel `background: var(--ac-cream)`
    - Hero slide fade transition; `.active` class controls opacity
    - `.ac-hero-badge` absolutely positioned card
    - `.ac-btn-primary` (green), `.ac-btn-ghost` (outlined purple) button styles
    - `.ac-trust-row` badge strip styles
    - _Requirements: 3.1, 3.6_

  - [ ] 3.2 Start hero timer and JS initialisations in `OnAfterRenderAsync`
    - On `firstRender`: start `_heroTimer` (5-second interval, calls `InvokeAsync(() => { activeHeroIndex = (activeHeroIndex + 1) % heroImages.Count; StateHasChanged(); })`)
    - Call `JS.InvokeVoidAsync("initStickyHeader", "#ac-header")` wrapped in try/catch
    - Call `JS.InvokeVoidAsync("initScrollReveal")` wrapped in try/catch
    - Call `JS.InvokeVoidAsync("initGuaranteeTicker")` wrapped in try/catch
    - _Requirements: 3.3, 3.4, 3.7, 14.2_

  - [ ]* 3.3 Write property test for hero carousel index bounds
    - **Property 1: Hero carousel index is always in bounds**
    - **Validates: Requirements 3.3, 3.4**
    - For any N in [1..100] and any T in [0..1000], verify `(startIndex + T) % N` is always in `[0, N)`

- [ ] 4. Add guarantee ticker strip

  - [ ] 4.1 Add ticker markup to `Home.razor`
    - Add `<section class="ac-ticker">` immediately after the hero section
    - Use same `guarantee-ticker-track` pattern as existing code with the 5 guarantee items, duplicated for seamless loop
    - _Requirements: 4.1, 4.2_

  - [ ] 4.2 Add `ac-ticker` CSS to `app.css`
    - Purple background `var(--ac-purple)`, white text, CSS `@keyframes ac-ticker-scroll` animation
    - `.ac-ticker` responsive overflow: hidden
    - _Requirements: 4.1, 4.3_

- [ ] 5. Implement tabbed services showcase

  - [ ] 5.1 Add services tab markup to `Home.razor`
    - Add `<section class="ac-services" id="services">` with `SetTab(string id)` method in `@code`
    - Render four `<button class="ac-tab">` elements from `serviceCards`; apply `ac-tab--active` to the tab matching `activeTab`
    - Render a content panel showing the active service's icon, name, summary, bullet list, and a "View Service & Book →" link to `/services/@activeTab`
    - `activeTab` initialised to `"weight-loss"` in `OnInitialized`
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

  - [ ] 5.2 Add `ac-services` CSS to `app.css`
    - `.ac-services-tabs` flex row of tab buttons; `.ac-tab--active` uses service accent colour border-bottom
    - `.ac-services-panel` two-column grid: text panel left, feature image right
    - Panel content fade transition on tab change
    - _Requirements: 5.1, 5.4_

  - [ ]* 5.3 Write property test for active tab validity
    - **Property 5: Active tab is always a valid service ID**
    - **Validates: Requirements 5.2, 5.3, 5.6**
    - For any sequence of SetTab calls with valid IDs, verify activeTab always equals the last-set ID

- [ ] 6. Build the Doctor Section ("Meet the Expert")

  - [ ] 6.1 Add doctor section markup to `Home.razor`
    - Add `<section class="ac-doctor" id="doctor">` with `data-reveal` attribute
    - Left column: `<img src="/images/clinic-gallery/gallery-27.jpeg">` with `onerror` handler; ISO 9001:2015 credential badge overlay
    - Right column: `ac-kicker` ("MEET THE EXPERT"), `<h2>Dr. Manjari</h2>`, title, bio paragraph, credentials `<ul>`, and "Book a Consultation →" CTA link to `/book`
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

  - [ ] 6.2 Add `ac-doctor` CSS to `app.css`
    - Two-column split: photo left, copy right; cream background
    - `.ac-doctor-cred-badge` positioned overlay with green background and white text
    - `.ac-doctor-credentials` styled list with tick icons
    - _Requirements: 6.1, 6.2_

- [ ] 7. Implement the animated stats strip

  - [ ] 7.1 Add stats strip markup to `Home.razor`
    - Add `<section class="ac-stats social-proof-section" data-reveal>` (reusing the `social-proof-section` class so existing `initStatCounters` is triggered by `initScrollReveal`)
    - Render counters using existing `socialProofStats` list with `data-count-target` and `data-count-display` attributes
    - _Requirements: 7.1, 7.2, 7.3_

  - [ ] 7.2 Add `ac-stats` CSS to `app.css`
    - Dark green background `var(--ac-green-dark)`, white text
    - Four-column grid of stat cards with large number and label
    - _Requirements: 7.1_

- [ ] 8. Implement Before/After comparison slider

  - [ ] 8.1 Add `BeforeAfterPair` record and data in `@code`
    - Add `record BeforeAfterPair(string Before, string After, string Label)` to `Home.razor @code`
    - Populate `List<BeforeAfterPair> beforeAfterPairs` in `OnInitialized()` using curated gallery image pairs from `/images/clinic-gallery/`
    - _Requirements: 8.1_

  - [ ] 8.2 Add before/after section markup to `Home.razor`
    - Add `<section class="ac-before-after" id="gallery" data-reveal>`
    - `ac-kicker` ("TREATMENT RESULTS"), `<h2>`, `<div class="ac-ba-grid">`
    - `@foreach (var pair in beforeAfterPairs)`: render `<div class="ac-ba-item" data-ba>` with `.ac-ba-before` image, `.ac-ba-after` image (initial width 50%), `.ac-ba-handle`, `.ac-ba-label` spans; `onerror` on all images
    - Call `JS.InvokeVoidAsync("initBeforeAfterSliders", "[data-ba]")` in `OnAfterRenderAsync(firstRender=true)` wrapped in try/catch
    - _Requirements: 8.1, 8.2, 8.3, 8.5, 8.7_

  - [ ] 8.3 Add `ac-before-after` CSS to `app.css`
    - `.ac-ba-item`: `position: relative; overflow: hidden; user-select: none`
    - `.ac-ba-before`: full-width base layer
    - `.ac-ba-after`: `position: absolute; top: 0; left: 0; overflow: hidden; width: 50%`; `will-change: width`
    - `.ac-ba-after img`: `width: 100vw` (wider than container to avoid cropping)
    - `.ac-ba-handle`: absolute vertical line + circular drag knob
    - `.ac-ba-label` badges positioned at image corners
    - _Requirements: 8.2, 8.3, 8.4_

  - [ ] 8.4 Add `initBeforeAfterSliders` and `destroyBeforeAfterSliders` to `site.js`
    - Implement pointer-events-based drag as per algorithm in design document
    - Clamp `pct` to [0.05, 0.95] before applying to `.ac-ba-after.style.width`
    - Store cleanup in `window._baCleanup`
    - Implement `window.destroyBeforeAfterSliders = function()` that calls `window._baCleanup()`
    - _Requirements: 8.3, 8.4, 8.5, 8.6_

  - [ ]* 8.5 Write property test for Before/After slider clamping
    - **Property 4: Before/After slider width is always clamped**
    - **Validates: Requirements 8.3, 8.4**
    - For any clientX value (negative, zero, within bounds, exceeding bounds) and any container width > 0, verify computed pct is always in [0.05, 0.95]

- [ ] 9. Implement testimonial carousel

  - [ ] 9.1 Add testimonial carousel markup and logic to `Home.razor`
    - Add `<section class="ac-testimonials" data-reveal>`
    - `@for` loop rendering slides with `.ac-testi--active` on `activeTestiIndex`; stars, quote, footer
    - Arrow buttons calling `PrevTesti()` and `NextTesti()` (`@onclick`)
    - Dot buttons calling `JumpToTesti(i)` for each testimonial
    - In `@code`: implement `NextTesti()`, `PrevTesti()`, `JumpToTesti(int index)` — all use modulo wrap and call `_testiTimer?.Change(...)`
    - Start `_testiTimer` in `OnAfterRenderAsync(firstRender=true)` with 6-second interval
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6_

  - [ ] 9.2 Add `ac-testimonials` CSS to `app.css`
    - Cream/light background, `.ac-testi-slide` with opacity transition
    - `.ac-testi--active` visible; others `opacity: 0; pointer-events: none`
    - `.ac-stars` yellow star colour, `.ac-testi-quote` serif italic font, `.ac-testi-footer` muted
    - Arrow buttons and dot indicators styles
    - _Requirements: 9.1, 9.7_

  - [ ]* 9.3 Write property test for testimonial carousel index bounds
    - **Property 2: Testimonial carousel index is always in bounds**
    - **Validates: Requirements 9.3, 9.4, 9.6**
    - For any N in [1..50] and any random sequence of NextTesti/PrevTesti/JumpToTesti calls, verify activeTestiIndex is always in [0, N)

- [ ] 10. Implement FAQ accordion

  - [ ] 10.1 Add FAQ accordion markup and logic to `Home.razor`
    - Add `<section class="ac-faq" data-reveal>`
    - Populate `List<FaqItem> faqItems` in `OnInitialized()` with at minimum 6 items covering: treatment safety, cost, how long results take, eligibility, number of sessions, weight loss guarantees
    - `@foreach` loop rendering `.ac-faq-item` with `.ac-faq--open` on item matching `openFaq`
    - `<button class="ac-faq-q">` calls `ToggleFaq(i)` — implements: `openFaq = (openFaq == i) ? -1 : i`
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_

  - [ ] 10.2 Add `ac-faq` CSS to `app.css`
    - `.ac-faq-a` wraps answer in a div with `max-height: 0; overflow: hidden; transition: max-height 0.35s ease`
    - `.ac-faq--open .ac-faq-a` sets `max-height: 400px`
    - `.ac-faq-icon` rotates `+` to `−` via CSS transform on `.ac-faq--open`
    - Clean border separator between items
    - _Requirements: 10.2, 10.3_

  - [ ]* 10.3 Write property test for FAQ accordion invariant
    - **Property 3: FAQ accordion — at most one item open**
    - **Validates: Requirements 10.2, 10.3, 10.4, 10.5**
    - For any FAQ list length N ≥ 1 and any sequence of ToggleFaq(i) calls where 0 ≤ i < N, verify openFaq is always in {-1} ∪ [0, N-1]; verify clicking the same index twice returns to -1

- [ ] 11. Add contact section and footer

  - [ ] 11.1 Add contact section markup to `Home.razor`
    - Add `<section id="contact" class="ac-contact" data-reveal>` with two-column layout
    - Left: `ac-kicker`, `<h2>`, intro paragraph, "Book an Appointment →" button to `/book`, WhatsApp button to `https://wa.me/919028027888` with `target="_blank" rel="noopener noreferrer"`
    - Right: phone numbers, email, address, Google Maps directions link
    - Add `<footer class="ac-footer">` with copyright line
    - _Requirements: 11.1, 11.2, 11.3, 11.4_

  - [ ] 11.2 Add `ac-contact` and `ac-footer` CSS to `app.css`
    - Purple background `var(--ac-purple)` for contact section, yellow kicker, white body text
    - Right column bordered contact details layout
    - Simple dark footer
    - _Requirements: 11.1_

- [ ] 12. Implement floating CTA buttons

  - [ ] 12.1 Add floating CTA markup to `Home.razor`
    - Add `<div class="ac-floating-cta">` with two `<a>` buttons: WhatsApp (💬) and Book (📅)
    - WhatsApp: `href="https://wa.me/919028027888"`, `target="_blank"`, `rel="noopener noreferrer"`
    - Book: `href="/book"`
    - Both have `aria-label` attributes
    - _Requirements: 12.1, 12.3, 12.4, 12.5_

  - [ ] 12.2 Add `ac-floating-cta` CSS to `app.css`
    - `position: fixed; bottom: 2rem; right: 2rem; z-index: 999; display: flex; flex-direction: column; gap: 0.75rem`
    - Circular buttons with brand colours (green for Book, WhatsApp green for WA)
    - Hover scale transform; box shadow glow effect
    - _Requirements: 12.2, 12.5_

- [ ] 13. Implement `DisposeAsync` resource cleanup

  - [ ] 13.1 Implement `DisposeAsync` in `Home.razor @code`
    - Dispose `_heroTimer` and `_testiTimer` with null checks
    - Call `JS.InvokeVoidAsync("destroyAestheticAnimations")` wrapped in try/catch (suppress exceptions)
    - _Requirements: 14.1, 14.2, 14.3, 14.5_

  - [ ] 13.2 Add `destroyAestheticAnimations` to `site.js`
    - Implement `window.destroyAestheticAnimations = function()` that:
      - Removes `window._stickyHeaderHandler` scroll listener
      - Calls `window._baCleanup()` if set, then nulls it
      - Disconnects `window._scrollRevealObserver` if set
      - Resets sticky bar via existing `destroyHomepageAnimations` logic (call `_origDestroy` if accessible)
    - _Requirements: 14.3, 14.4_

  - [ ]* 13.3 Write property test for DisposeAsync exception suppression
    - **Property 6: DisposeAsync suppresses all JS exceptions**
    - **Validates: Requirements 14.5**
    - Mock `IJSRuntime` to throw on any invocation; verify `DisposeAsync` completes without throwing

- [ ] 14. Add responsive CSS breakpoints

  - [ ] 14.1 Add `@media (max-width: 850px)` rules to `app.css`
    - Hero: `grid-template-columns: 1fr` (stack vertically)
    - Services tabs: `flex-wrap: wrap` / horizontal scroll
    - Doctor section: `grid-template-columns: 1fr`
    - Contact section: `grid-template-columns: 1fr`
    - Stats strip: 2-column grid
    - Header: hide `ac-nav` links, keep logo + Login + Call
    - _Requirements: 13.1, 13.2, 13.4_

  - [ ] 14.2 Add `@media (max-width: 600px)` rules to `app.css`
    - Before/After grid: `grid-template-columns: 1fr` (stack pairs vertically)
    - Hero: reduce heading font size
    - Floating CTA: reduce button size to 48px
    - _Requirements: 13.3, 13.5_

- [ ] 15. Final checkpoint — verify all sections render correctly
  - Ensure all tests pass, ask the user if questions arise.
  - Verify `app.css` has no syntax errors and that all new rules are under `.ac-page` scope
  - Verify `Home.razor` references all required JS functions with try/catch wrappers
  - Verify `DisposeAsync` disposes both timers and calls `destroyAestheticAnimations`
  - Confirm no admin/dashboard CSS selectors were modified

---

## Notes

- Tasks marked with `*` are optional and can be skipped for a faster MVP
- All existing JS functions in `site.js` must remain untouched; only append new functions
- The `social-proof-section` class is reused on the stats strip so that `initScrollReveal` automatically triggers `initStatCounters` — no new JS needed for counters
- The `data-reveal` attribute is reused on all new sections to activate the existing scroll-reveal observer
- Gallery images `gallery-01` through `gallery-60` are the source for all hero, before/after, and doctor section images
- The `initGuaranteeTicker` function already handles `prefers-reduced-motion` — reuse as-is

---

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1"] },
    { "id": 1, "tasks": ["2.1", "2.2"] },
    { "id": 2, "tasks": ["2", "3.1"] },
    { "id": 3, "tasks": ["3.2", "4.2"] },
    { "id": 4, "tasks": ["3.3", "4.1", "5.2", "6.2", "7.2", "8.3", "9.2", "10.2", "11.2", "12.2"] },
    { "id": 5, "tasks": ["5.1", "6.1", "7.1", "8.1", "8.4", "9.1", "10.1", "11.1", "12.1"] },
    { "id": 6, "tasks": ["5.3", "8.2", "8.5", "9.3", "10.3"] },
    { "id": 7, "tasks": ["13.1", "13.2"] },
    { "id": 8, "tasks": ["13.3", "14.1"] },
    { "id": 9, "tasks": ["14.2"] }
  ]
}
```
