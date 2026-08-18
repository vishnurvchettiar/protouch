# Requirements Document

## Introduction

This document specifies the functional and non-functional requirements for redesigning the ProTouch Physio - Slimming Clinic public-facing website. The redesign transforms the current homepage (`Home.razor`) into a luxury wellness brand experience inspired by the Aesthetic Clinic template, adding a Before/After gallery, Meet the Expert section, testimonial carousel, FAQ accordion, and persistent floating CTAs — while keeping all existing backend logic, booking flows, and admin/dashboard styles completely unchanged.

---

## Glossary

- **AC_Page**: The redesigned homepage component rendered by `Home.razor`, scoped within `<div class="ac-page">`.
- **Hero_Carousel**: The rotating image panel in the hero section that auto-advances every 5 seconds.
- **Services_Tab**: The tabbed interface within the services section that switches the displayed service panel.
- **Before_After_Slider**: An interactive drag-to-compare widget showing treatment result image pairs.
- **Testimonial_Carousel**: The auto-advancing testimonial display with manual prev/next and dot navigation.
- **FAQ_Accordion**: The expandable FAQ list where at most one item is open at any time.
- **Floating_CTA**: The persistent fixed-position WhatsApp and Book Appointment buttons.
- **Sticky_Header**: The navigation header that transitions from transparent to solid white when the page is scrolled more than 80 pixels.
- **Guarantee_Ticker**: The animated horizontal scrolling strip of clinic promise items.
- **Doctor_Section**: The "Meet the Expert" panel featuring Dr. Manjari's credentials and photo.
- **Stats_Strip**: The animated counter section showing clinic outcome numbers.
- **JS_Runtime**: Blazor's `IJSRuntime` used to invoke JavaScript functions in `site.js`.
- **Timer**: `System.Threading.Timer` used for auto-advancing carousels.
- **PublicLayout**: `PublicLayout.razor` — the layout component, which must remain a passthrough `@Body` wrapper.
- **ServiceDetail**: `ServiceDetail.razor` — unchanged service detail and inline booking page.
- **ServiceBooking**: `ServiceBooking.razor` — unchanged dedicated booking form at `/book`.

---

## Requirements

### Requirement 1: Page Wrapper and CSS Scope

**User Story:** As a developer, I want the redesigned homepage to use a new CSS scope, so that the new styles do not conflict with existing admin dashboard or `wellness-page` styles.

#### Acceptance Criteria

1. THE AC_Page SHALL be rendered inside a `<div class="ac-page">` wrapper element.
2. THE AC_Page CSS SHALL define all new design tokens and section styles scoped under the `.ac-page` selector.
3. WHEN new CSS rules are added to `app.css`, THE AC_Page styles SHALL NOT modify or override any existing `.wellness-page`, admin, or dashboard CSS rules.
4. THE AC_Page SHALL use `--ac-*` prefixed CSS custom properties for all new design tokens.

---

### Requirement 2: Sticky Navigation Header

**User Story:** As a visitor, I want a navigation header that stays visible while I scroll, so that I can always navigate to different sections of the page.

#### Acceptance Criteria

1. THE Sticky_Header SHALL be rendered at the top of AC_Page and display the ProTouch logo, navigation links (Services, About, Gallery, Contact), a Login button, and a Call Now button.
2. WHEN the page is first loaded, THE Sticky_Header SHALL be transparent with light-coloured text to overlay the dark hero section.
3. WHEN the user scrolls more than 80 pixels from the top, THE Sticky_Header SHALL transition to a solid white background with dark text by adding the `.ac-header--scrolled` CSS class.
4. WHEN the user scrolls back to within 80 pixels of the top, THE Sticky_Header SHALL remove the `.ac-header--scrolled` class and return to transparent.
5. THE Sticky_Header SHALL remain fixed at the top of the viewport at all times during scrolling.
6. THE `initStickyHeader` JavaScript function SHALL store its scroll event listener as `window._stickyHeaderHandler` to support cleanup.

---

### Requirement 3: Dark/Light Split Hero Section

**User Story:** As a visitor, I want a visually impactful hero section, so that I immediately understand the clinic's premium positioning and can take a booking action.

#### Acceptance Criteria

1. THE AC_Page hero section SHALL use a two-column layout: a dark left panel (background `#1a1025`) containing the headline and CTAs, and a light right panel containing the rotating Hero_Carousel.
2. THE Hero_Carousel SHALL display clinic photography images cycling through a curated list of gallery images.
3. WHEN `OnAfterRenderAsync` fires with `firstRender = true`, THE Hero_Carousel SHALL start a Timer that advances `activeHeroIndex` by 1 every 5 seconds.
4. WHEN `activeHeroIndex` reaches the end of the `heroImages` list, THE Hero_Carousel SHALL wrap around to index 0.
5. THE hero section SHALL contain a primary CTA button linking to `/book` and a secondary CTA button linking to the clinic's WhatsApp number.
6. THE hero section SHALL display trust badges: ISO 9001:2015 certification, star rating (4.9/5.0), and patient count (500+).
7. IF `prefers-reduced-motion` is detected, THE Hero_Carousel SHALL display all images as immediately visible without animation.

---

### Requirement 4: Guarantee Ticker Strip

**User Story:** As a visitor, I want to see the clinic's core promises displayed prominently, so that I feel reassured about the treatment approach.

#### Acceptance Criteria

1. THE Guarantee_Ticker SHALL display the promises "No Fasting", "No Medicine", "No Surgery", "No Side Effects", "No Down Time" as a horizontally scrolling strip.
2. THE Guarantee_Ticker SHALL loop seamlessly by duplicating the item list.
3. IF `prefers-reduced-motion` is detected, THE Guarantee_Ticker SHALL pause its CSS animation.

---

### Requirement 5: Tabbed Services Showcase

**User Story:** As a visitor, I want to browse each clinic service in a rich panel view, so that I can understand what each service offers before booking.

#### Acceptance Criteria

1. THE Services_Tab component SHALL display four tab buttons corresponding to: Weight Loss & Slimming, Physiotherapy & Rehabilitation, Yoga & Holistic Wellness, and Specialised Programs.
2. WHEN a visitor clicks a tab button, THE Services_Tab SHALL update the content panel to show the selected service's icon, name, summary, and bullet points.
3. THE Services_Tab SHALL initialise with "weight-loss" as the default active tab.
4. WHEN a tab is active, THE Services_Tab SHALL visually distinguish it from inactive tabs using the service's accent colour.
5. THE services panel SHALL contain a "View Service & Book →" link that navigates to the corresponding `/services/{id}` page.
6. WHILE a tab is active, THE Services_Tab SHALL keep `activeTab` equal to the selected service ID.

---

### Requirement 6: Meet the Expert — Doctor Section

**User Story:** As a visitor, I want to learn about Dr. Manjari's qualifications, so that I can trust the clinic's medical expertise before booking.

#### Acceptance Criteria

1. THE Doctor_Section SHALL display a professional photo of Dr. Manjari alongside her name, title ("Physiotherapist & Slimming Expert"), a biographical paragraph, and a list of credentials and certifications.
2. THE Doctor_Section SHALL display the ISO 9001:2015 certification badge visually overlaid on or adjacent to the doctor's photo.
3. THE Doctor_Section SHALL include a "Book a Consultation →" CTA button linking to `/book`.
4. THE Doctor_Section SHALL be reachable by scrolling; its container element SHALL carry `id="doctor"` for anchor navigation.

---

### Requirement 7: Animated Stats Strip

**User Story:** As a visitor, I want to see key clinic outcome numbers animate into view, so that the scale of results is communicated with visual impact.

#### Acceptance Criteria

1. THE Stats_Strip SHALL display at minimum four counters: weight loss per month (4–8 kg), inch loss per session (2–5 in), patients served (500+), and years of experience (10+).
2. WHEN the Stats_Strip scrolls into the viewport, THE Stats_Strip SHALL trigger the `initStatCounters` JavaScript function to animate each counter from 0 to its target value over 1200 milliseconds.
3. IF `prefers-reduced-motion` is detected, THE Stats_Strip SHALL display all counter values immediately without animation.

---

### Requirement 8: Before/After Gallery with Comparison Slider

**User Story:** As a visitor, I want to see before/after treatment results with an interactive comparison tool, so that I can visually evaluate the clinic's effectiveness.

#### Acceptance Criteria

1. THE Before_After_Slider SHALL display a grid of before/after image pairs sourced from `/images/clinic-gallery/` paths.
2. EACH Before_After_Slider item SHALL show a "Before" image on the left and an "After" image on the right, separated by a draggable handle.
3. WHEN a visitor drags the handle horizontally, THE Before_After_Slider SHALL update the width of the after-image layer to reveal more or less of each image.
4. THE Before_After_Slider SHALL clamp the after-image width to a minimum of 5% and a maximum of 95% of the container width.
5. THE Before_After_Slider SHALL use the Pointer Events API to support both mouse and touch input without separate handlers.
6. WHEN `DisposeAsync` is called, THE Before_After_Slider SHALL remove all pointer event listeners via `window._baCleanup`.
7. IF a gallery image returns a 404 error, THE Before_After_Slider SHALL hide the broken image element via an `onerror` handler.

---

### Requirement 9: Testimonial Carousel

**User Story:** As a visitor, I want to read testimonials from other patients in a dynamic carousel, so that I am reassured by social proof before booking.

#### Acceptance Criteria

1. THE Testimonial_Carousel SHALL display patient testimonials with a star rating, quote text, and patient role label.
2. WHEN `OnAfterRenderAsync` fires with `firstRender = true`, THE Testimonial_Carousel SHALL start a Timer that advances `activeTestiIndex` by 1 every 6 seconds.
3. WHEN `activeTestiIndex` reaches the end of the testimonials list, THE Testimonial_Carousel SHALL wrap around to index 0.
4. THE Testimonial_Carousel SHALL provide previous and next arrow buttons that allow manual navigation between testimonials.
5. WHEN a manual navigation action is taken, THE Testimonial_Carousel SHALL reset the auto-advance timer to avoid a double-advance within the same cycle.
6. THE Testimonial_Carousel SHALL display dot indicators showing the current slide position and allow clicking a dot to jump to that testimonial.
7. IF `prefers-reduced-motion` is detected, THE Testimonial_Carousel SHALL display all testimonials without slide transition animation.

---

### Requirement 10: FAQ Accordion

**User Story:** As a visitor, I want to read answers to common questions in an expandable FAQ, so that I can resolve concerns before booking.

#### Acceptance Criteria

1. THE FAQ_Accordion SHALL display at minimum six frequently asked questions relevant to the clinic's services (e.g., safety, cost, eligibility, results timeline).
2. WHEN a visitor clicks a closed FAQ question, THE FAQ_Accordion SHALL expand that item's answer panel using a CSS height transition.
3. WHEN a visitor clicks an open FAQ question, THE FAQ_Accordion SHALL collapse that item's answer panel.
4. WHILE the FAQ_Accordion is displayed, AT MOST one FAQ item SHALL be open at any time.
5. THE FAQ_Accordion SHALL initialise with all items closed (`openFaq = -1`).

---

### Requirement 11: Contact Section

**User Story:** As a visitor, I want to find the clinic's contact details and a clear booking call-to-action in one place, so that I can take the next step easily.

#### Acceptance Criteria

1. THE AC_Page contact section SHALL display the clinic's primary phone number (96735 30031), WhatsApp number (+91 90280 27888), email address (protouch1998@gmail.com), and full postal address.
2. THE contact section SHALL contain a prominent "Book an Appointment →" button linking to `/book`.
3. THE contact section SHALL contain a WhatsApp button linking to `https://wa.me/919028027888` with `target="_blank"` and `rel="noopener noreferrer"`.
4. THE contact section SHALL be reachable by scrolling; its container element SHALL carry `id="contact"` for anchor navigation.

---

### Requirement 12: Floating CTA Buttons

**User Story:** As a visitor on any part of the page, I want a WhatsApp and Book button always visible in the corner of the screen, so that I can take action without scrolling back to the top.

#### Acceptance Criteria

1. THE Floating_CTA SHALL render two circular buttons fixed to the bottom-right of the viewport: a WhatsApp button and a Book Appointment button.
2. THE Floating_CTA SHALL have `position: fixed`, `bottom: 2rem`, `right: 2rem`, and `z-index: 999` so it stays above all page content.
3. THE WhatsApp button SHALL link to `https://wa.me/919028027888` with `target="_blank"` and `rel="noopener noreferrer"`.
4. THE Book button SHALL link to `/book`.
5. THE Floating_CTA SHALL be present on the page at all times and SHALL NOT be conditionally hidden or shown based on scroll position.

---

### Requirement 13: Responsive Layout

**User Story:** As a visitor on a mobile device, I want the redesigned homepage to be fully usable on small screens, so that I can browse and book from my phone.

#### Acceptance Criteria

1. THE AC_Page hero section SHALL stack vertically (single column) at viewport widths of 850px or less.
2. THE Services_Tab tabs SHALL wrap or scroll horizontally on viewport widths of 850px or less.
3. THE Before_After_Slider SHALL stack its image pairs vertically at viewport widths of 600px or less.
4. THE Sticky_Header navigation links SHALL be hidden on viewport widths of 850px or less, with only the logo, Login, and Call buttons remaining visible.
5. THE Floating_CTA buttons SHALL remain visible and accessible at all viewport widths.

---

### Requirement 14: Resource Cleanup and Memory Safety

**User Story:** As a developer, I want all JavaScript listeners and Blazor timers to be properly cleaned up when the component is disposed, so that navigating away from the homepage does not cause memory leaks.

#### Acceptance Criteria

1. THE AC_Page component SHALL implement `IAsyncDisposable` and call `DisposeAsync` to clean up all resources.
2. WHEN `DisposeAsync` is called, THE AC_Page SHALL dispose `_heroTimer` and `_testiTimer`.
3. WHEN `DisposeAsync` is called, THE AC_Page SHALL invoke `JS.InvokeVoidAsync("destroyAestheticAnimations")` to remove all JavaScript event listeners registered during initialisation.
4. THE `destroyAestheticAnimations` JavaScript function SHALL remove the sticky header scroll listener, the before/after pointer listeners, the scroll reveal observer, and the sticky bar observer.
5. IF a JavaScript invocation in `DisposeAsync` throws an exception, THE AC_Page SHALL catch and suppress it without re-throwing.

---

### Requirement 15: Existing Pages and Styles Unchanged

**User Story:** As a developer, I want to ensure that the redesign does not break any existing functionality, so that admin, booking, and service detail pages continue to work correctly.

#### Acceptance Criteria

1. THE PublicLayout SHALL remain a passthrough `@Body` wrapper with no additional markup added.
2. THE ServiceDetail page SHALL remain functionally unchanged; its styles, booking form, and service catalogue SHALL NOT be modified.
3. THE ServiceBooking page SHALL remain functionally unchanged; its form, validation logic, and styles SHALL NOT be modified.
4. THE admin dashboard and client dashboard pages SHALL remain visually and functionally unchanged; no `.glass-panel`, `.btn-clinic`, or admin CSS rules SHALL be modified.
5. WHEN new functions are added to `site.js`, THE existing functions (`initScrollReveal`, `initStatCounters`, `initGuaranteeTicker`, `initStickyBar`, `initHeroStagger`, `initGlassCardTilt`, `initSvgDraw`, `initServiceAmbientGlow`, `destroyHomepageAnimations`, `destroyGlassCardTilt`) SHALL remain functionally identical.
