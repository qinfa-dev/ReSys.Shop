using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Orders.Shipments;

/// <summary>
/// Configures the database mapping for the <see cref="Shipment"/> entity.
/// </summary>
public sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    /// <summary>
    /// Configures the entity of type <see cref="Shipment"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        #region Table
        // Set the table name for the Shipment entity.
        builder.ToTable(name: Schema.Shipments);
        #endregion

        #region Primary Key
        // Configure the primary key for the Shipment entity.
        builder.HasKey(keyExpression: s => s.Id);
        #endregion

        #region Properties
        // Configure properties for the Shipment entity.
        builder.Property(propertyExpression: s => s.Id)
            .HasColumnName(name: "id")
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the shipment. Value generated never.");

        builder.Property(propertyExpression: s => s.OrderId)
            .IsRequired()
            .HasComment(comment: "OrderId: Foreign key to the associated Order.");

        builder.Property(propertyExpression: s => s.Number)
            .HasMaxLength(maxLength: 50)
            .IsRequired()
            .HasComment(comment: "Number: Unique shipment number.");

        builder.Property(propertyExpression: s => s.State)
            .HasConversion<string>()
            .HasMaxLength(maxLength: 50)
            .IsRequired()
            .HasComment(comment: "State: Current state of the shipment (e.g., Pending, Shipped).");

        builder.Property(propertyExpression: s => s.TrackingNumber)
            .HasMaxLength(maxLength: 100)
            .IsRequired(required: false)
            .HasComment(comment: "TrackingNumber: The tracking number for the shipment.");

        builder.Property(propertyExpression: s => s.ShippedAt)
            .IsRequired(required: false)
            .HasComment(comment: "ShippedAt: Timestamp when the shipment was shipped.");

        builder.Property(propertyExpression: s => s.DeliveredAt)
            .IsRequired(required: false)
            .HasComment(comment: "DeliveredAt: Timestamp when the shipment was delivered.");

        builder.Property(propertyExpression: s => s.ShippingMethodId)
            .IsRequired()
            .HasComment(comment: "ShippingMethodId: Foreign key to the associated ShippingMethod.");

        builder.Property(propertyExpression: s => s.StockLocationId)
            .IsRequired(required: false)
            .HasComment(comment: "StockLocationId: Foreign key to the warehouse (StockLocation) from which this shipment is fulfilled.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the Shipment entity.
        builder.HasOne(navigationExpression: s => s.Order)
            .WithMany(navigationExpression: o => o.Shipments)
            .HasForeignKey(foreignKeyExpression: s => s.OrderId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasOne(navigationExpression: s => s.ShippingMethod)
            .WithMany(navigationExpression: sm => sm.Shipments)
            .HasForeignKey(foreignKeyExpression: s => s.ShippingMethodId)
            .OnDelete(deleteBehavior: DeleteBehavior.Restrict); // Prevent deleting shipping method if shipments exist

        builder.HasOne(navigationExpression: s => s.StockLocation)
            .WithMany()
            .HasForeignKey(foreignKeyExpression: s => s.StockLocationId)
            .IsRequired(required: false)
            .OnDelete(deleteBehavior: DeleteBehavior.SetNull); // Warehouse can be deleted, but shipment record remains
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: s => s.OrderId);
        builder.HasIndex(indexExpression: s => s.StockLocationId);
        builder.HasIndex(indexExpression: s => s.Number).IsUnique();
        builder.HasIndex(indexExpression: s => s.State);
        #endregion

        #region Ignored Properties
        // Ignore domain-specific or computed properties that should not be mapped to the database.
        builder.Ignore(propertyExpression: s => s.IsShipped);
        builder.Ignore(propertyExpression: s => s.IsDelivered);
        builder.Ignore(propertyExpression: s => s.IsCanceled);
        builder.Ignore(propertyExpression: s => s.IsPending);
        builder.Ignore(propertyExpression: s => s.IsReady);
        #endregion
    }
}