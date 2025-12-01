using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Identity.Users.Roles;

/// <summary>
/// Configures the database mapping for the <see cref="UserRole"/> join entity.
/// </summary>
public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    /// <summary>
    /// Configures the entity of type <see cref="UserRole"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        #region Table
        // Set the table name for the ApplicationUserRole join entity.
        builder.ToTable(name: Schema.UserRoles);
        #endregion

        #region Primary Key
        // The composite primary key (UserId, RoleId) is inherited from IdentityUserRole<string>
        // and is configured by default by ASP.NET Core Identity.
        builder.HasKey(keyExpression: ur => new { ur.UserId, ur.RoleId });
        #endregion

        #region Properties
        builder.Property(propertyExpression: ur => ur.UserId)
            .HasComment(comment: "UserId: Foreign key to the associated ApplicationUser.");

        builder.Property(propertyExpression: ur => ur.RoleId)
            .HasComment(comment: "RoleId: Foreign key to the associated ApplicationRole.");

        // Apply common configurations using extension methods.
        builder.ConfigureAssignable(); // Configures properties related to assignable entities.
        #endregion

        #region Relationships
        // Configure relationships for the ApplicationUserRole entity.
        builder.HasOne(navigationExpression: ur => ur.ApplicationUser)
            .WithMany(navigationExpression: u => u.UserRoles)
            .HasForeignKey(foreignKeyExpression: ur => ur.UserId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasOne(navigationExpression: ur => ur.Role)
            .WithMany(navigationExpression: r => r.UserRoles)
            .HasForeignKey(foreignKeyExpression: ur => ur.RoleId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion
    }
}