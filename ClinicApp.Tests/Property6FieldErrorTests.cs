using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Property 6: Canonical Field-Error Styling.
/// For any page in the Anonymous_Account_Page set that renders a ValidationMessage or
/// ValidationSummary, that element SHALL carry the pt-auth-field-error class and SHALL NOT
/// carry login-field-error, auth-field-error, or text-danger.
/// Validates: Requirements 3.5
/// </summary>
public class Property6FieldErrorTests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));

    private static readonly string PagesRoot =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Account/Pages");

    // Legacy class tokens must be checked as standalone tokens, not raw substrings —
    // "auth-field-error" is itself a substring of the canonical "pt-auth-field-error",
    // so a plain Assert.DoesNotContain would always fail. The negative lookbehind
    // excludes matches that are actually part of the "pt-auth-field-error" token.
    private static readonly Regex LegacyAuthFieldErrorRegex = new(
        "(?<!pt-)auth-field-error",
        RegexOptions.Compiled);

    private static readonly Regex LegacyLoginFieldErrorRegex = new(
        "login-field-error",
        RegexOptions.Compiled);

    public static IEnumerable<object[]> FieldErrorPages()
    {
        yield return new object[] { "Login.razor" };
        yield return new object[] { "Register.razor" };
        yield return new object[] { "ForgotPassword.razor" };
        yield return new object[] { "ResetPassword.razor" };
        yield return new object[] { "LoginWith2fa.razor" };
        yield return new object[] { "LoginWithRecoveryCode.razor" };
        yield return new object[] { "VerifyPhone.razor" };
        yield return new object[] { "ResendEmailConfirmation.razor" };
        yield return new object[] { "ExternalLogin.razor" };
    }

    [Theory]
    [MemberData(nameof(FieldErrorPages))]
    public void Page_UsesCanonicalFieldErrorStyling(string fileName)
    {
        var content = File.ReadAllText(Path.Combine(PagesRoot, fileName));

        Assert.Contains("pt-auth-field-error", content);

        Assert.False(
            LegacyLoginFieldErrorRegex.IsMatch(content),
            $"{fileName} contains legacy class 'login-field-error'");
        Assert.False(
            LegacyAuthFieldErrorRegex.IsMatch(content),
            $"{fileName} contains legacy class 'auth-field-error'");
        Assert.DoesNotContain("text-danger", content);
    }
}
