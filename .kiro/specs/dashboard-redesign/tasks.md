# Implementation Plan: Dashboard Redesign

## Overview

Upgrade the ProTouch Physio & Slimming Clinic Blazor Server app's presentation layer with a premium clinic-grade UI. All changes are confined to five files: `app.css` (tokens + styles), `NavMenu.razor` (premium sidebar), `MainLayout.razor` (top bar), `AdminDashboard.razor` (admin overview section), and `ClientDashboard.razor` (client hero section). No backend changes, migrations, or new service methods are introduced.

---

## Tasks

- [ ] 1. Add CSS design tokens and `.pt-sidebar` base styles to `app.css`
  - [ ] 1.1 Add `.dashboard-theme` CSS custom properties block to `app.css`
    - Define all 15 `--pt-*` tokens inside `.dashboard-theme { }`: `--pt-sidebar-width: 240px`, `--pt-sidebar-collapsed: 64px`, `--pt-brand-green: #008d36`, `--pt-brand-purple: #7c1b68`, `--pt-brand-yellow: #ffeb00`, `--pt-topbar-height: 60px`, `--pt-sidebar-bg: #0d1117`, `--pt-sidebar-border`, `--pt-sidebar-text`, `--pt-sidebar-icon`, `--pt-sidebar-active-bg`, `--pt-sidebar-active-border`, `--pt-sidebar-active-text`, `--pt-card-stat-size: 2.4rem`, `--pt-card-stat-weight: 900`
    - All selectors must be nested inside `.dashboard-theme` — no top-level `.pt-*` rules
    - _Requirements: 16.1, 16.2_

  - [ ] 1.2 Add `.pt-sidebar` layout block and `.pt-nav-link` / `.pt-nav-link--active` styles
    - `.pt-sidebar`: `position: fixed`, `width: var(--pt-sidebar-width)`, `min-height: 100vh`, `background: var(--pt-sidebar-bg)`, `border-right`, `flex-direction: column`, `z-index: 1000`, `transition: width 0.25s cubic-bezier(0.4, 0, 0.2, 1)`, `overflow: hidden`
    - `.pt-nav-link`: `display: flex`, `align-items: center`, `gap: 12px`, `padding: 10px 16px`, `border-radius: 10px`, `margin: 2px 8px`, `font-family: 'Outfit'`, `font-size: 0.875rem`, `font-weight: 500`, `white-space: nowrap`, hover state
    - `.pt-nav-link--active`: green left border 3px, green text, light-green bg, `font-weight: 700`, compensate left padding by 1px
    - _Requirements: 1.4, 1.5, 16.1_

  - [ ] 1.3 Add responsive collapse media query (`@media (max-width: 768px)`)
    - Collapse `.pt-sidebar` to `var(--pt-sidebar-collapsed)` (64px)
    - Hide `.pt-nav-label`, `.pt-sidebar-wordmark`, `.pt-logout-label`
    - Center icons in collapsed state: `justify-content: center`, `padding: 12px`, `margin: 2px 4px`
    - Replace active left border with active bottom border: `border-left: none; border-bottom: 3px solid var(--pt-sidebar-active-border)`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

  - [ ] 1.4 Add `.pt-topbar` and `.pt-role-chip` styles
    - `.pt-topbar`: `height: var(--pt-topbar-height)`, `position: sticky`, `top: 0`, `z-index: 900`, `backdrop-filter: blur(12px)`, `background: rgba(13,17,23,0.8)`, `border-bottom`, flex layout with space-between
    - `.pt-topbar-title`: `font-family: 'Playfair Display', serif`, `font-size: 1rem`, `font-weight: 700`, `letter-spacing: 0.03em`
    - `.pt-role-chip` base + `.admin` (purple bg/border) + `.client` (green bg/border)
    - _Requirements: 3.4, 3.5, 3.8, 16.1_

  - [ ] 1.5 Add `.pt-stat-card` upgrade styles
    - `.pt-stat-card`: `border-left: 4px solid var(--accent-colour, var(--pt-brand-green))`, `padding: 24px`, hover transition: `transform: translateY(-3px)` + box-shadow `0 12px 36px rgba(0,0,0,0.35)`
    - `.stat-number`: `font-size: var(--pt-card-stat-size)`, `font-weight: var(--pt-card-stat-weight)`, `font-family: 'Outfit'`
    - `.stat-trend`: base pill styles + `.up` (green bg+text) + `.down` (red bg+text)
    - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5, 15.6, 15.7_

  - [ ] 1.6 Add client hero and sub-component styles
    - `.pt-client-hero`: gradient bg, border, `border-radius: 20px`, `padding: 32px`, `display: flex`, `flex-wrap: wrap`, `gap: 28px`
    - `.pt-progress-ring` / `.pt-ring-svg`: 120×120px, 100% fill
    - `.pt-stat-pill`: glass panel, flex-column, `border-radius: 12px`, `.pill-value` 1.2rem bold, `.pill-label` uppercase 0.7rem
    - `.pt-checkin-prompt` base + `.needs-log` (amber) + `.logged` (green)
    - `.pt-diet-preview` card (re-use `.glass-panel`)
    - _Requirements: 10.4, 11.5, 13.1, 13.2, 14.1, 16.1_

  - [ ] 1.7 Add admin overview component styles
    - `.pt-quick-actions`, `.pt-quick-btn` + hover lift `translateY(-1px)`
    - `.pt-activity-item`, `.pt-activity-dot.visit` (green), `.pt-activity-dot.booking` (purple)
    - `.pt-appt-strip` (horizontal scroll, `overflow-x: auto`, `scrollbar-width: thin`), `.pt-appt-chip`
    - _Requirements: 6.6, 7.4, 7.5, 8.2, 16.1_

- [ ] 2. Upgrade `NavMenu.razor` to premium sidebar
  - [ ] 2.1 Add `NavItem` record, `adminNavItems` and `clientNavItems` arrays
    - Define `record NavItem(string Href, string Icon, string Label)` in `@code` block
    - `adminNavItems`: 8 entries — Overview (`/admin-dashboard`), Clients (`?tab=clients`), Visits (`?tab=visits`), Appointments (`?tab=appointments`), Meal Review (`?tab=mealreview`), Bookings (`?tab=bookings`), WhatsApp (`?tab=whatsapp`), Settings (`?tab=settings`)
    - `clientNavItems`: 5 entries — Overview (`/client-dashboard`), Check-In (`#checkin`), Appointments (`#appointments`), Diet Plan (`#diet`), My History (`#history`)
    - _Requirements: 1.1, 1.2_

  - [ ] 2.2 Replace existing sidebar markup with `.pt-sidebar` structure
    - Wrap entire sidebar in `<nav class="pt-sidebar">` replacing current Bootstrap nav
    - Logo section: brand mark image + `<span class="pt-sidebar-wordmark">ProTouch</span>` inside a `<div class="pt-sidebar-logo">` at the top
    - Nav list: `<ul class="pt-nav-list">` containing `<li class="pt-nav-item">` for each item with `<NavLink class="pt-nav-link" ActiveClass="pt-nav-link--active">` — icon in `<span class="pt-nav-icon">`, label in `<span class="pt-nav-label">`
    - Use `<AuthorizeView Roles="Admin">` and `<AuthorizeView Roles="Client">` to gate item sets
    - Add `title="@item.Label"` on each `<NavLink>` for collapsed tooltip
    - Logout section: `<hr>` spacer + logout form/button with `<span class="pt-logout-label">Logout</span>` at bottom via `mt-auto`
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.6, 2.3, 2.4_

  - [ ]* 2.3 Write unit tests for sidebar nav item rendering
    - Test that admin role renders exactly 8 nav items
    - Test that client role renders exactly 5 nav items
    - Test that logout button is always present
    - _Requirements: 1.1, 1.2, 1.6_

- [ ] 3. Upgrade `MainLayout.razor` top bar
  - [ ] 3.1 Add `Playfair Display` Google Font import to `wwwroot/index.html`
    - Add `<link href="https://fonts.googleapis.com/css2?family=Playfair+Display:wght@700&display=swap" rel="stylesheet">` in `<head>` before existing font links
    - _Requirements: 16.6_

  - [ ] 3.2 Replace top bar markup with `.pt-topbar` structure
    - Wrap header element in `<header class="pt-topbar">`
    - Left slot: mini-logo `<img src="/images/logo-mark.png" width="16" height="16" alt="ProTouch" class="me-2">`
    - Centre slot: `<span class="pt-topbar-title">PROTOUCH Physio · Management Portal</span>`
    - Right slot (inside `<div class="d-flex align-items-center gap-2">`):
      - `<a href="/" class="btn-clinic-secondary btn-sm">Visit Website</a>`
      - `<AuthorizeView Roles="Admin"><span class="pt-role-chip admin">ADMIN</span></AuthorizeView>`
      - `<AuthorizeView Roles="Client"><span class="pt-role-chip client">CLIENT</span></AuthorizeView>`
      - Username label from `context.User.Identity?.Name`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8_

  - [ ]* 3.3 Write unit tests for top bar rendering
    - Test that admin role chip renders with admin class
    - Test that client role chip renders with client class
    - Test that title text is exactly "PROTOUCH Physio · Management Portal"
    - _Requirements: 3.2, 3.3, 3.4, 3.5_

- [ ] 4. Add Admin Overview section to `AdminDashboard.razor`
  - [ ] 4.1 Add `ActivityItem` and `StatCardModel` view-model records to `@code` block
    - `record ActivityItem(string Title, string SubText, DateTime Timestamp, string Kind)`
    - `record StatCardModel(string Label, string Value, string Icon, string AccentColour, string Trend, string TabTarget)`
    - Add private fields: `adminName`, `errorMessage`, `recentActivity`, `todayAppointments`, `clients`, `attendanceLogs`, `allAppointments`, `allBookings`, `whatsappLogs`, `todayAttendanceCount`, `pendingBookingsCount`
    - _Requirements: 17.4_

  - [ ] 4.2 Implement `LoadAdminDataAsync()` with parallel `Task.WhenAll` fan-out
    - Fan-out all 5 `ClinicService` calls simultaneously with `Task.WhenAll`
    - Resolve `adminName` from `AuthStateProvider` / `UserManager`
    - Compute `todayAttendanceCount` using `TimeZoneInfo.ConvertTimeFromUtc(a.CheckInTime, ClinicService.IST).Date == istNow.Date`
    - Compute `pendingBookingsCount = allBookings.Count(b => b.Status == "Pending")`
    - Build `recentActivity = BuildActivityItems()`
    - Build `todayAppointments` filtered + ordered ascending by IST AppointmentDate
    - Wrap in try/catch; on exception set `errorMessage` and return early
    - Call `LoadAdminDataAsync()` from `OnInitializedAsync()`
    - _Requirements: 5.11, 17.1, 18.1_

  - [ ] 4.3 Implement `BuildStatCards()` method
    - Return exactly 5 `StatCardModel` items in order: Active Clients (`#008d36`), Today's Visits (`#7c1b68`), Pending Bookings (`#ffeb00`), Pending Fees (`#ef4444`), WhatsApp Sent (`#3b82f6`)
    - Active Clients: `clients.Count(c => c.IsActive)`
    - Today's Visits: `todayAttendanceCount`
    - Pending Bookings: `pendingBookingsCount`
    - Pending Fees: formatted decimal sum from active payment notifications
    - WhatsApp Sent: `whatsappLogs.Count`
    - All `Value` strings must be non-null, non-empty
    - Use placeholder `"—"` trend badges (no yesterday comparison needed for MVP)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_

  - [ ] 4.4 Implement `BuildActivityItems()` method
    - Take last 8 attendance records (by `CheckInTime` desc) mapped to `kind="visit"` items with title `"[ClientName] logged a visit"` and sub-text = treatment type
    - Take last 8 booking requests (by `SubmittedAt` desc) mapped to `kind="booking"` items with title `"Booking: [FullName] ([ServiceType])"` and sub-text = preferred date
    - Concat both, sort descending by `Timestamp`, take first 8
    - Return empty list (not null) when both sources are empty
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

  - [ ] 4.5 Add overview tab markup — greeting bar and stat cards
    - Add `activeTab == "overview"` branch at the top of the tab-panel section (set as default when `activeTab` is null or empty on `OnInitializedAsync`)
    - Greeting bar: time-of-day salutation + `@adminName` + IST date formatted `"dddd, dd MMMM yyyy"` + `↻ Refresh` button calling `LoadAdminDataAsync()`
    - Error banner: `@if (!string.IsNullOrEmpty(errorMessage))` → `.clinic-alert.clinic-alert-danger` with `Retry` button
    - Stat cards row: `<div class="dashboard-grid mb-4">` + `@foreach (var card in BuildStatCards())` → `<button class="glass-panel pt-stat-card" style="--accent-colour: @card.AccentColour" @onclick='() => NavigateToTab(card.TabTarget)'>`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 5.1, 5.8, 5.9, 5.12, 18.1, 18.2_

  - [ ] 4.6 Add overview tab markup — quick actions, activity feed, appointment strip
    - Quick Actions row: 4 `.pt-quick-btn` buttons with `@onclick='() => NavigateToTab("...")'` targets: clients, visits, bookings, whatsapp
    - Activity feed: `<div class="pt-activity-feed">` + `@foreach (var item in recentActivity)` — green dot for `visit`, purple dot for `booking`, title, sub-text, `TimeAgo(item.Timestamp)` helper
    - Empty-state: `@if (!recentActivity.Any())` → `<p class="text-secondary">No recent activity.</p>`
    - Appointment strip: `<div class="pt-appt-strip">` + `@foreach (var appt in todayAppointments)` → `.pt-appt-chip` showing IST time, treatment, status
    - Empty-state: `@if (!todayAppointments.Any())` → `<p class="text-secondary">No appointments today.</p>`
    - Add `NavigateToTab(string tab)` helper: `NavigationManager.NavigateTo($"/admin-dashboard?tab={tab}")`
    - Add `GetGreeting()` helper returning "Good morning"/"Good afternoon"/"Good evening" by IST hour
    - Add `TimeAgo(DateTime utc)` helper returning relative time string (e.g. "2 hours ago")
    - _Requirements: 4.1, 6.1, 6.2, 6.3, 6.4, 6.5, 7.1, 7.6, 7.7, 8.1, 8.3, 8.4_

  - [ ]* 4.7 Write property test: `BuildStatCards()` always returns exactly 5 non-null items (Property 7)
    - Use FsCheck `Prop.ForAll` with generators for arbitrary client/attendance/booking/whatsapp list combinations
    - Assert `result.Count == 5` and all `Label` and `Value` are non-null and non-empty
    - Tag: `Feature: dashboard-redesign, Property 7: Stat Cards Count Invariant`
    - _Requirements: 5.1, 5.7_

  - [ ]* 4.8 Write property test: `BuildActivityItems()` always returns ≤ 8 items sorted descending (Property 4)
    - Use FsCheck generators for `List<AttendanceRecord>` (0..100 items) and `List<BookingRequest>` (0..100 items)
    - Assert `result.Count <= 8`
    - Assert `result` is ordered descending by `Timestamp` (each item's `Timestamp >= next.Timestamp`)
    - Tag: `Feature: dashboard-redesign, Property 4: Activity Feed Size Invariant`
    - _Requirements: 7.1, 7.2, 7.3_

  - [ ]* 4.9 Write property test: today's IST visit count accuracy (Property 5)
    - Use FsCheck generator producing lists of `AttendanceRecord` with random `CheckInTime` values spanning multiple days
    - Assert computed `todayAttendanceCount` equals `records.Count(r => ConvertToIST(r.CheckInTime).Date == todayIST.Date)`
    - Tag: `Feature: dashboard-redesign, Property 5: Today's Visit Count Accuracy`
    - _Requirements: 5.3_

- [ ] 5. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 6. Add Client Hero section to `ClientDashboard.razor`
  - [ ] 6.1 Add `GetDietPreview()` and `GetNextAppointment()` static helper methods
    - `private static string GetDietPreview(string? dietPlan)`: return empty string if null/whitespace; truncate to 120 chars + "…" if length > 120; otherwise return as-is
    - `private static Appointment? GetNextAppointment(List<Appointment> appointments)`: return first `a.AppointmentDate > DateTime.UtcNow && a.Status != "Cancelled"` ordered by `AppointmentDate` ascending; return null if none
    - _Requirements: 12.1, 14.1, 14.3, 14.4, 14.5, 18.4_

  - [ ] 6.2 Add inline progress ring computation block in `@code` or Razor `@{ }` block
    - Compute `kgLost = userProfile.StartWeight - latestWeight`
    - Compute `totalTarget = userProfile.StartWeight - userProfile.TargetWeight`
    - Compute `goalPct = totalTarget > 0 ? (int)Math.Clamp(kgLost / totalTarget * 100.0, 0.0, 100.0) : 0`
    - Compute `strokeDashoffset = C * (1.0 - goalPct / 100.0)` where `C = 2 * Math.PI * 44.0`
    - Compute `ringLabel = totalTarget <= 0 ? "Goal not yet set" : $"{pct}% of goal"`
    - All computations inside `@if (userProfile != null)` guard
    - _Requirements: 10.2, 10.3, 18.5_

  - [ ] 6.3 Add Client Hero markup — greeting, progress ring, stat pills
    - Insert `<section class="pt-client-hero">` at the top of the `userProfile != null` block, before existing content
    - Greeting: `<h2>@GetGreeting(), @userProfile.FullName 👋</h2>` + motivational sub-line `<p class="text-secondary">Keep going — every session counts.</p>`
    - SVG progress ring: 100×100 viewBox, background circle (grey), progress circle (green stroke with `stroke-dasharray="@C.ToString("F2")" stroke-dashoffset="@dashOffset.ToString("F2")" transform="rotate(-90 50 50)"`), centred `<text>` showing `@($"{kgLost:F1} kg")` and `@ringLabel`; `aria-label="Weight goal @goalPct% achieved"`
    - Stat pills: `<div class="pt-stat-pill">` × 3 — Current Weight (`@latestWeight.ToString("F1") kg`), Target Weight (`@userProfile.TargetWeight.ToString("F1") kg`), Total Visits (`@dailyLogs.Count`)
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 10.1, 10.4, 10.5, 10.6, 10.7, 10.8, 11.1, 11.2, 11.3, 11.4, 11.5_

  - [ ] 6.4 Add Client Hero markup — next appointment card
    - `@{ var nextAppt = GetNextAppointment(appointments); }`
    - `@if (nextAppt != null)` branch: card showing `ConvertTimeFromUtc(nextAppt.AppointmentDate, ClinicService.IST).ToString("ddd dd MMM, h:mm tt")`, treatment type, status badge
    - `@else` branch: empty-state `<p>No upcoming appointments</p>` + `<a href="/book">Book now →</a>`
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 18.4_

  - [ ] 6.5 Add Client Hero markup — check-in prompt and diet preview
    - Check-In Prompt: `<div class="pt-checkin-prompt @(hasLoggedWeightToday ? "logged" : "needs-log")">` — amber message if `!hasLoggedWeightToday`, green confirmation if `hasLoggedWeightToday`
    - Diet Preview: `@{ var preview = GetDietPreview(userProfile.DietPlan); }` → card with `@(string.IsNullOrWhiteSpace(preview) ? "No plan assigned yet — contact your clinic." : preview)` + `<a href="#diet">View full plan →</a>` (hidden when empty)
    - _Requirements: 13.1, 13.2, 13.3, 14.1, 14.2, 14.3, 18.3_

  - [ ]* 6.6 Write property test: progress ring `goalPct` always in `[0, 100]` (Property 1)
    - Use FsCheck `Prop.ForAll` with `Gen.Choose(0, 300)` for `startWeight`, `latestWeight`, `targetWeight` (all ≥ 0)
    - Assert `goalPct >= 0 && goalPct <= 100`
    - Assert that when `startWeight <= targetWeight`, `goalPct == 0`
    - Assert `strokeDashoffset >= 0.0 && strokeDashoffset <= C`
    - Tag: `Feature: dashboard-redesign, Property 1: Progress Ring Percentage Clamping`
    - _Requirements: 10.2, 10.3, 18.5_

  - [ ]* 6.7 Write property test: `GetDietPreview()` always returns ≤ 123 characters (Property 2)
    - Use FsCheck `Prop.ForAll` with arbitrary nullable strings (null, empty, whitespace, short, long)
    - Assert `result.Length <= 123`
    - Assert that when input is non-null and `input.Length > 120`, result ends with `"…"`
    - Assert `result` (non-empty non-null input) is always a prefix of the input string
    - Assert function does not throw for any input
    - Tag: `Feature: dashboard-redesign, Property 2: Diet Preview Truncation`
    - _Requirements: 14.1, 14.4, 14.5_

  - [ ]* 6.8 Write property test: `GetNextAppointment()` never returns past or cancelled appointments (Property 3)
    - Use FsCheck generators for `List<Appointment>` with random `AppointmentDate` values (past and future) and random `Status` values including `"Cancelled"`
    - Assert result is either null or `result.AppointmentDate > DateTime.UtcNow`
    - Assert result is either null or `result.Status != "Cancelled"`
    - Assert result is the minimum `AppointmentDate` among all qualifying appointments (when multiple qualify)
    - Tag: `Feature: dashboard-redesign, Property 3: Next Appointment Selection`
    - _Requirements: 12.1, 12.4, 18.4_

- [ ] 7. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

---

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP delivery
- The design uses C#/Razor throughout — all code examples are implementation-ready
- The `Playfair Display` font (Task 3.1) must be added to `index.html` before the top bar renders correctly
- All `--pt-*` tokens and `.pt-*` classes must live inside `.dashboard-theme { }` to avoid bleed into public pages
- `ClinicService.IST` is a pre-resolved `TimeZoneInfo`; always use `TimeZoneInfo.ConvertTimeFromUtc(utcDate, ClinicService.IST)` for IST display
- Property tests live in `ClinicApp.Tests/PropertyTests.cs` (xUnit + FsCheck, already present)
- Each property test file must tag tests with `Feature: dashboard-redesign, Property N: <Title>` for traceability
- The `activeTab` default must be set to `"overview"` in `OnInitializedAsync` of `AdminDashboard.razor` when no query string is present

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "1.3", "1.4", "1.5", "1.6", "1.7"] },
    { "id": 2, "tasks": ["2.1", "3.1", "4.1", "6.1"] },
    { "id": 3, "tasks": ["2.2", "3.2", "4.2", "4.3", "4.4", "6.2"] },
    { "id": 4, "tasks": ["2.3", "3.3", "4.5", "4.7", "4.8", "4.9", "6.3", "6.6", "6.7", "6.8"] },
    { "id": 5, "tasks": ["4.6", "6.4", "6.5"] }
  ]
}
```
