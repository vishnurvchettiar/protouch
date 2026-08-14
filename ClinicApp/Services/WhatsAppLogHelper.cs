using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ClinicApp.Services;

/// <summary>
/// Extracted static helper for WhatsApp log badge rendering.
/// Kept public and static for testability.
/// </summary>
public static class WhatsAppLogHelper
{
    private static readonly HashSet<string> ValidCssClasses =
        ["badge-active", "badge-info", "badge-danger", "badge-pending"];

    /// <summary>
    /// Parses a JSON array string from WhatsApp log's LogDetail field and returns
    /// a list of (Label, Css) tuples for badge rendering.
    /// Never throws — returns empty list on any error (including invalid JSON).
    /// </summary>
    public static List<(string Label, string Css)> GetLogDetailBadges(string? logDetail)
    {
        if (string.IsNullOrWhiteSpace(logDetail))
            return new();

        try
        {
            var items = JsonSerializer.Deserialize<List<JsonElement>>(logDetail);
            if (items == null) return new();

            return items.Select(d =>
            {
                string status;
                if (d.TryGetProperty("Status", out var s))
                    status = s.GetString() ?? "Unknown";
                else
                    status = "Unknown";

                var css = status switch
                {
                    "Sent"      => "badge-active",
                    "Simulated" => "badge-info",
                    "Failed"    => "badge-danger",
                    _           => "badge-pending"
                };
                return (status, css);
            }).ToList();
        }
        catch
        {
            return new();
        }
    }

    /// <summary>
    /// Formats a diet plan WhatsApp message for a specific client.
    /// </summary>
    public static string FormatDietPlanMessage(string clientName, string planText) =>
        $"Dear {clientName}, here is your personalised diet plan from Dr. Manjari:\n\n{planText}";
}
