namespace ReSys.Core.Domain.Catalog.OptionTypes;

/// <summary>
/// Configures the database mapping for the <see cref="OptionType"/> entity.
/// </summary>
public sealed class OptionTypeConfiguration : IEntityTypeConfiguration<OptionType>
{
    /// <summary>
    /// Configures the entity of type <see cref="OptionType"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<OptionType> builder)
    {
        #region Table
        // Set the table name for the OptionType entity.
        builder.ToTable(name: Schema.OptionTypes);
        #endregion

        #region Primary Key
        // Configure the primary key for the OptionType entity.
        builder.HasKey(keyExpression: ot => ot.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: ot => ot.Name).IsUnique();
        #endregion

        #region Properties
        // Configure properties for the OptionType entity.
        builder.Property(propertyExpression: ot => ot.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the option type. Value generated never.");

        builder.Property(propertyExpression: ot => ot.Name)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired()
            .HasComment(comment: "Name: The unique name of the option type (e.g., 'Color', 'Size').");

        builder.Property(propertyExpression: ot => ot.Presentation)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired()
            .HasComment(comment: "Presentation: The human-readable display name of the option type.");

        builder.Property(propertyExpression: ot => ot.Position)
            .IsRequired()
            .HasComment(comment: "Position: The display order of the option type.");

        builder.Property(propertyExpression: ot => ot.PublicMetadata)
            .HasColumnType(typeName: "jsonb") // Assuming jsonb for PostgreSQL
            .IsRequired(required: false)
            .HasComment(comment: "PublicMetadata: JSONB field for public-facing key-value pairs (metadata).");

        builder.Property(propertyExpression: ot => ot.PrivateMetadata)
            .HasColumnType(typeName: "jsonb") // Assuming jsonb for PostgreSQL
            .IsRequired(required: false)
            .HasComment(comment: "PrivateMetadata: JSONB field for internal or sensitive key-value pairs (metadata).");

        // Apply common configurations using extension methods.
        builder.ConfigureParameterizableName();
        builder.ConfigurePosition();
        builder.ConfigureMetadata();
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the OptionType entity.
        builder.HasMany(navigationExpression: ot => ot.OptionValues)
            .WithOne(navigationExpression: ov => ov.OptionType)
            .HasForeignKey(foreignKeyExpression: ov => ov.OptionTypeId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: ot => ot.ProductOptionTypes)
            .WithOne(navigationExpression: pot => pot.OptionType)
            .HasForeignKey(foreignKeyExpression: pot => pot.OptionTypeId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion
    }
}
