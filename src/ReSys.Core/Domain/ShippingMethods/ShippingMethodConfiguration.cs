namespace ReSys.Core.Domain.ShippingMethods;

/// <summary>
/// Configures the Entity Framework Core database mapping for the <see cref="ShippingMethod"/> aggregate root.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong>
/// Defines how ShippingMethod domain model maps to the database (PostgreSQL), including:
/// <list type="bullet">
/// <item><description>Table and column structure</description></item>
/// <item><description>Data type conversions (e.g., enum → string)</description></item>
/// <item><description>Constraints and indices for performance/consistency</description></item>
/// <item><description>Relationships with StoreShippingMethod and Shipment entities</description></item>
/// <item><description>Common concerns configuration (metadata, auditing, positioning)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Concerns Applied:</strong>
/// <list type="bullet">
/// <item><description>IHasUniqueName: Unique index on Name column, enforces business rule</description></item>
/// <item><description>IHasPosition: Manages display ordering via Position property</description></item>
/// <item><description>IHasParameterizableName: Dual Name/Presentation for identifier and display</description></item>
/// <item><description>IHasMetadata: PublicMetadata and PrivateMetadata dictionaries</description></item>
/// <item><description>IHasAuditable: CreatedAt, UpdatedAt automatic tracking</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Key Configuration Decisions:</strong>
/// <list type="bullet">
/// <item><description>Type stored as string (not int ordinal) for database readability and migrations</description></item>
/// <item><description>BaseCost, MaxWeight: decimal(18,2) for financial precision</description></item>
/// <item><description>StoreShippingMethod: Cascade delete (cleanup unused store-method associations)</description></item>
/// <item><description>Shipment: Restrict delete (prevent accidental removal of fulfilled orders)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class ShippingMethodConfiguration : IEntityTypeConfiguration<ShippingMethod>
{
    /// <summary>
    /// Configures the entity of type <see cref="ShippingMethod"/> for database mapping.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Configuration Steps (in order):</strong>
    /// <list type="number">
    /// <item><description><strong>Table:</strong> Maps to "ShippingMethods" table in database</description></item>
    /// <item><description><strong>Primary Key:</strong> Id column (Guid)</description></item>
    /// <item><description><strong>Indexes:</strong> Unique index on Name for O(1) lookups and business rule enforcement</description></item>
    /// <item><description><strong>Properties:</strong> Column types, constraints, defaults, max lengths</description></item>
    /// <item><description><strong>Type Enum:</strong> Stored as string for readability (Standard, Express, etc.)</description></item>
    /// <item><description><strong>Concerns:</strong> Applies IHasParameterizableName, IHasPosition, IHasMetadata, IHasAuditable</description></item>
    /// <item><description><strong>Relationships:</strong> 1:N with StoreShippingMethod (cascade), 1:N with Shipment (restrict)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<ShippingMethod> builder)
    {
        #region Table
        builder.ToTable(name: Schema.ShippingMethods);
        #endregion

        #region Primary Key
        builder.HasKey(keyExpression: sm => sm.Id);
        #endregion

        #region Indexes
        builder.HasIndex(indexExpression: sm => sm.Name).IsUnique();
        #endregion

        #region Properties
        builder.Property(propertyExpression: sm => sm.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the shipping method. Value generated never.");

        builder.Property(propertyExpression: sm => sm.Name)
            .IsRequired()
            .HasMaxLength(maxLength: ShippingMethod.Constraints.NameMaxLength)
            .HasComment(comment: "Name: The name of the shipping method.");

        builder.Property(propertyExpression: sm => sm.Presentation)
            .IsRequired()
            .HasMaxLength(maxLength: ShippingMethod.Constraints.NameMaxLength)
            .HasComment(comment: "Presentation: Display name shown to customers (e.g., 'Ground (5-7 days)').");

        builder.Property(propertyExpression: sm => sm.Description)
            .HasMaxLength(maxLength: CommonInput.Constraints.Text.DescriptionMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Description: A detailed description of the shipping method.");

        builder.Property(propertyExpression: sm => sm.Type)
            .IsRequired()
            .HasConversion<string>() // Store enum as string for readability
            .HasComment(comment: "Type: The type of shipping method (Standard, Express, Overnight, Pickup, FreeShipping).");

        builder.Property(propertyExpression: sm => sm.Active)
            .IsRequired()
            .HasComment(comment: "Active: Indicates if the shipping method is active and available for selection.");

        builder.Property(propertyExpression: sm => sm.BaseCost)
            .IsRequired()
            .HasColumnType(typeName: "decimal(18,2)")
            .HasComment(comment: "BaseCost: The base cost of the shipping method in specified currency.");

        builder.Property(propertyExpression: sm => sm.Currency)
            .IsRequired()
            .HasMaxLength(maxLength: 3) // e.g., "USD", "EUR", "GBP"
            .HasComment(comment: "Currency: ISO 4217 currency code (USD, EUR, GBP, etc.).");

        builder.Property(propertyExpression: sm => sm.EstimatedDaysMin)
            .IsRequired(required: false)
            .HasComment(comment: "EstimatedDaysMin: Minimum estimated delivery days (e.g., 5 for 5-7 days delivery).");

        builder.Property(propertyExpression: sm => sm.EstimatedDaysMax)
            .IsRequired(required: false)
            .HasComment(comment: "EstimatedDaysMax: Maximum estimated delivery days (e.g., 7 for 5-7 days delivery).");

        builder.Property(propertyExpression: sm => sm.MaxWeight)
            .HasColumnType(typeName: "decimal(18,2)")
            .IsRequired(required: false)
            .HasComment(comment: "MaxWeight: Maximum weight eligible for base cost (exceed = 1.5x surcharge).");

        // Apply common configurations using extension methods.
        builder.ConfigureParameterizableName(); // For IHasParameterizableName concern
        builder.ConfigurePosition();              // For IHasPosition concern
        builder.ConfigureMetadata();              // For IHasMetadata concern (PublicMetadata, PrivateMetadata)
        builder.ConfigureAuditable();             // For IHasAuditable concern (CreatedAt, UpdatedAt)
        #endregion

        #region Relationships
        builder.HasMany(navigationExpression: sm => sm.StoreShippingMethods)
            .WithOne(navigationExpression: ssm => ssm.ShippingMethod)
            .HasForeignKey(foreignKeyExpression: ssm => ssm.ShippingMethodId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);
        #endregion
    }
}