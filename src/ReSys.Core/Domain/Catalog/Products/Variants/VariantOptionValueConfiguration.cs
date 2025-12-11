// Add this using directive

namespace ReSys.Core.Domain.Catalog.Products.Variants;

/// <summary>
/// Configures the database mapping for the <see cref="VariantOptionValue"/> entity.
/// </summary>
public sealed class VariantOptionValueConfiguration : IEntityTypeConfiguration<VariantOptionValue>
{
    /// <summary>
    /// Configures the entity of type <see cref="VariantOptionValue"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<VariantOptionValue> builder)
    {
        #region Table
        // Set the table name for the VariantOptionValue entity.
        builder.ToTable(name: Schema.VariantOptionValues);
        #endregion

        #region Primary Key
        // Configure the primary key for the VariantOptionValue entity.
        builder.HasKey(keyExpression: ovv => ovv.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: ovv => ovv.VariantId);
        builder.HasIndex(indexExpression: ovv => ovv.OptionValueId);
        builder.HasIndex(indexExpression: ovv => new { ovv.VariantId, ovv.OptionValueId }).IsUnique();
        #endregion

        #region Properties
        // Configure properties for the VariantOptionValue entity.
        builder.Property(propertyExpression: ovv => ovv.Id)
            .HasComment(comment: "Id: Unique identifier for the option value variant. Value generated never.");

        builder.Property(propertyExpression: ovv => ovv.VariantId)
            .IsRequired()
            .HasComment(comment: "VariantId: Foreign key to the associated Product Variant.");

        builder.Property(propertyExpression: ovv => ovv.OptionValueId)
            .IsRequired()
            .HasComment(comment: "OptionValueId: Foreign key to the associated OptionValue.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable(); // Add this line
        #endregion

        #region Relationships
        // Configure relationships for the VariantOptionValue entity.
        builder.HasOne(navigationExpression: ovv => ovv.Variant)
            .WithMany(navigationExpression: v => v.OptionValueVariants)
            .HasForeignKey(foreignKeyExpression: ovv => ovv.VariantId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasOne(navigationExpression: ovv => ovv.OptionValue)
            .WithMany(navigationExpression: ov => ov.VariantOptionValues)
            .HasForeignKey(foreignKeyExpression: ovv => ovv.OptionValueId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion
    }
}
