namespace ReSys.Core.Domain.Inventories.Locations;

/// <summary>
/// Configures the database mapping for the <see cref="StockLocation"/> entity.
/// </summary>
public sealed class StockLocationConfiguration : IEntityTypeConfiguration<StockLocation>
{
    /// <summary>
    /// Configures the entity of type <see cref="StockLocation"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<StockLocation> builder)
    {
        #region Table
        // Set the table name for the StockLocation entity.
        builder.ToTable(name: Schema.StockLocations);
        #endregion

        #region Primary Key
        // Configure the primary key for the StockLocation entity.
        builder.HasKey(keyExpression: sl => sl.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: sl => sl.Name).IsUnique();
        builder.HasIndex(indexExpression: sl => sl.Active);
        builder.HasIndex(indexExpression: sl => sl.Default);
        builder.HasIndex(indexExpression: sl => new { sl.ShipEnabled, sl.IsDeleted })
            .HasDatabaseName("IX_StockLocation_ShipEnabled_IsDeleted");
        builder.HasIndex(indexExpression: sl => new { sl.PickupEnabled, sl.IsDeleted })
            .HasDatabaseName("IX_StockLocation_PickupEnabled_IsDeleted");
        builder.HasIndex(indexExpression: sl => new { sl.Latitude, sl.Longitude })
            .HasDatabaseName("IX_StockLocation_Coordinates");
        #endregion

        #region Properties
        // Configure properties for the StockLocation entity.
        builder.Property(propertyExpression: sl => sl.Id)
            .HasColumnName(name: "id")
            .ValueGeneratedNever()
            .HasComment(comment: "Unique identifier for the stock location.");

        // Apply common configurations using extension methods.
        builder.ConfigureParameterizableName();
        builder.ConfigureUniqueName();

        builder.Property(propertyExpression: sl => sl.Name)
            .HasComment(comment: "Name: The internal system name for the stock location (e.g., 'main-warehouse', 'nyc-store').");

        builder.Property(propertyExpression: sl => sl.Presentation)
            .HasComment(comment: "Presentation: The human-readable display name for the stock location (e.g., 'Main Warehouse', 'NYC Retail Store').");

        builder.Property(propertyExpression: sl => sl.Active)
            .IsRequired()
            .HasDefaultValue(value: true)
            .HasComment(comment: "Active: Indicates if the stock location is active. Required, defaults to true.");

        builder.Property(propertyExpression: sl => sl.Default)
            .IsRequired()
            .HasDefaultValue(value: false)
            .HasComment(comment: "Default: Indicates if this is the default stock location. Required, defaults to false.");

        builder.Property(propertyExpression: a => a.Company)
            .HasMaxLength(maxLength: StockLocation.Constraints.CompanyMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Company: The company name associated with the stock location address.");

        builder.Property(propertyExpression: a => a.Address1)
            .HasMaxLength(maxLength: StockLocation.Constraints.AddressMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Address1: The primary line of the street address.");

        builder.Property(propertyExpression: a => a.Address2)
            .HasMaxLength(maxLength: StockLocation.Constraints.AddressMaxLength)
            .IsRequired(required: false) 
            .HasComment(comment: "Address2: The secondary line of the street address.");

        builder.Property(propertyExpression: a => a.City)
            .HasMaxLength(maxLength: StockLocation.Constraints.CityMaxLength)
            .IsRequired(required: false) 
            .HasComment(comment: "City: The city of the address.");

        builder.Property(propertyExpression: a => a.Zipcode)
            .ConfigurePostalCode(isRequired: false)
            .HasMaxLength(maxLength: StockLocation.Constraints.ZipcodeMaxLength)
            .HasComment(comment: "Zipcode: The postal code of the address.");

        builder.Property(propertyExpression: a => a.Phone)
            .ConfigurePhone(isRequired: false)
            .HasMaxLength(maxLength: StockLocation.Constraints.PhoneMaxLength)
            .HasComment(comment: "Phone: The phone number associated with the address.");

        builder.Property(propertyExpression: a => a.Email)
            .HasMaxLength(maxLength: StockLocation.Constraints.EmailMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Email: The email address associated with the stock location.");

        builder.Property(propertyExpression: sl => sl.Type)
            .ConfigurePostgresEnum()
            .HasComment(comment: "Type: Enum indicating location type (Warehouse, RetailStore, Both). Stored as string for flexibility.");

        builder.Property(propertyExpression: sl => sl.ShipEnabled)
            .IsRequired()
            .HasDefaultValue(true)
            .HasComment(comment: "ShipEnabled: Whether this location can ship orders. Defaults to true.");

        builder.Property(propertyExpression: sl => sl.PickupEnabled)
            .IsRequired()
            .HasDefaultValue(false)
            .HasComment(comment: "PickupEnabled: Whether this location supports store pickup. Defaults to false.");

        builder.Property(propertyExpression: sl => sl.Latitude)
            .HasPrecision(9, 6)
            .IsRequired(required: false)
            .HasComment(comment: "Latitude: Geographic latitude coordinate (-90 to 90) for distance calculations.");

        builder.Property(propertyExpression: sl => sl.Longitude)
            .HasPrecision(9, 6)
            .IsRequired(required: false)
            .HasComment(comment: "Longitude: Geographic longitude coordinate (-180 to 180) for distance calculations.");

        builder.Property(propertyExpression: sl => sl.OperatingHours)
            .ConfigureDictionary(false)
            .HasColumnType("jsonb")
            .HasComment(comment: "OperatingHours: JSON dictionary of operating hours by day of week (e.g., {\"Monday\": \"09:00-17:00\"}).");

        builder.ConfigureMetadata();
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the StockLocation entity.
        builder.HasMany(navigationExpression: sl => sl.StockItems)
            .WithOne(navigationExpression: si => si.StockLocation)
            .HasForeignKey(foreignKeyExpression: si => si.StockLocationId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasOne(navigationExpression: a => a.Country)
            .WithMany(navigationExpression: m => m.StockLocations)
            .HasForeignKey(foreignKeyExpression: a => a.CountryId)
            .OnDelete(deleteBehavior: DeleteBehavior.SetNull);

        builder.HasOne(navigationExpression: a => a.State)
            .WithMany(navigationExpression: m => m.StockLocations)
            .HasForeignKey(foreignKeyExpression: a => a.StateId)
            .OnDelete(deleteBehavior: DeleteBehavior.SetNull);
        #endregion
    }
}
