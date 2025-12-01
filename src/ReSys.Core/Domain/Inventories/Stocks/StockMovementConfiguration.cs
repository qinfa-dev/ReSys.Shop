using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Inventories.Stocks;

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
        // Set the table name for the StockMovement entity.
        builder.ToTable(name: Schema.StockMovements);
        #endregion

        #region Primary Key
        // Configure the primary key for the StockMovement entity.
        builder.HasKey(keyExpression: sm => sm.Id);
        #endregion

        #region Properties
        // Configure properties for the StockMovement entity.
        builder.Property(propertyExpression: sm => sm.Id)
            .HasColumnName(name: "id")
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the stock movement. Value generated never.");

        builder.Property(propertyExpression: sm => sm.StockItemId)
            .IsRequired()
            .HasComment(comment: "StockItemId: Foreign key to the associated StockItem.");

        builder.Property(propertyExpression: sm => sm.Quantity)
            .IsRequired()
            .HasComment(comment: "Quantity: The quantity of the variant moved.");

        builder.Property(propertyExpression: sm => sm.Originator)
            .HasConversion<string>()
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired()
            .HasComment(comment: "Originator: The source or reason for the stock movement.");

        builder.Property(propertyExpression: sm => sm.Action)
            .HasConversion<string>()
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired()
            .HasComment(comment: "Action: The action performed (e.g., Received, Sold, Adjusted).");

        builder.Property(propertyExpression: sm => sm.Reason)
            .HasMaxLength(maxLength: CommonInput.Constraints.Text.ShortTextMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Reason: Optional reason or note for the movement.");

        builder.Property(propertyExpression: sm => sm.StockTransferId)
            .IsRequired(required: false)
            .HasComment(comment: "StockTransferId: Foreign key to the associated StockTransfer.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the StockMovement entity.
        builder.HasOne(navigationExpression: sm => sm.StockItem)
            .WithMany(navigationExpression: si => si.StockMovements)
            .HasForeignKey(foreignKeyExpression: sm => sm.StockItemId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasOne(navigationExpression: sm => sm.StockTransfer)
            .WithMany(navigationExpression: st => st.Movements)
            .HasForeignKey(foreignKeyExpression: sm => sm.StockTransferId)
            .IsRequired(required: false)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: sm => sm.StockItemId);
        builder.HasIndex(indexExpression: sm => sm.Originator);
        builder.HasIndex(indexExpression: sm => sm.Action);
        #endregion

        #region Ignored Properties
        // Ignore domain-specific or computed properties that should not be mapped to the database.
        builder.Ignore(propertyExpression: sm => sm.IsIncrease);
        builder.Ignore(propertyExpression: sm => sm.IsDecrease);
        #endregion
    }
}