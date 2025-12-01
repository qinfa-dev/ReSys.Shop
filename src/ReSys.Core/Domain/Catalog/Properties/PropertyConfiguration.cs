using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Catalog.Properties;

/// <summary>
/// Configures the database mapping for the <see cref="Property"/> entity.
/// </summary>
public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    /// <summary>
    /// Configures the entity of type <see cref="Property"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        #region Table
        // Set the table name for the Property entity.
        builder.ToTable(name: Schema.Properties);
        #endregion

        #region Primary Key
        // Configure the primary key for the Property entity.
        builder.HasKey(keyExpression: p => p.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: p => p.Name).IsUnique();
        builder.HasIndex(indexExpression: p => p.Position);
        builder.HasIndex(indexExpression: p => p.FilterParam).IsUnique();
        #endregion

        #region Properties
        // Configure properties for the Property entity.
        builder.Property(propertyExpression: p => p.Id)
            .HasColumnName(name: "id")
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the property. Value generated never.");

        builder.Property(propertyExpression: p => p.Name)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired()
            .HasComment(comment: "Name: The unique internal name of the property (e.g., 'color', 'material').");

        builder.Property(propertyExpression: p => p.Presentation)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired()
            .HasComment(comment: "Presentation: The human-readable display name of the property (e.g., 'Color', 'Material').");

        builder.Property(propertyExpression: p => p.Kind)
            .HasConversion<string>()
            .HasMaxLength(maxLength: 50)
            .IsRequired()
            .HasComment(comment: "Kind: The data type or input type of the property (e.g., 'ShortText', 'Number', 'Boolean').");

        builder.Property(propertyExpression: p => p.Filterable)
            .IsRequired()
            .HasComment(comment: "Filterable: Indicates if this property can be used for filtering products.");

        builder.Property(propertyExpression: p => p.DisplayOn)
            .HasConversion<string>()
            .HasMaxLength(maxLength: 50)
            .IsRequired()
            .HasComment(comment: "DisplayOn: Specifies where the property should be displayed in the UI (e.g., 'Both', 'Shop', 'Admin').");

        builder.Property(propertyExpression: p => p.Position)
            .IsRequired()
            .HasComment(comment: "Position: The display order of the property.");

        builder.Property(propertyExpression: p => p.FilterParam)
            .HasMaxLength(maxLength: Property.Constraints.FilterParamMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "FilterParam: A URL-friendly slug for filtering based on this property.");

        builder.Property(propertyExpression: p => p.PublicMetadata)
            .HasColumnType(typeName: "jsonb") // Assuming jsonb for PostgreSQL
            .IsRequired(required: false)
            .HasComment(comment: "PublicMetadata: JSONB field for public-facing key-value pairs (metadata).");

        builder.Property(propertyExpression: p => p.PrivateMetadata)
            .HasColumnType(typeName: "jsonb") // Assuming jsonb for PostgreSQL
            .IsRequired(required: false)
            .HasComment(comment: "PrivateMetadata: JSONB field for internal or sensitive key-value pairs (metadata).");

        // Apply common configurations using extension methods.
        builder.ConfigureParameterizableName();
        builder.ConfigurePosition();
        builder.ConfigureMetadata();
        builder.ConfigureAuditable();
        builder.ConfigureUniqueName();
        builder.ConfigureDisplayOn();
        builder.ConfigureFilterParam();
        #endregion

        #region Relationships
        // Configure relationships for the Property entity.
        builder.HasMany(navigationExpression: p => p.ProductProperties)
            .WithOne(navigationExpression: pp => pp.Property)
            .HasForeignKey(foreignKeyExpression: pp => pp.PropertyId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion

        #region Ignored Properties
        //builder.Ignore(propertyExpression: p => p.Products);
        #endregion
    }
}
