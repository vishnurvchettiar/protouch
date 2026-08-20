# Homepage Performance Enhancements — Requirements

## Overview
This spec captures the performance, maintainability and mobile-experience improvements
for the ProTouch Physio Clinic homepage (`Home.razor`).

---

## Requirements

### 1. Remove Server-Side Hero Animation Timer
- **REQ-1.1** The hero slideshow MUST be driven by a CSS `@keyframes` animation rather than a C# `System.Threading.Timer`.
- **REQ-1.2** The hero image list MUST contain exactly 3 entries (`gallery-37`, `gallery-42`, `gallery-55`).
- **REQ-1.3** Under `prefers-reduced-motion: reduce`, the CSS animation MUST be disabled so all hero slides are visible or the first slide is shown statically.
- **REQ-1.4** The `_heroTimer` field and its `Dispose` call MUST be removed from `Home.razor`.

### 2. Move Testimonial Auto-Advance Off Server
- **REQ-2.1** The testimonial carousel MUST auto-advance via a client-side `setInterval` (6000 ms), not a server `System.Threading.Timer`.
- **REQ-2.2** The interval MUST only run while the testimonials section is visible in the viewport (IntersectionObserver).
- **REQ-2.3** The interval MUST be cleared when the section leaves the viewport, and a cleanup function (`destroyTestiObserver`) MUST be exposed on `window`.
- **REQ-2.4** The `_testiTimer` field, its instantiation, its `Dispose` call, and all `_testiTimer?.Change(...)` calls MUST be removed from `Home.razor`.
- **REQ-2.5** A `[JSInvokable] AdvanceTesti()` method MUST be added to invoke carousel advancement from JavaScript.

### 3. Remove SVG Hotspot Map
- **REQ-3.1** The `<div class="ac-hotspot-wrap">` element and its SVG child MUST be removed from the service panel markup.
- **REQ-3.2** The `_hoveredHotspot` field MUST be deleted from `Home.razor`.
- **REQ-3.3** The `.ac-hotspot-wrap`, `.ac-body-svg`, `.ac-body-outline`, and `.ac-hotspot` CSS rules MUST be commented out or removed in `app.css`.

### 4. WebP Image Conversion Script
- **REQ-4.1** A PowerShell script MUST exist at `scripts/Convert-ImagesToWebP.ps1`.
- **REQ-4.2** The script MUST recursively discover all `*.jpeg` and `*.jpg` files under `wwwroot/images/`.
- **REQ-4.3** The script MUST skip conversion if a `.webp` file already exists and is newer than the source.
- **REQ-4.4** The script MUST print per-file status and a final summary (Converted / Skipped / Failed counts).
- **REQ-4.5** The script MUST use ImageMagick `magick` if available, falling back to a documented alternative.

### 5. WebP `<picture>` Elements
- **REQ-5.1** Every `/images/` `<img>` tag on the homepage MUST be wrapped in a `<picture>` element with a `<source type="image/webp">` and a JPEG/fallback `<img>`.
- **REQ-5.2** Explicit `width` and `height` attributes MUST be added to all `<img>` elements to prevent layout shift (CLS).
- **REQ-5.3** Above-the-fold images (first hero slide, logo) MUST use `loading="eager"` and `fetchpriority="high"`.
- **REQ-5.4** Below-the-fold images MUST use `loading="lazy"`.

### 6. Remove Google Fonts
- **REQ-6.1** The three Google Fonts `<link>` tags MUST be removed from `App.razor`.
- **REQ-6.2** All `font-family: 'Outfit'` and `font-family: 'Playfair Display'` declarations MUST be replaced with system font stacks in `app.css`.
- **REQ-6.3** CSS custom properties `--ac-font-body` and `--ac-font-serif` in `.ac-page` MUST be updated to system stacks.

### 7. Defer Non-Critical JavaScript
- **REQ-7.1** Critical JS initialisations (`initStickyHeader`, `initGalleryLightbox`, `initTestiObserver`) MUST run immediately in `OnAfterRenderAsync`.
- **REQ-7.2** Non-critical JS (`initScrollReveal`, `initGuaranteeTicker`, `initBackToTop`, `initActiveNavHighlight`) MUST be deferred using `requestIdleCallback` (or `setTimeout` fallback).
- **REQ-7.3** A `window.scheduleDeferredInit_home` function MUST be defined in `site.js` encapsulating all non-critical inits.

### 8. Mobile Responsive CSS
- **REQ-8.1** The hero section MUST collapse to a single column on screens ≤ 768 px.
- **REQ-8.2** Hero heading font size MUST use `clamp()` for fluid scaling on mobile.
- **REQ-8.3** CTA buttons MUST stack vertically on screens ≤ 480 px with a minimum touch target of 44 px.
- **REQ-8.4** The gallery grid MUST display 2 columns at ≤ 768 px and 1 column at ≤ 480 px.
- **REQ-8.5** Floating CTAs MUST be hidden while the lightbox is open.
