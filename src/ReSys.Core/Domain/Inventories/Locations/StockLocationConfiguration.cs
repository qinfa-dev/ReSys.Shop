using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

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
        #endregion

        #region Properties
        // Configure properties for the StockLocation entity.
        builder.Property(propertyExpression: sl => sl.Id)
            .HasColumnName(name: "id")
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the stock location. Value generated never.");

        // Apply common configurations using extension methods.
        builder.ConfigureParameterizableName();
        builder.ConfigureUniqueName();

        builder.Property(propertyExpression: sl => sl.Active)
            .IsRequired()
            .HasDefaultValue(value: true)
            .HasComment(comment: "Active: Indicates if the stock location is active. Required, defaults to true.");

        builder.Property(propertyExpression: sl => sl.Default)
            .IsRequired()
            .HasDefaultValue(value: false)
            .HasComment(comment: "Default: Indicates if this is the default stock location. Required, defaults to false.");

        builder.Property(propertyExpression: a => a.Company)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Company: The company name associated with the stock location address.");

        builder.Property(propertyExpression: a => a.Address1)
            .HasMaxLength(maxLength: CommonInput.Constraints.Text.ShortTextMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Address1: The primary line of the street address.");

        builder.Property(propertyExpression: a => a.Address2)
            .HasMaxLength(maxLength: CommonInput.Constraints.Text.ShortTextMaxLength)
            .IsRequired(required: false) 
            .HasComment(comment: "Address2: The secondary line of the street address.");

        builder.Property(propertyExpression: a => a.City)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired(required: false) 
            .HasComment(comment: "City: The city of the address.");

        builder.Property(propertyExpression: a => a.Zipcode)
            .ConfigurePostalCode(isRequired: false)
            .HasComment(comment: "Zipcode: The postal code of the address.");

        builder.Property(propertyExpression: a => a.Phone)
            .ConfigurePhone(isRequired: false)
            .HasComment(comment: "Phone: The phone number associated with the address.");

        builder.ConfigureMetadata();
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the StockLocation entity.
        builder.HasMany(navigationExpression: sl => sl.StockItems)
            .WithOne(navigationExpression: si => si.StockLocation)
            .HasForeignKey(foreignKeyExpression: si => si.StockLocationId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: sl => sl.StoreStockLocations)
            .WithOne(navigationExpression: sls => sls.StockLocation)
            .HasForeignKey(foreignKeyExpression: sls => sls.StockLocationId)
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

        #region Ignored Properties
        // Ignore domain-specific or computed properties that should not be mapped to the database.
        builder.Ignore(propertyExpression: sl => sl.Stores);
        #endregion
    }
}
