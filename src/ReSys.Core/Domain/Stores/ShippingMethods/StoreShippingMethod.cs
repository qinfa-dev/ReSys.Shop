using ErrorOr;

using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Shipping;

namespace ReSys.Core.Domain.Stores.ShippingMethods;

/// <summary>
/// Represents the relationship between a Store and a ShippingMethod.
/// Enables per-store shipping method availability and cost customization.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong>
/// This owned entity maps shipping methods to specific stores and allows customization of:
/// <list type="bullet">
/// <item><description>Availability: Enable/disable shipping method per store</description></item>
/// <item><description>Store Base Cost: Override global shipping cost for specific store</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Key Characteristics:</strong>
/// <list type="bullet">
/// <item><description>Composite FK: Unique per Store + ShippingMethod combination</description></item>
/// <item><description>Availability Toggle: Enable/disable method per store independently</description></item>
/// <item><description>Cost Override: Store-specific shipping cost (e.g., EUR vs USD pricing)</description></item>
/// <item><description>Auditable: CreatedAt/UpdatedAt for change tracking</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class StoreShippingMethod : AuditableEntity
{
    #region Errors
    public static class Errors
    {
        public static Error NotFound(Guid id) => Error.NotFound(code: "StoreShippingMethod.NotFound", description: $"Storefront shipping method with ID '{id}' was not found.");
    }
    #endregion

    #region Properties
    /// <summary>The ID of the Store this shipping method is available in.</summary>
    public Guid StoreId { get; set; }
    
    /// <summary>The ID of the ShippingMethod being offered in this store.</summary>
    public Guid ShippingMethodId { get; set; }
    
    /// <summary>
    /// Indicates if this shipping method is available to customers in this store.
    /// Default: true. Set to false to temporarily disable without deleting.
    /// </summary>
    public bool Available { get; set; } = true;
    
    /// <summary>
    /// Store-specific shipping cost override in store's default currency.
    /// Null means use global ShippingMethod base cost.
    /// Used for regional pricing (e.g., €25 in EU, $25 in US).
    /// </summary>
    public decimal? StoreBaseCost { get; set; }
    #endregion

    #region Relationships
    public Store? Store { get; set; }
    public ShippingMethod? ShippingMethod { get; set; }
    #endregion

    #region Constructors
    private StoreShippingMethod() { }
    #endregion

    #region Factory
    /// <summary>
    /// Creates a new store-to-shipping-method mapping with availability and cost configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Pre-Conditions:</strong>
    /// <list type="bullet">
    /// <item><description>storeId must not be empty</description></item>
    /// <item><description>shippingMethodId must not be empty</description></item>
    /// <item><description>Store and ShippingMethod must exist</description></item>
    /// <item><description>Method should not already be linked to this store</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static ErrorOr<StoreShippingMethod> Create(
        Guid storeId, 
        Guid shippingMethodId, 
        bool available = true,
        decimal? storeBaseCost = null)
    {
        return new StoreShippingMethod
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            ShippingMethodId = shippingMethodId,
            Available = available,
            StoreBaseCost = storeBaseCost,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
    #endregion

    #region Business Logic
    /// <summary>
    /// Updates the availability and cost configuration for this shipping method in the store.
    /// </summary>
    /// <remarks>
    /// Only provided parameters are updated. Returns updated entity for method chaining.
    /// </remarks>
    public ErrorOr<StoreShippingMethod> Update(bool? available = null, decimal? storeBaseCost = null)
    {
        bool changed = false;
        if (available.HasValue && available != Available) { Available = available.Value; changed = true; }
        if (storeBaseCost.HasValue && storeBaseCost != StoreBaseCost) { StoreBaseCost = storeBaseCost; changed = true; }
        if (changed) UpdatedAt = DateTimeOffset.UtcNow;
        return this;
    }

    /// <summary>
    /// Marks this store-shipping-method mapping for deletion.
    /// </summary>
    /// <remarks>
    /// This should be called through Store.RemoveShippingMethod() rather than directly.
    /// Always use the aggregate root to maintain consistency.
    /// </remarks>
    public ErrorOr<Deleted> Delete() => Result.Deleted;
    #endregion


}