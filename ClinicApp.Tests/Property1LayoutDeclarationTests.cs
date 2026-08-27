using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Structural correctness-property test verifying that every in-scope
/// Anonymous_Account_Page declares exactly one @layout directive, and that
/// directive is the literal "@layout AuthLayout".
///
/// Feature: account-status-pages-consistency, Property 1: Canonical Layout Declaration
/// Validates: Requirements 1.1, 1.3, 1.4, 6.1
/// </summary>
public class Property1LayoutDeclarationTests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));

    private static readonly string PagesRoot =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Account/Pages");

    // Matches any "@layout <Name>" directive occurrence in the source.
    private static readonly Regex LayoutDirectiveRegex = new(@"@layout\s+(\S+)", RegexOptions.Compiled);

    public static TheoryData<string> InScopePageRelativePaths => new()
    {
        "Login.razor",
        "Register.razor",
        "ConfirmEmail.razor",
        "ConfirmEmailChange.razor",
        "RegisterConfirmation.razor",
        "ForgotPassword.razor",
        "ForgotPasswordConfirmation.razor",
        "ResetPassword.razor",
        "ResetPasswordConfirmation.razor",
        "InvalidPasswordReset.razor",
        "InvalidUser.razor",
        "Lockout.razor",
        "AccessDenied.razor",
        "LoginWith2fa.razor",
        "LoginWithRecoveryCode.razor",
        "VerifyPhone.razor",
        "ResendEmailConfirmation.razor",
        "ExternalLogin.razor",
    };

    [Theory]
    [MemberData(nameof(InScopePageRelativePaths))]
    public void Page_ContainsCanonicalLayoutDirective_AndNoOtherLayoutDirective(string relativePath)
    {
        var fullPath = Path.Combine(PagesRoot, relativePath);
        var content = File.ReadAllText(fullPath);

        // Must contain the literal canonical directive.
        Assert.Contains("@layout AuthLayout", content);

        // Must not contain any other @layout directive (e.g. @layout PublicLayout).
        var matches = LayoutDirectiveRegex.Matches(content);
        var distinctLayouts = new HashSet<string>();
        foreach (Match match in matches)
        {
            distinctLayouts.Add(match.Groups[1].Value);
        }

        Assert.Equal(new[] { "AuthLayout" }, distinctLayouts);
    }
}
