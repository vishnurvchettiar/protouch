using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Regression tests for the password-reset / email-confirmation flow pages
/// (ForgotPassword.razor, ResetPassword.razor, ResendEmailConfirmation.razor)
/// migrated onto AuthLayout and the pt-auth-* canonical CSS class family.
/// Ensures the layout migration preserves existing Identity behavior.
/// Validates: Requirements 2.4, 5.1, 5.2, 5.7
/// </summary>
public class PasswordResetFlowIdentityRegressionTests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));

    private static readonly string AccountPagesDir =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Account/Pages");

    private static readonly string ForgotPasswordPath =
        Path.Combine(AccountPagesDir, "ForgotPassword.razor");

    private static readonly string ResetPasswordPath =
        Path.Combine(AccountPagesDir, "ResetPassword.razor");

    private static readonly string ResendEmailConfirmationPath =
        Path.Combine(AccountPagesDir, "ResendEmailConfirmation.razor");

    public static IEnumerable<object[]> PagesWithExpectedFormName()
    {
        yield return new object[] { ForgotPasswordPath, "forgot-password" };
        yield return new object[] { ResetPasswordPath, "reset-password" };
        yield return new object[] { ResendEmailConfirmationPath, "resend-email-confirmation" };
    }

    public static IEnumerable<object[]> AllPages()
    {
        yield return new object[] { ForgotPasswordPath };
        yield return new object[] { ResetPasswordPath };
        yield return new object[] { ResendEmailConfirmationPath };
    }

    // Requirements 1.1, 5.1 — each page declares @layout AuthLayout and preserves its FormName literal
    [Theory]
    [MemberData(nameof(PagesWithExpectedFormName))]
    public void Page_ContainsAuthLayoutAndExpectedFormName(string path, string expectedFormName)
    {
        var content = File.ReadAllText(path);

        Assert.Contains("@layout AuthLayout", content);
        Assert.Contains($"FormName=\"{expectedFormName}\"", content);
    }

    // Requirement 5.2, 5.7 — ResetPassword.razor preserves ResetPasswordAsync call and redirect targets
    [Fact]
    public void ResetPassword_PreservesIdentityBehavior()
    {
        var content = File.ReadAllText(ResetPasswordPath);

        Assert.Contains("UserManager.ResetPasswordAsync", content);
        Assert.Contains("Account/InvalidPasswordReset", content);
        Assert.Contains("Account/ResetPasswordConfirmation", content);
    }

    // Requirement 5.2, 5.7 — ForgotPassword.razor preserves token generation call and redirect target
    [Fact]
    public void ForgotPassword_PreservesIdentityBehavior()
    {
        var content = File.ReadAllText(ForgotPasswordPath);

        Assert.Contains("GeneratePasswordResetTokenAsync", content);
        Assert.Contains("Account/ForgotPasswordConfirmation", content);
    }

    // Requirement 2.4 — legacy Bootstrap classes must be fully retired from these 3 pages
    [Theory]
    [MemberData(nameof(AllPages))]
    public void Page_DoesNotContainLegacyBootstrapClasses(string path)
    {
        var content = File.ReadAllText(path);

        Assert.DoesNotContain("form-control", content);
        Assert.DoesNotContain("btn-primary", content);
        Assert.DoesNotContain("text-danger", content);
    }
}
