using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Catalog.Products.Images;

public sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        #region Table
        builder.ToTable(name: Schema.ProductImages);
        #endregion

        #region Primary Key
        builder.HasKey(keyExpression: pi => pi.Id);
        #endregion

        #region Indexes
        builder.HasIndex(indexExpression: pi => pi.ProductId);
        builder.HasIndex(indexExpression: pi => pi.VariantId);
        builder.HasIndex(indexExpression: pi => pi.Position);
        #endregion

        #region Properties
        builder.Property(propertyExpression: pi => pi.Id)
           .ValueGeneratedNever()
           .HasComment(comment: "Id: Unique identifier for the product image.");

        builder.Property(propertyExpression: pi => pi.Url)
            .HasMaxLength(maxLength: ProductImage.Constraints.UrlMaxLength)
            .IsRequired()
            .HasComment(comment: "Url: The URL of the image asset.");

        builder.Property(propertyExpression: pi => pi.Alt)
            .HasMaxLength(maxLength: ProductImage.Constraints.AltMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Alt: Alternative text for the image.");

        builder.Property(propertyExpression: pi => pi.ContentType)
            .HasMaxLength(maxLength: 50)
            .IsRequired()
            .HasComment(comment: "ContentType: MIME type of the image.");

        builder.Property(propertyExpression: pi => pi.Width)
            .IsRequired(required: false)
            .HasComment(comment: "Width: Image width in pixels.");

        builder.Property(propertyExpression: pi => pi.Height)
            .IsRequired(required: false)
            .HasComment(comment: "Height: Image height in pixels.");

        builder.Property(propertyExpression: pi => pi.DimensionsUnit)
            .HasMaxLength(maxLength: 10)
            .IsRequired(required: false)
            .HasComment(comment: "DimensionsUnit: Unit of measurement.");

        builder.Property(propertyExpression: pi => pi.Type)
            .HasMaxLength(maxLength: 50)
            .IsRequired()
            .HasComment(comment: "Type: Image type (Default, Thumbnail, Gallery).");

        builder.Property(propertyExpression: pi => pi.ProductId)
            .IsRequired(required: false)
            .HasComment(comment: "ProductId: Foreign key to Product.");

        builder.Property(propertyExpression: pi => pi.VariantId)
            .IsRequired(required: false)
            .HasComment(comment: "VariantId: Foreign key to Variant.");

        #region Vector Embedding Configuration
        builder.Property(propertyExpression: pi => pi.Embedding)
            .HasColumnType(typeName: "vector(1024)")
            .IsRequired(required: false)
            .HasComment(comment: "Embedding: 1024-dimensional OpenCLIP vector for visual similarity search.");

        builder.Property(propertyExpression: pi => pi.EmbeddingModel)
            .HasMaxLength(maxLength: 100)
            .HasDefaultValue(value: "openclip-vit-h-14-laion2b")
            .HasComment(comment: "EmbeddingModel: Model version used to generate embedding.");

        builder.Property(propertyExpression: pi => pi.EmbeddingGeneratedAt)
            .IsRequired(required: false)
            .HasComment(comment: "EmbeddingGeneratedAt: Timestamp of embedding generation.");

        // Vector similarity index using HNSW
        // NOTE: HNSW index creation via HasMethod is not supported in all EF Core versions
        // You may need to create this manually in a migration
        builder.HasIndex(indexExpression: pi => pi.Embedding)
            .HasDatabaseName(name: "ix_product_images_embedding_hnsw")
            .HasMethod(method: "hnsw")
            .HasOperators(operators: "vector_cosine_ops");
        #endregion

        builder.ConfigurePosition();
        builder.ConfigureMetadata();
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        builder.HasOne(navigationExpression: pi => pi.Product)
            .WithMany(navigationExpression: p => p.Images)
            .HasForeignKey(foreignKeyExpression: pi => pi.ProductId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade)
            .IsRequired(required: false);

        builder.HasOne(navigationExpression: pi => pi.Variant)
            .WithMany(navigationExpression: v => v.Images)
            .HasForeignKey(foreignKeyExpression: pi => pi.VariantId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade)
            .IsRequired(required: false);
        #endregion
    }
}