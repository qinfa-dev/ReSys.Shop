// Add this using directive

namespace ReSys.Core.Domain.Auditing;

/// <summary>
/// Configures the database mapping for the <see cref="AuditLog"/> entity.
/// </summary>
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    /// <summary>
    /// Configures the entity of type <see cref="AuditLog"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        #region Table
        // Set the table name for the AuditLog entity.
        builder.ToTable(name: Schema.AuditLogs);
        #endregion

        #region Primary Key
        // Configure the primary key for the AuditLog entity.
        builder.HasKey(keyExpression: al => al.Id);
        #endregion

        #region Properties
        // Configure properties for the AuditLog entity.
        builder.Property(propertyExpression: al => al.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the audit log. Value generated never.");

        builder.Property(propertyExpression: al => al.EntityId)
            .IsRequired()
            .HasComment(comment: "EntityId: The ID of the entity that was audited.");

        builder.Property(propertyExpression: al => al.EntityName)
            .IsRequired()
            .HasMaxLength(maxLength: AuditLog.Constraints.EntityNameMaxLength)
            .HasComment(comment: "EntityName: The name of the entity that was audited.");

        builder.Property(propertyExpression: al => al.Action)
            .IsRequired()
            .HasMaxLength(maxLength: AuditLog.Constraints.ActionMaxLength)
            .HasComment(comment: "Action: The action performed on the entity (e.g., 'Create', 'Update', 'Delete').");

        builder.Property(propertyExpression: al => al.Timestamp)
            .IsRequired()
            .HasComment(comment: "Timestamp: The date and time when the audit log entry was created.");

        builder.Property(propertyExpression: al => al.UserId)
            .IsRequired(required: false) // Nullable
            .HasComment(comment: "UserId: The ID of the user who performed the action.");

        builder.Property(propertyExpression: al => al.UserName)
            .HasMaxLength(maxLength: AuditLog.Constraints.UserNameMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "UserName: The username of the user who performed the action.");

        builder.Property(propertyExpression: al => al.UserEmail)
            .HasMaxLength(maxLength: AuditLog.Constraints.UserEmailMaxLength) // Use new constraint
            .IsRequired(required: false)
            .HasComment(comment: "UserEmail: The email of the user who performed the action.");

        builder.Property(propertyExpression: al => al.OldValues)
            .HasColumnType(typeName: "jsonb") // Storefront as JSONB
            .IsRequired(required: false)
            .HasComment(comment: "OldValues: JSONB field containing the old values of the entity's properties before the action.");

        builder.Property(propertyExpression: al => al.NewValues)
            .HasColumnType(typeName: "jsonb") // Storefront as JSONB
            .IsRequired(required: false)
            .HasComment(comment: "NewValues: JSONB field containing the new values of the entity's properties after the action.");

        builder.Property(propertyExpression: al => al.ChangedProperties)
            .HasColumnType(typeName: "jsonb") // Storefront as JSONB
            .IsRequired(required: false)
            .HasComment(comment: "ChangedProperties: JSONB field containing the names of properties that were changed.");

        builder.Property(propertyExpression: al => al.IpAddress)
            .HasMaxLength(maxLength: AuditLog.Constraints.IpAddressMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "IpAddress: The IP address from which the action was performed.");

        builder.Property(propertyExpression: al => al.UserAgent)
            .HasMaxLength(maxLength: AuditLog.Constraints.UserAgentMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "UserAgent: The user agent string of the client that performed the action.");

        builder.Property(propertyExpression: al => al.RequestId)
            .IsRequired(required: false)
            .HasComment(comment: "RequestId: The ID of the request that triggered the action.");

        builder.Property(propertyExpression: al => al.Reason)
            .IsRequired(required: false)
            .HasComment(comment: "Reason: The reason for the action, if provided.");

        builder.Property(propertyExpression: al => al.AdditionalData)
            .HasColumnType(typeName: "jsonb") // Storefront as JSONB
            .IsRequired(required: false)
            .HasComment(comment: "AdditionalData: JSONB field for any additional data related to the audit log entry.");

        builder.Property(propertyExpression: al => al.Severity)
            .IsRequired()
            .HasConversion<string>() // Storefront enum as string
            .HasComment(comment: "Severity: The severity level of the audit log entry (e.g., 'Information', 'Warning', 'Error').");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable(); // Add this line
        #endregion
    }
}
