using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Structural correctness-property test verifying that every logo &lt;img&gt;
/// reference across AuthLayout.razor and the 18 in-scope Anonymous_Account_Page
/// files points at the Canonical_Logo_Asset, and that none of these files
/// reference the logo URL with unescaped spaces.
///
/// Feature: account-status-pages-consistency, Property 8: Canonical Logo Asset Reference
/// Validates: Requirements 4.3, 4.5
/// </summary>
public class Property8LogoAssetTests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));

    private static readonly string PagesRoot =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Account/Pages");

    private static readonly string LayoutRoot =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Layout");

    private const string CanonicalLogoPath = "/images/WhatsApp%20Image%202026-08-12%20at%2012.25.21%20AM.jpeg";
    private const string DeprecatedLogoPath = "/images/WhatsApp Image 2026-08-12 at 12.25.21 AM.jpeg";

    // Matches any /images/... asset reference inside a src="..." attribute.
    private static readonly Regex ImageSrcRegex = new(
        "src\\s*=\\s*\"(/images/[^\"]+)\"",
        RegexOptions.Compiled);

    public static TheoryData<string> AllFileRelativePaths => new()
    {
        "Layout::AuthLayout.razor",
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

    private static string ResolveFullPath(string relativePath)
    {
        if (relativePath.StartsWith("Layout::"))
        {
            return Path.Combine(LayoutRoot, relativePath.Substring("Layout::".Length));
        }

        return Path.Combine(PagesRoot, relativePath);
    }

    [Theory]
    [MemberData(nameof(AllFileRelativePaths))]
    public void File_DoesNotContain_DeprecatedLogoAsset(string relativePath)
    {
        var content = File.ReadAllText(ResolveFullPath(relativePath));

        Assert.DoesNotContain(DeprecatedLogoPath, content);
    }

    [Theory]
    [MemberData(nameof(AllFileRelativePaths))]
    public void File_IfItRendersALogoImage_ReferencesOnlyTheCanonicalLogoAsset(string relativePath)
    {
        var content = File.ReadAllText(ResolveFullPath(relativePath));

        var matches = ImageSrcRegex.Matches(content);
        if (matches.Count == 0)
        {
            // This file renders zero /images/* <img> references at all -
            // nothing further to assert (e.g. Register.razor and most bare pages).
            return;
        }

        var referencedPaths = new List<string>();
        foreach (Match match in matches)
        {
            referencedPaths.Add(match.Groups[1].Value);
        }

        Assert.All(referencedPaths, path => Assert.Equal(CanonicalLogoPath, path));
    }

    // Explicitly confirms the two files known to render a logo <img> at all
    // (AuthLayout.razor once via the shell, Login.razor twice at the page
    // level) actually do contain the canonical asset reference, so this test
    // class can't pass vacuously if the logo were removed entirely.
    [Theory]
    [InlineData("Layout::AuthLayout.razor")]
    [InlineData("Login.razor")]
    public void KnownLogoRenderingFile_Contains_CanonicalLogoAsset(string relativePath)
    {
        var content = File.ReadAllText(ResolveFullPath(relativePath));

        Assert.Contains(CanonicalLogoPath, content);
    }
}
