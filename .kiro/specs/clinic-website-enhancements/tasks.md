# Implementation Plan: Clinic Website Enhancements

## Overview

This plan breaks the six enhancement areas into incremental coding tasks for the ProTouch Blazor Server application. Tasks are ordered so that the data layer is established first, then the service layer, then presentation components, then design polish, and finally the test suite. No task depends on a later task being complete.

The implementation language is **C# (.NET 8 / Blazor Server)**. Property-based tests use **xUnit + FsCheck**.

---

## Tasks

---

### 1. Data layer — `BookingRequest` model, `ApplicationDbContext` update, and EF Core migration

- [ ] 1.1 Add `BookingRequest` model to `ClinicApp/Data/Models.cs`
  - Define all properties as specified in the design (`Id`, `FullName`, `PhoneNumber`, `ServiceType`, `PreferredDate`, `Notes`, `SubmittedAt`, `Status`)
  - Add `[Required]`, `[MaxLength]`, and default value annotations
  - _Requirements: 14.1, 16.1_

- [ ] 1.2 Register `BookingRequests` DbSet in `ApplicationDbContext`
  - Add `public DbSet<BookingRequest> BookingRequests { get; set; }` to `ApplicationDbContext.cs`
  - _Requirements: 14.1_

- [ ] 1.3 Configure cascade-delete FK relationships in `ApplicationDbContext.OnModelCreating`
  - Configure `DailyLog`, `AttendanceRecord`, `Appointment`, `PaymentNotification` → `ApplicationUser` with cascade delete
  - _Requirements: 2.4_

- [ ]* 1.4 Write unit test: `BookingRequest` default `Status` is `"Pending"` and `SubmittedAt` is UTC
  - Instantiate `BookingRequest` directly and assert defaults hold
  - _Requirements: 14.1_

- [ ] 1.5 Add EF Core migration `AddBookingRequestAndCascades`
  - Run `dotnet ef migrations add AddBookingRequestAndCascades` in `ClinicApp/`
  - Verify the generated migration file includes the `BookingRequests` table and any new FK cascade constraints
  - _Requirements: 14.1, 2.4_

---

### 2. Service layer — `ClinicService` extensions (visit, meal log, daily log upsert)

- [ ] 2.1 Add `DeleteAttendanceRecordAsync(int recordId)` to `ClinicService`
  - Find and remove the `AttendanceRecord`; no-op if not found (log error)
  - _Requirements: 3.7, 3.8_

- [ ] 2.2 Add `UpdateAttendanceRecordAsync(AttendanceRecord record)` to `ClinicService`
  - Update treatment type, therapist, notes, next appointment date, checkout time, and status
  - Validate: therapist not empty, notes ≤ 2000 chars, checkout time ≥ check-in time
  - Throw or return a validation result on violation
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [ ] 2.3 Add `GetAllMealLogsAsync()` to `ClinicService`
  - Query `DailyLogs` where `MealPhotoUrl` is not null/empty
  - Include `User` navigation property
  - Order by descending `LogDate`
  - Support skip/take parameters for pagination (page size 20)
  - _Requirements: 5.2, 5.4, 5.5_

- [ ] 2.4 Add `DeleteDailyLogAsync(int logId)` to `ClinicService`
  - Find and remove the `DailyLog`; surface error if not found
  - _Requirements: 5.3_

- [ ] 2.5 Add `UpdateDailyLogAsync(DailyLog log)` to `ClinicService`
  - Check for existing record with same `UserId` + `LogDate`; update if found, insert if not
  - Validate macro sum = 100 before persisting; throw `InvalidOperationException` if sum ≠ 100
  - _Requirements: 7.4, 7.5, 8.3, 8.4_

- [ ]* 2.6 Write property test for `UpdateDailyLogAsync` — no duplicates per user per day (Property 6)
  - **Property 6: Meal Log Upsert — No Duplicates per User per Day**
  - **Validates: Requirements 8.3**
  - Generate arbitrary `(userId, logDate)` pairs; call `UpdateDailyLogAsync` N times for the same pair; assert exactly one record exists

- [ ]* 2.7 Write property test for `UpdateDailyLogAsync` — macro sum invariant (Property 1)
  - **Property 1: Macro Sum Invariant**
  - **Validates: Requirements 7.1, 7.3, 7.4**
  - Generate `DailyLog` values where sum ≠ 100; assert service throws; generate valid sums and assert persistence succeeds

---

### 3. Service layer — `ClinicService` extensions (booking request)

- [ ] 3.1 Add `SubmitBookingRequestAsync(BookingRequest request)` to `ClinicService`
  - Validate `FullName` (2–100 chars), `PhoneNumber` (Indian pattern), `PreferredDate` (1 hr–90 days future), `ServiceType` non-empty
  - Persist with `Status = "Pending"` and `SubmittedAt = DateTime.UtcNow`
  - Call `SendWhatsAppMessageAsync` to the clinic number with the notification format from the design
  - Implement retry: up to 3 attempts at 20-second intervals on WhatsApp failure
  - Return the new `BookingRequest.Id`
  - _Requirements: 14.1, 14.2, 14.4, 14.5, 14.6, 14.7, 14.8, 14.9_

- [ ] 3.2 Add `GetAllBookingRequestsAsync()` to `ClinicService`
  - Return all `BookingRequest` records ordered by descending `SubmittedAt`
  - _Requirements: 16.1_

- [ ] 3.3 Add `UpdateBookingRequestStatusAsync(int id, string newStatus)` to `ClinicService`
  - Enforce valid transitions: `Pending→Confirmed`, `Pending→Cancelled`, `Confirmed→Cancelled`
  - Throw `InvalidOperationException` for any other transition
  - _Requirements: 16.2, 16.3_

- [ ]* 3.4 Write property test for `SubmitBookingRequestAsync` — default status and UTC timestamp (Property 2)
  - **Property 2: Booking Request Default Status**
  - **Validates: Requirements 14.1**
  - For any valid `BookingRequest` input, assert `Status = "Pending"` and `SubmittedAt` is UTC and ≤ `DateTime.UtcNow`

- [ ]* 3.5 Write property test for `SubmitBookingRequestAsync` — preferred date in future (Property 3)
  - **Property 3: Booking Date Must Be in the Future**
  - **Validates: Requirements 14.6**
  - Generate `PreferredDate` values in the past; assert service rejects them; generate valid future dates and assert acceptance

- [ ]* 3.6 Write property test for `UpdateBookingRequestStatusAsync` — status transitions monotone (Property 10)
  - **Property 10: BookingRequest Status Transitions Are Monotone**
  - **Validates: Requirements 16.2, 16.3**
  - Generate all invalid transition pairs; assert `InvalidOperationException` is thrown for each

---

### 4. Service layer — `ClinicService` extensions (WhatsApp enhancements)

- [ ] 4.1 Add opt-out footer appending to `SendCsvWhatsAppCampaignAsync` and `SendBulkWhatsAppCampaignAsync`
  - Read `Setting` key `WhatsApp:OptOutFooter`; fall back to `"Reply STOP to unsubscribe."` if absent
  - Append two newlines + footer to every outbound message body before dispatch
  - If total message length > 4096 chars, truncate the body (not the footer) and surface a warning flag in the return value
  - _Requirements: 9.1, 9.2, 9.3, 9.4_

- [ ] 4.2 Add `PreviewCsvCampaignAsync(string template, IEnumerable<CsvRecipient> recipients)` to `ClinicService`
  - Return a `List<CsvPreviewRow>` with personalised message per recipient (substitute `{{Name}}` / `{Name}`, append opt-out footer)
  - Must not write to any DB table or call any Twilio API
  - Preserve unknown placeholders literally; use `{{Name}}` literally for blank/null name cells
  - _Requirements: 10.1, 10.2, 10.3, 10.5, 10.6_

- [ ] 4.3 Add per-message delay and progress tracking to `SendCsvWhatsAppCampaignAsync` / `SendBulkWhatsAppCampaignAsync`
  - Read `Setting` key `WhatsApp:BulkDelayMs`; validate range 100–60000; default to 1000 if absent or out of range
  - Accept a `IProgress<(int sent, int failed, int total)>` callback parameter for UI progress reporting
  - On individual Twilio failure: log `"Failed"` for that recipient, increment failed count, continue
  - _Requirements: 11.1, 11.2, 11.3, 11.4_

- [ ] 4.4 Update phone normalisation in `BuildWhatsAppLink` to comply with Requirement 18
  - Strip all non-digit chars
  - Strip leading `00` prefix; then strip leading `91` if result > 10 digits
  - Prepend `91` if result is exactly 10 digits
  - Return empty string and log error if result is not exactly 10 digits after stripping
  - _Requirements: 18.1, 18.2, 18.3_

- [ ]* 4.5 Write property test for phone normalisation idempotency (Property 4)
  - **Property 4: Phone Normalisation Idempotency**
  - **Validates: Requirements 18.2**
  - Generate arbitrary phone strings; assert `NormalisePhone(NormalisePhone(p)) = NormalisePhone(p)` for all inputs

- [ ]* 4.6 Write property test for CSV preview produces no side effects (Property 5)
  - **Property 5: CSV Preview Produces No Side Effects**
  - **Validates: Requirements 10.2, 10.3**
  - Record `WhatsAppLog` count before; call `PreviewCsvCampaignAsync` N times; assert count unchanged after all calls

- [ ]* 4.7 Write property test for opt-out footer always present in bulk messages (Property 9)
  - **Property 9: Opt-Out Footer Always Present in Bulk Messages**
  - **Validates: Requirements 9.1, 9.3**
  - Generate arbitrary templates and recipient lists; dispatch via mock mode; assert every logged message body contains the footer value

---

### 5. Checkpoint — service layer complete

- [ ] 5. Checkpoint — Ensure all service-layer unit and property tests pass; ask the user if questions arise.

---

### 6. Data layer — file upload security helper

- [ ] 6.1 Extract and harden the `SaveImageAsync` method in `ClientDashboard.razor` into a shared `FileUploadService` or static helper in `ClinicApp/Services/`
  - Validate both file extension and declared MIME content-type against allowlist (`.jpg`/`image/jpeg`, `.jpeg`/`image/jpeg`, `.png`/`image/png`, `.webp`/`image/webp`)
  - Validate file size ≤ 8 MB before writing any bytes
  - Return `(string? path, string? errorMessage)` so callers can display inline error without blocking other fields
  - _Requirements: 6.4, 6.5, 6.6, 19.1, 19.2, 19.3_

- [ ]* 6.2 Write property test for file upload extension allowlist enforcement (Property 8)
  - **Property 8: File Upload Extension Allowlist Enforcement**
  - **Validates: Requirements 6.4, 19.1**
  - Generate filenames with arbitrary extensions; assert rejected for any extension outside the allowlist

- [ ]* 6.3 Write property test for file upload size enforcement (Property 13)
  - **Property 13: File Upload Size Enforcement**
  - **Validates: Requirements 6.5, 19.2**
  - Generate file sizes above and at/below 8 MB; assert rejection iff size > 8 MB

---

### 7. Presentation layer — `app.css` design-system utility classes

- [ ] 7.1 Add new CSS utility classes to `ClinicApp/wwwroot/app.css`
  - Add `.macro-slider-group` — flex container for four macro sliders with live percentage labels, using `--accent-teal` thumb colour
  - Add `.macro-bar` — colour-coded horizontal bar (Carbs = teal, Protein = blue, Fat = purple, Veg = success green) using `--accent-*` tokens
  - Add `.meal-timeline` — vertical list of meal photo cards with date spine using `--border-glass` and `--bg-glass`
  - Add `.service-card` — extends `glass-panel`, hover `box-shadow` using `--accent-teal`, selected state `border-color` using `--accent-teal`
  - Add `.booking-success` — full-width confirmation panel with animated checkmark using `--grad-primary`
  - Add `.gallery-grid` — responsive CSS grid (`auto-fill`, `minmax(260px, 1fr)`, gap `1rem`) with `object-fit: cover` images
  - _Requirements: 17.6, 17.7_

---

### 8. Presentation layer — Home page image asset refresh

- [ ] 8.1 Rename and reorganise landing-page image assets in `wwwroot/images/`
  - Rename hero image to `hero-treatment.jpg`, "Why ProTouch" image to `care-panel.jpg`
  - Place all clinic gallery images under `wwwroot/images/clinic-gallery/` named `gallery-01.jpg` through `gallery-N.jpg`
  - Update all `<img>` `src` attributes in `Home.razor` to use paths beginning with `/images/`
  - Remove any filename matching the pattern `IMG-[0-9]{8}-WA[0-9]+` from markup and disk
  - _Requirements: 1.1, 1.5_

- [ ] 8.2 Fix duplicate `<PageTitle>` and duplicate services `<section>` in `Home.razor`
  - Remove the duplicate `<PageTitle>` tag leaving exactly one
  - Remove the duplicate services `<section>` element leaving exactly one
  - _Requirements: 1.2_

- [ ] 8.3 Add responsive photo gallery section to `Home.razor`
  - Enumerate images in `wwwroot/images/clinic-gallery/` at startup (or via a static list) and render each in the `.gallery-grid` CSS class
  - Use `object-fit: cover` on each `<img>` with `glass-panel` overlay style
  - If `wwwroot/images/clinic-gallery/` is empty, render a placeholder message and no `<img>` elements
  - Add a "Book an appointment" link pointing to `/book` in the landing page CTA area
  - _Requirements: 1.3, 1.4_

- [ ] 8.4 Configure `Cache-Control: max-age=31536000, immutable` header for `wwwroot/images/` static files in `Program.cs`
  - Use `StaticFileOptions` with a custom `OnPrepareResponse` for files under the `images` path prefix
  - _Requirements: 1.6_

---

### 9. Presentation layer — Admin dashboard enhancements (CRUD, Meal Review, Bookings tabs)

- [ ] 9.1 Add Edit and Delete actions to the Visits tab in `AdminDashboard.razor`
  - Add inline edit form per `AttendanceRecord` row (treatment, therapist, notes, next appointment, checkout time)
  - Add checkout button that calls `UpdateAttendanceRecordAsync` with the checkout time set
  - Add delete button per row with a confirmation prompt showing the visit date and client name
  - Wire to `ClinicService.UpdateAttendanceRecordAsync` and `DeleteAttendanceRecordAsync`
  - Display inline `clinic-alert clinic-alert-danger` on validation or not-found errors
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_

- [ ] 9.2 Add confirmation modal for client delete in `AdminDashboard.razor`
  - Before calling `UserManager.DeleteAsync`, query counts of related records (`DailyLog`, `AttendanceRecord`, `Appointment`, `PaymentNotification`)
  - Display a modal listing the counts; require explicit confirmation before proceeding
  - On failure, display inline error and leave all records unchanged
  - _Requirements: 2.3, 2.4_

- [ ] 9.3 Add inline user edit (name, email, phone) toggle to the "Add user" tab in `AdminDashboard.razor`
  - Add an "Edit existing user" mode toggle that loads the selected user's current values into the form
  - On save, update `ApplicationUser.FullName`, `.Email`, `.PhoneNumber` via `UserManager.UpdateAsync`
  - If the new email already exists on another account, display inline error and make no changes
  - _Requirements: 2.5, 2.6_

- [ ] 9.4 Add server-side validation for clinical measurement fields in `AdminDashboard.razor`
  - Reject height, start weight, target weight, waist, chest, hips, arms values that are negative, zero, or > 999.9
  - Display an inline validation error per out-of-range field before calling `UpdateClientProfileAsync`
  - _Requirements: 2.7_

- [ ] 9.5 Add Appointments sub-tab to `AdminDashboard.razor`
  - Load all appointments via `ClinicService.GetAllAppointmentsAsync()` ordered by descending date
  - Render each appointment with a status dropdown (`Scheduled`, `Confirmed`, `Completed`, `Cancelled`)
  - On status change, call `ClinicService.UpdateAppointmentStatusAsync` and update the row without page reload
  - On failure, display inline error and revert the displayed status
  - _Requirements: 4.1, 4.2, 4.3, 4.4_

- [ ] 9.6 Add Meal Review tab to `AdminDashboard.razor`
  - Add tab button "Meal Review" to the tab strip
  - Load paginated meal logs via `ClinicService.GetAllMealLogsAsync(page, pageSize: 20)`
  - Render each as a card showing client name, date, diet notes, and macro bar (using `.macro-bar` class) and thumbnail
  - Add delete button per card that calls `ClinicService.DeleteDailyLogAsync` with inline error on failure
  - Add pagination controls (Previous / Next)
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [ ] 9.7 Add Bookings tab to `AdminDashboard.razor`
  - Add tab button "Bookings" to the tab strip; show pending count badge on the stat card
  - Load all booking requests via `ClinicService.GetAllBookingRequestsAsync()`
  - Render each record with full name, phone, service type, preferred date, notes, status badge (`soft-pill`)
  - Add status dropdown for valid transitions (`Pending→Confirmed`, `Pending→Cancelled`, `Confirmed→Cancelled`); call `UpdateBookingRequestStatusAsync`; display inline error if transition is rejected
  - Add WhatsApp reply button per record using `ClinicService.BuildWhatsAppLink`; disable and show error if phone is missing or malformed
  - Display empty-state message when no booking requests exist
  - _Requirements: 16.1, 16.2, 16.3, 16.4_

---

### 10. Checkpoint — Admin dashboard enhancements complete

- [ ] 10. Checkpoint — Verify admin CRUD flows work end-to-end; ensure all tests pass; ask the user if questions arise.

---

### 11. Presentation layer — WhatsApp Center UI enhancements

- [ ] 11.1 Add template-approval notice banner to the WhatsApp Center tab in `AdminDashboard.razor`
  - Render a persistent `clinic-alert` info banner with Twilio console link explaining pre-approved template requirement
  - _Requirements: (design Section 4 WhatsApp policy guidance)_

- [ ] 11.2 Add Preview button and dry-run preview panel to the WhatsApp Center tab
  - Add "Preview" button next to "Send Bulk Campaign"
  - On click, call `ClinicService.PreviewCsvCampaignAsync` and display personalised message per recipient in a scrollable panel (up to 500 rows)
  - Display validation message if CSV is empty
  - _Requirements: 10.1, 10.4_

- [ ] 11.3 Add progress bar and per-message status feedback during bulk dispatch
  - Pass an `IProgress<(int sent, int failed, int total)>` callback into the campaign methods
  - Render a progress bar in the UI showing `sent / total` with failed count badge, updating after each send
  - On dispatch complete, show final summary (sent, failed)
  - _Requirements: 11.3, 11.4_

- [ ] 11.4 Expand delivery status column in the WhatsApp activity log
  - Parse `LogDetail` JSON for each `WhatsAppLog` entry; render per-recipient `soft-pill` badges (`Delivered`, `Failed`, `Simulated`)
  - If `LogDetail` is null, empty, or invalid JSON, render `"No detail available"` label
  - _Requirements: 12.1, 12.2_

---

### 12. Presentation layer — Client portal macro sliders and measurement fields

- [ ] 12.1 Replace macro `InputNumber` fields with interactive slider group in `ClientDashboard.razor`
  - Render four `<input type="range">` elements inside `.macro-slider-group` for `CarbsPercentage`, `ProteinPercentage`, `FatPercentage`, `VegPercentage`
  - On each slider change, adjust other sliders proportionally to maintain sum = 100
  - Initialise sliders from the latest `DailyLog` macros or to defaults (35/30/15/20) if no prior log
  - _Requirements: 7.1, 7.2_

- [ ] 12.2 Add live `.macro-bar` colour-coded bar below the slider group
  - Render a single flex-row bar divided into four colour-coded segments using `--accent-teal`, `--accent-blue`, `--accent-purple`, `--accent-success`
  - Update segment widths reactively as sliders change
  - _Requirements: 7.3_

- [ ] 12.3 Add macro sum validation to the daily check-in form submission handler
  - If `CarbsPercentage + ProteinPercentage + FatPercentage + VegPercentage ≠ 100` on submit, prevent form submission and display inline validation error adjacent to the slider group
  - _Requirements: 7.4_

- [ ] 12.4 Add `WaistInches`, `ChestInches`, `HipsInches`, `ArmsInches` optional input fields to the daily check-in form
  - Add four optional `<input type="number" step="0.1">` fields inside `.row.g-3` in the check-in form
  - Accept blank as null (not zero); validate range 0.1–999.9 with max one decimal place
  - Display inline validation error per out-of-range field and prevent submission if invalid
  - _Requirements: 8.1, 8.2, 8.5_

- [ ] 12.5 Update `HandleLogSubmission` in `ClientDashboard.razor` to call `UpdateDailyLogAsync` (upsert)
  - Replace `AddDailyLogAsync` with `UpdateDailyLogAsync` to support same-day updates
  - Display "Updated today's log" confirmation message if a record for today already existed
  - _Requirements: 8.3, 8.4_

- [ ] 12.6 Add client-side meal photo preview before form submission in `ClientDashboard.razor`
  - On `InputFile` change, read the selected file into a local data URL and render as a preview `<img>` above the submit button
  - _Requirements: 6.1_

- [ ] 12.7 Add scrollable meal photo timeline section to `ClientDashboard.razor`
  - Below the check-in form, query all `DailyLog` records for the current client where `MealPhotoUrl` is non-empty
  - Render using the `.meal-timeline` CSS class; each entry shows thumbnail, date, diet notes
  - Add a toggle ("Log Entry" / "Photo History") to switch between the log form and timeline view
  - _Requirements: 6.2, 6.3_

- [ ] 12.8 Update `ClientDashboard.razor` to use the hardened `SaveImageAsync` helper from Task 6.1
  - Replace inline upload logic with calls to the shared helper
  - Display `clinic-alert clinic-alert-danger` adjacent to the file input on rejection; do not block other fields
  - _Requirements: 6.4, 6.5, 6.6, 19.1, 19.2, 19.3_

---

### 13. Checkpoint — Client portal enhancements complete

- [ ] 13. Checkpoint — Verify client portal macro sliders, measurement fields, and meal photo flows work; ask the user if questions arise.

---

### 14. Presentation layer — Public Service Booking page (`/book`)

- [ ] 14.1 Create `ClinicApp/Components/Pages/ServiceBooking.razor` at route `/book`
  - Do NOT add `[Authorize]` attribute — page must be accessible anonymously
  - Render service selection cards using `.service-card` CSS class (seeded static service list)
  - On card selection, apply selected style and populate `ServiceType` field; deselect previous card
  - If no service categories exist, show placeholder message and disable submit button
  - Render blank booking form: full name, phone number, preferred date/time picker, optional notes
  - Form fields must always be blank on load — read NO session or account data regardless of auth state
  - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.6_

- [ ] 14.2 Implement form submission handler in `ServiceBooking.razor`
  - Validate all required fields with inline errors (name 2–100 chars, Indian phone, future date 1 hr–90 days, service selected)
  - On valid submit, call `ClinicService.SubmitBookingRequestAsync`; on success, replace form with `.booking-success` panel
  - On `BookingRequest` persistence failure, display inline error and preserve all field values
  - _Requirements: 13.5, 14.1, 14.2, 14.3, 14.4, 14.5, 14.6, 14.7, 14.9_

- [ ] 14.3 Add ASP.NET Core rate limiter middleware for `/book` endpoint in `Program.cs`
  - Register a sliding-window rate limiter: max 5 requests per IP per 60-minute window
  - On HTTP 429, display in-page message (via middleware or Blazor error handling) instructing visitor to retry after 60 minutes
  - _Requirements: 15.1, 15.2, 15.3_

- [ ]* 14.4 Write property test for booking form always renders blank (Property 11)
  - **Property 11: Booking Form Always Renders Blank on `/book`**
  - **Validates: Requirements 13.1, 13.2**
  - Simulate page load for authenticated and unauthenticated sessions; assert all form fields are empty strings / null

- [ ]* 14.5 Write property test for booking phone validation rejects invalid numbers (Property 12)
  - **Property 12: Booking Phone Validation Rejects Invalid Numbers**
  - **Validates: Requirements 14.5**
  - Generate phone strings that do not match the Indian 10-digit pattern; assert `SubmitBookingRequestAsync` rejects each

---

### 15. Presentation layer — `GetAllMealLogsAsync` completeness property test

- [ ]* 15.1 Write property test for `GetAllMealLogsAsync` returns only logs with photos (Property 7)
  - **Property 7: Meal Review Returns Only Logs With Photos**
  - **Validates: Requirements 5.2, 5.5**
  - Seed logs with and without `MealPhotoUrl`; assert result contains only non-empty-photo logs; assert all non-empty-photo logs appear

---

### 16. Final wiring — navigation and integration

- [ ] 16.1 Add "Book Now" navigation link on the public `Home.razor` page pointing to `/book`
  - Place as a prominent CTA button in the hero section using `btn-clinic` class
  - _Requirements: 13.1_

- [ ] 16.2 Register the `BookingRequests` tab stat card in the Admin dashboard summary grid
  - Add a stat card showing count of pending `BookingRequest` records; clicking opens the Bookings tab
  - _Requirements: 16.1_

- [ ] 16.3 Verify all new UI components use only design-token CSS variables and prescribed CSS classes
  - Audit `ServiceBooking.razor`, Meal Review tab markup, Admin Bookings tab markup, and macro slider panel
  - Replace any hard-coded colours with `var(--accent-*)` / `var(--bg-glass)` / `var(--border-glass)` equivalents
  - Ensure all form labels/inputs use `clinic-label` / `clinic-input`, all CTAs use `btn-clinic` / `btn-clinic-secondary` / `btn-clinic-danger`, all status pills use `soft-pill`
  - _Requirements: 17.1, 17.2, 17.3, 17.4, 17.5, 17.8_

---

### 17. Final checkpoint

- [ ] 17. Final Checkpoint — Run the full test suite; ensure all unit tests, property tests, and integration smoke tests pass; review all 19 requirements for coverage; ask the user if questions arise.

---

## Notes

- Tasks marked with `*` are optional and can be skipped for a faster MVP
- Property tests use **xUnit + FsCheck** — add the `FsCheck.Xunit` NuGet package to the test project
- The `/book` page is **strictly public**: no `[Authorize]` attribute, no session pre-fill, no `Appointment` model used — only `BookingRequest`
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation at the end of each major phase
- Property tests should use an in-memory SQLite `ApplicationDbContext` to avoid hitting the real DB

---

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.3"] },
    { "id": 1, "tasks": ["1.4", "1.5", "7.1"] },
    { "id": 2, "tasks": ["2.1", "2.2", "2.3", "2.4", "2.5", "3.1", "3.2", "3.3", "4.1", "4.2", "4.3", "4.4", "6.1"] },
    { "id": 3, "tasks": ["2.6", "2.7", "3.4", "3.5", "3.6", "4.5", "4.6", "4.7", "6.2", "6.3"] },
    { "id": 4, "tasks": ["8.1", "8.2", "8.3", "8.4", "9.1", "9.2", "9.3", "9.4", "9.5", "9.6", "9.7", "11.1", "11.2", "11.3", "11.4", "12.1", "12.2", "12.3", "12.4", "12.5", "12.6"] },
    { "id": 5, "tasks": ["12.7", "12.8", "14.1", "14.2", "14.3", "15.1"] },
    { "id": 6, "tasks": ["14.4", "14.5"] },
    { "id": 7, "tasks": ["16.1", "16.2", "16.3"] }
  ]
}
```
