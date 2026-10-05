using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Structural correctness-property test verifying that the Canonical_Logo_Asset
/// (/images/WhatsApp Image 2026-08-12 at 12.25.21 AM.jpeg) is referenced at the page-content level a bounded,
/// exact number of times for every in-scope Anonymous_Account_Page: zero times for
/// the 17 pages whose only logo rendering comes from the AuthLayout shell, and
/// exactly twice for Login.razor, which additionally renders the logo in its own
/// page-local form panel and decorative visual panel (the permitted secondary
/// decorative use documented in Requirement 4.4).
///
/// Feature: account-status-pages-consistency, Property 9: Bounded Logo Cardinality
/// Validates: Requirements 4.4
/// </summary>
public class Property9LogoCardinalityTests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));

    private static readonly string PagesRoot =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Account/Pages");

    private const string CanonicalLogoPath = "/images/WhatsApp Image 2026-08-12 at 12.25.21 AM.jpeg";

    public static TheoryData<string, int> InScopePagesWithExpectedLogoCount => new()
    {
        { "Login.razor", 2 },
        { "Register.razor", 0 },
        { "ConfirmEmail.razor", 0 },
        { "ConfirmEmailChange.razor", 0 },
        { "RegisterConfirmation.razor", 0 },
        { "ForgotPassword.razor", 0 },
        { "ForgotPasswordConfirmation.razor", 0 },
        { "ResetPassword.razor", 0 },
        { "ResetPasswordConfirmation.razor", 0 },
        { "InvalidPasswordReset.razor", 0 },
        { "InvalidUser.razor", 0 },
        { "Lockout.razor", 0 },
        { "AccessDenied.razor", 0 },
        { "LoginWith2fa.razor", 0 },
        { "LoginWithRecoveryCode.razor", 0 },
        { "VerifyPhone.razor", 0 },
        { "ResendEmailConfirmation.razor", 0 },
        { "ExternalLogin.razor", 0 },
    };

    [Theory]
    [MemberData(nameof(InScopePagesWithExpectedLogoCount))]
    public void Page_ReferencesCanonicalLogo_ExactlyExpectedNumberOfTimes(string relativePath, int expectedCount)
    {
        var fullPath = Path.Combine(PagesRoot, relativePath);
        var content = File.ReadAllText(fullPath);

        var occurrences = Regex.Matches(content, Regex.Escape(CanonicalLogoPath)).Count;

        Assert.Equal(expectedCount, occurrences);
    }
}
