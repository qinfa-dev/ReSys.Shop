namespace ReSys.Core.Domain.Catalog.Products;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        #region Table
        builder.ToTable(name: Schema.Products);
        #endregion

        #region Primary Key
        builder.HasKey(keyExpression: p => p.Id);
        #endregion

        #region Properties
        builder.Property(propertyExpression: p => p.Id)
            .HasColumnName(name: "id")
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the product. Value generated never.");

        builder.ConfigureUniqueName();
        builder.ConfigureMetadata();
        builder.ConfigureSeoMetadata();

        builder.Property(propertyExpression: p => p.Name)
            .ConfigureName()
            .HasComment(comment: "Name: The name of the product.");

        builder.Property(propertyExpression: p => p.Slug)
            .ConfigureSlug()
            .HasComment(comment: "Slug: SEO-friendly URL slug for the product.");

        builder.Property(propertyExpression: p => p.Description)
            .ConfigureLongTextOptional(isRequired: false)
            .HasComment(comment: "Description: Full description of the product.");

        builder.Property(propertyExpression: p => p.AvailableOn)
            .IsRequired(required: false)
            .HasComment(comment: "AvailableOn: Date when the product becomes available.");

        builder.Property(propertyExpression: p => p.DiscontinueOn)
            .IsRequired(required: false)
            .HasComment(comment: "DiscontinueOn: Date when the product is discontinued.");

        builder.Property(propertyExpression: p => p.MakeActiveAt)
            .IsRequired(required: false)
            .HasComment(comment: "MakeActiveAt: Date when the product should become active.");

        builder.Property(propertyExpression: p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(maxLength: CommonInput.Constraints.Text.TinyTextMaxLength)
            .IsRequired(required: true)
            .HasComment(comment: "Status: The current status of the product (e.g., Draft, Active, Archived).");

        builder.Property(propertyExpression: p => p.IsDigital)
            .IsRequired()
            .HasComment(comment: "IsDigital: Indicates if the product is digital.");

        builder.Property(propertyExpression: p => p.MetaTitle)
            .ConfigureTitleOptional(isRequired: false)
            .HasComment(comment: "MetaTitle: SEO title for the product.");

        builder.Property(propertyExpression: p => p.MetaDescription)
            .ConfigureDescriptionOptional(isRequired: false)
            .HasComment(comment: "MetaDescription: SEO description for the product.");

        builder.Property(propertyExpression: p => p.MetaKeywords)
            .ConfigureMediumTextOptional(isRequired: false)
            .HasComment(comment: "MetaKeywords: SEO keywords for the product.");

        builder.Property(propertyExpression: p => p.MarkedForRegenerateTaxonProducts)
            .IsRequired()
            .HasComment(comment: "MarkedForRegenerateTaxonProducts: Flag to indicate if automatic taxon products need regeneration.");

        builder.Property(propertyExpression: p => p.DeletedAt)
            .IsRequired(required: false)
            .HasComment(comment: "DeletedAt: Date when the product was soft-deleted.");

        builder.Property(propertyExpression: p => p.DeletedBy)
            .ConfigureNameOptional(isRequired: false)
            .HasComment(comment: "DeletedBy: User who soft-deleted the product.");

        builder.Property(propertyExpression: p => p.IsDeleted)
            .IsRequired()
            .HasComment(comment: "IsDeleted: Flag indicating if the product is soft-deleted.");

        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        builder.HasMany(navigationExpression: p => p.Images)
            .WithOne(navigationExpression: pi => pi.Product)
            .HasForeignKey(foreignKeyExpression: pi => pi.ProductId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade)
            .IsRequired(required: false);

        builder.HasMany(navigationExpression: p => p.ProductOptionTypes)
            .WithOne(navigationExpression: po => po.Product)
            .HasForeignKey(foreignKeyExpression: po => po.ProductId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: p => p.ProductPropertyTypes)
            .WithOne(navigationExpression: pp => pp.Product)
            .HasForeignKey(foreignKeyExpression: pp => pp.ProductId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: p => p.Classifications)
            .WithOne(navigationExpression: c => c.Product)
            .HasForeignKey(foreignKeyExpression: c => c.ProductId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: p => p.Variants)
            .WithOne(navigationExpression: v => v.Product)
            .HasForeignKey(foreignKeyExpression: v => v.ProductId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: p => p.StoreProducts)
            .WithOne(navigationExpression: sp => sp.Product)
            .HasForeignKey(foreignKeyExpression: sp => sp.ProductId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade)
            .IsRequired(false);
        
        builder.HasMany(navigationExpression: p => p.Reviews)
            .WithOne(navigationExpression: sp => sp.Product)
            .HasForeignKey(foreignKeyExpression: sp => sp.ProductId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion

        #region Indexes
        builder.HasIndex(indexExpression: p => p.Name).IsUnique();
        builder.HasIndex(indexExpression: p => p.Slug).IsUnique();
        #endregion

        #region Ignored Properties
        builder.Ignore(propertyExpression: p => p.HasVariants);
        builder.Ignore(propertyExpression: p => p.Available);
        builder.Ignore(propertyExpression: p => p.DefaultVariant);
        builder.Ignore(propertyExpression: p => p.DefaultImage);
        builder.Ignore(propertyExpression: p => p.SecondaryImage);
        builder.Ignore(propertyExpression: p => p.Purchasable);
        builder.Ignore(propertyExpression: p => p.InStock);
        builder.Ignore(propertyExpression: p => p.Backorderable);
        builder.Ignore(propertyExpression: p => p.TotalOnHand);
        builder.Ignore(propertyExpression: p => p.MainTaxon);
        builder.Ignore(propertyExpression: p => p.Discontinued);
        builder.Ignore(propertyExpression: p => p.CanSupply);
        builder.Ignore(propertyExpression: p => p.Backordered);
        builder.Ignore(propertyExpression: p => p.Orders);
        builder.Ignore(propertyExpression: p => p.OptionTypes);
        builder.Ignore(propertyExpression: p => p.Properties);
        builder.Ignore(propertyExpression: p => p.Taxons);
        builder.Ignore(propertyExpression: p => p.Taxonomies);
        builder.Ignore(propertyExpression: p => p.Stores);

        #endregion

        #region Concurrency
        builder.ConfigureVersion();
        #endregion
    }
}