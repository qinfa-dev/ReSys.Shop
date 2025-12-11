namespace ReSys.Core.Domain.Catalog.OptionTypes;

/// <summary>
/// Configures the database mapping for the <see cref="OptionValue"/> entity.
/// </summary>
public sealed class OptionValueConfiguration : IEntityTypeConfiguration<OptionValue>
{
    /// <summary>
    /// Configures the entity of type <see cref="OptionValue"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<OptionValue> builder)
    {
        #region Table
        // Set the table name for the OptionValue entity.
        builder.ToTable(name: Schema.OptionValues);
        #endregion

        #region Primary Key
        // Configure the primary key for the OptionValue entity.
        builder.HasKey(keyExpression: ov => ov.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: ov => new { ov.OptionTypeId, ov.Name }).IsUnique();
        #endregion

        #region Properties
        // Configure properties for the OptionValue entity.
        builder.Property(propertyExpression: ov => ov.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the option value. Value generated never.");

        builder.Property(propertyExpression: ov => ov.OptionTypeId)
            .IsRequired()
            .HasComment(comment: "OptionTypeId: Foreign key to the associated OptionType.");

        builder.Property(propertyExpression: ov => ov.Name)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired()
            .HasComment(comment: "Name: The unique name of the option value (e.g., 'Red', 'Small').");

        builder.Property(propertyExpression: ov => ov.Presentation)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired()
            .HasComment(comment: "Presentation: The human-readable display name of the option value.");

        builder.Property(propertyExpression: ov => ov.Position)
            .IsRequired()
            .HasComment(comment: "Position: The display order of the option value.");

        builder.Property(propertyExpression: ov => ov.PublicMetadata)
            .HasColumnType(typeName: "jsonb") // Assuming jsonb for PostgreSQL
            .IsRequired(required: false)
            .HasComment(comment: "PublicMetadata: JSONB field for public-facing key-value pairs (metadata).");

        builder.Property(propertyExpression: ov => ov.PrivateMetadata)
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
        // Configure relationships for the OptionValue entity.
        builder.HasOne(navigationExpression: ov => ov.OptionType)
            .WithMany(navigationExpression: ot => ot.OptionValues)
            .HasForeignKey(foreignKeyExpression: ov => ov.OptionTypeId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: ov => ov.VariantOptionValues)
            .WithOne(navigationExpression: ovv => ovv.OptionValue)
            .HasForeignKey(foreignKeyExpression: ovv => ovv.OptionValueId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion
    }
}
