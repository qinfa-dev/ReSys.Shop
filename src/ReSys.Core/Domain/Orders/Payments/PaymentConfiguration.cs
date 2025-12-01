using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Constants;

namespace ReSys.Core.Domain.Orders.Payments;

/// <summary>
/// Configures the database mapping for the <see cref="Payment"/> entity.
/// </summary>
public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    /// <summary>
    /// Configures the entity of type <see cref="Payment"/>.
    /// </summary>
    /// <param name="builder">The builder to be used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        #region Table
        // Set the table name for the Payment entity.
        builder.ToTable(name: Schema.Payments);
        #endregion

        #region Primary Key
        // Configure the primary key for the Payment entity.
        builder.HasKey(keyExpression: p => p.Id);
        #endregion

        #region Properties
        // Configure properties for the Payment entity.
        builder.Property(propertyExpression: p => p.Id)
            .HasColumnName(name: "id")
            .ValueGeneratedNever()
            .HasComment(comment: "Id: Unique identifier for the payment. Value generated never.");

        builder.Property(propertyExpression: p => p.OrderId)
            .IsRequired()
            .HasComment(comment: "OrderId: Foreign key to the associated Order.");

        builder.Property(propertyExpression: p => p.AmountCents)
            .IsRequired()
            .HasComment(comment: "AmountCents: The payment amount in cents.");

        builder.Property(propertyExpression: p => p.Currency)
            .HasMaxLength(maxLength: CommonInput.Constraints.Text.TinyTextMaxLength)
            .IsRequired()
            .HasDefaultValue(value: "USD")
            .HasComment(comment: "Currency: The currency of the payment.");

        builder.Property(propertyExpression: p => p.State)
            .HasConversion<string>()
            .HasMaxLength(maxLength: 50)
            .IsRequired()
            .HasComment(comment: "State: Current state of the payment (e.g., Pending, Completed).");

        builder.Property(propertyExpression: p => p.CapturedAt)
            .IsRequired(required: false)
            .HasComment(comment: "CapturedAt: Timestamp when the payment was captured.");

        builder.Property(propertyExpression: p => p.VoidedAt)
            .IsRequired(required: false)
            .HasComment(comment: "VoidedAt: Timestamp when the payment was voided.");

        builder.Property(propertyExpression: p => p.RefundedAt)
            .IsRequired(required: false)
            .HasComment(comment: "RefundedAt: Timestamp when the payment was refunded.");

        builder.Property(propertyExpression: p => p.PaymentMethodType)
            .HasMaxLength(maxLength: Payment.Constraints.PaymentMethodTypeMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "PaymentMethodType: The type of payment method used.");

        builder.Property(propertyExpression: p => p.TransactionId)
            .HasMaxLength(maxLength: Payment.Constraints.TransactionIdMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "TransactionId: The transaction ID from the payment gateway.");

        builder.Property(propertyExpression: p => p.FailureReason)
            .HasMaxLength(maxLength: Payment.Constraints.FailureReasonMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "FailureReason: Reason for payment failure.");

        builder.Property(propertyExpression: p => p.PaymentMethodId)
            .IsRequired(required: false)
            .HasComment(comment: "PaymentMethodId: Foreign key to the associated PaymentMethod.");

        // Apply common configurations using extension methods.
        builder.ConfigureAuditable();
        #endregion

        #region Relationships
        // Configure relationships for the Payment entity.
        builder.HasOne(navigationExpression: p => p.Order)
            .WithMany(navigationExpression: o => o.Payments)
            .HasForeignKey(foreignKeyExpression: p => p.OrderId)
            .OnDelete(deleteBehavior: DeleteBehavior.Cascade);

        builder.HasOne(navigationExpression: p => p.PaymentMethod)
            .WithMany(navigationExpression: pm => pm.Payments)
            .HasForeignKey(foreignKeyExpression: p => p.PaymentMethodId)
            .IsRequired(required: false)
            .OnDelete(deleteBehavior: DeleteBehavior.SetNull); // Payment method can be deleted, but payment remains
        #endregion

        #region Indexes
        // Configure indexes for frequently queried columns to improve performance.
        builder.HasIndex(indexExpression: p => p.OrderId);
        builder.HasIndex(indexExpression: p => p.PaymentMethodId);
        builder.HasIndex(indexExpression: p => p.State);
        builder.HasIndex(indexExpression: p => p.TransactionId).IsUnique(unique: false); // Not necessarily unique
        #endregion

        #region Ignored Properties
        // Ignore domain-specific or computed properties that should not be mapped to the database.
        builder.Ignore(propertyExpression: p => p.IsCompleted);
        builder.Ignore(propertyExpression: p => p.IsPending);
        builder.Ignore(propertyExpression: p => p.IsVoid);
        builder.Ignore(propertyExpression: p => p.IsRefunded);
        builder.Ignore(propertyExpression: p => p.Amount);
        builder.Ignore(propertyExpression: p => p.IsFailed);
        #endregion
    }
}