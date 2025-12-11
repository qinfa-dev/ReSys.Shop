namespace ReSys.Core.Domain.Location.States;

/// <summary>
/// Configures the database mapping for the <see cref="State"/> entity.
/// </summary>
public sealed class StateConfiguration : IEntityTypeConfiguration<State>
{
    /// <summary>
    /// Configures the entity of type <see cref="State"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<State> builder)
    {
        #region Table
        // Set the table name for the State entity.
        builder.ToTable(name: Schema.States);
        #endregion

        #region Primary Key
        // Configure the primary key for the State entity.
        builder.HasKey(keyExpression: s => s.Id);
        #endregion

        #region Properties
        // Configure individual properties of the State entity.
        builder.Property(propertyExpression: s => s.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the state.");

        builder.Property(propertyExpression: s => s.Name)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired()
            .HasComment(comment: "Name: The full name of the state. Required.");

        builder.Property(propertyExpression: s => s.Abbr)
            .HasMaxLength(maxLength: CommonInput.Constraints.DateAndTime.TimeMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Abbr: The abbreviation for the state (e.g., 'CA'). Optional.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        builder.Property(propertyExpression: s => s.CountryId)
            .IsRequired()
            .HasComment(comment: "CountryId: Foreign key to the Country entity.");

        builder.HasOne(navigationExpression: s => s.Country)
            .WithMany(navigationExpression: c => c.States)
            .HasForeignKey(foreignKeyExpression: s => s.CountryId)
            .OnDelete(deleteBehavior: DeleteBehavior.SetNull); // Prevent deletion of a country if it has associated states.

        builder.HasMany(navigationExpression: s => s.UserAddresses)
            .WithOne(navigationExpression: ua => ua.State)
            .HasForeignKey(foreignKeyExpression: ua => ua.StateId)
            .IsRequired(required: false)
            .OnDelete(deleteBehavior: DeleteBehavior.SetNull); // Prevent deletion of a state if it has associated user addresses.

        builder.HasMany(navigationExpression: s => s.StockLocations)
            .WithOne(navigationExpression: sl => sl.State)
            .HasForeignKey(foreignKeyExpression: sl => sl.StateId)
            .IsRequired(required: false)
            .OnDelete(deleteBehavior: DeleteBehavior.SetNull); // Prevent deletion of a state if it has associated stock locations.
        #endregion
    }
}
