# Bugfix Requirements Document

## Introduction

This document captures confirmed bugs discovered through live site inspection of
https://protouch.onrender.com/ and static code review of the ProTouch Clinic application
(`ClinicApp`). The bugs span authentication, dashboard data loading, markup correctness,
data integrity, and utility logic. Eleven defects are catalogued across four categories:

- **Critical (Bugs 1–2)**: Users cannot log in; dashboards do not load after sign-in.
- **Data integrity (Bugs 5–7)**: Duplicate daily log entries; orphaned records on client
  deletion; macro percentages that can go negative in the seed data.
- **Configuration (Bug 8)**: New registrants are permanently blocked from signing in.
- **UI / markup (Bugs 3–4)**: Duplicate HTML elements and broken image URLs on the home page.
- **Utility logic (Bugs 9–11)**: Missing `StateHasChanged` calls, and a double-prefix edge
  case in WhatsApp link generation.

---

## Bug Analysis

### Current Behavior (Defect)

**Bug 1 — Login redirect loop**

1.1 WHEN a user navigates directly to `/Account/Login` (no `ReturnUrl` query parameter) and submits valid credentials THEN the system stays on `/Account/Login` indefinitely, showing no error and performing no redirect

1.2 WHEN `RedirectManager.RedirectTo(ReturnUrl)` is called with `ReturnUrl = null` THEN the system calls `NavigationManager.NavigateTo("")` which redirects to `/`, but the Identity middleware immediately bounces the authenticated request back to `/Account/Login`, creating an infinite redirect loop

1.3 WHEN a user successfully authenticates THEN the system does not inspect their role and therefore cannot route Admin users to `/admin-dashboard` or Client users to `/client-dashboard`

---

**Bug 2 — Dashboard data does not load after login**

2.1 WHEN a user is redirected to `/client-dashboard` immediately after an SSR login POST THEN the system renders `ClientDashboard.razor` with `currentUser = null` and displays the "Profile record could not be resolved" error panel

2.2 WHEN `OnInitializedAsync` completes with `currentUser = null` (auth state not yet propagated) THEN the system never retries and the dashboard remains stuck in the error state for the lifetime of the session

2.3 WHEN `LoadAdminDataAsync` completes in `AdminDashboard.razor` THEN the system does not call `StateHasChanged()`, so some UI actions (Refresh, Save visit) appear to do nothing until the next render cycle

---

**Bug 3 — Duplicate `<PageTitle>` and `<section id="services">` in Home.razor**

3.1 WHEN the home page is rendered THEN the system emits two `<PageTitle>` components, producing duplicate `<title>` tags in the document head

3.2 WHEN the home page is rendered THEN the system emits two `<section id="services">` blocks with identical content, producing duplicate DOM ids, duplicate visible content, and broken `#services` anchor navigation

---

**Bug 4 — Broken hero and care-panel images on Home.razor**

4.1 WHEN the home page is loaded in a browser or served from a web host THEN the system fails to serve `WhatsApp Image 2026-08-12 at 12.21.53 AM (1).jpeg` and `WhatsApp Image 2026-08-12 at 12.21.53 AM.jpeg` because filenames with spaces and parentheses are not reliably URL-encoded by all web servers and CDNs, causing broken image placeholders

---

**Bug 5 — Duplicate daily log entries on repeated form submission**

5.1 WHEN a client submits the daily weight/diet check-in form when a log for today already exists THEN the system inserts a new `DailyLog` record rather than updating the existing one, producing duplicate rows in the database for the same `UserId` and `LogDate`

5.2 WHEN `logModel` is loaded from an existing `DailyLog` (where `logModel.Id > 0`) and `HandleLogSubmission` is called THEN the system calls `_db.DailyLogs.Add(logModel)` which Entity Framework Core treats as an INSERT, not an UPDATE

---

**Bug 6 — Orphaned clinical records after client deletion**

6.1 WHEN an admin deletes a client from the Admin Dashboard THEN the system calls `UserManager.DeleteAsync(user)` which removes the `AspNetUsers` row but leaves orphaned rows in `DailyLogs`, `AttendanceRecords`, `Appointments`, `PaymentNotifications`, `ClientProfiles`, and `Settings` (profile photo key)

6.2 WHEN the `UserId` foreign key references are left dangling THEN the system may throw referential integrity exceptions on subsequent queries or silently accumulate unreachable data in the database

---

**Bug 7 — Negative macro percentages in seed data**

7.1 WHEN `DbInitializer` seeds `DailyLog` records and the random values `carb + prot + fat > 100` THEN the system stores a negative integer in `VegPercentage` (e.g., `veg = 100 – 44 – 44 – 24 = -12`)

7.2 WHEN negative `VegPercentage` values are stored in the database THEN the system displays negative percentage values in the client dashboard UI

---

**Bug 8 — New registrants blocked from signing in due to unconfirmed email**

8.1 WHEN a user registers via the portal and completes phone OTP verification in `VerifyPhone.razor` THEN the system redirects to `Account/RegisterConfirmation` (because `user.EmailConfirmed = false`) rather than signing the user in

8.2 WHEN `Program.cs` has `options.SignIn.RequireConfirmedAccount = true` and a newly registered user attempts to sign in THEN the system returns `result.Succeeded = false` from `PasswordSignInAsync` with no error message displayed to the user, silently blocking access

---

**Bug 9 — `AdminDashboard` UI does not update after async actions**

9.1 WHEN an admin clicks "Save visit", "Save profile & role", or "Refresh" THEN the system completes the async data reload in `LoadAdminDataAsync` but does not call `StateHasChanged()`, so the UI may not reflect the updated data until the next external render trigger

---

**Bug 10 — (Covered by Bug 7)** Negative macro display values in the client dashboard are a symptom of Bug 7's seed data defect. No separate clause required.

---

**Bug 11 — Double country-code prefix in WhatsApp links**

11.1 WHEN a phone number longer than 12 digits and starting with `91` is passed to `BuildWhatsAppLink` (e.g., a number where the admin has entered the `91` prefix and the system has previously added another `91`) THEN the system produces a malformed `https://wa.me/91919812345678` URL that WhatsApp cannot route

---

### Expected Behavior (Correct)

**Bug 1**

2.1 WHEN a user submits valid credentials on `/Account/Login` with no `ReturnUrl` THEN the system SHALL determine the user's role and redirect Admin users to `/admin-dashboard` and Client users to `/client-dashboard`

2.2 WHEN `ReturnUrl` is provided and valid THEN the system SHALL redirect to that URL after successful authentication

2.3 WHEN credentials are invalid THEN the system SHALL display an error message and remain on the login page

---

**Bug 2**

2.4 WHEN `ClientDashboard.razor` renders on first load and `currentUser` is null due to auth state timing THEN the system SHALL retry loading via `OnAfterRenderAsync` and call `StateHasChanged()` to reflect the populated data

2.5 WHEN `AdminDashboard.razor` completes any data-loading operation THEN the system SHALL call `StateHasChanged()` so the UI immediately reflects the new state

---

**Bug 3**

2.6 WHEN the home page is rendered THEN the system SHALL emit exactly one `<PageTitle>` component with the value `SRI SAI PROTOUCH | Physio & Slimming Clinic`

2.7 WHEN the home page is rendered THEN the system SHALL emit exactly one `<section id="services">` block

---

**Bug 4**

2.8 WHEN the home page hero image and care-panel image are requested THEN the system SHALL serve them from URL-safe filenames (`hero-treatment.jpeg` and `care-panel.jpeg`) that load reliably across all web hosts and CDNs

---

**Bug 5**

2.9 WHEN a client submits the daily check-in form and `logModel.Id > 0` (existing record) THEN the system SHALL call `UpdateDailyLogAsync` to update the existing row in place

2.10 WHEN a client submits the daily check-in form and `logModel.Id == 0` (new record) THEN the system SHALL call `AddDailyLogAsync` to insert a new row

---

**Bug 6**

2.11 WHEN an admin deletes a client THEN the system SHALL first delete all associated `DailyLog`, `AttendanceRecord`, `Appointment`, `PaymentNotification`, `ClientProfile`, and `Settings` (photo key) rows via `DeleteClientDataAsync` before deleting the Identity user

---

**Bug 7**

2.12 WHEN `DbInitializer` generates random macro percentages THEN the system SHALL ensure `CarbsPercentage + ProteinPercentage + FatPercentage + VegPercentage = 100` and all individual values are ≥ 0

---

**Bug 8**

2.13 WHEN a newly registered user completes phone OTP verification THEN the system SHALL allow them to sign in without requiring email confirmation, because phone OTP is the designated verification mechanism

---

**Bug 9**

2.14 WHEN any admin action triggers `LoadAdminDataAsync` THEN the system SHALL call `StateHasChanged()` after the data reload so the UI updates immediately

---

**Bug 11**

2.15 WHEN `BuildWhatsAppLink` receives a phone number string that, after digit extraction, results in a number longer than 12 digits starting with `91` THEN the system SHALL strip the leading two digits to prevent a double-prefix URL

---

### Unchanged Behavior (Regression Prevention)

3.1 WHEN a user signs in with a valid `ReturnUrl` query parameter THEN the system SHALL CONTINUE TO redirect to that `ReturnUrl` after successful authentication

3.2 WHEN an invalid email or password is submitted on the login form THEN the system SHALL CONTINUE TO display the "Invalid login attempt" error and remain on `/Account/Login`

3.3 WHEN a locked-out user attempts to sign in THEN the system SHALL CONTINUE TO redirect to `/Account/Lockout`

3.4 WHEN a client visits the client dashboard and is already fully authenticated THEN the system SHALL CONTINUE TO load all profile data in `OnInitializedAsync` without requiring the `OnAfterRenderAsync` fallback

3.5 WHEN a client submits a new daily log (no existing log for today) THEN the system SHALL CONTINUE TO insert a new `DailyLog` row as before

3.6 WHEN an admin views the client list or visit register THEN the system SHALL CONTINUE TO display all existing data correctly

3.7 WHEN `BuildWhatsAppLink` receives a standard 10-digit Indian mobile number THEN the system SHALL CONTINUE TO prepend `91` and produce a valid `https://wa.me/91XXXXXXXXXX` URL

3.8 WHEN `BuildWhatsAppLink` receives a number already prefixed with `+91` (12 digits after stripping `+`) THEN the system SHALL CONTINUE TO produce a valid `https://wa.me/91XXXXXXXXXX` URL without double-prefixing

3.9 WHEN seeded client data is loaded THEN the system SHALL CONTINUE TO show weight logs in the correct descending order

3.10 WHEN an admin logs a new visit THEN the system SHALL CONTINUE TO persist the `AttendanceRecord` and show it in the visit register

3.11 WHEN the home page `#services` anchor link is clicked THEN the system SHALL CONTINUE TO scroll to the services section (now with the duplicate removed, the single section remains)

3.12 WHEN existing authenticated clients access `/client-dashboard` directly (not via login redirect) THEN the system SHALL CONTINUE TO serve the dashboard without the auth-retry path being triggered
