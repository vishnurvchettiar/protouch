using System.Collections.Generic;
using System.Threading.Tasks;
using ClinicApp.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Negative tests for WhatsApp / CSV campaign methods.
/// Validates: Requirements 9.1–9.3
/// </summary>
public class WhatsAppNegativeTests
{
    // Req 9.1 — Empty recipient list: SendCsvWhatsAppCampaignAsync should either throw or produce no logs
    [Fact]
    public async Task SendCsvCampaign_EmptyRecipientList_ThrowsOrReturnsEmpty()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var emptyList = new List<CsvRecipient>();

        // The service filters empty phones and iterates, so with 0 recipients it completes silently
        // with 0 messages sent. Verify no WhatsApp logs are created for individual sends.
        var countBefore = await db.WhatsAppLogs.CountAsync();

        try
        {
            await service.SendCsvWhatsAppCampaignAsync("Empty Campaign", "Hello {{Name}}!", emptyList);
            // If it completes, no individual messages should have been sent (only possibly a bulk log)
            // The test passes either way (no unhandled exception)
        }
        catch (ArgumentException)
        {
            // Also acceptable if the service throws for empty list
        }

        // Either way: no individual logs for recipients should be added
        var countAfter = await db.WhatsAppLogs.CountAsync();
        // At most one campaign-summary log may be added, but no individual recipient logs
        Assert.True(countAfter - countBefore <= 1, "No individual send logs should be created for empty recipient list");
    }

    // Req 9.2 — Template with no {{Name}} placeholder must not throw
    [Fact]
    public async Task PreviewCsv_TemplateWithNoNamePlaceholder_DoesNotThrow()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var recipients = new List<CsvRecipient>
        {
            new("Alice", "9876543210")
        };

        // Template has no {{Name}} — should not throw, just use the template as-is
        var result = await service.PreviewCsvCampaignAsync("This template has no name.", recipients);
        Assert.NotNull(result);
    }

    // Req 9.3 — Whitespace-only template must not throw
    [Fact]
    public async Task PreviewCsv_WhitespaceTemplate_DoesNotThrow()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var recipients = new List<CsvRecipient>
        {
            new("Bob", "9876543210")
        };

        // Template is only whitespace — should not throw
        var result = await service.PreviewCsvCampaignAsync("   ", recipients);
        Assert.NotNull(result);
    }
}
