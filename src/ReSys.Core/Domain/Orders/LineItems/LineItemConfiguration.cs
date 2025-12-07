using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Orders.LineItems;

/// <summary>
/// Configures the database mapping for the <see cref="LineItem"/> entity.
/// </summary>
public sealed class LineItemConfiguration : IEntityTypeConfiguration<LineItem>
{
    /// <summary>
    /// Configures the entity of type <see cref="LineItem"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<LineItem> builder)
    {
        #region Table
        // Set the table name for the LineItem entity.
        builder.ToTable(name: Schema.LineItems);
        #endregion

        #region Primary Key
        // Configure the primary key for the LineItem entity.
        builder.HasKey(keyExpression: li => li.Id);
        #endregion

        #region Properties
        // Configure properties for the LineItem entity.
        builder.Property(propertyExpression: li => li.Id)
            .HasColumnName(name: "id")
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the line item. Value generated never.");

        builder.Property(propertyExpression: li => li.OrderId)
            .IsRequired()
            .HasComment(comment: "OrderId: Foreign key to the associated Order.");

        builder.Property(propertyExpression: li => li.VariantId)
            .IsRequired()
            .HasComment(comment: "VariantId: Foreign key to the associated Product Variant.");

        builder.Property(propertyExpression: li => li.Quantity)
            .IsRequired()
            .HasComment(comment: "Quantity: Number of units of the product variant.");

        builder.Property(propertyExpression: li => li.PriceCents)
            .IsRequired()
            .HasComment(comment: "PriceCents: Price of a single unit in cents at the time of order.");

        builder.Property(propertyExpression: li => li.Currency)
            .HasMaxLength(maxLength: LineItem.Constraints.CurrencyMaxLength)
            .IsRequired()
            .HasComment(comment: "Currency: The currency of the line item.");

        builder.Property(propertyExpression: li => li.CapturedName)
            .HasMaxLength(maxLength: LineItem.Constraints.CapturedNameMaxLength)
            .IsRequired()
            .HasComment(comment: "CapturedName: Name of the product variant at the time of order.");

        builder.Property(propertyExpression: li => li.CapturedSku)
            .HasMaxLength(maxLength: LineItem.Constraints.CapturedSkuMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "CapturedSku: SKU of the product variant at the time of order.");

        builder.Property(propertyExpression: li => li.IsPromotional)
            .IsRequired()
            .HasDefaultValue(value: false)
            .HasComment(comment: "IsPromotional: Indicates if this line item was part of a promotion.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the LineItem entity.
        builder.HasOne(navigationExpression: li => li.Order)
            .WithMany(navigationExpression: o => o.LineItems)
            .HasForeignKey(foreignKeyExpression: li => li.OrderId)
            .OnDelete(deleteBehavior: DeleteBehavior.Restrict); // Restrict to prevent deleting order if line items exist

        builder.HasOne(navigationExpression: li => li.Variant)
            .WithMany(navigationExpression: v => v.LineItems)
            .HasForeignKey(foreignKeyExpression: li => li.VariantId)
            .OnDelete(deleteBehavior: DeleteBehavior.Restrict); // Restrict to prevent deleting variant if line items exist


        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: li => li.OrderId);
        builder.HasIndex(indexExpression: li => li.VariantId);
        #endregion

        #region Ignored Properties
        // Ignore domain-specific or computed properties that should not be mapped to the database.
        builder.Ignore(propertyExpression: li => li.SubtotalCents);
        builder.Ignore(propertyExpression: li => li.Subtotal);
        builder.Ignore(propertyExpression: li => li.UnitPrice);
        builder.Ignore(propertyExpression: li => li.TotalCents);
        builder.Ignore(propertyExpression: li => li.Total);
        builder.Ignore(propertyExpression: li => li.InventoryUnits);
        #endregion
    }
}
