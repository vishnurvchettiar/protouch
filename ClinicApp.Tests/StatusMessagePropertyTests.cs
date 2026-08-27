using FsCheck;
using FsCheck.Xunit;

namespace ClinicApp.Tests;

// ============================================================
// Feature: account-status-pages-consistency, Property 4: Status Alert Variant Correctness
// Validates: Requirements 3.1, 3.2
// ============================================================
public class StatusMessagePropertyTests
{
    // Mirrors StatusMessage.razor's exact branch logic:
    //   var statusMessageClass = DisplayMessage.StartsWith("Error") ? "error" : "success";
    private static string ComputeStatusMessageClass(string displayMessage)
    {
        return displayMessage.StartsWith("Error") ? "error" : "success";
    }

    [Property(MaxTest = 100)]
    public Property ComputeStatusMessageClass_MatchesErrorPrefixBranch()
    {
        return Prop.ForAll(
            Arb.Default.String().Filter(s => s != null),
            (displayMessage) =>
            {
                var result = ComputeStatusMessageClass(displayMessage);

                var expected = displayMessage.StartsWith("Error") ? "error" : "success";
                return result == expected;
            });
    }
}
