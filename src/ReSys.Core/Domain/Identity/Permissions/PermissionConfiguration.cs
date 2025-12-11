namespace ReSys.Core.Domain.Identity.Permissions;

/// <summary>
/// Configures the database mapping for the <see cref="AccessPermission"/> entity.
/// </summary>
public sealed class PermissionConfiguration : IEntityTypeConfiguration<AccessPermission>
{
    /// <summary>
    /// Configures the entity of type <see cref="AccessPermission"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<AccessPermission> builder)
    {
        #region Table
        // Set the table name for access permissions.
        builder.ToTable(name: Schema.AccessPermissions);
        #endregion

        #region Primary Key
        // Set the primary key for the table.
        builder.HasKey(keyExpression: p => p.Id);
        #endregion

        #region Properties
        // Configure properties for the AccessPermission entity.
        builder.Property(propertyExpression: p => p.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the access permission. Value generated never.");

        // The unique name of the permission, e.g., "admin.users.create".
        builder.Property(propertyExpression: p => p.Name)
            .IsRequired()
            .HasMaxLength(maxLength: AccessPermission.Constraints.MaxNameLength)
            .HasComment(comment: "Name: The unique name of the permission (e.g., 'admin.users.create').");

        // The area or domain of the permission, e.g., "admin".
        builder.Property(propertyExpression: p => p.Area)
            .IsRequired()
            .HasMaxLength(maxLength: AccessPermission.Constraints.MaxSegmentLength)
            .HasComment(comment: "Area: The area or domain of the permission (e.g., 'admin').");

        // The resource the permission applies to, e.g., "users".
        builder.Property(propertyExpression: p => p.Resource)
            .IsRequired()
            .HasMaxLength(maxLength: AccessPermission.Constraints.MaxSegmentLength)
            .HasComment(comment: "Resource: The resource the permission applies to (e.g., 'users').");

        // The action allowed by the permission, e.g., "create".
        builder.Property(propertyExpression: p => p.Action)
            .IsRequired()
            .HasMaxLength(maxLength: AccessPermission.Constraints.MaxSegmentLength)
            .HasComment(comment: "Action: The action allowed by the permission (e.g., 'create').");

        // A user-friendly display name for the permission.
        builder.Property(propertyExpression: p => p.DisplayName)
            .HasMaxLength(maxLength: AccessPermission.Constraints.MaxDisplayNameLength)
            .IsRequired(required: false)
            .HasComment(comment: "DisplayName: A user-friendly display name for the permission.");

        // A detailed description of what the permission allows.
        builder.Property(propertyExpression: p => p.Description)
            .HasMaxLength(maxLength: AccessPermission.Constraints.MaxDescriptionLength)
            .IsRequired(required: false)
            .HasComment(comment: "Description: A detailed description of what the permission allows.");

        // The category of the permission, stored as a string.
        builder.Property(propertyExpression: p => p.Category)
            .ConfigurePostgresEnumOptional()
            .HasComment(comment: "Category: The category of the permission.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable();
        #endregion

        #region Indexes
        // A unique index on the permission name ensures no duplicate permissions.
        builder.HasIndex(indexExpression: p => p.Name)
            .IsUnique();

        // A unique composite index on Area, Resource, and Action to prevent duplicate permission definitions.
        builder.HasIndex(indexExpression: p => new { p.Area, p.Resource, p.Action })
            .IsUnique();
        #endregion
    }
}