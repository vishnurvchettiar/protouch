# Requirements Document

## Introduction

This feature delivers a cohesive, modern UI redesign for the ProTouch Physio - Slimming Clinic application. The redesign covers both the public-facing website (Home, ServiceDetail, ServiceBooking) and the authenticated management portal (AdminDashboard, ClientDashboard, NavMenu, Auth pages) under a unified design token system. Public pages adopt a clean, light aesthetic anchored by the ProTouch green brand colour (`#008d36`); dashboards retain a premium dark glassmorphism look elevated with the same green brand tokens. Tailwind CSS is introduced as the primary utility framework, supplementing the existing CSS custom-property token system and progressively replacing ad-hoc inline styles.

---

## Glossary

- **Design Token System**: A set of CSS custom properties (`--pt-*`) that define colours, spacing, typography scale, shadow levels and border-radius values shared across all themes.
- **Public Theme**: The light visual mode applied to pages using `PublicLayout.razor` — `Home.razor`, `ServiceDetail.razor`, `ServiceBooking.razor`.
- **Dashboard Theme**: The dark glassmorphism visual mode applied to pages using `MainLayout.razor` — `AdminDashboard.razor`, `ClientDashboard.razor`.
- **Auth Pages**: Identity scaffold pages under `Components/Account/Pages/` — Login, Register, ForgotPassword, ResetPassword, ConfirmEmail and their Manage sub-pages.
- **Brand Green**: The ProTouch primary brand colour `#008d36`.
- **Utility Layer**: Tailwind CSS utility classes applied via CDN or npm integration to all Razor component markup.
- **Glassmorphism Card**: A dark, semi-transparent card with a backdrop blur, subtle border and elevated shadow, used in the Dashboard Theme.
- **NavMenu**: The `NavMenu.razor` sidebar component rendered inside `MainLayout.razor` for authenticated users.
- **Micro-interaction**: A subtle CSS or JS-driven animation on hover, focus or state transition — e.g. button lift, link underline slide, input focus ring.
- **WCAG AA**: Web Content Accessibility Guidelines 2.1 Level AA contrast and interaction requirements.
- **ProTouch System**: The Blazor Server application (`ClinicApp`) hosted inside the `protouch/` repository.

---

## Requirements

### Requirement 1 — Design Token Foundation

**User Story:** As a developer, I want a single source-of-truth CSS custom-property token system, so that both the Public Theme and the Dashboard Theme share consistent brand values that can be updated in one place.

#### Acceptance Criteria

1. THE ProTouch System SHALL define a set of global CSS custom properties including `--pt-brand-green: #008d36`, `--pt-brand-green-hover: #007030`, `--pt-brand-green-light: #eef6f1`, `--pt-font-sans: 'Outfit', 'Manrope', sans-serif`, `--pt-radius-sm: 6px`, `--pt-radius-md: 12px`, `--pt-radius-lg: 20px`, `--pt-shadow-sm`, `--pt-shadow-md`, and `--pt-shadow-lg`.
2. THE ProTouch System SHALL define a `[data-theme="light"]` scope that sets surface, text, border and background tokens to light-mode values (`--pt-bg-page: #f8faf8`, `--pt-surface: #ffffff`, `--pt-text-primary: #1a1520`, `--pt-text-muted: #6b636d`).
3. THE ProTouch System SHALL define a `[data-theme="dark"]` scope that sets surface, text, border and background tokens to dark-mode values (`--pt-bg-page: #0b0c16`, `--pt-surface: rgba(255,255,255,0.04)`, `--pt-text-primary: #f8fafc`, `--pt-text-muted: #94a3b8`).
4. WHEN a page is rendered under `PublicLayout.razor`, THE ProTouch System SHALL apply `data-theme="light"` to the root layout element.
5. WHEN a page is rendered under `MainLayout.razor`, THE ProTouch System SHALL apply `data-theme="dark"` to the root layout element.
6. THE ProTouch System SHALL expose all design tokens as a CSS layer (`@layer tokens`) so that Tailwind CSS utility classes take precedence when applied explicitly.

---

### Requirement 2 — Tailwind CSS Integration

**User Story:** As a developer, I want Tailwind CSS available in the project, so that I can compose utility classes in Razor markup without writing bespoke CSS for every layout variation.

#### Acceptance Criteria

1. THE ProTouch System SHALL include Tailwind CSS via a `<script>` CDN tag in `App.razor` or via an npm/PostCSS build step so that all Tailwind utility classes are available globally.
2. WHEN Tailwind CSS is loaded, THE ProTouch System SHALL configure the `content` array (or safelist) to include all `.razor` and `.razor.css` file paths so that purging does not remove used classes.
3. THE ProTouch System SHALL extend the Tailwind theme configuration to map `colors.brand` to `#008d36`, `colors.brand-hover` to `#007030`, and `colors.brand-light` to `#eef6f1`.
4. IF a utility class conflicts with an existing Bootstrap or custom CSS rule, THEN THE ProTouch System SHALL resolve the conflict by scoping legacy rules under their original class names without removing them, preserving backward compatibility.
5. THE ProTouch System SHALL not remove or break any existing Bootstrap `lib/` classes that are currently used in `AdminDashboard.razor` and `ClientDashboard.razor`.

---

### Requirement 3 — Public Pages: Typography and Spacing

**User Story:** As a visitor, I want the public pages to feel clean, spacious and easy to read, so that I can quickly find the information I need and trust the clinic's brand.

#### Acceptance Criteria

1. THE ProTouch System SHALL apply `font-family: var(--pt-font-sans)` as the base body font on all pages using the Public Theme.
2. THE ProTouch System SHALL set a minimum body font size of `16px` and a line-height of `1.65` on all Public Theme pages.
3. THE ProTouch System SHALL apply a type scale where `<h1>` is at minimum `2.5rem`, `<h2>` at minimum `1.875rem`, `<h3>` at minimum `1.375rem`, and `<p>` at `1rem`, with responsive upscaling at the `md` Tailwind breakpoint.
4. THE ProTouch System SHALL apply consistent section vertical padding of `4rem` on mobile and `6rem` on screens wider than `768px` to all public page sections.
5. THE ProTouch System SHALL constrain all public page content to a maximum width of `1200px` and centre it horizontally within the viewport.

---

### Requirement 4 — Public Pages: Colour, Brand and Visual Identity

**User Story:** As a visitor, I want the public website to feel fresh, green-branded and professional, so that I immediately associate it with a trusted healthcare clinic.

#### Acceptance Criteria

1. THE ProTouch System SHALL use Brand Green (`#008d36`) as the primary action colour for all buttons, links and highlights on Public Theme pages.
2. WHEN a primary button on a Public Theme page is hovered, THE ProTouch System SHALL darken the background to `#007030` and elevate the button with a `translateY(-2px)` transform and an increase in box-shadow.
3. THE ProTouch System SHALL render the ProTouch logo in the public header at a minimum height of `40px` with `object-fit: contain`.
4. THE ProTouch System SHALL apply a sticky public header that reduces in height and gains a `backdrop-filter: blur(12px)` and a semi-transparent background when the user scrolls more than `60px` from the top of the page.
5. THE ProTouch System SHALL style all primary call-to-action sections (hero, booking banner, dark CTA) with a background gradient anchored to Brand Green, ensuring contrast ratio of at least 4.5:1 against overlaid text per WCAG AA.
6. THE ProTouch System SHALL display service cards in a responsive grid: 1 column on mobile, 2 columns at `sm` breakpoint, and 4 columns at `lg` breakpoint, each with a `var(--pt-radius-lg)` border-radius and a `var(--pt-shadow-md)` box-shadow.

---

### Requirement 5 — Public Pages: Interaction and Animation

**User Story:** As a visitor, I want subtle, smooth animations as I scroll and interact, so that the website feels polished and engaging without being distracting.

#### Acceptance Criteria

1. WHEN a page section with the `data-reveal` attribute enters the viewport, THE ProTouch System SHALL animate it from `opacity: 0; translateY(24px)` to `opacity: 1; translateY(0)` over `480ms` with an `ease-out` timing function using an `IntersectionObserver`.
2. WHEN a user hovers over a service card on the Home or ServiceDetail page, THE ProTouch System SHALL lift the card by `translateY(-6px)` and increase its `box-shadow` over a `200ms` transition.
3. WHEN a user hovers over a navigation link in the public header, THE ProTouch System SHALL animate a Brand Green underline from `width: 0` to `width: 100%` over `220ms`.
4. THE ProTouch System SHALL wrap all scroll-driven and hover animations in a `@media (prefers-reduced-motion: reduce)` override that disables transforms and transitions for users who opt out.
5. WHEN the floating CTA buttons (WhatsApp, Book) are rendered, THE ProTouch System SHALL apply a `bounce-in` entrance animation on first render and a `box-shadow` pulse on hover.

---

### Requirement 6 — ServiceBooking Page

**User Story:** As a visitor, I want the booking form to feel trustworthy and easy to complete on any device, so that I can request an appointment without friction.

#### Acceptance Criteria

1. THE ProTouch System SHALL render the service-selection cards in a 2-column grid at `sm` breakpoint and a single column on mobile, with the selected card receiving a Brand Green border and a `var(--pt-brand-green-light)` background tint.
2. WHEN a form field on the ServiceBooking page is focused, THE ProTouch System SHALL display a `2px` Brand Green `outline` ring with a `4px` offset and a `var(--pt-brand-green-light)` inner glow.
3. WHEN a validation error exists for a form field, THE ProTouch System SHALL apply a red border (`#ef4444`) to the input and render the error text in `#ef4444` at `0.875rem` immediately below the field.
4. THE ProTouch System SHALL render the booking success panel with a Brand Green checkmark icon, a `font-size: 1.875rem` heading, and a smooth `fade-in` entrance at `300ms`.
5. THE ProTouch System SHALL ensure the "Request Appointment →" button spans full width on mobile and is at least `44px` in height on all viewports per WCAG touch-target guidelines.

---

### Requirement 7 — ServiceDetail Page

**User Story:** As a visitor exploring a service, I want a visually rich, informative page that builds confidence in the clinic's expertise, so that I am motivated to book.

#### Acceptance Criteria

1. THE ProTouch System SHALL render the service hero banner at a minimum height of `420px` with the gradient background from the service data, the hero image positioned at the right on `lg` screens and hidden on mobile.
2. THE ProTouch System SHALL render key-stat cards in a 2-column grid on mobile and a 4-column row on `lg` screens, each with a Brand Green accent border-top of `3px`.
3. THE ProTouch System SHALL render treatment cards in a glassmorphism style (`background: rgba(255,255,255,0.06)`, `backdrop-filter: blur(8px)`, `border: 1px solid rgba(255,255,255,0.1)`) on the dark-gradient treatments band section.
4. THE ProTouch System SHALL render the sub-service pills as rounded badge buttons with a `var(--pt-radius-lg)` border-radius; WHEN a pill is selected, THE ProTouch System SHALL fill it with Brand Green and change text to white.
5. THE ProTouch System SHALL render the inline booking section with a dark green gradient background (`linear-gradient(135deg, #1a4d2e, #2d7a44)`), white text, and the booking form fields in dark semi-transparent inputs.

---

### Requirement 8 — Dashboard Layout: MainLayout and NavMenu

**User Story:** As an authenticated Admin or Client, I want a sleek, intuitive sidebar navigation and topbar, so that I can move between sections efficiently on any screen size.

#### Acceptance Criteria

1. THE ProTouch System SHALL render the `NavMenu` sidebar at a fixed width of `220px` on `lg` screens, collapsing to a `64px` icon-only rail at `md` screens, and to a slide-in drawer on mobile.
2. WHEN a nav item in `NavMenu` is the active route, THE ProTouch System SHALL apply a Brand Green left border of `3px`, a `var(--pt-brand-green-light)` background tint at `8% opacity`, and white text.
3. THE ProTouch System SHALL render the topbar at a fixed height of `56px` with `--pt-bg-page` background, a 1px bottom border in `rgba(255,255,255,0.08)`, and the ProTouch logo mark at `20px` on the left.
4. WHEN the viewport width is below `768px`, THE ProTouch System SHALL render a hamburger menu button in the topbar that toggles the mobile nav drawer.
5. THE ProTouch System SHALL apply a smooth `200ms` transition to the sidebar collapse and drawer open/close animations.

---

### Requirement 9 — AdminDashboard: Overview and Stat Cards

**User Story:** As an Admin, I want the overview tab to present key metrics clearly and attractively, so that I can assess the clinic's status at a glance.

#### Acceptance Criteria

1. THE ProTouch System SHALL render the five stat cards in a CSS Grid with `repeat(auto-fill, minmax(200px, 1fr))` column sizing so they reflow gracefully at all screen widths.
2. WHEN a stat card is hovered, THE ProTouch System SHALL lift it by `translateY(-4px)` and brighten the accent icon background over a `180ms` ease transition.
3. THE ProTouch System SHALL render the "Today's Appointments" strip chips with a `var(--pt-radius-md)` border-radius, a Brand Green left accent line, and a text size of `0.875rem`.
4. THE ProTouch System SHALL render the "Recent Activity" feed items with a coloured dot indicator (`visit` = Brand Green, `booking` = `#3b82f6`) and a `border-bottom: 1px solid rgba(255,255,255,0.05)` separator.
5. THE ProTouch System SHALL render the quick-action buttons row with icon + label, a minimum width of `140px`, a `var(--pt-radius-md)` border-radius, and a Brand Green fill on hover within `160ms`.

---

### Requirement 10 — AdminDashboard: Tab Content Panels

**User Story:** As an Admin, I want every tab panel to have consistent card styling and legible data tables, so that I can manage clients, visits and bookings without visual clutter.

#### Acceptance Criteria

1. THE ProTouch System SHALL render all `glass-panel` cards across AdminDashboard with `background: var(--pt-surface)`, `border: 1px solid rgba(255,255,255,0.07)`, `border-radius: var(--pt-radius-lg)`, and `box-shadow: var(--pt-shadow-md)`.
2. THE ProTouch System SHALL render the clinic data tables (`clinic-table`) with alternating row backgrounds (`rgba(255,255,255,0.02)` and transparent), a sticky `<thead>` with `--pt-bg-page` background, and a bottom border on each row.
3. WHEN a table row is hovered, THE ProTouch System SHALL apply a `rgba(0,141,54,0.06)` row highlight over a `120ms` transition.
4. THE ProTouch System SHALL render form inputs (`clinic-input`) with a `border: 1px solid rgba(255,255,255,0.12)`, `border-radius: var(--pt-radius-sm)`, and a Brand Green `outline` on focus with `box-shadow: 0 0 0 3px rgba(0,141,54,0.25)`.
5. THE ProTouch System SHALL render alert banners (`clinic-alert-danger`, `clinic-alert-info`) with a left accent border of `4px`, matching icon colour, `var(--pt-radius-md)` border-radius, and `padding: 12px 16px`.

---

### Requirement 11 — ClientDashboard: Hero Section and Progress Ring

**User Story:** As a Client, I want the top of my dashboard to immediately show my progress and next appointment in a visually engaging way, so that I feel motivated and informed.

#### Acceptance Criteria

1. THE ProTouch System SHALL render the `pt-client-hero` section in a two-column layout on `lg` screens (progress ring + greeting left, appointment card + diet preview right), stacking to single column on mobile.
2. THE ProTouch System SHALL style the SVG progress ring with a `8px` stroke, a grey track (`rgba(255,255,255,0.08)`) and a Brand Green arc, and apply a `stroke-dashoffset` CSS transition of `800ms ease-out` on first render.
3. THE ProTouch System SHALL render the "next appointment" card and "diet plan preview" card with `var(--pt-surface)` background, `var(--pt-radius-md)` border-radius, and a `2px` Brand Green top border.
4. WHEN the client has not logged weight today, THE ProTouch System SHALL render the daily check-in prompt with an amber left border (`#f59e0b`) and amber icon, distinct from the Brand Green success state.
5. THE ProTouch System SHALL render the three stat pills in a horizontal flex row with `gap: 12px`, each pill having `background: rgba(0,141,54,0.1)`, `border-radius: var(--pt-radius-sm)`, `padding: 8px 14px`, and white text.

---

### Requirement 12 — ClientDashboard: Weight Chart and Daily Log

**User Story:** As a Client, I want the weight chart and daily log form to be clear and easy to use, so that I can track and submit my progress without confusion.

#### Acceptance Criteria

1. THE ProTouch System SHALL render the SVG weight-trend chart within a container that maintains a `16:9` aspect ratio on `lg` screens and reduces to `4:3` on mobile, using `viewBox` scaling.
2. THE ProTouch System SHALL render chart data points as `<circle r="5">` elements with a white fill and Brand Green stroke of `2px`, displaying the weight label above each point in `font-size: 10px`.
3. THE ProTouch System SHALL render the macro sliders within a visually distinct panel with a `1px rgba(255,255,255,0.08)` border and a `var(--pt-radius-md)` border-radius, with each slider thumb styled in Brand Green.
4. WHEN the macro percentages do not sum to 100%, THE ProTouch System SHALL render an amber warning bar above the submit button, replacing any previous success message.
5. THE ProTouch System SHALL render meal photo upload and preview within a dashed-border upload zone (`border: 2px dashed rgba(255,255,255,0.2)`, `border-radius: var(--pt-radius-md)`); WHEN a preview image is available, THE ProTouch System SHALL display it at a maximum height of `200px` with `object-fit: cover` and a `var(--pt-radius-sm)` border-radius.

---

### Requirement 13 — Auth Pages

**User Story:** As a new or returning user, I want the login and registration pages to match the ProTouch brand and feel polished, so that the onboarding experience feels consistent with the rest of the application.

#### Acceptance Criteria

1. THE ProTouch System SHALL render Auth pages with the Public Theme (`data-theme="light"`) and a centred single-column card layout with a maximum width of `440px`.
2. THE ProTouch System SHALL render the ProTouch logo above the auth form card at a height of `48px`.
3. THE ProTouch System SHALL style the primary submit button on each Auth page with Brand Green fill, white text, full-width layout, and the hover lift micro-interaction defined in Requirement 4.
4. WHEN an Identity validation error is displayed on an Auth page, THE ProTouch System SHALL render it in a `clinic-alert-danger`-styled panel consistent with Requirement 10 Acceptance Criterion 5.
5. THE ProTouch System SHALL render form inputs on Auth pages with the same focus-ring style defined in Requirement 10 Acceptance Criterion 4, so that the input styling is visually consistent across all pages.

---

### Requirement 14 — Responsive Layout

**User Story:** As a user on any device, I want the entire application to be usable on screens from 375px wide to 1920px wide without horizontal scroll or overlapping elements, so that I can access clinic services and manage my health from my phone, tablet or desktop.

#### Acceptance Criteria

1. THE ProTouch System SHALL apply responsive breakpoints that match the Tailwind defaults: `sm: 640px`, `md: 768px`, `lg: 1024px`, `xl: 1280px`, and `2xl: 1536px`.
2. WHEN the viewport width is below `640px`, THE ProTouch System SHALL render single-column layouts for all grid sections on public pages and dashboard panels.
3. THE ProTouch System SHALL ensure all interactive touch targets (buttons, nav items, tab buttons) have a minimum tap size of `44px × 44px` on mobile viewports.
4. THE ProTouch System SHALL ensure no horizontal scrollbar appears on any page at viewport widths of `375px`, `768px`, `1024px`, or `1440px`.
5. WHEN images are rendered in gallery grids or service cards, THE ProTouch System SHALL apply `object-fit: cover` and explicit `aspect-ratio` values to prevent layout shift during image load.

---

### Requirement 15 — Accessibility

**User Story:** As a user with a visual or motor impairment, I want the application to be navigable by keyboard and screen reader, so that I can access clinic services independently.

#### Acceptance Criteria

1. THE ProTouch System SHALL ensure all interactive elements (buttons, links, inputs, nav items) have a visible focus indicator with a minimum contrast ratio of 3:1 against adjacent surfaces per WCAG AA.
2. THE ProTouch System SHALL ensure all non-decorative images include descriptive `alt` text; decorative images SHALL use `alt=""` and `aria-hidden="true"`.
3. THE ProTouch System SHALL ensure all form inputs have an associated `<label>` element linked by `for`/`id` or `aria-label`.
4. WHEN modal-style overlays (gallery lightbox, diet plan panel, delete confirmation) are open, THE ProTouch System SHALL trap keyboard focus within the overlay and restore focus to the triggering element on close.
5. THE ProTouch System SHALL maintain a logical heading hierarchy (`h1 → h2 → h3`) on every page with no skipped heading levels.
6. THE ProTouch System SHALL ensure all colour combinations used for text on backgrounds achieve at minimum 4.5:1 contrast ratio per WCAG AA, measured against `--pt-text-primary` on `--pt-bg-page` for both light and dark themes.
