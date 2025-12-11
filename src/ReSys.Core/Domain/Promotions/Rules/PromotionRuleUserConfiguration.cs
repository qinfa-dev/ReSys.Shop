namespace ReSys.Core.Domain.Promotions.Rules;

/// <summary>
/// Configures the database mapping for the <see cref="PromotionRuleUser"/> entity.
/// </summary>
public sealed class PromotionRuleUserConfiguration : IEntityTypeConfiguration<PromotionRuleUser>
{
    /// <summary>
    /// Configures the entity of type <see cref="PromotionRuleUser"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<PromotionRuleUser> builder)
    {
        #region Table
        // Set the table name for the PromotionRuleUser entity.
        builder.ToTable(name: Schema.PromotionRuleUsers);
        #endregion

        #region Primary Key
        // Configure the primary key for the PromotionRuleUser entity.
        builder.HasKey(keyExpression: pru => pru.Id);
        #endregion

        #region Properties
        // Configure properties for the PromotionRuleUser entity.
        builder.Property(propertyExpression: pru => pru.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the promotion rule user. Value generated never.");

        builder.Property(propertyExpression: pru => pru.PromotionRuleId)
            .IsRequired()
            .HasComment(comment: "PromotionRuleId: Foreign key to the associated PromotionRule.");

        builder.Property(propertyExpression: pru => pru.UserId)
            .IsRequired()
            .HasComment(comment: "UserId: Foreign key to the associated ApplicationUser.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable(); // For CreatedAt, UpdatedAt
        #endregion

        #region Relationships
        // Configure relationships for the PromotionRuleUser entity.
        builder.HasOne(navigationExpression: pru => pru.PromotionRule)
            .WithMany(navigationExpression: pr => pr.PromotionRuleUsers)
            .HasForeignKey(foreignKeyExpression: pru => pru.PromotionRuleId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasOne(navigationExpression: pru => pru.User)
            .WithMany()
            .HasForeignKey(foreignKeyExpression: pru => pru.UserId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion
    }
}
