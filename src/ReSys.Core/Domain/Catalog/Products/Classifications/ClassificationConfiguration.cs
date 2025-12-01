using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Catalog.Products.Classifications;

/// <summary>
/// Configures the database mapping for the <see cref="Classification"/> entity.
/// </summary>
public sealed class ClassificationConfiguration : IEntityTypeConfiguration<Classification>
{
    /// <summary>
    /// Configures the entity of type <see cref="Classification"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<Classification> builder)
    {
        #region Table
        // Set the table name for the Classification entity.
        builder.ToTable(name: Schema.Classifications);
        #endregion

        #region Primary Key
        // Configure the primary key for the Classification entity.
        builder.HasKey(keyExpression: c => c.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: c => c.ProductId);
        builder.HasIndex(indexExpression: c => c.TaxonId);
        builder.HasIndex(indexExpression: c => new { c.ProductId, c.TaxonId }).IsUnique();
        builder.HasIndex(indexExpression: c => c.Position);
        #endregion

        #region Properties
        // Apply common configurations using extension methods.
        builder.ConfigurePosition();
        builder.ConfigureAuditable();

        // Configure properties for the Classification entity.
        builder.Property(propertyExpression: c => c.Id)
            .HasColumnName(name: "id")
            .HasComment(comment: "Id: Unique identifier for the classification. Value generated never.");

        builder.Property(propertyExpression: c => c.ProductId)
            .HasComment(comment: "ProductId: Foreign key to the associated Product.");

        builder.Property(propertyExpression: c => c.TaxonId)
            .HasComment(comment: "TaxonId: Foreign key to the associated Taxon.");

        builder.Property(propertyExpression: c => c.Position)
            .HasComment(comment: "Position: The display order of the classification within a taxon's product list.");

        #endregion

        #region Relationships
        // Configure relationships for the Classification entity.
        builder.HasOne(navigationExpression: c => c.Product)
            .WithMany(navigationExpression: p => p.Classifications)
            .HasForeignKey(foreignKeyExpression: c => c.ProductId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasOne(navigationExpression: c => c.Taxon)
            .WithMany(navigationExpression: t => t.Classifications)
            .HasForeignKey(foreignKeyExpression: c => c.TaxonId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion
    }
}
