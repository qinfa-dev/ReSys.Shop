namespace ReSys.Core.Domain.Promotions.Rules;

/// <summary>
/// Configures the database mapping for the <see cref="PromotionRule"/> entity.
/// </summary>
public sealed class PromotionRuleConfiguration : IEntityTypeConfiguration<PromotionRule>
{
    /// <summary>
    /// Configures the entity of type <see cref="PromotionRule"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<PromotionRule> builder)
    {
        #region Table
        // Set the table name for the PromotionRule entity.
        builder.ToTable(name: Schema.PromotionRules);
        #endregion

        #region Primary Key
        // Configure the primary key for the PromotionRule entity.
        builder.HasKey(keyExpression: pr => pr.Id);
        #endregion

        #region Properties
        // Configure properties for the PromotionRule entity.
        builder.Property(propertyExpression: pr => pr.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the promotion rule. Value generated never.");

        builder.Property(propertyExpression: pr => pr.PromotionId)
            .IsRequired()
            .HasComment(comment: "PromotionId: Foreign key to the associated Promotion.");

        builder.Property(propertyExpression: pr => pr.Type)
            .IsRequired()
            .HasConversion<string>() // Storefront enum as string
            .HasComment(comment: "Type: The type of the promotion rule (e.g., 'UserLoggedIn', 'ProductInCart').");

        builder.Property(propertyExpression: pr => pr.Value)
            .IsRequired()
            .HasMaxLength(maxLength: PromotionRule.Constraints.ValueMaxLength)
            .HasComment(comment: "Value: The value associated with the rule (e.g., a product ID, a minimum quantity).");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable(); // For CreatedAt, UpdatedAt
        #endregion

        #region Relationships
        // Configure relationships for the PromotionRule entity.
        builder.HasOne(navigationExpression: pr => pr.Promotion)
            .WithMany(navigationExpression: p => p.PromotionRules)
            .HasForeignKey(foreignKeyExpression: pr => pr.PromotionId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: pr => pr.PromotionRuleTaxons)
            .WithOne(navigationExpression: prt => prt.PromotionRule)
            .HasForeignKey(foreignKeyExpression: prt => prt.PromotionRuleId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: pr => pr.PromotionRuleUsers)
            .WithOne(navigationExpression: pru => pru.PromotionRule)
            .HasForeignKey(foreignKeyExpression: pru => pru.PromotionRuleId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion
    }
}
