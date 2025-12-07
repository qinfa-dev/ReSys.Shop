using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Payments;

/// <summary>
/// Configures the database mapping for the <see cref="PaymentMethod"/> entity.
/// </summary>
public sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    /// <summary>
    /// Configures the entity of type <see cref="PaymentMethod"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        #region Table
        // Set the table name for the PaymentMethod entity.
        builder.ToTable(name: Schema.PaymentMethods);
        #endregion

        #region Primary Key
        // Configure the primary key for the PaymentMethod entity.
        builder.HasKey(keyExpression: pm => pm.Id);
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: pm => pm.Name).IsUnique();
        #endregion

        #region Properties
        // Configure properties for the PaymentMethod entity.
        builder.Property(propertyExpression: pm => pm.Id)
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the payment method. Value generated never.");

        builder.Property(propertyExpression: pm => pm.Name)
            .IsRequired()
            .HasMaxLength(maxLength: PaymentMethod.Constraints.NameMaxLength)
            .HasComment(comment: "Name: The name of the payment method.");

        builder.Property(propertyExpression: pm => pm.Description)
            .HasMaxLength(maxLength: CommonInput.Constraints.Text.DescriptionMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "Description: A detailed description of the payment method.");

        builder.Property(propertyExpression: pm => pm.Type)
            .IsRequired()
            .HasConversion<string>() // Storefront enum as string
            .HasComment(comment: "Type: The type of payment method (e.g., 'CreditCard', 'PayPal').");

        builder.Property(propertyExpression: pm => pm.Active)
            .IsRequired()
            .HasComment(comment: "Active: Indicates if the payment method is active.");

        builder.Property(propertyExpression: pm => pm.AutoCapture)
            .IsRequired()
            .HasComment(comment: "AutoCapture: Indicates if payments made with this method should be automatically captured.");

        builder.Property(propertyExpression: pm => pm.DisplayOn)
            .IsRequired()
            .HasConversion<string>() // Storefront enum as string
            .HasComment(comment: "DisplayOn: Specifies where the payment method should be displayed (e.g., 'Frontend', 'Backend').");

        builder.Property(propertyExpression: pm => pm.DeletedAt)
            .IsRequired(required: false) // Nullable
            .HasComment(comment: "DeletedAt: The timestamp when the payment method was soft deleted (null if not deleted).");

        // Apply common configurations using extension methods.
        builder.ConfigureParameterizableName();
        builder.ConfigurePosition();
        builder.ConfigureMetadata();
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the PaymentMethod entity.
        builder.HasMany(navigationExpression: pm => pm.StorePaymentMethods)
            .WithOne(navigationExpression: spm => spm.PaymentMethod)
            .HasForeignKey(foreignKeyExpression: spm => spm.PaymentMethodId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasMany(navigationExpression: pm => pm.Payments)
            .WithOne(navigationExpression: p => p.PaymentMethod)
            .HasForeignKey(foreignKeyExpression: p => p.PaymentMethodId)
            .OnDelete(deleteBehavior: DeleteBehavior.Restrict); // Payments should not be deleted if payment method is deleted
        #endregion
    }
}
