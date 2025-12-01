using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Catalog.Products.Variants;

public sealed class VariantConfiguration : IEntityTypeConfiguration<Variant>
{
    public void Configure(EntityTypeBuilder<Variant> builder)
    {
        #region Table
        builder.ToTable(name: Schema.Variants);
        #endregion

        #region Primary Key
        builder.HasKey(keyExpression: v => v.Id);
        #endregion

        #region Properties
        builder.Property(propertyExpression: v => v.Id)
            .HasColumnName(name: "id")
            .HasComment(comment: "Id: Unique identifier for the product variant. Value generated never.");

        builder.Property(propertyExpression: v => v.ProductId)
            .IsRequired()
            .HasComment(comment: "ProductId: Foreign key to the associated Product.");

        builder.Property(propertyExpression: v => v.Sku)
            .ConfigureInputOptional(maxLength: Variant.Constraints.SkuMaxLength)
            .HasComment(comment: "Sku: Stock Keeping Unit for the variant.");

        builder.Property(propertyExpression: v => v.Barcode)
            .ConfigureInputOptional(maxLength: Variant.Constraints.BarcodeMaxLength)
            .HasComment(comment: "Barcode: Unique code printed on product label for internal or store scanning.");

        builder.Property(propertyExpression: v => v.TrackInventory)
            .IsRequired()
            .HasDefaultValue(value: true)
            .HasComment(comment: "TrackInventory: Indicates if inventory should be tracked for this variant.");

        builder.Property(propertyExpression: v => v.IsMaster)
            .IsRequired()
            .HasComment(comment: "IsMaster: Indicates if this is the master variant.");

        builder.Property(propertyExpression: v => v.Weight)
            .HasColumnType(typeName: "decimal(18,4)")
            .IsRequired(required: false)
            .HasComment(comment: "Weight: The weight of the variant.");

        builder.Property(propertyExpression: v => v.Height)
            .HasColumnType(typeName: "decimal(18,4)")
            .IsRequired(required: false)
            .HasComment(comment: "Height: The height of the variant.");

        builder.Property(propertyExpression: v => v.Width)
            .HasColumnType(typeName: "decimal(18,4)")
            .IsRequired(required: false)
            .HasComment(comment: "Width: The width of the variant.");

        builder.Property(propertyExpression: v => v.Depth)
            .HasColumnType(typeName: "decimal(18,4)")
            .IsRequired(required: false)
            .HasComment(comment: "Depth: The depth of the variant.");

        builder.Property(propertyExpression: v => v.DimensionsUnit)
            .ConfigureInputOptional(maxLength: CommonInput.Constraints.Text.TinyTextMaxLength)
            .HasComment(comment: "DimensionsUnit: The unit of measurement for dimensions (e.g., mm, cm).");

        builder.Property(propertyExpression: v => v.WeightUnit)
            .ConfigureInputOptional(maxLength: CommonInput.Constraints.Text.TinyTextMaxLength)
            .HasComment(comment: "WeightUnit: The unit of measurement for weight (e.g., g, kg).");

        builder.Property(propertyExpression: v => v.CostPrice)
            .HasColumnType(typeName: "decimal(18,4)")
            .IsRequired(required: false)
            .HasComment(comment: "CostPrice: The cost price of the variant.");

        builder.ConfigurePosition();

        // Soft delete properties are now handled by ConfigureSoftDelete()
        builder.ConfigureSoftDelete();

        builder.Property(propertyExpression: v => v.DiscontinueOn)
            .IsRequired(required: false)
            .HasComment(comment: "DiscontinueOn: Date when the variant is discontinued.");

        builder.Property(propertyExpression: v => v.RowVersion)
            .IsRowVersion()
            .IsRequired(required: false)
            .HasComment(comment: "RowVersion: Concurrency token for optimistic locking.");

        builder.ConfigureMetadata();
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        builder.HasOne(navigationExpression: v => v.Product)
            .WithMany(navigationExpression: p => p.Variants)
            .HasForeignKey(foreignKeyExpression: v => v.ProductId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: v => v.OptionValueVariants)
            .WithOne(navigationExpression: ovv => ovv.Variant)
            .HasForeignKey(foreignKeyExpression: ovv => ovv.VariantId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: v => v.StockItems)
            .WithOne(navigationExpression: si => si.Variant)
            .HasForeignKey(foreignKeyExpression: si => si.VariantId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: v => v.Images)
            .WithOne(navigationExpression: pi => pi.Variant)
            .HasForeignKey(foreignKeyExpression: pi => pi.VariantId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade)
            .IsRequired(required: false);
        
        #endregion

        #region Indexes
        builder.HasIndex(indexExpression: v => v.ProductId);
        builder.HasIndex(indexExpression: v => v.Sku).IsUnique(unique: false); // SKU can be null, so not unique
        builder.HasIndex(indexExpression: v => v.Barcode).IsUnique(unique: false); // Barcode can be null, so not unique
        #endregion

        #region Ignored Properties
        builder.Ignore(propertyExpression: v => v.Deleted);
        builder.Ignore(propertyExpression: v => v.Discontinued);
        builder.Ignore(propertyExpression: v => v.Available);
        builder.Ignore(propertyExpression: v => v.Purchasable);
        builder.Ignore(propertyExpression: v => v.InStock);
        builder.Ignore(propertyExpression: v => v.Backorderable);
        builder.Ignore(propertyExpression: v => v.Backordered);
        builder.Ignore(propertyExpression: v => v.CanSupply);
        builder.Ignore(propertyExpression: v => v.ShouldTrackInventory);
        builder.Ignore(propertyExpression: v => v.HasPrice);
        builder.Ignore(propertyExpression: v => v.TotalOnHand);
        builder.Ignore(propertyExpression: v => v.OptionsText);
        builder.Ignore(propertyExpression: v => v.DescriptiveName);
        builder.Ignore(propertyExpression: v => v.DefaultImage);
        builder.Ignore(propertyExpression: v => v.SecondaryImage);
        builder.Ignore(propertyExpression: v => v.Volume);
        builder.Ignore(propertyExpression: v => v.Dimension);
        builder.Ignore(propertyExpression: v => v.OptionValues);
        builder.Ignore(propertyExpression: v => v.LineItems);
        builder.Ignore(propertyExpression: v => v.Orders);
        #endregion
    }
}