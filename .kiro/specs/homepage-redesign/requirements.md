# Requirements Document

## Introduction

This feature enhances the public-facing homepage (`Home.razor`) of the Sri Sai Protouch clinic website. The goal is to transform the page from a static, text-heavy layout into a visually engaging, modern wellness landing page. Key changes include: a continuous auto-rotating carousel replacing the static gallery grid, expandable service detail panels, a refreshed hero section using existing clinic images, a lighter green-accented color palette, improved typographic hierarchy, and accented navigation elements. All changes are constrained to `Home.razor` and `app.css`; no new back-end services, database queries, or external JavaScript libraries are introduced.

---

## Glossary

- **Carousel**: An auto-rotating visual component that cycles through a set of image slides with smooth CSS transitions, implemented using a Blazor `System.Threading.Timer` and CSS animations.
- **Service_Card**: A clickable UI element representing one of the four clinic service categories displayed in the services section.
- **Service_Detail_Panel**: An expanded in-page view that opens below or within a Service_Card when it is selected, revealing detailed information about that service.
- **Home_Page**: The Blazor Server component at `/projects/sandbox/protouch/ClinicApp/Components/Pages/Home.razor`, publicly accessible without authentication.
- **Carousel_Slide**: A single frame within the Carousel consisting of a clinic gallery image and an optional text overlay.
- **Gallery_Images**: The 60 JPEG files located at `/images/clinic-gallery/gallery-01.jpeg` through `gallery-60.jpeg`, plus `hero-treatment.jpeg` and `care-panel.jpeg`.
- **Hero_Section**: The topmost full-width visual section of the Home_Page containing a headline, subheading, call-to-action buttons, and a featured image.
- **Guarantee_Strip**: The horizontal band below the Hero_Section displaying the five trust guarantees ("No Fasting", "No Medicine", etc.).
- **Nav_Bar**: The `<header>` element at the top of the Home_Page containing the logo, navigation links, and the call button.
- **Wellness_Page**: The outermost `<div class="wellness-page">` container that defines the light-themed public page scope, distinct from the dark admin dashboard.
- **CSS_Timer**: A `System.Threading.Timer` instance created within the Blazor component's `@code {}` block, used to advance the Carousel automatically.

---

## Requirements

---

### Requirement 1: Auto-Rotating Image Carousel

**User Story:** As a site visitor, I want to see a continuous auto-rotating carousel of clinic images and service posters, so that the page feels dynamic and I can get a rich visual impression of the clinic without scrolling through a static grid.

#### Acceptance Criteria

1. WHEN the Home_Page loads, THE Carousel SHALL begin auto-rotating through a curated set of Carousel_Slides without any user interaction required.
2. THE Carousel SHALL display one Carousel_Slide at a time and transition to the next slide every 4 seconds using a CSS `opacity` fade or `transform: translateX` slide animation.
3. THE Carousel SHALL use a CSS_Timer (`System.Threading.Timer`) defined in the `@code {}` block of Home.razor to advance the active slide index and call `StateHasChanged()`.
4. THE Carousel SHALL include at minimum 12 Carousel_Slides drawn from the Gallery_Images set, distributed to represent all four service categories (Weight Loss, Physiotherapy, Yoga, Specialised Programs).
5. WHEN the CSS_Timer fires, THE Carousel SHALL advance the active slide index by 1, wrapping back to index 0 after the final slide.
6. THE Carousel SHALL render dot or dash indicators below the image area showing the total number of slides and highlighting the currently active slide.
7. WHEN a visitor clicks a dot indicator, THE Carousel SHALL immediately jump to the corresponding Carousel_Slide and reset the auto-rotation timer interval.
8. THE Carousel SHALL replace the existing static gallery grid (`<div class="gallery-grid">`) and the associated `galleryImages` list currently rendered in the `@foreach` loop in Home.razor.
9. WHERE the visitor's viewport width is 768 px or less, THE Carousel SHALL maintain a 16:9 aspect ratio and remain fully visible without horizontal overflow.
10. IF the CSS_Timer encounters a disposal error during component teardown, THEN THE Home_Page SHALL suppress the exception and dispose the timer cleanly by implementing `IDisposable` in the `@code {}` block.
11. THE Carousel SHALL optionally display a short text label on each Carousel_Slide identifying the service category depicted, rendered as a semi-transparent overlay at the bottom of the image.

---

### Requirement 2: Service Card Detail Expansion

**User Story:** As a site visitor, I want to click on a service card and see detailed information about that service, so that I can make an informed decision before booking an appointment.

#### Acceptance Criteria

1. WHEN a visitor clicks a Service_Card, THE Home_Page SHALL expand a Service_Detail_Panel directly below that card, displaying detailed treatment descriptions, key benefits, and a "Book Now" call-to-action link pointing to `/book`.
2. THE Home_Page SHALL track the currently expanded service in a `string? expandedService` field in the `@code {}` block; its default value SHALL be `null` (no panel open on page load).
3. WHEN a visitor clicks a Service_Card that is already expanded, THE Home_Page SHALL collapse the Service_Detail_Panel and set `expandedService` to `null`.
4. WHEN a visitor clicks a different Service_Card while one Service_Detail_Panel is already open, THE Home_Page SHALL close the currently open panel and open the new panel for the clicked card.
5. THE Service_Detail_Panel SHALL animate its appearance using a CSS `max-height` transition from `0` to `auto` (or a fixed large value), providing a smooth expand/collapse effect.
6. THE Service_Detail_Panel for "Weight Loss & Slimming" SHALL include: treatments offered (LPG Endermologie, RF therapy, body wraps, cryotherapy), expected outcomes (4–8 kg/month, 2–5 inches/session), and a list of sub-services (figure correction, body shaping, toning, skin tightening, breast correction, body spa).
7. THE Service_Detail_Panel for "Physiotherapy & Rehabilitation" SHALL include: conditions treated (back pain, neck pain, knee pain, arthritis, spondylitis, sinusitis, paralysis, stress/strain), therapy modalities (electrotherapy, ultrasound, TENS, manual therapy), and specialties (orthopaedic, neurological, sports injury).
8. THE Service_Detail_Panel for "Yoga & Holistic Wellness" SHALL include: health areas covered (diabetes, blood pressure, thyroid, cholesterol, varicose veins, hormonal health), specific programs (menstrual wellness, pre-natal, post-natal), and the personalised nature of the plans.
9. THE Service_Detail_Panel for "Specialised Programs" SHALL include: the height increase program details (up to 23 years, 1–3 inches growth), sports rehabilitation programs, and any other focused clinician-guided plans available.
10. THE Service_Detail_Panel SHALL be fully readable on viewport widths of 320 px and above without horizontal overflow or text truncation.
11. WHERE a visitor navigates via the keyboard (Tab + Enter), THE Service_Card SHALL be operable and THE Service_Detail_Panel SHALL expand or collapse in the same manner as a click event.

---

### Requirement 3: Hero Section Image Enhancement

**User Story:** As a site visitor, I want the hero section to feel visually compelling and professional, so that I immediately understand the clinic's quality and trust it with my health.

#### Acceptance Criteria

1. THE Hero_Section SHALL retain its two-column grid layout (content left, image right) on viewports wider than 850 px.
2. THE Hero_Section SHALL replace the single static `hero-treatment.jpeg` image with a small auto-cycling sub-carousel of 3–5 images drawn from Gallery_Images, advancing every 5 seconds using a separate CSS_Timer or a combined timer with an offset index.
3. THE Hero_Section image area SHALL apply a subtle `object-fit: cover` crop with a `border-radius` matching the existing asymmetric style (4px 58px 4px 58px) and a soft green-tinted overlay (`rgba(0,141,54,0.08)`) to tie the image to the brand palette.
4. IF the hero image sub-carousel images fail to load (404 response), THEN THE Hero_Section SHALL display a solid gradient fallback background using the brand green (#008d36) and white (#ffffff) without broken image icons.
5. WHERE the viewport width is 850 px or less, THE Hero_Section SHALL stack to a single column and the image SHALL be rendered as a full-width banner at 280 px height above the hero copy text.
6. THE Hero_Section SHALL preserve all existing call-to-action buttons ("Book an Appointment", "WhatsApp Us", "Talk to us") and the ISO certification mark without modification to their links or label text.

---

### Requirement 4: Light Green-Accented Color Palette

**User Story:** As a site visitor, I want the homepage to feel clean, fresh, and inviting rather than dark, so that I associate the clinic with health, growth, and professionalism.

#### Acceptance Criteria

1. THE Wellness_Page SHALL use a base background color of `#f5f9f6` (a very light green-white) in place of the current `#f7f6ef` cream, applied via the `.wellness-page` CSS rule in `app.css`.
2. THE Guarantee_Strip SHALL change its background from `#7c1b68` (purple) to the brand green `#008d36`, with white text, to reinforce the health-forward brand identity.
3. THE `.care-copy` section background SHALL remain `#008d36` (green) as currently defined; no change is required.
4. THE `.services` section background SHALL change from `#f2f0e9` to `#eef6f1` (a light green tint) for visual cohesion with the updated palette.
5. THE `.portal-panel` (if present on the page) background SHALL use `#e8f5ed` instead of `#dffbef` for a more muted, professional green tone.
6. THE `.section-kicker` text color SHALL remain `#008d36` across all sections; no change is required.
7. ALL changes to the color palette SHALL be implemented exclusively in the CSS rules within `app.css` under the `.wellness-page` scope, so that dashboard and admin pages are not affected.
8. THE updated palette SHALL maintain a minimum contrast ratio of 4.5:1 between body text and background colors to meet WCAG 2.1 AA accessibility standards.

---

### Requirement 5: Enhanced Typography

**User Story:** As a site visitor, I want the page text to be easy to read and visually engaging, so that I can quickly understand what the clinic offers and feel confident about the quality of care.

#### Acceptance Criteria

1. THE Home_Page SHALL import and apply a secondary display font ("Playfair Display" from Google Fonts) for `<h1>` and `<h2>` elements within `.wellness-page`, while retaining "Outfit" for body text, labels, and navigation.
2. THE hero `<h1>` SHALL render at a minimum `clamp(3rem, 5.2vw, 5.3rem)` font size (unchanged from current value) but SHALL apply `line-height: 1.05` and `letter-spacing: -0.03em` for tighter, more editorial spacing.
3. THE `<h2>` headings in all sections SHALL use `font-weight: 700` and `color: #2f2732` (no change) with `line-height: 1.1` and `letter-spacing: -0.02em`.
4. THE `.large-copy` paragraph class SHALL apply `font-size: 1.2rem`, `line-height: 1.72`, and `color: #3d3640` for improved readability.
5. THE `.muted-copy` paragraph class SHALL apply `font-size: 0.97rem`, `line-height: 1.72`, and `color: #6b636d`.
6. THE `.section-kicker` SHALL apply `letter-spacing: 0.18em` and `font-size: 0.72rem` (unchanged) with `font-weight: 900` and `color: #008d36`.
7. THE `guarantee-strip` text SHALL apply `font-size: 0.88rem` and `font-weight: 800` for clear legibility on the green background.
8. ALL typography changes SHALL be scoped to `.wellness-page` selectors in `app.css` so that the admin dashboard typography is not affected.

---

### Requirement 6: Navigation Color Enhancements

**User Story:** As a site visitor, I want the navigation bar to be visually clear and on-brand, so that I can easily find and click the links I need.

#### Acceptance Criteria

1. THE Nav_Bar SHALL apply a white (`#ffffff`) background with a subtle `box-shadow: 0 2px 12px rgba(0,0,0,0.07)` to distinguish it clearly from the page body on light backgrounds.
2. THE Nav_Bar navigation links SHALL render in `#2a2030` (near-black) at `font-weight: 700` in their default state and transition to `#008d36` (brand green) on hover, with a `transition: color 0.2s ease` animation.
3. WHEN a navigation link is hovered, THE Nav_Bar SHALL display a `2px solid #008d36` bottom border on the link element using `border-bottom` to provide a clear active-state indicator.
4. THE "Call" button (`.wellness-call`) SHALL retain its `#008d36` green background and white text; on hover, THE button SHALL darken to `#006d2a`.
5. WHERE the viewport width is 850 px or less, THE Nav_Bar SHALL hide the navigation link list (already implemented via `display:none` at that breakpoint) and display only the logo and the call button.
6. THE logo image SHALL remain 205 px wide at full viewport and scale to 172 px on viewports of 850 px or less (unchanged from current values).
7. ALL Nav_Bar style changes SHALL be scoped to `.wellness-page .wellness-header` selectors in `app.css` so that admin navigation is not affected.

---

### Requirement 7: Meaningful Gallery Image Integration

**User Story:** As a site visitor, I want to see real clinic images throughout the page rather than in a single dumped grid, so that the page feels authentic and the visuals support the content they appear next to.

#### Acceptance Criteria

1. THE Home_Page SHALL remove the standalone `<section id="gallery">` grid block (currently rendering up to 60 images in a `gallery-grid` div) and replace it with the Carousel defined in Requirement 1.
2. THE Hero_Section SHALL display 3–5 Gallery_Images in the hero image sub-carousel as specified in Requirement 3, Acceptance Criterion 2.
3. THE `.care-panel` section SHALL continue to use `care-panel.jpeg` as its full-height left-column image; no change is required for this image.
4. THE Service_Detail_Panel for each service (Requirement 2) SHALL include one representative Gallery_Image displayed as a thumbnail alongside the detail text, selected statically from the Gallery_Images set.
5. THE Carousel (Requirement 1) SHALL display gallery images in a visually curated order, not in sequential `gallery-01` through `gallery-60` numerical order, to ensure visual variety across the 12 featured slides.
6. THE `galleryImages` list in the `@code {}` block of Home.razor SHALL be replaced with a `carouselSlides` list of records containing at minimum an `ImagePath` string and a `Caption` string.
7. THE Home_Page `@code {}` block SHALL implement `IDisposable` and dispose all CSS_Timer instances in the `Dispose()` method to prevent memory leaks on page navigation.

---
