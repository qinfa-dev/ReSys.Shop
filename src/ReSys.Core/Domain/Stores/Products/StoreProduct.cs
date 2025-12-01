using ErrorOr;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Catalog.Products;

namespace ReSys.Core.Domain.Stores.Products;

/// <summary>
/// Represents the relationship between a Store and a Product.
/// Controls product visibility and featured status on a per-store basis.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong>
/// This owned entity maps products to specific stores with visibility and featured controls.
/// Enables product-level visibility management for multi-store scenarios without duplicating product master data.
/// </para>
/// <para>
/// <strong>Key Characteristics:</strong>
/// <list type="bullet">
/// <item><description>Composite FK: Unique per Store + Product combination</description></item>
/// <item><description>Visibility Control: Products can be shown/hidden per store</description></item>
/// <item><description>Featured Status: Highlight products in specific stores</description></item>
/// <item><description>Sort Order: Position field for custom product ordering per store</description></item>
/// <item><description>Auditable: CreatedAt/UpdatedAt for audit trail</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Important Invariants:</strong>
/// <list type="bullet">
/// <item><description>Featured items MUST be visible (don't hide featured products)</description></item>
/// <item><description>Position must be non-negative (enforced in Update method)</description></item>
/// <item><description>A product can only be linked once per store (unique constraint)</description></item>
/// <item><description>Deletion is handled through Store aggregate (not direct deletion)</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class StoreProduct : AuditableEntity, IHasPosition
{
    public static class Errors
    {
        public static Error StoreRequired => Error.Validation(code: "StoreProduct.StoreRequired", description: "Storefront is required.");
        public static Error ProductRequired => Error.Validation(code: "StoreProduct.ProductRequired", description: "Product is required.");
        public static Error NotFound(Guid id) => Error.NotFound(code: "StoreProduct.NotFound", description: $"StoreProduct with ID '{id}' was not found.");
    }

    public Guid StoreId { get; set; }
    public Guid ProductId { get; set; }
    public bool Visible { get; set; } = true;
    public bool Featured { get; set; }
    public int Position { get; set; }
    public Store Store { get; set; } = null!;
    public Product Product { get; set; } = null!;
    private StoreProduct() { }

    /// <summary>
    /// Creates a new store-product mapping with visibility configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Pre-Conditions:</strong>
    /// <list type="bullet">
    /// <item><description>storeId must not be empty (Guid.Empty)</description></item>
    /// <item><description>productId must not be empty (Guid.Empty)</description></item>
    /// <item><description>Product must exist in catalog before linking</description></item>
    /// <item><description>Store must exist before linking products</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Post-Conditions:</strong>
    /// <list type="bullet">
    /// <item><description>New StoreProduct is created with unique ID</description></item>
    /// <item><description>Timestamps are set to UTC now</description></item>
    /// <item><description>Position is normalized (negative values become 0)</description></item>
    /// <item><description>Ready to be added to Store.StoreProducts collection</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static ErrorOr<StoreProduct> Create(
        Guid storeId,
        Guid productId,
        bool visible = true,
        bool featured = false,
        int position = 0)
    {
        if (storeId == Guid.Empty) return Errors.StoreRequired;
        if (productId == Guid.Empty) return Errors.ProductRequired;

        return new StoreProduct
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            ProductId = productId,
            Visible = visible,
            Featured = featured,
            Position = Math.Max(val1: 0, val2: position),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Updates product visibility, featured status, and sort position in this store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Behavior:</strong>
    /// <list type="bullet">
    /// <item><description>Only provided parameters are updated (null parameters are ignored)</description></item>
    /// <item><description>UpdatedAt is only changed if actual changes are made</description></item>
    /// <item><description>Negative positions are normalized to 0</description></item>
    /// <item><description>No validation error if featured=true and visible=false (use business logic in command handler)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public ErrorOr<StoreProduct> Update(bool? visible = null, bool? featured = null, int? position = null)
    {
        bool changed = false;
        if (visible.HasValue && visible != Visible) { Visible = visible.Value; changed = true; }
        if (featured.HasValue && featured != Featured) { Featured = featured.Value; changed = true; }
        if (position.HasValue && position != Position) { Position = Math.Max(val1: 0, val2: position.Value); changed = true; }
        if (changed) UpdatedAt = DateTimeOffset.UtcNow;
        return this;
    }

    /// <summary>
    /// Marks this store-product mapping for deletion.
    /// </summary>
    /// <remarks>
    /// This should be called through Store.RemoveProduct() rather than directly.
    /// Always call through the aggregate root to maintain consistency.
    /// </remarks>
    public ErrorOr<Deleted> Delete() => Result.Deleted;
}