using ClinicApp.Services;

namespace ClinicApp.Tests;

public class AppStorageTests
{
    [Theory]
    [InlineData("Data Source=/var/data/app.db", "/srv/app", "/var/data")]
    [InlineData("Data Source=Data/app.db", "/srv/app", "/srv/app/Data")]
    public void Folder_IsTheOneThatHoldsTheDatabase(string conn, string contentRoot, string expected)
        => Assert.Equal(expected, AppStorage.FolderFromConnectionString(conn, contentRoot));

    [Theory]
    [InlineData("Data Source=:memory:")]
    [InlineData("Server=db;Database=x")]
    public void NoFolder_ForInMemoryOrOtherDatabases(string conn)
        => Assert.Null(AppStorage.FolderFromConnectionString(conn, "/srv/app"));

    [Fact]
    public void UploadsPlansAndKeys_AllLiveUnderTheSameRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "ptx-storage-" + Guid.NewGuid().ToString("N"));
        try
        {
            var s = new AppStorage(root);
            foreach (var p in new[] { s.UploadsRoot, s.DietPlanRoot, s.KeysRoot })
            {
                Assert.StartsWith(root, p);
                Assert.True(Directory.Exists(p));
            }
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
