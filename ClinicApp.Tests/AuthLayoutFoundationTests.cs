using System.IO;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Verifies the AuthLayout.razor and canonical pt-auth-* CSS foundation are in place,
/// and that the legacy `.auth-page`/`.login-split-page` CSS sections have been removed.
/// Validates: Requirements 1.4, 2.1, 4.5
/// </summary>
public class AuthLayoutFoundationTests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));

    private static readonly string AuthLayoutPath =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Layout/AuthLayout.razor");

    private static readonly string AppCssPath =
        Path.Combine(SolutionRoot, "ClinicApp/wwwroot/app.css");

    [Fact]
    public void AuthLayout_Contains_PtAuthShell()
    {
        var content = File.ReadAllText(AuthLayoutPath);
        Assert.Contains("pt-auth-shell", content);
    }

    [Fact]
    public void AuthLayout_Contains_PtAuthCard()
    {
        var content = File.ReadAllText(AuthLayoutPath);
        Assert.Contains("pt-auth-card", content);
    }

    [Fact]
    public void AuthLayout_Contains_LogoAssetReference()
    {
        var content = File.ReadAllText(AuthLayoutPath);
        Assert.Contains("/images/WhatsApp Image 2026-08-12 at 12.25.21 AM.jpeg", content);
    }

    [Fact]
    public void AppCss_Contains_CanonicalAuthUiLibrarySection()
    {
        var content = File.ReadAllText(AppCssPath);
        Assert.Contains("CANONICAL AUTH UI LIBRARY", content);
    }

    [Fact]
    public void AppCss_DoesNotContain_LegacyAuthPageRule()
    {
        var content = File.ReadAllText(AppCssPath);
        Assert.DoesNotContain(".auth-page {", content);
    }

    [Fact]
    public void AppCss_DoesNotContain_LegacyLoginSplitPageRule()
    {
        var content = File.ReadAllText(AppCssPath);
        Assert.DoesNotContain(".login-split-page {", content);
    }
}
