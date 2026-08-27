using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Structural property test verifying that every in-scope Anonymous_Account_Page
/// is rendered nested inside AuthLayout's "pt-auth-card" container.
/// Feature: account-status-pages-consistency, Property 3: Canonical Card Container Presence
/// Validates: Requirements 2.5
/// </summary>
public class Property3CardContainerTests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));

    private static readonly string AuthLayoutPath =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Layout/AuthLayout.razor");

    private static readonly string PagesDirectory =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Account/Pages");

    public static IEnumerable<object[]> InScopePageFiles()
    {
        yield return new object[] { "Login.razor" };
        yield return new object[] { "Register.razor" };
        yield return new object[] { "ConfirmEmail.razor" };
        yield return new object[] { "ConfirmEmailChange.razor" };
        yield return new object[] { "RegisterConfirmation.razor" };
        yield return new object[] { "ForgotPassword.razor" };
        yield return new object[] { "ForgotPasswordConfirmation.razor" };
        yield return new object[] { "ResetPassword.razor" };
        yield return new object[] { "ResetPasswordConfirmation.razor" };
        yield return new object[] { "InvalidPasswordReset.razor" };
        yield return new object[] { "InvalidUser.razor" };
        yield return new object[] { "Lockout.razor" };
        yield return new object[] { "AccessDenied.razor" };
        yield return new object[] { "LoginWith2fa.razor" };
        yield return new object[] { "LoginWithRecoveryCode.razor" };
        yield return new object[] { "VerifyPhone.razor" };
        yield return new object[] { "ResendEmailConfirmation.razor" };
        yield return new object[] { "ExternalLogin.razor" };
    }

    // Assert AuthLayout.razor wraps @Body inside an element carrying "pt-auth-card"
    [Fact]
    public void AuthLayout_WrapsBody_InsidePtAuthCard()
    {
        var content = File.ReadAllText(AuthLayoutPath);

        Assert.Contains("class=\"pt-auth-card\"", content);
        Assert.Contains("@Body", content);

        var cardIndex = content.IndexOf("pt-auth-card");
        var bodyIndex = content.IndexOf("@Body");

        Assert.True(cardIndex >= 0, "AuthLayout.razor must contain 'pt-auth-card'.");
        Assert.True(bodyIndex >= 0, "AuthLayout.razor must contain '@Body'.");
        Assert.True(cardIndex < bodyIndex,
            "The 'pt-auth-card' element must open before '@Body' so that @Body is nested inside it.");
    }

    // Combined with the AuthLayout assertion above, establishing every page declares
    // @layout AuthLayout establishes that every page's content is nested inside pt-auth-card.
    [Theory]
    [MemberData(nameof(InScopePageFiles))]
    public void Page_Declares_AuthLayout(string fileName)
    {
        var path = Path.Combine(PagesDirectory, fileName);
        var content = File.ReadAllText(path);

        Assert.Contains("@layout AuthLayout", content);
    }
}
