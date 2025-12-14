namespace ReSys.Core.Domain.Catalog.Products.PropertyTypes;

/// <summary>
/// Configures the database mapping for the <see cref="ProductPropertyType"/> entity.
/// </summary>
public sealed class ProductPropertyTypeConfiguration : IEntityTypeConfiguration<ProductPropertyType>
{
    /// <summary>
    /// Configures the entity of type <see cref="ProductPropertyType"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<ProductPropertyType> builder)
    {
        #region Table
        // Set the table name for the ProductProperty entity.
        builder.ToTable(name: Schema.ProductPropertyTypes);
        #endregion

        #region Primary Key
        // Configure the primary key for the ProductProperty entity.
        builder.HasKey(keyExpression: pp => pp.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: pp => pp.ProductId);
        builder.HasIndex(indexExpression: pp => pp.PropertyTypeId);
        builder.HasIndex(indexExpression: pp => new { pp.ProductId, PropertyId = pp.PropertyTypeId }).IsUnique();
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

        builder.Property(propertyExpression: pp => pp.PropertyTypeId)
            .IsRequired()
            .HasComment(comment: "PropertyId: Foreign key to the associated Property.");

        builder.Property(propertyExpression: pp => pp.PropertyTypeValue)
            .HasMaxLength(maxLength: ProductPropertyType.Constraints.MaxValueLength)
            .IsRequired()
            .HasComment(comment: "Value: The value of the property for this product.");

        builder.Property(propertyExpression: pp => pp.Position)
            .IsRequired()
            .HasComment(comment: "Position: The display order of the product property.");

        // Apply common configurations using extension methods.
        builder.ConfigurePosition();
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the ProductProperty entity.
        builder.HasOne(navigationExpression: pp => pp.Product)
            .WithMany(navigationExpression: p => p.ProductPropertyTypes)
            .HasForeignKey(foreignKeyExpression: pp => pp.ProductId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasOne(navigationExpression: pp => pp.PropertyType)
            .WithMany(navigationExpression: p => p.ProductPropertyTypes)
            .HasForeignKey(foreignKeyExpression: pp => pp.PropertyTypeId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion
    }
}
