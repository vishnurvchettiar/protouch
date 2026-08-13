# Requirements Document

## Introduction

This document captures the formal requirements for the ProTouch Clinic website enhancement set. Six concern areas are addressed: image asset refresh on the public landing page, complete CRUD operations in the admin dashboard, an enhanced client portal with meal photo upload and macro tracking, policy-compliant WhatsApp bulk messaging with CSV broadcast, a fully public service booking page at `/book` (no authentication required), and consistent application of the dark glass-morphism design system across all new and modified components.

All requirements are derived from the approved design document and are expressed using EARS patterns for unambiguous testability.

---

## Glossary

- **System**: The ProTouch Clinic Blazor Server application
- **Admin**: An authenticated user with the `Admin` role who operates the Clinic Control Center
- **Client**: An authenticated user with the `Client` role who accesses the Patient Portal
- **Visitor**: Any person accessing the `/book` page, regardless of authentication state
- **BookingRequest**: The data model representing a public service booking submission (`Status`, `FullName`, `PhoneNumber`, `ServiceType`, `PreferredDate`, `Notes`, `SubmittedAt`)
- **DailyLog**: The data model representing a client's daily weight, measurement, diet, and meal photo check-in
- **AttendanceRecord**: The data model representing a physical clinic visit
- **Appointment**: The data model representing a scheduled appointment created by an authenticated client
- **ClientProfile**: The data model representing a client's clinical profile (height, weight targets, measurements, doctor notes)
- **WhatsAppLog**: The data model representing a record of a WhatsApp message dispatch event
- **CsvRecipient**: An in-memory record containing `Name` and `Phone` fields parsed from an uploaded CSV file
- **MacroSlider**: The interactive UI element group used to set `CarbsPercentage`, `ProteinPercentage`, `FatPercentage`, and `VegPercentage` fields on a `DailyLog`
- **OptOutFooter**: The configurable opt-out text appended to bulk WhatsApp messages (stored under `Setting` key `WhatsApp:OptOutFooter`; default: `"Reply STOP to unsubscribe."`)
- **DesignToken**: A CSS custom property variable defined in `app.css` (e.g. `--bg-glass`, `--accent-teal`, `--grad-primary`)
- **GlassPanel**: The CSS class `glass-panel` applied to all card and panel containers
- **ClinicService**: The application service class that wraps all data access and Twilio operations
- **NormalisePhone**: The phone normalisation logic inside `ClinicService.BuildWhatsAppLink` that strips country code prefixes and formats to E.164

---

## Requirements

---

### Requirement 1: Image Asset Refresh on the Public Landing Page

**User Story:** As a prospective patient visiting the ProTouch website, I want to see a polished, professional gallery of clinic images, so that I can get a clear impression of the clinic's facilities and services before booking.

#### Acceptance Criteria

1. THE System SHALL serve all landing-page image assets from the `wwwroot/images/` directory using semantically named filenames (e.g. `hero-treatment.jpg`, `care-panel.jpg`) and from `wwwroot/images/clinic-gallery/` for gallery images named `gallery-01.jpg` through `gallery-N.jpg`, where N does not exceed 50.
2. WHEN the Home page renders, THE System SHALL display exactly one `<PageTitle>` tag and exactly one services `<section>` element.
3. WHEN the Home page renders, THE System SHALL display a responsive photo gallery section using all images found in `wwwroot/images/clinic-gallery/`, laid out in a CSS grid with at least 2 columns at viewport widths of 768 px or wider, and at least 1 column below 768 px.
4. IF no image files exist in `wwwroot/images/clinic-gallery/`, THEN THE System SHALL render a placeholder message in the gallery section and SHALL NOT render any broken image elements.
5. THE System SHALL reference all landing-page images via paths beginning with `/images/`; no filename matching the pattern `IMG-[0-9]{8}-WA[0-9]+` SHALL appear in any markup.
6. WHERE static image assets are served, THE System SHALL include a `Cache-Control: max-age=31536000, immutable` header for all files under `wwwroot/images/`.

---

### Requirement 2: Admin Full CRUD — Client Profiles

**User Story:** As an Admin, I want to fully create, read, update, and delete client profiles including their clinical measurements and access roles, so that I can maintain accurate and up-to-date records for every patient.

#### Acceptance Criteria

1. WHEN an Admin updates a client's profile fields (height, weight targets, waist, chest, hips, arms, doctor notes, active status), THE System SHALL persist all changes to the `ClientProfile` record.
2. WHEN an Admin changes a client's access role in the profile editor and the selected role differs from the current role, THE System SHALL remove all previous roles and assign only the selected role using ASP.NET Core Identity; IF the identity role assignment fails, THEN THE System SHALL display an inline error and leave the existing role unchanged.
3. WHEN an Admin clicks the delete action for a client, THE System SHALL display a confirmation step showing the count of related `DailyLog`, `AttendanceRecord`, `Appointment`, and `PaymentNotification` records before proceeding.
4. WHEN an Admin confirms deletion of a client, THE System SHALL delete the `ApplicationUser` record and all cascade-related `DailyLog`, `AttendanceRecord`, `Appointment`, and `PaymentNotification` records; IF the deletion operation fails, THEN THE System SHALL display an inline error and leave all records unchanged.
5. WHEN an Admin updates an existing user's full name, email, or phone number from the edit interface (without re-creating the account), THE System SHALL persist those changes to the `ApplicationUser` record; IF the updated email already exists on another account, THEN THE System SHALL display an inline error and make no changes.
6. IF a client profile cannot be found during an admin edit action, THEN THE System SHALL display an inline error message and leave all existing records unchanged.
7. WHEN an Admin saves clinical measurement fields (height, start weight, target weight, waist, chest, hips, arms), THE System SHALL reject any value that is negative, zero, or exceeds 999.9, and display an inline validation error for each out-of-range field.

---

### Requirement 3: Admin Full CRUD — Visit Records

**User Story:** As an Admin, I want to edit, check out, and delete existing visit records, so that I can correct errors and maintain an accurate treatment history for each client.

#### Acceptance Criteria

1. WHEN an Admin edits an existing `AttendanceRecord` and saves, THE System SHALL update the treatment type, therapist, notes, and next appointment date fields in the database, provided the therapist field is not empty and the notes field does not exceed 2000 characters.
2. WHEN an Admin sets a checkout time for a visit, THE System SHALL verify that the checkout time is greater than or equal to `CheckInTime` before setting `CheckOutTime` and updating `Status` to `"Completed"`.
3. IF the therapist field is empty or the notes field exceeds 2000 characters when saving a visit edit, THEN THE System SHALL display an inline validation error identifying the invalid field and SHALL NOT persist the changes.
4. IF the `AttendanceRecord` being edited no longer exists in the database at the time of save, THEN THE System SHALL display an inline error message and take no further action.
5. IF the provided checkout time is earlier than `CheckInTime`, THEN THE System SHALL display an inline validation error and SHALL NOT update the record.
6. WHEN an Admin clicks the delete action for a visit record, THE System SHALL display a confirmation prompt before proceeding with deletion.
7. WHEN an Admin confirms deletion of a visit record, THE System SHALL remove the `AttendanceRecord` from the database.
8. IF an Admin attempts to delete a visit record that does not exist, THEN THE System SHALL display an inline error and take no further action.

---

### Requirement 4: Admin Full CRUD — Appointment Status Management

**User Story:** As an Admin, I want to view all client appointments and update their statuses, so that I can confirm, complete, or cancel bookings from a central interface.

#### Acceptance Criteria

1. THE System SHALL display all `Appointment` records across all clients in a dedicated Appointments sub-tab of the Admin dashboard, ordered by descending scheduled appointment date and time.
2. WHEN an Admin updates an appointment status from the list, THE System SHALL persist the new status value (`"Scheduled"`, `"Confirmed"`, `"Completed"`, or `"Cancelled"`) to the `Appointment` record.
3. WHEN an Admin updates an appointment status, THE System SHALL reflect the change in the appointments list within 2 seconds and without requiring a full page reload.
4. IF the status persistence operation fails, THEN THE System SHALL display an inline error message and revert the displayed status to its previous value.

---

### Requirement 5: Admin Meal Review Tab

**User Story:** As an Admin, I want to review all client meal photos alongside their diet notes and macro breakdowns, so that the clinic dietician can monitor client adherence and follow up via WhatsApp.

#### Acceptance Criteria

1. THE System SHALL provide a dedicated "Meal Review" tab in the Admin dashboard.
2. WHEN the Meal Review tab is active, THE System SHALL display only `DailyLog` records that have a non-empty `MealPhotoUrl`, ordered by descending `LogDate`, including the associated client name, log date, diet notes, and `CarbsPercentage`, `ProteinPercentage`, `FatPercentage`, and `VegPercentage` values for each.
3. WHEN an Admin deletes a meal log entry from the Meal Review tab, THE System SHALL remove the corresponding `DailyLog` record from the database; IF the deletion fails, THE System SHALL display an inline error message and retain the record.
4. WHERE the total number of qualifying meal log records exceeds 20, THE System SHALL paginate the Meal Review list with a page size of 20.
5. IF a `DailyLog` record has an empty or null `MealPhotoUrl`, THEN THE System SHALL exclude that record from the Meal Review tab.

---

### Requirement 6: Client Portal — Meal Photo Upload and Timeline

**User Story:** As a Client, I want to upload a photo of my meal and view my previous meal photos in a timeline, so that I can share my dietary adherence with the clinic team and track my own progress.

#### Acceptance Criteria

1. WHEN a Client selects an image file for upload in the daily check-in form, THE System SHALL display a client-side preview of the selected image before the form is submitted.
2. WHEN a Client submits the daily check-in form, IF a meal photo was selected and passes all validation checks, THEN THE System SHALL save the image file to `wwwroot/uploads/meals/` and store the relative path in `DailyLog.MealPhotoUrl`; submission of other form fields SHALL NOT be blocked if no photo is selected.
3. WHEN a Client views the Client Dashboard, THE System SHALL display a scrollable meal photo timeline showing all previous `DailyLog` entries that have a non-empty `MealPhotoUrl`, ordered by descending `LogDate`, with each entry showing a thumbnail, date, and diet notes.
4. IF an uploaded file's extension is not one of `.jpg`, `.jpeg`, `.png`, or `.webp`, THEN THE System SHALL reject the upload and display an inline error message indicating the file type is not supported, without blocking submission of other form fields.
5. IF an uploaded file exceeds 8 MB, THEN THE System SHALL reject the upload and display an inline error message indicating the maximum file size is 8 MB, without blocking submission of other form fields.
6. IF saving the image file to the filesystem fails, THEN THE System SHALL display an inline error message and submit the daily log entry without a `MealPhotoUrl`.

---

### Requirement 7: Client Portal — Macro Percentage Sliders

**User Story:** As a Client, I want to enter my daily macro-nutrient split using sliders that enforce a total of 100%, so that I can accurately record my diet without accidentally submitting invalid percentages.

#### Acceptance Criteria

1. WHEN a Client adjusts any macro slider, THE System SHALL adjust the values of the other macro sliders proportionally so that `CarbsPercentage + ProteinPercentage + FatPercentage + VegPercentage = 100` at all times; each slider SHALL accept only integer values in the range 0–100.
2. WHEN the daily check-in form first renders, THE System SHALL initialise the macro sliders to the values stored in the most recent `DailyLog` for the client, or to the defaults `CarbsPercentage = 35, ProteinPercentage = 30, FatPercentage = 15, VegPercentage = 20` if no prior log exists.
3. WHEN a Client views the macro input area, THE System SHALL render a live colour-coded bar (Carbs = teal, Protein = blue, Fat = purple, Veg = success green) reflecting the current macro split.
4. IF a Client submits the daily check-in form and `CarbsPercentage + ProteinPercentage + FatPercentage + VegPercentage ≠ 100`, THEN THE System SHALL prevent form submission and display an inline validation error adjacent to the macro slider group identifying the sum discrepancy.
5. WHEN a `DailyLog` is saved, THE System SHALL persist the exact integer percentages for all four macro fields to the database.

---

### Requirement 8: Client Portal — Measurement Fields in Daily Log

**User Story:** As a Client, I want to record my body measurements (waist, chest, hips, arms) as part of my daily check-in, so that I can track changes in my body composition alongside my weight.

#### Acceptance Criteria

1. WHEN a Client opens the daily check-in form, THE System SHALL display optional input fields for `WaistInches`, `ChestInches`, `HipsInches`, and `ArmsInches`, each accepting decimal values in the range 0.1–999.9 with a maximum of one decimal place.
2. WHEN a Client submits the daily check-in form with measurement values, THE System SHALL persist those values in the corresponding `DailyLog` record; IF a measurement field is left blank or omitted, THE System SHALL store `null` for that field, not zero.
3. IF a Client submits a daily log for a date that already has a record, THEN THE System SHALL update the existing `DailyLog` record rather than inserting a new one.
4. IF a Client submits a daily log for today and a record already exists, THEN THE System SHALL display a "Updated today's log" confirmation message after saving.
5. IF a measurement field contains a value outside the range 0.1–999.9 or more than one decimal place, THEN THE System SHALL display an inline validation error for that field and prevent form submission.

---

### Requirement 9: WhatsApp Bulk Messaging — Opt-Out Footer

**User Story:** As an Admin, I want every bulk WhatsApp message to automatically include an opt-out instruction, so that the clinic complies with WhatsApp Business messaging policies.

#### Acceptance Criteria

1. WHEN a bulk WhatsApp campaign is dispatched (CSV broadcast or registered-client broadcast), THE System SHALL append two newlines followed by the value stored in `Setting` key `WhatsApp:OptOutFooter` to every outbound message body.
2. IF the `WhatsApp:OptOutFooter` setting has not been configured, THEN THE System SHALL use the default value `"Reply STOP to unsubscribe."` as the footer text.
3. THE System SHALL always append the opt-out footer to every bulk message body, regardless of whether the admin's template already contains similar text.
4. IF appending the opt-out footer causes the total message body length to exceed 4096 characters, THEN THE System SHALL truncate the message body (excluding the footer) to fit within the limit, and display an inline warning to the Admin before dispatch.

---

### Requirement 10: WhatsApp Bulk Messaging — Dry-Run Preview

**User Story:** As an Admin, I want to preview the personalised message that each CSV recipient would receive before sending the campaign, so that I can review content and catch errors before delivery.

#### Acceptance Criteria

1. WHEN an Admin clicks the Preview button for a loaded CSV campaign, THE System SHALL render the personalised message for up to 500 recipient rows by substituting `{{Name}}` and `{Name}` placeholders (case-insensitive) with each recipient's name value from the CSV, and appending the configured opt-out footer text.
2. WHEN the preview is rendered, THE System SHALL NOT create any `WhatsAppLog` record, invoke any Twilio API call, or write to any database table.
3. WHEN an Admin runs the preview multiple times with the same template and recipients, THE System SHALL produce identical output each time.
4. IF the CSV recipient list is empty when the Preview button is clicked, THEN THE System SHALL display a validation message instructing the Admin to upload a CSV file first.
5. IF a recipient's name cell in the CSV is blank or null, THEN THE System SHALL render the placeholder token literally (e.g. `{{Name}}`) in the preview output for that row.
6. IF the message template contains a placeholder that does not correspond to a known CSV column (`Name`), THEN THE System SHALL preserve that placeholder text literally in the preview output without substitution.

---

### Requirement 11: WhatsApp Bulk Messaging — Rate Limiting and Progress Indicator

**User Story:** As an Admin, I want bulk message dispatch to respect Twilio's rate limits and show me a progress indicator, so that the campaign completes reliably without hitting API errors.

#### Acceptance Criteria

1. WHEN a bulk WhatsApp campaign is dispatched, THE System SHALL introduce a per-message delay between consecutive sends equal to the value stored in `Setting` key `WhatsApp:BulkDelayMs`, which must be within the range 100–60000 milliseconds inclusive.
2. IF the `WhatsApp:BulkDelayMs` setting has not been configured, OR if the configured value is outside the range 100–60000 ms, THEN THE System SHALL use a default delay of 1000 milliseconds.
3. WHILE a bulk campaign dispatch is in progress, THE System SHALL display a progress indicator showing the number of messages sent (accepted by the Twilio API) and the number failed, out of the total recipient count, updating after each API response.
4. IF a Twilio API call returns an error for an individual recipient, THE System SHALL log that recipient as failed, increment the failed count in the progress indicator, and continue dispatching to the remaining recipients.

---

### Requirement 12: WhatsApp Bulk Messaging — Delivery Status in Activity Log

**User Story:** As an Admin, I want to see per-recipient delivery status in the WhatsApp activity log, so that I can identify failed sends and take corrective action.

#### Acceptance Criteria

1. WHEN the WhatsApp activity log is displayed, THE System SHALL attempt to parse the `LogDetail` JSON field of each `WhatsAppLog` record and display per-recipient status badges (`"Delivered"`, `"Failed"`, or `"Simulated"`) for each entry; IF the `LogDetail` field is missing, null, or cannot be parsed as valid JSON, THE System SHALL display a `"No detail available"` label for that entry.
2. IF a Twilio API call returns a non-success response or throws an exception for an individual recipient during a bulk dispatch, THEN THE System SHALL record `"Failed"` as the status for that recipient in the `WhatsAppLog.LogDetail` JSON.

---

### Requirement 13: Service Booking Page — Public Anonymous Access

**User Story:** As a Visitor (prospective patient, referral, or walk-in enquiry), I want to submit a booking request without needing to log in or create an account, so that I can express my interest in clinic services frictionlessly.

#### Acceptance Criteria

1. THE System SHALL serve the `/book` page without requiring authentication; no `[Authorize]` attribute or login redirect SHALL be applied to this route.
2. WHEN the `/book` page loads, THE System SHALL render all form fields (full name, phone number, preferred date/time, notes) as blank, with no session or account data pre-filled, regardless of whether the visitor is authenticated.
3. WHEN the `/book` page loads, THE System SHALL display a set of service selection cards for each available service category, each showing the service name, duration estimate, and description; IF no service categories are configured, THE System SHALL display a placeholder message and disable the booking form submission button.
4. WHEN a Visitor selects a service card, THE System SHALL apply a visually distinct selected style to that card, deselect any previously selected card, and populate the `ServiceType` field in the booking form with the selected service name.
5. WHEN a Visitor clicks the submit button on the booking form with all required fields valid, THE System SHALL attempt to create a `BookingRequest` and proceed with the submission flow defined in Requirement 14.
6. WHEN the `/book` page loads, THE System SHALL treat the `Notes` field as optional; full name, phone number, preferred date/time, and service type SHALL be required fields.

---

### Requirement 14: Service Booking Page — Booking Submission and Confirmation

**User Story:** As a Visitor, I want to submit my booking request and receive immediate confirmation, so that I know the clinic has been notified and will follow up with me.

#### Acceptance Criteria

1. WHEN a Visitor submits the booking form with valid inputs, THE System SHALL create a `BookingRequest` record with `Status = "Pending"` and `SubmittedAt` set to the current UTC datetime.
2. WHEN a `BookingRequest` is persisted, THE System SHALL send a WhatsApp notification to the clinic's configured number containing the visitor's full name, phone number, selected service, preferred date, and any notes; THE System SHALL retry the notification up to 3 times at 20-second intervals before marking it as failed.
3. WHEN a booking is successfully submitted, THE System SHALL replace the booking form with a success confirmation panel informing the visitor that the clinic will contact them via WhatsApp or phone call.
4. IF the `FullName` field is empty, fewer than 2 characters, or exceeds 100 characters, THEN THE System SHALL prevent form submission and display an inline validation error on the name field.
5. IF the `PhoneNumber` field does not match a valid 10-digit Indian phone number (optionally prefixed with `+91` or `91`), THEN THE System SHALL prevent form submission and display an inline validation error on the phone field.
6. IF the `PreferredDate` is not at least 1 hour and no more than 90 days in the future relative to the submission time, THEN THE System SHALL prevent form submission and display an inline validation error on the date field.
7. IF no service type has been selected when the form is submitted, THEN THE System SHALL prevent form submission and display an inline validation error on the service field.
8. IF the WhatsApp notification to the clinic fails after all 3 retry attempts once a `BookingRequest` is persisted, THEN THE System SHALL retain the `BookingRequest` record with `Status = "Pending"` and display a visible failed-notification count badge on the Admin Bookings tab.
9. IF persisting the `BookingRequest` record itself fails, THEN THE System SHALL display an inline error message on the booking form, preserve all form field values, and take no further action.

---

### Requirement 15: Service Booking Page — Rate Limiting

**User Story:** As a system operator, I want the public booking page to be protected against submission abuse, so that the clinic database and messaging quota are not exhausted by automated or malicious submissions.

#### Acceptance Criteria

1. IF a single IP address has submitted 5 or more booking requests within the current rolling 60-minute window, THEN THE System SHALL reject additional booking submissions from that IP with an HTTP 429 response.
2. WHEN a booking submission is rate-limited, THE System SHALL display a message stating the submission limit has been reached and instructing the visitor to try again after 60 minutes from their first submission in the current window.
3. WHEN the oldest submission in the rolling window falls outside the 60-minute window, THE System SHALL remove it from the count, allowing new submissions from that IP once the count drops below 5.

---

### Requirement 16: Admin Bookings Tab — BookingRequest Management

**User Story:** As an Admin, I want to view and manage all public booking requests from a dedicated tab in the Admin dashboard, so that I can confirm or cancel requests and communicate with prospective patients directly.

#### Acceptance Criteria

1. THE System SHALL provide a "Bookings" tab in the Admin dashboard displaying all `BookingRequest` records ordered by descending `SubmittedAt` date; IF no `BookingRequest` records exist, THE System SHALL display an empty-state message.
2. WHEN an Admin updates a `BookingRequest` status, THE System SHALL only accept the following transitions: `"Pending"` → `"Confirmed"`, `"Pending"` → `"Cancelled"`, and `"Confirmed"` → `"Cancelled"`.
3. IF an Admin attempts a status transition that is not permitted (e.g. `"Confirmed"` → `"Pending"` or any transition from `"Cancelled"`), THEN THE System SHALL reject the update, display an inline error message adjacent to the affected record, and leave the status unchanged.
4. WHEN an Admin clicks the WhatsApp reply button for a `BookingRequest`, THE System SHALL open a pre-composed WhatsApp link addressed to the booking's `PhoneNumber` containing the visitor's name and booking reference; IF the `PhoneNumber` is missing or malformed, THE System SHALL display an inline error and disable the reply button for that record.

---

### Requirement 17: Design Consistency — Dark Glass-Morphism Token System

**User Story:** As a clinic administrator and as a client, I want every new and updated page to look and feel consistent with the existing dark glass-morphism design language, so that the application is visually cohesive and professional throughout.

#### Acceptance Criteria

1. THE System SHALL apply the `glass-panel` CSS class to all new block-level elements that group related content as a card or panel container in the `/book` page, Meal Review tab, Admin Bookings tab, and any new macro input panels.
2. THE System SHALL use only the defined DesignToken CSS custom properties (`--bg-glass`, `--border-glass`, `--accent-teal`, `--accent-blue`, `--accent-purple`, `--accent-success`, `--accent-warning`, `--accent-danger`, `--text-secondary`, `--grad-primary`) for all colour and gradient values in new UI components.
3. THE System SHALL apply the `btn-clinic` class to all primary action buttons (sole or main CTA in a form or modal), `btn-clinic-secondary` to supplementary/ghost buttons, and `btn-clinic-danger` to buttons that permanently delete or cancel data, across all new components.
4. THE System SHALL apply the `clinic-label` and `clinic-input` classes to all form labels and input elements in new components.
5. THE System SHALL apply the `soft-pill` class to all elements displaying appointment, booking, or meal state labels in new components.
6. THE System SHALL add the following CSS utility classes to `app.css`, each carrying the design-system-defined styles used across its respective component scope: `.macro-slider-group`, `.macro-bar`, `.meal-timeline`, `.service-card`, `.booking-success`, and `.gallery-grid`.
7. WHEN `.service-card` elements are rendered, THE System SHALL apply a `box-shadow` using `--accent-teal` on hover, and a `background-color` or `border-color` change using a defined design token to indicate the selected state, visually distinct from the unselected state.
8. THE System SHALL apply the design-system requirements in this section exclusively to the following new UI areas: the `/book` service booking page, the Admin Meal Review tab, the Admin Bookings tab, and the Client Portal macro input panel.

---

### Requirement 18: Phone Number Normalisation

**User Story:** As a system operator, I want phone numbers to be normalised consistently before any WhatsApp link is generated, so that messages are routed to the correct number regardless of how the number was originally entered.

#### Acceptance Criteria

1. WHEN `NormalisePhone` is applied to a phone number string, THE System SHALL produce a 12-digit E.164-compatible string by: (a) removing all non-digit characters, (b) stripping a leading `00` prefix if present, then stripping a leading `91` prefix if the result has more than 10 digits, and (c) prepending `91` to the result if it has exactly 10 digits.
2. IF the digit-only result after prefix stripping does not equal exactly 10 digits, THEN THE System SHALL treat the input as invalid, log an error, and return an empty string rather than generating a WhatsApp link.
3. WHEN `NormalisePhone` is applied to an already-normalised phone number string (12-digit E.164), THE System SHALL produce the same output as a single application of `NormalisePhone` (idempotency).

---

### Requirement 19: File Upload Security

**User Story:** As a system operator, I want all image file uploads to be validated on the server side before being persisted, so that malicious files cannot be stored or served through the application.

#### Acceptance Criteria

1. WHEN a file upload is processed (meal photo or profile photo), THE System SHALL verify both the file extension and the declared MIME content-type are within the permitted set (`.jpg`/`image/jpeg`, `.jpeg`/`image/jpeg`, `.png`/`image/png`, `.webp`/`image/webp`) before writing any bytes to the filesystem.
2. WHEN a file upload is processed, THE System SHALL verify the file size does not exceed 8 MB before writing any bytes to the filesystem.
3. IF an uploaded file fails either the extension/MIME check or the size check, THEN THE System SHALL discard the upload without writing any bytes to the filesystem, display an inline `clinic-alert clinic-alert-danger` message adjacent to the file input field indicating the reason for rejection (invalid file type or file size exceeded), and allow submission of all other form fields to proceed.
