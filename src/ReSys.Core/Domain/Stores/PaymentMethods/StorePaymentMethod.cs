using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Settings.PaymentMethods;

namespace ReSys.Core.Domain.Stores.PaymentMethods;

/// <summary>
/// Represents the relationship between a Store and a PaymentMethod.
/// Controls payment method availability on a per-store basis.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong>
/// This owned entity maps payment methods to specific stores and controls which payment
/// options are available to customers in each store. Enables regional payment provider selection
/// and PCI compliance isolation.
/// </para>
/// <para>
/// <strong>Key Characteristics:</strong>
/// <list type="bullet">
/// <item><description>Composite FK: Unique per Store + PaymentMethod combination</description></item>
/// <item><description>Availability Control: Enable/disable payment method per store</description></item>
/// <item><description>Regional Selection: Different stores can offer different payment methods</description></item>
/// <item><description>Auditable: CreatedAt/UpdatedAt for compliance tracking</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class StorePaymentMethod : AuditableEntity
{
    #region Errors
    public static class Errors
    {
        public  static Error Required => Error.Validation(code: "StorePaymentMethod.Required", description: "Storefront payment method requires both StoreId and PaymentMethodId.");
        public  static Error AlreadyLinked => Error.Conflict(code: "StorePaymentMethod.AlreadyLinked", description: "Payment method is already linked to this store.");
        public static Error NotFound(Guid id) => Error.NotFound(code: "StorePaymentMethod.NotFound", description: $"Storefront payment method with ID '{id}' was not found.");
    }
    #endregion

    #region Properties
    /// <summary>The ID of the Store this payment method is available in.</summary>
    public Guid? StoreId { get; set; }
    
    /// <summary>The ID of the PaymentMethod being offered in this store.</summary>
    public Guid PaymentMethodId { get; set; }
    
    /// <summary>
    /// Indicates if this payment method is available to customers during checkout.
    /// Default: true. Set to false to temporarily disable (e.g., during gateway maintenance).
    /// </summary>
    public bool Available { get; set; } = true;
    #endregion

    #region Relationships
    public Store? Store { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    #endregion

    #region Constructors
    private StorePaymentMethod() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Creates a new store-to-payment-method mapping with availability configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Pre-Conditions:</strong>
    /// <list type="bullet">
    /// <item><description>storeId must not be empty</description></item>
    /// <item><description>paymentMethodId must not be empty</description></item>
    /// <item><description>Store and PaymentMethod must exist</description></item>
    /// <item><description>Method should not already be linked to this store</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static ErrorOr<StorePaymentMethod> Create(
        Guid? storeId,
        Guid paymentMethodId,
        bool available = true)
    {
        return new StorePaymentMethod
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            PaymentMethodId = paymentMethodId,
            Available = available,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
    #endregion

    #region Business Logic
    /// <summary>
    /// Updates the availability of this payment method in the store.
    /// </summary>
    /// <remarks>
    /// Only updates if new value differs from current. Returns updated entity for method chaining.
    /// </remarks>
    public ErrorOr<StorePaymentMethod> Update(bool? available = null)
    {
        bool changed = false;
        if (available.HasValue && available != Available) { Available = available.Value; changed = true; }
        if (changed) UpdatedAt = DateTimeOffset.UtcNow;
        return this;
    }

    /// <summary>
    /// Marks this store-payment-method mapping for deletion.
    /// </summary>
    /// <remarks>
    /// This should be called through Store.RemovePaymentMethod() rather than directly.
    /// Always use the aggregate root to maintain consistency.
    /// </remarks>
    public ErrorOr<Deleted> Delete() => Result.Deleted;
    #endregion
}