using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicApp.Data;

/// <summary>
/// The MongoDB EF provider (EF Core 9) does not support cross-collection joins.
/// Keep ASP.NET Identity's standard persistence for users, but resolve role links
/// in two supported collection queries instead of the default SQL-style joins.
/// </summary>
public sealed class MongoCompatibleUserStore(
    ApplicationDbContext context,
    IdentityErrorDescriber? describer = null)
    : UserStore<ApplicationUser, IdentityRole, ApplicationDbContext>(context, describer)
{
    public override async Task<ApplicationUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default)
    {
        var login = await Context.UserLogins.FirstOrDefaultAsync(
            item => item.LoginProvider == loginProvider && item.ProviderKey == providerKey,
            cancellationToken);
        return login == null
            ? null
            : await Context.Users.FirstOrDefaultAsync(user => user.Id == login.UserId, cancellationToken);
    }

    public override async Task<IList<ApplicationUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claim);
        var claims = await Context.UserClaims.Where(item => item.ClaimType == claim.Type && item.ClaimValue == claim.Value)
            .ToListAsync(cancellationToken);
        var userIds = claims.Select(item => item.UserId).Distinct().ToList();
        return userIds.Count == 0
            ? Array.Empty<ApplicationUser>()
            : await Context.Users.Where(user => userIds.Contains(user.Id)).ToListAsync(cancellationToken);
    }

    public override async Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        var links = await Context.UserRoles.Where(link => link.UserId == user.Id).ToListAsync(cancellationToken);
        if (links.Count == 0) return Array.Empty<string>();
        var roleIds = links.Select(link => link.RoleId).Distinct().ToList();
        var roles = await Context.Roles.Where(role => roleIds.Contains(role.Id)).ToListAsync(cancellationToken);
        return roles.Where(role => role.Name != null).Select(role => role.Name!).ToList();
    }

    public override async Task<bool> IsInRoleAsync(ApplicationUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(normalizedRoleName);
        var role = await FindMongoRoleAsync(normalizedRoleName, cancellationToken);
        return role != null && await Context.UserRoles.AnyAsync(
            link => link.UserId == user.Id && link.RoleId == role.Id,
            cancellationToken);
    }

    public override async Task AddToRoleAsync(ApplicationUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(normalizedRoleName);
        var role = await FindMongoRoleAsync(normalizedRoleName, cancellationToken)
            ?? throw new InvalidOperationException($"Role '{normalizedRoleName}' was not found.");

        if (!await Context.UserRoles.AnyAsync(
                link => link.UserId == user.Id && link.RoleId == role.Id,
                cancellationToken))
        {
            Context.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = role.Id });
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public override async Task RemoveFromRoleAsync(ApplicationUser user, string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(normalizedRoleName);
        var role = await FindMongoRoleAsync(normalizedRoleName, cancellationToken);
        if (role == null) return;

        var link = await Context.UserRoles.FirstOrDefaultAsync(
            item => item.UserId == user.Id && item.RoleId == role.Id,
            cancellationToken);
        if (link != null)
        {
            Context.UserRoles.Remove(link);
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public override async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(normalizedRoleName);
        var role = await FindMongoRoleAsync(normalizedRoleName, cancellationToken);
        if (role == null) return Array.Empty<ApplicationUser>();

        var links = await Context.UserRoles.Where(link => link.RoleId == role.Id).ToListAsync(cancellationToken);
        var userIds = links.Select(link => link.UserId).Distinct().ToList();
        return userIds.Count == 0
            ? Array.Empty<ApplicationUser>()
            : await Context.Users.Where(user => userIds.Contains(user.Id)).ToListAsync(cancellationToken);
    }

    private Task<IdentityRole?> FindMongoRoleAsync(string normalizedRoleName, CancellationToken cancellationToken) =>
        Context.Roles.FirstOrDefaultAsync(role => role.NormalizedName == normalizedRoleName, cancellationToken);
}
