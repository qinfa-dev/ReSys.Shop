namespace ReSys.Core.Domain.Catalog.Taxonomies.Taxa;

/// <summary>
/// Configures the database mapping for the <see cref="Taxon"/> entity.
/// </summary>
public sealed class TaxonConfiguration : IEntityTypeConfiguration<Taxon>
{
    /// <summary>
    /// Configures the entity of type <see cref="Taxon"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<Taxon> builder)
    {
        #region Table
        // Set the table name for the Taxon entity.
        builder.ToTable(name: Schema.Taxons);
        #endregion

        #region Primary Key
        // Configure the primary key for the Taxon entity.
        builder.HasKey(keyExpression: t => t.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: t => t.Name).IsUnique();
        builder.HasIndex(indexExpression: t => t.Position);
        builder.HasIndex(indexExpression: t => t.TaxonomyId);
        builder.HasIndex(indexExpression: t => t.ParentId);
        builder.HasIndex(indexExpression: t => t.Lft);
        builder.HasIndex(indexExpression: t => t.Rgt);
        builder.HasIndex(indexExpression: t => t.Depth);
        builder.HasIndex(indexExpression: t => t.Permalink).IsUnique();
        #endregion

        #region Properties
        // Configure properties for the Taxon entity.
        builder.Property(propertyExpression: t => t.Id)
            .HasColumnName(name: "id")
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the taxon. Value generated never.");

        builder.Property(propertyExpression: t => t.Description)
            .HasMaxLength(maxLength: CommonInput.Constraints.Text.DescriptionMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Description: A detailed description of the taxon.");

        builder.Property(propertyExpression: t => t.Permalink)
            .HasMaxLength(maxLength: CommonInput.Constraints.SlugsAndVersions.SlugMaxLength)
            .IsRequired()
            .HasComment(comment: "Permalink: The unique, URL-friendly identifier for the taxon.");

        builder.Property(propertyExpression: t => t.PrettyName)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired()
            .HasComment(comment: "PrettyName: A user-friendly name for the taxon.");

        builder.Property(propertyExpression: t => t.HideFromNav)
            .IsRequired()
            .HasComment(comment: "HideFromNav: Indicates if the taxon should be hidden from navigation menus.");

        builder.Property(propertyExpression: t => t.Lft)
            .IsRequired()
            .HasComment(comment: "Lft: Left value for the nested set model, used for hierarchical ordering.");

        builder.Property(propertyExpression: t => t.Rgt)
            .IsRequired()
            .HasComment(comment: "Rgt: Right value for the nested set model, used for hierarchical ordering.");

        builder.Property(propertyExpression: t => t.Depth)
            .IsRequired()
            .HasComment(comment: "Depth: The depth of the taxon in the hierarchy.");

        builder.Property(propertyExpression: t => t.Automatic)
            .IsRequired()
            .HasComment(comment: "Automatic: Indicates if the taxon's product associations are managed automatically by rules.");

        builder.Property(propertyExpression: t => t.RulesMatchPolicy)
            .HasMaxLength(maxLength: 50)
            .IsRequired()
            .HasComment(comment: "RulesMatchPolicy: Defines how multiple rules are applied to determine product association (e.g., 'All', 'Any').");

        builder.Property(propertyExpression: t => t.SortOrder)
            .HasMaxLength(maxLength: 50)
            .IsRequired()
            .HasComment(comment: "SortOrder: The order in which the taxon should appear in lists.");

        builder.Property(propertyExpression: t => t.MarkedForRegenerateTaxonProducts)
            .IsRequired()
            .HasComment(comment: "MarkedForRegenerateTaxonProducts: Indicates if the taxon's product associations need to be re-evaluated.");

        // Apply common configurations using extension methods.
        builder.ConfigureParameterizableName();
        builder.ConfigurePosition();
        builder.ConfigureSeoMetadata();
        builder.ConfigureUniqueName();
        builder.ConfigureMetadata();
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the Taxon entity.
        builder.HasOne(navigationExpression: t => t.Taxonomy)
            .WithMany(navigationExpression: tx => tx.Taxons)
            .HasForeignKey(foreignKeyExpression: t => t.TaxonomyId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasOne(navigationExpression: t => t.Parent)
            .WithMany(navigationExpression: t => t.Children)
            .HasForeignKey(foreignKeyExpression: t => t.ParentId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade); // Prevent deleting parent if children exist

        builder.HasMany(navigationExpression: t => t.TaxonImages)
            .WithOne(navigationExpression: ti => ti.Taxon)
            .HasForeignKey(foreignKeyExpression: ti => ti.TaxonId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: t => t.Classifications)
            .WithOne(navigationExpression: c => c.Taxon)
            .HasForeignKey(foreignKeyExpression: c => c.TaxonId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade); // Prevent deleting taxon if products are classified

        builder.HasMany(navigationExpression: t => t.TaxonRules)
            .WithOne(navigationExpression: tr => tr.Taxon)
            .HasForeignKey(foreignKeyExpression: tr => tr.TaxonId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion

    }
}
