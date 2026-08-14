# Implementation Plan: Homepage Redesign — Sri Sai Protouch

## Overview

All changes are contained within exactly two files:
- **`ClinicApp/Components/Pages/Home.razor`** — upgraded to `@rendermode InteractiveServer`, `@implements IDisposable`, new data models and component state.
- **`ClinicApp/wwwroot/app.css`** — new CSS rules scoped under `.wellness-page` only.

The implementation proceeds in six areas: color palette & typography foundations first (CSS-only, no C# risk), then navigation, then the data models and carousel, then the hero sub-carousel, then the service detail panels, and finally wiring and cleanup. A bUnit + FsCheck test project is set up alongside the first C# change.

---

## Tasks

- [ ] 1. Update color palette and typography in `app.css`
  - [ ] 1.1 Apply light green-accented color palette changes
    - In `app.css`, under the `.wellness-page` scope, update the following rules:
      - `.wellness-page` background: change `#f7f6ef` → `#f5f9f6`
      - `.guarantee-strip` background: change `#7c1b68` → `#008d36`; set `color: white`
      - `.services` section background: change `#f2f0e9` → `#eef6f1`
      - `.portal-panel` background: change `#dffbef` → `#e8f5ed`
    - Verify all new rules sit inside `.wellness-page` selectors so admin/dashboard styles are unaffected
    - _Requirements: 4.1, 4.2, 4.4, 4.5, 4.7_

  - [ ] 1.2 Apply enhanced typography rules
    - Add `@import` for "Playfair Display" from Google Fonts alongside the existing Outfit import in `app.css`
    - Inside `.wellness-page`, add/update rules:
      - `.wellness-page h1, .wellness-page h2` → `font-family: 'Playfair Display', Georgia, serif`
      - Hero `h1`: `line-height: 1.05; letter-spacing: -0.03em`
      - `h2`: `font-weight: 700; line-height: 1.1; letter-spacing: -0.02em`
      - `.large-copy`: `font-size: 1.2rem; line-height: 1.72; color: #3d3640`
      - `.muted-copy`: `font-size: 0.97rem; line-height: 1.72; color: #6b636d`
      - `.section-kicker`: `letter-spacing: 0.18em; font-weight: 900`
      - `.guarantee-strip` text: `font-size: 0.88rem; font-weight: 800`
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_

- [ ] 2. Update navigation bar styles in `app.css`
  - [ ] 2.1 Apply `.wellness-header` CSS enhancements
    - Scope all new rules under `.wellness-page .wellness-header` in `app.css`:
      - Set `background: #ffffff` and `box-shadow: 0 2px 12px rgba(0,0,0,0.07)`
      - Navigation `<a>` links: `color: #2a2030; font-weight: 700; transition: color 0.2s ease`
      - Navigation `<a>:hover`: `color: #008d36; border-bottom: 2px solid #008d36`
      - `.wellness-call:hover`: `background: #006d2a`
    - Ensure the new `.wellness-page .wellness-header` rules do not touch `.navbar-clinic` (admin nav)
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.7_

- [ ] 3. Set up data models and component scaffolding in `Home.razor`
  - [ ] 3.1 Add component directives and define data records
    - At the top of `Home.razor`, add:
      - `@rendermode InteractiveServer`
      - `@implements IDisposable`
    - Inside the `@code {}` block, define private records:
      - `record CarouselSlide(string ImagePath, string Caption)`
      - `record ServiceDetail(string ThumbnailPath, string[] Treatments, string[] Outcomes, string[] SubServices)`
      - `record ServiceDefinition(string Id, string Number, string Name, string Summary, string[] Bullets, ServiceDetail Detail)`
    - Declare all state fields with their defaults (see design Data Models table):
      `carouselSlides`, `activeSlideIndex`, `_carouselTimer`, `heroImages`, `activeHeroIndex`, `_heroTimer`, `expandedService`, `serviceDefinitions`
    - Remove the existing `galleryImages` field and its `OnInitialized` list population loop
    - _Requirements: 1.3, 1.10, 2.2, 7.6, 7.7_

  - [ ] 3.2 Populate static data in `OnInitialized()`
    - Initialise `carouselSlides` with the 12 curated slides from the design doc (non-sequential order, 3 slides per category interleaved)
    - Initialise `heroImages` with the 5 paths listed in the design doc (ending with `hero-treatment.jpeg`)
    - Initialise `serviceDefinitions` with all 4 service entries including full `ServiceDetail` content:
      - "Weight Loss & Slimming": LPG Endermologie, RF therapy, body wraps, cryotherapy; 4–8 kg/month, 2–5 inches/session; figure correction, body shaping, toning, skin tightening, breast correction, body spa
      - "Physiotherapy & Rehabilitation": back/neck/knee pain, arthritis, spondylitis, sinusitis, paralysis, stress/strain; electrotherapy, ultrasound, TENS, manual therapy; orthopaedic, neurological, sports injury
      - "Yoga & Holistic Wellness": diabetes, blood pressure, thyroid, cholesterol, varicose veins, hormonal health; menstrual wellness, pre-natal, post-natal; personalised plans
      - "Specialised Programs": height increase program (up to 23 years, 1–3 inches growth), sports rehabilitation, clinician-guided plans
    - _Requirements: 1.4, 2.6, 2.7, 2.8, 2.9, 7.5_

  - [ ] 3.3 Implement timer lifecycle methods
    - In `OnInitialized()` (or `OnAfterRender` if SSR timing requires it), create both timers:
      - `_carouselTimer = new Timer(_ => AdvanceSlide(), null, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(4))`
      - `_heroTimer = new Timer(_ => AdvanceHero(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5))`
    - Implement `AdvanceSlide()`: guards on empty list; advances `activeSlideIndex = (activeSlideIndex + 1) % carouselSlides.Count`; calls `InvokeAsync(StateHasChanged)` inside try/catch swallowing `ObjectDisposedException`
    - Implement `AdvanceHero()`: same pattern for `activeHeroIndex` and `heroImages`
    - Implement `Dispose()`: disposes both timers inside a try/catch
    - _Requirements: 1.3, 1.5, 1.10, 7.7_

  - [ ]* 3.4 Write property test for slide index advancement wraps (PBT-1)
    - Set up a `ClinicApp.Tests` xUnit project with bUnit and FsCheck NuGet packages if not already present
    - **Property 1: Slide Index Advancement Wraps**
    - Generator: random `listLength` ∈ [1, 60], random `startIndex` ∈ [0, listLength-1]
    - Assert `(startIndex + 1) % listLength` is in [0, listLength-1]; assert wrap-around when `startIndex == listLength - 1`
    - **Validates: Requirements 1.5**
    - _Minimum 100 iterations_

- [ ] 4. Implement the main image carousel
  - [ ] 4.1 Add carousel CSS rules to `app.css`
    - Under `.wellness-page` in `app.css`, add `.carousel-section`, `.carousel-track`, `.carousel-slide`, `.carousel-slide.active`, `.carousel-caption`, `.carousel-dots`, `.carousel-dot`, and `.carousel-dot.active` rules:
      - Track: `position: relative; overflow: hidden`
      - Slides: `position: absolute; top: 0; left: 0; width: 100%; height: 100%; opacity: 0; transition: opacity 0.6s ease`
      - Active slide: `opacity: 1; position: relative`
      - Aspect ratio enforcement: `aspect-ratio: 16/9` on `.carousel-section`
      - Caption overlay: `position: absolute; bottom: 0; left: 0; right: 0; background: rgba(0,0,0,0.45); color: #fff; padding: 0.75rem 1rem`
      - Dots: positioned below the image, `display: flex; justify-content: center; gap: 8px; padding: 12px 0`
      - Each dot: `width: 10px; height: 10px; border-radius: 50%; background: rgba(0,141,54,0.3); border: none; cursor: pointer`
      - Active dot: `background: #008d36`
      - Responsive at ≤768px: maintain `aspect-ratio: 16/9`, no `overflow-x`
    - _Requirements: 1.2, 1.6, 1.9, 1.11_

  - [ ] 4.2 Implement carousel markup in `Home.razor`
    - Replace the existing `<section id="gallery">` block (the `gallery-grid` div and its `@foreach` over `galleryImages`) with the carousel HTML structure from the design doc
    - Render one `.carousel-slide` per entry in `carouselSlides`; apply `active` class when `i == activeSlideIndex`
    - Render `.carousel-caption` overlay only when `slide.Caption` is non-empty
    - Add `onerror="this.style.opacity='0'"` on each slide `<img>`
    - Render `.carousel-dots` section with a `<button class="carousel-dot">` per slide; bind `@onclick="() => JumpToSlide(idx)"`
    - Implement `JumpToSlide(int idx)`: bounds-check, set `activeSlideIndex = idx`, dispose and recreate `_carouselTimer`
    - Add `@if (carouselSlides.Count > 0)` guard around the entire carousel section
    - _Requirements: 1.1, 1.2, 1.3, 1.6, 1.7, 1.8, 1.11, 7.1, 7.5, 7.6_

  - [ ]* 4.3 Write property test for dot indicator count matches slides (PBT-2)
    - **Property 2: Dot Indicator Count Matches Slides**
    - Generator: `List<CarouselSlide>` of random length N ∈ [1, 30]
    - Render `Home` component via bUnit with the generated list injected
    - Assert number of `.carousel-dot` elements == N; assert exactly one `.carousel-dot.active` at position `activeSlideIndex`
    - **Validates: Requirements 1.6**

  - [ ]* 4.4 Write property test for caption overlay conditional rendering (PBT-3)
    - **Property 3: Caption Overlay Conditional Rendering**
    - Generator: `CarouselSlide` with 50% chance empty Caption, 50% chance random non-empty string (1–60 chars)
    - Render component with single-slide carousel; assert `.carousel-caption` present iff Caption is non-empty, text equals Caption
    - **Validates: Requirements 1.11, 7.6**

- [ ] 5. Checkpoint — verify carousel and foundations
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 6. Implement the hero image sub-carousel
  - [ ] 6.1 Add hero sub-carousel CSS rules to `app.css`
    - Inside `.wellness-page .hero-photo`, add positioning context: `position: relative; overflow: hidden`
    - Add `.hero-slide`: `position: absolute; width: 100%; height: 100%; object-fit: cover; border-radius: 4px 58px 4px 58px; opacity: 0; transition: opacity 0.7s ease; filter: saturate(0.85)`
    - Add `.hero-slide.active`: `opacity: 1; position: relative`
    - Add green overlay via pseudo-element or direct CSS: `:after { background: rgba(0,141,54,0.08) }` on `.hero-photo`
    - Add `.hero-photo.hero-fallback`: `background: linear-gradient(135deg, #008d36 0%, #ffffff 100%); min-height: 460px`
    - Responsive at ≤850px: hero stacks to single column; image becomes full-width at `height: 280px`
    - _Requirements: 3.1, 3.3, 3.4, 3.5_

  - [ ] 6.2 Implement hero sub-carousel markup in `Home.razor`
    - Replace the single `<img src="/images/clinic-gallery/hero-treatment.jpeg" …>` inside `.hero-photo` with `@foreach` over `heroImages`, rendering each as `<img class="hero-slide @(i == activeHeroIndex ? "active" : "")" src="@img" … onerror="this.style.display='none'; this.parentElement.classList.add('hero-fallback')" />`
    - Keep the existing `.hero-badge` element inside `.hero-photo` unchanged
    - Verify all three CTA buttons ("Book an Appointment", "WhatsApp Us", "Talk to us") and the ISO mark remain unmodified
    - _Requirements: 3.2, 3.4, 3.5, 3.6, 7.2_

- [ ] 7. Implement service card detail expansion
  - [ ] 7.1 Add service detail panel CSS to `app.css`
    - Under `.wellness-page .service-grid-v2`, add rules for `.service-article` (the new `<button>` wrapper):
      - Reset button styles: `background: none; border: none; text-align: left; width: 100%; cursor: pointer; padding: 0`
      - Add hover/focus ring matching the existing article style
      - Add `.service-article.expanded` with a bottom border accent in `#008d36`
    - Add `.service-detail-panel`:
      - `max-height: 0; overflow: hidden; transition: max-height 0.4s ease`
    - Add `.service-detail-panel.open`:
      - `max-height: 900px` (large fixed value for smooth CSS animation)
    - Style panel internals: padding, thumbnail `<img>` at ~120px width, treatment list styling, "Book Now" button in `#008d36`
    - Ensure panel is readable at 320px width with no horizontal overflow
    - _Requirements: 2.5, 2.10_

  - [ ] 7.2 Implement service card toggle and detail panels in `Home.razor`
    - Replace the current static `<article>` elements in `.service-grid-v2` with `@foreach` over `serviceDefinitions`
    - Each service renders as a `<div>` containing:
      - A `<button class="service-article @(expandedService == svc.Id ? "expanded" : "")" @onclick="() => ToggleService(svc.Id)" aria-expanded="@(expandedService == svc.Id)">`
      - A `<div class="service-detail-panel @(expandedService == svc.Id ? "open" : "")">` with thumbnail `<img src="@svc.Detail.ThumbnailPath">`, treatments/outcomes/sub-services lists, and a `<a href="/book" class="btn-clinic">Book Now →</a>`
    - Implement `ToggleService(string id)`: `expandedService = (expandedService == id) ? null : id`
    - Ensure keyboard operability: button elements are natively keyboard-focusable; no extra JS needed
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.6, 2.7, 2.8, 2.9, 2.11, 7.3, 7.4_

  - [ ]* 7.3 Write property test for service panel toggle idempotence (PBT-4)
    - **Property 4: Service Panel Toggle Idempotence**
    - Generator: random service ID from `{ "weight-loss", "physio", "yoga", "specialised" }`
    - Start with `expandedService = null`; call `ToggleService(id)` → assert `expandedService == id`; call again → assert `expandedService == null`
    - **Validates: Requirements 2.3**

  - [ ]* 7.4 Write property test for service panel exclusive expansion (PBT-5)
    - **Property 5: Service Panel Exclusive Expansion**
    - Generator: two distinct IDs A and B from the 4 known service IDs
    - Set `expandedService = A`; call `ToggleService(B)`; assert `expandedService == B && expandedService != A`
    - **Validates: Requirements 2.4**

  - [ ]* 7.5 Write property test for service detail panel gallery thumbnail (PBT-6)
    - **Property 6: Service Detail Panel Contains Gallery Thumbnail**
    - Generator: random `ServiceDefinition` from the 4 defined services
    - Render component with `expandedService = svc.Id`; find `.service-detail-panel.open img` elements
    - Assert exactly one `<img>` in the open panel; assert its `src` starts with `/images/clinic-gallery/`
    - **Validates: Requirements 7.4**

- [ ] 8. Checkpoint — verify service panels and hero carousel
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 9. Write example-based unit tests
  - [ ]* 9.1 Write bUnit example-based tests covering static content and initialisation
    - `CarouselStartsOnFirstSlide`: assert `activeSlideIndex == 0` on init
    - `CarouselHasMinimum12Slides`: assert `carouselSlides.Count >= 12`
    - `CarouselCoversAllFourCategories`: assert all 4 category caption strings appear in slide list
    - `DotClickJumpsToSlide`: call `JumpToSlide(3)`, assert `activeSlideIndex == 3`
    - `HeroHas3to5Images`: assert `heroImages.Count` in [3, 5]
    - `HeroFallbackOnImageError`: assert `onerror` attribute adds `hero-fallback` class to parent
    - `HeroPreservesAllCTAs`: assert "Book an Appointment", "WhatsApp Us", "Talk to us" links and ISO mark present
    - `WeightLossDetailContent`: assert "LPG Endermologie", "RF therapy", "body wraps", "cryotherapy" in markup when panel open
    - `PhysioDetailContent`: assert "back pain", "TENS", "orthopaedic" present
    - `YogaDetailContent`: assert "diabetes", "menstrual wellness", "pre-natal" present
    - `SpecialisedDetailContent`: assert "height increase", "23 years", "sports rehabilitation" present
    - `ExpandedPanelDefaultIsNull`: assert `expandedService == null` on init
    - `GalleryGridRemoved`: assert no element with class `gallery-grid` in rendered output
    - `CarouselSlidesNotSequential`: assert first 5 slides are not `gallery-01` through `gallery-05`
    - `IDisposableImplemented`: assert component type implements `IDisposable`
    - _Requirements: 1.1, 1.4, 1.7, 1.10, 2.2, 2.6, 2.7, 2.8, 2.9, 3.2, 3.4, 3.6, 7.1, 7.5, 7.7_

- [ ] 10. Final wiring and cleanup
  - [ ] 10.1 Remove `galleryImages` field and verify full integration
    - Confirm `galleryImages` field and its `OnInitialized` population loop are deleted from `@code {}`
    - Confirm `carouselSlides` is typed as `List<CarouselSlide>` (not `List<string>`)
    - Confirm `@rendermode InteractiveServer` directive is present at the top of `Home.razor`
    - Confirm `@implements IDisposable` is present and `Dispose()` disposes both `_carouselTimer` and `_heroTimer`
    - Smoke-check the nav: `.wellness-header` CSS uses `background: #ffffff`; no changes to `.navbar-clinic` rules
    - _Requirements: 1.3, 1.10, 7.6, 7.7_

- [ ] 11. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

---

## Notes

- Tasks marked with `*` are optional and can be skipped for a faster MVP; the core feature is fully functional without them.
- All C# code changes are in `Home.razor`; all visual changes are in `app.css`. No other files should be created or modified.
- The testing stack is bUnit (component rendering) + FsCheck (property-based generators). Run with `dotnet test` in the `ClinicApp.Tests` project.
- Timer callbacks must use `InvokeAsync(StateHasChanged)` (not `StateHasChanged()` directly) because they fire on thread-pool threads, not the Blazor synchronisation context.
- The `@rendermode InteractiveServer` directive is required for `System.Threading.Timer` to work; without it, the component renders statically and timers have no effect.
- Checkpoints at Tasks 5 and 8 are lightweight — run `dotnet test` and visually verify the page in a browser before moving on.
- Each task references specific requirements for traceability.

---

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "2.1"] },
    { "id": 1, "tasks": ["3.1"] },
    { "id": 2, "tasks": ["3.2", "3.3"] },
    { "id": 3, "tasks": ["3.4", "4.1", "6.1", "7.1"] },
    { "id": 4, "tasks": ["4.2", "6.2", "7.2"] },
    { "id": 5, "tasks": ["4.3", "4.4", "7.3", "7.4", "7.5"] },
    { "id": 6, "tasks": ["9.1", "10.1"] }
  ]
}
```
