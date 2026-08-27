using System.IO;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Structural correctness-property test verifying that every file in the
/// explicitly out-of-scope set for this feature was left untouched by the
/// AuthLayout/pt-auth-* migration: none of them reference "AuthLayout" or
/// the "pt-auth-" class prefix.
///
/// Feature: account-status-pages-consistency, Property 10: Out-of-Scope File Isolation
/// Validates: Requirements 6.4, 6.5
/// </summary>
public class OutOfScopeIsolationTests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));

    public static TheoryData<string> OutOfScopeRelativePaths => new()
    {
        // Dashboard shell + nav
        "ClinicApp/Components/Layout/MainLayout.razor",
        "ClinicApp/Components/Layout/NavMenu.razor",
        "ClinicApp/Components/Account/Shared/ManageLayout.razor",

        // Every file under ClinicApp/Components/Account/Pages/Manage/
        "ClinicApp/Components/Account/Pages/Manage/ChangePassword.razor",
        "ClinicApp/Components/Account/Pages/Manage/DeletePersonalData.razor",
        "ClinicApp/Components/Account/Pages/Manage/Disable2fa.razor",
        "ClinicApp/Components/Account/Pages/Manage/Email.razor",
        "ClinicApp/Components/Account/Pages/Manage/EnableAuthenticator.razor",
        "ClinicApp/Components/Account/Pages/Manage/ExternalLogins.razor",
        "ClinicApp/Components/Account/Pages/Manage/GenerateRecoveryCodes.razor",
        "ClinicApp/Components/Account/Pages/Manage/Index.razor",
        "ClinicApp/Components/Account/Pages/Manage/PersonalData.razor",
        "ClinicApp/Components/Account/Pages/Manage/ResetAuthenticator.razor",
        "ClinicApp/Components/Account/Pages/Manage/SetPassword.razor",
        "ClinicApp/Components/Account/Pages/Manage/TwoFactorAuthentication.razor",
        "ClinicApp/Components/Account/Pages/Manage/_Imports.razor",

        // Dashboards + marketing pages
        "ClinicApp/Components/Pages/AdminDashboard.razor",
        "ClinicApp/Components/Pages/ClientDashboard.razor",
        "ClinicApp/Components/Pages/Home.razor",
        "ClinicApp/Components/Pages/ServiceDetail.razor",
        "ClinicApp/Components/Pages/ServiceBooking.razor",

        // PublicLayout — intentionally left completely untouched per the design
        "ClinicApp/Components/Layout/PublicLayout.razor",
    };

    [Theory]
    [MemberData(nameof(OutOfScopeRelativePaths))]
    public void OutOfScopeFile_DoesNotReference_AuthLayoutOrCanonicalClassPrefix(string relativePath)
    {
        var fullPath = Path.Combine(SolutionRoot, relativePath);
        var content = File.ReadAllText(fullPath);

        Assert.DoesNotContain("AuthLayout", content);
        Assert.DoesNotContain("pt-auth-", content);
    }
}
