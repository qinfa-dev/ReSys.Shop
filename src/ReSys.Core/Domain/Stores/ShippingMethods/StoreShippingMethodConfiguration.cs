using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Stores.ShippingMethods;
/// <summary>
/// Configures the database mapping for the <see cref="StoreShippingMethod"/> entity.
/// </summary>
public sealed class StoreShippingMethodConfiguration : IEntityTypeConfiguration<StoreShippingMethod>
{
    /// <summary>
    /// Configures the entity of type <see cref="StoreShippingMethod"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<StoreShippingMethod> builder)
    {
        #region Table
        // Set the table name for the StoreShippingMethod entity.
        builder.ToTable(name: Schema.StoreShippingMethods);
        #endregion

        #region Primary Key
        // Configure the primary key for the StoreShippingMethod entity.
        builder.HasKey(keyExpression: ssm => ssm.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: ssm => ssm.StoreId);
        builder.HasIndex(indexExpression: ssm => ssm.ShippingMethodId);
        builder.HasIndex(indexExpression: ssm => new { ssm.StoreId, ssm.ShippingMethodId }).IsUnique();
        #endregion

        #region Properties
        // Configure properties for the StoreShippingMethod entity.
        builder.Property(propertyExpression: ssm => ssm.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the store shipping method. Value generated never.");

        builder.Property(propertyExpression: ssm => ssm.StoreId)
            .IsRequired()
            .HasComment(comment: "StoreId: Foreign key to the associated Storefront.");

        builder.Property(propertyExpression: ssm => ssm.ShippingMethodId)
            .IsRequired()
            .HasComment(comment: "ShippingMethodId: Foreign key to the associated ShippingMethod.");

        builder.Property(propertyExpression: ssm => ssm.Available)
            .IsRequired()
            .HasComment(comment: "Available: Indicates if the shipping method is available for this store.");

        builder.Property(propertyExpression: ssm => ssm.StoreBaseCost)
            .HasColumnType(typeName: "decimal(18,2)")
            .IsRequired(required: false)
            .HasComment(comment: "StoreBaseCost: The base cost of the shipping method for this specific store.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable(); // For CreatedAt, UpdatedAt
        #endregion

        #region Relationships
        // Configure relationships for the StoreShippingMethod entity.
        builder.HasOne(navigationExpression: ssm => ssm.ShippingMethod)
            .WithMany(navigationExpression: sm => sm.StoreShippingMethods)
            .HasForeignKey(foreignKeyExpression: ssm => ssm.ShippingMethodId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion
    }
}
