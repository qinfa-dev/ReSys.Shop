using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReSys.Core.Domain.Constants;
using ReSys.Core.Common.Domain.Concerns;

// For StockItem

namespace ReSys.Core.Domain.Orders;

/// <summary>
/// Configures the database mapping for the <see cref="StockMovement"/> entity.
/// </summary>
public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    /// <summary>
    /// Configures the entity of type <see cref="StockMovement"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        #region Table
        builder.ToTable(name: Schema.StockMovements);
        #endregion

        #region Primary Key
        builder.HasKey(keyExpression: sm => sm.Id);
        #endregion

        #region Properties
        builder.Property(propertyExpression: sm => sm.Id)
            .ValueGeneratedNever();

        builder.Property(propertyExpression: sm => sm.StockItemId)
            .IsRequired();

        builder.Property(propertyExpression: sm => sm.Quantity)
            .IsRequired();
            
        builder.Property(propertyExpression: sm => sm.Originator)
            .HasConversion<string>() // Store enum as string for readability
            .IsRequired();

        builder.Property(propertyExpression: sm => sm.Action)
            .HasConversion<string>() // Store enum as string for readability
            .IsRequired();
            
        builder.Property(propertyExpression: sm => sm.Reason)
            .HasMaxLength(255) // Assuming a reasonable max length for a reason
            .IsRequired(false);

        builder.Property(propertyExpression: sm => sm.OriginatorId)
            .IsRequired(false);

        // Configure Auditable concerns
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        builder.HasOne(navigationExpression: sm => sm.StockItem)
            .WithMany(navigationExpression: si => si.StockMovements)
            .HasForeignKey(foreignKeyExpression: sm => sm.StockItemId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion

        #region Indexes
        builder.HasIndex(indexExpression: sm => sm.StockItemId);
        builder.HasIndex(indexExpression: sm => sm.Originator);
        builder.HasIndex(indexExpression: sm => sm.Action);
        builder.HasIndex(indexExpression: sm => sm.OriginatorId);
        #endregion
    }
}