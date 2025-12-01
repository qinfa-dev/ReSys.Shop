using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Orders;

/// <summary>
/// Configures the database mapping for the <see cref="Order"/> entity.
/// </summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <summary>
    /// Configures the entity of type <see cref="Order"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        #region Table
        // Set the table name for the Order entity.
        builder.ToTable(name: Schema.Orders);
        #endregion

        #region Primary Key
        // Configure the primary key for the Order entity.
        builder.HasKey(keyExpression: o => o.Id);
        #endregion

        #region Properties
        // Configure properties for the Order entity.
        builder.Property(propertyExpression: o => o.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the order. Value generated never.");

        builder.Property(propertyExpression: o => o.StoreId)
            .IsRequired()
            .HasComment(comment: "StoreId: Foreign key to the associated Storefront.");

        builder.Property(propertyExpression: o => o.UserId)
            .IsRequired(required: false)
            .HasComment(comment: "UserId: Foreign key to the associated ApplicationUser.");

        builder.Property(propertyExpression: o => o.Number)
            .ConfigureInput(maxLength: 50)
            .HasComment(comment: "Number: Unique order number.");

        builder.Property(propertyExpression: o => o.State)
            .HasConversion<string>()
            .HasMaxLength(maxLength: 50)
            .IsRequired(required: true)
            .HasComment(comment: "State: Current state of the order (e.g., Cart, Complete).");

        builder.Property(propertyExpression: o => o.ItemTotalCents)
            .IsRequired()
            .HasDefaultValue(value: 0L)
            .HasComment(comment: "ItemTotalCents: Total amount of all line items in cents.");

        builder.Property(propertyExpression: o => o.ShipmentTotalCents)
            .IsRequired()
            .HasDefaultValue(value: 0L)
            .HasComment(comment: "ShipmentTotalCents: Total amount for shipping in cents.");

        builder.Property(propertyExpression: o => o.AdjustmentTotalCents)
            .IsRequired()
            .HasDefaultValue(value: 0L)
            .HasComment(comment: "AdjustmentTotalCents: Total amount for adjustments (e.g., discounts) in cents.");

        builder.Property(propertyExpression: o => o.TotalCents)
            .IsRequired()
            .HasDefaultValue(value: 0L)
            .HasComment(comment: "TotalCents: Grand total of the order in cents.");

        builder.Property(propertyExpression: o => o.Currency)
            .ConfigureCurrencyCode()
            .HasDefaultValue(value: "USD")
            .HasComment(comment: "Currency: The currency of the order (e.g., USD, EUR).");

        builder.Property(propertyExpression: o => o.Email)
            .ConfigureEmail(isRequired: false)
            .HasComment(comment: "Email: Customer's email address.");

        builder.Property(propertyExpression: o => o.SpecialInstructions)
            .ConfigureInputOptional(maxLength: Order.Constraints.SpecialInstructionsMaxLength)
            .HasComment(comment: "SpecialInstructions: Any special instructions for the order.");

        builder.Property(propertyExpression: o => o.CompletedAt)
            .IsRequired(required: false)
            .HasComment(comment: "CompletedAt: Timestamp when the order was completed.");

        builder.Property(propertyExpression: o => o.CanceledAt)
            .IsRequired(required: false)
            .HasComment(comment: "CanceledAt: Timestamp when the order was canceled.");

        builder.Property(propertyExpression: o => o.PromotionId)
            .IsRequired(required: false)
            .HasComment(comment: "PromotionId: Foreign key to the associated Promotion.");

        builder.Property(propertyExpression: o => o.ShippingMethodId)
            .IsRequired(required: false)
            .HasComment(comment: "ShippingMethodId: Foreign key to the associated ShippingMethod.");

        builder.Property(propertyExpression: o => o.FulfillmentLocationId)
            .IsRequired(required: false)
            .HasComment(comment: "FulfillmentLocationId: Foreign key to the warehouse (StockLocation) for order fulfillment.");

        builder.Property(propertyExpression: o => o.PromoCode)
            .ConfigureInputOptional(maxLength: Order.Constraints.PromoCodeMaxLength)
            .HasComment(comment: "PromoCode: Promotional code applied to the order.");

        // Apply common configurations using extension methods.
        builder.ConfigureMetadata();
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the Order entity.
        builder.HasOne(navigationExpression: o => o.Store)
            .WithMany(navigationExpression: s => s.Orders)
            .HasForeignKey(foreignKeyExpression: o => o.StoreId)
            .OnDelete(deleteBehavior: DeleteBehavior.Restrict); // Prevent deleting store if orders exist

        builder.HasOne(navigationExpression: o => o.User)
            .WithMany(navigationExpression: u => u.Orders)
            .HasForeignKey(foreignKeyExpression: o => o.UserId)
            .IsRequired(required: false)
            .OnDelete(deleteBehavior: DeleteBehavior.SetNull); // User can be deleted, but order remains

        builder.HasOne(navigationExpression: o => o.Promotion)
            .WithMany(navigationExpression: p => p.Orders)
            .HasForeignKey(foreignKeyExpression: o => o.PromotionId)
            .IsRequired(required: false)
            .OnDelete(deleteBehavior: DeleteBehavior.SetNull); // Promotion can be deleted, but order remains

        builder.HasOne(navigationExpression: o => o.FulfillmentLocation)
            .WithMany()
            .HasForeignKey(foreignKeyExpression: o => o.FulfillmentLocationId)
            .IsRequired(required: false)
            .OnDelete(deleteBehavior: DeleteBehavior.SetNull); // Warehouse can be deleted, but order record remains

        builder.HasMany(navigationExpression: o => o.LineItems)
            .WithOne(navigationExpression: li => li.Order)
            .HasForeignKey(foreignKeyExpression: li => li.OrderId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: o => o.Adjustments)
            .WithOne(navigationExpression: oa => oa.Order)
            .HasForeignKey(foreignKeyExpression: oa => oa.OrderId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: o => o.Shipments)
            .WithOne(navigationExpression: s => s.Order)
            .HasForeignKey(foreignKeyExpression: s => s.OrderId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: o => o.Payments)
            .WithOne(navigationExpression: p => p.Order)
            .HasForeignKey(foreignKeyExpression: p => p.OrderId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: o => o.StoreId);
        builder.HasIndex(indexExpression: o => o.UserId);
        builder.HasIndex(indexExpression: o => o.PromotionId);
        builder.HasIndex(indexExpression: o => o.ShippingMethodId);
        builder.HasIndex(indexExpression: o => o.FulfillmentLocationId);
        builder.HasIndex(indexExpression: o => o.Number).IsUnique();
        builder.HasIndex(indexExpression: o => o.State);
        builder.HasIndex(indexExpression: o => o.CompletedAt);
        #endregion

        #region Ignored Properties
        // Ignore domain-specific or computed properties that should not be mapped to the database.
        builder.Ignore(propertyExpression: o => o.IsCart);
        builder.Ignore(propertyExpression: o => o.IsComplete);
        builder.Ignore(propertyExpression: o => o.IsCanceled);
        builder.Ignore(propertyExpression: o => o.ItemCount);
        builder.Ignore(propertyExpression: o => o.Total);
        builder.Ignore(propertyExpression: o => o.ItemTotal);
        builder.Ignore(propertyExpression: o => o.ShipmentTotal);
        builder.Ignore(propertyExpression: o => o.TotalWeight);
        builder.Ignore(propertyExpression: o => o.PromotionTotalCents);
        builder.Ignore(propertyExpression: o => o.PromotionTotal);
        builder.Ignore(propertyExpression: o => o.HasPromotion);
        builder.Ignore(propertyExpression: o => o.IsFullyDigital);
        #endregion
    }
}