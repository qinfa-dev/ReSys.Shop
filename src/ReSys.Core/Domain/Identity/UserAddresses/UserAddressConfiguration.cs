namespace ReSys.Core.Domain.Identity.UserAddresses;

/// <summary>
/// Configures the database mapping for the <see cref="UserAddress"/> entity.
/// </summary>
public sealed class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
{
    /// <summary>
    /// Configures the entity of type <see cref="UserAddress"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<UserAddress> builder)
    {
        #region Table
        // Set the table name for the UserAddress entity.
        builder.ToTable(name: Schema.UserAddresses);
        #endregion

        #region Primary Key
        // Configure the primary key for the UserAddress entity.
        builder.HasKey(keyExpression: ua => ua.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: ua => ua.UserId);
        #endregion

        #region Properties
        // Configure properties for the UserAddress entity.
        builder.Property(propertyExpression: ua => ua.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the user address. Value generated never.");

        builder.Property(propertyExpression: ua => ua.FirstName)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired()
            .HasComment(comment: "FirstName: The first name of the recipient. Required.");

        builder.Property(propertyExpression: ua => ua.LastName)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired()
            .HasComment(comment: "LastName: The last name of the recipient. Required.");

        builder.Property(propertyExpression: ua => ua.Label)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .HasComment(comment: "Label: A user-defined label for the address (e.g., 'Home', 'Work').");

        builder.Property(propertyExpression: ua => ua.Type)
            .HasConversion<string>()
            .HasMaxLength(maxLength: 50)
            .IsRequired()
            .HasComment(comment: "Type: The type of address (e.g., Shipping, Billing). Stored as a string.");

        builder.Property(propertyExpression: a => a.Company)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .HasComment(comment: "Company: The company name associated with the address.");

        builder.Property(propertyExpression: a => a.Address1)
            .HasMaxLength(maxLength: CommonInput.Constraints.Text.ShortTextMaxLength)
            .HasComment(comment: "Address1: The primary line of the street address.");

        builder.Property(propertyExpression: a => a.Address2)
            .HasMaxLength(maxLength: CommonInput.Constraints.Text.ShortTextMaxLength)
            .HasComment(comment: "Address2: The secondary line of the street address (e.g., apartment, suite).");

        builder.Property(propertyExpression: a => a.City)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .HasComment(comment: "City: The city of the address.");

        builder.Property(propertyExpression: a => a.Zipcode)
            .ConfigurePostalCode(isRequired: false)
            .HasComment(comment: "Zipcode: The postal code of the address.");

        builder.Property(propertyExpression: a => a.Phone)
            .ConfigurePhone(isRequired: false)
            .HasComment(comment: "Phone: The phone number associated with the address.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the UserAddress entity.
        builder.HasOne(navigationExpression: ua => ua.User)
            .WithMany(navigationExpression: u => u.UserAddresses)
            .HasForeignKey(foreignKeyExpression: ua => ua.UserId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasOne(navigationExpression: a => a.Country)
            .WithMany(navigationExpression: m => m.UserAddresses)
            .HasForeignKey(foreignKeyExpression: a => a.CountryId)
            .IsRequired(required: false);

        builder.HasOne(navigationExpression: a => a.State)
            .WithMany(navigationExpression: m => m.UserAddresses)
            .HasForeignKey(foreignKeyExpression: a => a.StateId)
            .IsRequired(required: false);
        #endregion
    }
}
