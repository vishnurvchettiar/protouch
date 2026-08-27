# Implementation Plan: Account & Status Pages Consistency

## Overview

This plan migrates all 18 `Anonymous_Account_Page` files plus `StatusMessage.razor` and `ExternalLoginPicker.razor` onto a new `AuthLayout.razor` and a new `pt-auth-*` canonical CSS class family, replacing the legacy `.login-*`/`.auth-*` systems and the bare/unstyled confirmation pages, while leaving every `@code` block's method signatures, redirect targets, and Identity API calls unchanged (Requirement 5). Work proceeds in the order mandated by the design's Migration Plan: foundation first (layout + CSS), then page groups (each group touches disjoint files so groups can be executed in parallel), then shared components, then comprehensive structural-property tests across the full page set, then whole-solution verification.

Convert the feature design into a series of prompts for a code-generation LLM that will implement each step with incremental progress. Make sure that each prompt builds on the previous prompts, and ends with wiring things together. There should be no hanging or orphaned code that isn't integrated into a previous step. Focus ONLY on tasks that involve writing, modifying, or testing code.

## Tasks

- [x] 1. Create AuthLayout and the canonical CSS foundation
  - [x] 1.1 Create `ClinicApp/Components/Layout/AuthLayout.razor`
    - New file. `@inherits LayoutComponentBase`. Render `pt-auth-shell` > `pt-auth-card` wrapper containing a `pt-auth-brand` block with `<img src="/images/protouch-logo.svg" alt="ProTouch Physio - Slimming Clinic" class="pt-auth-logo" />`, followed by `@Body`, exactly as specified in the design's "`AuthLayout.razor` (new file)" section.
    - _Requirements: 1.1, 1.2, 1.4, 4.3, 4.4, 4.5_

  - [x] 1.2 Add the `CANONICAL AUTH UI LIBRARY (pt-auth-*)` CSS section to `ClinicApp/wwwroot/app.css` and remove the legacy sections
    - Delete the `AUTH PAGES — Login & Register (uses PublicLayout, no sidebar)` banner-commented section (the `.auth-page`, `.auth-card`, `.auth-card-wide`, `.auth-brand`, `.auth-error`, `.auth-grid`, `.auth-field`, `.auth-input`, `.auth-field-error`, `.auth-remember`, `.auth-submit`, `.auth-footer` rules and their media queries).
    - Delete the `PROTOUCH LOGIN PAGE — Split-Screen Redesign` banner-commented section (the `.login-split-page`, `.login-form-panel`, `.login-form-inner`, `.login-logo`, `.login-role-badge`, `.login-heading`, `.login-subheading`, `.login-error-alert`, `.login-validation-summary`, `.login-field`, `.login-label`, `.login-input-wrap`, `.login-input`, `.login-field-error`, `.login-remember`, `.login-toggle-*`, `.login-submit-btn`, `.login-spinner`, `.login-form-footer`, `.login-visual-panel`, `.login-orb-*`, `.login-visual-*`, `.login-stat-*`, `.login-pills` rules and their media/reduced-motion queries).
    - In their place, add one new banner-commented section titled `CANONICAL AUTH UI LIBRARY (pt-auth-*)` defining exactly one rule set per Auth_UI_Library concept from the design's class-contract table: `.pt-auth-shell`, `.pt-auth-card` (plus a wide-card modifier for `Register.razor`'s two-column grid), `.pt-auth-brand`, `.pt-auth-logo`, `.pt-auth-heading`, `.pt-auth-subheading`, `.pt-auth-input`, `.pt-auth-button`, `.pt-auth-alert` + `.pt-auth-alert--success`/`.pt-auth-alert--error`/`.pt-auth-alert--info`, `.pt-auth-field-error`, `.pt-auth-link`, and the page-local split-panel classes `Login.razor` still needs (`.pt-auth-split-page`, `.pt-auth-form-panel`, `.pt-auth-visual-panel`, and related decorative/orb/stat classes), all scoped to the `.pt-auth-` prefix so the section cannot collide with `.wellness-page`, `.dashboard-theme`, or any other existing scope. Include a responsive breakpoint and reduced-motion block mirroring the removed sections' coverage.
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 3.1, 3.2, 3.3, 3.4, 3.5, 4.3, 4.4_

  - [x]* 1.3 Write unit test verifying the AuthLayout/CSS foundation
    - New file `ClinicApp.Tests/AuthLayoutFoundationTests.cs`, following the `SolutionRoot`/`Path.Combine` pattern in `LayoutRegressionTests.cs`.
    - Assert `AuthLayout.razor` contains `pt-auth-shell`, `pt-auth-card`, and `/images/protouch-logo.svg`.
    - Assert `app.css` contains `CANONICAL AUTH UI LIBRARY` and no longer contains `.auth-page {` or `.login-split-page {`.
    - _Requirements: 1.4, 2.1, 4.5_

- [x] 2. Migrate `Login.razor` and `Register.razor` to the canonical shell (Group A — the two deliberately-designed pages)
  - [x] 2.1 Migrate `ClinicApp/Components/Account/Pages/Login.razor`
    - Change `@layout PublicLayout` to `@layout AuthLayout`. Replace every `login-*` class with the corresponding `pt-auth-*` class from the design's class-contract table (`login-heading`→`pt-auth-heading`, `login-input`→`pt-auth-input`, `login-submit-btn`→`pt-auth-button`, `login-field-error`→`pt-auth-field-error`, `login-error-alert`→`pt-auth-alert pt-auth-alert--error`, footer link→`pt-auth-link`, etc.). Preserve the existing split-panel structure (form panel + ambient decorative visual panel) as page-local markup using the new `pt-auth-split-page`/`pt-auth-form-panel`/`pt-auth-visual-panel` classes added in task 1.2. Keep both `<img src="/images/WhatsApp Image 2026-08-12 at 12.25.21 AM.jpeg" ...>` references changed to `/images/protouch-logo.svg` (one in the form panel, one in the decorative visual panel — the permitted secondary use per Requirement 4.4). Do not change `LoginUser`, `OnInitializedAsync`, `FormName="login"`, or any redirect target string in the `@code` block.
    - _Requirements: 1.1, 1.2, 1.3, 2.2, 2.6, 3.4, 3.5, 4.3, 4.4, 4.5, 5.1, 5.2, 5.7_

  - [x] 2.2 Migrate `ClinicApp/Components/Account/Pages/Register.razor`
    - Change `@layout PublicLayout` to `@layout AuthLayout`. Replace every `auth-*` class with the corresponding `pt-auth-*` class (`auth-card auth-card-wide`→shell's wide-card modifier, `auth-grid`→`pt-auth-grid` or equivalent, `auth-field`/`auth-input`/`auth-field-error`/`auth-submit`/`auth-footer`→`pt-auth-*` equivalents). Remove the page-level `<img src="/images/protouch-logo.svg" ...>` and its wrapping `auth-brand` block entirely, since the shell now renders the logo once (avoiding a duplicate per Requirement 4.4); keep the `<p>Create your client account</p>` tagline as page-local markup below the shell logo if no longer inside the removed brand block. Do not change `RegisterUser`, `FormName="register"`, or any redirect target string in the `@code` block.
    - _Requirements: 1.1, 1.2, 1.3, 2.3, 2.6, 3.4, 3.5, 4.3, 4.4, 4.5, 5.1, 5.2, 5.7_

  - [x]* 2.3 Write unit tests for `Login.razor`/`Register.razor` migration
    - New file `ClinicApp.Tests/LoginRegisterMigrationTests.cs`.
    - Assert both files contain the literal `@layout AuthLayout` and no other `@layout` directive.
    - Assert both files contain `FormName="login"` / `FormName="register"` respectively (Requirement 5.1 preservation).
    - Assert `Login.razor` references `/images/protouch-logo.svg` exactly twice and `Register.razor` references it exactly zero times at the page level (Requirement 4.4/4.5).
    - Assert neither file contains any class token starting with `login-` or `auth-`.
    - _Requirements: 1.1, 2.2, 2.3, 4.4, 4.5, 5.1_

- [x] 3. Migrate the 9 previously-bare confirmation/error pages (Group B)
  - [x] 3.1 Migrate `ClinicApp/Components/Account/Pages/ConfirmEmail.razor`
    - Add `@layout AuthLayout`. Change `<h1>Confirm email</h1>` to `<h1 class="pt-auth-heading">Confirm email</h1>`. Do not change the `OnInitializedAsync` redirect, 404 status-code assignment, or `StatusMessage Message="@statusMessage"` usage.
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 2.5, 4.1, 5.2, 5.7_

  - [x] 3.2 Migrate `ClinicApp/Components/Account/Pages/ConfirmEmailChange.razor`
    - Add `@layout AuthLayout`. Change `<h1>Confirm email change</h1>` to `<h1 class="pt-auth-heading">Confirm email change</h1>`. Do not change the `OnInitializedAsync` redirect or Identity API calls.
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 2.5, 4.1, 5.2, 5.7_

  - [x] 3.3 Migrate `ClinicApp/Components/Account/Pages/RegisterConfirmation.razor`
    - Add `@layout AuthLayout`. Change `<h1>Register confirmation</h1>` to `<h1 class="pt-auth-heading">Register confirmation</h1>`. Change `<p role="alert">Please check your email to confirm your account.</p>` to carry `class="pt-auth-alert pt-auth-alert--info"`. Do not change the email-confirmation-link generation logic, the 404 status-code assignment, or the redirect for a missing `Email` query parameter.
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 2.5, 3.3, 4.1, 5.2, 5.6, 5.7_

  - [x] 3.4 Migrate `ClinicApp/Components/Account/Pages/ForgotPasswordConfirmation.razor`
    - Add `@layout AuthLayout`. Change `<h1>Forgot password confirmation</h1>` to `<h1 class="pt-auth-heading">Forgot password confirmation</h1>`. Change `<p role="alert">Please check your email to reset your password.</p>` to carry `class="pt-auth-alert pt-auth-alert--success"`.
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 2.5, 3.3, 4.1_

  - [x] 3.5 Migrate `ClinicApp/Components/Account/Pages/ResetPasswordConfirmation.razor`
    - Add `@layout AuthLayout`. Change `<h1>Reset password confirmation</h1>` to `<h1 class="pt-auth-heading">Reset password confirmation</h1>`. Change the `<p role="alert">` to carry `class="pt-auth-alert pt-auth-alert--success"`, and change the `<a href="Account/Login">click here to log in</a>` link to carry `class="pt-auth-link"`.
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 2.5, 3.3, 4.1_

  - [x] 3.6 Migrate `ClinicApp/Components/Account/Pages/InvalidPasswordReset.razor`
    - Add `@layout AuthLayout`. Change `<h1>Invalid password reset</h1>` to `<h1 class="pt-auth-heading">Invalid password reset</h1>`. Change `<p role="alert">The password reset link is invalid.</p>` to carry `class="pt-auth-alert pt-auth-alert--error"`.
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 2.5, 3.3, 4.1_

  - [x] 3.7 Migrate `ClinicApp/Components/Account/Pages/InvalidUser.razor`
    - Add `@layout AuthLayout`. Correct the heading element from `<h3>Invalid user</h3>` to `<h1 class="pt-auth-heading">Invalid user</h1>` (Requirement 4.2 heading-level fix). Leave `<StatusMessage />` untouched.
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 2.5, 4.1, 4.2_

  - [x] 3.8 Migrate `ClinicApp/Components/Account/Pages/Lockout.razor`
    - Add `@layout AuthLayout`. Change `<h1 class="text-danger">Locked out</h1>` to `<h1 class="pt-auth-heading">Locked out</h1>`. Change `<p class="text-danger" role="alert">This account has been locked out, please try again later.</p>` to carry `class="pt-auth-alert pt-auth-alert--error"` (remove `text-danger`).
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 2.5, 3.3, 4.1_

  - [x] 3.9 Migrate `ClinicApp/Components/Account/Pages/AccessDenied.razor`
    - Add `@layout AuthLayout`. Change `<h1 class="text-danger">Access denied</h1>` to `<h1 class="pt-auth-heading">Access denied</h1>`. Change `<p class="text-danger">You do not have access to this resource.</p>` to carry `class="pt-auth-alert pt-auth-alert--error"` (remove `text-danger`).
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 2.5, 3.3, 4.1_

  - [x]* 3.10 Write unit tests for the 9 bare-page migrations' Identity-behavior preservation
    - New file `ClinicApp.Tests/BarePagesIdentityRegressionTests.cs`.
    - For `ConfirmEmail.razor`: assert it still contains `StatusCodes.Status404NotFound` and `UserManager.ConfirmEmailAsync`.
    - For `ConfirmEmailChange.razor`: assert it still contains `UserManager.ChangeEmailAsync` and `SignInManager.RefreshSignInAsync`.
    - For `RegisterConfirmation.razor`: assert it still contains `StatusCodes.Status404NotFound` and `GenerateEmailConfirmationTokenAsync`.
    - Use a `[Theory]`/`[MemberData]` list over all 9 files asserting each contains `@layout AuthLayout`.
    - _Requirements: 5.2, 5.6, 5.7_

- [x] 4. Checkpoint - ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Migrate password-reset and email-confirmation flow pages (Group C)
  - [x] 5.1 Migrate `ClinicApp/Components/Account/Pages/ForgotPassword.razor`
    - Add `@layout AuthLayout`. Replace `<h1>Forgot your password?</h1>`/`<h2>Enter your email.</h2>` with `<h1 class="pt-auth-heading">Forgot your password?</h1>` and `<p class="pt-auth-subheading">Enter your email.</p>`. Replace `form-control`/`text-danger`/`btn-primary`/`btn-lg`/`w-100` Bootstrap classes with `pt-auth-input`/`pt-auth-field-error`/`pt-auth-button`. Do not change `OnValidSubmitAsync`, `FormName="forgot-password"`, or the password-reset-token/callback-URL generation logic.
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 2.5, 3.5, 4.1, 5.1, 5.2, 5.7_

  - [x] 5.2 Migrate `ClinicApp/Components/Account/Pages/ResetPassword.razor`
    - Add `@layout AuthLayout`. Replace `<h1>Reset password</h1>`/`<h2>Reset your password.</h2>` with `<h1 class="pt-auth-heading">Reset password</h1>` and `<p class="pt-auth-subheading">Reset your password.</p>`. Replace Bootstrap `form-control`/`text-danger`/`btn-primary` classes with `pt-auth-input`/`pt-auth-field-error`/`pt-auth-button`. Do not change `OnValidSubmitAsync`, `FormName="reset-password"`, the `Code` query-parameter handling, or `UserManager.ResetPasswordAsync`.
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 2.5, 3.5, 4.1, 5.1, 5.2, 5.7_

  - [x] 5.3 Migrate `ClinicApp/Components/Account/Pages/ResendEmailConfirmation.razor`
    - Add `@layout AuthLayout`. Replace `<h1>Resend email confirmation</h1>`/`<h2>Enter your email.</h2>` with `<h1 class="pt-auth-heading">Resend email confirmation</h1>` and `<p class="pt-auth-subheading">Enter your email.</p>`. Replace Bootstrap `form-control`/`text-danger`/`btn-primary` classes with `pt-auth-input`/`pt-auth-field-error`/`pt-auth-button`. Do not change `OnValidSubmitAsync`, `FormName="resend-email-confirmation"`, or `GenerateEmailConfirmationTokenAsync`/`SendConfirmationLinkAsync` calls.
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 2.5, 3.5, 4.1, 5.1, 5.2, 5.7_

  - [x]* 5.4 Write unit tests for the password-reset/email-confirmation flow migrations
    - New file `ClinicApp.Tests/PasswordResetFlowIdentityRegressionTests.cs`.
    - Assert each of the 3 files contains `@layout AuthLayout` and its exact `FormName` literal (`"forgot-password"`, `"reset-password"`, `"resend-email-confirmation"`).
    - Assert `ResetPassword.razor` still contains `UserManager.ResetPasswordAsync` and `Account/InvalidPasswordReset`/`Account/ResetPasswordConfirmation` redirect targets.
    - Assert `ForgotPassword.razor` still contains `GeneratePasswordResetTokenAsync` and `Account/ForgotPasswordConfirmation`.
    - Assert none of the 3 files contain `form-control`, `btn-primary`, or `text-danger`.
    - _Requirements: 2.4, 5.1, 5.2, 5.7_

- [x] 6. Migrate 2FA, recovery-code, phone-OTP, and external-login pages (Group D)
  - [x] 6.1 Migrate `ClinicApp/Components/Account/Pages/LoginWith2fa.razor`
    - Add `@layout AuthLayout`. Change `<h1>Two-factor authentication</h1>` to `<h1 class="pt-auth-heading">Two-factor authentication</h1>`. Replace `form-control`/`text-danger`/`btn btn-lg btn-primary` classes with `pt-auth-input`/`pt-auth-field-error`/`pt-auth-button`; the `Don't have access...` link becomes `pt-auth-link`. Do not change `OnValidSubmitAsync`, `FormName="login-with-2fa"`, `SignInManager.GetTwoFactorAuthenticationUserAsync`, or `SignInManager.TwoFactorAuthenticatorSignInAsync`.
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 2.5, 3.5, 4.1, 5.4, 5.7_

  - [x] 6.2 Migrate `ClinicApp/Components/Account/Pages/LoginWithRecoveryCode.razor`
    - Add `@layout AuthLayout`. Change `<h1>Recovery code verification</h1>` to `<h1 class="pt-auth-heading">Recovery code verification</h1>`. Replace `form-control`/`text-danger`/`btn btn-lg btn-primary` classes with `pt-auth-input`/`pt-auth-field-error`/`pt-auth-button`. Do not change `OnValidSubmitAsync`, `FormName="login-with-recovery-code"`, or `SignInManager.TwoFactorRecoveryCodeSignInAsync`.
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 2.5, 3.5, 4.1, 5.4, 5.7_

  - [x] 6.3 Migrate `ClinicApp/Components/Account/Pages/VerifyPhone.razor`
    - Add `@layout AuthLayout`. Correct the heading element from `<h3>Verify your phone number</h3>` to `<h1 class="pt-auth-heading">Verify your phone number</h1>` (Requirement 4.2 heading-level fix). Replace `form-control`/`text-danger`/`btn btn-primary`/`btn btn-secondary` classes with `pt-auth-input`/`pt-auth-field-error`/`pt-auth-button`. Do not change `VerifyOtp`, `ResendOtp`, `ClinicService.VerifyPhoneOtpAsync`, `ClinicService.GeneratePhoneOtpAsync`, or `SignInManager.SignInAsync`.
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 2.5, 3.5, 4.1, 4.2, 5.5, 5.7_

  - [x] 6.4 Migrate `ClinicApp/Components/Account/Pages/ExternalLogin.razor`
    - Add `@layout AuthLayout`. Consolidate the two headings (`<h1>Register</h1>` + `<h2>Associate your @ProviderDisplayName account.</h2>`) into a single `<h1 class="pt-auth-heading">Register</h1>`, demoting the provider-association text to `<p class="pt-auth-subheading">Associate your @ProviderDisplayName account.</p>` (Requirement 4.1 single-`h1` rule). Change `<div class="alert alert-info">...</div>` to carry `class="pt-auth-alert pt-auth-alert--info"`. Replace `form-control`/`text-danger`/`btn btn-lg btn-primary` classes with `pt-auth-input`/`pt-auth-field-error`/`pt-auth-button`. Do not change `OnInitializedAsync`, `OnLoginCallbackAsync`, `OnValidSubmitAsync`, `FormName="confirmation"`, `SignInManager.GetExternalLoginInfoAsync`, `SignInManager.ExternalLoginSignInAsync`, or any redirect target.
    - _Requirements: 1.1, 1.2, 1.3, 2.4, 2.5, 3.3, 4.1, 5.6, 5.7_

  - [x]* 6.5 Write unit tests for the 2FA/recovery/OTP/external-login migrations
    - New file `ClinicApp.Tests/TwoFactorRecoveryOtpExternalLoginIdentityRegressionTests.cs`.
    - Assert each of the 4 files contains `@layout AuthLayout` and its exact `FormName` literal (`"login-with-2fa"`, `"login-with-recovery-code"`, `"confirmation"`) where applicable.
    - Assert `LoginWith2fa.razor` still contains `TwoFactorAuthenticatorSignInAsync`; `LoginWithRecoveryCode.razor` still contains `TwoFactorRecoveryCodeSignInAsync`; `VerifyPhone.razor` still contains `VerifyPhoneOtpAsync` and `GeneratePhoneOtpAsync`; `ExternalLogin.razor` still contains `ExternalLoginSignInAsync` and `GetExternalLoginInfoAsync`.
    - Assert `VerifyPhone.razor` contains exactly one `<h1` and zero `<h3` (Requirement 4.2 regression guard).
    - Assert `ExternalLogin.razor` contains exactly one `<h1` and zero `<h2` used as a page heading.
    - _Requirements: 4.1, 4.2, 5.4, 5.5, 5.6, 5.7_

- [x] 7. Migrate the shared `StatusMessage` and `ExternalLoginPicker` components
  - [x] 7.1 Migrate `ClinicApp/Components/Account/Shared/StatusMessage.razor`
    - Change `<div class="alert alert-@statusMessageClass" role="alert">` to `<div class="pt-auth-alert pt-auth-alert--@statusMessageClass" role="alert">`. Do not change the `statusMessageClass` computation (`DisplayMessage.StartsWith("Error") ? "danger" : "success"` may be relabeled to `"error"`/`"success"` to match the `pt-auth-alert--error`/`pt-auth-alert--success` modifier names, but the branch condition itself — `StartsWith("Error")` — must remain unchanged), the `Message` parameter, or the `OnInitialized` cookie read/delete logic against `IdentityRedirectManager.StatusCookieName`.
    - _Requirements: 3.1, 3.2, 3.4, 5.3_

  - [x] 7.2 Migrate `ClinicApp/Components/Account/Shared/ExternalLoginPicker.razor`
    - Change `<button type="submit" class="btn btn-primary" ...>` to `<button type="submit" class="pt-auth-button" ...>`. Change the informational `<p>There are no external authentication services configured...</p>` to carry `class="pt-auth-alert pt-auth-alert--info"`. Do not change `OnInitializedAsync`, `SignInManager.GetExternalAuthenticationSchemesAsync`, the `action="Account/PerformExternalLogin"` form target, or the `AntiforgeryToken`/`ReturnUrl` hidden-field wiring.
    - _Requirements: 2.4, 3.3, 5.6_

  - [x]* 7.3 Write FsCheck property test for Property 4 (Status Alert Variant Correctness)
    - New file `ClinicApp.Tests/StatusMessagePropertyTests.cs`, using `FsCheck.Xunit` (already referenced in `ClinicApp.Tests.csproj`).
    - Generate arbitrary non-null `string` values for `DisplayMessage` (via a component test harness or by exercising the equivalent branch logic directly against `StatusMessage`'s computation), and assert the rendered/derived alert modifier is `pt-auth-alert--error` if and only if the string starts with `"Error"`, and `pt-auth-alert--success` otherwise. Configure a minimum of 100 iterations.
    - Tag the test with a comment: `// Feature: account-status-pages-consistency, Property 4: Status Alert Variant Correctness`.
    - _Requirements: 3.1, 3.2_

  - [x]* 7.4 Write unit test for `ExternalLoginPicker.razor` migration
    - New file `ClinicApp.Tests/ExternalLoginPickerRegressionTests.cs`.
    - Assert the file contains `pt-auth-button` and `pt-auth-alert pt-auth-alert--info`, and does not contain `btn btn-primary`.
    - Assert the file still contains `GetExternalAuthenticationSchemesAsync` and `action="Account/PerformExternalLogin"`.
    - _Requirements: 2.4, 3.3, 5.6_

- [x] 8. Checkpoint - ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Write comprehensive structural correctness-property tests across the full 18-page `Anonymous_Account_Page` set
  - [x]* 9.1 Write Theory-based test for Property 1 (Canonical Layout Declaration)
    - New file `ClinicApp.Tests/Property1LayoutDeclarationTests.cs`. Use a `[Theory]`/`[MemberData]` list enumerating all 18 in-scope page file paths. For each, assert the file contains the literal directive `@layout AuthLayout` and does not contain any other `@layout` directive (e.g. `@layout PublicLayout`).
    - **Property 1: Canonical Layout Declaration** — **Validates: Requirements 1.1, 1.3, 1.4, 6.1**

  - [x]* 9.2 Write Theory-based test for Property 2 (Legacy Class Family Elimination)
    - New file `ClinicApp.Tests/Property2LegacyClassEliminationTests.cs`. Use a `[Theory]`/`[MemberData]` list enumerating all 18 in-scope page file paths. For each, assert the file's source contains no CSS class token beginning with `login-` or `auth-` (other than the canonical `pt-auth-` prefix).
    - **Property 2: Legacy Class Family Elimination** — **Validates: Requirements 2.2, 2.3, 2.4**

  - [x]* 9.3 Write Theory-based test for Property 3 (Canonical Card Container Presence)
    - New file `ClinicApp.Tests/Property3CardContainerTests.cs`. Assert `AuthLayout.razor` wraps `@Body` inside an element carrying `pt-auth-card`. Use a `[Theory]`/`[MemberData]` list enumerating all 18 in-scope page file paths asserting each declares `@layout AuthLayout` (which, combined with the `AuthLayout` assertion, establishes every page's content is nested inside `pt-auth-card`).
    - **Property 3: Canonical Card Container Presence** — **Validates: Requirements 2.5**

  - [x]* 9.4 Write Theory-based test for Property 5 (Inline Alert Canonical Styling)
    - New file `ClinicApp.Tests/Property5InlineAlertTests.cs`. Use a `[Theory]`/`[MemberData]` list enumerating `ExternalLogin.razor`, `RegisterConfirmation.razor`, `ForgotPasswordConfirmation.razor`, `ResetPasswordConfirmation.razor`, `InvalidPasswordReset.razor`, `Lockout.razor`, and `AccessDenied.razor`. For each, assert the file contains `pt-auth-alert` with an appropriate `--success`/`--error`/`--info` modifier and does not contain `alert-info`, `alert-danger`, `alert-success`, or `text-danger`.
    - **Property 5: Inline Alert Canonical Styling** — **Validates: Requirements 3.3**

  - [x]* 9.5 Write Theory-based test for Property 6 (Canonical Field-Error Styling)
    - New file `ClinicApp.Tests/Property6FieldErrorTests.cs`. Use a `[Theory]`/`[MemberData]` list enumerating every in-scope page that renders a `ValidationMessage`/`ValidationSummary` (`Login`, `Register`, `ForgotPassword`, `ResetPassword`, `LoginWith2fa`, `LoginWithRecoveryCode`, `VerifyPhone`, `ResendEmailConfirmation`, `ExternalLogin`). For each, assert the file contains `pt-auth-field-error` and does not contain `login-field-error`, `auth-field-error`, or `text-danger`.
    - **Property 6: Canonical Field-Error Styling** — **Validates: Requirements 3.5**

  - [x]* 9.6 Write Theory-based test for Property 7 (Single H1 Per Page)
    - New file `ClinicApp.Tests/Property7SingleH1Tests.cs`. Use a `[Theory]`/`[MemberData]` list enumerating all 18 in-scope page file paths that render a top-level heading. For each, assert the file contains exactly one `<h1` occurrence and zero `<h2`/`<h3` occurrences used as a page-level heading (this specifically guards `InvalidUser.razor` and `VerifyPhone.razor` against regressing back to `<h3>`, and `ExternalLogin.razor` against regressing back to a second `<h1>`/`<h2>`).
    - **Property 7: Single H1 Per Page** — **Validates: Requirements 4.1, 4.2**

  - [x]* 9.7 Write Theory-based test for Property 8 (Canonical Logo Asset Reference)
    - New file `ClinicApp.Tests/Property8LogoAssetTests.cs`. Use a `[Theory]`/`[MemberData]` list enumerating `AuthLayout.razor` plus all 18 in-scope page file paths. For each file that renders a logo `<img>`, assert its `src` references `/images/protouch-logo.svg`, and assert no file in the set references `/images/WhatsApp Image 2026-08-12 at 12.25.21 AM.jpeg` or its URL-encoded form (`WhatsApp%20Image...`).
    - **Property 8: Canonical Logo Asset Reference** — **Validates: Requirements 4.3, 4.5**

  - [x]* 9.8 Write Theory-based test for Property 9 (Bounded Logo Cardinality)
    - New file `ClinicApp.Tests/Property9LogoCardinalityTests.cs`. Use a `[Theory]`/`[MemberData]` list enumerating all 18 in-scope page file paths. For each, count occurrences of `/images/protouch-logo.svg`; assert the count is exactly 1 for every page except `Login.razor`, which must be exactly 2 (accounting for the shell's single render plus each page's own page-local `<img>` references, per the documented Requirement 4.4 exception).
    - **Property 9: Bounded Logo Cardinality** — **Validates: Requirements 4.4**

- [x] 10. Verify out-of-scope file isolation and run full-solution verification
  - [x]* 10.1 Write unit test for Property 10 (Out-of-Scope File Isolation)
    - New file `ClinicApp.Tests/OutOfScopeIsolationTests.cs`. Use a `[Theory]`/`[MemberData]` list enumerating `MainLayout.razor`, `NavMenu.razor`, `ManageLayout.razor`, every file under `ClinicApp/Components/Account/Pages/Manage/`, `AdminDashboard.razor`, `ClientDashboard.razor`, `Home.razor`, `ServiceDetail.razor`, `ServiceBooking.razor`, and `PublicLayout.razor`. For each, assert the file's content does not contain `AuthLayout` or `pt-auth-`.
    - **Property 10: Out-of-Scope File Isolation** — **Validates: Requirements 6.4, 6.5**

  - [x] 10.2 Run `git diff --stat` against the pre-feature base and confirm only in-scope files changed
    - Run `git diff --stat` (or `git diff --stat main` if working on a feature branch) from the repository root and confirm the listed changed files are limited to: `ClinicApp/Components/Layout/AuthLayout.razor` (new), `ClinicApp/wwwroot/app.css`, the 18 `Anonymous_Account_Page` `.razor` files listed in Requirement 6.1, `ClinicApp/Components/Account/Shared/StatusMessage.razor`, `ClinicApp/Components/Account/Shared/ExternalLoginPicker.razor`, and the new files under `ClinicApp.Tests/`. If any file outside this list appears in the diff, revert that file's changes before proceeding.
    - _Requirements: 6.4, 6.5_

  - [x] 10.3 Run `dotnet build` across the solution
    - Run `dotnet build` from the repository root (or against the `.sln`) and confirm the build succeeds with no new errors or warnings introduced by this feature's changes.
    - _Requirements: 5.7_

  - [x] 10.4 Run `dotnet test` across `ClinicApp.Tests`
    - Run `dotnet test` against `ClinicApp.Tests` and confirm every test — pre-existing and newly added — passes, including the FsCheck property test from task 7.3 run at its configured minimum of 100 iterations.
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7_

- [x] 11. Final checkpoint - ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP; core implementation tasks (layout, CSS, and page migrations) are never marked optional.
- Every page-migration task explicitly calls out which `@code`-block members must remain unchanged, per Requirement 5.7.
- Group B (task 3), Group C (task 5), and Group D (task 6) each touch a disjoint set of `.razor` files and can be executed in parallel once tasks 1.1 and 1.2 are complete.
- The comprehensive structural-property tests (task 9) are written as `[Theory]`/`[MemberData]` tests enumerating the full 18-page set — per the design's Testing Strategy — rather than duplicating per-page assertions already covered by the per-group regression tests in tasks 2.3, 3.10, 5.4, and 6.5.
- Property 4 is the only property in this feature tested via FsCheck-driven property-based testing (task 7.3, minimum 100 iterations); every other property is a static, finite-file-set assertion (xUnit `[Theory]`/example tests).
- No GitHub branch push or pull request creation is included in this task list; implementation stops at a verified, locally-passing state.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "10.1"] },
    { "id": 1, "tasks": ["1.3", "2.1", "2.2", "3.1", "3.2", "3.3", "3.4", "3.5", "3.6", "3.7", "3.8", "3.9", "5.1", "5.2", "5.3", "6.1", "6.2", "6.3", "6.4", "7.1", "7.2"] },
    { "id": 2, "tasks": ["2.3", "3.10", "5.4", "6.5", "7.3", "7.4", "9.1", "9.2", "9.3", "9.4", "9.5", "9.6", "9.7", "9.8"] },
    { "id": 3, "tasks": ["10.2"] },
    { "id": 4, "tasks": ["10.3"] },
    { "id": 5, "tasks": ["10.4"] }
  ]
}
```
