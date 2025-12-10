using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Inventories.StorePickups;

/// <summary>
/// Configures the EF Core mapping for the <see cref="StorePickup"/> aggregate root.
/// Defines table structure, column properties, relationships, and indexes for optimal querying.
/// </summary>
public sealed class StorePickupConfiguration : IEntityTypeConfiguration<StorePickup>
{
    public void Configure(EntityTypeBuilder<StorePickup> builder)
    {
        #region Table
        builder.ToTable(name: Schema.StorePickups);
        #endregion

        #region Primary Key
        builder.HasKey(keyExpression: p => p.Id);
        #endregion

        #region Properties
        builder.Property(propertyExpression: p => p.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the store pickup. Value generated never.");

        builder.Property(propertyExpression: p => p.OrderId)
            .IsRequired()
            .HasComment(comment: "OrderId: The ID of the order associated with this store pickup.");

        builder.Property(propertyExpression: p => p.StockLocationId)
            .IsRequired()
            .HasComment(comment: "StockLocationId: The ID of the stock location where the pickup will occur.");

        builder.Property(propertyExpression: p => p.State)
            .IsRequired()
            .HasConversion(
                v => (int)v,
                v => (StorePickup.PickupState)v)
            .HasComment(comment: "State: The current state of the store pickup (e.g., pending, ready, picked_up, cancelled).");

        builder.Property(propertyExpression: p => p.PickupCode)
            .IsRequired()
            .HasMaxLength(maxLength: StorePickup.Constraints.PickupCodeMaxLength)
            .HasComment(comment: "PickupCode: A unique, human-readable code for the customer to use for pickup.");

        builder.Property(propertyExpression: p => p.ScheduledPickupTime)
            .HasComment(comment: "ScheduledPickupTime: The time the customer is expected to pick up the order.");

        builder.Property(propertyExpression: p => p.ReadyAt)
            .HasComment(comment: "ReadyAt: The timestamp when the pickup order was marked as ready.");

        builder.Property(propertyExpression: p => p.PickedUpAt)
            .HasComment(comment: "PickedUpAt: The timestamp when the order was successfully picked up by the customer.");

        builder.Property(propertyExpression: p => p.CancelledAt)
            .HasComment(comment: "CancelledAt: The timestamp when the store pickup was cancelled.");

        builder.Property(propertyExpression: p => p.CancellationReason)
            .HasMaxLength(maxLength: StorePickup.Constraints.CancellationReasonMaxLength)
            .HasComment(comment: "CancellationReason: The reason provided if the store pickup was cancelled.");

        builder.Property(propertyExpression: p => p.CreatedAt)
            .IsRequired()
            .HasComment(comment: "CreatedAt: The timestamp when the store pickup record was created.");

        builder.Property(propertyExpression: p => p.UpdatedAt)
            .HasComment(comment: "UpdatedAt: The timestamp when the store pickup record was last updated.");
        #endregion

        #region Relationships

        builder.HasOne(navigationExpression: p => p.StockLocation)
            .WithMany(sl => sl.StorePickups)
            .HasForeignKey(foreignKeyExpression: p => p.StockLocationId)
            .OnDelete(deleteBehavior: DeleteBehavior.Restrict);

        // Note: Order relationship is not directly modeled in this configuration
        // because Order.Pickups would need to be a collection property.
        // If Order navigation is needed, configure from OrderConfiguration instead.
        #endregion

        #region Indexes

        builder.HasIndex(indexExpression: p => p.OrderId);
        builder.HasIndex(indexExpression: p => p.StockLocationId);
        builder.HasIndex(indexExpression: p => p.PickupCode);
        builder.HasIndex(indexExpression: p => new { p.StockLocationId, p.State });
        builder.HasIndex(indexExpression: p => new { p.State, p.CreatedAt });
        builder.HasIndex(indexExpression: p => p.State);
        builder.HasIndex(indexExpression: p => new { p.StockLocationId, p.State, p.ScheduledPickupTime });
        #endregion

        #region Ignored Properties
        // Domain events are handled by the base Aggregate class and EF Core interceptors
        // No additional configuration needed here
        
        // Note: Configure navigation to UncommittedEvents if using automatic event publishing:
        builder.Ignore(propertyExpression: p => p.DomainEvents);
        #endregion
    }
}
