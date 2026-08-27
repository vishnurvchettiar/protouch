using System.IO;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Regression tests verifying that ExternalLoginPicker.razor's migration to the
/// canonical pt-auth-* class family preserves its existing Identity-related
/// behavior (external authentication scheme enumeration and external login
/// form submission target).
///
/// Feature: account-status-pages-consistency
/// Validates: Requirements 2.4, 3.3, 5.6
/// </summary>
public class ExternalLoginPickerRegressionTests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));

    private static readonly string ExternalLoginPickerPath =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Account/Shared/ExternalLoginPicker.razor");

    [Fact]
    public void ExternalLoginPicker_Contains_CanonicalButtonClass()
    {
        var content = File.ReadAllText(ExternalLoginPickerPath);
        Assert.Contains("pt-auth-button", content);
    }

    [Fact]
    public void ExternalLoginPicker_Contains_CanonicalInfoAlertClass()
    {
        var content = File.ReadAllText(ExternalLoginPickerPath);
        Assert.Contains("pt-auth-alert pt-auth-alert--info", content);
    }

    [Fact]
    public void ExternalLoginPicker_DoesNotContain_LegacyButtonClass()
    {
        var content = File.ReadAllText(ExternalLoginPickerPath);
        Assert.DoesNotContain("btn btn-primary", content);
    }

    [Fact]
    public void ExternalLoginPicker_PreservesIdentityBehavior()
    {
        var content = File.ReadAllText(ExternalLoginPickerPath);
        Assert.Contains("GetExternalAuthenticationSchemesAsync", content);
        Assert.Contains("action=\"Account/PerformExternalLogin\"", content);
    }
}
