# Implementation Plan

---

## Phase 1 — Explore: Bug Condition Tests (Run BEFORE any fix)

- [ ] 1. Write bug condition exploration tests
  - **Property 1: Bug Condition** — Login Redirect Loop, Duplicate Log, Negative Macros, Double WhatsApp Prefix, Blocked Registrant
  - **CRITICAL**: Write ALL exploration tests BEFORE implementing any fix
  - **GOAL**: Surface counterexamples that confirm each bug exists
  - **DO NOT attempt to fix the code when these tests fail — failure is the expected and correct outcome**

  - [ ] 1.1 Explore Bug 1 — Login redirect loop
    - **Property 1: Bug Condition** — Login Redirect (Bug 1)
    - Create a unit test for `LoginUser()` in `Login.razor` where `Input.ReturnUrl = null` and credentials are valid
    - Assert that the resulting navigation does NOT stay on `/Account/Login`
    - Run on UNFIXED code
    - **EXPECTED OUTCOME**: Test FAILS — navigation loops back to login page or stays at `/Account/Login`
    - Document counterexample: which URL `RedirectManager` receives
    - _Requirements: 1.1, 1.2_

  - [ ] 1.2 Explore Bug 5 — Duplicate daily log INSERT
    - **Property 1: Bug Condition** — Daily Log Duplicate (Bug 5)
    - Create a unit/integration test: seed a `DailyLog` with `Id=42` for today's date and a known `UserId`
    - Load `logModel` from that existing row (`logModel.Id = 42`)
    - Call `HandleLogSubmission` (or `ClinicService.AddDailyLogAsync(logModel)` directly)
    - Assert `DailyLog` count for `(UserId, LogDate=Today)` equals 1
    - Run on UNFIXED code
    - **EXPECTED OUTCOME**: Test FAILS — count becomes 2 (duplicate INSERT)
    - Document counterexample: second row with same UserId/LogDate
    - _Requirements: 1.7, 1.8_

  - [ ] 1.3 Explore Bug 7 — Negative macro percentages
    - **Property 1: Bug Condition** — Negative Macros (Bug 7)
    - Create a test that runs `DbInitializer` seed logic with fixed random values: `carb=44, prot=44, fat=24`
    - Assert `VegPercentage >= 0` and `carb + prot + fat + veg = 100`
    - Run on UNFIXED code
    - **EXPECTED OUTCOME**: Test FAILS — `VegPercentage = -12`, sum calculation shows negative value
    - Document counterexample: `{carb=44, prot=44, fat=24, veg=-12}`
    - _Requirements: 1.9, 1.10_

  - [ ] 1.4 Explore Bug 11 — Double country-code prefix
    - **Property 1: Bug Condition** — Double WhatsApp Prefix (Bug 11)
    - Call `ClinicService.BuildWhatsAppLink("91919812345678", "Hello")`
    - Assert result equals `https://wa.me/919812345678?text=Hello`
    - Run on UNFIXED code
    - **EXPECTED OUTCOME**: Test FAILS — result is `https://wa.me/91919812345678?text=Hello`
    - Document counterexample: double-`91` prefix in the URL
    - _Requirements: 1.13_

  - [ ] 1.5 Explore Bug 8 — New registrant blocked by email confirmation
    - **Property 1: Bug Condition** — Email Confirmation Gate (Bug 8)
    - Create a test user with `EmailConfirmed = false` and `PhoneNumberConfirmed = true`
    - Call `SignInManager.PasswordSignInAsync` with that user's credentials
    - Assert `result.Succeeded = true`
    - Run on UNFIXED code (with `RequireConfirmedAccount = true`)
    - **EXPECTED OUTCOME**: Test FAILS — `result.Succeeded = false`
    - Document counterexample: login silently blocked for phone-verified users
    - _Requirements: 1.11, 1.12_

---

## Phase 2 — Preserve: Baseline Tests (Run BEFORE any fix, must PASS)

- [ ] 2. Write preservation property tests (BEFORE implementing any fix)
  - **Property 2: Preservation** — All non-buggy login paths, log INSERT path, and WhatsApp link formats
  - **IMPORTANT**: Follow observation-first methodology — run against UNFIXED code to capture baseline
  - **EXPECTED OUTCOME**: All preservation tests PASS on unfixed code (confirming baseline to preserve)

  - [ ] 2.1 Preservation — Login with valid ReturnUrl
    - **Property 2: Preservation** — ReturnUrl redirect (Bug 1)
    - Observe that `LoginUser()` with a non-null `ReturnUrl = "/admin-dashboard"` redirects correctly on unfixed code
    - Write test: valid credentials + `ReturnUrl = "/admin-dashboard"` → assert redirect target is `/admin-dashboard`
    - Verify passes on UNFIXED code
    - _Requirements: 3.1_

  - [ ] 2.2 Preservation — Invalid credentials show error
    - **Property 2: Preservation** — Invalid login error message (Bug 1)
    - Observe that invalid credentials produce `errorMessage = "Error: Invalid login attempt."` on unfixed code
    - Write test: invalid password → assert `errorMessage` is set and no redirect occurs
    - Verify passes on UNFIXED code
    - _Requirements: 3.2_

  - [ ] 2.3 Preservation — New daily log INSERT (logModel.Id == 0)
    - **Property 2: Preservation** — New log path (Bug 5)
    - Observe that submitting with `logModel.Id = 0` inserts one new row on unfixed code
    - Write property-based test: for all submissions where `logModel.Id = 0`, count of DailyLog rows increases by exactly 1
    - Verify passes on UNFIXED code
    - _Requirements: 3.5_

  - [ ] 2.4 Preservation — WhatsApp link for 10-digit numbers
    - **Property 2: Preservation** — Standard 10-digit phone (Bug 11)
    - Observe that `BuildWhatsAppLink("9812345678", "Hi")` returns `https://wa.me/919812345678?text=Hi` on unfixed code
    - Write property-based test: for any 10-digit digit string, result equals `https://wa.me/91{digits}?text=...`
    - Verify passes on UNFIXED code
    - _Requirements: 3.7_

  - [ ] 2.5 Preservation — WhatsApp link for +91-prefixed numbers
    - **Property 2: Preservation** — +91 prefixed phone (Bug 11)
    - Observe that `BuildWhatsAppLink("+919812345678", "Hi")` returns `https://wa.me/919812345678?text=Hi` on unfixed code
    - Write test: `+91XXXXXXXXXX` input → assert URL has exactly 12 digits after `wa.me/`
    - Verify passes on UNFIXED code
    - _Requirements: 3.8_

  - [ ] 2.6 Preservation — Macro sum equals 100 for valid inputs
    - **Property 2: Preservation** — Valid macro sums (Bug 7)
    - Observe that seed inputs where `carb + prot + fat <= 100` produce `veg >= 0` on unfixed code
    - Write property-based test: generate 1000 random combos where sum ≤ 100, assert `veg ≥ 0`
    - Verify passes on UNFIXED code
    - _Requirements: 3.9_

---

## Phase 3 — Implement: Apply All Fixes

- [ ] 3. Fix Bug 1 — Login redirect loop (`Login.razor`)

  - [ ] 3.1 Implement role-based redirect after successful login
    - Inject `UserManager<ApplicationUser>` into `Login.razor`
    - After `result.Succeeded`, look up user by `Input.Email` using `UserManager.FindByEmailAsync`
    - Get user's roles via `UserManager.GetRolesAsync(user)`
    - If `ReturnUrl` is non-null and non-empty → `RedirectManager.RedirectTo(ReturnUrl)`
    - Else if user is in role `"Admin"` → `RedirectManager.RedirectTo("/admin-dashboard")`
    - Else → `RedirectManager.RedirectTo("/client-dashboard")`
    - _Bug_Condition: isBugCondition_Bug1(request) where request.ReturnUrl = null_
    - _Expected_Behavior: Admin → /admin-dashboard; Client → /client-dashboard_
    - _Preservation: ReturnUrl-based redirect and error paths unchanged_
    - _Requirements: 2.1, 2.2, 2.3_

  - [ ] 3.2 Verify Bug 1 exploration test now passes
    - **Property 1: Expected Behavior** — Login Redirect (Bug 1)
    - Re-run the SAME test from task 1.1
    - **EXPECTED OUTCOME**: Test PASSES — login with no ReturnUrl correctly redirects to role-specific dashboard

  - [ ] 3.3 Verify Bug 1 preservation tests still pass
    - **Property 2: Preservation** — Login paths (Bug 1)
    - Re-run tests from tasks 2.1 and 2.2
    - **EXPECTED OUTCOME**: Both tests PASS — ReturnUrl redirect and invalid-credentials path unchanged

---

- [ ] 4. Fix Bug 2 — Dashboard auth-state timing (`ClientDashboard.razor`)

  - [ ] 4.1 Add `OnAfterRenderAsync` fallback and `StateHasChanged` calls
    - In `OnInitializedAsync`, add `StateHasChanged()` after `loading = false`
    - Add `OnAfterRenderAsync(bool firstRender)` override:
      - Guard: `if (!firstRender || currentUser != null) return`
      - Re-fetch auth state; if authenticated, set `loading = true`, call `StateHasChanged()`
      - Re-run `UserManager.FindByNameAsync`, `EnsureClientProfileExistsAsync`, `GetClientProfileByUserIdAsync`, `LoadProfileDataAsync`
      - Set `loading = false`, call `StateHasChanged()`
    - _Bug_Condition: isBugCondition_Bug2 where auth state not ready at OnInitializedAsync_
    - _Expected_Behavior: dashboard data loads and renders within two render cycles_
    - _Requirements: 2.4_

  - [ ] 4.2 Add `StateHasChanged` to `AdminDashboard.razor` (Bug 9)
    - Add `StateHasChanged()` at end of `LoadAdminDataAsync` method
    - _Requirements: 2.5, 2.14_

---

- [ ] 5. Fix Bug 3 — Duplicate `<PageTitle>` and `<section id="services">` (`Home.razor`)

  - [ ] 5.1 Remove first `<PageTitle>` tag
    - Delete the line `<PageTitle> PROTOUCH | Physio & Slimming Clinic</PageTitle>`
    - Retain the second: `<PageTitle>SRI SAI PROTOUCH | Physio & Slimming Clinic</PageTitle>`
    - _Requirements: 2.6_

  - [ ] 5.2 Remove duplicate `<section id="services">` block
    - Identify the second `<section id="services" class="wellness-section services">` block (the one with "Yoga & Russian Nature Cure" and "Specialised Programs For Children")
    - Remove that entire block (the first occurrence is the corrected version with "Yoga & Holistic Wellness" and "Specialised Programs")
    - Verify exactly one `id="services"` remains in the rendered HTML
    - _Requirements: 2.7, 3.11_

---

- [ ] 6. Fix Bug 4 — Broken image filenames (`Home.razor` + wwwroot)

  - [ ] 6.1 Rename image files to URL-safe names
    - Rename `wwwroot/images/clinic-gallery/WhatsApp Image 2026-08-12 at 12.21.53 AM (1).jpeg` → `hero-treatment.jpeg`
    - Rename `wwwroot/images/clinic-gallery/WhatsApp Image 2026-08-12 at 12.21.53 AM.jpeg` → `care-panel.jpeg`
    - _Requirements: 2.8_

  - [ ] 6.2 Update `<img>` src references in `Home.razor`
    - Update hero image src: `/images/clinic-gallery/hero-treatment.jpeg`
    - Update care-panel image src: `/images/clinic-gallery/care-panel.jpeg`
    - Verify both images render correctly in browser after rename
    - _Requirements: 2.8_

---

- [ ] 7. Fix Bug 5 — Duplicate daily log entries (`ClientDashboard.razor` + `ClinicService.cs`)

  - [ ] 7.1 Add `UpdateDailyLogAsync` to `ClinicService`
    - Add method to `ClinicService.cs`:
      ```csharp
      public async Task UpdateDailyLogAsync(DailyLog log) { ... }
      ```
    - Find by `log.Id` using `_db.DailyLogs.FindAsync(log.Id)`
    - Update: `Weight`, `WaistInches`, `ChestInches`, `HipsInches`, `ArmsInches`, `DietNotes`, `MealPhotoUrl`, `CarbsPercentage`, `ProteinPercentage`, `FatPercentage`, `VegPercentage`, `LoggedAt`
    - Call `_db.SaveChangesAsync()`
    - _Requirements: 2.9_

  - [ ] 7.2 Add upsert branch in `HandleLogSubmission`
    - In `ClientDashboard.razor`, modify `HandleLogSubmission`:
      - If `logModel.Id > 0` → call `await ClinicService.UpdateDailyLogAsync(logModel)`
      - Else → call `await ClinicService.AddDailyLogAsync(logModel)`
    - _Bug_Condition: isBugCondition_Bug5 where logModel.Id > 0_
    - _Expected_Behavior: single DailyLog row per (UserId, LogDate)_
    - _Preservation: new log INSERT path unchanged when logModel.Id == 0_
    - _Requirements: 2.9, 2.10, 3.5_

  - [ ] 7.3 Verify Bug 5 exploration test now passes
    - **Property 1: Expected Behavior** — Daily Log Upsert (Bug 5)
    - Re-run the SAME test from task 1.2
    - **EXPECTED OUTCOME**: Test PASSES — second submission updates existing row, count stays 1

  - [ ] 7.4 Verify Bug 5 preservation test still passes
    - **Property 2: Preservation** — New log INSERT (Bug 5)
    - Re-run test from task 2.3
    - **EXPECTED OUTCOME**: Test PASSES — new log INSERT still creates one new row

---

- [ ] 8. Fix Bug 6 — Cascade delete (`AdminDashboard.razor` + `ClinicService.cs`)

  - [ ] 8.1 Add `DeleteClientDataAsync` to `ClinicService`
    - Add method to `ClinicService.cs` that removes all related rows for the given `userId`:
      - `DailyLogs`, `AttendanceRecords`, `Appointments`, `PaymentNotifications`, `ClientProfiles`
      - The `Settings` row for key `ClientPhoto:{userId}`
    - Save all changes in a single `SaveChangesAsync` call
    - _Requirements: 2.11_

  - [ ] 8.2 Call `DeleteClientDataAsync` before `UserManager.DeleteAsync`
    - In `AdminDashboard.razor`'s `DeleteSelectedClient` method:
      - Extract `var userId = selectedClient.UserId`
      - Call `await ClinicService.DeleteClientDataAsync(userId)` before `await UserManager.DeleteAsync(user)`
    - _Bug_Condition: isBugCondition_Bug6 where relatedRecordCount > 0 and cascadeDeleteCalled = false_
    - _Expected_Behavior: all related rows removed before user deletion_
    - _Requirements: 2.11_

---

- [ ] 9. Fix Bug 7 — Negative macro percentages (`DbInitializer.cs`)

  - [ ] 9.1 Tighten random ranges and add fallback clamp
    - Change ranges to: `carb = rand.Next(25, 35)`, `prot = rand.Next(25, 35)`, `fat = rand.Next(10, 20)`
    - Keep `veg = 100 - carb - prot - fat`
    - Add guard: `if (veg < 0) { carb = 30; prot = 30; fat = 20; veg = 20; }`
    - _Bug_Condition: isBugCondition_Bug7 where carb + prot + fat > 100_
    - _Expected_Behavior: veg >= 0 and sum = 100 for all seed rows_
    - _Requirements: 2.12_

  - [ ] 9.2 Verify Bug 7 exploration test now passes
    - **Property 1: Expected Behavior** — Macro Percentages (Bug 7)
    - Re-run the SAME test from task 1.3 with `carb=44, prot=44, fat=24`
    - **EXPECTED OUTCOME**: Test PASSES — fallback triggers, `veg = 20`, sum = 100

  - [ ] 9.3 Verify Bug 7 preservation test still passes
    - **Property 2: Preservation** — Valid macro inputs (Bug 7)
    - Re-run test from task 2.6
    - **EXPECTED OUTCOME**: Test PASSES — valid inputs still produce `veg >= 0`

---

- [ ] 10. Fix Bug 8 — New registrant blocked by email confirmation (`Program.cs`)

  - [ ] 10.1 Set `RequireConfirmedAccount = false`
    - In `Program.cs`, change:
      `options.SignIn.RequireConfirmedAccount = true;`
      to:
      `options.SignIn.RequireConfirmedAccount = false; // Phone OTP is used for verification`
    - _Bug_Condition: isBugCondition_Bug8 where EmailConfirmed=false and PhoneNumberConfirmed=true_
    - _Expected_Behavior: PasswordSignInAsync returns Succeeded=true for phone-verified users_
    - _Requirements: 2.13_

  - [ ] 10.2 Verify Bug 8 exploration test now passes
    - **Property 1: Expected Behavior** — Email Confirmation Gate (Bug 8)
    - Re-run the SAME test from task 1.5
    - **EXPECTED OUTCOME**: Test PASSES — `result.Succeeded = true` for phone-verified users

---

- [ ] 11. Fix Bug 11 — Double WhatsApp country-code prefix (`ClinicService.cs`)

  - [ ] 11.1 Add double-prefix guard in `BuildWhatsAppLink`
    - After the `StartsWith("00")` strip, add:
      ```csharp
      if (normalized.Length > 12 && normalized.StartsWith("91"))
          normalized = normalized[2..];
      ```
    - _Bug_Condition: isBugCondition_Bug11 where digits.Length > 12 and starts with "91"_
    - _Expected_Behavior: URL contains exactly the correct 12-digit number_
    - _Preservation: 10-digit and +91-prefixed numbers unchanged_
    - _Requirements: 2.15, 3.7, 3.8_

  - [ ] 11.2 Verify Bug 11 exploration test now passes
    - **Property 1: Expected Behavior** — Double WhatsApp Prefix (Bug 11)
    - Re-run the SAME test from task 1.4
    - **EXPECTED OUTCOME**: Test PASSES — `https://wa.me/919812345678?text=Hello`

  - [ ] 11.3 Verify Bug 11 preservation tests still pass
    - **Property 2: Preservation** — WhatsApp link formats (Bug 11)
    - Re-run tests from tasks 2.4 and 2.5
    - **EXPECTED OUTCOME**: Both PASS — 10-digit and +91 inputs still produce correct URLs

---

## Phase 4 — Checkpoint

- [ ] 12. Checkpoint — Ensure all tests pass and commit changes

  - [ ] 12.1 Run full test suite
    - Ensure all exploration tests (now fix-checking) pass
    - Ensure all preservation tests still pass
    - Ask user if any questions arise before proceeding

  - [ ] 12.2 Commit and push to a new branch
    - Stage all changed files:
      - `Components/Account/Pages/Login.razor`
      - `Components/Pages/ClientDashboard.razor`
      - `Components/Pages/AdminDashboard.razor`
      - `Services/ClinicService.cs`
      - `Components/Pages/Home.razor`
      - `Data/DbInitializer.cs`
      - `Program.cs`
      - `wwwroot/images/clinic-gallery/hero-treatment.jpeg` (renamed)
      - `wwwroot/images/clinic-gallery/care-panel.jpeg` (renamed)
    - Commit with message: `fix: resolve 11 confirmed bugs (login redirect, dashboard load, duplicate logs, cascade delete, macro seed, email gate, WhatsApp prefix, markup duplicates, image URLs)`
    - Push to a new branch `fix/protouch-bug-fixes`
    - Open a pull request targeting `main`
