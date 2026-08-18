# Implementation Plan: Homepage Enhancement

## Overview

This plan converts the homepage enhancement design into an incremental series of coding tasks. Changes are scoped to `Home.razor`, `app.css` (append, `.ac-page` scoped), `site.js` (append-only), `SiteFeedback.cs` (new), `ApplicationDbContext.cs` (add DbSet), `ClinicService.cs` (3 new methods), `AdminDashboard.razor` (FeedbackTab), and one EF Core migration. Tasks are ordered so each step compiles and leaves no orphaned code.

---

## Tasks

- [ ] 1. Remove Before/After Slider and set up CSS/JS foundations
  - [ ] 1.1 Remove Before/After Slider from `Home.razor`
    - Delete `beforeAfterPairs` list and `BeforeAfterPair` record from `@code`
    - Remove the `<section class="ac-before-after">` markup block
    - Remove the `initBeforeAfterSliders` JS interop call from `OnAfterRenderAsync`
    - Remove any `ac-ba-*` CSS rules from `app.css` (or confirm they don't exist yet)
    - _Requirements: 1.7_

  - [ ] 1.2 Append CSS custom property tokens and visual overhaul base styles to `app.css`
    - Add BrandPalette tokens (`--ac-green`, `--ac-purple`, `--ac-yellow`, `--ac-red`) under `.ac-page`
    - Add typography scale tokens (`--ac-display`, `--ac-heading`, `--ac-body`)
    - Add section spacing tokens (`--ac-section-py-desktop`, `--ac-section-py-mobile`, `--ac-section-px`)
    - Add `[data-reveal]` ScrollReveal animation keyframes and `.revealed` state
    - Add three-breakpoint section padding rules (desktop ≥ 1024 px, tablet 768–1023 px, mobile < 768 px)
    - _Requirements: 4.1, 4.2, 4.3, 4.5, 4.6, 4.8_

  - [ ] 1.3 Append JS foundation functions to `site.js`
    - Add `initStickyHeader(selector)` — scroll listener toggling `ac-header--scrolled` at 60 px
    - Add `initBackToTop()` — scroll listener toggling `ac-back-to-top--visible` at 400 px
    - Add `scrollToTop()` — smooth scroll to top
    - Add `initActiveNavHighlight()` — IntersectionObserver applying `ac-nav--active` at 50 % threshold
    - Add `smoothScrollTo(selector)` — smooth scroll to CSS selector
    - Add `initGalleryLightbox(dotNetRef)` — `keydown` ESC listener that calls `dotNetRef.invokeMethodAsync("CloseLightbox")`
    - Add updated `destroyAestheticAnimations()` — cleans up all new scroll/resize listeners and observers
    - _Requirements: 4.4, 5.2, 5.3, 5.4, 1.6_

- [ ] 2. SiteFeedback data layer (model, DbContext, migration)
  - [ ] 2.1 Create `SiteFeedback.cs` entity
    - Create `ClinicApp/Data/SiteFeedback.cs`
    - Define `Id` (int PK), `Rating` (int NOT NULL), `Comment` (string?, `[MaxLength(500)]`), `SubmittedAt` (DateTime NOT NULL)
    - _Requirements: 6.8_

  - [ ] 2.2 Add `DbSet<SiteFeedback>` to `ApplicationDbContext.cs`
    - Add `public DbSet<SiteFeedback> SiteFeedbacks { get; set; }` property
    - _Requirements: 6.11_

  - [ ] 2.3 Generate EF Core migration `AddSiteFeedback`
    - Run `dotnet ef migrations add AddSiteFeedback --project ClinicApp`
    - Run `dotnet ef database update --project ClinicApp`
    - Verify generated migration creates `SiteFeedbacks` table with correct columns
    - _Requirements: 6.11_

- [ ] 3. ClinicService — feedback persistence methods
  - [ ] 3.1 Implement `SaveFeedbackAsync` in `ClinicService.cs`
    - Validate rating in `[1, 5]`; throw `ArgumentOutOfRangeException` if outside range
    - Trim comment; enforce max 500 chars
    - Construct `SiteFeedback` with `SubmittedAt = DateTime.UtcNow`; persist via `_db.SaveChangesAsync()`; return entity
    - _Requirements: 6.6, 6.9_

  - [ ]* 3.2 Write unit tests for `SaveFeedbackAsync`
    - Test valid (rating 1–5, comment within limit) → returns saved entity with correct fields
    - Test rating = 0 → throws `ArgumentOutOfRangeException`
    - Test rating = 6 → throws `ArgumentOutOfRangeException`
    - Test comment over 500 chars → throws `ArgumentException`
    - _Requirements: 6.9_

  - [ ] 3.3 Implement `GetFeedbackAsync` in `ClinicService.cs`
    - Return page of `SiteFeedback` ordered by `SubmittedAt` descending, with `Skip((page-1)*pageSize).Take(pageSize)`
    - _Requirements: 6.10_

  - [ ] 3.4 Implement `DeleteFeedbackAsync` in `ClinicService.cs`
    - Find record by PK; throw `KeyNotFoundException` if missing
    - Remove and call `SaveChangesAsync`
    - _Requirements: 7.5_

  - [ ]* 3.5 Write property tests for ClinicService feedback methods
    - **Property 14: SaveFeedbackAsync Round-Trip** — for any rating `r ∈ [1,5]` and comment `c` of length ≤ 500, returned entity has `Rating == r`, trimmed `Comment`, and `SubmittedAt` close to UTC now
    - **Property 15: GetFeedbackAsync Pagination Order** — for any seeded list, retrieved page is ordered by `SubmittedAt` descending with correct Skip/Take
    - **Property 19: DeleteFeedbackAsync Removes Record** — after deletion, `GetFeedbackAsync` with sufficient page size does not include deleted id
    - **Validates: Requirements 6.9, 6.10, 7.5**

- [ ] 4. Checkpoint — Ensure all data layer tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 5. Improved Navigation markup and Blazor state
  - [ ] 5.1 Update sticky header nav links in `Home.razor`
    - Replace current nav links with anchors: Services, About, Gallery, Feedback, Contact
    - Add hamburger button markup with `aria-expanded` binding to `_mobileMenuOpen`
    - Add mobile nav dropdown div with `ac-mobile-nav--open` conditional class
    - Add `_mobileMenuOpen` bool field and `ToggleMobileMenu()` to `@code`
    - Add `<button class="ac-back-to-top" id="ac-back-to-top">` markup outside header
    - Wire `@onclick` on Back-to-Top button to `JS.InvokeVoidAsync("scrollToTop")`
    - Call `initActiveNavHighlight()`, `initBackToTop()`, and `initStickyHeader(".ac-header")` in `OnAfterRenderAsync`
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

  - [ ] 5.2 Append navigation CSS to `app.css`
    - Add `.ac-hamburger` styles (three-bar icon, mobile-only visibility)
    - Add `.ac-mobile-nav` and `.ac-mobile-nav--open` transition styles
    - Add `.ac-nav--active` highlight style using `--ac-green`
    - Add `.ac-back-to-top` fixed button styles; add `.ac-back-to-top--visible` visibility toggle
    - Add `.ac-header--scrolled` background + drop shadow transition
    - _Requirements: 4.4, 5.3, 5.4, 5.5_

  - [ ]* 5.3 Write property test for mobile menu toggle
    - **Property 9: Mobile Menu Toggle Idempotence** — for any initial `_mobileMenuOpen`, calling `ToggleMobileMenu()` twice returns it to original value
    - **Validates: Requirements 5.5**

- [ ] 6. Service Filter Chips
  - [ ] 6.1 Add Service Filter Chips markup to `Home.razor`
    - Insert `<div class="ac-chip-row">` above the `.ac-services-tabs` section
    - Render one `<button class="ac-tab-chip">` per entry in `serviceCards` with icon and name
    - Bind `aria-pressed` and `--chip-accent` inline style to `activeTab == svc.Id`
    - Wire `@onclick` to existing `SetTab(svc.Id)`
    - After `SetTab`, call `JS.InvokeVoidAsync("smoothScrollTo", "#services")` if section not visible
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [ ] 6.2 Append Service Filter Chip CSS to `app.css`
    - Add `.ac-chip-row` flex row styles with gap and wrapping
    - Add `.ac-tab-chip` pill styles (border-radius, padding, border)
    - Add `.ac-tab-chip--active` variant using `background: var(--chip-accent)` and contrasting text
    - _Requirements: 2.2, 2.6_

  - [ ]* 6.3 Write property tests for filter chip behaviour
    - **Property 5: SetTab Activates Correct Service** — for any `svc` in `serviceCards`, calling `SetTab(svc.Id)` yields `activeTab == svc.Id`
    - **Property 6: Filter Chip aria-pressed Consistency** — `aria-pressed == "true"` iff `activeTab == chip.serviceId`
    - **Validates: Requirements 2.2, 2.6**

- [ ] 7. SVG Body Hotspot Map
  - [ ] 7.1 Add Body Hotspot Map markup to `Home.razor`
    - Add `_hoveredHotspot` string field to `@code`
    - Insert `<div class="ac-hotspot-wrap">` with inline SVG (`viewBox="0 0 300 450"`) inside or adjacent to Tabbed Services section
    - Add simplified body outline `<path class="ac-body-outline">`
    - Add three `<ellipse>` hotspot elements (shoulder cx=150 cy=80, waist cx=150 cy=185, knee cx=150 cy=300)
    - Bind `ac-hotspot--hover` and `ac-hotspot--active` classes to `_hoveredHotspot` and `activeTab`
    - Add `role="button"`, `tabindex="0"`, `aria-label`, and `<title>` to each ellipse
    - Wire `@onclick`, `@onmouseenter`, and `@onmouseleave` handlers
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

  - [ ] 7.2 Append Body Hotspot Map CSS to `app.css`
    - Add `.ac-hotspot-wrap` centering and size constraints
    - Add `.ac-body-svg` responsive width rules
    - Add `.ac-body-outline` stroke/fill styles
    - Add `.ac-hotspot` fill + opacity defaults
    - Add `.ac-hotspot--hover` and `.ac-hotspot--active` BrandPalette colour states
    - _Requirements: 3.6_

  - [ ]* 7.3 Write property tests for hotspot map
    - **Property 7: Hotspot Click Activates Mapped Service** — clicking each ellipse sets `activeTab` to its mapped service id
    - **Property 8: Hotspot aria-label Non-Empty** — each hotspot element has a non-empty `aria-label` containing both region and action description
    - **Validates: Requirements 3.3, 3.5**

- [ ] 8. BMI / Result Estimator Widget
  - [ ] 8.1 Add BMI Estimator Blazor state and logic to `Home.razor`
    - Add input fields: `_heightCm` (int), `_weightKg` (double), `_bmiGoal` (string)
    - Add result fields: `_bmiValue` (double?), `_bmiCategory`, `_bmiColor`, `_bmiMessage`, `_bmiOutcome`
    - Add validation error fields: `_heightError`, `_weightError`, `_goalError`
    - Implement `ComputeBmi()`: validate ranges (height 100–250, weight 20–300, goal non-empty), compute `BMI = weight / (height/100)²` rounded to 1 dp, classify into Underweight/Normal/Overweight/Obese, set `_bmiColor` per BrandPalette, look up message from `WellnessMessageMap`, look up outcome from `OutcomeMap`
    - Define static `WellnessMessageMap` (4 categories × 5 goals = 20 entries) and `OutcomeMap` (5 goal entries)
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.8, 8.9_

  - [ ] 8.2 Add BMI Estimator markup to `Home.razor`
    - Insert `<section class="ac-bmi" id="bmi-estimator" data-reveal>` between the Animated Stats Strip and the Gallery Grid sections
    - Add form inputs: height number input (min 100 max 250), weight number input (min 20 max 300 step 0.1), wellness goal select with 5 options
    - Display field-level validation error spans adjacent to each input
    - Add "Calculate My Estimate →" button wired to `ComputeBmi`
    - Conditionally render result block (`@if (_bmiValue.HasValue)`) with: BMI numeric score, category label, colour-coded container using `--bmi-color`, personalised message, outcome statement, and "Book Your Free Consultation →" link to `/book`
    - Add three decorative SVG blob dividers at: Stats Strip → BMI Estimator, BMI Estimator → Gallery, Gallery → Testimonials
    - _Requirements: 8.1, 8.2, 8.6, 8.7, 8.8, 4.7_

  - [ ] 8.3 Append BMI Estimator CSS to `app.css`
    - Add `.ac-bmi` section layout and `.ac-bmi-inner` max-width container
    - Add `.ac-bmi-form` grid layout for the three input fields
    - Add `.ac-bmi-field` label + input + error stack styles
    - Add `.ac-bmi-result` card styles using `var(--bmi-color)` for border/accent
    - Add `.ac-bmi-score` large number display and `.ac-bmi-label` badge
    - Add `.ac-field-error` red validation message styles
    - Add `.ac-btn-green` variant using `var(--ac-green)` background
    - Add decorative blob divider SVG `overflow: hidden; margin: -1px 0` styles
    - _Requirements: 8.7, 8.10, 4.7_

  - [ ]* 8.4 Write property tests for BMI Estimator
    - **Property 20: BMI Input Validation Blocks Computation** — height outside [100,250] or weight outside [20,300] or empty goal leaves `_bmiValue == null` and sets non-empty error string
    - **Property 21: BMI Formula Correctness** — for any valid (h, w), `_bmiValue == Math.Round(w / Math.Pow(h/100.0, 2), 1)`
    - **Property 22: BMI Category Classification** — correct boundary assignments at 18.5, 25.0, 30.0
    - **Property 23: BMI Category Colour Encoding** — `_bmiColor` maps Underweight→`#7c1b68`, Normal→`#008d36`, Overweight→`#ffeb00`, Obese→`#e52321`
    - **Property 24: Wellness Message Coverage** — `WellnessMessageMap[c][g]` is non-empty for all 20 category × goal combinations
    - **Property 25: BMI Estimator No Persistence** — `ComputeBmi()` with valid inputs makes no `SaveChangesAsync` call
    - **Validates: Requirements 8.2, 8.3, 8.4, 8.5, 8.9, 8.10**

- [ ] 9. Photo Gallery Grid
  - [ ] 9.1 Add Gallery Grid Blazor state and logic to `Home.razor`
    - Add fields: `_galleryVisible = 24`, `_galleryTotal = 60`, `_isLightboxOpen = false`, `_selectedLightboxIndex = 0`
    - Add computed properties: `GalleryImages` (Range 1–60, formatted `/images/clinic-gallery/gallery-{i:D2}.jpeg`), `CanLoadMore`
    - Add event handlers: `LoadMoreGallery()`, `OpenLightbox(int index)`, `CloseLightbox()` (annotated with `[JSInvokable]`), `LightboxNext()`, `LightboxPrev()`
    - Call `JS.InvokeVoidAsync("initGalleryLightbox", DotNetObjectReference.Create(this))` in `OnAfterRenderAsync` when `_isLightboxOpen` changes
    - _Requirements: 1.2, 1.3, 1.4, 1.5, 1.6_

  - [ ] 9.2 Add Gallery Grid markup to `Home.razor`
    - Replace removed Before/After section with `<section class="ac-gallery" id="gallery" data-reveal>`
    - Add kicker, heading, `<div class="ac-gallery-grid">` with image loop (24 initial, `loading` eager/lazy by index, `onerror` hide)
    - Add "Load More" button gated on `CanLoadMore`
    - Add lightbox overlay (`@if (_isLightboxOpen)`) with close button, prev/next buttons, full-size image
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.8_

  - [ ] 9.3 Append Gallery Grid CSS to `app.css`
    - Add `.ac-gallery-grid` CSS grid with auto-fill columns (min 160 px)
    - Add `.ac-gallery-item` hover scale and cursor pointer
    - Add `.ac-load-more` button styles aligned with BrandPalette
    - Add `.ac-lightbox` full-screen overlay (fixed, z-index, dark background)
    - Add `.ac-lightbox-close`, `.ac-lightbox-prev`, `.ac-lightbox-next` button styles
    - _Requirements: 1.1, 1.5_

  - [ ]* 9.4 Write property tests for Gallery Grid
    - **Property 1: Gallery Initial Load Count** — freshly initialised state has `_galleryVisible == 24`
    - **Property 2: Load More Increments by 12** — `LoadMoreGallery()` increments `_galleryVisible` by 12, capped at 60
    - **Property 3: Lazy Loading Attribute by Index** — index < 6 gets `"eager"`, index ≥ 6 gets `"lazy"`
    - **Property 4: Lightbox Open/Close Round-Trip** — `OpenLightbox(i)` sets `_isLightboxOpen = true` and `_selectedLightboxIndex == i`; `CloseLightbox()` sets `_isLightboxOpen = false`
    - **Validates: Requirements 1.2, 1.3, 1.4, 1.5, 1.6**

- [ ] 10. Checkpoint — Ensure all component logic tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 11. Feedback Widget (Home.razor)
  - [ ] 11.1 Add Feedback Widget Blazor state and logic to `Home.razor`
    - Add fields: `_feedbackRating`, `_feedbackHoverRating`, `_feedbackComment`, `_feedbackSubmitting`, `_feedbackSuccess`, `_feedbackError`
    - Add constant `FeedbackMaxChars = 500` and computed `FeedbackCharsRemaining`
    - Add event handlers: `SetFeedbackStar(int star)`, `HoverFeedbackStar(int star)`, `UnhoverFeedbackStar()`
    - Implement `SubmitFeedbackAsync()`: validate `_feedbackRating != 0`, call `ClinicService.SaveFeedbackAsync`, set `_feedbackSuccess` on success or `_feedbackError` on exception
    - Add `@inject ClinicApp.Services.ClinicService ClinicService` directive
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7_

  - [ ] 11.2 Add Feedback Widget markup to `Home.razor`
    - Insert `<section class="ac-feedback" id="feedback" data-reveal>` between FAQ and Contact sections
    - Add kicker, heading, star rating row (5 `<button>` elements with hover/lit class logic), validation error paragraph, textarea with `maxlength="500"` and `oninput` bind, character counter, submit button with disabled binding and loading label, success confirmation block
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7_

  - [ ] 11.3 Append Feedback Widget CSS to `app.css`
    - Add `.ac-feedback` section layout and `.ac-feedback-inner` max-width container
    - Add `.ac-stars-row` flex row and `.ac-star-btn` reset styles
    - Add `.ac-star--lit` yellow highlight using `var(--ac-yellow)` and scale transform
    - Add `.ac-feedback-textarea` full-width textarea styles
    - Add `.ac-char-counter` small muted text
    - Add `.ac-feedback-error` red inline validation message
    - Add `.ac-feedback-success` confirmation block with icon
    - _Requirements: 6.2, 6.3, 6.4, 6.5_

  - [ ]* 11.4 Write property tests for Feedback Widget
    - **Property 10: Star Rating Highlight Reflects Selection** — for `r ∈ [1,5]`, stars 1–r are lit, r+1–5 are unlit
    - **Property 11: Hover Does Not Commit Rating** — setting `_feedbackHoverRating` does not change `_feedbackRating`
    - **Property 12: Comment Character Counter** — `FeedbackCharsRemaining == 500 - comment.Length` for any `n ≤ 500`
    - **Property 13: Zero-Rating Submission Blocked** — `SubmitFeedbackAsync` with `_feedbackRating == 0` does not persist and sets `_feedbackError` non-empty
    - **Validates: Requirements 6.2, 6.3, 6.4, 6.5**

- [ ] 12. Admin Feedback Management Tab
  - [ ] 12.1 Add FeedbackTab state and logic to `AdminDashboard.razor`
    - Add fields: `_feedbackList`, `_feedbackPage`, `_feedbackStarFilter`, `_feedbackDeleteId`
    - Add computed properties: `_feedbackTotal`, `_feedbackAvgRating`, `_feedbackPerStar`, `FilteredFeedback`
    - Add helper methods: `SwitchToFeedback()` (loads all records), `ConfirmDeleteFeedback(int id)` (calls service, removes from list), `ToggleFeedbackExpand(int id)`, `GetIst()` (returns `Asia/Kolkata` TimeZoneInfo)
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7_

  - [ ] 12.2 Add FeedbackTab button and markup to `AdminDashboard.razor`
    - Add "Feedback" tab button to existing tab bar, wired to `SwitchToFeedback()`
    - Add `@if (activeTab == "feedback")` content block containing:
      - Summary stats row (total, average rating, per-star counts)
      - Star filter `<select>` bound to `_feedbackStarFilter`
      - Paginated table with columns: Date/Time (IST), Rating (star chars), Comment (truncated 100 chars + expand), Actions (delete with confirm)
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6_

  - [ ]* 12.3 Write property tests for Admin Feedback Tab
    - **Property 16: Feedback Comment Truncation** — for any `Comment` of length `n`, table renders ≤ 100 chars; "Show more" present iff `n > 100`
    - **Property 17: Admin Summary Stats Correctness** — `total`, `avgRating`, `perStar[k]` satisfy the correctness equations for any seeded list
    - **Property 18: Star Filter Exclusivity** — for any filter `f ∈ [1,5]`, all records in `FilteredFeedback` have `Rating == f`
    - **Validates: Requirements 7.2, 7.3, 7.6**

- [ ] 13. Responsive CSS for all new sections
  - [ ] 13.1 Append responsive breakpoint rules to `app.css`
    - Add tablet (768–1023 px) media query overrides for: gallery grid column count, BMI form field layout, chip row wrapping, hotspot map sizing, feedback widget layout
    - Add mobile (< 768 px) media query overrides for: single-column gallery grid, full-width BMI inputs, stacked chip row, full-width feedback textarea, admin table horizontal scroll
    - Verify no horizontal overflow at any breakpoint for all new sections
    - _Requirements: 4.8, 1.1, 2.1, 3.1, 6.1, 8.1_

- [ ] 14. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

---

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- All CSS changes are appended to `app.css` and scoped exclusively under `.ac-page` — no existing rules are modified
- All JS changes are appended to `site.js` — no existing functions are removed except `destroyAestheticAnimations` which is updated in place
- The BMI Estimator is fully client-side; it MUST NOT write any data to the database
- `CloseLightbox()` must carry the `[JSInvokable]` attribute to allow ESC key interop from `site.js`
- IST timezone conversion in AdminDashboard uses `TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata")` (Linux) or `"India Standard Time"` (Windows); use a helper that tries both
- Each task references specific requirements for full traceability
- Checkpoints ensure incremental validation after key milestones

---

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3", "2.1"] },
    { "id": 1, "tasks": ["2.2", "5.1"] },
    { "id": 2, "tasks": ["2.3", "5.2", "5.3"] },
    { "id": 3, "tasks": ["3.1", "3.3", "3.4", "6.1"] },
    { "id": 4, "tasks": ["3.2", "3.5", "6.2", "6.3", "7.1"] },
    { "id": 5, "tasks": ["7.2", "7.3", "8.1"] },
    { "id": 6, "tasks": ["8.2", "8.3", "8.4", "9.1"] },
    { "id": 7, "tasks": ["9.2", "9.3", "9.4", "11.1"] },
    { "id": 8, "tasks": ["11.2", "11.3", "11.4", "12.1"] },
    { "id": 9, "tasks": ["12.2", "12.3", "13.1"] }
  ]
}
```
