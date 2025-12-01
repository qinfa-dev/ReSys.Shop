using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Stores.StockLocations;

/// <summary>
/// Configures the database mapping for the <see cref="StoreStockLocation"/> entity.
/// </summary>
public sealed class StoreStockLocationConfiguration : IEntityTypeConfiguration<StoreStockLocation>
{
    /// <summary>
    /// Configures the entity of type <see cref="StoreStockLocation"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<StoreStockLocation> builder)
    {
        #region Table
        // Set the table name for the StockLocationStore entity.
        builder.ToTable(name: Schema.StockLocationStores);
        #endregion

        #region Primary Key
        // Configure the primary key for the StockLocationStore entity.
        builder.HasKey(keyExpression: sls => sls.Id);
        #endregion

        #region Properties
        // Configure properties for the StockLocationStore entity.
        builder.Property(propertyExpression: sls => sls.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the stock location store link. Value generated never.");

        builder.Property(propertyExpression: sls => sls.StockLocationId)
            .IsRequired()
            .HasComment(comment: "StockLocationId: Foreign key to the associated StockLocation.");

        builder.Property(propertyExpression: sls => sls.StoreId)
            .IsRequired()
            .HasComment(comment: "StoreId: Foreign key to the associated Storefront.");

        builder.Property(propertyExpression: sls => sls.Priority)
            .IsRequired()
            .HasComment(comment: "Priority: The priority of this stock location for the store.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable(); // For CreatedAt, UpdatedAt
        #endregion

        #region Relationships
        builder.HasOne(navigationExpression: sls => sls.StockLocation)
            .WithMany(navigationExpression: sl => sl.StoreStockLocations)
            .HasForeignKey(foreignKeyExpression: sls => sls.StockLocationId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasOne(navigationExpression: sls => sls.Store)
            .WithMany(navigationExpression: s => s.StoreStockLocations)
            .HasForeignKey(foreignKeyExpression: sls => sls.StoreId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion
    }
}
