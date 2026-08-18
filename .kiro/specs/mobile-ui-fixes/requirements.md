# Requirements Document

## Introduction

This feature addresses two confirmed mobile UI bugs on the ProTouch Physio Blazor Server clinic website (`app.css` + Razor pages):

1. **Login page submit button hidden on mobile** — On viewports ≤ 768 px the `.login-form-panel` layout pushes the submit button below the visible area. The fix scopes to a single-column, scrollable layout for the login page on small viewports.
2. **Services and Approach sections invisible/clipped on tablets and landscape phones** — The existing CSS breakpoints skip the 851 px – 1024 px range, leaving `.service-grid-v2` in a 4-column layout and `.split-intro` in a 2-column layout at viewport widths where both are too narrow to display correctly.

All fixes must be additive CSS changes scoped to `app.css`. Desktop and laptop viewports (> 1024 px) must be entirely unaffected.

---

## Glossary

- **Login Form Panel** — The left column of `.login-split-page` carrying the form; CSS class `.login-form-panel`.
- **Login Form Inner** — The constrained content wrapper inside the form panel; CSS class `.login-form-inner`.
- **Submit Button** — The `<button class="login-submit-btn">` element that submits the login form.
- **Visual Panel** — The right decorative column of `.login-split-page`; CSS class `.login-visual-panel`.
- **Service Grid** — The 4-column grid of service cards on the public home page; CSS class `.service-grid-v2` inside `.wellness-page`.
- **Service Card** — An individual service card inside the grid; CSS class `.service-card-home`.
- **Approach Section** — The two-column introductory split layout on the home page; CSS class `.split-intro` inside `.wellness-page`.
- **Wellness Section** — The base section wrapper carrying large vertical padding; CSS class `.wellness-section` inside `.wellness-page`.
- **Tablet/Landscape Range** — Viewport widths from 851 px to 1024 px inclusive.
- **Mobile Range** — Viewport widths of 768 px and below, as defined by the existing `@media (max-width: 768px)` breakpoint in `app.css`.

---

## Requirements

### Requirement 1 — Login Submit Button Visibility on Mobile

**User Story:** As a clinic staff member signing in on a mobile device, I want the login form submit button to always be visible and reachable without horizontal scrolling, so that I can complete sign-in without needing to zoom or scroll unexpectedly.

#### Acceptance Criteria

1. WHEN the viewport width is ≤ 768 px, THE Login Form Panel SHALL occupy the full viewport width and the full viewport height as a single scrollable column, removing the split-screen layout.
2. WHEN the viewport width is ≤ 768 px, THE Login Form Panel SHALL allow vertical scrolling so that all form elements — including the Submit Button — are reachable by normal page scroll.
3. WHEN the viewport width is ≤ 768 px, THE Login Form Panel SHALL NOT use `overflow: hidden` or any property that clips content below the visible fold.
4. WHEN the viewport width is ≤ 768 px, THE Visual Panel SHALL be hidden, allowing the Login Form Panel to take the full screen width.
5. WHEN the viewport width is ≤ 768 px, THE Login Form Inner SHALL NOT exceed the available width of the Login Form Panel.
6. WHILE the viewport width is > 768 px, THE Login Form Panel SHALL retain its existing split-screen two-column appearance and styles without modification.

---

### Requirement 2 — Services Section Visibility on Tablets and Landscape Phones

**User Story:** As a potential patient browsing on a tablet or landscape phone, I want the Services section to display all service cards in a readable layout, so that I can understand the available treatments without horizontal scrolling or clipped content.

#### Acceptance Criteria

1. WHEN the viewport width is ≤ 1024 px, THE Service Grid SHALL display service cards in a 2-column layout instead of 4 columns.
2. WHEN the viewport width is ≤ 1024 px, THE Service Card SHALL NOT enforce a fixed minimum height, allowing card height to be determined by its content.
3. WHEN the viewport width is ≤ 1024 px, THE Wellness Section SHALL reduce its vertical padding so that sections remain compact but readable.
4. WHILE the viewport width is > 1024 px, THE Service Grid SHALL retain its existing 4-column layout without modification.

---

### Requirement 3 — Approach Section Visibility on Tablets and Landscape Phones

**User Story:** As a potential patient browsing on a tablet or landscape phone, I want the Approach section text and headings to display in a single, readable column, so that I can read the full message without text being clipped or columns being too narrow.

#### Acceptance Criteria

1. WHEN the viewport width is ≤ 1024 px, THE Approach Section SHALL switch from a 2-column grid layout to a single-column layout.
2. WHEN the viewport width is ≤ 1024 px, THE Approach Section SHALL reduce the gap between its column contents to a value appropriate for single-column display.
3. WHILE the viewport width is > 1024 px, THE Approach Section SHALL retain its existing 2-column grid layout without modification.

---

### Requirement 4 — Desktop and Laptop Viewport Preservation

**User Story:** As a clinic administrator or patient using a desktop or laptop browser, I want the website layout to remain exactly as it was before these fixes, so that no visual regressions are introduced on large screens.

#### Acceptance Criteria

1. WHILE the viewport width is > 1024 px, THE Login Form Panel SHALL retain all pre-existing styles including the split-screen two-column layout.
2. WHILE the viewport width is > 1024 px, THE Service Grid SHALL retain all pre-existing styles including the 4-column layout and card minimum heights.
3. WHILE the viewport width is > 1024 px, THE Approach Section SHALL retain all pre-existing styles including the 2-column grid.
4. THE app.css file SHALL contain only additive rules within new or existing `@media` blocks — no existing desktop rules SHALL be modified or removed.
