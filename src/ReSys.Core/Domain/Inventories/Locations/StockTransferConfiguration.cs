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
        builder.ToTable(name: Schema.StockTransfers);
        #endregion

        #region Primary Key
        builder.HasKey(keyExpression: st => st.Id);
        #endregion

        #region Properties
        builder.Property(propertyExpression: st => st.Id)
            .ValueGeneratedNever();

        builder.Property(propertyExpression: st => st.SourceLocationId)
            .IsRequired(false);

        builder.Property(propertyExpression: st => st.DestinationLocationId)
            .IsRequired();

        builder.Property(propertyExpression: st => st.Number)
            .HasMaxLength(maxLength: StockTransfer.Constraints.NumberMaxLength)
            .IsRequired();

        builder.Property(propertyExpression: st => st.Reference)
            .HasMaxLength(maxLength: StockTransfer.Constraints.ReferenceMaxLength)
            .IsRequired(false);

        // Configure Auditable concerns
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        builder.HasOne(navigationExpression: st => st.SourceLocation)
            .WithMany()
            .HasForeignKey(foreignKeyExpression: st => st.SourceLocationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(navigationExpression: st => st.DestinationLocation)
            .WithMany()
            .HasForeignKey(foreignKeyExpression: st => st.DestinationLocationId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(navigationExpression: st => st.Movements)
            .WithOne() // No direct navigation from StockMovement back to StockTransfer for this relationship
            .HasForeignKey(sm => sm.OriginatorId)
            .IsRequired(false) // OriginatorId is nullable
            .OnDelete(DeleteBehavior.SetNull); // If StockTransfer is deleted, clear OriginatorId in StockMovement
        #endregion

        #region Indexes
        builder.HasIndex(indexExpression: st => st.SourceLocationId);
        builder.HasIndex(indexExpression: st => st.DestinationLocationId);
        builder.HasIndex(indexExpression: st => st.Number).IsUnique();
        #endregion
    }
}