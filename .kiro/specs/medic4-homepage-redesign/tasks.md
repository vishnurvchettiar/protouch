# Implementation Plan: medic4-homepage-redesign

## Overview

This plan converts the current dark "aesthetic clinic" `Home.razor` into a light, professional BeMedic4-inspired layout using a new `pt-*` CSS class system. The implementation is split into four logical waves:

1. **Foundation** — CSS design tokens, font loading, and the new stylesheet wired into the app shell
2. **Code-block extension** — new C# records and state fields added to `Home.razor @code`
3. **Markup replacement** — all eight new sections + five preserved widget sections rewritten in `pt-*` HTML
4. **Testing and verification** — property tests for pure C# logic; build verification

All existing `@code` C# logic, JS interop calls, and `site.js` functions are preserved verbatim. Zero changes to `site.js`. All backward-compatible class hooks (`ac-testimonials`, `social-proof-section`, `ac-page`, `.ac-header--scrolled` CSS target) are retained as secondary classes on the new `pt-*` elements so existing JS continues to work without modification.

---

## Tasks

- [ ] 1. Create `home-medic4.css` — Design tokens and global resets
  - [ ] 1.1 Create `ClinicApp/wwwroot/home-medic4.css` with the `.pt-page` CSS custom property block
    - Define all eight colour tokens on `.pt-page`: `--pt-primary: #008d36`, `--pt-mint: #B8E5D2`, `--pt-surface: #EDF4F2`, `--pt-cta-bg: #DAFFE7`, `--pt-dark: #0d2b1e`, `--pt-text: #737373`, `--pt-heading: #1a1a1a`, `--pt-white: #ffffff`
    - Define all typography tokens: `--pt-font-body`, `--pt-font-heading`, `--pt-text-sm` through `--pt-text-5xl`
    - Define spacing, shadow, and transition tokens: `--pt-section-v`, `--pt-section-v-mob`, `--pt-shadow-card`, `--pt-shadow-hover`, `--pt-trans-fast`, `--pt-trans-med`
    - Add global resets scoped to `.pt-page`: background, color, font-family, link colour overrides (overrides dark `app.css` globals)
    - Add heading font rules: `.pt-page h1, h2, h3` → `font-family: var(--pt-font-heading)`
    - Add utility classes: `.pt-kicker`, `.pt-btn-primary`, `.pt-btn-ghost`, `.pt-btn-inverse`, `.pt-link`
    - _Requirements: 1.1, 1.2, 1.3, 2.3, 2.5_

  - [ ]* 1.2 Write property test for CSS design token completeness
    - **Property 1: All CSS design tokens are defined on `.pt-page`**
    - Validates: Requirements 1.1, 1.2, 1.3
    - Test in `ClinicApp.Tests/Medic4HomepageTests.cs`; parse `wwwroot/home-medic4.css` text and assert all eight `--pt-*` colour property names are present within a `.pt-page { }` rule block
    - Tag: `// Feature: medic4-homepage-redesign, Property 1`

  - [ ]* 1.3 Write property test for no hard-coded hex colours in CSS utility classes
    - **Property 2: No hard-coded hex colours in markup** (CSS utility class variant)
    - Validates: Requirements 1.3
    - Assert that the content of `home-medic4.css` does not contain literal hex values outside of the `.pt-page { }` token definition block (i.e., all other rules reference `var(--pt-*)`)
    - Tag: `// Feature: medic4-homepage-redesign, Property 2`

- [ ] 2. Add stylesheet and font links to `App.razor`
  - [ ] 2.1 Add Google Fonts non-blocking `<link>` tags to `App.razor` `<head>` before `<HeadOutlet />`
    - Add `<link rel="preconnect" href="https://fonts.googleapis.com" />` and `<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />`
    - Add font stylesheet link using `media="print" onload="this.media='all'"` pattern for Manrope (400, 500, 600) and Instrument Sans (400, 600, 700) with `display=swap`
    - Add `<noscript>` fallback font link
    - _Requirements: 2.1, 2.2, 2.5, 18.1, 18.2_

  - [ ] 2.2 Add `home-medic4.css` stylesheet link to `App.razor` immediately after the font `<noscript>` tag
    - Add `<link rel="stylesheet" href="@Assets["home-medic4.css"]" />`
    - _Requirements: 18.7_

- [ ] 3. Extend `Home.razor @code` block with new data records and fields
  - [ ] 3.1 Add `BodyPartTab` record, `HowItWorksStep` record, `SupportingPractitioner` record to the `@code` block
    - Add `private record BodyPartTab(string Id, string Label, string Description);`
    - Add `private record HowItWorksStep(string Number, string Title, string Description);`
    - Add `private record SupportingPractitioner(string Title);`
    - Add state fields: `private string activeBodyPart = "neck-shoulder";`, `private List<BodyPartTab> bodyPartTabs = new();`, `private List<HowItWorksStep> howItWorksSteps = new();`, `private List<SupportingPractitioner> supportingPractitioners = new();`, `private List<string> doctorCredentials = new();`
    - Add method: `private void SetBodyPart(string id) => activeBodyPart = id;`
    - _Requirements: 6.6, 8.3, 10.2_

  - [ ] 3.2 Populate new lists in `OnInitialized` within `Home.razor @code`
    - Populate `bodyPartTabs` with all six tab definitions from Requirements 6.6
    - Populate `howItWorksSteps` with the three steps from Requirements 8.3
    - Populate `supportingPractitioners` with three placeholder entries: "Senior Physiotherapist", "Yoga Therapist", "Diet & Nutrition Consultant"
    - Populate `doctorCredentials` with the six credential bullet strings already in the doctor section
    - _Requirements: 6.6, 8.3, 10.2_

- [ ] 4. Checkpoint — Build verification after code-block extension
  - Run `dotnet build ProTouch.sln` and confirm zero errors and zero warnings on the `ClinicApp` project before touching markup
  - Ensure all tests pass, ask the user if questions arise

- [ ] 5. Write `home-medic4.css` — Header, Hero, and Booking Banner styles
  - [ ] 5.1 Add `.pt-header` styles to `home-medic4.css`
    - Transparent → solid scroll transition: `.pt-header` transparent base; `.pt-header.ac-header--scrolled` solid white (JS adds `.ac-header--scrolled` — matches existing `initStickyHeader` output)
    - Desktop layout: flex row, space-between, `position: fixed`, z-index 1000, `transition: background-color var(--pt-trans-med), box-shadow var(--pt-trans-med)`
    - `.pt-nav` links: spacing, colour, hover `--pt-primary`, focus `outline: 2px solid var(--pt-primary)`
    - `.pt-hamburger` hidden on desktop; mobile breakpoint shows it
    - `.pt-mobile-nav` — full-width overlay, hidden by default; `.pt-mobile-nav--open` transitions in
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 19.1_

  - [ ] 5.2 Add `.pt-hero` styles to `home-medic4.css`
    - 50/50 grid layout (`grid-template-columns: 1fr 1fr`), `min-height: 100vh`, `padding-top: 80px`
    - `.pt-hero-left`: flex column, white background, centered content, padding `4rem 3rem 4rem 6rem`
    - `.pt-hero-right`: relative, overflow hidden
    - CSS-only crossfade: `@keyframes ptHeroCrossfade`, three `.pt-hero-slide--1/2/3` rules with `animation-delay: 0s / 5s / 10s`
    - `.pt-hero-overlay-strip`: bottom decorative strip using `--pt-primary`
    - Stats row `.pt-stat-pill`: number in `--pt-primary`, label in `--pt-text`
    - `@media (prefers-reduced-motion: reduce)`: `.pt-hero-slide--1` opacity 1 static; slides 2 & 3 hidden
    - Mobile: stack text above image, image `max-height: 40vh`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 17.2_

  - [ ] 5.3 Add `.pt-booking-banner` styles to `home-medic4.css`
    - Full-width flex strip, `background: var(--pt-primary)`, `min-height: 80px`, vertically centred
    - `.pt-btn-inverse`: white background, `color: var(--pt-primary)`, links to `/book`
    - Mobile: stack text and button vertically, centred
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [ ] 6. Write `home-medic4.css` — Body Parts, Services, How It Works, and Dark CTA styles
  - [ ] 6.1 Add `.pt-body-parts` styles to `home-medic4.css`
    - Two-column panel (`.pt-bp-panel-left` / `.pt-bp-panel-right`), section spacing
    - `.pt-bp-tabs` tab row: flex wrap, `--pt-primary` active underline/border-bottom indicator
    - `.pt-bp-tab--active`: `border-bottom: 2px solid var(--pt-primary)`, `color: var(--pt-primary)`
    - `aria-selected` attribute styling for keyboard focus
    - Mobile: vertical stacked tabs, hide panel-right image
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 17.4_

  - [ ] 6.2 Add `.pt-services` and `.pt-svc-card` styles to `home-medic4.css`
    - `.pt-services-row`: `display: flex; overflow-x: auto; scroll-snap-type: x mandatory; -webkit-overflow-scrolling: touch`
    - `.pt-svc-card`: `flex: 0 0 280px; scroll-snap-align: start; border-radius: 12px; box-shadow: var(--pt-shadow-card)`
    - Hover: `box-shadow: var(--pt-shadow-hover); transform: translateY(-4px); transition: ≤200ms`
    - Card image container, `<h3>` title, description, `.pt-svc-link` button
    - Mobile: `flex-direction: column; overflow-x: visible`; cards `width: 100%`
    - _Requirements: 7.1, 7.2, 7.3, 7.5, 7.6, 17.3_

  - [ ] 6.3 Add `.pt-how-it-works` and `.pt-step` styles to `home-medic4.css`
    - Steps grid (3 columns desktop, 1 column mobile with vertical connector line)
    - `.pt-step-num`: large font, `color: var(--pt-primary)`, `aria-hidden="true"` in markup
    - Pull-quote block `.pt-pull-quote`: border-left `--pt-primary`, italic, centred
    - Scroll reveal: `data-reveal` attribute respected by existing `initScrollReveal` JS
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 17.6, 19.8_

  - [ ] 6.4 Add `.pt-dark-cta` and `.pt-dark-stats` styles to `home-medic4.css`
    - `background: var(--pt-dark); color: var(--pt-white)`, full-width
    - `.pt-dark-stat-num` counter elements with `data-count-target` / `data-count-display` attributes
    - Container retains `social-proof-section` secondary class so `initScrollReveal` triggers `initStatCounters`
    - `@media (prefers-reduced-motion: reduce)`: counters show final values immediately (handled by JS — no CSS needed)
    - Mobile: 2×2 stats grid, stack heading/stats/CTA vertically
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7_

- [ ] 7. Write `home-medic4.css` — Specialists, preserved widgets, footer, and utilities
  - [ ] 7.1 Add `.pt-specialists` styles to `home-medic4.css`
    - Primary card (`.pt-specialist-card--primary`): larger, side-by-side photo + bio layout
    - Supporting cards: equal-width columns in a grid
    - Credential list `ul.pt-specialist-creds`: styled bullets in `--pt-primary`
    - Mobile: single-column card scroll
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 17.5_

  - [ ] 7.2 Add BMI widget styles to `home-medic4.css` under `.pt-bmi`
    - Input borders: `--pt-mint`; focus ring: `2px solid var(--pt-primary)`
    - Font: `var(--pt-font-body)`
    - `.pt-field-error` inline error styling
    - Result card: `--bmi-color` CSS variable accent (preserves existing C# `_bmiColor` logic)
    - _Requirements: 11.1, 11.2, 11.3, 11.7_

  - [ ] 7.3 Add gallery, testimonials, FAQ, feedback, contact, and footer styles to `home-medic4.css`
    - `.pt-gallery`: hover overlay `--pt-primary` at 30%; lightbox `role="dialog"` focus-trap CSS (z-index, backdrop)
    - `.pt-testimonials` (retains `ac-testimonials` secondary class for JS): quote font `--pt-font-heading`; nav dots/arrows `--pt-primary` active state
    - `.pt-faq`: open item `border-left: 3px solid var(--pt-primary)`; `.ac-faq--open .ac-faq-icon { transform: rotate(45deg) }` (class names preserved from existing toggle logic)
    - `.pt-feedback`: star lit colour `--pt-primary`; hover preview `--pt-mint`; textarea border `--pt-mint`, focus `--pt-primary`
    - `.pt-contact`: primary/ghost button styles; footer copyright line `--pt-text`
    - `.pt-floating-cta`: `position: fixed; bottom: 1.5rem; right: 1.5rem`; min tap target 44×44px
    - `.ac-back-to-top`: preserved class, styled with `--pt-primary` background
    - _Requirements: 12.2, 13.3, 13.4, 13.5, 14.3, 14.4, 15.4, 15.5, 16.1, 16.2, 16.3, 16.4, 17.7_

  - [ ] 7.4 Add scroll reveal, responsive breakpoints, and `@media` blocks to `home-medic4.css`
    - `[data-reveal]` opacity/transform base state; `.revealed` end state; `prefers-reduced-motion` override
    - Tablet breakpoint `@media (max-width: 1024px)`: adjust hero padding, services columns
    - Mobile breakpoint `@media (max-width: 768px)`: hero stack, nav hamburger, all section adjustments from Requirements 17
    - Small mobile `@media (max-width: 480px)`: font size adjustments, stat card 1-column
    - `prefers-color-scheme: dark` block explicitly keeping light palette (dark mode opt-out)
    - _Requirements: 4.4, 17.1, 17.2, 17.3, 17.4, 17.5, 1.4_

- [ ] 8. Replace `Home.razor` markup — Header, Mobile Nav, Hero, Booking Banner
  - [ ] 8.1 Replace the `<div class="ac-page">` root and Header section in `Home.razor`
    - Change root wrapper from `<div class="ac-page">` to `<div class="pt-page ac-page">` (retain `ac-page` as secondary class so `initActiveNavHighlight` finds `section[id]` elements correctly)
    - Replace `<header class="ac-header" id="ac-header">` with `<header class="pt-header" id="pt-header">` — update the `initStickyHeader` JS call argument in `OnAfterRenderAsync` from `"#ac-header"` to `"#pt-header"`
    - Replace `<nav class="ac-nav">` with `<nav class="pt-nav ac-nav" ...>` (retain `ac-nav` for `initActiveNavHighlight` JS compat)
    - Update nav links to include `#body-parts` and `#team` per Requirements 3.1; remove old `#doctor` link
    - Replace `<div class="ac-header-actions">` with `<div class="pt-header-actions">` containing only "Book a Visit" button (remove Login and Call Now buttons from header)
    - Replace hamburger `class="ac-hamburger"` with `class="pt-hamburger"`
    - _Requirements: 3.1, 3.2, 3.4, 3.5, 3.6, 3.7, 3.8, 20.1_

  - [ ] 8.2 Replace the Mobile Nav overlay in `Home.razor`
    - Change `class="ac-mobile-nav ..."` to `class="pt-mobile-nav ..."` and update open-state conditional class to `pt-mobile-nav--open`
    - Update mobile nav links to match desktop: `#services`, `#body-parts`, `#team`, `#feedback`, `#contact`
    - Add "Book a Visit" link in mobile nav per Requirements 3.6
    - _Requirements: 3.5, 3.6, 3.7_

  - [ ] 8.3 Replace the Hero section in `Home.razor`
    - Change `<section class="ac-hero" id="top">` to `<section class="pt-hero" id="top">`
    - Replace left panel with `pt-hero-left` containing: kicker, `<h1>Take care of your health</h1>`, sub-paragraph, stats row (99% Cured + 4–8 kg / Month pills), two CTAs ("Book a Visit" → `/book` and "Our Services →" → `#services`)
    - Replace right panel with `pt-hero-right`; update slide classes from `ac-hero-slide--@idx` to `pt-hero-slide--1/2/3`; add `pt-hero-overlay-strip` div
    - Remove the `ac-hero-badge` doctor overlay (moved to Specialists section)
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7_

  - [ ] 8.4 Remove the Guarantee Ticker section and add the Booking Banner section in `Home.razor`
    - Remove the `<section class="ac-ticker" ...>` guarantee ticker markup (the C# `guarantees` array and `initGuaranteeTicker` call are preserved — just remove from markup)
    - Add `<section class="pt-booking-banner" aria-label="Booking call to action">` immediately after the closing `</section>` of the hero
    - Banner content: left text "Ready to start your wellness journey?" + right "Book a Visit Now →" button (`.pt-btn-inverse`)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [ ] 9. Replace `Home.razor` markup — Body Parts, Services, How It Works, Dark CTA
  - [ ] 9.1 Replace the Services section with Body-Part section + new Services section in `Home.razor`
    - Remove old `<section class="ac-services" id="services" ...>` chip row / tab / panel markup entirely
    - Add `<section class="pt-body-parts" id="body-parts" data-reveal>` with ARIA tablist, six tab buttons (bound to `SetBodyPart`), and the conditional two-column panel (using `bodyPartTabs` list from `@code`)
    - Add `<section class="pt-services" id="services" data-reveal>` with four service cards in `.pt-services-row`; each card uses `serviceCards` list (existing data, new markup); first card `loading="eager" fetchpriority="high"`, rest `loading="lazy"`
    - Tab buttons must have `role="tab"`, `aria-selected`, and container must have `role="tablist"` per Requirements 19.6
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 19.6_

  - [ ] 9.2 Add How It Works section in `Home.razor` after the Services section
    - Add `<section class="pt-how-it-works" data-reveal>`
    - Render three steps from `howItWorksSteps` list; step number spans: `<span class="pt-step-num" aria-hidden="true">@step.Number</span>`
    - Add pull-quote block with text and ISO attribution per Requirements 8.4
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 19.8_

  - [ ] 9.3 Replace the Stats section with Dark CTA section in `Home.razor`
    - Remove `<section class="ac-stats social-proof-section" ...>` markup
    - Add `<section class="pt-dark-cta" data-reveal>` containing copy block + stats grid
    - Stats container: `<div class="pt-dark-stats social-proof-section ac-stats" data-reveal>` — retain `social-proof-section` and `ac-stats` as secondary classes for `initStatCounters` JS compat
    - Include CTA button "Book a Visit Now →" per Requirements 9.6
    - Remove the blob dividers (`ac-blob-divider`) — not part of BeMedic4 design
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7_

- [ ] 10. Replace `Home.razor` markup — Specialists, preserved widgets, footer, floating CTAs
  - [ ] 10.1 Replace the Doctor section with Specialists section in `Home.razor`
    - Remove `<section class="ac-doctor" id="doctor" ...>` markup
    - Add `<section class="pt-specialists" id="team" data-reveal>`
    - Primary card: Dr. Manjari photo, name, title, `doctorCredentials` list, "Book Consultation →" button
    - Three supporting practitioner cards from `supportingPractitioners` list with placeholder avatar and "Learn More →" link to `#contact`
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_

  - [ ] 10.2 Update BMI, Gallery, Testimonials, FAQ, Feedback, Contact sections to use `pt-*` class prefix in `Home.razor`
    - BMI: rename wrapper from `ac-bmi` to `pt-bmi`; update field/error class names to `pt-bmi-field`, `pt-field-error`, `pt-bmi-result`; preserve all `@bind`, `@onclick`, and `_bmi*` field references
    - Gallery: rename `ac-gallery` → `pt-gallery`; preserve all `GalleryImages`, `OpenLightbox`, `CanLoadMore`, `_isLightboxOpen`, lightbox navigation handlers; keep `loading="eager"` for first 6 images
    - Testimonials: rename `ac-testimonials` → `pt-testimonials ac-testimonials` (retain `ac-testimonials` for `initTestiObserver` JS); preserve `activeTestiIndex`, `PrevTesti`, `NextTesti`, `JumpToTesti` handlers
    - FAQ: rename `ac-faq` → `pt-faq`; preserve `openFaq`, `ToggleFaq`, open-state class `ac-faq--open` (keep this class so existing CSS transitions still apply or remap in CSS)
    - Feedback: rename `ac-feedback` → `pt-feedback`; preserve `_feedbackRating`, `_feedbackSuccess`, `SubmitFeedbackAsync`, star button handler refs; keep `ac-star--lit` class name as the lit-state class (used in existing C# expression `star <= _feedbackRating`)
    - Contact: rename `ac-contact` → `pt-contact`; preserve all href values, WhatsApp link, Google Maps link, `rel="noopener noreferrer"`
    - _Requirements: 11.1, 11.2, 12.1, 12.2, 12.3, 12.5, 13.1, 13.4, 13.5, 14.1, 15.1, 15.2, 15.3, 16.1, 16.2, 16.3, 20.3_

  - [ ] 10.3 Update Footer, Floating CTA, and Back-to-Top in `Home.razor`
    - Rename `ac-footer` → `pt-footer`; preserve copyright text, Dr. Manjari name, city
    - Rename `ac-floating-cta` → `pt-floating-cta`; preserve WhatsApp and Book appointment links, `aria-label` attributes
    - Keep `ac-back-to-top` id and class on back-to-top button (JS uses `id="ac-back-to-top"` — update `initBackToTop` JS lookup if needed, or keep id as `ac-back-to-top` for zero-JS-change approach)
    - _Requirements: 16.4, 17.7, 20.1_

  - [ ] 10.4 Verify all `OnAfterRenderAsync` JS call arguments are updated in `Home.razor`
    - Confirm `initStickyHeader` call uses `"#pt-header"` (updated in task 8.1)
    - Confirm all other JS calls remain unchanged: `initActiveNavHighlight`, `initBackToTop`, `initGalleryLightbox`, `initTestiObserver`, `scheduleDeferredInit_home`
    - Confirm `DisposeAsync` calls unchanged: `destroyAestheticAnimations`, `destroyHomepageAnimations`, `destroyTestiObserver`
    - _Requirements: 20.1, 20.2, 20.4, 20.5_

- [ ] 11. Checkpoint — Full build and runtime smoke check
  - Run `dotnet build ProTouch.sln` and confirm zero errors
  - Confirm the page renders at `/` without Blazor circuit errors (check browser console is clean)
  - Ensure all tests pass, ask the user if questions arise

- [ ] 12. Write property and unit tests for new and preserved logic
  - [ ] 12.1 Write property test for BodyPartTab selection renders correct panel content
    - **Property 4: Body-part tab selection renders correct panel**
    - Validates: Requirements 6.3
    - Use FsCheck to generate a random index into the 6-item `bodyPartTabs` list; call `SetBodyPart(tab.Id)` on a `Home` component instance; assert `activeBodyPart == tab.Id` and the panel description matches `tab.Description`
    - Tag: `// Feature: medic4-homepage-redesign, Property 4`
    - _Requirements: 6.3_

  - [ ]* 12.2 Write property test for ARIA tab attributes consistency
    - **Property 5: ARIA tab attributes are consistent — exactly one `aria-selected="true"`**
    - Validates: Requirements 6.2, 19.6
    - For any valid tab ID in the set, after `SetBodyPart(id)`: exactly one tab in `bodyPartTabs` matches `activeBodyPart`; the remaining `n-1` do not
    - Tag: `// Feature: medic4-homepage-redesign, Property 5`
    - _Requirements: 6.2, 19.6_

  - [ ]* 12.3 Write property test for BMI computation determinism and category mapping
    - **Property 7: BMI computation is deterministic and produces valid category**
    - Validates: Requirements 11.3, 11.7
    - Use FsCheck generators: height in [100, 250], weight in [20.0, 300.0], goal = "Weight Loss"
    - Assert: computed BMI equals `Math.Round(w / (h/100.0)^2, 1)`; `_bmiCategory` is one of `{"Underweight","Normal","Overweight","Obese"}`; `_bmiColor` matches the category-to-colour mapping
    - Tag: `// Feature: medic4-homepage-redesign, Property 7`
    - _Requirements: 11.3, 11.7_

  - [ ]* 12.4 Write property test for gallery load-more arithmetic
    - **Property 8: Gallery load-more grows visible count by 12 per activation**
    - Validates: Requirements 12.4
    - For any starting `_galleryVisible` in [1, 60), `LoadMoreGallery()` must set `_galleryVisible` to `Math.Min(initial + 12, 60)`
    - Tag: `// Feature: medic4-homepage-redesign, Property 8`
    - _Requirements: 12.4_

  - [ ]* 12.5 Write property test for testimonial carousel index bounds
    - **Property 9: Testimonial carousel index is always in bounds**
    - Validates: Requirements 13.1, 13.2
    - For any number of `NextTesti()` and `PrevTesti()` calls, `activeTestiIndex` must remain in `[0, testimonials.Count - 1]`
    - Tag: `// Feature: medic4-homepage-redesign, Property 9`
    - _Requirements: 13.1, 13.2_

  - [ ]* 12.6 Write property test for FAQ accordion toggle consistency
    - **Property 10: FAQ accordion toggle is a consistent toggle**
    - Validates: Requirements 14.1, 14.2
    - Assert: `ToggleFaq(i)` when `openFaq == i` → `openFaq == -1`; `ToggleFaq(i)` when `openFaq != i` → `openFaq == i`; calling `ToggleFaq(i)` then `ToggleFaq(j)` (i ≠ j) → `openFaq == j`
    - Tag: `// Feature: medic4-homepage-redesign, Property 10`
    - _Requirements: 14.1, 14.2_

  - [ ]* 12.7 Write property test for feedback star selection lit count
    - **Property 6: Feedback star selection round-trip**
    - Validates: Requirements 15.4
    - After `SetFeedbackStar(s)` for any `s` in `{1..5}`: `_feedbackRating == s`; expression `star <= _feedbackRating` is true for exactly `s` values of `star` in `{1..5}`
    - Tag: `// Feature: medic4-homepage-redesign, Property 6`
    - _Requirements: 15.4_

  - [ ] 12.8 Write unit tests for BMI validation edge cases and feedback submission
    - `ComputeBmi_HeightBelowRange_SetsHeightError`: height = 99 → `_heightError` non-empty, `_bmiValue` null
    - `ComputeBmi_WeightAboveRange_SetsWeightError`: weight = 301 → `_weightError` non-empty
    - `ComputeBmi_NoGoalSelected_SetsGoalError`: goal = "" → `_goalError` non-empty
    - `SubmitFeedback_NoRating_SetsErrorMessage`: `_feedbackRating = 0` → `_feedbackError == "Please select a star rating before submitting."`
    - `LoadMoreGallery_AtCeiling_CapsAt60`: `_galleryVisible = 54` → after LoadMore → `_galleryVisible == 60`
    - `ToggleFaq_SameIndex_Closes`: `openFaq = 2`, `ToggleFaq(2)` → `openFaq == -1`
    - `ToggleFaq_DifferentIndex_OpensNew`: `openFaq = 1`, `ToggleFaq(3)` → `openFaq == 3`
    - _Requirements: 11.4, 11.5, 11.6, 12.4, 14.2, 15.3_

- [ ] 13. Final checkpoint — Build and all tests pass
  - Run `dotnet build ProTouch.sln` confirming zero errors
  - Run `dotnet test ProTouch.sln` confirming all tests pass, including new tests in `ClinicApp.Tests/Medic4HomepageTests.cs`
  - Ensure all tests pass, ask the user if questions arise

---

## Notes

- Tasks marked with `*` are optional and can be skipped for a faster MVP implementation
- Each task references specific requirements for full traceability
- Backward-compatible secondary classes (`ac-testimonials`, `social-proof-section`, `ac-page`, `ac-nav`) are intentional — they preserve existing `site.js` behavior with zero JS changes
- `initStickyHeader` in `site.js` adds `.ac-header--scrolled` (hardcoded) — the CSS in `home-medic4.css` targets `.pt-header.ac-header--scrolled` to handle this without JS modification
- `initActiveNavHighlight` in `site.js` queries `.ac-page section[id]` — the root wrapper retains `ac-page` as a secondary class for this reason
- All property tests are written against the `Home` component's `@code` logic (pure C#), not against rendered DOM — no bUnit dependency required
- New tests go in `ClinicApp.Tests/Medic4HomepageTests.cs`

---

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "2.1"] },
    { "id": 1, "tasks": ["1.2", "1.3", "2.2", "3.1"] },
    { "id": 2, "tasks": ["3.2"] },
    { "id": 3, "tasks": ["5.1", "5.2", "5.3", "6.1", "6.2", "6.3", "6.4", "7.1", "7.2", "7.3", "7.4"] },
    { "id": 4, "tasks": ["8.1", "8.2", "8.3", "8.4"] },
    { "id": 5, "tasks": ["9.1", "9.2", "9.3"] },
    { "id": 6, "tasks": ["10.1", "10.2", "10.3"] },
    { "id": 7, "tasks": ["10.4"] },
    { "id": 8, "tasks": ["12.1", "12.2", "12.3", "12.4", "12.5", "12.6", "12.7", "12.8"] }
  ]
}
```
