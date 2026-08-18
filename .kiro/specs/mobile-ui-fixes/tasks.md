# Implementation Plan: Mobile UI Fixes

## Overview

CSS-only, additive fix appended to `ClinicApp/wwwroot/app.css`. Two new `@media` blocks are appended at the end of the file — no existing rules are touched, so desktop and laptop viewports (> 1024 px) are completely unaffected.

1. **`@media (max-width: 768px)`** — corrects the Login Form Panel so the submit button is always reachable on mobile.
2. **`@media (max-width: 1024px)`** — collapses the Service Grid to 2 columns and the Approach Section to 1 column on tablets and landscape phones.

---

## Tasks

- [ ] 1. Append Login mobile fix to `app.css`
  - [ ] 1.1 Add `@media (max-width: 768px)` block with Login Form Panel rules
    - In `ClinicApp/wwwroot/app.css`, append a new `@media (max-width: 768px)` block scoped to `.login-split-page`, `.login-form-panel`, and `.login-form-inner`
    - Set `.login-split-page { grid-template-columns: 1fr; }` to collapse to single column
    - Set `.login-form-panel { align-items: flex-start; overflow-y: auto; overflow-x: hidden; padding-top: 40px; min-height: 100vh; }` — **do NOT use `overflow: hidden`**
    - Set `.login-form-inner { max-width: 100%; width: 100%; }` to prevent horizontal overflow
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

  - [ ]* 1.2 Write property test for Login Form Inner width containment
    - **Property 1: Login Form Inner Width Containment**
    - For any viewport width W ∈ [320, 768], `offsetWidth(.login-form-inner) ≤ offsetWidth(.login-form-panel)`
    - **Validates: Requirements 1.5**

  - [ ]* 1.3 Write unit tests for Login mobile fix
    - At 768 px: assert `.login-split-page` has `grid-template-columns` = 1 column
    - At 768 px: assert `.login-form-panel` `overflow-y` ≠ `hidden` and ≠ `clip`
    - At 768 px: assert submit button bounding rect is within the scrollable document height
    - At 1280 px: assert `.login-split-page` still has 2-column grid (desktop preservation)
    - _Requirements: 1.1, 1.2, 1.3, 1.6_

- [ ] 2. Append tablet/landscape-phone fix to `app.css`
  - [ ] 2.1 Add `@media (max-width: 1024px)` block with Service Grid and Approach Section rules
    - In `ClinicApp/wwwroot/app.css`, append a new `@media (max-width: 1024px)` block **after** the existing `@media (max-width: 850px)` and `@media (max-width: 520px)` blocks so cascade order is correct
    - Set `.wellness-page .service-grid-v2 { grid-template-columns: 1fr 1fr; }` — 2-column layout for 851 px – 1024 px range
    - Set `.wellness-page .service-card-home { min-height: auto; }` — content-driven card height
    - Set `.wellness-page .wellness-section { padding: 60px 20px; }` — reduced vertical padding
    - Set `.wellness-page .split-intro { grid-template-columns: 1fr; gap: 30px; }` — single column Approach Section
    - _Requirements: 2.1, 2.2, 2.3, 3.1, 3.2_

  - [ ]* 2.2 Write property test for additive-only CSS change
    - **Property 2: CSS Additive-Only Change**
    - Verify that every rule present in `app.css` before this feature still exists unchanged — the diff must contain only added lines inside new `@media` blocks and no deletions or modifications outside those blocks
    - **Validates: Requirements 4.4**

  - [ ]* 2.3 Write unit tests for tablet/landscape-phone fix
    - At 1024 px: assert `.wellness-page .service-grid-v2` renders 2 columns
    - At 1024 px: assert `.wellness-page .service-card-home` computed `min-height` = `0px` or `auto`
    - At 1024 px: assert `.wellness-page .wellness-section` `padding-top` < 100 px
    - At 1024 px: assert `.wellness-page .split-intro` renders 1 column
    - At 1024 px: assert `.wellness-page .split-intro` `gap` ≤ 30 px
    - At 1440 px: assert `.wellness-page .service-grid-v2` still renders 4 columns (desktop preservation)
    - At 1440 px: assert `.wellness-page .split-intro` still renders 2 columns (desktop preservation)
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 3.1, 3.2, 3.3, 4.1, 4.2, 4.3_

- [ ] 3. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

---

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- **Only one file changes:** `ClinicApp/wwwroot/app.css` — no Razor (`.razor`) files are modified
- **Desktop/laptop unaffected:** All rules are inside `max-width` media queries; viewports > 1024 px are completely unaffected (Requirement 4)
- **No `overflow: hidden` on `.login-form-panel`** — this would clip the submit button (Requirement 1.3)
- The new `@media (max-width: 1024px)` block must be appended **after** the existing `max-width: 850px` and `max-width: 520px` blocks so narrower breakpoints still correctly override the 1024 px rules at their respective widths
- Property tests validate universally-quantified correctness properties from the design document
- Unit tests validate specific viewport snapshots and computed-style assertions

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["2.1", "1.2", "1.3"] },
    { "id": 2, "tasks": ["2.2", "2.3"] }
  ]
}
```
