namespace ReSys.Core.Domain.Promotions.Audits;

/// <summary>
/// Configures the database mapping for the <see cref="PromotionUsage"/> entity.
/// </summary>
public sealed class PromotionUsageConfiguration : IEntityTypeConfiguration<PromotionUsage>
{
    /// <summary>
    /// Configures the entity of type <see cref="PromotionUsage"/>.
    /// </summary>
    /// <param name="builder">The builder to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<PromotionUsage> builder)
    {
        #region Table
        // Set the table name for the PromotionAuditLog entity.
        builder.ToTable(name: "PromotionAuditLogs");
        #endregion

        #region Primary Key
        // Configure the primary key for the PromotionAuditLog entity.
        builder.HasKey(keyExpression: x => x.Id);
        #endregion

        #region Indexes
        // Index on PromotionId for efficient querying of logs by promotion.
        builder.HasIndex(indexExpression: x => x.PromotionId);

        // Index on Action for grouped retrieval of specific audit types.
        builder.HasIndex(indexExpression: x => x.Action);

        // Index on CreatedAt for chronological sorting and filtering.
        builder.HasIndex(indexExpression: x => x.CreatedAt);

        // Composite index for optimized queries by PromotionId and timestamp.
        builder.HasIndex(indexExpression: x => new { x.PromotionId, x.CreatedAt });
        #endregion

        #region Properties
        builder.Property(propertyExpression: x => x.PromotionId)
            .IsRequired()
            .HasComment(comment: "PromotionId: Identifier of the promotion this audit entry belongs to.");

        builder.Property(propertyExpression: x => x.Action)
            .IsRequired()
            .HasMaxLength(50)
            .HasComment(comment: "Action: Name of the action performed (Created, Updated, Activated, etc.).");

        builder.Property(propertyExpression: x => x.Description)
            .IsRequired()
            .HasMaxLength(500)
            .HasComment(comment: "Description: Detailed explanation of the audit event.");

        builder.Property(propertyExpression: x => x.UserId)
            .HasMaxLength(450)
            .HasComment(comment: "UserId: Identifier of the user who performed the action, if available.");

        builder.Property(propertyExpression: x => x.UserEmail)
            .HasMaxLength(256)
            .HasComment(comment: "UserEmail: Email of the user who performed the action, if available.");

        builder.Property(propertyExpression: x => x.IpAddress)
            .HasMaxLength(45)
            .HasComment(comment: "IpAddress: IP address from which the action originated.");

        builder.Property(propertyExpression: x => x.UserAgent)
            .HasMaxLength(500)
            .HasComment(comment: "UserAgent: Client user-agent string associated with the action.");

        // JSON dictionaries storing before/after change snapshots and metadata.
        builder.Property(propertyExpression: x => x.ChangesBefore)
            .ConfigureDictionary()
            .HasComment(comment: "ChangesBefore: Dictionary snapshot of entity state before the action.");

        builder.Property(propertyExpression: x => x.ChangesAfter)
            .ConfigureDictionary()
            .HasComment(comment: "ChangesAfter: Dictionary snapshot of entity state after the action.");

        builder.Property(propertyExpression: x => x.Metadata)
            .ConfigureDictionary()
            .HasComment(comment: "Metadata: Additional contextual metadata for the audit entry.");
        #endregion

        #region Relationships
        // Define navigation property relationship with Promotion.
        builder.HasOne(navigationExpression: x => x.Promotion)
            .WithMany(p => p.PromotionUsages)
            .HasForeignKey(foreignKeyExpression: x => x.PromotionId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion
    }
}
