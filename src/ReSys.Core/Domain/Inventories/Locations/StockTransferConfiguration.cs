using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Inventories.Locations;

/// <summary>
/// Configures the database mapping for the <see cref="StockTransfer"/> entity.
/// This aggregate orchestrates stock transfers between locations and supplier receipts,
/// ensuring proper FK relationships and deletion behaviors.
/// </summary>
/// <remarks>
/// <b>Design Note:</b>
/// StockTransfer.Movements collection uses a polymorphic pattern (StockMovement.OriginatorId + Originator enum)
/// rather than a direct FK. This allows a single movements table to track movements from multiple sources
/// (transfers, orders, returns, damage, etc.). The Movements collection is populated dynamically via query
/// when needed, maintaining clean aggregate boundaries.
/// </remarks>
public sealed class StockTransferConfiguration : IEntityTypeConfiguration<StockTransfer>
{
    /// <summary>
    /// Configures the entity of type <see cref="StockTransfer"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<StockTransfer> builder)
    {
        #region Table
        builder.ToTable(name: Schema.StockTransfers);
        #endregion

        #region Primary Key
        builder.HasKey(keyExpression: st => st.Id);
        #endregion

        #region Properties
        builder.Property(propertyExpression: st => st.Id)
            .HasColumnName(name: "id")
            .ValueGeneratedNever()
            .HasComment(comment: "Unique identifier for the stock transfer.");

        builder.Property(propertyExpression: st => st.SourceLocationId)
            .IsRequired(required: false)
            .HasComment(comment: "Foreign key to the source location (null for supplier receipts).");

        builder.Property(propertyExpression: st => st.DestinationLocationId)
            .IsRequired()
            .HasComment(comment: "Foreign key to the destination location (required).");

        builder.Property(propertyExpression: st => st.Number)
            .HasMaxLength(maxLength: StockTransfer.Constraints.NumberMaxLength)
            .IsRequired()
            .HasComment(comment: "Number: Auto-generated transfer number for reference.");

        builder.Property(propertyExpression: st => st.Reference)
            .HasMaxLength(maxLength: StockTransfer.Constraints.ReferenceMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Reference: Optional reference code (e.g., purchase order number).");

        builder.ConfigureAuditable();
        #endregion

        #region Relationships

        /// <summary>
        /// Configures the relationship to the source location (nullable for supplier receipts).
        /// </summary>
        builder.HasOne(navigationExpression: st => st.SourceLocation)
            .WithMany() // StockLocation does not expose an inverse collection
            .HasForeignKey(foreignKeyExpression: st => st.SourceLocationId)
            .OnDelete(deleteBehavior: DeleteBehavior.SetNull);

        /// <summary>
        /// Configures the relationship to the destination location (required).
        /// </summary>
        builder.HasOne(navigationExpression: st => st.DestinationLocation)
            .WithMany() // StockLocation does not expose an inverse collection
            .HasForeignKey(foreignKeyExpression: st => st.DestinationLocationId)
            .OnDelete(deleteBehavior: DeleteBehavior.Restrict);

        /// <summary>
        /// Note: Movements collection is NOT configured here.
        /// StockMovement uses a polymorphic OriginatorId + Originator enum pattern.
        /// Movements are queried dynamically (e.g., where OriginatorId == transferId and Originator == StockTransfer).
        /// This approach maintains the polymorphic auditing pattern while avoiding direct FK coupling.
        /// </summary>
        // Movements are NOT configured as a direct relationship (polymorphic pattern instead)

        #endregion

        #region Indexes
        builder.HasIndex(indexExpression: st => st.Number)
            .IsUnique();
        builder.HasIndex(indexExpression: st => st.SourceLocationId);
        builder.HasIndex(indexExpression: st => st.DestinationLocationId);
        builder.HasIndex(indexExpression: st => st.Reference);
        builder.HasIndex(indexExpression: st => st.CreatedAt);
        #endregion
    }
}
