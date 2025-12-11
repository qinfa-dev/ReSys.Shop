namespace ReSys.Core.Domain.Orders.Returns;

/// <summary>
/// Entity Framework Core configuration for ReturnItem aggregate.
/// </summary>
public sealed class ReturnItemConfiguration : IEntityTypeConfiguration<ReturnItem>
{
    public void Configure(EntityTypeBuilder<ReturnItem> builder)
    {
        builder.ToTable(name: "return_items");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName(name: "id")
            .ValueGeneratedNever();

        builder.Property(e => e.InventoryUnitId)
            .HasColumnName(name: "inventory_unit_id")
            .IsRequired();

        builder.Property(e => e.ReturnAuthorizationId)
            .HasColumnName(name: "return_authorization_id")
            .IsRequired(false);

        builder.Property(e => e.CustomerReturnId)
            .HasColumnName(name: "customer_return_id")
            .IsRequired(false);

        builder.Property(e => e.ReimbursementId)
            .HasColumnName(name: "reimbursement_id")
            .IsRequired(false);

        builder.Property(e => e.ExchangeVariantId)
            .HasColumnName(name: "exchange_variant_id")
            .IsRequired(false);

        builder.Property(e => e.ReturnQuantity)
            .HasColumnName(name: "return_quantity")
            .IsRequired();

        builder.Property(e => e.PreTaxAmountCents)
            .HasColumnName(name: "pre_tax_amount_cents")
            .IsRequired();

        builder.Property(e => e.ReceptionStatus)
            .HasColumnName(name: "reception_status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.AcceptanceStatus)
            .HasColumnName(name: "acceptance_status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Resellable)
            .HasColumnName(name: "resellable")
            .IsRequired();

        builder.Property(e => e.AcceptanceStatusErrors)
            .HasColumnName(name: "acceptance_status_errors")
            .HasColumnType(typeName: "jsonb")
            .IsRequired(false);

        // NEW: Optimistic Concurrency Token
        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .HasComment(comment: "RowVersion: Used for optimistic concurrency control.");

        // Audit fields
        builder.Property(e => e.CreatedAt)
            .HasColumnName(name: "created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName(name: "updated_at")
            .IsRequired(false);

        // Relationships
        builder.HasOne(e => e.InventoryUnit)
            .WithMany()
            .HasForeignKey(e => e.InventoryUnitId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.ExchangeVariant)
            .WithMany()
            .HasForeignKey(e => e.ExchangeVariantId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        // Indexes
        builder.HasIndex(e => e.InventoryUnitId)
            .HasDatabaseName(name: "idx_return_items_inventory_unit_id");

        builder.HasIndex(e => e.ReturnAuthorizationId)
            .HasDatabaseName(name: "idx_return_items_return_authorization_id");

        builder.HasIndex(e => e.CustomerReturnId)
            .HasDatabaseName(name: "idx_return_items_customer_return_id");

        builder.HasIndex(e => e.ReimbursementId)
            .HasDatabaseName(name: "idx_return_items_reimbursement_id");

        builder.HasIndex(e => e.ExchangeVariantId)
            .HasDatabaseName(name: "idx_return_items_exchange_variant_id");

        builder.HasIndex(e => e.ReceptionStatus)
            .HasDatabaseName(name: "idx_return_items_reception_status");

        builder.HasIndex(e => e.AcceptanceStatus)
            .HasDatabaseName(name: "idx_return_items_acceptance_status");

        // Composite indexes for common queries
        builder.HasIndex(e => new { e.InventoryUnitId, e.ReceptionStatus })
            .HasDatabaseName(name: "idx_return_items_inventory_unit_reception_status");

        builder.HasIndex(e => new { e.ReceptionStatus, e.AcceptanceStatus })
            .HasDatabaseName(name: "idx_return_items_status_compound");

        // Query filters
        // Note: Soft delete or active filters would go here if implemented
        // Example:
        // builder.HasQueryFilter(e => e.ReceptionStatus != ReturnItem.ReturnReceptionStatus.Cancelled);
    }
}
