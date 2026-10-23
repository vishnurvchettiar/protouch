using System.IO;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// File-content regression tests that verify the logo and CSS fixes remain in place.
/// If a developer accidentally reverts the fixes, these tests fail immediately.
/// Validates: Requirements 3.1–3.6
/// </summary>
public class LogoReferenceTests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));

    private static readonly string NavMenuPath =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Layout/NavMenu.razor");

    private static readonly string MainLayoutPath =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Layout/MainLayout.razor");

    // Req 3.1 — NavMenu must NOT reference the old broken path
    [Fact]
    public void NavMenu_DoesNotContain_OldLogoMarkPng()
    {
        var content = File.ReadAllText(NavMenuPath);
        Assert.DoesNotContain("logo-mark.png", content);
    }

    // Req 3.2 — NavMenu MUST reference the correct URL-encoded JPEG
    [Fact]
    public void NavMenu_Contains_CorrectLogoPath()
    {
        var content = File.ReadAllText(NavMenuPath);
        Assert.Contains("WhatsApp%20Image%202026-08-12%20at%2012.25.21%20AM.jpeg", content);
    }

    // Req 3.3 — MainLayout topbar must NOT reference the old broken path
    [Fact]
    public void MainLayout_DoesNotContain_OldLogoMarkPng()
    {
        var content = File.ReadAllText(MainLayoutPath);
        Assert.DoesNotContain("logo-mark.png", content);
    }
}

public class SidebarCssTests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));

    private static readonly string MainLayoutCssPath =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Layout/MainLayout.razor.css");

    // The layout reserves one sidebar column; the nested fixed sidebar must not
    // also push main content with a second margin.
    [Fact]
    public void MainLayoutCss_Sidebar_HasWidthRule()
    {
        var content = File.ReadAllText(MainLayoutCssPath);
        Assert.Contains("flex-basis: 240px", content);
    }

    // The sidebar wrapper owns the layout width, so main must not add a duplicate offset.
    [Fact]
    public void MainLayoutCss_Main_HasMarginLeft()
    {
        var content = File.ReadAllText(MainLayoutCssPath);
        Assert.Contains("main { margin-left: 0; }", content);
    }

    // Req 3.6 — mobile breakpoint collapses the sidebar wrapper to 64px without a duplicate main offset.
    [Fact]
    public void MainLayoutCss_MobileBreakpoint_CollapsesSidebarTo64px()
    {
        var content = File.ReadAllText(MainLayoutCssPath);
        Assert.Contains("max-width: 768px", content);
        Assert.Contains("width: 64px", content);
        Assert.Contains("main { margin-left: 0; }", content);
    }
}
