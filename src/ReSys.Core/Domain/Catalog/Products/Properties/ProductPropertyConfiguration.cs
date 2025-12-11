namespace ReSys.Core.Domain.Catalog.Products.Properties;

/// <summary>
/// Configures the database mapping for the <see cref="ProductProperty"/> entity.
/// </summary>
public sealed class ProductPropertyConfiguration : IEntityTypeConfiguration<ProductProperty>
{
    /// <summary>
    /// Configures the entity of type <see cref="ProductProperty"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<ProductProperty> builder)
    {
        #region Table
        // Set the table name for the ProductProperty entity.
        builder.ToTable(name: Schema.ProductProperties);
        #endregion

        #region Primary Key
        // Configure the primary key for the ProductProperty entity.
        builder.HasKey(keyExpression: pp => pp.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: pp => pp.ProductId);
        builder.HasIndex(indexExpression: pp => pp.PropertyId);
        builder.HasIndex(indexExpression: pp => new { pp.ProductId, pp.PropertyId }).IsUnique();
        builder.HasIndex(indexExpression: pp => pp.Position);
        #endregion

        #region Properties
        // Configure properties for the ProductProperty entity.
        builder.Property(propertyExpression: pp => pp.Id)
            .HasColumnName(name: "id")
            .HasComment(comment: "Id: Unique identifier for the product property. Value generated never.");

        builder.Property(propertyExpression: pp => pp.ProductId)
            .IsRequired()
            .HasComment(comment: "ProductId: Foreign key to the associated Product.");

        builder.Property(propertyExpression: pp => pp.PropertyId)
            .IsRequired()
            .HasComment(comment: "PropertyId: Foreign key to the associated Property.");

        builder.Property(propertyExpression: pp => pp.Value)
            .HasMaxLength(maxLength: ProductProperty.Constraints.MaxValueLength)
            .IsRequired()
            .HasComment(comment: "Value: The value of the property for this product.");

        builder.Property(propertyExpression: pp => pp.Position)
            .IsRequired()
            .HasComment(comment: "Position: The display order of the product property.");

        builder.Property(propertyExpression: pp => pp.FilterParam)
            .HasMaxLength(maxLength: ProductProperty.Constraints.MaxFilterParamLength)
            .IsRequired(required: false)
            .HasComment(comment: "FilterParam: A URL-friendly slug for filtering based on this property value.");

        // Apply common configurations using extension methods.
        builder.ConfigurePosition();
        builder.ConfigureFilterParam();
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the ProductProperty entity.
        builder.HasOne(navigationExpression: pp => pp.Product)
            .WithMany(navigationExpression: p => p.ProductProperties)
            .HasForeignKey(foreignKeyExpression: pp => pp.ProductId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasOne(navigationExpression: pp => pp.Property)
            .WithMany(navigationExpression: p => p.ProductProperties)
            .HasForeignKey(foreignKeyExpression: pp => pp.PropertyId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion
    }
}
