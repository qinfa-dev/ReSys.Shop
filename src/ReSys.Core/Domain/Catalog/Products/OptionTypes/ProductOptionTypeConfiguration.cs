namespace ReSys.Core.Domain.Catalog.Products.OptionTypes;
/// <summary>
/// Configures the database mapping for the <see cref="ProductOptionType"/> entity.
/// </summary>
internal class ProductOptionTypeConfiguration : IEntityTypeConfiguration<ProductOptionType>
{
    /// <summary>
    /// Configures the entity of type <see cref="ProductOptionType"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<ProductOptionType> builder)
    {
        #region Table
        // Set the table name for the ProductOptionType entity.
        builder.ToTable(name: Schema.ProductOptionTypes);
        #endregion

        #region Primary Key
        // Configure the primary key for the ProductOptionType entity.
        builder.HasKey(keyExpression: pot => pot.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: pot => pot.ProductId);
        builder.HasIndex(indexExpression: pot => pot.OptionTypeId); // Add index for OptionTypeId
        builder.HasIndex(indexExpression: pot => new { pot.ProductId, pot.OptionTypeId }).IsUnique(); // Add unique composite index
        builder.HasIndex(indexExpression: pot => pot.Position); // Add index for Position
        #endregion

        #region Properties
        // Configure properties for the ProductOptionType entity.
        builder.Property(propertyExpression: pot => pot.Id)
            .HasComment(comment: "Id: Unique identifier for the product option type. Value generated never.");

        builder.Property(propertyExpression: pot => pot.ProductId)
            .IsRequired()
            .HasComment(comment: "ProductId: Foreign key to the associated Product.");

        builder.Property(propertyExpression: pot => pot.OptionTypeId)
            .IsRequired()
            .HasComment(comment: "OptionTypeId: Foreign key to the associated OptionType.");

        builder.Property(propertyExpression: pot => pot.Position)
            .IsRequired()
            .HasComment(comment: "Position: The display order of the option type for the product.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable();
        builder.ConfigurePosition();
        #endregion

        #region Relationships
        // Configure relationships for the ProductOptionType entity.
        builder.HasOne(navigationExpression: pot => pot.Product)
            .WithMany(navigationExpression: p => p.ProductOptionTypes)
            .HasForeignKey(foreignKeyExpression: pot => pot.ProductId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(navigationExpression: pot => pot.OptionType)
            .WithMany(navigationExpression: ot => ot.ProductOptionTypes)
            .HasForeignKey(foreignKeyExpression: pot => pot.OptionTypeId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade)
            .IsRequired();
        #endregion
    }
}
