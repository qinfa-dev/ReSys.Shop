using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Orders.Adjustments;

/// <summary>
/// Configures the database mapping for the <see cref="OrderAdjustment"/> entity.
/// </summary>
public sealed class OrderAdjustmentConfiguration : IEntityTypeConfiguration<OrderAdjustment>
{
    /// <summary>
    /// Configures the entity of type <see cref="OrderAdjustment"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<OrderAdjustment> builder)
    {
        #region Table
        // Set the table name for the OrderAdjustment entity.
        builder.ToTable(name: Schema.OrderAdjustments);
        #endregion

        #region Primary Key
        // Configure the primary key for the OrderAdjustment entity.
        builder.HasKey(keyExpression: oa => oa.Id);
        #endregion

        #region Properties
        // Configure properties for the OrderAdjustment entity.
        builder.Property(propertyExpression: oa => oa.Id)
            .HasColumnName(name: "id")
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the order adjustment. Value generated never.");

        builder.Property(propertyExpression: oa => oa.OrderId)
            .IsRequired()
            .HasComment(comment: "OrderId: Foreign key to the associated Order.");

        builder.Property(propertyExpression: oa => oa.PromotionId)
            .IsRequired(required: false)
            .HasComment(comment: "PromotionId: Foreign key to the associated Promotion.");

        builder.Property(propertyExpression: oa => oa.AmountCents)
            .IsRequired()
            .HasComment(comment: "AmountCents: The adjustment amount in cents.");

        builder.Property(propertyExpression: oa => oa.Description)
            .HasMaxLength(maxLength: OrderAdjustment.Constraints.DescriptionMaxLength)
            .IsRequired()
            .HasComment(comment: "Description: Description of the adjustment.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the OrderAdjustment entity.
        builder.HasOne(navigationExpression: oa => oa.Order)
            .WithMany(navigationExpression: o => o.Adjustments)
            .HasForeignKey(foreignKeyExpression: oa => oa.OrderId)
            .OnDelete(deleteBehavior: DeleteBehavior.Restrict); // Restrict to prevent deleting order if adjustments exist

        builder.HasOne(navigationExpression: oa => oa.Promotion)
            .WithMany(navigationExpression: p => p.OrderAdjustments)
            .HasForeignKey(foreignKeyExpression: oa => oa.PromotionId)
            .IsRequired(required: false)
            .OnDelete(deleteBehavior: DeleteBehavior.SetNull); // Promotion can be deleted, but adjustment remains
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: oa => oa.OrderId);
        builder.HasIndex(indexExpression: oa => oa.PromotionId);
        #endregion

        #region Ignored Properties
        // Ignore domain-specific or computed properties that should not be mapped to the database.
        builder.Ignore(propertyExpression: oa => oa.IsPromotion);
        #endregion
    }
}
