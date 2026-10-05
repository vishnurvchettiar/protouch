using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Regression tests verifying the Login.razor / Register.razor migration onto AuthLayout
/// and the canonical pt-auth-* class family, while preserving Identity behavior.
/// Validates: Requirements 1.1, 2.2, 2.3, 4.4, 4.5, 5.1
/// </summary>
public class LoginRegisterMigrationTests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));

    private static readonly string LoginPath =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Account/Pages/Login.razor");

    private static readonly string RegisterPath =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Account/Pages/Register.razor");

    private static readonly Regex LayoutDirectiveRegex = new(@"@layout\s+(\S+)", RegexOptions.Compiled);

    private static readonly Regex ClassAttributeRegex = new(@"class\s*=\s*""([^""]*)""", RegexOptions.Compiled);

    // Req 1.1 — both files must declare @layout AuthLayout and no other @layout directive
    [Theory]
    [InlineData("Login.razor")]
    [InlineData("Register.razor")]
    public void File_Contains_OnlyAuthLayoutDirective(string fileName)
    {
        var path = fileName == "Login.razor" ? LoginPath : RegisterPath;
        var content = File.ReadAllText(path);

        var matches = LayoutDirectiveRegex.Matches(content);

        Assert.NotEmpty(matches);
        foreach (Match match in matches)
        {
            Assert.Equal("AuthLayout", match.Groups[1].Value);
        }
    }

    // Req 5.1 — FormName values must be preserved exactly as before this feature's implementation
    [Fact]
    public void Login_Contains_FormNameLogin()
    {
        var content = File.ReadAllText(LoginPath);
        Assert.Contains("FormName=\"login\"", content);
    }

    [Fact]
    public void Register_Contains_FormNameRegister()
    {
        var content = File.ReadAllText(RegisterPath);
        Assert.Contains("FormName=\"register\"", content);
    }

    // Req 4.4 / 4.5 — Login.razor references the canonical logo exactly twice (shell/form-panel
    // logo + permitted secondary decorative-panel use); Register.razor references it zero times
    // at the page level because the shell (AuthLayout) now renders it once instead.
    [Fact]
    public void Login_References_CanonicalLogo_ExactlyTwice()
    {
        var content = File.ReadAllText(LoginPath);
        var occurrences = Regex.Matches(content, Regex.Escape("/images/WhatsApp Image 2026-08-12 at 12.25.21 AM.jpeg")).Count;
        Assert.Equal(2, occurrences);
    }

    [Fact]
    public void Register_References_CanonicalLogo_ExactlyZeroTimes()
    {
        var content = File.ReadAllText(RegisterPath);
        var occurrences = Regex.Matches(content, Regex.Escape("/images/WhatsApp Image 2026-08-12 at 12.25.21 AM.jpeg")).Count;
        Assert.Equal(0, occurrences);
    }

    // Req 2.2 / 2.3 — neither file contains a legacy login-* or auth-* class token (the
    // canonical pt-auth-* prefix is not a match since its tokens start with "pt-", not
    // "login-"/"auth-" — checked per whole class token, not via raw substring Contains).
    [Theory]
    [InlineData("Login.razor")]
    [InlineData("Register.razor")]
    public void File_Contains_NoLegacyClassTokens(string fileName)
    {
        var path = fileName == "Login.razor" ? LoginPath : RegisterPath;
        var content = File.ReadAllText(path);

        foreach (Match classAttrMatch in ClassAttributeRegex.Matches(content))
        {
            var tokens = classAttrMatch.Groups[1].Value.Split(
                (char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                Assert.False(
                    token.StartsWith("login-", StringComparison.Ordinal) ||
                    token.StartsWith("auth-", StringComparison.Ordinal),
                    $"Found legacy class token '{token}' in {fileName}");
            }
        }
    }
}
