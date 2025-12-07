//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using ReSys.Core.Common.Domain.Concerns;
//using ReSys.Core.Domain.Constants;

//namespace ReSys.Core.Domain.Fulfillment;

//public sealed class FulfillmentLineItemConfiguration : IEntityTypeConfiguration<FulfillmentLineItem>
//{
//    public void Configure(EntityTypeBuilder<FulfillmentLineItem> builder)
//    {
//        // Table configuration
//        builder.ToTable(name: Schema.FulfillmentLineItems);

//        // Primary Key
//        builder.HasKey(fli => fli.Id);

//        // Properties
//        builder.Property(fli => fli.Id)
//            .ValueGeneratedNever();

//        builder.Property(fli => fli.FulfillmentOrderId)
//            .IsRequired();

//        builder.Property(fli => fli.LineItemId)
//            .IsRequired();

//        builder.Property(fli => fli.VariantId)
//            .IsRequired();

//        builder.Property(fli => fli.Quantity)
//            .IsRequired();

//        // Apply Auditable concern
//        builder.ConfigureAuditable();

//        // Relationships
//        builder.HasOne(fli => fli.FulfillmentOrder)
//            .WithMany(fo => fo.Items)
//            .HasForeignKey(fli => fli.FulfillmentOrderId)
//            .OnDelete(DeleteBehavior.Cascade);

//        builder.HasOne(fli => fli.Variant)
//            .WithMany()
//            .HasForeignKey(fli => fli.VariantId)
//            .OnDelete(DeleteBehavior.Restrict);

//        // Indexes
//        builder.HasIndex(fli => fli.FulfillmentOrderId);
//        builder.HasIndex(fli => fli.LineItemId);
//        builder.HasIndex(fli => fli.VariantId);
//    }
//}
