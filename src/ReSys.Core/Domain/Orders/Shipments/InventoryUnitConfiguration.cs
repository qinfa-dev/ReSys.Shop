using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ReSys.Core.Domain.Orders.Shipments;

/// <summary>
/// Entity Framework Core configuration for InventoryUnit entity.
/// </summary>
public sealed class InventoryUnitConfiguration : IEntityTypeConfiguration<InventoryUnit>
{
    public void Configure(EntityTypeBuilder<InventoryUnit> builder)
    {
        builder.ToTable(name: "inventory_units");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.VariantId)
            .IsRequired();

        builder.Property(e => e.OrderId)
            .IsRequired();

        builder.Property(e => e.LineItemId)
            .IsRequired();

        builder.Property(e => e.ShipmentId)
            .IsRequired();

        builder.Property(e => e.StockLocationId)
            .IsRequired(false);

        builder.Property(e => e.State)
            .HasConversion<int>()
            .IsRequired();
            
        builder.Property(e => e.StateChangedAt)
            .IsRequired();

        builder.Property(e => e.OriginalReturnItemId)
            .IsRequired(false);

        builder.Property(e => e.RowVersion)
            .IsRowVersion();

        // Audit fields
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired(false);

        // Relationships
        builder.HasOne(e => e.Variant)
            .WithMany()
            .HasForeignKey(e => e.VariantId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.Order)
            .WithMany()
            .HasForeignKey(e => e.OrderId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.LineItem)
            .WithMany()
            .HasForeignKey(e => e.LineItemId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Shipment)
            .WithMany(s => s.InventoryUnits)
            .HasForeignKey(e => e.ShipmentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.StockLocation)
            .WithMany()
            .HasForeignKey(e => e.StockLocationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        // Return items relationship
        builder.HasMany(e => e.ReturnItems)
            .WithOne(ri => ri.InventoryUnit)
            .HasForeignKey(ri => ri.InventoryUnitId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // Exchange units relationship (self-referencing)
        builder.HasMany(e => e.ExchangeUnits)
            .WithOne()
            .HasForeignKey(e => e.OriginalReturnItemId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.OriginalReturnItem)
            .WithMany()
            .HasForeignKey(e => e.OriginalReturnItemId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes
        builder.HasIndex(e => e.OrderId);
        builder.HasIndex(e => e.LineItemId);
        builder.HasIndex(e => e.VariantId);
        builder.HasIndex(e => e.ShipmentId);
        builder.HasIndex(e => e.StockLocationId);
        builder.HasIndex(e => e.State);
        builder.HasIndex(e => new { e.OrderId, e.State });
        builder.HasIndex(e => new { e.VariantId, e.State });
    }
}
