using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using ReSys.Core.Common.Constants;
using ReSys.Core.Domain.Catalog.OptionTypes;
using ReSys.Core.Domain.Configurations;
using ReSys.Core.Domain.Constants;
using ReSys.Core.Domain.Identity.Roles;
using ReSys.Core.Domain.Identity.Roles.Claims;
using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Domain.Identity.Users.Claims;
using ReSys.Core.Domain.Identity.Users.Logins;
using ReSys.Core.Domain.Identity.Users.Roles;
using ReSys.Core.Domain.Identity.Users.Tokens;
using ReSys.Core.Domain.Inventories.StorePickups;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Infrastructure.Persistence.Contexts;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<
        User, Role, string,
        UserClaim, UserRole, UserLogin,
        RoleClaim, UserToken>(options: options), IApplicationDbContext
{
    public DbSet<Configuration> Configurations { get; set; }
    public DbSet<StorePickup> StorePickups { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder: builder);
        builder.HasPostgresExtension(name: "vector");
        builder.HasDefaultSchema(schema: Schema.Default);

        builder.ApplyConfigurationsFromAssembly(assembly: typeof(OptionType).Assembly);
        builder.ApplyUtcConversions();

    }
}
