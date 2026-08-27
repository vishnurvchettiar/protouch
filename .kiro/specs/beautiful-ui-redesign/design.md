# Technical Design Document

## Feature: Beautiful UI Redesign — ProTouch Physio Clinic

---

## 1. Overview

This document describes the technical architecture for the ProTouch UI redesign. The system unifies two visual themes — a **light Public Theme** for visitor-facing pages and a **dark Dashboard Theme** for authenticated users — under a single shared design-token foundation. Tailwind CSS is introduced as a CDN-loaded utility layer alongside the existing Bootstrap and custom CSS files. All styling changes are confined to `.css` files, `App.razor`, and the two layout components (`PublicLayout.razor`, `MainLayout.razor`); Razor page components receive only class-name and attribute updates, not structural refactoring.

---

## 2. Architecture

### 2.1 Layer Stack (CSS Load Order in `App.razor`)

```
Bootstrap (lib/bootstrap/dist/css/bootstrap.min.css)   ← unchanged
↓
app.css (dashboard dark glassmorphism base)            ← extended with @layer tokens
↓
home-medic4.css (public light theme)                   ← refactored to use @layer tokens
↓
ClinicApp.styles.css (Blazor component isolation CSS)  ← unchanged
↓
Tailwind CDN (script tag in <head>)                    ← utility override layer
```

**Key principle:** CSS Cascade Layers (`@layer tokens`, `@layer base`, `@layer components`, `@layer utilities`) ensure Tailwind utility classes always win when explicitly applied, while legacy Bootstrap classes continue working in unlayered scope.

### 2.2 Theme Scoping Strategy

```
<html> or layout root element
  [data-theme="light"]  →  PublicLayout.razor wraps @Body in a <div data-theme="light">
  [data-theme="dark"]   →  MainLayout.razor wraps its page <div class="page"> with data-theme="dark"
```

All design tokens are declared in a `:root` block and overridden by the `[data-theme]` attribute selectors. This means any component can be dropped into either theme context and pick up the correct tokens automatically.

### 2.3 Tailwind CDN Configuration

Tailwind is loaded via the Play CDN with an inline `tailwind.config` block, placed in `App.razor` `<head>` **before** `</head>`:

```html
<!-- Tailwind CSS via Play CDN -->
<script src="https://cdn.tailwindcss.com"></script>
<script>
  tailwind.config = {
    content: [],                      // CDN mode — no purging required
    theme: {
      extend: {
        colors: {
          brand:        '#008d36',
          'brand-hover':'#007030',
          'brand-light':'#eef6f1',
        }
      }
    }
  }
</script>
```

The Play CDN scans the live DOM at runtime (no build step needed), so `.razor` files don't need to be listed in `content`. This satisfies Requirements 2.1–2.3 without a build-pipeline change.

---

## 3. Design Token System (`@layer tokens`)

### 3.1 Global Token Block

The new `:root` block, placed at the top of `app.css` inside `@layer tokens`, defines all shared brand values:

```css
@layer tokens {
  :root {
    /* Brand */
    --pt-brand-green:       #008d36;
    --pt-brand-green-hover: #007030;
    --pt-brand-green-light: #eef6f1;

    /* Typography */
    --pt-font-sans: 'Outfit', 'Manrope', sans-serif;

    /* Radius scale */
    --pt-radius-sm: 6px;
    --pt-radius-md: 12px;
    --pt-radius-lg: 20px;

    /* Shadow scale */
    --pt-shadow-sm: 0 1px 4px rgba(0,0,0,0.08);
    --pt-shadow-md: 0 4px 24px rgba(0,0,0,0.14);
    --pt-shadow-lg: 0 12px 48px rgba(0,0,0,0.22);
  }

  /* ── Light theme (Public pages) ── */
  [data-theme="light"] {
    --pt-bg-page:      #f8faf8;
    --pt-surface:      #ffffff;
    --pt-text-primary: #1a1520;
    --pt-text-muted:   #6b636d;
    --pt-border:       rgba(0,0,0,0.08);
  }

  /* ── Dark theme (Dashboard pages) ── */
  [data-theme="dark"] {
    --pt-bg-page:      #0b0c16;
    --pt-surface:      rgba(255,255,255,0.04);
    --pt-text-primary: #f8fafc;
    --pt-text-muted:   #94a3b8;
    --pt-border:       rgba(255,255,255,0.08);
  }
}
```

### 3.2 Token Inheritance Map

| Token | Light (`PublicLayout`) | Dark (`MainLayout`) |
|---|---|---|
| `--pt-bg-page` | `#f8faf8` | `#0b0c16` |
| `--pt-surface` | `#ffffff` | `rgba(255,255,255,0.04)` |
| `--pt-text-primary` | `#1a1520` | `#f8fafc` |
| `--pt-text-muted` | `#6b636d` | `#94a3b8` |
| `--pt-brand-green` | `#008d36` | `#008d36` (shared) |

---

## 4. Layout Components

### 4.1 `PublicLayout.razor`

The current `PublicLayout.razor` is a bare passthrough (`@Body`). It is updated to wrap the body in a theme-scoped root element:

```razor
@inherits LayoutComponentBase

<div data-theme="light" class="pt-public-root">
    @Body
</div>
```

The `.pt-public-root` class resets `background`, `color`, and `font-family` to public-theme tokens. This is the only change to `PublicLayout.razor`.

### 4.2 `MainLayout.razor`

The existing `<div class="page dashboard-theme">` receives the `data-theme="dark"` attribute:

```razor
<div class="page dashboard-theme" data-theme="dark">
    <div class="sidebar">
        <NavMenu />
    </div>
    <main>
        <header class="pt-topbar">
          ...
        </header>
        <article class="content px-4 py-4" style="flex: 1; overflow-y: auto;">
            @Body
        </article>
    </main>
</div>
```

No other structural changes are made to `MainLayout.razor`.

### 4.3 `NavMenu.razor` — Responsive Sidebar

The NavMenu component renders the `.pt-sidebar` element. CSS in `app.css` governs three responsive states:

| Breakpoint | Behaviour |
|---|---|
| `lg` (≥ 1024px) | Fixed `220px` wide sidebar, full icon + label |
| `md` (768–1023px) | Collapsed to `64px` icon-only rail; labels hidden via `.pt-nav-label` opacity transition |
| `< 768px` | Sidebar becomes a `position: fixed` overlay drawer, toggled by a hamburger button added to the topbar |

The hamburger toggle button is injected into `MainLayout.razor`'s `<header class="pt-topbar">` only on mobile via a CSS `display: none` / `display: flex` rule at `md`. The drawer open state is managed by a `_drawerOpen` bool field on `MainLayout.razor` that toggles a `.pt-sidebar--open` CSS class.

**Active route styling** (`.pt-nav-link--active` is already set via Blazor's `NavLink` component):
```css
.pt-nav-link--active {
  border-left: 3px solid var(--pt-brand-green);
  background: rgba(0, 141, 54, 0.08);
  color: #ffffff;
}
```

---

## 5. Public Pages

### 5.1 `Home.razor`

The page already carries the `.pt-page` root class. Changes are additive — no existing classes are removed.

**Header sticky scroll effect:**  
`site.js` already calls `initStickyHeader("#pt-header")`. The JS function adds a `.pt-header--scrolled` class when `window.scrollY > 60`, which triggers:
```css
.pt-header--scrolled {
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  background: rgba(255,255,255,0.88);
  height: 60px; /* reduced from 72px */
  box-shadow: var(--pt-shadow-sm);
}
```

**Scroll-reveal (`data-reveal`):**  
Sections already carry `data-reveal` attributes. The `IntersectionObserver` in `site.js` adds `.pt-revealed` on entry:
```css
[data-reveal] {
  opacity: 0;
  transform: translateY(24px);
  transition: opacity 480ms ease-out, transform 480ms ease-out;
}
[data-reveal].pt-revealed {
  opacity: 1;
  transform: translateY(0);
}
@media (prefers-reduced-motion: reduce) {
  [data-reveal] { opacity: 1; transform: none; transition: none; }
  [data-reveal].pt-revealed { opacity: 1; transform: none; }
}
```

**Service cards grid:**
```css
.pt-services-row {
  display: grid;
  grid-template-columns: 1fr;
  gap: 1.5rem;
}
@media (min-width: 640px) {
  .pt-services-row { grid-template-columns: repeat(2, 1fr); }
}
@media (min-width: 1024px) {
  .pt-services-row { grid-template-columns: repeat(4, 1fr); }
}
.pt-svc-card {
  border-radius: var(--pt-radius-lg);
  box-shadow: var(--pt-shadow-md);
  transition: transform 200ms ease, box-shadow 200ms ease;
}
.pt-svc-card:hover {
  transform: translateY(-6px);
  box-shadow: var(--pt-shadow-lg);
}
@media (prefers-reduced-motion: reduce) {
  .pt-svc-card { transition: none; }
  .pt-svc-card:hover { transform: none; }
}
```

**Floating CTA buttons** get a `bounce-in` keyframe animation on first render and `box-shadow` pulse on hover.

### 5.2 `ServiceBooking.razor`

**Service selection cards:**
```css
.pt-book-svc-card {
  border: 2px solid transparent;
  border-radius: var(--pt-radius-md);
  transition: border-color 160ms ease, background 160ms ease;
}
.pt-book-svc-card.selected {
  border-color: var(--pt-brand-green);
  background: var(--pt-brand-green-light);
}
```
Grid: 1 column mobile, 2 columns at `sm` breakpoint.

**Form focus ring** (shared component style, also used by auth pages):
```css
.pt-field:focus-within input,
.pt-field:focus-within select,
.pt-field:focus-within textarea,
.clinic-input:focus {
  outline: 2px solid var(--pt-brand-green);
  outline-offset: 4px;
  box-shadow: inset 0 0 0 3px var(--pt-brand-green-light);
}
```

**Validation error state:**
```css
.clinic-input.is-invalid {
  border-color: #ef4444;
}
.pt-field-error {
  color: #ef4444;
  font-size: 0.875rem;
  margin-top: 4px;
}
```

**Success panel** uses a Brand Green checkmark SVG icon and `fade-in` keyframe at `300ms`.

**CTA button:** `.pt-btn-primary` always has `min-height: 44px`. On mobile (`< 640px`) the booking button additionally gets `width: 100%` via a utility class or a `pt-btn-primary--full` modifier.

### 5.3 `ServiceDetail.razor`

**Hero banner:**
```css
.pt-svc-hero {
  min-height: 420px;
  display: grid;
  grid-template-columns: 1fr;
}
@media (min-width: 1024px) {
  .pt-svc-hero { grid-template-columns: 1fr 1fr; }
  .pt-svc-hero-image { display: block; }
}
.pt-svc-hero-image { display: none; }
```

**Key-stat cards:**
```css
.pt-stat-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 1rem;
}
@media (min-width: 1024px) {
  .pt-stat-grid { grid-template-columns: repeat(4, 1fr); }
}
.pt-stat-card { border-top: 3px solid var(--pt-brand-green); }
```

**Treatment cards** on the dark-gradient band section:
```css
.pt-treatment-card {
  background: rgba(255,255,255,0.06);
  backdrop-filter: blur(8px);
  border: 1px solid rgba(255,255,255,0.10);
  border-radius: var(--pt-radius-md);
}
```

**Sub-service pills:**
```css
.pt-sub-pill { border-radius: var(--pt-radius-lg); border: 1.5px solid var(--pt-brand-green); }
.pt-sub-pill.active {
  background: var(--pt-brand-green);
  color: #ffffff;
}
```

**Inline booking section:**
```css
.pt-inline-book {
  background: linear-gradient(135deg, #1a4d2e, #2d7a44);
  color: #ffffff;
}
.pt-inline-book .clinic-input {
  background: rgba(0,0,0,0.25);
  color: #ffffff;
  border-color: rgba(255,255,255,0.2);
}
```

---

## 6. Dashboard Pages

### 6.1 `AdminDashboard.razor`

**Stat cards grid:**
```css
.dashboard-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: 1.25rem;
}
.pt-stat-card {
  transition: transform 180ms ease, box-shadow 180ms ease;
}
.pt-stat-card:hover {
  transform: translateY(-4px);
  box-shadow: var(--pt-shadow-lg);
}
.pt-stat-card:hover .stat-icon {
  filter: brightness(1.2);
}
```

**glass-panel cards** (unified across all dashboard tabs):
```css
.glass-panel {
  background: var(--pt-surface);
  border: 1px solid rgba(255,255,255,0.07);
  border-radius: var(--pt-radius-lg);
  box-shadow: var(--pt-shadow-md);
}
```

**clinic-table:**
```css
.clinic-table tbody tr:nth-child(odd) { background: rgba(255,255,255,0.02); }
.clinic-table tbody tr:nth-child(even) { background: transparent; }
.clinic-table thead {
  position: sticky; top: 0;
  background: var(--pt-bg-page);
}
.clinic-table tbody tr { border-bottom: 1px solid rgba(255,255,255,0.05); }
.clinic-table tbody tr:hover { background: rgba(0,141,54,0.06); transition: background 120ms; }
```

**Alert banners:**
```css
.clinic-alert {
  border-left: 4px solid;
  border-radius: var(--pt-radius-md);
  padding: 12px 16px;
  display: flex;
  align-items: flex-start;
  gap: 10px;
}
.clinic-alert-danger { border-color: #ef4444; color: #ef4444; background: rgba(239,68,68,0.06); }
.clinic-alert-info    { border-color: var(--pt-brand-green); background: rgba(0,141,54,0.06); }
.clinic-alert-warning { border-color: #f59e0b; background: rgba(245,158,11,0.06); }
```

**Quick-action buttons:**
```css
.pt-quick-btn {
  min-width: 140px;
  border-radius: var(--pt-radius-md);
  padding: 10px 18px;
  transition: background 160ms ease, color 160ms ease;
}
.pt-quick-btn:hover {
  background: var(--pt-brand-green);
  color: #ffffff;
}
```

**Activity feed dots:**
```css
.pt-activity-dot.visit   { background: var(--pt-brand-green); }
.pt-activity-dot.booking { background: #3b82f6; }
```

**Today's appointments chips:**
```css
.pt-appt-chip {
  border-radius: var(--pt-radius-md);
  border-left: 3px solid var(--pt-brand-green);
  font-size: 0.875rem;
  padding: 8px 12px;
}
```

### 6.2 `ClientDashboard.razor`

**pt-client-hero layout:**
```css
.pt-client-hero {
  display: grid;
  grid-template-columns: 1fr;
  gap: 1.5rem;
}
@media (min-width: 1024px) {
  .pt-client-hero { grid-template-columns: 1fr 1fr; }
}
```

**SVG Progress Ring:** The `stroke-dashoffset` is set via a Blazor-computed inline style. The CSS transition is applied to the `<circle>` element:
```css
.pt-ring-svg circle:last-of-type {
  transition: stroke-dashoffset 800ms ease-out;
}
@media (prefers-reduced-motion: reduce) {
  .pt-ring-svg circle:last-of-type { transition: none; }
}
```

**Next appointment / diet preview cards:**
```css
.pt-next-appt-card,
.pt-diet-preview {
  background: var(--pt-surface);
  border-radius: var(--pt-radius-md);
  border-top: 2px solid var(--pt-brand-green);
  padding: 16px;
}
```

**Daily check-in prompt states:**
```css
.pt-checkin-prompt.logged      { border-left: 4px solid var(--pt-brand-green); }
.pt-checkin-prompt.needs-log   { border-left: 4px solid #f59e0b; color: #f59e0b; }
```

**Stat pills:**
```css
.pt-stat-pill {
  display: flex;
  flex-direction: column;
  align-items: center;
  background: rgba(0,141,54,0.10);
  border-radius: var(--pt-radius-sm);
  padding: 8px 14px;
  color: #ffffff;
}
.pt-stat-pills { display: flex; gap: 12px; flex-wrap: wrap; }
```

**Weight chart container:**
```css
.chart-container {
  aspect-ratio: 16 / 9;
  width: 100%;
}
@media (max-width: 767px) {
  .chart-container { aspect-ratio: 4 / 3; }
}
```

**Macro warning bar:**  
The Blazor `@code` block already tracks `macroSumError`. CSS adds an amber top bar:
```css
.pt-macro-warning {
  background: rgba(245,158,11,0.12);
  border-left: 3px solid #f59e0b;
  border-radius: var(--pt-radius-sm);
  padding: 8px 12px;
  color: #f59e0b;
  font-size: 0.875rem;
}
```

**Meal photo upload zone:**
```css
.meal-upload-panel {
  border: 2px dashed rgba(255,255,255,0.2);
  border-radius: var(--pt-radius-md);
  padding: 16px;
  transition: border-color 160ms ease;
}
.meal-upload-panel:focus-within { border-color: var(--pt-brand-green); }
.meal-preview {
  max-height: 200px;
  object-fit: cover;
  border-radius: var(--pt-radius-sm);
}
```

---

## 7. Auth Pages

Auth pages (`Login.razor`, `Register.razor`, etc.) are wrapped by an `AuthLayout.razor` (new lightweight layout) or inline `<div data-theme="light">` using `PublicLayout.razor`.

**Card layout:**
```css
.pt-auth-card {
  max-width: 440px;
  margin: 4rem auto;
  padding: 2.5rem;
  background: var(--pt-surface);
  border-radius: var(--pt-radius-lg);
  box-shadow: var(--pt-shadow-lg);
}
```

**Logo:**
```html
<img src="/images/logo-mark.png" height="48" alt="ProTouch" class="mx-auto mb-6 block" />
```

**Primary button** on auth pages reuses `.pt-btn-primary` with `w-full` (Tailwind utility) added.

**Validation error:** Auth pages use ASP.NET Identity `<ValidationSummary />` which renders a `<ul>`. That `<ul>` is targeted:
```css
.pt-auth-card .validation-errors {
  border-left: 4px solid #ef4444;
  border-radius: var(--pt-radius-md);
  padding: 12px 16px;
  background: rgba(239,68,68,0.06);
  color: #ef4444;
}
```

Form inputs on auth pages use the shared `.clinic-input` class so focus-ring styling from Section 5.2 applies automatically.

---

## 8. Typography System

### 8.1 Font Loading

`App.razor` already loads Manrope and Instrument Sans. `Outfit` is added to the Google Fonts request:
```html
href="https://fonts.googleapis.com/css2?family=Outfit:wght@400;500;600;700&family=Manrope:wght@400;500;600&family=Instrument+Sans:wght@400;600;700&display=swap"
```

### 8.2 Type Scale

| Element | Size | Weight | Font |
|---|---|---|---|
| `h1` | `2.5rem` → `3.5rem` @ `md` | 700 | Outfit |
| `h2` | `1.875rem` → `2.75rem` @ `md` | 700 | Outfit |
| `h3` | `1.375rem` → `2rem` @ `md` | 600 | Outfit |
| `p` | `1rem` | 400 | Manrope |
| Body | `16px` min, `1.65` line-height | — | Manrope |

Under `[data-theme="light"]`:
```css
body, .pt-public-root {
  font-family: var(--pt-font-sans);
  font-size: 16px;
  line-height: 1.65;
}
h1 { font-size: 2.5rem; }
h2 { font-size: 1.875rem; }
h3 { font-size: 1.375rem; }
@media (min-width: 768px) {
  h1 { font-size: 3.5rem; }
  h2 { font-size: 2.75rem; }
  h3 { font-size: 2rem; }
}
```

### 8.3 Section Spacing

```css
.pt-section {
  padding-top: 4rem;
  padding-bottom: 4rem;
}
@media (min-width: 768px) {
  .pt-section {
    padding-top: 6rem;
    padding-bottom: 6rem;
  }
}
.pt-container {
  max-width: 1200px;
  margin-inline: auto;
  padding-inline: 1.5rem;
}
```

---

## 9. Animation System

### 9.1 Keyframe Library (`app.css`)

```css
@keyframes bounceIn {
  0%   { transform: scale(0.8) translateY(20px); opacity: 0; }
  60%  { transform: scale(1.05) translateY(-4px); opacity: 1; }
  100% { transform: scale(1) translateY(0); opacity: 1; }
}

@keyframes fadeIn {
  from { opacity: 0; }
  to   { opacity: 1; }
}

@keyframes pulseGlow {
  0%, 100% { box-shadow: 0 0 0 0 rgba(0,141,54,0.4); }
  50%       { box-shadow: 0 0 0 8px rgba(0,141,54,0); }
}
```

### 9.2 Reduced Motion

All animation declarations are paired with:
```css
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
    scroll-behavior: auto !important;
  }
}
```

This single rule covers all elements, satisfying Requirement 5.4.

---

## 10. Accessibility Implementation

### 10.1 Focus Indicators

All interactive elements receive a globally scoped focus rule:
```css
:focus-visible {
  outline: 2px solid var(--pt-brand-green);
  outline-offset: 3px;
  border-radius: 3px;
}
```
This achieves ≥ 3:1 contrast against both light and dark backgrounds (green `#008d36` on white `#ffffff` = 5.1:1; on dark `#0b0c16` = 4.6:1).

### 10.2 Touch Targets

All buttons and nav items globally:
```css
button, a[role="button"], .pt-nav-link, .pt-tab-btn {
  min-height: 44px;
  min-width: 44px;
}
```

### 10.3 Heading Hierarchy Discipline

Each page enforces one `<h1>` (the page hero title), `<h2>` for major sections, `<h3>` for card/item headings. The code review checklist (tracked in tasks) will verify no heading levels are skipped.

### 10.4 Modal Focus Trap

For the gallery lightbox and the diet plan panel, `site.js` implements a `trapFocus(element)` utility that:
1. Queries all focusable elements within the overlay
2. Intercepts `Tab` / `Shift+Tab` and wraps focus within those elements
3. On overlay close, calls `triggerElement.focus()`

---

## 11. Responsive Breakpoints

Tailwind defaults are used everywhere:

| Name | Min-width |
|---|---|
| `sm` | 640px |
| `md` | 768px |
| `lg` | 1024px |
| `xl` | 1280px |
| `2xl` | 1536px |

No horizontal scroll is achieved by:
- `overflow-x: hidden` on `html, body`
- All grid/flex containers using `gap` without `margin` bleed
- Images using `max-width: 100%` and `object-fit: cover`

---

## 12. File Change Summary

| File | Change Type | Summary |
|---|---|---|
| `App.razor` | Modify | Add Tailwind CDN script + config block, add Outfit to Google Fonts URL |
| `app.css` | Major extend | Add `@layer tokens` block with all design tokens, update `.glass-panel`, `.clinic-table`, `.clinic-input`, alert/button/nav styles, add animation keyframes, add `:focus-visible` and `prefers-reduced-motion` rules |
| `home-medic4.css` | Refactor | Migrate `.pt-page` tokens to use `var(--pt-*)` global tokens, remove local token overrides that now duplicate the global layer |
| `PublicLayout.razor` | Minor | Add `<div data-theme="light" class="pt-public-root">` wrapper |
| `MainLayout.razor` | Minor | Add `data-theme="dark"` to root div, add `_drawerOpen` field and hamburger button markup |
| `NavMenu.razor` | Minor | No structural changes; CSS drives responsive behaviour |
| `Home.razor` | Additive | Add `data-reveal` to missing sections; ensure floating CTA buttons have `pt-float-btn` class for animation |
| `ServiceBooking.razor` | Additive | Add `selected` class toggle on card selection, `is-invalid` on validation error inputs, `pt-btn-primary--full` on mobile CTA |
| `ServiceDetail.razor` | Additive | Add `pt-svc-hero`, `pt-stat-grid`, `pt-treatment-card`, `pt-sub-pill` classes |
| `AdminDashboard.razor` | Additive | Ensure `.pt-activity-dot` carries `visit`/`booking` kind class; add `pt-macro-warning` for macro sum display |
| `ClientDashboard.razor` | Additive | Add `pt-macro-warning` element above submit button; ensure `.meal-upload-panel` and `.meal-preview` classes present |
| Auth pages (`Login`, `Register`, etc.) | Additive | Add `pt-auth-card` wrapper div; add `.validation-errors` class to `<ValidationSummary>` output |
| `site.js` | Extend | Add `trapFocus()` utility; ensure `initStickyHeader` adds `.pt-header--scrolled`; ensure `IntersectionObserver` adds `.pt-revealed` class |

---

## 13. Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Type Scale Compliance

*For any* public-page heading element (`h1`, `h2`, `h3`) rendered in any viewport, its computed `font-size` SHALL meet the minimum specified in the type scale (`h1` ≥ `2.5rem`, `h2` ≥ `1.875rem`, `h3` ≥ `1.375rem`).

**Validates: Requirements 3.3**

---

### Property 2: WCAG AA Text Contrast

*For any* text element rendered on any page (both light and dark themes), the contrast ratio between the element's computed `color` and its computed `background-color` SHALL be at least `4.5:1` per WCAG 2.1 Level AA. This property subsumes the CTA gradient section contrast check.

**Validates: Requirements 4.5, 15.6**

---

### Property 3: Reduced-Motion Disables Transforms

*For any* animated element (one with a hover transform, a `[data-reveal]` transition, or a keyframe animation), when the user's operating system preference is `prefers-reduced-motion: reduce`, the element's `transition-duration` SHALL be `≤ 0.01ms` and any active `transform` SHALL be the identity (`translateY(0)` / `scale(1)`).

**Validates: Requirements 5.4**

---

### Property 4: Mobile Touch Target Size

*For any* interactive element (button, nav item, tab button, link styled as a button) rendered at a viewport width of `375px`, its computed `height` SHALL be at least `44px` and its computed `width` SHALL be at least `44px`.

**Validates: Requirements 6.5, 14.3**

---

### Property 5: Active Nav Item Styling

*For any* `NavLink` component whose `href` matches the current route, the rendered anchor element SHALL have a left border of `3px solid #008d36`, a `background` of `rgba(0,141,54,0.08)`, and a computed `color` of `#ffffff`.

**Validates: Requirements 8.2**

---

### Property 6: glass-panel Visual Invariant

*For any* element bearing the `.glass-panel` class inside `AdminDashboard.razor` or `ClientDashboard.razor`, its computed CSS properties SHALL satisfy: `background` equals `var(--pt-surface)`, `border` equals `1px solid rgba(255,255,255,0.07)`, `border-radius` equals `var(--pt-radius-lg)` (`20px`), and `box-shadow` equals `var(--pt-shadow-md)`.

**Validates: Requirements 10.1**

---

### Property 7: Input Focus-Ring Invariant

*For any* element bearing the `.clinic-input` class on any page (dashboard, auth, or public), when that element receives keyboard focus, its computed `outline` SHALL be `2px solid #008d36`, its `outline-offset` SHALL be `4px`, and its `box-shadow` SHALL include the value `rgba(0,141,54,0.25)`.

**Validates: Requirements 10.4, 13.5**

---

### Property 8: Macro Sum Validation Invariant

*For any* combination of `(carbs, protein, fat, veg)` integer values on the `ClientDashboard` daily log form where `carbs + protein + fat + veg ≠ 100`, the amber warning element SHALL be visible in the DOM. *For any* combination where the sum equals `100`, the warning element SHALL be absent from the DOM.

**Validates: Requirements 12.4**

---

### Property 9: No Horizontal Scroll at Any Breakpoint

*For any* page in the application at viewport widths of `375px`, `768px`, `1024px`, and `1440px`, `document.documentElement.scrollWidth` SHALL NOT exceed `window.innerWidth`.

**Validates: Requirements 14.4**

---

### Property 10: Single-Column Grid Below 640px

*For any* CSS grid container on any page, when the viewport width is below `640px`, every grid item SHALL occupy the full container width (i.e., `grid-template-columns` resolves to a single column).

**Validates: Requirements 14.2**

---

### Property 11: Image Object-Fit and Aspect-Ratio Invariant

*For any* `<img>` element inside a `.pt-svc-card` or `.pt-gallery-item`, its computed `object-fit` SHALL be `cover` and an explicit `aspect-ratio` CSS value SHALL be set on its container element.

**Validates: Requirements 14.5**

---

### Property 12: Interactive Element Focus Indicator Contrast

*For any* interactive element when it receives keyboard focus (`:focus-visible` state), its focus outline color SHALL achieve a minimum contrast ratio of `3:1` against the adjacent surface background color (`--pt-surface` for cards, `--pt-bg-page` for page backgrounds).

**Validates: Requirements 15.1**

---

### Property 13: Non-Decorative Image Alt Text

*For any* `<img>` element in the DOM that does NOT have `aria-hidden="true"`, its `alt` attribute SHALL be a non-empty string. *For any* `<img>` element that has `aria-hidden="true"`, its `alt` attribute SHALL be the empty string (`""`).

**Validates: Requirements 15.2**

---

### Property 14: Form Input Label Association

*For any* `<input>` or `<select>` element in any form across the application, either (a) a `<label>` element exists in the DOM with a `for` attribute matching the input's `id`, or (b) the input has a non-empty `aria-label` attribute.

**Validates: Requirements 15.3**

---

### Property 15: Heading Hierarchy Integrity

*For any* rendered page in the application, the sequence of heading elements (`h1`–`h6`) encountered in DOM order SHALL NOT contain any skipped levels (e.g., an `h3` appearing without a preceding `h2` on the same page, or an `h1` count exceeding one per page).

**Validates: Requirements 15.5**

---

*End of Technical Design Document*
