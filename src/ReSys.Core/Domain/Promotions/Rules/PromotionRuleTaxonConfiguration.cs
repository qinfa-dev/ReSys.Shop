using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Promotions.Rules;

/// <summary>
/// Configures the database mapping for the <see cref="PromotionRuleTaxon"/> entity.
/// </summary>
public sealed class PromotionRuleTaxonConfiguration : IEntityTypeConfiguration<PromotionRuleTaxon>
{
    /// <summary>
    /// Configures the entity of type <see cref="PromotionRuleTaxon"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<PromotionRuleTaxon> builder)
    {
        #region Table
        // Set the table name for the PromotionRuleTaxon entity.
        builder.ToTable(name: Schema.PromotionRuleTaxons);
        #endregion

        #region Primary Key
        // Configure the primary key for the PromotionRuleTaxon entity.
        builder.HasKey(keyExpression: prt => prt.Id);
        #endregion

        #region Properties
        // Configure properties for the PromotionRuleTaxon entity.
        builder.Property(propertyExpression: prt => prt.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the promotion rule taxon. Value generated never.");

        builder.Property(propertyExpression: prt => prt.PromotionRuleId)
            .IsRequired()
            .HasComment(comment: "PromotionRuleId: Foreign key to the associated PromotionRule.");

        builder.Property(propertyExpression: prt => prt.TaxonId)
            .IsRequired()
            .HasComment(comment: "TaxonId: Foreign key to the associated Taxon.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable(); // For CreatedAt, UpdatedAt
        #endregion

        #region Relationships
        // Configure relationships for the PromotionRuleTaxon entity.
        builder.HasOne(navigationExpression: prt => prt.PromotionRule)
            .WithMany(navigationExpression: pr => pr.PromotionRuleTaxons)
            .HasForeignKey(foreignKeyExpression: prt => prt.PromotionRuleId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasOne(navigationExpression: prt => prt.Taxon)
            .WithMany(navigationExpression: pr => pr.PromotionRuleTaxons)
            .HasForeignKey(foreignKeyExpression: prt => prt.TaxonId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion
    }
}
