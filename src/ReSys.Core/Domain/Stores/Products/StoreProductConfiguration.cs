using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Stores.Products;

/// <summary>
/// Configures the database mapping for the <see cref="StoreProduct"/> entity.
/// </summary>
public sealed class StoreProductConfiguration : IEntityTypeConfiguration<StoreProduct>
{
    /// <summary>
    /// Configures the entity of type <see cref="StoreProduct"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<StoreProduct> builder)
    {
        #region Table
        // Set the table name for the StoreProduct entity.
        builder.ToTable(name: Schema.StoreProducts);
        #endregion

        #region Primary Key
        // Configure the primary key for the StoreProduct entity.
        builder.HasKey(keyExpression: sp => sp.Id);
        #endregion

        #region Properties
        // Configure properties for the StoreProduct entity.
        builder.Property(propertyExpression: sp => sp.Id)
            .HasColumnName(name: "id")
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the store product link. Value generated never.");

        builder.Property(propertyExpression: sp => sp.StoreId)
            .IsRequired(required: false)
            .HasComment(comment: "StoreId: Foreign key to the associated Storefront.");

        builder.Property(propertyExpression: sp => sp.ProductId)
            .IsRequired()
            .HasComment(comment: "ProductId: Foreign key to the associated Product.");

        builder.Property(propertyExpression: sp => sp.Visible)
            .IsRequired()
            .HasDefaultValue(value: true)
            .HasComment(comment: "Visible: Indicates if the product is visible in the store.");

        builder.Property(propertyExpression: sp => sp.Featured)
            .IsRequired()
            .HasDefaultValue(value: false)
            .HasComment(comment: "Featured: Indicates if the product is featured in the store.");

        // Apply common configurations using extension methods.
        builder.ConfigurePosition();
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the StoreProduct entity.
        builder.HasOne(navigationExpression: sp => sp.Store)
            .WithMany(navigationExpression: s => s.StoreProducts)
            .HasForeignKey(foreignKeyExpression: sp => sp.StoreId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasOne(navigationExpression: sp => sp.Product)
            .WithMany(navigationExpression: p => p.StoreProducts)
            .HasForeignKey(foreignKeyExpression: sp => sp.ProductId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: sp => new { sp.StoreId, sp.ProductId }).IsUnique();
        builder.HasIndex(indexExpression: sp => sp.ProductId);
        builder.HasIndex(indexExpression: sp => sp.StoreId);
        builder.HasIndex(indexExpression: sp => sp.Visible);
        builder.HasIndex(indexExpression: sp => sp.Featured);
        #endregion
    }
}