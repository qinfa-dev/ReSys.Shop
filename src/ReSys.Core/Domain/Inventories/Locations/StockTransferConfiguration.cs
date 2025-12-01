using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Inventories.Locations;

/// <summary>
/// Configures the database mapping for the <see cref="StockTransfer"/> entity.
/// </summary>
public sealed class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    /// <summary>
    /// Configures the entity of type <see cref="StockTransfer"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        #region Table
        // Set the table name for the StockTransfer entity.
        builder.ToTable(name: Schema.StockTransfers);
        #endregion

        #region Primary Key
        // Configure the primary key for the StockTransfer entity.
        builder.HasKey(keyExpression: st => st.Id);
        #endregion

        #region Properties
        // Configure properties for the StockTransfer entity.
        builder.Property(propertyExpression: st => st.Id)
            .HasColumnName(name: "id")
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the stock transfer. Value generated never.");

        builder.Property(propertyExpression: st => st.SourceLocationId)
            .IsRequired(required: false)
            .HasComment(comment: "SourceLocationId: Foreign key to the source StockLocation (null for external receipts).");

        builder.Property(propertyExpression: st => st.DestinationLocationId)
            .IsRequired()
            .HasComment(comment: "DestinationLocationId: Foreign key to the destination StockLocation.");

        builder.Property(propertyExpression: st => st.Number)
            .HasMaxLength(maxLength: 50)
            .IsRequired()
            .HasComment(comment: "Number: Unique reference number for the stock transfer.");

        builder.Property(propertyExpression: st => st.Reference)
            .HasMaxLength(maxLength: 255)
            .IsRequired(required: false)
            .HasComment(comment: "Reference: Optional external reference for the transfer.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the StockTransfer entity.
        builder.HasOne(navigationExpression: st => st.SourceLocation)
            .WithMany()
            .HasForeignKey(foreignKeyExpression: st => st.SourceLocationId)
            .IsRequired(required: false)
            .OnDelete(deleteBehavior: DeleteBehavior.Restrict); // Prevent deleting source location if transfers exist

        builder.HasOne(navigationExpression: st => st.DestinationLocation)
            .WithMany()
            .HasForeignKey(foreignKeyExpression: st => st.DestinationLocationId)
            .IsRequired()
            .OnDelete(deleteBehavior: DeleteBehavior.Restrict); // Prevent deleting destination location if transfers exist

        builder.HasMany(navigationExpression: st => st.Movements)
            .WithOne(navigationExpression: sm => sm.StockTransfer)
            .HasForeignKey(foreignKeyExpression: sm => sm.StockTransferId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: st => st.Number).IsUnique();
        builder.HasIndex(indexExpression: st => st.SourceLocationId);
        builder.HasIndex(indexExpression: st => st.DestinationLocationId);
        #endregion
    }
}