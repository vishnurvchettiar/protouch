using System.Text.RegularExpressions;

namespace ClinicApp.Services;

/// <summary>Normalizes credential characters in Atlas URIs without logging secrets.</summary>
public static partial class MongoConnectionString
{
    private static readonly Regex MongoHosts = new(
        @"^[A-Za-z0-9.-]+\.mongodb\.net(?::\d+)?(?:,[A-Za-z0-9.-]+\.mongodb\.net(?::\d+)?)*",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Normalize(string value)
    {
        var connection = value.Trim().Trim('"', '\'');
        var schemeEnd = connection.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd < 0 ||
            !(connection.StartsWith("mongodb://", StringComparison.OrdinalIgnoreCase) ||
              connection.StartsWith("mongodb+srv://", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("MONGODB_URI must be an Atlas mongodb:// or mongodb+srv:// connection string.");

        var credentialsStart = schemeEnd + 3;
        var authorityEnd = connection.IndexOfAny(['/', '?', '#'], credentialsStart);
        if (authorityEnd < 0) authorityEnd = connection.Length;

        // Locate the @ immediately before an Atlas host. This also works when a password
        // itself contains an unescaped @, which otherwise makes the whole URI unparsable.
        var separator = -1;
        for (var candidate = connection.IndexOf('@', credentialsStart); candidate >= 0 && candidate < authorityEnd;
             candidate = connection.IndexOf('@', candidate + 1))
        {
            var host = connection[(candidate + 1)..authorityEnd];
            if (MongoHosts.IsMatch(host))
            {
                separator = candidate;
                break;
            }
        }

        if (separator < 0) return connection;
        var credentials = connection[credentialsStart..separator];
        var colon = credentials.IndexOf(':');
        if (colon < 0) return connection;

        static string EscapeCredential(string part)
        {
            // Decode then re-encode so already encoded passwords are not double-escaped.
            try { part = Uri.UnescapeDataString(part); }
            catch (UriFormatException) { }
            return Uri.EscapeDataString(part);
        }

        var normalizedCredentials = EscapeCredential(credentials[..colon]) + ":" + EscapeCredential(credentials[(colon + 1)..]);
        return connection[..credentialsStart] + normalizedCredentials + connection[separator..];
    }
}
