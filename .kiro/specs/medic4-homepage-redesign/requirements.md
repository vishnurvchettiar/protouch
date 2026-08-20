# Requirements Document

## Introduction

This feature redesigns the public-facing home page (`Home.razor`) of the PROTOUCH Physio - Slimming Clinic website, replacing the current aesthetic with a layout and design system inspired by the BeMedic4 medical clinic template ([themes.muffingroup.com/be/medic4](https://themes.muffingroup.com/be/medic4/)). The redesign adopts the template's teal-to-green palette (swapped to ProTouch brand green #008d36), Manrope + Instrument Sans typography, and its eight-section page structure, while fully preserving all existing interactive widgets: the BMI estimator, patient feedback widget, clinic gallery with lightbox, FAQ accordion, and testimonial carousel. All existing Blazor server-side logic, JS interop hooks, and performance patterns (CSS-only hero crossfade, IntersectionObserver-driven counters, `requestIdleCallback` deferred init) must be carried forward intact.

---

## Glossary

- **HomePage**: The public-facing Blazor page rendered at `/` via `Home.razor`.
- **Header**: The top navigation bar rendered on every scroll position.
- **HeroSection**: The first full-viewport section containing the primary headline, stats, and CTA.
- **BookingBanner**: The full-width teal/green strip placed immediately below the hero.
- **BodyPartSection**: The interactive tab area that maps clinic specialities to body regions.
- **ServicesSection**: The horizontally scrollable service card row.
- **HowItWorksSection**: The numbered-steps section explaining the patient journey.
- **DarkCTASection**: The dark/teal full-width section with animated stat counters and a booking CTA.
- **SpecialistsSection**: The team/doctor cards section.
- **BMIEstimator**: The existing wellness calculator widget retained from the current page.
- **FeedbackWidget**: The existing star-rating + comment form retained from the current page.
- **GallerySection**: The existing photo grid with lightbox retained from the current page.
- **FAQSection**: The existing accordion retained from the current page.
- **TestimonialCarousel**: The existing patient-quote carousel retained from the current page.
- **DesignSystem**: The coordinated set of colour tokens, type scale, and spacing rules defined in this document.
- **ColorToken**: A named CSS custom property (e.g. `--pt-primary`) holding a colour value.
- **Manrope**: The primary body/UI typeface to be loaded from Google Fonts or self-hosted.
- **InstrumentSans**: The heading typeface to be loaded from Google Fonts or self-hosted.
- **ScrollRevealObserver**: The existing `IntersectionObserver` that adds `.revealed` to `[data-reveal]` elements.
- **ClinicService**: The existing Blazor-injected service used by `FeedbackWidget` and `BMIEstimator`.
- **JSRuntime**: Blazor's `IJSRuntime` already injected in `Home.razor`.
- **MobileBreakpoint**: Viewport widths ≤ 768 px.
- **TabletBreakpoint**: Viewport widths 769 px – 1024 px.

---

## Requirements

---

### Requirement 1: Design System — Colour Tokens

**User Story:** As the clinic owner, I want the homepage to use a consistent colour palette matching the BeMedic4 look adapted to ProTouch branding, so that the site feels professional and cohesive.

#### Acceptance Criteria

1. THE HomePage SHALL define the following CSS custom properties on `:root` (or a scoped `.pt-page` wrapper):
   - `--pt-primary: #008d36` (ProTouch brand green, replaces BeMedic4 teal #176973)
   - `--pt-mint: #B8E5D2` (soft mint accent)
   - `--pt-surface: #EDF4F2` (off-white mint background)
   - `--pt-cta-bg: #DAFFE7` (pale green CTA background)
   - `--pt-dark: #0d2b1e` (dark section background)
   - `--pt-text: #737373` (body text grey)
   - `--pt-heading: #1a1a1a` (heading near-black)
   - `--pt-white: #ffffff`
2. WHEN a ColorToken value changes in the CSS source, THE HomePage SHALL reflect the updated colour across all sections that reference that token without requiring per-section overrides.
3. THE HomePage SHALL NOT hard-code hex colour values in inline styles or component markup; all colours SHALL be expressed via ColorToken references.
4. WHERE the `prefers-color-scheme: dark` media query is active, THE HomePage SHALL maintain the same light design system without switching to a dark theme (dark mode opt-out for v1).

---

### Requirement 2: Design System — Typography

**User Story:** As the clinic owner, I want the page to use the Manrope and Instrument Sans typefaces matching the BeMedic4 template, so that the visual hierarchy feels modern and medical.

#### Acceptance Criteria

1. THE HomePage SHALL load Manrope (weights 400, 500, 600) as the body/UI typeface using `<link rel="preconnect">` and `<link rel="stylesheet">` tags placed in `App.razor` `<head>`, with a system-font fallback stack: `'Manrope', system-ui, -apple-system, sans-serif`.
2. THE HomePage SHALL load Instrument Sans (weights 400, 600, 700) as the heading typeface using the same pattern, with fallback: `'Instrument Sans', Georgia, serif`.
3. THE HomePage SHALL define a type scale via CSS custom properties:
   - `--pt-font-body: 'Manrope', system-ui, -apple-system, sans-serif`
   - `--pt-font-heading: 'Instrument Sans', Georgia, serif`
   - `--pt-text-sm: 0.875rem`
   - `--pt-text-base: 1rem`
   - `--pt-text-lg: 1.125rem`
   - `--pt-text-xl: 1.25rem`
   - `--pt-text-2xl: 1.5rem`
   - `--pt-text-3xl: 2rem`
   - `--pt-text-4xl: 2.75rem`
   - `--pt-text-5xl: 3.5rem`
4. WHEN the Manrope or Instrument Sans font files fail to load (network error), THE HomePage SHALL remain fully readable using the defined system-font fallback stack.
5. THE HomePage SHALL set `font-display: swap` on all custom font face declarations to prevent invisible text during font load.
6. THE HomePage SHALL NOT import additional decorative or icon fonts beyond Manrope and Instrument Sans.

---

### Requirement 3: Transparent Sticky Header

**User Story:** As a site visitor, I want a header that starts transparent over the hero and becomes solid when I scroll, so that I can navigate the page without the header obscuring content.

#### Acceptance Criteria

1. THE Header SHALL render a horizontal bar containing: the ProTouch logo (left-aligned), a navigation menu with links to `#services`, `#body-parts`, `#team`, `#feedback`, and `#contact` (centred), and a "Book a Visit" CTA button (right-aligned) linking to `/book`.
2. WHEN the page scroll position is 0 px, THE Header SHALL display with a transparent background and white or light-coloured text/links so it overlays the HeroSection image without a visible bar.
3. WHEN the page scroll position exceeds 80 px, THE Header SHALL transition to a solid `--pt-white` background with `--pt-heading` coloured text within 200 ms using a CSS `transition` on `background-color`.
4. THE Header SHALL remain fixed (CSS `position: fixed`) at the top of the viewport at all scroll positions.
5. WHEN the viewport width is at or below MobileBreakpoint, THE Header SHALL hide the desktop navigation links and the "Book a Visit" button, and display a hamburger button (three horizontal lines).
6. WHEN the hamburger button is activated, THE Header SHALL expand a full-width mobile navigation overlay containing all navigation links and the "Book a Visit" button.
7. WHEN a mobile navigation link is tapped, THE Header SHALL close the mobile navigation overlay.
8. THE Header's "Book a Visit" CTA button SHALL have a `background-color` of `--pt-primary` and `color` of `--pt-white`, with a 4 px border-radius and 12 px × 24 px padding.

---

### Requirement 4: Hero Section

**User Story:** As a site visitor, I want a compelling full-viewport hero section with the clinic's headline, key stats, and a booking CTA, so that I immediately understand the value of the clinic and can take action.

#### Acceptance Criteria

1. THE HeroSection SHALL use a split layout: the left half contains text content; the right half contains the clinic imagery.
2. THE HeroSection left panel SHALL display, in order: a kicker label "ISO 9001:2015 CERTIFIED CLINIC", an `<h1>` reading "Take care of your health", a subheading paragraph describing ProTouch's physiotherapy-led approach, a stats row containing "99% Cured" and "4–8 kg / Month" figures, a primary CTA button "Book a Visit" linking to `/book`, and a secondary text link "Our Services →" linking to `#services`.
3. THE HeroSection right panel SHALL display a CSS-driven crossfade slideshow cycling through the existing hero images (`gallery-37.jpeg`, `gallery-42.jpeg`, `gallery-55.jpeg`) at 5-second intervals with a teal/green colour overlay strip.
4. WHEN `prefers-reduced-motion: reduce` is active, THE HeroSection SHALL stop the crossfade animation and display only the first hero image statically.
5. THE HeroSection SHALL occupy 100 vh on desktop and a minimum of 75 vh on mobile, ensuring the headline and CTA are visible above the fold without scrolling.
6. THE HeroSection stats row SHALL use `--pt-primary` as the stat number colour and `--pt-text` for the label.
7. THE HeroSection primary CTA button SHALL match the DesignSystem: `background: --pt-primary`, `color: --pt-white`, `font-family: --pt-font-body`, `font-weight: 600`, rounded corners at 8 px.

---

### Requirement 5: Booking Banner Strip

**User Story:** As a site visitor, I want a high-visibility "Book a visit now" strip below the hero, so that I have a persistent prompt to schedule an appointment while I browse.

#### Acceptance Criteria

1. THE BookingBanner SHALL render as a full-width horizontal strip with `background: --pt-primary` immediately below the HeroSection.
2. THE BookingBanner SHALL contain the text "Ready to start your wellness journey?" on the left and a "Book a Visit Now →" button on the right.
3. THE BookingBanner "Book a Visit Now" button SHALL have `background: --pt-white`, `color: --pt-primary`, and link to `/book`.
4. WHEN the viewport width is at or below MobileBreakpoint, THE BookingBanner SHALL stack the text and button vertically, both centred.
5. THE BookingBanner SHALL have a minimum height of 80 px and vertically centred content using CSS flexbox.

---

### Requirement 6: Body-Part Treatments Section

**User Story:** As a site visitor, I want to explore which conditions the clinic treats by clicking on body areas, so that I can quickly find whether my specific complaint is addressed.

#### Acceptance Criteria

1. THE BodyPartSection SHALL have a section heading "We can help with any part of you" and the section `id="body-parts"`.
2. THE BodyPartSection SHALL display a horizontal tab row with the labels: Neck & Shoulder, Back & Spine, Knee & Leg, Arm & Elbow, Abdomen & Core, Full Body Wellness.
3. WHEN a tab is selected, THE BodyPartSection SHALL display a two-column panel: left column shows the selected area's name, a one-paragraph description of related treatments offered by the clinic, and a "Book Treatment →" link to `/book`; right column shows a static human silhouette illustration or a relevant gallery image from `/images/clinic-gallery/`.
4. THE BodyPartSection tab row SHALL use `--pt-primary` as the active tab underline/indicator colour.
5. WHEN the viewport width is at or below MobileBreakpoint, THE BodyPartSection SHALL stack the tab row vertically (accordion-style) and hide the silhouette illustration.
6. THE BodyPartSection SHALL be pre-populated with the following tab definitions in `Home.razor` C# `OnInitialized`:

   | Tab Label           | Treatment Description |
   |---------------------|-----------------------|
   | Neck & Shoulder     | Physiotherapy for cervical spondylitis, neck pain, frozen shoulder and upper-limb rehabilitation |
   | Back & Spine        | Treatment for lumbar spondylitis, sciatica, slipped disc and post-operative spinal care |
   | Knee & Leg          | Knee pain, arthritis, ligament rehabilitation and sports injury recovery |
   | Arm & Elbow         | Tennis elbow, golfer's elbow, repetitive strain and fracture rehabilitation |
   | Abdomen & Core      | Core strengthening, diastasis recti, post-natal rehabilitation and inch-loss programmes |
   | Full Body Wellness  | Weight loss, inch loss, yoga therapy, Russian nature cure and overall body contouring |

---

### Requirement 7: Services Section

**User Story:** As a site visitor, I want to browse the clinic's service offerings in a visually rich card layout, so that I can understand each service and navigate to its detail page.

#### Acceptance Criteria

1. THE ServicesSection SHALL have the section heading "Our Services" with the `id="services"`, a sub-kicker "WHAT WE OFFER", and a brief introductory paragraph.
2. THE ServicesSection SHALL display four service cards in a horizontally scrollable row on desktop (overflow-x: auto, snap scrolling) and as a single-column stack on MobileBreakpoint.
3. EACH service card SHALL display: a service image from `/images/clinic-gallery/` (the same mapping already used in `Home.razor`), the service name as an `<h3>`, a two-sentence description, and a "View & Book →" link to `/services/{service-id}`.
4. THE ServicesSection SHALL use the existing four service definitions from `Home.razor` `OnInitialized`:
   - Weight Loss & Slimming (accent `--pt-primary`)
   - Physiotherapy & Rehabilitation (accent `#7c1b68`)
   - Yoga & Russian Nature Cure (accent `#f4b41a`)
   - Specialised Programs for Children (accent `#e52321`)
5. WHEN the user hovers a service card on a pointer device, THE ServicesSection SHALL elevate the card with a CSS `box-shadow` transition (duration ≤ 200 ms) — no JS required.
6. THE ServicesSection service cards SHALL use `loading="lazy"` on all `<img>` elements except the first card, which SHALL use `loading="eager"` and `fetchpriority="high"`.

---

### Requirement 8: How It Works Section

**User Story:** As a prospective patient, I want to see a simple numbered explanation of how to get started with the clinic, so that I feel confident the process is straightforward.

#### Acceptance Criteria

1. THE HowItWorksSection SHALL have the heading "Our team of specialists is at your service" and display exactly three numbered steps.
2. EACH step SHALL display: a large step number (01, 02, 03) in `--pt-primary` colour, a bold step title, and a two-sentence description.
3. THE HowItWorksSection SHALL use the following step content:
   - **01 — Book a Consultation**: Choose your preferred service and submit a booking request online or by calling 96735 30031. Our team will confirm your appointment within 24 hours.
   - **02 — Personalised Assessment**: Dr. Manjari conducts a thorough assessment of your health profile, goals and medical history. A tailored treatment plan is created specifically for you.
   - **03 — Start Your Journey**: Begin your programme with expert guidance, track your progress and achieve lasting results — no fasting, no medicine, no surgery.
4. THE HowItWorksSection SHALL display a pull-quote block beneath the steps with the text: *"The best care for lasting results!"* and a sub-line "— ISO 9001:2015 Certified Clinic".
5. WHEN the HowItWorksSection enters the viewport (ScrollRevealObserver threshold 0.15), THE HowItWorksSection SHALL add the `.revealed` class to the section to trigger the standard reveal animation.
6. WHEN the viewport width is at or below MobileBreakpoint, THE HowItWorksSection SHALL stack the three steps vertically with a connecting vertical line between them.

---

### Requirement 9: Dark CTA Section with Animated Stat Counters

**User Story:** As a site visitor, I want to see a bold full-width section with animated statistics that demonstrate the clinic's track record, so that I feel reassured before booking.

#### Acceptance Criteria

1. THE DarkCTASection SHALL use `background: --pt-dark` (or a deep teal gradient to `--pt-primary`) and `color: --pt-white`, spanning the full viewport width.
2. THE DarkCTASection SHALL display the heading "Take care of your health anytime, anywhere" and a supporting paragraph describing the clinic's availability and approach.
3. THE DarkCTASection SHALL display four animated counters using the existing `initStatCounters` JS function: `500+` (Patients Served), `10+` (Years Experience), `4–8 kg` (Weight Loss / Month), `4.9` (Average Rating).
4. WHEN the DarkCTASection enters the viewport (IntersectionObserver threshold 0.2), THE DarkCTASection SHALL trigger `window.initStatCounters('.pt-dark-stats', 1200)` via the existing `scheduleDeferredInit_home` mechanism.
5. WHEN `prefers-reduced-motion: reduce` is active, THE DarkCTASection SHALL display the final counter values immediately without animation.
6. THE DarkCTASection SHALL display a "Book a Visit Now →" CTA button with `background: --pt-primary`, `color: --pt-white`, linking to `/book`.
7. WHEN the viewport width is at or below MobileBreakpoint, THE DarkCTASection SHALL stack the heading, stats grid, and CTA vertically, with the stats displayed in a 2×2 grid.

---

### Requirement 10: Specialists / Team Section

**User Story:** As a prospective patient, I want to see the clinic team presented in a professional card layout, so that I feel confident in the expertise behind my treatment.

#### Acceptance Criteria

1. THE SpecialistsSection SHALL have the heading "Our team of specialists is at your service", a kicker label "MEET THE TEAM", and the `id="team"`.
2. THE SpecialistsSection SHALL display a primary doctor card for Dr. Manjari, containing: her photo (`/images/clinic-gallery/gallery-27.jpeg`), her name ("Dr. Manjari"), her title ("Physiotherapist & Slimming Expert"), her six credential bullets from the existing `Home.razor` list, and a "Book Consultation →" link to `/book`.
3. THE SpecialistsSection SHALL display three supporting practitioner cards (placeholders) for: "Senior Physiotherapist", "Yoga Therapist", and "Diet & Nutrition Consultant" — each with a placeholder avatar, title, and "Learn More →" link to `#contact`.
4. WHEN the SpecialistsSection enters the viewport (ScrollRevealObserver threshold 0.15), THE SpecialistsSection SHALL add `.revealed` to trigger the standard reveal animation.
5. WHEN the viewport width is at or below MobileBreakpoint, THE SpecialistsSection SHALL display cards in a single-column scroll.

---

### Requirement 11: Preserved — BMI Estimator Widget

**User Story:** As a site visitor, I want to calculate my BMI and receive a personalised wellness recommendation, so that I understand which programme is right for me.

#### Acceptance Criteria

1. THE BMIEstimator SHALL remain on the HomePage with its existing section ID `id="bmi-estimator"`.
2. THE BMIEstimator form fields (Height, Weight, Wellness Goal) SHALL adopt the DesignSystem font (`--pt-font-body`) and input border colour `--pt-mint`, with a `--pt-primary` focus ring.
3. WHEN the user submits the BMI form with valid inputs, THE BMIEstimator SHALL compute and display the BMI value, category, message, and outcome using the existing C# `ComputeBmi` logic — no changes to the business logic.
4. IF the user submits the BMI form with height outside 100–250 cm, THE BMIEstimator SHALL display the existing validation error message for the height field.
5. IF the user submits the BMI form with weight outside 20–300 kg, THE BMIEstimator SHALL display the existing validation error message for the weight field.
6. IF the user submits the BMI form without selecting a wellness goal, THE BMIEstimator SHALL display the existing validation error message for the goal field.
7. THE BMIEstimator result card's accent colour SHALL be determined by the existing `_bmiColor` logic (underweight → `#7c1b68`, normal → `#008d36`, overweight → `#ffeb00`, obese → `#e52321`).

---

### Requirement 12: Preserved — Gallery Section with Lightbox

**User Story:** As a site visitor, I want to browse clinic photos and view them full-screen, so that I can get a feel for the clinic environment before visiting.

#### Acceptance Criteria

1. THE GallerySection SHALL remain on the HomePage with the `id="gallery"`, displaying the existing 60-image grid (`/images/clinic-gallery/gallery-01.jpeg` – `gallery-60.jpeg`).
2. THE GallerySection grid SHALL adopt the DesignSystem colours (hover overlay uses `--pt-primary` at 30% opacity) while preserving all existing Blazor event handlers (`OpenLightbox`, `LoadMoreGallery`).
3. WHEN the user clicks a gallery image, THE GallerySection SHALL open the lightbox overlay with previous/next navigation and keyboard ESC-to-close — preserving the existing `initGalleryLightbox` JS interop.
4. WHEN the user clicks "Load More", THE GallerySection SHALL load 12 additional images up to the total of 60, matching the existing `LoadMoreGallery` method.
5. THE GallerySection SHALL use `loading="eager"` for the first 6 images and `loading="lazy"` for all subsequent images, consistent with the existing implementation.

---

### Requirement 13: Preserved — Testimonial Carousel

**User Story:** As a prospective patient, I want to read real patient experiences, so that I feel reassured about the quality of care.

#### Acceptance Criteria

1. THE TestimonialCarousel SHALL remain on the HomePage, preserving all five existing testimonials, their quotes, and role labels.
2. THE TestimonialCarousel SHALL auto-advance every 6 000 ms using the existing `initTestiObserver` / `AdvanceTesti` IntersectionObserver mechanism.
3. WHEN `prefers-reduced-motion: reduce` is active, THE TestimonialCarousel SHALL stop auto-advancing and display the first testimonial statically.
4. THE TestimonialCarousel SHALL adopt `--pt-font-heading` for the quote text and `--pt-font-body` for the footer attribution.
5. THE TestimonialCarousel navigation dots and arrows SHALL use `--pt-primary` for the active/focused state.

---

### Requirement 14: Preserved — FAQ Accordion

**User Story:** As a site visitor, I want quick answers to common questions without leaving the page, so that I can resolve doubts before booking.

#### Acceptance Criteria

1. THE FAQSection SHALL remain on the HomePage, preserving all six existing FAQ items and the existing `ToggleFaq` C# toggle logic.
2. WHEN a FAQ item is activated, THE FAQSection SHALL expand that item's answer panel and collapse any previously open panel — accordion behaviour with one open item at a time.
3. THE FAQSection active item indicator (the "+" icon) SHALL rotate 45° when the item is open, using a CSS `transform: rotate(45deg)` transition.
4. THE FAQSection SHALL adopt the DesignSystem: open item border-left in `--pt-primary`, question text in `--pt-font-heading`, answer text in `--pt-font-body` / `--pt-text`.

---

### Requirement 15: Preserved — Feedback Widget

**User Story:** As a patient, I want to leave a star rating and optional comment after my visit, so that the clinic can track satisfaction and prospective patients can see that feedback is collected.

#### Acceptance Criteria

1. THE FeedbackWidget SHALL remain on the HomePage with the `id="feedback"`, preserving the existing `SubmitFeedbackAsync` / `ClinicService.SaveFeedbackAsync` integration.
2. WHEN the user selects a star rating and clicks "Submit Feedback", THE FeedbackWidget SHALL call `ClinicService.SaveFeedbackAsync` and display the existing success message — no changes to the service layer.
3. IF the user attempts to submit feedback without selecting a star rating, THE FeedbackWidget SHALL display the existing error message "Please select a star rating before submitting."
4. THE FeedbackWidget star buttons SHALL use `--pt-primary` as the lit/active star colour and `--pt-mint` as the hover preview colour.
5. THE FeedbackWidget textarea SHALL adopt the DesignSystem border (`--pt-mint`), font (`--pt-font-body`), and focus ring (`--pt-primary`).

---

### Requirement 16: Contact Section and Footer

**User Story:** As a site visitor, I want to find the clinic's contact details and a quick way to get directions, so that I can reach the clinic without friction.

#### Acceptance Criteria

1. THE ContactSection SHALL be retained with the `id="contact"`, displaying the existing three contact detail items: phone numbers, email, and address.
2. THE ContactSection "Book an Appointment" and "WhatsApp Us" buttons SHALL use the DesignSystem: primary button uses `--pt-primary` background; ghost button uses `--pt-primary` border and text.
3. THE ContactSection "Get Directions →" link SHALL open the existing Google Maps URL in a new tab with `rel="noopener noreferrer"`.
4. THE Footer SHALL display the copyright line, clinic name, Dr. Manjari's name, and city, using `--pt-font-body` and `--pt-text` colour.

---

### Requirement 17: Mobile Responsiveness

**User Story:** As a visitor on a smartphone, I want the entire homepage to render correctly on small screens, so that I can use all features without horizontal scrolling or overlapping elements.

#### Acceptance Criteria

1. THE HomePage SHALL NOT produce horizontal overflow at viewport widths ≥ 320 px; all sections SHALL use responsive CSS (flexbox or grid with appropriate `flex-wrap` or `grid-template-columns` breakpoints).
2. WHEN the viewport width is at or below MobileBreakpoint (768 px), THE HeroSection SHALL stack the text panel above the image panel, with the image panel having a `max-height` of 40 vh.
3. WHEN the viewport width is at or below MobileBreakpoint, THE ServicesSection SHALL display cards in a single-column vertical scroll (no horizontal overflow scroll).
4. WHEN the viewport width is at or below MobileBreakpoint, THE BodyPartSection tab row SHALL become a vertical select/accordion.
5. WHEN the viewport width is at or below MobileBreakpoint, THE SpecialistsSection cards SHALL render in a single-column layout.
6. THE HomePage shall use `<meta name="viewport" content="width=device-width, initial-scale=1.0">` (already present in `App.razor`) — no additional viewport meta changes are needed.
7. THE floating WhatsApp and Book CTA buttons SHALL remain at fixed `bottom-right` on all viewport sizes, with a minimum tap target size of 44 × 44 px.

---

### Requirement 18: Performance — Font Loading and Animation

**User Story:** As a site visitor, I want the page to load quickly with no layout shift from font loading or blocking scripts, so that the experience feels instant.

#### Acceptance Criteria

1. THE HomePage SHALL add `<link rel="preconnect" href="https://fonts.googleapis.com">` and `<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>` to `App.razor` `<head>` before the font stylesheet link.
2. THE HomePage font `<link>` tags SHALL use `media="print" onload="this.media='all'"` to load fonts non-blocking, with a `<noscript>` fallback `<link>` for accessibility.
3. THE HomePage SHALL continue to call `window.scheduleDeferredInit_home()` from `OnAfterRenderAsync` (firstRender only) to defer `initScrollReveal`, `initGuaranteeTicker`, `initBackToTop`, and `initActiveNavHighlight` via `requestIdleCallback`.
4. WHEN `requestIdleCallback` is not available (Safari, Firefox), THE HomePage SHALL fall back to `setTimeout(fn, 200)` — matching the existing `scheduleDeferredInit_home` implementation.
5. THE HomePage SHALL preserve all existing `try { await JS.InvokeVoidAsync(...) } catch {}` guards in `OnAfterRenderAsync` to prevent unhandled JS interop exceptions on prerender.
6. THE HomePage SHALL implement lazy loading for all gallery images beyond index 5, hero images beyond index 0, and service card images beyond index 0 using the `loading="lazy"` attribute.
7. THE HomePage's CSS for the BeMedic4-inspired layout SHALL be placed in a dedicated scoped stylesheet file (e.g. `home-medic4.css` or as a Blazor scoped style) and NOT bloat `app.css`.

---

### Requirement 19: Accessibility

**User Story:** As a user with assistive technology or keyboard-only navigation, I want the homepage to be navigable and understandable, so that I have equal access to the clinic's information.

#### Acceptance Criteria

1. THE Header navigation links SHALL be keyboard-navigable with visible focus indicators using `outline: 2px solid --pt-primary`.
2. THE HeroSection `<h1>` SHALL be the only `<h1>` on the page; all other section titles SHALL use `<h2>` or lower heading levels in correct hierarchical order.
3. THE GallerySection lightbox SHALL trap keyboard focus within the overlay when open and restore focus to the triggering gallery item when closed.
4. THE BMIEstimator form fields SHALL each have an associated `<label>` element with a matching `for` / `id` pair — preserving the existing implementation.
5. THE FeedbackWidget star buttons SHALL each have a descriptive `aria-label` (e.g. "1 star", "2 stars") — preserving the existing implementation.
6. THE BodyPartSection tab buttons SHALL use `role="tab"`, `aria-selected`, and be grouped in an element with `role="tablist"`.
7. THE TestimonialCarousel navigation dot buttons SHALL have `aria-label` values matching the testimonial index (e.g. "Testimonial 1") — preserving the existing implementation.
8. THE HowItWorksSection step numbers SHALL be marked with `aria-hidden="true"` so screen readers read the step title directly without the decorative number prefix.

---

### Requirement 20: Preservation of Existing JS Interop and Dispose

**User Story:** As a developer, I want all existing JS interop registrations and cleanup logic retained in the redesigned `Home.razor`, so that there are no memory leaks or missing initialisation side-effects.

#### Acceptance Criteria

1. THE HomePage `OnAfterRenderAsync` SHALL call all existing JS functions in the same order: `initStickyHeader`, `initActiveNavHighlight`, `initBackToTop`, `initGalleryLightbox`, `initTestiObserver`, `scheduleDeferredInit_home`.
2. THE HomePage `IAsyncDisposable.DisposeAsync` SHALL call all existing cleanup functions: `destroyAestheticAnimations`, `destroyHomepageAnimations`, `destroyTestiObserver`.
3. THE HomePage SHALL expose the existing `[JSInvokable]` methods `AdvanceTesti` and `CloseLightbox` without signature changes, so that the existing `site.js` interop calls continue to resolve.
4. IF a JS interop call throws during `OnAfterRenderAsync`, THE HomePage SHALL catch and suppress the exception using the existing `try { … } catch { }` pattern, and SHALL NOT propagate the exception to the Blazor circuit.
5. THE HomePage SHALL maintain the `@implements IAsyncDisposable` directive and inject `IJSRuntime JS` and `ClinicService ClinicService` without modification.
