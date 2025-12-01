using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Location.Countries;

/// <summary>
/// Configures the database mapping for the <see cref="Country"/> entity.
/// </summary>
public sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    /// <summary>
    /// Configures the entity of type <see cref="Country"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        #region Table
        // Set the table name for the Country entity.
        builder.ToTable(name: Schema.Countries);
        #endregion

        #region Primary Key
        // Configure the primary key for the Country entity.
        builder.HasKey(keyExpression: c => c.Id);
        #endregion

        #region Properties
        // Configure individual properties of the Country entity.
        builder.Property(propertyExpression: c => c.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the country.");

        builder.Property(propertyExpression: c => c.Name)
            .ConfigureInput(maxLength: CommonInput.Constraints.Text.TinyTextMaxLength)
            .HasComment(comment: "Name: The full name of the country. Required.");

        builder.Property(propertyExpression: c => c.Iso)
            .ConfigureInput(maxLength: Country.Constraints.IsoMaxLength) 
            .HasComment(comment: "Iso: The two-letter ISO code for the country (e.g., 'US'). Required.");

        builder.Property(propertyExpression: c => c.Iso3)
            .ConfigureInput(maxLength: Country.Constraints.Iso3MaxLength) 
            .HasComment(comment: "Iso3: The three-letter ISO code for the country (e.g., 'USA'). Required.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        builder.HasMany(navigationExpression: c => c.States)
            .WithOne(navigationExpression: s => s.Country)
            .HasForeignKey(foreignKeyExpression: s => s.CountryId)
            .OnDelete(deleteBehavior: DeleteBehavior.SetNull); // Prevent deletion of a country if it has associated states.

        builder.HasMany(navigationExpression: c => c.UserAddresses)
            .WithOne(navigationExpression: ua => ua.Country)
            .HasForeignKey(foreignKeyExpression: ua => ua.CountryId)
            .OnDelete(deleteBehavior: DeleteBehavior.SetNull); // Prevent deletion of a country if it has associated user addresses.

        builder.HasMany(navigationExpression: c => c.StockLocations)
            .WithOne(navigationExpression: sl => sl.Country)
            .HasForeignKey(foreignKeyExpression: sl => sl.CountryId)
            .OnDelete(deleteBehavior: DeleteBehavior.SetNull); // Prevent deletion of a country if it has associated stock locations.
        #endregion
    }
}
