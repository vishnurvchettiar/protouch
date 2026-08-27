using System.IO;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Regression tests verifying that the 9 previously-bare confirmation/error pages
/// preserve their original Identity-related behavior (redirects, status codes,
/// Identity API calls) after being migrated onto AuthLayout.
///
/// Feature: account-status-pages-consistency
/// Validates: Requirements 5.2, 5.6, 5.7
/// </summary>
public class BarePagesIdentityRegressionTests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));

    private static readonly string PagesRoot =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Account/Pages");

    [Fact]
    public void ConfirmEmail_PreservesIdentityBehavior()
    {
        var content = File.ReadAllText(Path.Combine(PagesRoot, "ConfirmEmail.razor"));
        Assert.Contains("StatusCodes.Status404NotFound", content);
        Assert.Contains("UserManager.ConfirmEmailAsync", content);
    }

    [Fact]
    public void ConfirmEmailChange_PreservesIdentityBehavior()
    {
        var content = File.ReadAllText(Path.Combine(PagesRoot, "ConfirmEmailChange.razor"));
        Assert.Contains("UserManager.ChangeEmailAsync", content);
        Assert.Contains("SignInManager.RefreshSignInAsync", content);
    }

    [Fact]
    public void RegisterConfirmation_PreservesIdentityBehavior()
    {
        var content = File.ReadAllText(Path.Combine(PagesRoot, "RegisterConfirmation.razor"));
        Assert.Contains("StatusCodes.Status404NotFound", content);
        Assert.Contains("GenerateEmailConfirmationTokenAsync", content);
    }

    public static TheoryData<string> BarePageFileNames => new()
    {
        "ConfirmEmail.razor",
        "ConfirmEmailChange.razor",
        "RegisterConfirmation.razor",
        "ForgotPasswordConfirmation.razor",
        "ResetPasswordConfirmation.razor",
        "InvalidPasswordReset.razor",
        "InvalidUser.razor",
        "Lockout.razor",
        "AccessDenied.razor",
    };

    [Theory]
    [MemberData(nameof(BarePageFileNames))]
    public void BarePage_ContainsAuthLayoutDirective(string fileName)
    {
        var content = File.ReadAllText(Path.Combine(PagesRoot, fileName));
        Assert.Contains("@layout AuthLayout", content);
    }
}
