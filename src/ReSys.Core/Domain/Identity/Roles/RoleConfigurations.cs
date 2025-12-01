using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Identity.Roles;

/// <summary>
/// Configures the database mapping for the <see cref="Role"/> entity.
/// </summary>
public sealed class RoleConfigurations : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        // Set the table name for roles.
        builder.ToTable(name: Schema.Roles);

        // Set the primary key for the table.
        builder.HasKey(keyExpression: r => r.Id);

        #region Properties

        // Configure the Name property from the base IdentityRole.
        builder.Property(propertyExpression: e => e.Name)
            .IsRequired()
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength);

        // Configure the NormalizedName for case-insensitive role name lookups.
        builder.Property(propertyExpression: e => e.NormalizedName)
            .IsRequired()
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength);

        // Configure the custom Description property for the role.
        builder.Property(propertyExpression: e => e.Description)
            .HasMaxLength(maxLength: CommonInput.Constraints.Text.MediumTextMaxLength);

        builder.Property(propertyExpression: e => e.DisplayName)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired(required: false);

        builder.Property(propertyExpression: e => e.IsDefault)
            .IsRequired();

        builder.Property(propertyExpression: e => e.Priority)
            .IsRequired();

        builder.Property(propertyExpression: e => e.IsSystemRole)
            .IsRequired();

        builder.ConfigureAuditable();
        builder.ConfigureVersion();
        #endregion

        #region Indexes

        // A unique index on the normalized role name to prevent duplicate roles.
        builder.HasIndex(indexExpression: r => r.NormalizedName)
            .IsUnique();

        // Index for system roles to optimize queries.
        builder.HasIndex(indexExpression: r => r.IsSystemRole);

        // Index for default roles to optimize queries.
        builder.HasIndex(indexExpression: r => r.IsDefault);

        // Index for role priority to optimize queries.
        builder.HasIndex(indexExpression: r => r.Priority);

        #endregion

        // Configure relationships if any (e.g., with UserRoles, RoleClaims).

        #region Relationships
        // Each Role can have many entries in the UserRole join table
        builder.HasMany(navigationExpression: e => e.UserRoles)
            .WithOne(navigationExpression: e => e.Role)
            .HasForeignKey(foreignKeyExpression: ur => ur.RoleId)
            .IsRequired();

        // Each Role can have many associated RoleClaims
        builder.HasMany(navigationExpression: e => e.RoleClaims)
            .WithOne(navigationExpression: e => e.Role)
            .HasForeignKey(foreignKeyExpression: rc => rc.RoleId)
            .IsRequired();
        #endregion
    }
}
