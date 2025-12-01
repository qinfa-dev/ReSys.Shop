using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Catalog.Taxonomies.Rules;

/// <summary>
/// Configures the database mapping for the <see cref="TaxonRule"/> entity.
/// </summary>
public sealed class TaxonRuleConfiguration : IEntityTypeConfiguration<TaxonRule>
{
    /// <summary>
    /// Configures the entity of type <see cref="TaxonRule"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<TaxonRule> builder)
    {
        #region Table
        // Set the table name for the TaxonRule entity.
        builder.ToTable(name: Schema.TaxonRules);
        #endregion

        #region Primary Key
        // Configure the primary key for the TaxonRule entity.
        builder.HasKey(keyExpression: tr => tr.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: tr => new { tr.TaxonId, tr.Type });
        #endregion

        #region Properties
        // Configure properties for the TaxonRule entity.
        builder.Property(propertyExpression: tr => tr.Id)
            .HasColumnName(name: "id")
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the taxon rule. Value generated never.");

        builder.Property(propertyExpression: tr => tr.TaxonId)
            .IsRequired()
            .HasComment(comment: "TaxonId: Foreign key to the associated Taxon.");

        builder.Property(propertyExpression: tr => tr.Type)
            .ConfigureShortText()
            .HasComment(comment: "Type: The type of the rule (e.g., 'product_name', 'product_property').");

        builder.Property(propertyExpression: tr => tr.Value)
            .ConfigureMediumText()
            .HasComment(comment: "Value: The value to match against for the rule.");

        builder.Property(propertyExpression: tr => tr.MatchPolicy)
            .ConfigureShortText()
            .HasComment(comment: "MatchPolicy: The policy for matching (e.g., 'is_equal_to', 'contains').");

        builder.Property(propertyExpression: tr => tr.PropertyName)
            .ConfigureNameOptional(isRequired: false)
            .HasComment(comment: "PropertyName: The name of the product property if the rule type is 'product_property'.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the TaxonRule entity.
        builder.HasOne(navigationExpression: tr => tr.Taxon)
            .WithMany(navigationExpression: t => t.TaxonRules)
            .HasForeignKey(foreignKeyExpression: tr => tr.TaxonId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion
    }
}
