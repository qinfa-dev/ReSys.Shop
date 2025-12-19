namespace ReSys.Core.Domain.Orders.Shipments;

/// <summary>
/// Entity Framework Core configuration for InventoryUnit entity.
/// </summary>
public sealed class InventoryUnitConfiguration : IEntityTypeConfiguration<InventoryUnit>
{
    public void Configure(EntityTypeBuilder<InventoryUnit> builder)
    {
        #region Table
        // Set the table name for the InventoryUnit entity.
        builder.ToTable(name: Schema.InventoryUnits);
        #endregion

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.VariantId)
            .IsRequired();

        builder.Property(e => e.LineItemId)
            .IsRequired();

        builder.Property(e => e.ShipmentId)
            .IsRequired(false); // NULLABLE: Assigned only when shipment is allocated
        
        builder.Property(e => e.State)
            .HasConversion<int>()
            .IsRequired();
            
        builder.Property(e => e.StateChangedAt)
            .IsRequired();

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

        builder.HasOne(e => e.LineItem)
            .WithMany()
            .HasForeignKey(e => e.LineItemId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Shipment)
            .WithMany(s => s.InventoryUnits)
            .HasForeignKey(e => e.ShipmentId)
            .IsRequired(false) // NULLABLE: Shipment assigned during allocation
            .OnDelete(DeleteBehavior.SetNull); // SetNull instead of Cascade when shipment deleted

        // Indexes
        builder.HasIndex(e => e.LineItemId);
        builder.HasIndex(e => e.VariantId);
        builder.HasIndex(e => e.ShipmentId);
        builder.HasIndex(e => e.State);
        builder.HasIndex(e => new { e.VariantId, e.State });
        
        #region Ignored Properties
        builder.Ignore(propertyExpression: s => s.Order);
        builder.Ignore(propertyExpression: s => s.OrderId);
        builder.Ignore(propertyExpression: s => s.StockLocation);
        builder.Ignore(propertyExpression: s => s.StockLocationId);
        builder.Ignore(propertyExpression: s => s.IsPreShipment);
        builder.Ignore(propertyExpression: s => s.IsPostShipment);
        builder.Ignore(propertyExpression: s => s.IsCancelable);
        builder.Ignore(propertyExpression: s => s.IsShippable);
        builder.Ignore(propertyExpression: s => s.IsBackordered);
        builder.Ignore(propertyExpression: s => s.IsShipped);
        builder.Ignore(propertyExpression: s => s.IsCanceled);
        builder.Ignore(propertyExpression: s => s.AllowShip);
        #endregion
    }
}
