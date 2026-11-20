using ClinicApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.Driver;
using MongoDB.EntityFrameworkCore.Extensions;

namespace ClinicApp.Tests;

public sealed class MongoProviderModelTests
{
    [Fact]
    public void Mongo_model_builds_and_uses_manual_integer_keys()
    {
        var client = new MongoClient("mongodb://127.0.0.1:27017");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMongoDB(client, "protouch_model_test")
            .Options;
        using var db = new ApplicationDbContext(options, client.GetDatabase("protouch_model_test"));

        var intIdEntities = db.Model.GetEntityTypes()
            .Select(entity => entity.FindProperty("Id"))
            .Where(property => property?.ClrType == typeof(int))
            .ToArray();

        Assert.NotEmpty(intIdEntities);
        Assert.All(intIdEntities, property => Assert.Equal(ValueGenerated.Never, property!.ValueGenerated));
    }

    [Fact]
    public async Task Mongo_compatible_identity_store_resolves_role_links_without_joins()
    {
        using var db = TestDbFactory.CreateInMemoryDb();
        var role = new IdentityRole("Admin") { NormalizedName = "ADMIN" };
        var user = new ApplicationUser { Id = "client-1", UserName = "client@example.com", NormalizedUserName = "CLIENT@EXAMPLE.COM" };
        db.Roles.Add(role);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var store = new MongoCompatibleUserStore(db);
        await store.AddToRoleAsync(user, "ADMIN");

        Assert.Equal(new[] { "Admin" }, await store.GetRolesAsync(user));
        Assert.True(await store.IsInRoleAsync(user, "ADMIN"));
        Assert.Single(await store.GetUsersInRoleAsync("ADMIN"));

        await store.RemoveFromRoleAsync(user, "ADMIN");
        Assert.Empty(await store.GetRolesAsync(user));
    }
}
