using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Stores.PaymentMethods;

/// <summary>
/// Configures the database mapping for the <see cref="StorePaymentMethod"/> entity.
/// </summary>
public sealed class StorePaymentMethodConfiguration : IEntityTypeConfiguration<StorePaymentMethod>
{
    /// <summary>
    /// Configures the entity of type <see cref="StorePaymentMethod"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<StorePaymentMethod> builder)
    {
        #region Table
        // Set the table name for the StorePaymentMethod entity.
        builder.ToTable(name: Schema.StorePaymentMethods);
        #endregion

        #region Primary Key
        // Configure the primary key for the StorePaymentMethod entity.
        builder.HasKey(keyExpression: spm => spm.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: spm => spm.StoreId);
        builder.HasIndex(indexExpression: spm => spm.PaymentMethodId);
        builder.HasIndex(indexExpression: spm => new { spm.StoreId, spm.PaymentMethodId }).IsUnique();
        #endregion

        #region Properties
        // Configure properties for the StorePaymentMethod entity.
        builder.Property(propertyExpression: spm => spm.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the store payment method. Value generated never.");

        builder.Property(propertyExpression: spm => spm.StoreId)
            .IsRequired(required: false)
            .HasComment(comment: "StoreId: Foreign key to the associated Storefront.");

        builder.Property(propertyExpression: spm => spm.PaymentMethodId)
            .IsRequired()
            .HasComment(comment: "PaymentMethodId: Foreign key to the associated PaymentMethod.");

        builder.Property(propertyExpression: spm => spm.Available)
            .IsRequired()
            .HasComment(comment: "Available: Indicates if the payment method is available for this store.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable(); // For CreatedAt, UpdatedAt
        #endregion

        #region Relationships
        // Configure relationships for the StorePaymentMethod entity.

        builder.HasOne(navigationExpression: spm => spm.PaymentMethod)
            .WithMany(navigationExpression: pm => pm.StorePaymentMethods)
            .HasForeignKey(foreignKeyExpression: spm => spm.PaymentMethodId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);


        builder.HasOne(navigationExpression: spm => spm.Store)
            .WithMany(navigationExpression: s => s.StorePaymentMethods)
            .HasForeignKey(foreignKeyExpression: spm => spm.StoreId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade)
            .IsRequired(false);

        #endregion
    }
}