using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Catalog.Properties;

/// <summary>
/// Configures the database mapping for the <see cref="Property"/> entity using Entity Framework Core's Fluent API.
/// This configuration ensures that the <see cref="Property"/> aggregate is correctly mapped to the database schema,
/// including table name, primary key, indexes, property constraints, and relationships.
/// </summary>
/// <remarks>
/// This class plays a crucial role in defining how the <see cref="Property"/> domain model is persisted.
/// It sets up:
/// <list type="bullet">
/// <item><term>Table Name</term><description>Maps the entity to the 'properties' table in the database.</description></item>
/// <item><term>Primary Key</term><description>Defines 'Id' as the primary key.</description></item>
/// <item><term>Indexes</term><description>Creates unique and non-unique indexes on frequently queried columns (<c>Name</c>, <c>Position</c>, <c>FilterParam</c>) for performance.</description></item>
/// <item><term>Property Mappings</term><description>Configures column names, maximum lengths, nullability, and database comments for each property.</description></item>
/// <item><term>Type Conversions</term><description>Handles conversions for enums like <c>PropertyKind</c> and <c>DisplayOn</c> to string for database storage.</description></item>
/// <item><term>JSONB for Metadata</term><description>Specifies 'jsonb' column type for <c>PublicMetadata</c> and <c>PrivateMetadata</c> for flexible schema-less data storage (PostgreSQL specific).</description></item>
/// <item><term>Common Concerns</term><description>Applies reusable EF Core configurations for domain concerns like auditable fields, unique naming, and metadata handling via extension methods.</description></item>
/// <item><term>Relationships</term><description>Configures the one-to-many relationship with <c>ProductProperty</c> and sets up cascade delete behavior.</description></item>
/// </list>
/// </remarks>
public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    /// <summary>
    /// Configures the database mapping for the <see cref="Property"/> entity.
    /// This method is part of the Entity Framework Core <see cref="IEntityTypeConfiguration{TEntity}"/> interface
    /// and is automatically invoked by EF Core during model creation.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    /// <remarks>
    /// The configuration covers:
    /// <list type="bullet">
    /// <item><term>Table and Primary Key Definition</term><description>Sets the table name to <c>Schema.Properties</c> and defines <c>Id</c> as the primary key.</description></item>
    /// <item><term>Index Creation</term><description>Establishes indexes on <c>Name</c>, <c>Position</c>, and <c>FilterParam</c> to optimize data retrieval, with <c>Name</c> and <c>FilterParam</c> being unique.</description></item>
    /// <item><term>Property Mapping Details</term><description>Specifies detailed mapping for each property of the <see cref="Property"/> entity, including column names, max lengths, required status, and database comments. Enum properties (<c>Kind</c>, <c>DisplayOn</c>) are configured to be stored as strings.</description></item>
    /// <item><term>Metadata Storage</term><description><c>PublicMetadata</c> and <c>PrivateMetadata</c> are mapped as 'jsonb' columns, suitable for storing flexible JSON data in PostgreSQL.</description></item>
    /// <item><term>Reusable Configuration Concerns</term><description>Utilizes extension methods (e.g., <c>ConfigureAuditable()</c>, <c>ConfigureUniqueName()</c>) to apply common EF Core mapping patterns inherited from domain concerns.</description></item>
    /// <item><term>Relationships</term><description>Defines the one-to-many relationship with <c>ProductProperties</c>, ensuring referential integrity and specifying cascade delete behavior, meaning that if a <see cref="Property"/> is deleted, all its associated <c>ProductProperty</c> entries will also be removed.</description></item>
    /// </list>
    /// </remarks>
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
