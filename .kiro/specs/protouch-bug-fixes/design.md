# ProTouch Bug Fixes — Bugfix Design

## Overview

This document formalises the bug conditions, root causes, implementation changes, and
testing strategy for the eleven confirmed defects in the ProTouch Clinic application.

The bugs are grouped into three tiers by severity and testing approach:

| Tier | Bugs | Approach |
|------|------|----------|
| **Critical auth/render** | 1, 2, 8, 9 | Exploratory + fix-checking + preservation |
| **Data integrity** | 5, 6, 7 | Property-based (deterministic inputs) |
| **Markup / utility** | 3, 4, 11 | Example-based; snapshot / URL assertion |

---

## Glossary

- **Bug_Condition (C)**: The predicate over inputs that identifies the defective code path.
- **Property (P)**: The postcondition that the fixed code must satisfy for all inputs where C holds.
- **Preservation**: All behaviours that must remain identical between `F` (unfixed) and `F'` (fixed).
- **F**: The original (unfixed) function or component as it exists before this change set.
- **F'**: The fixed function or component after applying the changes described herein.
- **SSR**: Blazor Server-Side Rendering — the rendering model used by this application.
- **AuthState**: The `AuthenticationState` object provided by `IdentityRevalidatingAuthenticationStateProvider`.
- **ReturnUrl**: The optional query parameter passed to `/Account/Login` by the Identity middleware.
- **logModel**: The `DailyLog` form binding object in `ClientDashboard.razor`.
- **isBugCondition(X)**: A function returning `true` when input `X` triggers the defect.

---

## Bug Details

### Bug 1 — Login Redirect Loop

#### Bug Condition

The bug manifests when a user navigates directly to `/Account/Login` without a `ReturnUrl`
and successfully authenticates. `RedirectManager.RedirectTo(null)` resolves to
`NavigationManager.NavigateTo("")`, which emits an HTTP redirect to `/`. The Identity
middleware sees an authenticated request bound for the protected dashboard, finds no
role-specific route, and issues another redirect back to `/Account/Login`, creating a loop
that the browser breaks by staying on the login page.

**Formal Specification:**
```
FUNCTION isBugCondition_Bug1(request)
  INPUT: request of type LoginRequest
  OUTPUT: boolean

  RETURN request.credentials_valid = true
         AND (request.ReturnUrl = null OR request.ReturnUrl = "")
END FUNCTION
```

**Examples:**
- User navigates to `https://protouch.onrender.com/Account/Login` directly → enters `admin@clinic.com` / `AdminPass123!` → clicks Login → URL stays at `/Account/Login`. ✗
- User is redirected to `/Account/Login?ReturnUrl=%2Fadmin-dashboard` → logs in → redirected to `/admin-dashboard`. ✓ (only when ReturnUrl is present)

---

### Bug 2 — Dashboard Auth-State Timing

#### Bug Condition

The Blazor circuit for `ClientDashboard` is sometimes established before the Identity
cookie from the login POST is fully validated by `IdentityRevalidatingAuthenticationStateProvider`.
`OnInitializedAsync` runs once; if `GetAuthenticationStateAsync()` returns an unauthenticated
state at that moment, `currentUser` stays `null` and the dashboard shows an error panel.

**Formal Specification:**
```
FUNCTION isBugCondition_Bug2(renderEvent)
  INPUT: renderEvent of type BlazorInitEvent
  OUTPUT: boolean

  RETURN renderEvent.source = "post_login_redirect"
         AND renderEvent.auth_state.IsAuthenticated = false_at_init_time
         AND renderEvent.component = ClientDashboard
END FUNCTION
```

**Examples:**
- Client logs in → immediately redirected to `/client-dashboard` → spinner never resolves; error panel shows "Profile record could not be resolved". ✗

---

### Bug 5 — Duplicate Daily Log INSERT

#### Bug Condition

When a client has already logged weight today, `LoadProfileDataAsync` loads the existing
`DailyLog` row into `logModel` (including its `Id > 0`). When the form is submitted again,
`HandleLogSubmission` unconditionally calls `ClinicService.AddDailyLogAsync(logModel)` which
calls `_db.DailyLogs.Add(log)`. Entity Framework Core treats an entity with an existing Id as
a new tracked entity in the `Added` state, so it executes an INSERT rather than an UPDATE.

**Formal Specification:**
```
FUNCTION isBugCondition_Bug5(submission)
  INPUT: submission of type DailyLogFormSubmission
  OUTPUT: boolean

  RETURN submission.logModel.Id > 0
         AND submission.logModel.LogDate = DateTime.Today
         AND DailyLogExists(submission.logModel.UserId, DateTime.Today) = true
END FUNCTION
```

**Examples:**
- Client submits weight at 8am (new log, `Id=0`) → row inserted. ✓
- Client edits weight at 6pm (existing `Id=42`) → second row inserted for same date. ✗

---

### Bug 6 — Orphaned Records After Client Deletion

#### Bug Condition

`DeleteSelectedClient` calls `UserManager.DeleteAsync(user)` which only removes the
`AspNetUsers` row. The EF Core model does not configure cascade-delete behaviour for
`DailyLog`, `AttendanceRecord`, `Appointment`, `PaymentNotification`, `ClientProfile`, or
`Settings`, so those rows remain with a dangling `UserId` foreign key.

**Formal Specification:**
```
FUNCTION isBugCondition_Bug6(deleteOp)
  INPUT: deleteOp of type ClientDeleteOperation
  OUTPUT: boolean

  RETURN deleteOp.relatedRecordCount(userId) > 0
         AND deleteOp.cascadeDeleteCalled = false
END FUNCTION
```

---

### Bug 7 — Negative Macro Percentages in Seed Data

#### Bug Condition

`DbInitializer` generates random macro values from ranges that, when summed, can exceed 100:
- `carb ∈ [25, 44]`, `prot ∈ [30, 44]`, `fat ∈ [15, 24]`
- Maximum combined: `44 + 44 + 24 = 112` → `veg = 100 - 112 = -12`

**Formal Specification:**
```
FUNCTION isBugCondition_Bug7(seedInput)
  INPUT: seedInput of type MacroSeedValues
  OUTPUT: boolean

  RETURN (seedInput.carb + seedInput.prot + seedInput.fat) > 100
END FUNCTION
```

---

### Bug 8 — New Registrant Blocked by Email Confirmation Gate

#### Bug Condition

`Program.cs` sets `options.SignIn.RequireConfirmedAccount = true`. The registration flow
uses phone OTP for verification. `VerifyPhone.razor` sets `PhoneNumberConfirmed = true` but
never sets `EmailConfirmed = true`. Therefore `PasswordSignInAsync` returns
`result.Succeeded = false` silently.

**Formal Specification:**
```
FUNCTION isBugCondition_Bug8(user)
  INPUT: user of type ApplicationUser
  OUTPUT: boolean

  RETURN user.EmailConfirmed = false
         AND user.PhoneNumberConfirmed = true
         AND RequireConfirmedAccount = true
END FUNCTION
```

---

### Bug 11 — Double Country-Code in WhatsApp URL

#### Bug Condition

`BuildWhatsAppLink` strips `00`-prefixed numbers and prepends `91` for 10-digit numbers but
does not handle the case where the digit string is already 14+ characters beginning with `91`
(double-prefixed).

**Formal Specification:**
```
FUNCTION isBugCondition_Bug11(phone)
  INPUT: phone of type string
  OUTPUT: boolean

  digits ← ExtractDigits(phone)
  IF digits.StartsWith("00") THEN digits ← digits[2..]
  RETURN digits.Length > 12 AND digits.StartsWith("91")
END FUNCTION
```

---

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviours (must not regress):**
- Logging in with a valid `ReturnUrl` continues to redirect to that URL.
- Invalid credentials continue to show an error message on `/Account/Login`.
- Locked-out accounts continue to redirect to `/Account/Lockout`.
- Clients already authenticated navigating directly to `/client-dashboard` load data in `OnInitializedAsync` without the `OnAfterRenderAsync` fallback being required.
- New daily log submissions (no existing entry for today) continue to INSERT a new row.
- Admin can still create, edit, and view all clients and visits.
- `BuildWhatsAppLink` continues to correctly handle 10-digit numbers (`+91` prepend) and standard `+91XXXXXXXXXX` inputs.
- The home page continues to render the services section content intact.
- All seeded client and admin accounts continue to function post-fix.

---

## Hypothesized Root Causes

### Bug 1 — Login Redirect
1. **Null ReturnUrl not guarded**: `RedirectManager.RedirectTo(ReturnUrl)` passes `null` directly; no fallback path is defined.
2. **No role-based routing**: After login, the code does not inspect the user's role to choose between `/admin-dashboard` and `/client-dashboard`.

### Bug 2 — Dashboard Load Timing
1. **Single lifecycle hook**: `OnInitializedAsync` runs once; there is no fallback if auth state isn't ready.
2. **Missing `StateHasChanged` call**: After async data loads, the component doesn't explicitly signal a re-render in all code paths.

### Bug 3 — Duplicate Markup
1. **Merge/paste error**: Two `<PageTitle>` tags and two `<section id="services">` blocks were left in `Home.razor`, likely from a copy-paste refactoring.

### Bug 4 — Broken Image URLs
1. **Filenames contain spaces and parentheses**: WhatsApp export filenames are not URL-safe and break on web servers that don't auto-encode static file paths.

### Bug 5 — Duplicate Log INSERT
1. **EF Core `Add` on existing entity**: `_db.DailyLogs.Add(log)` on a detached entity with `Id > 0` generates an INSERT. The code should branch on `log.Id > 0` to call an UPDATE path.

### Bug 6 — Cascade Delete
1. **No cascade configured in DbContext**: EF Core defaults to `Restrict` for FK relationships pointing at Identity user tables. Manual deletion of related rows is required before deleting the user.

### Bug 7 — Negative Macros
1. **Random ranges too wide**: Upper bounds for `carb`, `prot`, and `fat` can sum to > 100, producing negative `veg`.

### Bug 8 — Email Confirmation Gate
1. **Mismatched verification strategy**: The app uses phone OTP but `RequireConfirmedAccount = true` gates on email confirmation, not phone confirmation.

### Bug 9 — StateHasChanged
1. **Implicit re-render assumption**: Blazor Server re-renders after event handlers complete, but only in the same synchronisation context. Operations that branch through `LoadAdminDataAsync` without explicit `StateHasChanged()` may miss a render cycle.

### Bug 11 — Double Country Code
1. **Missing > 12 digit guard**: The normalisation logic handles `00`-prefix and 10-digit numbers but doesn't strip an already-present `91` prefix on 14+ digit inputs.

---

## Correctness Properties

Property 1: Bug Condition — Critical Login Redirect

_For any_ login request where valid credentials are submitted with no `ReturnUrl`, the fixed
`LoginUser` method SHALL redirect Admin users to `/admin-dashboard` and Client users to
`/client-dashboard` without looping back to `/Account/Login`.

**Validates: Requirements 2.1, 2.2, 2.3**

---

Property 2: Preservation — Existing Login Paths Unaffected

_For any_ login request where `ReturnUrl` is a valid non-empty path, the fixed `LoginUser`
method SHALL produce the same redirect behaviour as the original (`F(X) = F'(X)` for all
`X` where `isBugCondition_Bug1(X) = false`), preserving locked-out redirects, invalid
credential errors, and `ReturnUrl`-based redirects.

**Validates: Requirements 3.1, 3.2, 3.3**

---

Property 3: Bug Condition — Daily Log Upsert Correctness

_For any_ form submission where `logModel.Id > 0`, the fixed `HandleLogSubmission` SHALL call
`UpdateDailyLogAsync` and the total count of `DailyLog` rows for the given `(UserId, LogDate)`
SHALL remain equal to 1 after submission.

**Validates: Requirements 2.9, 2.10**

---

Property 4: Preservation — New Log INSERT Unchanged

_For any_ form submission where `logModel.Id == 0`, the fixed `HandleLogSubmission` SHALL
produce exactly the same database outcome as the original (one new `DailyLog` row inserted),
preserving the existing insert path.

**Validates: Requirements 3.5**

---

Property 5: Bug Condition — Macro Percentages Always Valid

_For any_ seed invocation of `DbInitializer`, the fixed macro generation SHALL produce
`CarbsPercentage + ProteinPercentage + FatPercentage + VegPercentage = 100` and each
individual value SHALL be ≥ 0.

**Validates: Requirements 2.12**

---

Property 6: Preservation — WhatsApp Link Correctness Across Input Formats

_For any_ phone input where `isBugCondition_Bug11(phone) = false` (10-digit, `+91`-prefixed,
or `00`-prefixed), the fixed `BuildWhatsAppLink` SHALL produce the same `wa.me` URL as the
original function, preserving all valid link formats.

**Validates: Requirements 3.7, 3.8**

---

## Fix Implementation

### Changes Required

#### Bug 1 — `Login.razor`
**File**: `Components/Account/Pages/Login.razor`
1. After `result.Succeeded`, add `UserManager` injection (already available via `@inject`).
2. Look up the signed-in user by name and query their role.
3. If `ReturnUrl` is non-null and non-empty → redirect to `ReturnUrl`.
4. Else if role is `"Admin"` → redirect to `/admin-dashboard`.
5. Else → redirect to `/client-dashboard`.
6. Remove the bare `RedirectManager.RedirectTo(ReturnUrl)` call.

#### Bug 2 — `ClientDashboard.razor`
**File**: `Components/Pages/ClientDashboard.razor`
1. Add `StateHasChanged()` at the end of `OnInitializedAsync` after the data load.
2. Add `OnAfterRenderAsync(bool firstRender)` override: if `firstRender && currentUser == null`, re-attempt auth state retrieval and data load, then call `StateHasChanged()`.

#### Bug 2 / Bug 9 — `AdminDashboard.razor`
**File**: `Components/Pages/AdminDashboard.razor`
1. Add `StateHasChanged()` at the end of `LoadAdminDataAsync`.

#### Bug 3 — `Home.razor`
**File**: `Components/Pages/Home.razor`
1. Remove the first `<PageTitle>` tag.
2. Remove the entire duplicate second `<section id="services">` block.

#### Bug 4 — `Home.razor` + wwwroot
**Files**: `Components/Pages/Home.razor`, `wwwroot/images/clinic-gallery/`
1. Rename `WhatsApp Image 2026-08-12 at 12.21.53 AM (1).jpeg` → `hero-treatment.jpeg`
2. Rename `WhatsApp Image 2026-08-12 at 12.21.53 AM.jpeg` → `care-panel.jpeg`
3. Update `<img src="...">` references in `Home.razor` accordingly.

#### Bug 5 — `ClientDashboard.razor` + `ClinicService.cs`
**Files**: `Components/Pages/ClientDashboard.razor`, `Services/ClinicService.cs`
1. Add `UpdateDailyLogAsync(DailyLog log)` to `ClinicService` that finds by `Id` and updates all fields.
2. In `HandleLogSubmission`, branch: `if (logModel.Id > 0) UpdateDailyLogAsync` else `AddDailyLogAsync`.

#### Bug 6 — `AdminDashboard.razor` + `ClinicService.cs`
**Files**: `Components/Pages/AdminDashboard.razor`, `Services/ClinicService.cs`
1. Add `DeleteClientDataAsync(string userId)` to `ClinicService` that removes `DailyLogs`, `AttendanceRecords`, `Appointments`, `PaymentNotifications`, `ClientProfiles`, and the `ClientPhoto:{userId}` setting.
2. In `DeleteSelectedClient`, call `DeleteClientDataAsync` before `UserManager.DeleteAsync`.

#### Bug 7 — `DbInitializer.cs`
**File**: `Data/DbInitializer.cs`
1. Tighten random ranges: `carb ∈ [25,34]`, `prot ∈ [25,34]`, `fat ∈ [10,19]`.
2. Add fallback: if `veg < 0` then reset to `{ carb=30, prot=30, fat=20, veg=20 }`.

#### Bug 8 — `Program.cs`
**File**: `Program.cs`
1. Change `options.SignIn.RequireConfirmedAccount = true` to `false`.
   Rationale: Phone OTP is the designated verification mechanism; email confirmation is not wired up.

#### Bug 11 — `ClinicService.cs`
**File**: `Services/ClinicService.cs`
1. In `BuildWhatsAppLink`, after stripping `00`-prefix, add:
   `if (normalized.Length > 12 && normalized.StartsWith("91")) normalized = normalized[2..];`
   before the 10-digit `91`-prepend guard.

---

## Testing Strategy

### Validation Approach

The testing strategy follows the four-phase bug-condition methodology:
1. **Explore** — write tests against the UNFIXED code to surface counterexamples.
2. **Preserve** — write preservation tests against UNFIXED code; verify they pass.
3. **Implement** — apply all fixes.
4. **Validate** — re-run exploration tests (expect PASS) and preservation tests (expect still PASS).

---

### Exploratory Bug Condition Checking

**Goal**: Surface counterexamples that demonstrate each bug BEFORE implementing any fix.
Failure of these tests on unfixed code confirms the bugs exist.

**Test Cases:**
1. **Bug 1 — Login loop**: Simulate a `LoginUser()` call with valid credentials and `ReturnUrl = null`. Assert that the resulting navigation does NOT loop back to `/Account/Login`. *(Will FAIL on unfixed code)*
2. **Bug 5 — Duplicate log**: Call `HandleLogSubmission` twice with the same `logModel` where `logModel.Id = 42`. Assert that the `DailyLog` count for that `(UserId, LogDate)` remains 1. *(Will FAIL on unfixed code — count becomes 2)*
3. **Bug 7 — Negative macros**: Run `DbInitializer` seed with fixed random seed producing `carb=44, prot=44, fat=24`. Assert `VegPercentage ≥ 0`. *(Will FAIL on unfixed code)*
4. **Bug 11 — Double prefix**: Call `BuildWhatsAppLink("91919812345678", "test")`. Assert result equals `https://wa.me/919812345678?text=test`. *(Will FAIL on unfixed code)*
5. **Bug 8 — Blocked registrant**: Create a user with `EmailConfirmed = false`, `PhoneNumberConfirmed = true`. Call `PasswordSignInAsync`. Assert `result.Succeeded = true`. *(Will FAIL on unfixed code)*

**Expected Counterexamples:**
- Bug 1: `NavigationManager.NavigateTo` receives `""` or causes a loop.
- Bug 5: `_db.DailyLogs` gains a second row for the same `(UserId, LogDate)`.
- Bug 7: `VegPercentage = -12`.
- Bug 11: URL contains `91919…`.
- Bug 8: `result.Succeeded = false` with `RequireConfirmedAccount = true`.

---

### Fix Checking

**Goal**: Verify all bug-condition inputs produce the expected correct behavior after fixes.

**Pseudocode:**
```
FOR ALL input WHERE isBugCondition(input) DO
  result := fixedFunction(input)
  ASSERT expectedBehavior(result)
END FOR
```

---

### Preservation Checking

**Goal**: Verify that all non-buggy inputs produce the same results as the original code.

**Pseudocode:**
```
FOR ALL input WHERE NOT isBugCondition(input) DO
  ASSERT originalFunction(input) = fixedFunction(input)
END FOR
```

**Testing Approach**: Property-based testing is recommended for preservation of
`BuildWhatsAppLink` (generate arbitrary phone strings) and macro sum invariant (generate
random values). Unit tests suffice for login redirect and log upsert scenarios.

---

### Unit Tests

- `Login.razor` — test `LoginUser()` with `ReturnUrl = null`, Admin role → asserts redirect to `/admin-dashboard`.
- `Login.razor` — test `LoginUser()` with `ReturnUrl = "/somewhere"` → asserts redirect to `/somewhere`.
- `Login.razor` — test `LoginUser()` with invalid credentials → asserts error message set, no redirect.
- `ClinicService.UpdateDailyLogAsync` — pass existing `Id`, assert only that row is updated in DB.
- `ClinicService.DeleteClientDataAsync` — create full client data set, call method, assert all related tables are empty for that `UserId`.
- `ClinicService.BuildWhatsAppLink` — test with 10-digit, 12-digit (+91), and 14-digit double-prefix inputs.
- `DbInitializer` macro generation — assert sum equals 100 and all values ≥ 0 over 1000 random iterations.

### Property-Based Tests

- Generate 1000 random phone strings for `BuildWhatsAppLink`; assert each result is a valid `https://wa.me/{digits}` URL where `digits` is 10–12 characters (Property 6).
- Generate 1000 random macro seed inputs; assert `carb + prot + fat + veg = 100` and `veg ≥ 0` (Property 5).
- Generate 50 `DailyLog` submissions with `Id > 0`; assert single-row invariant holds post-submit (Property 3).

### Integration Tests

- Full login flow: navigate to `/Account/Login`, submit credentials, assert redirect to role-appropriate dashboard.
- Client dashboard: log in as client, navigate to dashboard, assert profile data visible within 3 seconds.
- Admin delete: create client, log a visit, delete client, query all related tables, assert all empty.
