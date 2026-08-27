# Requirements Document

## Introduction

The ClinicApp (ProTouch Physio & Slimming Clinic, Blazor Server) exposes a set of anonymous-flow account pages under `ClinicApp/Components/Account/Pages/` (login, registration, password reset, email confirmation, lockout, access denied, two-factor/recovery-code verification, phone verification, external login, and related confirmation pages), plus a shared `StatusMessage` component and a `PublicLayout` component.

Direct inspection of the repository confirms two problems:

1. **Structural layout misuse**: Several anonymous-flow pages (`ConfirmEmail`, `ConfirmEmailChange`, `RegisterConfirmation`, `ForgotPasswordConfirmation`, `ResetPasswordConfirmation`, `InvalidPasswordReset`, `InvalidUser`, `Lockout`, `AccessDenied`) declare no `@layout` directive. Because `Routes.razor` sets `DefaultLayout="typeof(Layout.MainLayout)"`, these anonymous pages currently render inside `MainLayout` — the authenticated dashboard shell (sidebar + topbar) — even though the visitor is never authenticated when reaching them.
2. **Design fragmentation**: `Login.razor` and `Register.razor` both declare `@layout PublicLayout` and both carry deliberate custom styling, but they use two entirely unrelated CSS class systems (`login-*` vs `auth-*`), two different logo image assets, and no shared visual language. The remaining anonymous pages listed above have no styling beyond bare Bootstrap defaults and no clinic branding at all.

This feature unifies all anonymous account-flow pages, the shared `StatusMessage` component, and `PublicLayout` around a single, consistent, ProTouch-branded visual design — while explicitly preserving all existing ASP.NET Core Identity behavior (redirects, form bindings, cookie-based status messages, two-factor/recovery flows, and external login flow).

## Glossary

- **Public_Auth_Layout**: The single shared layout component (currently `PublicLayout.razor`, or its replacement) that wraps every page in the Anonymous_Account_Page set and renders the Canonical_Auth_Shell instead of the authenticated dashboard shell (`MainLayout`).
- **Canonical_Auth_Shell**: The single, ProTouch-branded visual wrapper (logo, card/panel container, background treatment) rendered by the Public_Auth_Layout around the content of every Anonymous_Account_Page.
- **Anonymous_Account_Page**: Any of the following Razor pages, all reachable by an unauthenticated visitor: `Login`, `Register`, `ConfirmEmail`, `ConfirmEmailChange`, `RegisterConfirmation`, `ForgotPassword`, `ForgotPasswordConfirmation`, `ResetPassword`, `ResetPasswordConfirmation`, `InvalidPasswordReset`, `InvalidUser`, `Lockout`, `AccessDenied`, `LoginWith2fa`, `LoginWithRecoveryCode`, `VerifyPhone`, `ResendEmailConfirmation`, `ExternalLogin`.
- **Auth_UI_Library**: The single, shared set of canonical presentational building blocks (logo element, page heading, card/panel container, text input, primary button, alert/status banner, field validation message, footer link) used by every Anonymous_Account_Page in place of page-specific, one-off class systems (the existing `login-*` and `auth-*` class families).
- **StatusMessage_Component**: The shared `StatusMessage.razor` component that renders success/error banners sourced from either an explicit `Message` parameter or the status cookie set by `IdentityRedirectManager`.
- **Canonical_Alert_Style**: The single visual style (color, iconography, spacing) applied to every success, error, and informational banner rendered by the StatusMessage_Component or by inline alert markup on any Anonymous_Account_Page.
- **Canonical_Logo_Asset**: The single logo image file designated as the one clinic logo displayed across all Anonymous_Account_Page instances.
- **Identity_Behavior**: The existing functional behavior of the Anonymous_Account_Page set that is out of scope for change, including: redirect targets and query parameters produced by `IdentityRedirectManager`, `EditForm` model bindings and `FormName` values, cookie-based status message delivery, two-factor authentication and recovery-code sign-in flows, phone OTP verification flow, and the external login association flow.

## Requirements

### Requirement 1: Every Anonymous Account Page Renders Inside a Consistent Public Layout

**User Story:** As a clinic patient visiting a login, registration, confirmation, or error page while not signed in, I want every one of these pages to appear inside the same branded public page shell, so that I never see the internal staff dashboard chrome (sidebar/topbar) while completing an account-related task.

#### Acceptance Criteria

1. WHERE an Anonymous_Account_Page currently declares no `@layout` directive, THE Anonymous_Account_Page SHALL declare `@layout` referencing the Public_Auth_Layout.
2. WHEN an unauthenticated visitor navigates to any Anonymous_Account_Page, THE Public_Auth_Layout SHALL render the Canonical_Auth_Shell around that page's content.
3. WHEN an unauthenticated visitor navigates to any Anonymous_Account_Page, THE Anonymous_Account_Page SHALL NOT render inside `MainLayout`.
4. THE Public_Auth_Layout SHALL be the single layout component referenced by every Anonymous_Account_Page listed in the Glossary.
5. IF a future page is added under `ClinicApp/Components/Account/Pages/` without an explicit `@layout` directive, THEN THE Routes.razor default layout resolution SHALL remain `MainLayout`, and adding such a page without a Public_Auth_Layout declaration SHALL be treated as a defect relative to this requirement.

### Requirement 2: Single Canonical Set of Auth UI Primitives

**User Story:** As a clinic patient moving between the login page, the registration page, and any confirmation or error page, I want all of these pages to look like one cohesive product, so that I trust I am still interacting with ProTouch and not a different or broken site.

#### Acceptance Criteria

1. THE Auth_UI_Library SHALL define exactly one reusable presentational element for each of: logo, page heading, card/panel container, text input, primary submit button, alert/status banner, field validation message, and footer link.
2. WHEN `Login.razor` renders its form, THE `Login.razor` markup SHALL use the Auth_UI_Library elements in place of the `login-*` class family.
3. WHEN `Register.razor` renders its form, THE `Register.razor` markup SHALL use the Auth_UI_Library elements in place of the `auth-*` class family.
4. WHEN any other Anonymous_Account_Page renders a heading, form, input, button, or link, THE Anonymous_Account_Page SHALL use the corresponding Auth_UI_Library element.
5. THE Auth_UI_Library SHALL apply one consistent card/panel container treatment across every Anonymous_Account_Page, including pages that currently render bare, unstyled content.
6. IF two Anonymous_Account_Page instances render the same UI concept (for example, a primary submit button), THEN THE two instances SHALL reference the same Auth_UI_Library element rather than two independently defined styles.

### Requirement 3: Consistent Alert and Status Message Styling

**User Story:** As a clinic patient who sees a success or error message on any account page, I want that message to look and feel the same everywhere, so that I can quickly recognize whether my action succeeded or failed regardless of which page produced the message.

#### Acceptance Criteria

1. WHEN the StatusMessage_Component renders a message with `DisplayMessage` starting with `"Error"`, THE StatusMessage_Component SHALL apply the Canonical_Alert_Style error variant.
2. WHEN the StatusMessage_Component renders a message with `DisplayMessage` not starting with `"Error"`, THE StatusMessage_Component SHALL apply the Canonical_Alert_Style success variant.
3. WHEN an Anonymous_Account_Page renders an inline alert or validation summary outside the StatusMessage_Component (for example, the informational banner on `ExternalLogin.razor` or the `role="alert"` paragraphs on confirmation pages), THE Anonymous_Account_Page SHALL apply the Canonical_Alert_Style.
4. THE Canonical_Alert_Style SHALL be visually equivalent wherever it is applied, regardless of which Anonymous_Account_Page renders it.
5. WHEN `EditForm` validation errors are displayed via `ValidationSummary` or `ValidationMessage` on any Anonymous_Account_Page, THE Anonymous_Account_Page SHALL apply the Canonical_Alert_Style field-error variant in place of the existing `login-field-error`, `auth-field-error`, or plain `text-danger` classes.

### Requirement 4: Consistent Heading Hierarchy and Logo Usage

**User Story:** As a clinic patient and as an accessibility-tool user, I want every account page to present one clear top-level heading and the same clinic logo, so that I can orient myself on each page and the assistive technology I use can correctly identify the page's main heading.

#### Acceptance Criteria

1. WHERE an Anonymous_Account_Page renders a top-level page heading, THE Anonymous_Account_Page SHALL render exactly one `h1` element for that heading.
2. IF an Anonymous_Account_Page currently renders its top-level heading as an element other than `h1` (for example, `InvalidUser.razor` using `h3`), THEN THE Anonymous_Account_Page SHALL be corrected to render that heading as `h1`.
3. WHEN any Anonymous_Account_Page displays the clinic logo, THE Anonymous_Account_Page SHALL display the Canonical_Logo_Asset.
4. THE Canonical_Auth_Shell SHALL display the Canonical_Logo_Asset exactly once per rendered page unless the page's design calls for a secondary decorative use of the same asset.
5. THE Public_Auth_Layout and Auth_UI_Library SHALL use one designated Canonical_Logo_Asset file across all Anonymous_Account_Page instances, replacing the current use of two different logo files (`/images/WhatsApp Image 2026-08-12 at 12.25.21 AM.jpeg` on `Login.razor` and `/images/protouch-logo.svg` on `Register.razor`).

### Requirement 5: Preservation of Existing Identity Behavior

**User Story:** As a clinic administrator, I want the visual redesign of the account pages to introduce no functional regression, so that patients can still log in, register, reset passwords, verify their email or phone, and use two-factor authentication exactly as they could before the redesign.

#### Acceptance Criteria

1. WHEN a visitor submits any `EditForm` on an Anonymous_Account_Page, THE Anonymous_Account_Page SHALL bind and validate form data using the same `Input` model and `FormName` values as before this feature's implementation.
2. WHEN an Identity operation completes on any Anonymous_Account_Page, THE `IdentityRedirectManager` SHALL redirect to the same target route and query parameters as before this feature's implementation.
3. WHEN a status message is delivered via the status cookie set by `IdentityRedirectManager`, THE StatusMessage_Component SHALL continue to read and clear that cookie exactly as before this feature's implementation.
4. WHEN a user completes the two-factor authentication flow on `LoginWith2fa.razor` or the recovery-code flow on `LoginWithRecoveryCode.razor`, THE SignInManager-driven sign-in behavior SHALL remain unchanged.
5. WHEN a user completes the phone verification flow on `VerifyPhone.razor`, THE OTP generation, verification, and sign-in behavior SHALL remain unchanged.
6. WHEN a user completes the external login association flow on `ExternalLogin.razor`, THE external login sign-in, account creation, and email-confirmation-link generation behavior SHALL remain unchanged.
7. IF a redesigned Anonymous_Account_Page's `@code` block is modified to support the new markup, THEN THE modification SHALL NOT alter any existing method signature, redirect target, or Identity API call used by that page.

### Requirement 6: Complete Page-by-Page Inventory Coverage

**User Story:** As the developer implementing this redesign, I want an explicit, exhaustive inventory of every anonymous account-flow page in scope, so that no page is accidentally left unstyled or left rendering inside the wrong layout.

#### Acceptance Criteria

1. THE implementation SHALL apply Requirements 1 through 4 to every one of the following pages: `Login.razor`, `Register.razor`, `ConfirmEmail.razor`, `ConfirmEmailChange.razor`, `RegisterConfirmation.razor`, `ForgotPassword.razor`, `ForgotPasswordConfirmation.razor`, `ResetPassword.razor`, `ResetPasswordConfirmation.razor`, `InvalidPasswordReset.razor`, `InvalidUser.razor`, `Lockout.razor`, `AccessDenied.razor`, `LoginWith2fa.razor`, `LoginWithRecoveryCode.razor`, `VerifyPhone.razor`, `ResendEmailConfirmation.razor`, `ExternalLogin.razor`.
2. THE implementation SHALL apply Requirements 1 through 4 to the shared `ExternalLoginPicker.razor` component rendered on the login flow.
3. THE implementation SHALL apply Requirement 3 to `StatusMessage.razor`.
4. THE implementation SHALL apply Requirement 1 and Requirement 4 to `PublicLayout.razor`.
5. THE implementation SHALL leave `MainLayout.razor`, `NavMenu.razor`, `ManageLayout.razor`, all pages under `Components/Account/Pages/Manage/`, `AdminDashboard.razor`, `ClientDashboard.razor`, `Home.razor`, `ServiceDetail.razor`, and `ServiceBooking.razor` unmodified by this feature.
