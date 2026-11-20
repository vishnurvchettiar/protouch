namespace ClinicApp.Services;

/// <summary>
/// One persistent folder for everything the clinic saves: the database, uploaded photos, diet plan
/// documents and login keys. It is the folder that holds the SQLite file, so on Render it is the
/// mounted disk (/var/data). Anything kept outside it is wiped on every deploy.
/// </summary>
public sealed class AppStorage
{
    public AppStorage(string root)
    {
        Root = root;
        Directory.CreateDirectory(root);
    }

    public string Root { get; }
    public string UploadsRoot => Ensure(Path.Combine(Root, "uploads"));
    public string DietPlanRoot => Ensure(Path.Combine(Root, "diet-plan-documents"));
    public string KeysRoot => Ensure(Path.Combine(Root, "keys"));

    private static string Ensure(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>The folder that holds the SQLite database file in a connection string, or null if there is none.</summary>
    public static string? FolderFromConnectionString(string connectionString, string contentRoot)
    {
        const string prefix = "Data Source=";
        if (!connectionString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
        var path = connectionString[prefix.Length..].Trim();
        if (path.Length == 0 || path == ":memory:") return null;
        var full = Path.IsPathRooted(path) ? path : Path.Combine(contentRoot, path);
        return Path.GetDirectoryName(full);
    }
}
