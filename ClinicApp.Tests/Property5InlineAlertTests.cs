using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Property 5: Inline Alert Canonical Styling.
/// For any page in the Anonymous_Account_Page set that renders an inline alert or
/// informational banner outside of StatusMessage, that alert element SHALL carry a
/// pt-auth-alert class (with the appropriate --success/--error/--info modifier) and
/// SHALL NOT carry alert-info, alert-danger, alert-success, or text-danger.
/// Validates: Requirements 3.3
/// </summary>
public class Property5InlineAlertTests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));

    private static readonly string PagesRoot =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Account/Pages");

    public static IEnumerable<object[]> InlineAlertPages()
    {
        yield return new object[] { "ExternalLogin.razor", "pt-auth-alert--info" };
        yield return new object[] { "RegisterConfirmation.razor", "pt-auth-alert--info" };
        yield return new object[] { "ForgotPasswordConfirmation.razor", "pt-auth-alert--success" };
        yield return new object[] { "ResetPasswordConfirmation.razor", "pt-auth-alert--success" };
        yield return new object[] { "InvalidPasswordReset.razor", "pt-auth-alert--error" };
        yield return new object[] { "Lockout.razor", "pt-auth-alert--error" };
        yield return new object[] { "AccessDenied.razor", "pt-auth-alert--error" };
    }

    [Theory]
    [MemberData(nameof(InlineAlertPages))]
    public void Page_UsesCanonicalInlineAlertStyling(string fileName, string expectedModifierClass)
    {
        var content = File.ReadAllText(Path.Combine(PagesRoot, fileName));

        Assert.Contains("pt-auth-alert", content);
        Assert.Contains(expectedModifierClass, content);

        Assert.DoesNotContain("alert-info", content);
        Assert.DoesNotContain("alert-danger", content);
        Assert.DoesNotContain("alert-success", content);
        Assert.DoesNotContain("text-danger", content);
    }
}
