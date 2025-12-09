using System.Security.Claims;

using ErrorOr;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using ReSys.Core.Domain.Identity.Permissions;
using ReSys.Core.Domain.Identity.Roles;
using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Security.Authorization.Claims.Constants;
using ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;
using ReSys.Core.Feature.Common.Security.Authorization.Roles;
using ReSys.Infrastructure.Persistence.Contexts;

using Serilog;

namespace ReSys.Infrastructure.Seeders.Contexts;

public sealed class IdentityDataSeeder(IServiceProvider serviceProvider) : IDataSeeder
{
    private readonly ILogger _logger = Log.ForContext<IdentityDataSeeder>();

    public int Order => 1;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        RoleManager<Role> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
        UserManager<User> userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _logger.Information(messageTemplate: "Starting identity seeding process.");

        try
        {
            _logger.Information(messageTemplate: "--- Ensuring all permissions exist ---");
            await EnsureAllPermissionsExistAsync(dbContext: dbContext,
                cancellationToken: cancellationToken);
            _logger.Information(messageTemplate: "--- Permissions ensured ---");

            _logger.Information(messageTemplate: "--- Ensuring all roles exist ---");
            await EnsureAllRolesExistAsync(roleManager: roleManager,
                cancellationToken: cancellationToken);
            _logger.Information(messageTemplate: "--- Roles ensured ---");

            _logger.Information(messageTemplate: "--- Seeding users per role ---");
            await SeedUsersPerRoleAsync(userManager: userManager,
                roleManager: roleManager,
                cancellationToken: cancellationToken);
            _logger.Information(messageTemplate: "--- Users per role seeded ---");

            _logger.Information(messageTemplate: "--- Assigning all permissions to System Admin ---");
            await AssignAllPermissionsToSystemAdminAsync(roleManager: roleManager,
                cancellationToken: cancellationToken);
            _logger.Information(messageTemplate: "--- Permissions assigned to System Admin ---");

            _logger.Information(messageTemplate: "Identity seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error(exception: ex,
                messageTemplate: "Identity seeding failed with exception: {ErrorMessage}",
                propertyValue: ex.Message);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureAllPermissionsExistAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        _logger.Information(messageTemplate: "Ensuring all permissions exist in database.");

        // Get all predefined permissions
        AccessPermission[] allPermissions = Feature.AllPermissions;
        _logger.Information(messageTemplate: "Found {Count} predefined permissions.",
            propertyValue: allPermissions.Length);

        // Get existing permission names (case-insensitive)
        HashSet<string> existingPermissionNames = await dbContext.Set<AccessPermission>()
            .Select(selector: p => p.Name.ToLowerInvariant())
            .ToHashSetAsync(cancellationToken: cancellationToken);
        _logger.Information(messageTemplate: "Found {Count} existing permissions in the database.",
            propertyValue: existingPermissionNames.Count);

        // Deduplicate and filter permissions
        List<ErrorOr<AccessPermission>> permissionsToAdd = allPermissions
            .GroupBy(keySelector: p => p.Name.ToLowerInvariant())
            .Select(selector: g => g.First())
            .Where(predicate: p => !existingPermissionNames.Contains(item: p.Name.ToLowerInvariant()))
            .Select(selector: p => AccessPermission.Create(area: p.Area,
                resource: p.Resource,
                action: p.Action,
                displayName: p.Description,
                description: p.DisplayName))
            .ToList();
        _logger.Information(messageTemplate: "Identified {Count} new permissions to add.",
            propertyValue: permissionsToAdd.Count);

        List<AccessPermission> validPermissions = [];
        foreach (ErrorOr<AccessPermission> permissionResult in permissionsToAdd)
        {
            if (permissionResult.IsError)
            {
                _logger.Warning(messageTemplate: "Failed to create permission: {Errors}",
                    propertyValue: string.Join(separator: "; ",
                        values: permissionResult.Errors.Select(selector: e => e.Description)));
                continue;
            }
            validPermissions.Add(item: permissionResult.Value);
            _logger.Information(messageTemplate: "Prepared to add permission: {PermissionName}",
                propertyValue: permissionResult.Value.Name);
        }

        if (validPermissions.Count > 0)
        {
            await dbContext.Set<AccessPermission>().AddRangeAsync(entities: validPermissions,
                cancellationToken: cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
            _logger.Information(messageTemplate: "Successfully added {Count} new permissions to the database.",
                propertyValue: validPermissions.Count);
        }
        else
        {
            _logger.Information(messageTemplate: "All predefined permissions already exist in the database. No new permissions added.");
        }
    }

    private async Task EnsureAllRolesExistAsync(RoleManager<Role> roleManager, CancellationToken cancellationToken)
    {
        _logger.Information(messageTemplate: "Ensuring all roles exist");

        List<string> allRoleNames = DefaultRole.SystemRoles.Concat(second: DefaultRole.StorefrontRoles).Distinct().ToList();

        foreach (string roleName in allRoleNames)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(value: roleName))
            {
                _logger.Warning(messageTemplate: "Skipping empty role name");
                continue;
            }

            Role? existingRole = await roleManager.FindByNameAsync(roleName: roleName);
            if (existingRole == null)
            {
                bool isSystemRole = DefaultRole.SystemRoles.Contains(value: roleName);
                ErrorOr<Role> newRoleResult = Role.Create(
                    name: roleName,
                    description: $"System role: {roleName}",
                    isSystemRole: isSystemRole
                );

                if (newRoleResult.IsError)
                {
                    _logger.Error(messageTemplate: "Failed creating role {RoleName}: {Errors}",
                        propertyValue0: roleName,
                        propertyValue1: string.Join(separator: "; ",
                            values: newRoleResult.Errors.Select(selector: e => e.Description)));
                    continue;
                }

                Role newRole = newRoleResult.Value;
                IdentityResult result = await roleManager.CreateAsync(role: newRole);
                if (!result.Succeeded)
                {
                    _logger.Error(messageTemplate: "Failed creating role {RoleName}: {Errors}",
                        propertyValue0: roleName,
                        propertyValue1: string.Join(separator: "; ",
                            values: result.Errors.Select(selector: e => e.Description)));
                }
                else
                {
                    _logger.Information(messageTemplate: "Created role {RoleName}",
                        propertyValue: roleName);
                }
            }
        }

        // Deduplicate roles
        try
        {
            List<Role> allRoles = await roleManager.Roles.ToListAsync(cancellationToken: cancellationToken);
            List<IGrouping<string?, Role>> duplicates = allRoles
                .GroupBy(keySelector: r => r.NormalizedName)
                .Where(predicate: g => g.Count() > 1)
                .ToList();

            foreach (IGrouping<string?, Role> dupGroup in duplicates)
            {
                foreach (Role remove in dupGroup.Skip(count: 1))
                {
                    IdentityResult delResult = await roleManager.DeleteAsync(role: remove);
                    if (!delResult.Succeeded)
                    {
                        _logger.Warning(messageTemplate: "Failed to remove duplicate role {RoleName}: {Errors}",
                            propertyValue0: remove.Name,
                            propertyValue1: string.Join(separator: "; ",
                                values: delResult.Errors.Select(selector: e => e.Description)));
                    }
                    else
                    {
                        _logger.Information(messageTemplate: "Removed duplicate role {RoleName}",
                            propertyValue: remove.Name);
                    }
                }
            }
        }
        catch (DbUpdateException ex)
        {
            _logger.Warning(exception: ex,
                messageTemplate: "Could not deduplicate roles: {Message}",
                propertyValue: ex.Message);
        }
    }

    private async Task SeedUsersPerRoleAsync(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        CancellationToken cancellationToken)
    {
        List<Role> allRoles = await roleManager.Roles.ToListAsync(cancellationToken: cancellationToken);

        foreach (Role role in allRoles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(value: role.Name))
            {
                _logger.Warning(messageTemplate: "Skipping role with null or empty name");
                continue;
            }

            string userEmail = $"{role.Name.ToLowerInvariant()}@seeder.com";
            string? userName = role.Name;
            string password = Environment.GetEnvironmentVariable(variable: "SEEDER_PASSWORD") ?? "Seeder@123"; // Use env var for production

            User? user = await userManager.FindByEmailAsync(email: userEmail);
            if (user == null)
            {
                ErrorOr<User> newUserResult = User.Create(
                    email: userEmail,
                    emailConfirmed: true,
                    userName: userName);

                if (newUserResult.IsError)
                {
                    _logger.Error(messageTemplate: "Failed creating user for role {RoleName}: {Errors}",
                        propertyValue0: role.Name,
                        propertyValue1: string.Join(separator: "; ",
                            values: newUserResult.Errors.Select(selector: e => e.Description)));
                    continue;
                }

                user = newUserResult.Value;
                IdentityResult result = await userManager.CreateAsync(user: user,
                    password: password);
                if (!result.Succeeded)
                {
                    _logger.Error(messageTemplate: "Failed creating user for role {RoleName}: {Errors}",
                        propertyValue0: role.Name,
                        propertyValue1: string.Join(separator: "; ",
                            values: result.Errors.Select(selector: e => e.Description)));
                    continue;
                }
                _logger.Information(messageTemplate: "Created user {UserName} for role {RoleName}",
                    propertyValue0: userName,
                    propertyValue1: role.Name);
            }

            if (!await userManager.IsInRoleAsync(user: user,
                    role: role.Name))
            {
                IdentityResult result = await userManager.AddToRoleAsync(user: user,
                    role: role.Name);
                if (!result.Succeeded)
                {
                    _logger.Error(messageTemplate: "Failed assigning role {RoleName} to user {UserName}: {Errors}",
                        propertyValue0: role.Name,
                        propertyValue1: userName,
                        propertyValue2: string.Join(separator: "; ",
                            values: result.Errors.Select(selector: e => e.Description)));
                }
                else
                {
                    _logger.Information(messageTemplate: "Assigned role {RoleName} to user {UserName}",
                        propertyValue0: role.Name,
                        propertyValue1: userName);
                }
            }
        }
    }

    private async Task AssignAllPermissionsToSystemAdminAsync(
        RoleManager<Role> roleManager,
        CancellationToken cancellationToken)
    {
        const string systemAdminRoleName = DefaultRole.Admin;
        Role? systemAdminRole = await roleManager.FindByNameAsync(roleName: systemAdminRoleName);

        if (systemAdminRole == null)
        {
            _logger.Warning(messageTemplate: "System admin role '{RoleName}' not found",
                propertyValue: systemAdminRoleName);
            return;
        }

        AccessPermission[] allPermissions = Feature.AllPermissions;
        IList<Claim> existingClaims = await roleManager.GetClaimsAsync(role: systemAdminRole);

        int addedCount = 0;
        foreach (AccessPermission permission in allPermissions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!existingClaims.Any(predicate: c => c.Type == CustomClaim.Permission && c.Value == permission.Name))
            {
                Claim claim = new(type: CustomClaim.Permission,
                    value: permission.Name);
                IdentityResult result = await roleManager.AddClaimAsync(role: systemAdminRole,
                    claim: claim);
                if (!result.Succeeded)
                {
                    _logger.Error(messageTemplate: "Failed adding claim {ClaimValue} to role {RoleName}: {Errors}",
                        propertyValue0: permission.Name,
                        propertyValue1: systemAdminRoleName,
                        propertyValue2: string.Join(separator: "; ",
                            values: result.Errors.Select(selector: e => e.Description)));
                    continue;
                }
                addedCount++;
            }
        }

        _logger.Information(
            messageTemplate: "Assigned {AddedCount} permissions to system admin role (total: {TotalCount})",
            propertyValue0: addedCount,
            propertyValue1: allPermissions.Length);
    }
}