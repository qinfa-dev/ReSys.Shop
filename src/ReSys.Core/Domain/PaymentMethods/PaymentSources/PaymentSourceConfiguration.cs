namespace ReSys.Core.Domain.PaymentMethods.PaymentSources;

/// <summary>
/// Configures the database mapping for the <see cref="PaymentSource"/> entity.
/// </summary>
public sealed class PaymentSourceConfiguration : IEntityTypeConfiguration<PaymentSource>
{
    /// <summary>
    /// Configures the entity of type <see cref="PaymentSource"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<PaymentSource> builder)
    {
        #region Table
        // Set the table name for the PaymentSource entity.
        builder.ToTable(name: Schema.PaymentSources);
        #endregion

        #region Primary Key
        // Configure the primary key for the PaymentSource entity.
        builder.HasKey(keyExpression: ps => ps.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: ps => ps.UserId);
        builder.HasIndex(indexExpression: ps => ps.PaymentMethodId);
        builder.HasIndex(indexExpression: ps => ps.IsDefault);
        #endregion

        #region Properties
        // Configure properties for the PaymentSource entity.
        builder.Property(propertyExpression: ps => ps.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the payment source. Value generated never.");

        builder.Property(propertyExpression: ps => ps.UserId)
            .IsRequired()
            .HasComment(comment: "UserId: Foreign key to the associated ApplicationUser.");

        builder.Property(propertyExpression: ps => ps.PaymentMethodId)
            .IsRequired()
            .HasComment(comment: "PaymentMethodId: Foreign key to the associated PaymentMethod.");

        builder.Property(propertyExpression: ps => ps.Type)
            .HasMaxLength(maxLength: PaymentSource.Constraints.TypeMaxLength)
            .IsRequired()
            .HasComment(comment: "Type: The type of payment source (e.g., 'CreditCard', 'PayPal').");

        builder.Property(propertyExpression: ps => ps.Last4)
            .HasMaxLength(maxLength: PaymentSource.Constraints.Last4MaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Last4: The last four digits of the credit card number, if applicable.");

        builder.Property(propertyExpression: ps => ps.Brand)
            .HasMaxLength(maxLength: PaymentSource.Constraints.BrandMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Brand: The brand of the credit card (e.g., 'Visa', 'MasterCard').");

        builder.Property(propertyExpression: ps => ps.ExpirationMonth)
            .IsRequired(required: false)
            .HasComment(comment: "ExpirationMonth: The expiration month of the credit card.");

        builder.Property(propertyExpression: ps => ps.ExpirationYear)
            .IsRequired(required: false)
            .HasComment(comment: "ExpirationYear: The expiration year of the credit card.");

        builder.Property(propertyExpression: ps => ps.IsDefault)
            .IsRequired()
            .HasComment(comment: "IsDefault: Indicates if this is the user's default payment source.");

        // Apply common configurations using extension methods.
        builder.ConfigureMetadata();
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the PaymentSource entity.
        builder.HasOne(navigationExpression: ps => ps.User)
            .WithMany(navigationExpression: user => user.PaymentSources)
            .HasForeignKey(foreignKeyExpression: ps => ps.UserId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade); // Delete payment sources when user is deleted

        builder.HasOne(navigationExpression: ps => ps.PaymentMethod)
            .WithMany(navigationExpression: paymentMethod => paymentMethod.PaymentSources)
            .HasForeignKey(foreignKeyExpression: ps => ps.PaymentMethodId)
            .OnDelete(deleteBehavior: DeleteBehavior.Restrict); // Prevent deleting payment method if payment sources exist
        #endregion
    }
}
