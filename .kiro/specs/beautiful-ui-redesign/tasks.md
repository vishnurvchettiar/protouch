# Implementation Plan: Beautiful UI Redesign — ProTouch Physio Clinic

## Overview

This plan incrementally applies the design-token foundation, Tailwind integration, layout updates, and per-page styling changes described in the design document. Each task targets specific files and references the relevant requirements. Changes are additive wherever possible — no existing Bootstrap classes or structural Razor markup is removed. The plan concludes by wiring all changes together and verifying correctness properties through property-based and accessibility tests.

---

## Tasks

- [ ] 1. Establish design token foundation and CSS layer structure
  - [ ] 1.1 Add `@layer tokens` block to `app.css` with all `:root`, `[data-theme="light"]`, and `[data-theme="dark"]` CSS custom property declarations
    - Define `--pt-brand-green`, `--pt-brand-green-hover`, `--pt-brand-green-light`, `--pt-font-sans`, all `--pt-radius-*`, `--pt-shadow-*`, `--pt-bg-page`, `--pt-surface`, `--pt-text-primary`, `--pt-text-muted`, `--pt-border` tokens
    - _Requirements: 1.1, 1.2, 1.3, 1.6_

  - [ ] 1.2 Add global animation keyframes and `prefers-reduced-motion` override block to `app.css`
    - Add `@keyframes bounceIn`, `@keyframes fadeIn`, `@keyframes pulseGlow`
    - Add single `@media (prefers-reduced-motion: reduce)` rule that sets all `animation-duration`, `transition-duration` to `0.01ms`
    - _Requirements: 5.1, 5.4, 9.1, 9.2_

  - [ ] 1.3 Add global accessibility and touch-target rules to `app.css`
    - Add `:focus-visible` outline rule (`2px solid var(--pt-brand-green)`, offset `3px`)
    - Add `min-height: 44px; min-width: 44px` rule targeting `button`, `a[role="button"]`, `.pt-nav-link`, `.pt-tab-btn`
    - _Requirements: 6.5, 14.3, 15.1, 15.1_

  - [ ]* 1.4 Write property test for design token presence
    - **Property 1: Type Scale Compliance** — verify `h1`, `h2`, `h3` computed `font-size` values meet minimums in both themes
    - **Property 12: Interactive Element Focus Indicator Contrast** — verify `:focus-visible` outline achieves ≥ 3:1 contrast against adjacent surface
    - **Validates: Requirements 1.1, 3.3, 15.1**

- [ ] 2. Integrate Tailwind CSS and update `App.razor`
  - [ ] 2.1 Add Tailwind Play CDN `<script>` tag and inline `tailwind.config` block to `App.razor`
    - Map `colors.brand`, `colors.brand-hover`, `colors.brand-light` in the Tailwind theme extension
    - Add `Outfit` to the Google Fonts `href` string alongside the existing Manrope / Instrument Sans entries
    - _Requirements: 2.1, 2.2, 2.3, 8.1_

  - [ ] 2.2 Migrate `home-medic4.css` public-theme token references to use `var(--pt-*)` global tokens
    - Replace any locally-declared token overrides with references to the global `@layer tokens` values so there is no duplication
    - Preserve all existing Bootstrap class names untouched
    - _Requirements: 2.4, 2.5_

- [ ] 3. Update layout components for theme scoping and responsive navigation
  - [ ] 3.1 Update `PublicLayout.razor` to wrap `@Body` in `<div data-theme="light" class="pt-public-root">`
    - Add base `.pt-public-root` CSS rule to `app.css` resetting `background`, `color`, and `font-family` to light-theme tokens
    - _Requirements: 1.4, 3.1, 3.2_

  - [ ] 3.2 Update `MainLayout.razor` to add `data-theme="dark"` attribute to the root `.page` div, add `_drawerOpen` bool field, and inject a hamburger `<button>` in the `<header class="pt-topbar">` that toggles `_drawerOpen`
    - _Requirements: 1.5, 8.3, 8.4_

  - [ ] 3.3 Add responsive sidebar CSS rules to `app.css` for `NavMenu` — `220px` fixed at `lg`, `64px` icon-only rail at `md`, slide-in drawer below `768px` — including `.pt-sidebar--open` toggle class and `200ms` transition
    - Add `.pt-nav-link--active` active route styling (Brand Green left border, `rgba(0,141,54,0.08)` background, white text)
    - _Requirements: 8.1, 8.2, 8.5_

  - [ ]* 3.4 Write property test for active nav item styling
    - **Property 5: Active Nav Item Styling** — verify that the active `NavLink` anchor has `border-left: 3px solid #008d36`, `background: rgba(0,141,54,0.08)`, `color: #ffffff`
    - **Validates: Requirements 8.2**

- [ ] 4. Implement typography and section-spacing system
  - [ ] 4.1 Add type-scale CSS rules to `app.css` scoped under `[data-theme="light"]` / `.pt-public-root`
    - Set `body` / `.pt-public-root`: `font-family: var(--pt-font-sans)`, `font-size: 16px`, `line-height: 1.65`
    - Set `h1: 2.5rem`, `h2: 1.875rem`, `h3: 1.375rem` with `@media (min-width: 768px)` upscaling to `3.5rem / 2.75rem / 2rem`
    - Add `.pt-section` (`padding: 4rem 0`; `6rem 0` at `768px`) and `.pt-container` (`max-width: 1200px; margin-inline: auto`) utility classes
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

  - [ ]* 4.2 Write property test for type scale compliance
    - **Property 1: Type Scale Compliance** — verify computed `font-size` of each heading element is ≥ the minimum specified value at both mobile and `md` breakpoints
    - **Validates: Requirements 3.3**

- [ ] 5. Checkpoint — ensure token, Tailwind, and layout changes build cleanly
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 6. Style `Home.razor` — public page colour, brand identity, and interactions
  - [ ] 6.1 Add `.pt-btn-primary` CSS rule to `app.css` with Brand Green fill, white text, hover darken to `#007030`, `translateY(-2px)` transform, and box-shadow increase; add sticky header `.pt-header--scrolled` rule; add nav link underline slide animation for `.pt-nav-link`
    - _Requirements: 4.1, 4.2, 4.4, 5.3_

  - [ ] 6.2 Add service-card grid (`.pt-services-row`) and card hover-lift (`.pt-svc-card`) CSS rules to `app.css`
    - Grid: 1 col mobile → 2 at `sm` → 4 at `lg`; `var(--pt-radius-lg)` border-radius; `var(--pt-shadow-md)` box-shadow; `translateY(-6px)` on hover at `200ms`
    - _Requirements: 4.6, 5.2_

  - [ ] 6.3 Add floating CTA button styles (`.pt-float-btn`) with `bounceIn` entrance and `pulseGlow` hover animation to `app.css`
    - Ensure CTA sections (hero, booking banner) use Brand Green gradient with ≥ 4.5:1 contrast against white overlay text
    - _Requirements: 4.5, 5.5_

  - [ ] 6.4 Update `Home.razor` Razor markup — add `data-reveal` to any sections missing it; add `pt-float-btn` class to floating CTA buttons; ensure service card elements carry `.pt-svc-card` and image containers have `object-fit: cover` with explicit `aspect-ratio`
    - _Requirements: 5.1, 14.5_

  - [ ]* 6.5 Write property tests for public page visual invariants
    - **Property 3: Reduced-Motion Disables Transforms** — verify `transition-duration ≤ 0.01ms` and identity transform when `prefers-reduced-motion: reduce`
    - **Property 11: Image Object-Fit and Aspect-Ratio Invariant** — verify `.pt-svc-card` images have `object-fit: cover` and container has explicit `aspect-ratio`
    - **Validates: Requirements 5.4, 14.5**

- [ ] 7. Style `ServiceBooking.razor`
  - [ ] 7.1 Add service selection card grid and selected-state styles (`.pt-book-svc-card`, `.pt-book-svc-card.selected`) to `app.css`; add shared form focus-ring rule (`.clinic-input:focus`, `.pt-field:focus-within` children); add validation error styles (`.clinic-input.is-invalid`, `.pt-field-error`); add booking success `fade-in` panel style
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

  - [ ] 7.2 Update `ServiceBooking.razor` — add `selected` CSS class toggle on card selection, apply `is-invalid` to inputs with validation errors, add `pt-btn-primary--full` modifier to the submit button on mobile (`w-full` Tailwind utility), and ensure button `min-height: 44px`
    - _Requirements: 6.1, 6.3, 6.5_

  - [ ]* 7.3 Write property test for input focus-ring invariant
    - **Property 7: Input Focus-Ring Invariant** — verify `.clinic-input:focus` produces `outline: 2px solid #008d36`, `outline-offset: 4px`, and `box-shadow` containing `rgba(0,141,54,0.25)`
    - **Validates: Requirements 6.2, 10.4, 13.5**

- [ ] 8. Style `ServiceDetail.razor`
  - [ ] 8.1 Add hero banner (`.pt-svc-hero`), key-stat card grid (`.pt-stat-grid`, `.pt-stat-card`), treatment card glassmorphism (`.pt-treatment-card`), sub-service pill (`.pt-sub-pill`, `.pt-sub-pill.active`), and inline booking section (`.pt-inline-book`) CSS rules to `app.css`
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

  - [ ] 8.2 Update `ServiceDetail.razor` markup — add `pt-svc-hero`, `pt-stat-grid`, `pt-stat-card`, `pt-treatment-card`, `pt-sub-pill` / `active` toggle, and `pt-inline-book` classes to the appropriate elements
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 9. Style `AdminDashboard.razor`
  - [ ] 9.1 Add `.glass-panel`, `.clinic-table`, `.clinic-input`, `.clinic-alert` family, `.pt-stat-card` hover-lift, `.dashboard-grid`, `.pt-appt-chip`, `.pt-activity-dot`, and `.pt-quick-btn` CSS rules to `app.css`
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 10.1, 10.2, 10.3, 10.4, 10.5_

  - [ ] 9.2 Update `AdminDashboard.razor` markup — ensure `.pt-activity-dot` elements carry `visit` or `booking` kind class; verify stat cards use `.dashboard-grid` and `.pt-stat-card`; verify table uses `.clinic-table`; verify alert banners use `.clinic-alert-danger` / `.clinic-alert-info`
    - _Requirements: 9.3, 9.4, 9.5, 10.2, 10.5_

  - [ ]* 9.3 Write property test for glass-panel visual invariant
    - **Property 6: glass-panel Visual Invariant** — verify any `.glass-panel` element inside the dashboard has the correct `background`, `border`, `border-radius`, and `box-shadow` computed values
    - **Validates: Requirements 10.1**

- [ ] 10. Style `ClientDashboard.razor`
  - [ ] 10.1 Add `.pt-client-hero`, `.pt-ring-svg`, `.pt-next-appt-card`, `.pt-diet-preview`, `.pt-checkin-prompt` states, `.pt-stat-pill`, `.pt-stat-pills`, `.chart-container` aspect-ratio rules, macro slider panel, `.pt-macro-warning`, and `.meal-upload-panel` / `.meal-preview` CSS rules to `app.css`
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 12.1, 12.2, 12.3, 12.4, 12.5_

  - [ ] 10.2 Update `ClientDashboard.razor` markup — add `pt-macro-warning` amber warning bar above the submit button (rendered when macro sum ≠ 100); ensure `.meal-upload-panel` and `.meal-preview` classes are present on the upload zone; ensure SVG progress ring `<circle>` uses the `pt-ring-svg` class; verify stat pills carry `.pt-stat-pill`
    - _Requirements: 11.2, 11.4, 12.3, 12.4, 12.5_

  - [ ]* 10.3 Write property test for macro sum validation invariant
    - **Property 8: Macro Sum Validation Invariant** — verify amber warning element is visible when `carbs + protein + fat + veg ≠ 100` and absent when sum equals `100`
    - **Validates: Requirements 12.4**

- [ ] 11. Checkpoint — ensure all dashboard styling changes are visually consistent
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 12. Update Auth pages
  - [ ] 12.1 Add `.pt-auth-card` CSS rule (`max-width: 440px; margin: 4rem auto; background: var(--pt-surface); border-radius: var(--pt-radius-lg); box-shadow: var(--pt-shadow-lg)`) and `.pt-auth-card .validation-errors` alert panel styles to `app.css`
    - _Requirements: 13.1, 13.4_

  - [ ] 12.2 Update each Auth page (`Login.razor`, `Register.razor`, `ForgotPassword.razor`, `ResetPassword.razor`, `ConfirmEmail.razor` and Manage sub-pages) — wrap the form in a `<div class="pt-auth-card">`, add the ProTouch logo `<img>` at `height="48"` above the card, apply `w-full` to the primary submit button, and add `class="validation-errors"` to the `<ValidationSummary>` component
    - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5_

- [ ] 13. Extend `site.js` with sticky header, scroll-reveal, and focus-trap utilities
  - [ ] 13.1 Verify (or add) `initStickyHeader("#pt-header")` in `site.js` — must add `.pt-header--scrolled` class when `window.scrollY > 60` and remove it otherwise
    - _Requirements: 4.4_

  - [ ] 13.2 Verify (or add) `IntersectionObserver` in `site.js` that adds `.pt-revealed` class to all `[data-reveal]` elements when they enter the viewport (threshold `0.15`); configure it to not re-observe once revealed
    - _Requirements: 5.1_

  - [ ] 13.3 Add `trapFocus(element)` utility to `site.js` — queries all focusable children, intercepts `Tab` / `Shift+Tab` to wrap focus within the overlay, and restores focus to the triggering element on close; wire it to the gallery lightbox and diet plan panel open/close events
    - _Requirements: 15.4_

- [ ] 14. Implement responsive layout safeguards
  - [ ] 14.1 Add `overflow-x: hidden` to `html, body` in `app.css`; add `max-width: 100%` and `object-fit: cover` to all gallery and service card images globally
    - _Requirements: 14.1, 14.4, 14.5_

  - [ ] 14.2 Audit all grid and flex containers across `Home.razor`, `ServiceDetail.razor`, `ServiceBooking.razor`, `AdminDashboard.razor`, `ClientDashboard.razor` and add `grid-template-columns: 1fr` overrides in `app.css` for viewports below `640px`
    - _Requirements: 14.2_

  - [ ]* 14.3 Write property tests for responsive layout
    - **Property 9: No Horizontal Scroll at Any Breakpoint** — verify `document.documentElement.scrollWidth ≤ window.innerWidth` at `375px`, `768px`, `1024px`, and `1440px` viewport widths
    - **Property 10: Single-Column Grid Below 640px** — verify every grid container resolves to a single column at viewport width `< 640px`
    - **Validates: Requirements 14.2, 14.4**

- [ ] 15. Accessibility audit and fixes
  - [ ] 15.1 Audit all non-decorative `<img>` elements across all pages and add descriptive `alt` text; set `alt=""` and `aria-hidden="true"` on purely decorative images
    - _Requirements: 15.2_

  - [ ] 15.2 Audit all `<input>` and `<select>` elements across all forms; ensure each has either a `<label for="...">` or a non-empty `aria-label` attribute
    - _Requirements: 15.3_

  - [ ] 15.3 Audit heading hierarchy (`h1`–`h3`) on every page; correct any skipped levels or multiple `<h1>` occurrences
    - _Requirements: 15.5_

  - [ ]* 15.4 Write property tests for accessibility invariants
    - **Property 2: WCAG AA Text Contrast** — verify computed `color`/`background-color` contrast ≥ 4.5:1 for text elements in both light and dark themes
    - **Property 13: Non-Decorative Image Alt Text** — verify every `<img>` without `aria-hidden="true"` has a non-empty `alt`; decorative images have `alt=""`
    - **Property 14: Form Input Label Association** — verify every `<input>` / `<select>` has a matching `<label for>` or non-empty `aria-label`
    - **Property 15: Heading Hierarchy Integrity** — verify no skipped heading levels and at most one `<h1>` per page
    - **Validates: Requirements 15.2, 15.3, 15.5, 15.6**

  - [ ]* 15.5 Write property test for mobile touch target sizes
    - **Property 4: Mobile Touch Target Size** — verify computed `height ≥ 44px` and `width ≥ 44px` for all interactive elements at `375px` viewport width
    - **Validates: Requirements 6.5, 14.3**

- [ ] 16. Final integration and wiring
  - [ ] 16.1 Cross-check the File Change Summary in the design document against all modified files; ensure every file listed is updated, and no unlisted file has been inadvertently changed
    - _Requirements: 1.1–15.6 (all)_

  - [ ]* 16.2 Write integration tests for end-to-end theme and token propagation
    - **Property 7: Input Focus-Ring Invariant** — cross-page check: verify focus-ring CSS applies consistently on Auth, Dashboard, and Booking form inputs
    - **Property 5: Active Nav Item Styling** — route-change check: verify active class updates correctly when navigating between routes
    - **Validates: Requirements 10.4, 8.2, 13.5_

- [ ] 17. Final checkpoint — ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

---

## Notes

- Tasks marked with `*` are optional and can be skipped for a faster MVP delivery
- Each task references the specific requirement acceptance criteria for full traceability
- All CSS changes are concentrated in `app.css` and `home-medic4.css`; Razor files receive only additive class-name and attribute changes
- The Tailwind Play CDN approach requires no build-pipeline changes — purging is handled at runtime by DOM scanning
- Property tests (Tasks 1.4, 3.4, 4.2, 6.5, 7.3, 9.3, 10.3, 14.3, 15.4, 15.5, 16.2) validate universal CSS invariants using a browser automation or computed-style assertion framework (e.g., Playwright + computed style checks)
- Checkpoints at Tasks 5, 11, and 17 are synchronisation points — validate that previous changes are consistent before proceeding

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3"] },
    { "id": 1, "tasks": ["1.4", "2.1", "2.2"] },
    { "id": 2, "tasks": ["3.1", "3.2", "4.1"] },
    { "id": 3, "tasks": ["3.3", "3.4", "4.2"] },
    { "id": 4, "tasks": ["6.1", "6.2", "6.3"] },
    { "id": 5, "tasks": ["6.4", "7.1", "8.1", "9.1", "10.1", "12.1", "13.1", "13.2", "13.3", "14.1"] },
    { "id": 6, "tasks": ["6.5", "7.2", "8.2", "9.2", "10.2", "12.2", "14.2"] },
    { "id": 7, "tasks": ["7.3", "9.3", "10.3", "14.3", "15.1", "15.2", "15.3"] },
    { "id": 8, "tasks": ["15.4", "15.5", "16.1"] },
    { "id": 9, "tasks": ["16.2"] }
  ]
}
```
