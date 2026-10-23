using System.IO;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Regression tests guarding two real UI bugs found in the AuthLayout/pt-auth-*
/// design system introduced in the account-status-pages-consistency feature:
///
/// 1. Login.razor renders a full-viewport, two-column split-screen design
///    (.pt-auth-split-page, min-height: 100vh) with its own two page-local logo
///    instances. AuthLayout.razor must NOT nest that design inside the standard
///    460px-wide .pt-auth-card (which would visibly crush the split-screen layout
///    into a narrow floating box) or render its own additional brand logo above it
///    (which would show the logo three times on one page).
///
/// 2. app.css must not rely on the CSS :has() selector for layout-critical width
///    calculations -- it is unsupported in Firefox &lt; 121, Safari &lt; 16.4, and
///    Chrome &lt; 105, causing the Register.razor form to render cramped/misaligned
///    on those browsers. AuthLayout.razor now decides the wide-card treatment
///    itself from the current route instead.
/// </summary>
public class AuthLayoutFullBleedRegressionTests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));

    private static readonly string AuthLayoutPath =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Layout/AuthLayout.razor");

    private static readonly string AppCssPath =
        Path.Combine(SolutionRoot, "ClinicApp/wwwroot/app.css");

    [Fact]
    public void AuthLayout_DoesNotUnconditionallyWrapEveryPageInPtAuthCard()
    {
        var content = File.ReadAllText(AuthLayoutPath);

        // AuthLayout must branch on the current route before rendering the
        // pt-auth-card wrapper, rather than always wrapping @Body in it --
        // otherwise Login.razor's full-viewport split-screen design gets
        // crushed into the narrow standard card.
        Assert.Contains("@if", content);
        Assert.Contains("IsFullBleedPage", content);
    }

    [Fact]
    public void AuthLayout_SkipsCardWrapperOnlyForLoginRoute()
    {
        var content = File.ReadAllText(AuthLayoutPath);

        Assert.Contains("Account/Login", content);
    }

    [Fact]
    public void AuthLayout_AppliesWideModifierForRegisterRoute()
    {
        var content = File.ReadAllText(AuthLayoutPath);

        Assert.Contains("Account/Register", content);
        Assert.Contains("pt-auth-shell--wide", content);
    }

    [Fact]
    public void AuthLayout_StillWrapsDefaultPagesInPtAuthShellAndCard()
    {
        var content = File.ReadAllText(AuthLayoutPath);

        Assert.Contains("pt-auth-shell", content);
        Assert.Contains("pt-auth-card", content);
        Assert.Contains("pt-auth-brand", content);
        Assert.Contains("/images/WhatsApp%20Image%202026-08-12%20at%2012.25.21%20AM.jpeg", content);
        Assert.Contains("@Body", content);
    }

    [Fact]
    public void AppCss_DoesNotUseHasSelectorInActiveCssRules()
    {
        var content = File.ReadAllText(AppCssPath);

        // :has() is unsupported in Firefox < 121, Safari < 16.4, Chrome < 105.
        // A rule like `.pt-auth-card:has(> .pt-auth-card--wide)` would silently
        // fail to match in those browsers, leaving Register.razor's form
        // cramped into a 460px-wide card instead of the intended 680px-wide
        // layout. Strip /* ... */ comments first so explanatory prose that
        // merely *mentions* :has() (documenting why it was removed) doesn't
        // produce a false positive against this assertion.
        var withoutComments = System.Text.RegularExpressions.Regex.Replace(
            content, @"/\*.*?\*/", string.Empty, System.Text.RegularExpressions.RegexOptions.Singleline);

        Assert.DoesNotContain(":has(", withoutComments);
    }

    [Fact]
    public void AppCss_WideModifierAppliesWithoutHasSelector()
    {
        var content = File.ReadAllText(AppCssPath);

        // The wide-card width rule must instead key off a plain class
        // combinator (set on AuthLayout's own shell element based on route)
        // that every supported browser can evaluate.
        Assert.Contains(".pt-auth-shell--wide .pt-auth-card", content);
        Assert.Contains(".pt-auth-card--wide .pt-auth-grid", content);
    }
}
