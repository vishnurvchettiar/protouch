using ClinicApp.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>Regression tests for the bulk WhatsApp campaign fixes.</summary>
public class WhatsAppBulkFixTests
{
    [Theory]
    [InlineData("9876543210", "whatsapp:+919876543210")]
    [InlineData("+91 98765 43210", "whatsapp:+919876543210")]
    [InlineData("098765-43210", null)]
    [InlineData("whatsapp:+919876543210", "whatsapp:+919876543210")]
    [InlineData("12345", null)]
    [InlineData("", null)]
    public void ToWhatsAppAddress_BuildsTwilioAddress(string input, string? expected)
    {
        Assert.Equal(expected, ClinicService.ToWhatsAppAddress(input));
    }

    [Fact]
    public void Parser_FindsColumnsByHeading_InAnyOrder()
    {
        var csv = "Phone,Name\r\n9876543210,Priya\r\n9876543211,Arjun";
        var rows = CsvRecipientParser.Parse(csv);
        Assert.Equal(2, rows.Count);
        Assert.Equal(new CsvRecipient("Priya", "9876543210"), rows[0]);
    }

    [Fact]
    public void Parser_HandlesBomQuotesAndSemicolons()
    {
        var csv = "﻿Name;Mobile\n\"Nair, Meera\";9876543210";
        var rows = CsvRecipientParser.Parse(csv);
        Assert.Single(rows);
        Assert.Equal("Nair, Meera", rows[0].Name);
        Assert.Equal("9876543210", rows[0].Phone);
    }

    [Fact]
    public void Parser_WithoutHeadings_UsesFirstTwoColumns()
    {
        var rows = CsvRecipientParser.Parse("Priya,9876543210\nArjun,9876543211");
        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void Parser_SkipsRowsWithoutPhone()
    {
        var rows = CsvRecipientParser.Parse("Name,Phone\nPriya,\nArjun,9876543211");
        Assert.Single(rows);
        Assert.Equal("Arjun", rows[0].Name);
    }

    [Fact]
    public async Task Campaign_SkipsDuplicatesAndReportsSimulatedInMockMode()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        db.Settings.Add(new ClinicApp.Data.Setting { Key = "WhatsApp:BulkDelayMs", Value = "100" });
        await db.SaveChangesAsync();
        var service = TestDbFactory.CreateClinicService(db);

        var result = await service.SendCsvWhatsAppCampaignAsync("Test", "Hi {{Name}}", new List<CsvRecipient>
        {
            new("Alice", "9000000001"),
            new("Alice again", "+91 90000 00001"),
            new("Bob", "9000000002"),
        });

        Assert.Equal(0, result.Sent);
        Assert.Equal(2, result.Simulated);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(0, result.Failed);
        Assert.NotEqual("Twilio", result.Mode);
    }

    [Fact]
    public async Task Preview_FlagsInvalidAndDuplicateNumbers()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var rows = await service.PreviewCsvCampaignAsync("Hi {{Name}}", new List<CsvRecipient>
        {
            new("Alice", "9000000001"),
            new("Bob", "123"),
            new("Alice again", "9000000001"),
        });

        Assert.Null(rows[0].Warning);
        Assert.NotNull(rows[1].Warning);
        Assert.NotNull(rows[2].Warning);
    }
}
