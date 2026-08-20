# Homepage Performance Enhancements — Tasks

## Task 1: Remove _heroTimer → CSS animation
- [ ] Trim heroImages list to exactly 3 entries in Home.razor
- [ ] Delete `_heroTimer` field declaration
- [ ] Remove System.Threading.Timer instantiation for `_heroTimer` from OnAfterRenderAsync
- [ ] Remove `_heroTimer?.Dispose()` from DisposeAsync
- [ ] Add hero-slide CSS @keyframes + per-slide animation-delay in app.css
- [ ] Add prefers-reduced-motion disable rule

## Task 2: Remove _testiTimer → IntersectionObserver + rAF
- [ ] Delete `_testiTimer` field
- [ ] Remove System.Threading.Timer block for `_testiTimer` from OnAfterRenderAsync
- [ ] Remove `_testiTimer?.Dispose()` from DisposeAsync
- [ ] Remove all `_testiTimer?.Change(...)` calls in NextTesti, PrevTesti, JumpToTesti
- [ ] Add `[JSInvokable] AdvanceTesti()` method
- [ ] Add `window.initTestiObserver(dotNetRef)` in site.js
- [ ] Add `window.destroyTestiObserver()` in site.js
- [ ] Call `initTestiObserver` in OnAfterRenderAsync
- [ ] Add `destroyTestiObserver` call in DisposeAsync

## Task 3: Remove SVG hotspot map
- [ ] Remove `<div class="ac-hotspot-wrap">` element from Home.razor
- [ ] Delete `_hoveredHotspot` field
- [ ] Comment out hotspot CSS rules in app.css

## Task 4: Create WebP image conversion script
- [ ] Create scripts/Convert-ImagesToWebP.ps1

## Task 5 (Checkpoint): Verify Tasks 1–4 compile
- [ ] Run dotnet build

## Task 6: Wrap images in <picture> elements
- [ ] Add @using System.IO to Home.razor
- [ ] Wrap logo img
- [ ] Wrap hero slides
- [ ] Wrap doctor img
- [ ] Wrap service panel img
- [ ] Wrap gallery grid imgs
- [ ] Wrap lightbox img

## Task 7: Remove Google Fonts → system font stack
- [ ] Remove three Google Fonts link tags from App.razor
- [ ] Replace font-family declarations in app.css

## Task 8: Defer non-critical JS init
- [ ] Add `window.scheduleDeferredInit_home` in site.js
- [ ] Refactor OnAfterRenderAsync in Home.razor

## Task 9 (Checkpoint): Verify Tasks 6–8 compile
- [ ] Run dotnet build

## Task 10: Mobile responsive CSS
- [ ] Append mobile responsive rules to app.css

## Task 11 (Final Checkpoint): Run dotnet build
- [ ] Final build verification
