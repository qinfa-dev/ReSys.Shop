using ErrorOr;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Stocks;
using ReSys.Core.Domain.Location;
using ReSys.Core.Domain.Location.Countries;
using ReSys.Core.Domain.Location.States;
using ReSys.Core.Domain.Stores;
using ReSys.Core.Domain.Stores.StockLocations;

namespace ReSys.Core.Domain.Inventories.Locations;

/// <summary>
/// Represents a physical or logical location where inventory is held and managed.
/// Serves as a container for stock items and manages inventory operations at that location.
/// </summary>
/// <remarks>
/// <para>
/// <b>Responsibility:</b>
/// Manages a collection of StockItems, orchestrates stock movements (restocking and unstocking),
/// tracks address information, and links to stores for multi-location retail operations.
/// </para>
/// 
/// <para>
/// <b>Location Types:</b>
/// <list type="bullet">
/// <item><b>Warehouse:</b> Central distribution location for bulk inventory</item>
/// <item><b>Retail Store:</b> Physical retail location with customer-facing inventory</item>
/// <item><b>Staging Area:</b> Temporary holding location for transfers or returns</item>
/// <item><b>Fulfillment Center:</b> Distribution point for order fulfillment</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Key Operations:</b>
/// <list type="bullet">
/// <item><b>Restock:</b> Increase inventory at this location</item>
/// <item><b>Unstock:</b> Decrease inventory at this location</item>
/// <item><b>Link/Unlink Stores:</b> Associate with retail stores for multi-location management</item>
/// <item><b>Make Default:</b> Set as the default fulfillment location</item>
/// </list>
/// </para>
/// </remarks>
public sealed class StockLocation : Aggregate<Guid>, IAddress, IHasParameterizableName, IHasUniqueName, IHasMetadata,
    ISoftDeletable
{
    #region Constraints
    public static class Constraints
    {
        public const int NameMaxLength = 255;
        public const int PresentationMaxLength = 255;
        public const int AddressMaxLength = 255;
        public const int CityMaxLength = 100;
        public const int ZipcodeMaxLength = 20;
        public const int PhoneMaxLength = 50;
        public const int CompanyMaxLength = 255;
    }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error NotFound(Guid id) =>
            Error.NotFound(
                code: "StockLocation.NotFound",
                description: $"Stock location with ID '{id}' was not found.");

        public static Error HasStockItems =>
            Error.Conflict(
                code: "StockLocation.HasStockItems",
                description: "Cannot delete location with existing stock items. Remove all stock items first.");
    }
    #endregion

    #region Properties
    /// <summary>Gets the location name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets the display presentation name for the location.</summary>
    public string Presentation { get; set; } = string.Empty;

    /// <summary>Gets a value indicating whether this location is currently active.</summary>
    public bool Active { get; set; } = true;

    /// <summary>Gets a value indicating whether this is the default location for inventory operations.</summary>
    public bool Default { get; set; }

    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? Zipcode { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }

    public IDictionary<string, object?>? PublicMetadata { get; set; }
    public IDictionary<string, object?>? PrivateMetadata { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }
    #endregion

    #region Relationships
    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
    public ICollection<StoreStockLocation> StoreStockLocations { get; set; } = new List<StoreStockLocation>();

    public Guid? CountryId { get; set; }
    public Country? Country { get; set; }

    public Guid? StateId { get; set; }
    public State? State { get; set; }
    #endregion

    #region Computed Properties
    /// <summary>Gets the stores linked to this stock location.</summary>
    public ICollection<Store> Stores => StoreStockLocations.Select(selector: sls => sls.Store).ToList();
    #endregion

    #region Constructors
    private StockLocation() { }
    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new stock location with address and configuration information.
    /// </summary>
    /// <param name="name">The location name (required, e.g., "Main Warehouse", "NYC Store").</param>
    /// <param name="presentation">Optional display name (if different from name).</param>
    /// <param name="active">Whether this location is active (default: true).</param>
    /// <param name="isDefault">Whether this is the default location for operations (default: false).</param>
    /// <param name="countryId">Optional country ID for geographical context.</param>
    /// <param name="address1">Optional primary address line.</param>
    /// <param name="address2">Optional secondary address line.</param>
    /// <param name="city">Optional city name.</param>
    /// <param name="zipcode">Optional postal code.</param>
    /// <param name="stateId">Optional state/province ID.</param>
    /// <param name="phone">Optional phone number.</param>
    /// <param name="company">Optional company name.</param>
    /// <param name="publicMetadata">Optional metadata visible to all systems.</param>
    /// <param name="privateMetadata">Optional metadata for internal use only.</param>
    /// <returns>A new StockLocation instance.</returns>
    public static ErrorOr<StockLocation> Create(
        string name,
        string? presentation = null,
        bool active = true,
        bool isDefault = false,
        Guid? countryId = null,
        string? address1 = null,
        string? address2 = null,
        string? city = null,
        string? zipcode = null,
        Guid? stateId = null,
        string? phone = null,
        string? company = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        (name, presentation) = HasParameterizableName.NormalizeParams(name: name, presentation: presentation);

        var location = new StockLocation
        {
            Id = Guid.NewGuid(),
            Name = name,
            Presentation = presentation,
            Active = active,
            Default = isDefault,
            CreatedAt = DateTimeOffset.UtcNow,
            CountryId = countryId,
            Address1 = address1?.Trim(),
            Address2 = address2?.Trim(),
            City = city?.Trim(),
            Zipcode = zipcode?.Trim(),
            StateId = stateId,
            Phone = phone?.Trim(),
            Company = company?.Trim(),
            PublicMetadata = publicMetadata,
            PrivateMetadata = privateMetadata
        };

        location.AddDomainEvent(domainEvent: new Events.Created(StockLocationId: location.Id));
        return location;
    }

    #endregion

    #region Business Logic: Updates

    /// <summary>
    /// Updates location properties. Only non-null parameters are updated.
    /// </summary>
    /// <remarks>
    /// String values are trimmed. Null values are not applied to the entity
    /// (allowing for partial updates without clearing existing values).
    /// </remarks>
    public ErrorOr<StockLocation> Update(
        string? name = null,
        string? presentation = null,
        bool? active = null,
        string? address1 = null,
        string? address2 = null,
        string? city = null,
        string? zipcode = null,
        Guid? countryId = null,
        Guid? stateId = null,
        string? phone = null,
        string? company = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        bool changed = false;
        (name, presentation) = HasParameterizableName.NormalizeParams(
            name: name ?? Name,
            presentation: presentation);

        if (!string.IsNullOrWhiteSpace(value: name) && name != Name)
        {
            Name = name;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(value: presentation) && presentation != Presentation)
        {
            Presentation = presentation;
            changed = true;
        }

        if (active.HasValue && active != Active)
        {
            Active = active.Value;
            changed = true;
        }

        if (publicMetadata != null)
        {
            PublicMetadata = publicMetadata;
            changed = true;
        }

        if (privateMetadata != null)
        {
            PrivateMetadata = privateMetadata;
            changed = true;
        }

        if (address1 != null && Address1 != address1)
        {
            Address1 = address1.Trim();
            changed = true;
        }

        if (address2 != null && Address2 != address2)
        {
            Address2 = address2.Trim();
            changed = true;
        }

        if (city != null && City != city)
        {
            City = city.Trim();
            changed = true;
        }

        if (zipcode != null && Zipcode != zipcode)
        {
            Zipcode = zipcode.Trim();
            changed = true;
        }

        if (countryId.HasValue && countryId != CountryId)
        {
            CountryId = countryId.Value;
            changed = true;
        }

        if (stateId != null && stateId != StateId)
        {
            StateId = stateId.Value;
            changed = true;
        }

        if (phone != null && Phone != phone)
        {
            Phone = phone.Trim();
            changed = true;
        }

        if (company != null && Company != company)
        {
            Company = company.Trim();
            changed = true;
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(domainEvent: new Events.Updated(StockLocationId: Id));
        }

        return this;
    }

    #endregion

    #region Business Logic: Default Status

    /// <summary>
    /// Sets this location as the default inventory location.
    /// </summary>
    /// <remarks>
    /// Other locations should be updated separately to remove their default status if needed.
    /// </remarks>
    public ErrorOr<StockLocation> MakeDefault()
    {
        if (Default)
            return this;

        Default = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.StockLocationMadeDefault(StockLocationId: Id));
        return this;
    }

    #endregion

    #region Business Logic: Lifecycle

    /// <summary>
    /// Soft-deletes this location. Cannot be deleted if stock items exist.
    /// </summary>
    /// <returns>
    /// On success: Deleted result.
    /// On failure: Error if location still has stock items.
    /// </returns>
    public ErrorOr<Deleted> Delete()
    {
        if (StockItems.Any())
            return Errors.HasStockItems;

        DeletedAt = DateTimeOffset.UtcNow;
        IsDeleted = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.Deleted(StockLocationId: Id));

        return Result.Deleted;
    }

    /// <summary>
    /// Restores a previously deleted location.
    /// </summary>
    public ErrorOr<StockLocation> Restore()
    {
        if (!IsDeleted)
            return this;

        DeletedAt = null;
        DeletedBy = null;
        IsDeleted = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.Restored(StockLocationId: Id));
        return this;
    }

    #endregion

    #region Business Logic: Stock Management

    /// <summary>
    /// Gets an existing stock item for a variant or creates a new one if it doesn't exist.
    /// </summary>
    /// <param name="variant">The product variant to find or create stock for.</param>
    /// <returns>
    /// On success: The StockItem (existing or newly created).
    /// On failure: Error if creation fails.
    /// </returns>
    public ErrorOr<StockItem> StockItemOrCreate(Variant variant)
    {
        var stockItem = StockItems.FirstOrDefault(predicate: si => si.Variant.Id == variant.Id);

        if (stockItem != null)
            return stockItem;

        var result = StockItem.Create(
            variantId: variant.Id,
            stockLocationId: Id,
            sku: variant.Sku ?? string.Empty,
            quantityOnHand: 0,
            quantityReserved: 0,
            backorderable: variant.Backorderable);

        if (result.IsError)
            return result.FirstError;

        StockItems.Add(item: result.Value);
        return result.Value;
    }

    /// <summary>
    /// Decreases stock at this location (for outbound transfers).
    /// </summary>
    /// <param name="variant">The product variant to unstock.</param>
    /// <param name="quantity">The quantity to remove (must be positive).</param>
    /// <param name="originator">The originator of this movement (e.g., StockTransfer, Order).</param>
    /// <param name="stockTransferId">Optional reference to a stock transfer.</param>
    /// <returns>Success if unstock succeeds, or error if insufficient stock or variant not found.</returns>
    /// <remarks>
    /// For backorderable items, unstock always succeeds even if quantity exceeds physical stock.
    /// </remarks>
    public ErrorOr<Success> Unstock(
        Variant variant,
        int quantity,
        StockMovement.MovementOriginator originator,
        Guid? stockTransferId = null)
    {
        var stockItem = StockItems.FirstOrDefault(predicate: si => si.Variant.Id == variant.Id);

        if (stockItem == null || (!stockItem.Backorderable && stockItem.QuantityOnHand < quantity))
        {
            return Error.Validation(
                code: "StockLocation.InsufficientStock",
                description: $"Insufficient stock for variant {variant.Id}");
        }

        var result = stockItem.Adjust(
            quantity: -quantity,
            originator: originator,
            reason: "Unstock",
            stockTransferId: stockTransferId);

        return result.IsError ? result.FirstError : Result.Success;
    }

    /// <summary>
    /// Increases stock at this location (for inbound transfers or restock).
    /// </summary>
    /// <param name="variant">The product variant to restock.</param>
    /// <param name="quantity">The quantity to add (must be positive).</param>
    /// <param name="originator">The originator of this movement (e.g., Supplier, StockTransfer).</param>
    /// <param name="stockTransferId">Optional reference to a stock transfer.</param>
    /// <returns>Success if restock succeeds, or error if operation fails.</returns>
    /// <remarks>
    /// Creates a new StockItem if one doesn't exist for this variant.
    /// </remarks>
    public ErrorOr<Success> Restock(
        Variant variant,
        int quantity,
        StockMovement.MovementOriginator originator,
        Guid? stockTransferId = null)
    {
        var stockItemResult = StockItemOrCreate(variant: variant);

        if (stockItemResult.IsError)
            return stockItemResult.FirstError;

        var result = stockItemResult.Value.Adjust(
            quantity: quantity,
            originator: originator,
            reason: "Restock",
            stockTransferId: stockTransferId);

        return result.IsError ? result.FirstError : Result.Success;
    }

    #endregion

    #region Business Logic: Store Linkage

    /// <summary>
    /// Associates this location with a store for multi-location retail operations.
    /// </summary>
    /// <param name="store">The store to link.</param>
    /// <returns>Success if link succeeds, or conflict error if already linked.</returns>
    public ErrorOr<Success> LinkStore(Store store)
    {
        if (StoreStockLocations.Any(predicate: sls => sls.StoreId == store.Id))
        {
            return Error.Conflict(
                code: "StockLocation.StoreAlreadyLinked",
                description: $"Store '{store.Name}' is already linked to this location.");
        }

        var stockLocationStoreResult = StoreStockLocation.Create(
            stockLocationId: Id,
            storeId: store.Id);

        if (stockLocationStoreResult.IsError)
            return stockLocationStoreResult.Errors;

        StoreStockLocations.Add(item: stockLocationStoreResult.Value);
        AddDomainEvent(
            domainEvent: new Events.LinkedToStockLocation(
                StockLocationId: Id,
                StoreId: store.Id));

        return Result.Success;
    }

    /// <summary>
    /// Removes the association between this location and a store.
    /// </summary>
    /// <param name="store">The store to unlink.</param>
    /// <returns>Success if unlink succeeds, or not found error if not linked.</returns>
    public ErrorOr<Success> UnlinkStore(Store store)
    {
        var stockLocationStore = StoreStockLocations.FirstOrDefault(predicate: sls => sls.StoreId == store.Id);

        if (stockLocationStore == null)
        {
            return Error.NotFound(
                code: "StockLocation.StoreNotLinked",
                description: $"Store '{store.Name}' is not linked to this location.");
        }

        StoreStockLocations.Remove(item: stockLocationStore);
        AddDomainEvent(
            domainEvent: new Events.UnlinkedFromStockLocation(
                StockLocationId: Id,
                StoreId: store.Id));

        return Result.Success;
    }

    #endregion

    #region Domain Events

    public static class Events
    {
        /// <summary>
        /// Raised when a new stock location is created.
        /// </summary>
        public sealed record Created(Guid StockLocationId) : DomainEvent;

        /// <summary>
        /// Raised when a stock location's properties are updated.
        /// </summary>
        public sealed record Updated(Guid StockLocationId) : DomainEvent;

        /// <summary>
        /// Raised when a stock location is soft-deleted.
        /// </summary>
        public sealed record Deleted(Guid StockLocationId) : DomainEvent;

        /// <summary>
        /// Raised when a stock location is restored from deletion.
        /// </summary>
        public sealed record Restored(Guid StockLocationId) : DomainEvent;

        /// <summary>
        /// Raised when this location is set as the default location.
        /// </summary>
        public sealed record StockLocationMadeDefault(Guid StockLocationId) : DomainEvent;

        /// <summary>
        /// Raised when a store is linked to this location.
        /// </summary>
        public sealed record LinkedToStockLocation(Guid StockLocationId, Guid StoreId) : DomainEvent;

        /// <summary>
        /// Raised when a store is unlinked from this location.
        /// </summary>
        public sealed record UnlinkedFromStockLocation(Guid StockLocationId, Guid StoreId) : DomainEvent;
    }

    #endregion
}