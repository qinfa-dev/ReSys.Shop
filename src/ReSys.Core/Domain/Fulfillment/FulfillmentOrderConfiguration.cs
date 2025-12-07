//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using ReSys.Core.Common.Domain.Concerns;
//using ReSys.Core.Domain.Constants;

//namespace ReSys.Core.Domain.Fulfillment;

//public sealed class FulfillmentOrderConfiguration : IEntityTypeConfiguration<FulfillmentOrder>
//{
//    public void Configure(EntityTypeBuilder<FulfillmentOrder> builder)
//    {
//        // Table configuration
//        builder.ToTable(name: Schema.FulfillmentOrders);

//        // Primary Key
//        builder.HasKey(fo => fo.Id);

//        // Properties
//        builder.Property(fo => fo.Id)
//            .ValueGeneratedNever();

//        builder.Property(fo => fo.OrderId)
//            .IsRequired();

//        builder.Property(fo => fo.StockLocationId)
//            .IsRequired();

//        builder.Property(fo => fo.State)
//            .HasConversion<string>()
//            .HasMaxLength(50)
//            .IsRequired();

//        builder.Property(fo => fo.PackageId)
//            .HasMaxLength(255);

//        builder.Property(fo => fo.TrackingNumber)
//            .HasMaxLength(255);

//        builder.Property(fo => fo.AllocatedAt);
//        builder.Property(fo => fo.PickingStartedAt);
//        builder.Property(fo => fo.PickedAt);
//        builder.Property(fo => fo.PackedAt);
//        builder.Property(fo => fo.ReadyToShipAt);
//        builder.Property(fo => fo.ShippedAt);
//        builder.Property(fo => fo.DeliveredAt);
//        builder.Property(fo => fo.CanceledAt);

//        // Apply Auditable concern
//        builder.ConfigureAuditable();

//        // Relationships
//        builder.HasOne(fo => fo.Order)
//            .WithMany() // Assuming Order does not have a collection of FulfillmentOrders for now
//            .HasForeignKey(fo => fo.OrderId)
//            .OnDelete(DeleteBehavior.Restrict);

//        builder.HasOne(fo => fo.StockLocation)
//            .WithMany()
//            .HasForeignKey(fo => fo.StockLocationId)
//            .OnDelete(DeleteBehavior.Restrict);

//        builder.HasMany(fo => fo.Items)
//            .WithOne(fli => fli.FulfillmentOrder)
//            .HasForeignKey(fli => fli.FulfillmentOrderId)
//            .OnDelete(DeleteBehavior.Cascade);

//        // Indexes
//        builder.HasIndex(fo => fo.OrderId);
//        builder.HasIndex(fo => fo.StockLocationId);
//        builder.HasIndex(fo => fo.State);
//    }
//}
