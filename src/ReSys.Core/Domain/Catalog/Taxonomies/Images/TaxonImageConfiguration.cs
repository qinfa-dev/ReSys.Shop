namespace ReSys.Core.Domain.Catalog.Taxonomies.Images;

/// <summary>
/// Configures the database mapping for the <see cref="TaxonImage"/> entity.
/// </summary>
public sealed class TaxonImageConfiguration : IEntityTypeConfiguration<TaxonImage>
{
    /// <summary>
    /// Configures the entity of type <see cref="TaxonImage"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<TaxonImage> builder)
    {
        #region Table
        // Set the table name for the TaxonImage entity.
        builder.ToTable(name: Schema.TaxonImages);
        #endregion

        #region Primary Key
        // Configure the primary key for the TaxonImage entity.
        builder.HasKey(keyExpression: ti => ti.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: ti => ti.TaxonId);
        builder.HasIndex(indexExpression: ti => ti.Type);
        #endregion

        #region Properties
        // Configure properties for the TaxonImage entity.
        builder.Property(propertyExpression: ti => ti.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the taxon image. Value generated never.");

        builder.Property(propertyExpression: ti => ti.TaxonId)
            .IsRequired()
            .HasComment(comment: "TaxonId: Foreign key to the associated Taxon.");

        builder.Property(propertyExpression: ti => ti.Type)
            .ConfigureShortText()
            .HasComment(comment: "Type: The type of the image (e.g., 'default', 'square').");
        
        builder.Property(propertyExpression: pi => pi.Alt)
            .ConfigureTitleOptional()
            .HasComment(comment: "Alt: Alternative text for the image.");

        builder.Property(propertyExpression: ti => ti.Url)
            .ConfigureUrlOptional()
            .HasComment(comment: "Url: The URL of the image asset.");

        // Apply common configurations using extension methods.
        builder.ConfigurePosition();
        builder.ConfigureMetadata();
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the TaxonImage entity.
        builder.HasOne(navigationExpression: ti => ti.Taxon)
            .WithMany(navigationExpression: t => t.TaxonImages)
            .HasForeignKey(foreignKeyExpression: ti => ti.TaxonId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion

    }
}
