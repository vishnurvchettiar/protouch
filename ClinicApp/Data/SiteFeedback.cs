using System;
using System.ComponentModel.DataAnnotations;

namespace ClinicApp.Data;

public class SiteFeedback
{
    public int Id { get; set; }
    public int Rating { get; set; }           // 1–5

    [MaxLength(500)]
    public string? Comment { get; set; }      // nullable, max 500 chars

    public DateTime SubmittedAt { get; set; } // UTC, set by server
}
