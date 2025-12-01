using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Catalog.Products.Prices;

/// <summary>
/// Configures the database mapping for the <see cref="Price"/> entity.
/// </summary>
public sealed class PriceConfiguration : IEntityTypeConfiguration<Price>
{
    /// <summary>
    /// Configures the entity of type <see cref="Price"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<Price> builder)
    {
        #region Table
        // Set the table name for the Price entity.
        builder.ToTable(name: Schema.Prices);
        #endregion

        #region Primary Key
        // Configure the primary key for the Price entity.
        builder.HasKey(keyExpression: p => p.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: p => p.VariantId);
        builder.HasIndex(indexExpression: p => p.Currency);
        #endregion

        #region Properties
        // Configure properties for the Price entity.
        builder.Property(propertyExpression: p => p.Id)
            .HasColumnName(name: "id")
            .HasComment(comment: "Id: Unique identifier for the price. Value generated never.");

        builder.Property(propertyExpression: p => p.VariantId)
            .IsRequired()
            .HasComment(comment: "VariantId: Foreign key to the associated Product Variant.");

        builder.Property(propertyExpression: p => p.Amount)
            .HasColumnType(typeName: "decimal(18,4)")
            .IsRequired(required: false)
            .HasComment(comment: "Amount: The current price of the product variant.");

        builder.Property(propertyExpression: p => p.Currency)
            .HasMaxLength(maxLength: CommonInput.Constraints.CurrencyAndLanguage.CurrencyCodeLength)
            .IsRequired()
            .HasComment(comment: "Currency: The currency of the price (e.g., 'USD', 'EUR').");

        builder.Property(propertyExpression: p => p.CompareAtAmount)
            .HasColumnType(typeName: "decimal(18,4)")
            .IsRequired(required: false)
            .HasComment(comment: "CompareAtAmount: The original price for comparison, indicating a sale or discount.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the Price entity.
        builder.HasOne(navigationExpression: p => p.Variant)
            .WithMany(navigationExpression: v => v.Prices)
            .HasForeignKey(foreignKeyExpression: p => p.VariantId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion
    }
}
