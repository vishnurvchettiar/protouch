# Design Document — Mobile UI Fixes

## Overview

This document describes the technical design for two CSS-only, additive bug fixes in `app.css`:

1. **Login submit button hidden on mobile** — layout correction at ≤ 768 px so the `.login-form-panel` scrolls rather than clips the submit button below the fold.
2. **Services and Approach sections clipped on tablets / landscape phones** — a new `@media (max-width: 1024px)` breakpoint block for `.wellness-page` that collapses `.service-grid-v2` to 2 columns and `.split-intro` to 1 column.

All changes are **purely additive CSS** placed inside `@media` blocks. No existing desktop rules are touched. No Razor component logic changes are required.

---

## Architecture

### Constraints

| Constraint | Rationale |
|---|---|
| CSS-only changes | The bugs are purely layout/overflow issues; no Blazor state or component logic is involved. |
| Additive rules only | Existing desktop rules must not be modified or removed (Requirement 4.4). |
| Scoped to existing classes | All selectors use existing class names already present in `app.css` and the Razor pages; no new HTML attributes or classes need to be added. |
| Single file change | Only `app.css` is modified; no Razor (`.razor`) files need to change. |

### File Targeted

```
ClinicApp/wwwroot/app.css
```

Two new `@media` blocks are appended at the end of the file (after the existing blocks):

1. `@media (max-width: 768px)` — scoped to `.login-split-page` and its descendants.
2. `@media (max-width: 1024px)` — scoped to `.wellness-page` service and approach sections.

---

## Component Design

### Fix 1 — Login Submit Button Visibility (≤ 768 px)

#### Root Cause

The existing `@media (max-width: 768px)` block hides `.login-visual-panel` and sets `background:#fff` on `.login-form-panel`, but it does **not**:
- Remove the `align-items: center` from `.login-form-panel` (which vertically centres the form, pushing content below the fold on short screens).
- Ensure `overflow-y: auto` so the panel scrolls vertically.
- Remove any `overflow: hidden` that could clip descendant content.
- Constrain `.login-form-inner` to `max-width: 100%`.

#### Fix

```css
@media (max-width: 768px) {
    /* Re-stack the split page to single column (visual panel already hidden) */
    .login-split-page {
        grid-template-columns: 1fr;
    }

    /* Allow the form panel to scroll instead of clipping below the fold */
    .login-form-panel {
        align-items: flex-start;   /* stop vertical centering that pushes button off-screen */
        overflow-y: auto;          /* enable vertical scroll */
        overflow-x: hidden;        /* no horizontal scroll */
        padding-top: 40px;         /* comfortable top breathing room */
        min-height: 100vh;         /* fill the screen but allow scroll growth */
    }

    /* Prevent inner wrapper from exceeding panel width */
    .login-form-inner {
        max-width: 100%;
        width: 100%;
    }
}
```

**Why `align-items: flex-start`?**  
`.login-form-panel` is a flex container. `align-items: center` (the default inherited from the desktop layout) uses `height: 100vh` to vertically centre the form. On short mobile viewports (e.g. 568 px on a landscape phone) the form is taller than the viewport, so the centred position pushes the submit button below the visible fold. Switching to `flex-start` lets the form start at the top and the page scroll naturally.

**Why not `overflow: hidden`?**  
Requirement 1.3 explicitly forbids `overflow: hidden` on the Login Form Panel at mobile widths.

#### Desktop/Laptop Preservation

The fix is wrapped in `@media (max-width: 768px)`. At any viewport wider than 768 px the existing two-column split layout is completely unaffected.

---

### Fix 2 — Services Section on Tablets / Landscape Phones (≤ 1024 px)

#### Root Cause

The existing responsive breakpoints in `app.css` jump from:
- `@media (max-width: 850px)` — switches wellness hero and some layout changes, but leaves `.service-grid-v2` at 4 columns and `.wellness-section` padding at `100px 30px`.
- `@media (max-width: 520px)` — switches `.service-grid-v2` to 1 column.

The **851 px – 1024 px gap** is unhandled: at these widths (common for iPad landscape, Surface Go, and landscape Android phones) `.service-grid-v2` remains in a 4-column layout where each column is only ~220–250 px wide — too narrow to display card content correctly.

#### Fix

```css
@media (max-width: 1024px) {
    .wellness-page .service-grid-v2 {
        grid-template-columns: 1fr 1fr;   /* 2-column instead of 4 */
    }

    .wellness-page .service-card-home {
        min-height: auto;                  /* let card height be content-driven */
    }

    .wellness-page .wellness-section {
        padding: 60px 20px;               /* reduced from 100px 30px */
    }
}
```

**Column count rationale:** At 1024 px, 2 columns give each card ~480–490 px — enough for card content, images, and bullet lists to render without clipping.

**`min-height: auto`:** The desktop layout sets `min-height: 365px` (or `370px`) on `.service-card-home` to ensure uniform card heights in a 4-column row. With 2 columns this constraint is no longer needed; card height can be determined by content, which is more natural on touch devices.

**Padding reduction:** `.wellness-section` uses `padding: 100px 30px` on desktop. At tablet widths, 100 px of vertical padding wastes screen real estate; 60 px keeps sections compact without crowding.

#### Desktop/Laptop Preservation

The fix is wrapped in `@media (max-width: 1024px)`. At any viewport wider than 1024 px the 4-column grid, fixed card heights, and full padding are completely unaffected.

---

### Fix 3 — Approach Section on Tablets / Landscape Phones (≤ 1024 px)

#### Root Cause

`.split-intro` uses `grid-template-columns: 0.9fr 1.1fr` with `gap: 94px`. At 851 px – 1024 px viewport widths each column is only ~420–460 px wide (before the gap), and the 94 px gap consumes 9 % of the viewport — resulting in text that is too narrow and tightly spaced.

#### Fix

```css
/* Included in the same @media (max-width: 1024px) block as Fix 2 */

.wellness-page .split-intro {
    grid-template-columns: 1fr;   /* single column */
    gap: 30px;                    /* reduced from 94px */
}
```

**Single column:** With one column the heading and body text each get the full content width, which is legible at tablet widths.

**Gap 30 px:** A reasonable vertical gap between the heading block and the body copy when stacked vertically.

#### Desktop/Laptop Preservation

Same `@media (max-width: 1024px)` wrapper — desktop layout completely unaffected.

---

## Data Models

No data models are affected. This feature is pure CSS layout.

---

## Interfaces

No API, service, or component interfaces change. The sole interface is the CSS class contract between `app.css` and the Razor templates, which is preserved: all selectors used in the fix already exist in both files.

---

## Error Handling

No runtime error handling is required for CSS fixes. The following CSS edge cases are handled defensively:

| Edge Case | Handling |
|---|---|
| Very narrow screens (< 320 px) | `max-width: 100%; width: 100%` on `.login-form-inner` prevents horizontal overflow at any sub-320 px width. |
| Landscape phones at exactly 768 px | `max-width: 768px` is inclusive; the fix applies correctly. |
| Tablets at exactly 1024 px | `max-width: 1024px` is inclusive; the fix applies correctly. |
| Cascading conflicts with existing `@media (max-width: 850px)` block | The new 1024 px block targets different properties (`grid-template-columns` on `.service-grid-v2` and `.split-intro`) that the 850 px block does not set. No conflicts arise. |
| Cascading conflicts with `@media (max-width: 520px)` block | The 520 px block correctly overrides `.service-grid-v2` to `grid-template-columns: 1fr` (single column), which cascades on top of the 1024 px 2-column rule — this is the intended and correct behaviour. |

---

## Implementation Plan

All changes land in a single append to `app.css`. The new block to append:

```css
/* ═══════════════════════════════════════════════════════════════════
   MOBILE UI FIXES — Additive responsive corrections
   1. Login submit button visible on mobile (≤ 768 px)
   2. Service grid 2-column on tablets/landscape phones (≤ 1024 px)
   3. Approach section single-column on tablets/landscape phones (≤ 1024 px)
   All rules are additive — no existing desktop rules modified or removed.
   ═══════════════════════════════════════════════════════════════════ */

@media (max-width: 768px) {
    /* Fix 1 — Login submit button always reachable */
    .login-split-page {
        grid-template-columns: 1fr;
    }
    .login-form-panel {
        align-items: flex-start;
        overflow-y: auto;
        overflow-x: hidden;
        padding-top: 40px;
        min-height: 100vh;
    }
    .login-form-inner {
        max-width: 100%;
        width: 100%;
    }
}

@media (max-width: 1024px) {
    /* Fix 2 — Service grid: 2 columns on tablets/landscape phones */
    .wellness-page .service-grid-v2 {
        grid-template-columns: 1fr 1fr;
    }
    .wellness-page .service-card-home {
        min-height: auto;
    }
    .wellness-page .wellness-section {
        padding: 60px 20px;
    }

    /* Fix 3 — Approach section: single column on tablets/landscape phones */
    .wellness-page .split-intro {
        grid-template-columns: 1fr;
        gap: 30px;
    }
}
```

**Ordering note:** The 1024 px block is placed *before* the 850 px and 520 px blocks in terms of specificity cascade — but since it is appended after the existing blocks, the existing 850 px and 520 px blocks take precedence for viewports within those ranges. This is the correct cascade order: the existing narrower breakpoints remain authoritative for their ranges.

> **Correction:** In standard CSS cascade, later rules win. Since the new blocks are appended *after* the existing 850 px and 520 px blocks, the existing narrower breakpoints would be overridden by the 1024 px block for viewports that match *both* media queries (e.g. a 520 px viewport matches both `max-width: 1024px` and `max-width: 520px`). To preserve the existing narrow-breakpoint behaviour, the new 1024 px block must be inserted **before** the existing 850 px and 520 px blocks, OR the new block must explicitly not override properties already handled by the narrower blocks (it doesn't — the 520 px block sets `grid-template-columns: 1fr` for `.service-grid-v2`, and the 1024 px block sets `grid-template-columns: 1fr 1fr`; the later/narrower 520 px rule correctly overrides this). Since the existing blocks already cascade correctly and the appended 1024 px block only *adds* the 851–1024 px range behaviour, appending last is safe — the 520 px and 850 px rules will still override the 1024 px rules at their respective widths because they appear later in the file.

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Login Form Inner Width Containment

*For any* viewport width W where 320 px ≤ W ≤ 768 px, the rendered width of `.login-form-inner` SHALL be less than or equal to the rendered width of `.login-form-panel`.

**Validates: Requirements 1.5**

### Property 2: CSS Additive-Only Change

*For any* CSS rule R that existed in `app.css` before this feature's changes, R SHALL still be present and unchanged in the modified `app.css` — the diff SHALL contain only additions (new lines) within new `@media` blocks, and no deletions or modifications to lines outside those blocks.

**Validates: Requirements 4.4**

---

## Testing Strategy

### Unit / Example Tests (primary approach for CSS fixes)

Since the vast majority of acceptance criteria are discrete CSS property assertions at specific viewport breakpoints, **example-based snapshot and computed-style tests** are the right tool:

| AC | Test | Viewport | Assertion |
|---|---|---|---|
| 1.1 | Login layout is single column | 768 px | `.login-split-page` grid columns = 1 |
| 1.2 | Submit button reachable by scroll | 768 px | Submit button bounding rect within scrollable document |
| 1.3 | No overflow clipping on form panel | 768 px | `.login-form-panel` overflow-y ≠ `hidden`/`clip` |
| 1.4 | Visual panel hidden | 768 px | `.login-visual-panel` display = `none` |
| 1.6 | Desktop layout unchanged | 1280 px | `.login-split-page` grid = 2 columns |
| 2.1 | Service grid 2-column | 1024 px | `.service-grid-v2` 2 columns |
| 2.2 | Card height content-driven | 1024 px | `.service-card-home` min-height = `auto`/`0px` |
| 2.3 | Section padding reduced | 1024 px | `.wellness-section` padding-top < 100 px |
| 2.4 | Desktop service grid 4-column | 1440 px | `.service-grid-v2` 4 columns |
| 3.1 | Approach single column | 1024 px | `.split-intro` grid = 1 column |
| 3.2 | Approach gap reduced | 1024 px | `.split-intro` gap ≤ 30 px |
| 3.3 | Desktop approach unchanged | 1440 px | `.split-intro` 2 columns |
| 4.1–4.3 | No desktop regressions | 1440 px | All desktop rules intact |

### Property Tests (for universally-quantified properties)

- **Property 1** (inner width containment): Test with a range of viewport widths W ∈ [320, 768] — for any W in that range, `offsetWidth(.login-form-inner) ≤ offsetWidth(.login-form-panel)`.
- **Property 2** (additive-only): Verify via CSS diff tool or text comparison that the modified `app.css` contains only additions relative to the original.

### Manual / Visual Verification (recommended)

Given the CSS-only nature of the fix, manual device testing is the most practical final check:

- **iPhone SE (375 × 667)** — login form: submit button visible without scrolling / with scroll.
- **iPad (768 × 1024)** — login: single column form. Services: 2-column cards. Approach: single column.
- **iPad landscape (1024 × 768)** — Services and Approach at exactly the breakpoint boundary.
- **Desktop (1440 × 900)** — all sections visually unchanged.
