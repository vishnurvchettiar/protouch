using ClinicApp.Services;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Adversarial / negative tests for FileUploadService.ValidateFile.
/// Validates: Requirements 7.1–7.5
/// </summary>
public class FileUploadNegativeTests
{
    // Req 7.1 — Empty filename is invalid
    [Fact]
    public void ValidateFile_EmptyFilename_IsInvalid()
    {
        var (isValid, error) = FileUploadService.ValidateFile("", "image/jpeg", 1024);
        Assert.False(isValid);
        Assert.NotNull(error);
    }

    // Req 7.2 — Whitespace-only filename is invalid
    [Fact]
    public void ValidateFile_WhitespaceFilename_IsInvalid()
    {
        var (isValid, error) = FileUploadService.ValidateFile("   ", "image/jpeg", 1024);
        Assert.False(isValid);
        Assert.NotNull(error);
    }

    // Req 7.3 — Double extension with mismatched MIME type is invalid
    [Fact]
    public void ValidateFile_DoubleExtensionMismatch_IsInvalid()
    {
        var (isValid, error) = FileUploadService.ValidateFile("malware.exe.jpg", "application/octet-stream", 1024);
        Assert.False(isValid);
        Assert.NotNull(error);
    }

    // Req 7.4 — Zero byte file is invalid
    [Fact]
    public void ValidateFile_ZeroBytes_IsInvalid()
    {
        var (isValid, error) = FileUploadService.ValidateFile("test.jpg", "image/jpeg", 0);
        Assert.False(isValid);
        Assert.NotNull(error);
    }

    // Req 7.5 — long.MaxValue bytes is invalid (way over limit)
    [Fact]
    public void ValidateFile_LongMaxBytes_IsInvalid()
    {
        var (isValid, error) = FileUploadService.ValidateFile("test.jpg", "image/jpeg", long.MaxValue);
        Assert.False(isValid);
        Assert.NotNull(error);
    }
}
