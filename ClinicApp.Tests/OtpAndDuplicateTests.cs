using ClinicApp.Data;
using ClinicApp.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>One-time codes for sign up and password reset, and one account per mobile number.</summary>
public class OtpAndDuplicateTests
{
    [Theory]
    [InlineData("9876543210", "919876543210")]
    [InlineData("+91 98765 43210", "919876543210")]
    [InlineData("919876543210", "919876543210")]
    [InlineData("12345", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void CanonicalPhone_ReducesEveryWayOfTypingANumberToOneForm(string? input, string? expected)
    {
        Assert.Equal(expected, ClinicService.CanonicalPhone(input));
    }

    [Fact]
    public void MaskPhone_ShowsOnlyTheLastThreeDigits()
    {
        Assert.Equal("+91 ••••• ••210", ClinicService.MaskPhone("98765 43210"));
        Assert.Equal("your mobile number", ClinicService.MaskPhone("nope"));
    }

    [Fact]
    public async Task IsPhoneRegistered_MatchesNumbersTypedInDifferentWays()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        db.Users.Add(new ApplicationUser { Id = "u1", UserName = "a@x.com", Email = "a@x.com", FullName = "A", PhoneNumber = "+919876543210" });
        await db.SaveChangesAsync();
        var service = TestDbFactory.CreateClinicService(db);

        Assert.True(await service.IsPhoneRegisteredAsync("9876543210"));
        Assert.True(await service.IsPhoneRegisteredAsync("+91 98765 43210"));
        Assert.True(await service.IsPhoneRegisteredAsync("919876543210"));
        Assert.False(await service.IsPhoneRegisteredAsync("9876543211"));
        Assert.False(await service.IsPhoneRegisteredAsync("9876543210", exceptUserId: "u1")); // editing their own number
        Assert.False(await service.IsPhoneRegisteredAsync("not a number"));
    }

    [Fact]
    public async Task SendOtp_RejectsAnInvalidNumber()
    {
        var service = TestDbFactory.CreateClinicService(TestDbFactory.CreateInMemoryDb());
        var result = await service.SendOtpAsync("123");
        Assert.False(result.Sent);
    }

    [Fact]
    public async Task Otp_CorrectCodeWorksOnce()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var sent = await service.SendOtpAsync("9876543210");
        Assert.True(sent.Sent);
        Assert.NotNull(sent.TestCode);          // mock mode returns the code so a developer can see it
        Assert.Equal(6, sent.TestCode!.Length);

        var first = await service.VerifyOtpAsync("+91 98765 43210", sent.TestCode);
        Assert.True(first.Ok);
        var again = await service.VerifyOtpAsync("9876543210", sent.TestCode);
        Assert.False(again.Ok);                 // a code works once
    }

    [Fact]
    public async Task Otp_IsStoredHashedAndNeverLogged()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var sent = await service.SendOtpAsync("9876543211");

        var stored = await db.PhoneOtps.SingleAsync();
        Assert.NotEqual(sent.TestCode, stored.Code);
        Assert.Equal("919876543211", stored.PhoneNumber);
        var logs = await db.WhatsAppLogs.ToListAsync();
        Assert.All(logs, l =>
        {
            Assert.DoesNotContain(sent.TestCode!, l.MessageTemplate);
            Assert.DoesNotContain(sent.TestCode!, l.LogDetail ?? "");
        });
    }

    [Fact]
    public async Task Otp_WrongCodesAreCountedAndLockTheNumber()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var sent = await service.SendOtpAsync("9876543212");
        var wrong = sent.TestCode == "000000" ? "111111" : "000000";

        OtpVerifyResult last = new(false, null, 5);
        for (var i = 0; i < 5; i++) last = await service.VerifyOtpAsync("9876543212", wrong);
        Assert.False(last.Ok);
        Assert.Equal(0, last.AttemptsLeft);

        // Even the right code is refused once the number is locked.
        var rightButLocked = await service.VerifyOtpAsync("9876543212", sent.TestCode);
        Assert.False(rightButLocked.Ok);
    }

    [Fact]
    public async Task Otp_CannotBeRequestedAgainStraightAway()
    {
        var service = TestDbFactory.CreateClinicService(TestDbFactory.CreateInMemoryDb());
        Assert.True((await service.SendOtpAsync("9876543213")).Sent);

        var second = await service.SendOtpAsync("9876543213");
        Assert.False(second.Sent);
        Assert.True(second.RetryAfterSeconds > 0);
    }

    [Fact]
    public async Task Otp_OnlyTheNewestCodeWorks()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var first = await service.SendOtpAsync("9876543214");

        // Make the first code old enough that a new one may be requested.
        foreach (var o in db.PhoneOtps) o.CreatedAt = DateTime.UtcNow.AddMinutes(-2);
        await db.SaveChangesAsync();
        var second = await service.SendOtpAsync("9876543214");
        Assert.True(second.Sent);

        if (first.TestCode != second.TestCode)
            Assert.False((await service.VerifyOtpAsync("9876543214", first.TestCode)).Ok);
        Assert.True((await service.VerifyOtpAsync("9876543214", second.TestCode)).Ok);
    }

    [Fact]
    public async Task Otp_ExpiredCodeIsRefused()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var sent = await service.SendOtpAsync("9876543215");
        foreach (var o in db.PhoneOtps) o.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        Assert.False((await service.VerifyOtpAsync("9876543215", sent.TestCode)).Ok);
    }
}
