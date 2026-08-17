# Requirements Document

## Introduction

Redesign the public homepage (`/`) of the ProTouch Physio – Slimming Clinic Blazor Server application to be visually impressive, dynamic, and conversion-focused. The aesthetic must reflect a **professional physiotherapy and slimming clinic** — clean, credible, and medically trustworthy — focused on physiotherapy and clinical slimming treatment. All existing sections are retained and upgraded. Two new sections are added: a Social Proof section and a Floating Sticky Booking Bar. Animations are powered by vanilla JavaScript in `site.js` and called from Blazor via `IJSRuntime`. All CSS changes are scoped to `.wellness-page` in `app.css` so admin/dashboard styles are never affected.

---

## Glossary

- **Homepage**: The Blazor Server page rendered at route `/`, implemented in `Home.razor` and styled via `app.css`.
- **Hero Section**: The full-width banner at the top of the Homepage containing the headline, CTAs, and the 5-image sub-carousel.
- **Sub-carousel**: The 5-image auto-rotating image slideshow inside the Hero Section.
- **Gallery Carousel**: The 18-slide auto-rotating clinic gallery section with dot navigation.
- **Floating Sticky Bar**: A fixed-position booking bar that becomes visible after the Hero Section scrolls out of the viewport.
- **Social Proof Section**: A new section containing animated stat counters and 2–3 outcome callouts/testimonials.
- **Scroll Reveal**: A CSS + JS animation pattern where elements fade or slide into view as they enter the viewport, powered by `IntersectionObserver`.
- **Typewriter Effect**: A JS animation that renders headline text character-by-character to simulate typing.
- **Stat Counter Roll-Up**: A JS animation that increments a numeric value from zero to its target value when the element enters the viewport.
- **Guarantee Strip Ticker**: An animated horizontally-scrolling marquee displaying the clinic's five guarantee phrases.
- **IJSRuntime**: The Blazor interface used to call JavaScript functions from C# in `OnAfterRenderAsync`.
- **site.js**: The project's vanilla JavaScript file (`wwwroot/site.js`) where all animation and UI interaction functions are defined.
- **Reduced Motion**: The CSS media feature `prefers-reduced-motion: reduce`, used to suppress animations for users who have requested minimal motion in their OS settings.
- **Micro-interaction**: A small, localised animation triggered by user interaction (e.g. hover lift on a service card).
- **CTA**: Call to Action — a button or link prompting a specific user action such as booking or opening WhatsApp.
- **WhatsApp CTA**: A link opening `https://wa.me/919028027888` in a new tab.
- **Book CTA**: An internal link navigating to `/book`.
- **wellness-page**: The CSS class scoping all public-homepage styles; applied to the root `<div>` in `Home.razor`.
- **dashboard-theme**: The CSS class scoping all admin/portal styles; must remain unaffected by all changes in this spec.

---

## Requirements

---

### Requirement 1 — Hero Section: On-Load Animations

**User Story:** As a first-time visitor, I want to see the hero headline animate on load so that the page feels dynamic and professional from the first moment.

#### Acceptance Criteria

1. WHEN the Homepage finishes its first render, THE Hero Section SHALL trigger either a typewriter animation on the `<h1>` headline text OR stat counter roll-up animations on any numeric values displayed in the Hero Section.
2. WHEN the typewriter animation is active, THE Homepage SHALL render each character of the headline sequentially at a pace of no faster than 40 milliseconds per character until the full headline is visible.
3. WHEN stat counter roll-ups are used, THE Homepage SHALL animate each counter from zero to its target value over a duration of 1200 milliseconds using an easing curve.
4. WHEN the Homepage detects `prefers-reduced-motion: reduce`, THE Hero Section SHALL display the fully-rendered headline and all stat values immediately without any animation.
5. THE 5-image sub-carousel inside the Hero Section SHALL continue to auto-rotate at its existing 5-second interval after on-load animations complete.

---

### Requirement 2 — Scroll-Triggered Reveal Animations

**User Story:** As a visitor scrolling through the homepage, I want content to animate into view as I reach each section so that the page feels engaging and guides my attention progressively.

#### Acceptance Criteria

1. THE Homepage SHALL apply the `data-reveal` attribute to every section, service card, care-reason item, program point, social proof card, and gallery carousel wrapper that should animate on scroll.
2. WHEN a `data-reveal` element enters the viewport by at least 15% of its height, THE Homepage SHALL add the CSS class `revealed` to that element, triggering its fade-in and/or slide-up transition.
3. THE Homepage SHALL initialise a single `IntersectionObserver` instance in `site.js` with a threshold of `0.15` to manage all `data-reveal` elements.
4. THE Homepage SHALL call the `initScrollReveal` function in `site.js` via `IJSRuntime.InvokeVoidAsync` inside `OnAfterRenderAsync` on first render only.
5. WHEN `prefers-reduced-motion: reduce` is detected, THE Homepage SHALL apply the `revealed` class to all `data-reveal` elements immediately on page load, bypassing the `IntersectionObserver` entirely.
6. THE slide-in transition duration SHALL be 500 milliseconds with an ease-out timing function.
7. THE Homepage SHALL not apply scroll reveal to elements inside the fixed Floating Sticky Bar.

---

### Requirement 3 — Social Proof Section

**User Story:** As a prospective patient, I want to see verified clinic outcomes and patient statistics so that I can trust the clinic's credibility before booking.

#### Acceptance Criteria

1. THE Homepage SHALL include a Social Proof Section positioned after the "Why ProTouch" care panel and before the Programs section.
2. THE Social Proof Section SHALL display a minimum of four animated stat counters with the following values and labels: "4–8 kg" (Weight Loss / Month), "2–5 inches" (Lost Per Session), "500+" (Patients Served), and "10+" (Years of Experience).
3. WHEN a stat counter enters the viewport, THE Social Proof Section SHALL trigger a roll-up animation counting from zero to the target numeric value over 1200 milliseconds.
4. THE Social Proof Section SHALL display a minimum of two outcome callouts or patient testimonials, each containing a short quote or result description and a role or treatment label (e.g. "Weight Loss Patient").
5. THE Social Proof Section SHALL NOT include any personal patient names, photographs, or identifiable information.
6. WHEN `prefers-reduced-motion: reduce` is detected, THE Social Proof Section SHALL display all final stat values immediately without counter animation.
7. THE Social Proof Section SHALL be fully responsive: stat counters SHALL display in a 2×2 grid on viewports narrower than 520px and in a single row on viewports 521px and wider.
8. THE Social Proof Section SHALL use typography, colour, and spacing consistent with the `.wellness-page` design system (green `#008d36` accent, Playfair Display for headings, Outfit for body text).

---

### Requirement 4 — Floating Sticky Booking Bar

**User Story:** As a visitor reading any part of the homepage, I want a persistent booking prompt always visible so that I can initiate a booking without scrolling back to the top.

#### Acceptance Criteria

1. THE Homepage SHALL render a Floating Sticky Bar as a fixed-position element with `position: fixed`, `bottom: 0`, `left: 0`, `width: 100%`, and a `z-index` of at least 200.
2. WHEN the Hero Section scrolls fully out of the viewport (i.e. the Hero Section's bottom edge is above the top of the viewport), THE Floating Sticky Bar SHALL become visible by transitioning from `opacity: 0; transform: translateY(100%)` to `opacity: 1; transform: translateY(0)` over 300 milliseconds.
3. WHEN the Hero Section is fully or partially within the viewport, THE Floating Sticky Bar SHALL remain hidden (`opacity: 0; transform: translateY(100%)`).
4. THE Floating Sticky Bar SHALL contain a "Book Online" CTA linking to `/book` and a "WhatsApp Us" CTA linking to `https://wa.me/919028027888` with `target="_blank"` and `rel="noopener"`.
5. THE Floating Sticky Bar SHALL display both CTAs at equal visual weight using the same button size and padding.
6. THE Homepage SHALL initialise the Floating Sticky Bar visibility logic by calling the `initStickyBar` function in `site.js` via `IJSRuntime.InvokeVoidAsync` inside `OnAfterRenderAsync` on first render only.
7. WHEN `prefers-reduced-motion: reduce` is detected, THE Floating Sticky Bar SHALL appear and disappear instantly without transition.
8. THE Floating Sticky Bar SHALL be fully accessible: both CTA buttons SHALL have descriptive `aria-label` attributes.
9. THE Floating Sticky Bar SHALL NOT overlap or obscure the page footer when the footer is in view; the bar SHALL add bottom padding equal to the bar's own height to the `.wellness-page` element to prevent content being permanently obscured.

---

### Requirement 5 — Guarantee Strip Animated Ticker

**User Story:** As a visitor, I want to see the clinic's core guarantees presented as a smooth animated ticker so that the promises register clearly without requiring any interaction.

#### Acceptance Criteria

1. THE Guarantee Strip SHALL animate its five guarantee phrases ("No Fasting", "No Medicine", "No Surgery", "No Side Effects", "No Down Time") as a continuously scrolling horizontal marquee.
2. THE ticker animation SHALL use a CSS `@keyframes` animation named `ticker-scroll` that translates the content list from `translateX(0)` to `translateX(-50%)` over a duration of 18 seconds with `linear` timing and `infinite` iteration.
3. THE ticker content SHALL be duplicated so that the scroll appears seamless with no visible gap or reset jump.
4. WHEN `prefers-reduced-motion: reduce` is detected, THE Guarantee Strip SHALL display all five phrases statically in a centred flex row without any scrolling animation.
5. THE Guarantee Strip SHALL remain `background: #008d36` and `color: white` consistent with the existing `.wellness-page` design.

---

### Requirement 6 — Service Cards: Hover Micro-Interactions

**User Story:** As a visitor hovering over a service card, I want to see a subtle animated response so that the cards feel interactive and draw attention to their links.

#### Acceptance Criteria

1. WHEN a pointer device hovers over a `.service-card-home` element, THE service card SHALL transition to `transform: translateY(-6px)` and `box-shadow: 0 16px 48px rgba(0,0,0,0.12)` over 220 milliseconds with an ease-out timing function.
2. WHEN a pointer device hovers over a `.service-card-home` element, THE `.svc-icon-wrap` inside that card SHALL transition to `transform: scale(1.12) rotate(6deg)` over 220 milliseconds.
3. WHEN pointer hover ends, THE `.service-card-home` and `.svc-icon-wrap` SHALL return to their resting state over 220 milliseconds.
4. WHEN `prefers-reduced-motion: reduce` is detected, THE service card SHALL apply only the `box-shadow` change on hover and SHALL NOT apply any `transform` or rotation animation.
5. THE `.service-card-link:focus-visible .service-card-home` state SHALL apply the same lift as the hover state to ensure keyboard navigability.

---

### Requirement 7 — Gallery Carousel: Visual Polish

**User Story:** As a visitor viewing the gallery, I want the existing 18-slide carousel to look visually rich and polished so that the clinic's facilities are presented impressively.

#### Acceptance Criteria

1. THE Gallery Carousel SHALL retain all 18 existing slides, captions, and the dot navigation from the current implementation.
2. THE Gallery Carousel SHALL retain the existing 4-second auto-rotation timer and the `JumpToSlide` click-to-navigate behaviour.
3. THE slide transition SHALL use a cross-fade (`opacity` transition) of 600 milliseconds between slides.
4. THE active dot indicator SHALL scale to `1.4×` its resting size with a `background: #008d36` colour when the corresponding slide is active.
5. THE Gallery Carousel container SHALL display a subtle green-tinted `box-shadow: 0 16px 56px rgba(0,141,54,0.15)` to frame the carousel visually against the section background.
6. WHEN `prefers-reduced-motion: reduce` is detected, THE Gallery Carousel SHALL still advance slides but SHALL switch between slides instantly without the opacity cross-fade transition.

---

### Requirement 8 — Contact Section: Urgency and Prominence

**User Story:** As a visitor reaching the bottom of the homepage, I want the contact section to feel urgent and prominent so that I am motivated to reach out or book immediately.

#### Acceptance Criteria

1. THE Contact Section SHALL display a primary CTA ("Book Online →") using the `.wellness-primary` button style and a secondary WhatsApp CTA using the `.wellness-secondary` button style, both positioned prominently within the section.
2. THE Contact Section SHALL include a visible urgency label or sub-heading (e.g., "Take the first step today — consultations available now") rendered above the CTA buttons.
3. THE Contact Section background SHALL use `background: #1a4d2e` (deep clinical green) with white and `#ffeb00` text for clear contrast, maintaining the existing `.wellness-page` contact-section styling.
4. WHEN a user activates the "Book Online" CTA in the Contact Section, THE Contact Section SHALL navigate the user to `/book`.
5. WHEN a user activates the WhatsApp CTA in the Contact Section, THE Contact Section SHALL open `https://wa.me/919028027888` in a new browser tab with `rel="noopener"`.

---

### Requirement 9 — Typography and Spacing Upgrades

**User Story:** As a visitor, I want every section of the homepage to feel considered and premium so that the clinic's credibility is reinforced visually throughout.

#### Acceptance Criteria

1. THE Homepage SHALL apply `font-family: 'Playfair Display', Georgia, serif` to all `<h1>`, `<h2>`, and `<h3>` elements within `.wellness-page`.
2. THE Homepage SHALL apply `font-family: 'Outfit', sans-serif` to all body text, labels, and CTA buttons within `.wellness-page`.
3. THE section vertical padding within `.wellness-page .wellness-section` SHALL be a minimum of `100px` on viewports wider than 850px and a minimum of `72px` on narrower viewports.
4. THE Homepage SHALL render section kicker labels (`.section-kicker`) in uppercase with `letter-spacing: 0.18em` and `font-weight: 900` in `#008d36`.
5. ALL colour values, spacing tokens, and typographic scale used in new and upgraded sections SHALL be consistent with the existing `.wellness-page` design tokens defined in `app.css`.
6. NO CSS rule introduced by this feature SHALL target any selector outside `.wellness-page`, `.auth-page`, or elements scoped to the public layout — the `.dashboard-theme` and admin panel styles SHALL remain unaffected.

---

### Requirement 10 — JavaScript Architecture and Blazor Integration

**User Story:** As a developer maintaining this project, I want all animation JavaScript to be organised in `site.js` and called cleanly from Blazor so that the code is maintainable and avoids duplication.

#### Acceptance Criteria

1. THE Homepage SHALL define all animation and UI initialisation logic — including `initScrollReveal`, `initStickyBar`, `initTypewriter` (or `initStatCounters`), `initGuaranteeTicker`, and `initStatCounters` — as named functions exported on the global `window` object inside `site.js`.
2. THE Homepage SHALL call each `site.js` initialisation function exactly once, via `IJSRuntime.InvokeVoidAsync`, inside `OnAfterRenderAsync(bool firstRender)` gated by `if (firstRender)`.
3. WHEN the Blazor component is disposed, THE Homepage SHALL call a `destroyHomepageAnimations` function via `IJSRuntime.InvokeVoidAsync` inside the `Dispose()` method to disconnect all `IntersectionObserver` instances and clear all timers.
4. THE `site.js` functions SHALL each check `window.matchMedia('(prefers-reduced-motion: reduce)').matches` and apply reduced-motion behaviour as specified in the relevant requirement before setting up any animation.
5. THE `site.js` functions SHALL be idempotent: calling any initialisation function more than once SHALL NOT create duplicate observers or event listeners.
6. THE `Home.razor` component SHALL implement `IAsyncDisposable` to support `await`-based cleanup of `IJSRuntime` calls on disposal.

---

### Requirement 11 — Mobile Responsiveness

**User Story:** As a visitor on a mobile device, I want the redesigned homepage to look polished and function correctly on small screens so that I can engage with the clinic regardless of my device.

#### Acceptance Criteria

1. THE Homepage SHALL render in a single-column layout on viewports narrower than 850px for all multi-column sections (hero, services, care panel, programs, social proof, contact).
2. THE Floating Sticky Bar SHALL remain fully visible and usable on viewports as narrow as 320px, with both CTAs accessible without horizontal scrolling.
3. THE service card grid SHALL collapse from 4 columns to 2 columns at 850px viewport width and to 1 column at 520px viewport width.
4. THE Social Proof stat counter grid SHALL collapse from a single row to a 2×2 grid at 520px viewport width.
5. THE Guarantee Strip ticker SHALL maintain continuous horizontal scrolling on all viewport widths down to 320px.
6. THE hero `<h1>` SHALL scale using `clamp(1.7rem, 7vw, 3.4rem)` so it remains legible without overflow on any viewport width.
7. THE Gallery Carousel container SHALL maintain its `aspect-ratio: 16 / 9` and fill the viewport width on mobile, with dots remaining tappable at a minimum touch target of 24×24px.
