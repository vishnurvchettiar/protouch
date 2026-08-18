# Requirements Document

## Introduction

This document specifies the functional requirements for the **ProTouch Physio & Slimming Clinic** dashboard redesign. The redesign upgrades the presentation layer of the Blazor Server application, replacing the bare Bootstrap sidebar and minimal stat cards with a premium clinic-grade UI. Changes are confined entirely to the presentation layer: `NavMenu.razor`, `MainLayout.razor`, a new `AdminOverview.razor` component, an updated `ClientDashboard.razor`, and `app.css`. No new backend endpoints, service methods, or database migrations are required.

---

## Glossary

- **Dashboard_Theme**: The `.dashboard-theme` CSS scope wrapping all new dashboard styles in `app.css`. Ensures isolation from `.wellness-page` and `.ac-page` public-site scopes.
- **Sidebar**: The `NavMenu.razor` component rendered as `.pt-sidebar`, providing role-based navigation.
- **Top_Bar**: The enhanced header rendered in `MainLayout.razor` as `.pt-topbar`.
- **Admin_Overview**: The `AdminOverview.razor` component providing the default landing view for the Admin role at `/admin-dashboard`.
- **Client_Hero**: The `.pt-client-hero` hero section inserted at the top of `ClientDashboard.razor`.
- **Stat_Card**: A `.pt-stat-card` panel displaying a single aggregated metric with a trend badge and coloured left border.
- **Progress_Ring**: The SVG circular indicator in the Client Hero showing the client's weight-loss goal completion percentage.
- **Activity_Feed**: The `.pt-activity-feed` component in Admin Overview displaying the most recent visits and booking requests.
- **Appointment_Strip**: The `.pt-appt-strip` horizontal scroll area in Admin Overview showing today's appointments.
- **Quick_Actions**: The `.pt-quick-actions` row of shortcut buttons in Admin Overview.
- **Stat_Pill**: A `.pt-stat-pill` compact metric chip in the Client Hero (Current Weight, Target Weight, Total Visits).
- **Check_In_Prompt**: The `.pt-checkin-prompt` card in the Client Hero showing daily weight-log status.
- **Diet_Preview**: The `.pt-diet-preview` card in the Client Hero showing a truncated diet plan with a "View full plan" link.
- **ClinicService**: The existing service class (`ClinicApp.Services.ClinicService`) providing all data retrieval methods. No new methods are added.
- **IST**: India Standard Time (UTC+5:30), resolved via `ClinicService.IST` (`TimeZoneInfo`).
- **ActivityItem**: The component-scoped view-model record (`Title`, `SubText`, `Timestamp`, `Kind`) used in the Activity Feed.
- **StatCardModel**: The component-scoped record (`Label`, `Value`, `Icon`, `AccentColour`, `Trend`) passed to each Stat Card.
- **NavItem**: The component-scoped record (`Href`, `Icon`, `Label`) describing a single sidebar navigation entry.
- **Role_Chip**: The `.pt-role-chip` badge in the Top Bar indicating whether the authenticated user is `ADMIN` or `CLIENT`.

---

## Requirements

---

### Requirement 1: Premium Sidebar — Role-Based Navigation

**User Story:** As an authenticated user, I want a premium branded sidebar that shows only the navigation items relevant to my role, so that I can navigate quickly without encountering links that do not apply to me.

#### Acceptance Criteria

1. WHEN a user with the `Admin` role is authenticated, THE Sidebar SHALL render exactly 8 navigation items: Overview, Clients, Visits, Appointments, Meal Review, Bookings, WhatsApp, and Settings.
2. WHEN a user with the `Client` role is authenticated, THE Sidebar SHALL render exactly 5 navigation items: Overview, Check-In, Appointments, Diet Plan, and My History.
3. THE Sidebar SHALL render the ProTouch logo (brand mark image plus wordmark text) at the top of the navigation panel.
4. WHEN the current URL matches a navigation item's href prefix, THE Sidebar SHALL apply the `pt-nav-link--active` class to that item's link element.
5. WHEN the `pt-nav-link--active` class is applied, THE Sidebar SHALL render a green left border (`#008d36`), green text, and a light-green background (`rgba(0,141,54,0.12)`) on the active link.
6. THE Sidebar SHALL render a Logout button pinned at the bottom of the navigation panel, separated from the navigation items by a horizontal rule.
7. AT MOST one navigation item SHALL have the `pt-nav-link--active` class applied at any given time.

---

### Requirement 2: Premium Sidebar — Mobile Collapse

**User Story:** As a user on a small screen, I want the sidebar to collapse to icon-only mode, so that it does not obscure page content on devices with a viewport width of 768 px or less.

#### Acceptance Criteria

1. WHILE the viewport width is greater than 768 px, THE Sidebar SHALL display at a width of 240 px (`--pt-sidebar-width`) showing both icons and text labels for all navigation items.
2. WHEN the viewport width is 768 px or less, THE Sidebar SHALL collapse to a width of 64 px (`--pt-sidebar-collapsed`) and hide all navigation item text labels and the sidebar wordmark.
3. WHEN the Sidebar is in collapsed state, THE Sidebar SHALL display only the icon for each navigation item, centred within the 64 px width.
4. WHEN the Sidebar is in collapsed state and a navigation item icon is hovered, THE Sidebar SHALL display the navigation item label as a native browser tooltip via the `title` attribute.
5. THE Sidebar SHALL animate the width transition using a CSS `transition` of `0.25 s cubic-bezier(0.4, 0, 0.2, 1)` with no JavaScript required.
6. WHEN the Sidebar is in collapsed state, THE Sidebar SHALL replace the active left border indicator with a bottom border indicator on the active navigation item.

---

### Requirement 3: Enhanced Top Bar — Clinic Branding and Identity

**User Story:** As an authenticated user, I want the top bar to display the clinic's brand identity and my role, so that I always know which application I am using and who I am authenticated as.

#### Acceptance Criteria

1. THE Top_Bar SHALL display the ProTouch mini-logo (16×16 px brand mark) in the left slot.
2. THE Top_Bar SHALL display the text `"PROTOUCH Physio · Management Portal"` in Playfair Display serif font, centred in the top bar.
3. THE Top_Bar SHALL display a Role_Chip in the right slot indicating the authenticated user's role as either `ADMIN` or `CLIENT` in uppercase.
4. WHEN the authenticated user has the `Admin` role, THE Top_Bar SHALL render the Role_Chip with a purple background (`rgba(124,27,104,0.2)`) and purple border (`rgba(124,27,104,0.4)`).
5. WHEN the authenticated user has the `Client` role, THE Top_Bar SHALL render the Role_Chip with a green background (`rgba(0,141,54,0.2)`) and green border (`rgba(0,141,54,0.4)`).
6. THE Top_Bar SHALL display the authenticated user's username label adjacent to the Role_Chip in the right slot.
7. THE Top_Bar SHALL display a `Visit Website` button in the right slot that navigates the user to the public-facing clinic website.
8. THE Top_Bar SHALL remain sticky at the top of the viewport at a height of 60 px (`--pt-topbar-height`) as the user scrolls the page content.

---

### Requirement 4: Admin Overview — Greeting Bar

**User Story:** As an admin user, I want to see a personalised greeting with the current date when I land on the admin dashboard, so that I am immediately oriented and the interface feels welcoming.

#### Acceptance Criteria

1. THE Admin_Overview SHALL display a greeting heading that includes a time-of-day salutation (e.g. "Good morning", "Good afternoon", "Good evening") and the authenticated admin's display name.
2. THE Admin_Overview SHALL display the current date formatted as `dddd, dd MMMM yyyy` converted to IST below the greeting heading.
3. THE Admin_Overview SHALL display a `↻ Refresh` button in the greeting bar that, when clicked, re-executes `LoadAdminDataAsync()` and updates all displayed values.
4. THE Admin_Overview SHALL be the default view rendered when an Admin user navigates to `/admin-dashboard` (the `activeTab == "overview"` state).
5. THE Admin_Overview SHALL be protected by `[Authorize(Roles = "Admin")]`, preventing access by unauthenticated or Client-role users.

---

### Requirement 5: Admin Overview — Five Stat Cards

**User Story:** As an admin user, I want to see five key clinic metrics displayed as visual stat cards, so that I can assess the clinic's daily status at a glance without navigating to individual tabs.

#### Acceptance Criteria

1. THE Admin_Overview SHALL render exactly 5 Stat_Cards in the order: Active Clients, Today's Visits, Pending Bookings, Pending Fees, and WhatsApp Sent.
2. THE Admin_Overview SHALL derive the **Active Clients** value by counting `ClientProfile` records where `IsActive == true` using `ClinicService.GetAllClientsAsync()`.
3. THE Admin_Overview SHALL derive the **Today's Visits** value by counting `AttendanceRecord` entries whose `CheckInTime`, converted to IST, falls on today's IST date using `ClinicService.GetAllAttendanceAsync()`.
4. THE Admin_Overview SHALL derive the **Pending Bookings** value by counting `BookingRequest` records where `Status == "Pending"` using `ClinicService.GetAllBookingRequestsAsync()`.
5. THE Admin_Overview SHALL derive the **Pending Fees** value by summing `PaymentNotification.Amount` for all active payment notifications using `ClinicService.GetAllClientsAsync()`.
6. THE Admin_Overview SHALL derive the **WhatsApp Sent** value from the count of records returned by `ClinicService.GetWhatsAppLogsAsync()`.
7. WHEN all five data sources are loaded, THE Admin_Overview SHALL display all stat card values as non-null, non-empty strings.
8. EACH Stat_Card SHALL display a trend badge (`↑`, `↓`, or `—`) alongside the metric value.
9. EACH Stat_Card SHALL display a coloured left border whose colour is provided via the `--accent-colour` CSS custom property on the card element.
10. WHEN a Stat_Card is hovered, THE Stat_Card SHALL lift vertically by 3 px and increase its box shadow, using a CSS `transition` of `0.2 s ease`.
11. THE Admin_Overview SHALL load all five data sources in parallel using `Task.WhenAll` to avoid sequential database round-trips.
12. EACH Stat_Card SHALL navigate to the corresponding AdminDashboard tab when clicked.

---

### Requirement 6: Admin Overview — Quick Action Buttons

**User Story:** As an admin user, I want quick-access buttons for the most common tasks, so that I can reach key workflows in one click from the overview page.

#### Acceptance Criteria

1. THE Admin_Overview SHALL render a Quick_Actions row containing exactly 4 action buttons: `＋ Add Client`, `🏥 Log a Visit`, `📅 View Bookings`, and `💬 Send WhatsApp`.
2. WHEN the `＋ Add Client` button is clicked, THE Admin_Overview SHALL navigate to `/admin-dashboard` with the `clients` tab active.
3. WHEN the `🏥 Log a Visit` button is clicked, THE Admin_Overview SHALL navigate to `/admin-dashboard` with the `visits` tab active.
4. WHEN the `📅 View Bookings` button is clicked, THE Admin_Overview SHALL navigate to `/admin-dashboard` with the `bookings` tab active.
5. WHEN the `💬 Send WhatsApp` button is clicked, THE Admin_Overview SHALL navigate to `/admin-dashboard` with the `whatsapp` tab active.
6. WHEN a Quick_Actions button is hovered, THE Quick_Actions SHALL lift the button by 1 px using a CSS transition and deepen the green background tint.

---

### Requirement 7: Admin Overview — Recent Activity Feed

**User Story:** As an admin user, I want a unified recent activity feed showing the latest visits and booking requests, so that I can monitor clinic activity without switching between tabs.

#### Acceptance Criteria

1. THE Admin_Overview SHALL render a Recent Activity feed displaying at most 8 `ActivityItem` entries.
2. THE Activity_Feed SHALL be populated by combining the most recent visit records (from `ClinicService.GetAllAttendanceAsync()`) and the most recent booking requests (from `ClinicService.GetAllBookingRequestsAsync()`).
3. THE Activity_Feed SHALL sort all entries in descending order by `Timestamp` so the most recent activity appears first.
4. EACH activity entry of kind `"visit"` SHALL display a green dot indicator and a title of the form `"[ClientName] logged a visit"` with the treatment type as sub-text.
5. EACH activity entry of kind `"booking"` SHALL display a purple dot indicator and a title of the form `"Booking: [FullName] ([ServiceType])"` with the preferred date as sub-text.
6. THE Activity_Feed SHALL render timestamps as relative time-ago strings (e.g. "2 hours ago") derived from each item's `Timestamp` value.
7. IF no activity records exist, THE Activity_Feed SHALL render an empty-state message indicating that no recent activity is available.

---

### Requirement 8: Admin Overview — Today's Appointments Strip

**User Story:** As an admin user, I want to see all of today's appointments in a scrollable horizontal strip, so that I can quickly review the day's schedule without leaving the overview page.

#### Acceptance Criteria

1. THE Admin_Overview SHALL render an Appointment_Strip displaying all `Appointment` records whose `AppointmentDate`, converted to IST, falls on today's IST date, ordered ascending by `AppointmentDate`.
2. THE Appointment_Strip SHALL be horizontally scrollable when the number of appointment chips exceeds the available width.
3. EACH appointment chip in the strip SHALL display the appointment time (IST), the treatment type, and the appointment status.
4. IF no appointments exist for today, THE Appointment_Strip SHALL render an empty-state message indicating no appointments are scheduled for today.
5. THE Appointment_Strip SHALL derive its data from `ClinicService.GetAllAppointmentsAsync()` with no additional service calls.

---

### Requirement 9: Client Hero — Personalised Greeting and Layout

**User Story:** As a client user, I want to see a personalised greeting and motivational message at the top of my dashboard, so that the experience feels welcoming and relevant to my wellness journey.

#### Acceptance Criteria

1. THE Client_Hero SHALL be inserted at the top of `ClientDashboard.razor`, before all existing content.
2. THE Client_Hero SHALL display a time-of-day salutation (e.g. "Good morning", "Good afternoon", "Good evening") combined with the client's display name.
3. THE Client_Hero SHALL display a motivational sub-line below the greeting.
4. THE Client_Hero SHALL be rendered only when `userProfile != null`, preserving the existing authentication guard in `ClientDashboard.razor`.
5. THE Client_Hero SHALL use existing component-level state (`userProfile`, `dailyLogs`, `latestWeight`, `hasLoggedWeightToday`, `appointments`) with no new service calls.

---

### Requirement 10: Client Hero — SVG Progress Ring

**User Story:** As a client user, I want to see a visual progress ring showing how close I am to my weight-loss goal, so that I can stay motivated by seeing my progress at a glance.

#### Acceptance Criteria

1. THE Client_Hero SHALL render a Progress_Ring as an SVG element with a `viewBox` of `0 0 100 100` and a circle radius of 44.
2. THE Progress_Ring SHALL calculate the goal percentage as `(startWeight − latestWeight) / (startWeight − targetWeight) × 100`, clamped to the closed interval `[0, 100]`.
3. WHEN `startWeight ≤ targetWeight` or `startWeight == targetWeight`, THE Progress_Ring SHALL display a goal percentage of `0` and render the label `"Goal not yet set"` without performing a division operation.
4. THE Progress_Ring SHALL render the filled arc in brand green (`var(--pt-brand-green)`) and the unfilled arc in a low-opacity white (`rgba(255,255,255,0.08)`).
5. THE Progress_Ring SHALL display the kilograms lost (e.g. `"2.3 kg"`) as the primary label centred inside the ring, and the percentage complete (e.g. `"46% of goal"`) as a secondary label below it.
6. THE Progress_Ring SHALL use `stroke-dasharray` and `stroke-dashoffset` to render the arc such that `stroke-dashoffset` is always within `[0, 2πr]`.
7. THE Progress_Ring SHALL not require any JavaScript and SHALL be rendered as inline SVG within the Razor component.
8. THE Progress_Ring SHALL include an `aria-label` attribute of the form `"Weight goal X% achieved"` for accessibility.

---

### Requirement 11: Client Hero — Quick-Stat Pills

**User Story:** As a client user, I want to see my current weight, target weight, and total visit count as compact stat pills next to the progress ring, so that I have key metrics immediately visible.

#### Acceptance Criteria

1. THE Client_Hero SHALL render exactly 3 Stat_Pills: Current Weight, Target Weight, and Total Visits.
2. THE Stat_Pill for **Current Weight** SHALL display `latestWeight` formatted to one decimal place followed by `"kg"`.
3. THE Stat_Pill for **Target Weight** SHALL display `userProfile.TargetWeight` formatted to one decimal place followed by `"kg"`.
4. THE Stat_Pill for **Total Visits** SHALL display the count of `AttendanceRecord` entries belonging to the authenticated client.
5. EACH Stat_Pill SHALL display a value in a large bold font and a label in a small uppercase caption font, separated by a gap of 4 px.

---

### Requirement 12: Client Hero — Next Appointment Card

**User Story:** As a client user, I want to see my next upcoming appointment prominently displayed, so that I never miss a scheduled session.

#### Acceptance Criteria

1. THE Client_Hero SHALL render a next appointment card derived by selecting the appointment with the earliest `AppointmentDate` that is strictly after `DateTime.UtcNow` and whose `Status` is not `"Cancelled"`.
2. WHEN a next appointment exists, THE Client_Hero SHALL display the appointment date and time (converted to IST), the treatment type, and the appointment status.
3. WHEN no future non-cancelled appointment exists, THE Client_Hero SHALL render an empty-state message of `"No upcoming appointments"` with a `"Book now →"` anchor linking to the clinic booking form.
4. THE Client_Hero SHALL never display an appointment with a past `AppointmentDate` or a `Status` of `"Cancelled"` in the next appointment card.

---

### Requirement 13: Client Hero — Daily Check-In Prompt

**User Story:** As a client user, I want to see a clear prompt reminding me to log my daily weight when I have not done so yet, so that I maintain consistent progress tracking.

#### Acceptance Criteria

1. WHEN `hasLoggedWeightToday` is `false`, THE Client_Hero SHALL render the Check_In_Prompt with an amber background (`rgba(255,235,0,0.08)`) and amber border (`rgba(255,235,0,0.25)`), containing a message prompting the user to log their weight today.
2. WHEN `hasLoggedWeightToday` is `true`, THE Client_Hero SHALL render the Check_In_Prompt with a green background (`rgba(0,141,54,0.08)`) and green border (`rgba(0,141,54,0.25)`), confirming that today's log has been recorded.
3. THE Check_In_Prompt SHALL derive its state solely from the `hasLoggedWeightToday` boolean already present in `ClientDashboard.razor` with no new service calls.

---

### Requirement 14: Client Hero — Diet Plan Preview

**User Story:** As a client user, I want to see a short preview of my assigned diet plan on my dashboard, so that I can review my nutrition guidance without navigating away.

#### Acceptance Criteria

1. THE Client_Hero SHALL render a Diet_Preview card showing the first 120 characters of `userProfile.DietPlan`, followed by `"…"` if the plan exceeds 120 characters.
2. THE Diet_Preview card SHALL include a `"View full plan →"` anchor that navigates the user to the diet plan section of their dashboard.
3. WHEN `userProfile.DietPlan` is null or empty, THE Diet_Preview card SHALL display the message `"No plan assigned yet — contact your clinic."` instead of a truncated plan.
4. THE Diet_Preview truncation function SHALL return a string of at most 123 characters (120 content characters plus the 3-character ellipsis `"…"`).
5. THE Diet_Preview truncation function SHALL not mutate the `userProfile.DietPlan` input.

---

### Requirement 15: Visual Stat Card Upgrade

**User Story:** As a user, I want all stat cards across the dashboard to have a larger, bolder number, a trend badge, a coloured left border, and a hover lift effect, so that metrics are easier to read and the UI feels polished.

#### Acceptance Criteria

1. THE Stat_Card SHALL display the metric value at a font size of `2.4 rem` (`--pt-card-stat-size`) with a font weight of `900` (`--pt-card-stat-weight`) using the Outfit typeface.
2. THE Stat_Card SHALL display a trend badge (`↑`, `↓`, or `—`) styled as a small pill adjacent to the metric value.
3. WHEN the trend badge displays `↑`, THE Stat_Card SHALL render it with a green background (`rgba(0,141,54,0.15)`) and green text (`#008d36`).
4. WHEN the trend badge displays `↓`, THE Stat_Card SHALL render it with a red background (`rgba(239,68,68,0.15)`) and red text (`#ef4444`).
5. THE Stat_Card SHALL render a 4 px left border whose colour is controlled by the `--accent-colour` CSS custom property, defaulting to `var(--pt-brand-green)` when not overridden.
6. WHEN the Stat_Card is hovered, THE Stat_Card SHALL translate vertically by −3 px and apply `box-shadow: 0 12px 36px rgba(0,0,0,0.35)`, using a `transition` of `0.2 s ease`.
7. THE Stat_Card `padding` SHALL be `24px` on all sides.

---

### Requirement 16: CSS Scope Isolation

**User Story:** As a developer, I want all new dashboard styles to be scoped under `.dashboard-theme` so that there is zero risk of visual regressions on the public-facing wellness and clinic pages.

#### Acceptance Criteria

1. ALL new CSS custom properties (`--pt-*`) and class selectors (`.pt-*`) SHALL be defined within the `.dashboard-theme` scope in `app.css`.
2. THE Dashboard_Theme SHALL define the following CSS custom properties: `--pt-sidebar-width`, `--pt-sidebar-collapsed`, `--pt-brand-green`, `--pt-brand-purple`, `--pt-brand-yellow`, `--pt-topbar-height`, `--pt-sidebar-bg`, `--pt-sidebar-border`, `--pt-sidebar-text`, `--pt-sidebar-icon`, `--pt-sidebar-active-bg`, `--pt-sidebar-active-border`, `--pt-sidebar-active-text`, `--pt-card-stat-size`, `--pt-card-stat-weight`.
3. WHEN a page is rendered under the `.wellness-page` CSS scope, THE Dashboard_Theme SHALL not apply any `.pt-*` class styles or `--pt-*` custom property values to that page.
4. WHEN a page is rendered under the `.ac-page` CSS scope, THE Dashboard_Theme SHALL not apply any `.pt-*` class styles or `--pt-*` custom property values to that page.
5. NO new CSS selector from the dashboard redesign SHALL override or conflict with any existing selector scoped to `.wellness-page` or `.ac-page`.
6. THE `Playfair Display` Google Font SHALL be added to `index.html` as a prerequisite for the `.pt-topbar-title` style; no other new external font dependencies are required.

---

### Requirement 17: No Backend Changes

**User Story:** As a developer, I want all new UI data to be sourced exclusively from existing `ClinicService` methods, so that no database migrations, new API endpoints, or new service methods are required to ship the redesign.

#### Acceptance Criteria

1. THE Admin_Overview SHALL retrieve all data exclusively from the following existing methods: `ClinicService.GetAllClientsAsync()`, `ClinicService.GetAllAttendanceAsync()`, `ClinicService.GetAllAppointmentsAsync()`, `ClinicService.GetAllBookingRequestsAsync()`, and `ClinicService.GetWhatsAppLogsAsync()`.
2. THE Client_Hero SHALL derive all displayed values from existing component-level state (`userProfile`, `dailyLogs`, `latestWeight`, `hasLoggedWeightToday`, `appointments`) already loaded by `ClientDashboard.razor`.
3. THE redesign SHALL NOT introduce any new `ClinicService` methods, database migrations, EF Core model changes, or HTTP API endpoints.
4. THE `ActivityItem` and `StatCardModel` view-model records SHALL be defined as component-scoped types and SHALL NOT be added to the service or data layer.
5. THE IST timezone conversion SHALL use `ClinicService.IST` (a pre-resolved `TimeZoneInfo` instance) via `TimeZoneInfo.ConvertTimeFromUtc` — no additional timezone libraries are required.

---

### Requirement 18: Error Handling

**User Story:** As a user, I want the dashboard to handle data-loading failures gracefully so that a service outage does not leave me with a broken or empty page.

#### Acceptance Criteria

1. IF `LoadAdminDataAsync()` throws an exception, THEN THE Admin_Overview SHALL set an `errorMessage` string and render a `.clinic-alert.clinic-alert-danger` error banner visible to the admin user.
2. WHEN an error banner is displayed, THE Admin_Overview SHALL render a `"Retry"` button that, when clicked, calls `LoadAdminDataAsync()` again and clears the error banner on success.
3. IF `userProfile.DietPlan` is null or an empty string, THEN THE Client_Hero SHALL render the empty-state diet preview message `"No plan assigned yet — contact your clinic."` without throwing an exception.
4. IF no future non-cancelled appointment exists for the client, THEN THE Client_Hero SHALL render the empty-state next appointment message with a `"Book now →"` link without throwing an exception.
5. IF `userProfile.StartWeight ≤ userProfile.TargetWeight`, THEN THE Client_Hero SHALL set `goalPct` to `0` and display `"Goal not yet set"` in the Progress_Ring centre label without performing a division operation.
