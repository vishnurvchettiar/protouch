using System.Text;

namespace ClinicApp.Services;

/// <summary>
/// Reads recipients for a bulk WhatsApp campaign from CSV text.
/// Handles a byte-order mark, comma / semicolon / tab separators, quoted values
/// that contain separators, and finds the Name and Phone columns by their headings
/// (Name, Phone, Mobile, Number, WhatsApp). Without headings, the first column is
/// the name and the second is the phone number.
/// </summary>
public static class CsvRecipientParser
{
    public static List<CsvRecipient> Parse(string? content)
    {
        var result = new List<CsvRecipient>();
        if (string.IsNullOrWhiteSpace(content)) return result;

        var lines = content.TrimStart('﻿')
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();
        if (lines.Count == 0) return result;

        var separator = DetectSeparator(lines[0]);
        var rows = lines.Select(l => SplitLine(l, separator)).ToList();

        var header = rows[0].Select(h => h.Trim().ToLowerInvariant()).ToList();
        int nameCol = header.FindIndex(h => h is "name" or "full name" or "fullname" or "client" or "client name");
        int phoneCol = header.FindIndex(h => h is "phone" or "phone number" or "phonenumber" or "mobile" or "mobile number" or "number" or "whatsapp" or "contact");
        bool hasHeader = nameCol >= 0 || phoneCol >= 0;

        if (phoneCol < 0) phoneCol = nameCol == 1 ? 0 : 1;
        if (nameCol < 0) nameCol = phoneCol == 0 ? 1 : 0;

        foreach (var row in hasHeader ? rows.Skip(1) : rows.Skip(LooksLikeHeader(rows[0]) ? 1 : 0))
        {
            if (row.Count <= Math.Max(nameCol, phoneCol)) continue;
            var name = row[nameCol].Trim();
            var phone = row[phoneCol].Trim();
            if (string.IsNullOrWhiteSpace(phone)) continue;
            result.Add(new CsvRecipient(name, phone));
        }
        return result;
    }

    // A first row with no digits in the second column is a heading row we did not recognise.
    private static bool LooksLikeHeader(List<string> row) =>
        row.Count >= 2 && !row[1].Any(char.IsDigit);

    private static char DetectSeparator(string firstLine)
    {
        var best = ',';
        var bestCount = firstLine.Count(c => c == ',');
        foreach (var c in new[] { ';', '\t' })
        {
            var n = firstLine.Count(x => x == c);
            if (n > bestCount) { best = c; bestCount = n; }
        }
        return best;
    }

    private static List<string> SplitLine(string line, char separator)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                else inQuotes = !inQuotes;
            }
            else if (c == separator && !inQuotes)
            {
                fields.Add(sb.ToString());
                sb.Clear();
            }
            else sb.Append(c);
        }
        fields.Add(sb.ToString());
        return fields;
    }
}
