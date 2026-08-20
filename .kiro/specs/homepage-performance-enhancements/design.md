# Homepage Performance Enhancements — Design

## Goals
Reduce server load and improve Core Web Vitals (CLS, LCP, TBT) for the ProTouch homepage without changing visible behaviour for the end user.

---

## Architecture

### 1. Hero Slideshow → Pure CSS
**Before:** A `System.Threading.Timer` fires every 5 s on the server, calls `StateHasChanged()`, and the Blazor circuit sends a diff to the client.

**After:** Three `<img>` slides are rendered statically. A CSS `@keyframes` animation with `animation-delay` staggered per slide drives the cross-fade entirely on the GPU. No server roundtrip.

```
Slide 1: animation: hero-slide 15s infinite 0s
Slide 2: animation: hero-slide 15s infinite 5s
Slide 3: animation: hero-slide 15s infinite 10s
```

Slides use `opacity` to fade in and out. `prefers-reduced-motion` disables the animation.

### 2. Testimonials → IntersectionObserver + setInterval
**Before:** `System.Threading.Timer` fires every 6 s on the server.

**After:**
- `window.initTestiObserver(dotNetRef)` sets up an `IntersectionObserver` on `.ac-testimonials`.
- When the section enters the viewport, a `setInterval(6000)` is created; each tick calls `dotNetRef.invokeMethodAsync('AdvanceTesti')`.
- When the section leaves, `clearInterval` is called → zero CPU usage while off-screen.
- `window.destroyTestiObserver()` fully cleans up for SPA navigation.

### 3. SVG Hotspot Map Removal
The map added complexity, was not part of the core value proposition, and added unnecessary DOM nodes. Removing it simplifies the service panel to a single `<img>`.

### 4. WebP Conversion Script
PowerShell script using ImageMagick `magick convert` (widely available on CI/CD). Skips already-converted files by comparing `LastWriteTime`. Outputs summary table.

### 5. `<picture>` Elements
Every `<img src="/images/...">` is wrapped:
```html
<picture>
  <source srcset="/path/to/image.webp" type="image/webp">
  <img src="/path/to/image.jpeg" alt="..." width="W" height="H" loading="lazy">
</picture>
```
The browser picks WebP automatically if supported; JPEG is the fallback. Explicit dimensions prevent CLS.

### 6. System Font Stack
Removes the two Google Fonts network requests (≈ 200-400 ms on first visit):
```css
--ac-font-body:  -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
--ac-font-serif: Georgia, 'Times New Roman', serif;
```

### 7. Deferred JS Init
`window.scheduleDeferredInit_home` uses `requestIdleCallback` (Chrome/Edge) with a `setTimeout(fn, 200)` fallback (Safari/Firefox) to defer scroll-dependent features until after the main thread is idle. Critical path:
```
initStickyHeader → immediate
initGalleryLightbox → immediate
initTestiObserver → immediate
--- idle callback boundary ---
initScrollReveal → deferred
initGuaranteeTicker → deferred
initBackToTop → deferred
initActiveNavHighlight → deferred
```

### 8. Mobile CSS
Standard media queries scoped to `.ac-page` targeting the breakpoints already used by the project (768 px, 480 px).

---

## Files Changed
| File | Change |
|------|--------|
| `Home.razor` | Remove timers, add `AdvanceTesti`, wrap images, remove hotspot |
| `App.razor` | Remove Google Fonts links |
| `wwwroot/app.css` | Hero CSS animation, hotspot removal comment, system fonts, mobile CSS |
| `wwwroot/site.js` | Add `initTestiObserver`, `destroyTestiObserver`, `scheduleDeferredInit_home` |
| `scripts/Convert-ImagesToWebP.ps1` | New file |
