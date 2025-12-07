using ErrorOr;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Stocks;
using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Location;
using ReSys.Core.Domain.Location.Countries;
using ReSys.Core.Domain.Location.States;
using ReSys.Core.Domain.Stores;
using ReSys.Core.Domain.Stores.StockLocations;

namespace ReSys.Core.Domain.Inventories.Locations;

/// <summary>
/// Represents a physical or logical location where inventory is held and managed (e.g., a warehouse, a retail store).
/// This aggregate root serves as a container for stock items and orchestrates inventory operations specific to that location.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Responsibility:</strong>
/// Manages a collection of <see cref="StockItem"/>s, orchestrates stock movements (restocking and unstocking),
/// tracks address information, and links to stores for multi-location retail operations. It ensures
/// the integrity of stock levels and provides a centralized point for inventory management at a given site.
/// </para>
///
/// <para>
/// <strong>Location Types:</strong>
/// Stock locations can represent various types of inventory storage:
/// <list type="bullet">
/// <item><b>Warehouse:</b> Central distribution location for bulk inventory.</item>
/// <item><b>Retail Store:</b> Physical retail location with customer-facing inventory.</item>
/// <item><b>Staging Area:</b> Temporary holding location for transfers or returns.</item>
/// <item><b>Fulfillment Center:</b> Distribution point for order fulfillment.</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Operations:</strong>
/// <list type="bullet">
/// <item><b>Restock:</b> Increase inventory at this location for a specific <see cref="Variant"/>.</item>
/// <item><b>Unstock:</b> Decrease inventory at this location for a specific <see cref="Variant"/>.</item>
/// <item><b>Link/Unlink Stores:</b> Associate with retail stores for multi-location management via <see cref="StoreStockLocation"/>.</item>
/// <item><b>Make Default:</b> Set as the default fulfillment location for a store or the system.</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Concerns Implemented:</strong>
/// <list type="bullet">
/// <item><strong>IAddress</strong> - Incorporates standard address properties.</item>
/// <item><strong>IHasParameterizableName</strong> - Provides for both internal `Name` and display `Presentation`.</item>
/// <item><strong>IHasUniqueName</strong> - Ensures the `Name` is unique across stock locations.</item>
/// <item><strong>IHasMetadata</strong> - For flexible storage of additional public and private data.</item>
/// <item><strong>ISoftDeletable</strong> - Supports soft deletion, allowing for recovery and historical tracking.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class StockLocation : Aggregate<Guid>, IAddress, IHasParameterizableName, IHasUniqueName, IHasMetadata,
    ISoftDeletable
{
    #region Constraints
    /// <summary>
    /// Defines constraints and constant values specific to <see cref="StockLocation"/> properties.
    /// These constraints are applied during validation to ensure data integrity.
    /// </summary>
    public static class Constraints
    {
        /// <summary>Maximum allowed length for the <see cref="StockLocation.Name"/> property.</summary>
        public const int NameMaxLength = 255;
        /// <summary>Maximum allowed length for the <see cref="StockLocation.Presentation"/> property.</summary>
        public const int PresentationMaxLength = 255;
        /// <summary>Maximum allowed length for address lines (e.g., <see cref="StockLocation.Address1"/>).</summary>
        public const int AddressMaxLength = 255;
        /// <summary>Maximum allowed length for the <see cref="StockLocation.City"/> property.</summary>
        public const int CityMaxLength = 100;
        /// <summary>Maximum allowed length for the <see cref="StockLocation.Zipcode"/> property.</summary>
        public const int ZipcodeMaxLength = 20;
        /// <summary>Maximum allowed length for the <see cref="StockLocation.Phone"/> property.</summary>
        public const int PhoneMaxLength = 50;
        /// <summary>Maximum allowed length for the <see cref="StockLocation.Company"/> property.</summary>
        public const int CompanyMaxLength = 255;
    }
    #endregion

    #region Errors
    /// <summary>
    /// Defines domain error scenarios specific to <see cref="StockLocation"/> operations.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error indicating that a requested stock location could not be found.
        /// </summary>
        /// <param name="id">The unique identifier of the stock location that was not found.</param>
        public static Error NotFound(Guid id) =>
            Error.NotFound(
                code: "StockLocation.NotFound",
                description: $"Stock location with ID '{id}' was not found.");

        /// <summary>
        /// Error indicating that a stock location cannot be deleted because it still contains stock items.
        /// All stock items must be removed or transferred before deletion.
        /// </summary>
        public static Error HasStockItems =>
            Error.Conflict(
                code: "StockLocation.HasStockItems",
                description: "Cannot delete location with existing stock items. Remove all stock items first.");

        /// <summary>
        /// Error indicating that a stock location cannot be deleted because it has stock items with reserved quantities.
        /// All reserved stock must be fulfilled or unreserved before deletion.
        /// </summary>
        public static Error HasReservedStock =>
            Error.Conflict(
                code: "StockLocation.HasReservedStock",
                description: "Cannot delete location with reserved stock.");

        /// <summary>
        /// Error indicating an inconsistent state within a stock item (e.g., reserved quantity exceeds on-hand quantity).
        /// This suggests a data integrity issue.
        /// </summary>
        public static Error InvalidStockItemState =>
            Error.Validation(
                code: "StockLocation.InvalidStockItemState",
                description: "Stock item has an inconsistent state (e.g., reserved > on-hand).");

        /// <summary>
        /// Error indicating that a stock item has a negative quantity on hand.
        /// This is an invalid state, typically caught during inventory adjustments.
        /// </summary>
        public static Error NegativeQuantityOnHand =>
            Error.Validation(
                code: "StockLocation.NegativeQuantityOnHand",
                description: "Stock item has a negative quantity on hand.");

        /// <summary>
        /// Error indicating that a stock item has a negative quantity reserved.
        /// This is an invalid state, typically caught during reservation adjustments.
        /// </summary>
        public static Error NegativeQuantityReserved =>
            Error.Validation(
                code: "StockLocation.NegativeQuantityReserved",
                description: "Stock item has a negative quantity reserved.");

        /// <summary>
        /// Error indicating invalid store linkages (e.g., a <see cref="StoreStockLocation"/> referencing an invalid <see cref="Store"/> or <see cref="StockLocation"/>).
        /// </summary>
        public static Error InvalidStoreLinkage =>
            Error.Validation(
                code: "StockLocation.InvalidStoreLinkage",
                description: "Invalid store linkages detected.");

        /// <summary>
        /// Error indicating that a stock location cannot be deleted because it has pending shipments.
        /// All pending shipments must be resolved or cancelled first.
        /// </summary>
        public static Error HasPendingShipments =>
            Error.Conflict(
                code: "StockLocation.HasPendingShipments",
                description: "Cannot delete location with pending shipments. Resolve or cancel shipments first.");

        /// <summary>
        /// Error indicating that a stock location cannot be deleted because it has active stock transfers
        /// (either as a source or destination). All transfers must be completed or cancelled first.
        /// </summary>
        public static Error HasActiveStockTransfers =>
            Error.Conflict(
                code: "StockLocation.HasActiveStockTransfers",
                description: "Cannot delete location with active stock transfers (as source or destination). Complete or cancel transfers first.");

        /// <summary>
        /// Error indicating that a stock location cannot be deleted because it has backordered inventory units assigned.
        /// All backorders must be filled or reassigned to another location first.
        /// </summary>
        public static Error HasBackorderedInventoryUnits =>
            Error.Conflict(
                code: "StockLocation.HasBackorderedInventoryUnits",
                description: "Cannot delete location with backordered inventory units assigned. Fill or reassign backorders first.");
    }
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the internal system name for the stock location (e.g., "main-warehouse", "nyc-store").
    /// This name is unique, URL-safe, and used for identification.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable display name for the stock location (e.g., "Main Warehouse", "NYC Retail Store").
    /// This can differ from <see cref="Name"/> for better user experience.
    /// </summary>
    public string Presentation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this stock location is currently active and operational.
    /// Inactive locations may not be used for new inventory operations.
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this is the default stock location for inventory operations
    /// (e.g., for fulfilling orders if no other location is specified).
    /// </summary>
    public bool Default { get; set; }

    /// <summary>
    /// Gets or sets the first line of the street address for this stock location.
    /// </summary>
    public string? Address1 { get; set; }
    /// <summary>
    /// Gets or sets the second line of the street address for this stock location (optional).
    /// </summary>
    public string? Address2 { get; set; }
    /// <summary>
    /// Gets or sets the city or town name for this stock location.
    /// </summary>
    public string? City { get; set; }
    /// <summary>
    /// Gets or sets the postal code or ZIP code for this stock location.
    /// </summary>
    public string? Zipcode { get; set; }
    /// <summary>
    /// Gets or sets the phone number for this stock location.
    /// </summary>
    public string? Phone { get; set; }
    /// <summary>
    /// Gets or sets the company name associated with this stock location (optional).
    /// </summary>
    public string? Company { get; set; }

    /// <summary>
    /// Gets or sets public metadata: custom attributes visible to administrators and potentially exposed via public APIs.
    /// Use for: display hints, operational tags, geographical regions.
    /// </summary>
    public IDictionary<string, object?>? PublicMetadata { get; set; }
    /// <summary>
    /// Gets or sets private metadata: custom attributes visible only to administrators and backend systems.
    /// Use for: internal notes, integration markers, storage capacity details.
    /// </summary>
    public IDictionary<string, object?>? PrivateMetadata { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when this stock location was soft-deleted.
    /// Null if the location is not deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the user or system that soft-deleted this stock location.
    /// Null if the location is not deleted.
    /// </summary>
    public string? DeletedBy { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether this stock location has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }
    #endregion

    #region Relationships
    /// <summary>
    /// Gets or sets the collection of <see cref="StockItem"/>s managed at this location.
    /// This represents the current inventory for various product variants.
    /// </summary>
    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
    /// <summary>
    /// Gets or sets the collection of <see cref="StoreStockLocation"/> entities that link
    /// this stock location to various retail <see cref="Store"/>s.
    /// This enables multi-location inventory management for stores.
    /// </summary>
    public ICollection<StoreStockLocation> StoreStockLocations { get; set; } = new List<StoreStockLocation>();

    /// <summary>
    /// Gets or sets the unique identifier of the <see cref="Country"/> associated with this stock location.
    /// </summary>
    public Guid? CountryId { get; set; }
    /// <summary>
    /// Gets or sets the navigation property to the <see cref="Country"/> associated with this stock location.
    /// </summary>
    public Country? Country { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the <see cref="State"/> associated with this stock location (optional).
    /// </summary>
    public Guid? StateId { get; set; }
    /// <summary>
    /// Gets or sets the navigation property to the <see cref="State"/> associated with this stock location (optional).
    /// </summary>
    public State? State { get; set; }
    #endregion

    #region Computed Properties
    /// <summary>
    /// Gets a collection of <see cref="Store"/>s that are currently linked to this stock location.
    /// This is a computed property derived from the <see cref="StoreStockLocations"/> collection.
    /// </summary>
    public ICollection<Store> Stores => StoreStockLocations.Select(selector: sls => sls.Store).ToList();
    #endregion

    #region Constructors
    /// <summary>
    /// Private constructor for ORM (Entity Framework Core) materialization.
    /// </summary>
    private StockLocation() { }
    #endregion

    #region Factory Methods

    /// <summary>
    /// Factory method to create a new <see cref="StockLocation"/> instance.
    /// This method initializes the stock location with address and configuration information.
    /// </summary>
    /// <param name="name">The internal system name for the location (e.g., "main-warehouse", "nyc-store"). This is required.</param>
    /// <param name="presentation">Optional: The human-readable display name for the location. Defaults to <paramref name="name"/> if not provided.</param>
    /// <param name="active">Whether this location is active and operational. Defaults to true.</param>
    /// <param name="isDefault">Whether this is designated as the default location for inventory operations. Defaults to false.</param>
    /// <param name="countryId">Optional: The unique identifier of the <see cref="Country"/> for this location.</param>
    /// <param name="address1">Optional: The primary address line for this location.</param>
    /// <param name="address2">Optional: The secondary address line for this location.</param>
    /// <param name="city">Optional: The city name for this location.</param>
    /// <param name="zipcode">Optional: The postal code for this location.</param>
    /// <param name="stateId">Optional: The unique identifier of the <see cref="State"/> for this location.</param>
    /// <param name="phone">Optional: The phone number for this location.</param>
    /// <param name="company">Optional: The company name associated with this location.</param>
    /// <param name="publicMetadata">Optional: Dictionary for public-facing metadata.</param>
    /// <param name="privateMetadata">Optional: Dictionary for internal-only metadata.</param>
    /// <returns>
    /// An <see cref="ErrorOr{StockLocation}"/> result.
    /// Returns a new <see cref="StockLocation"/> instance on success.
    /// Returns errors if name normalization fails or required fields are missing (though basic string validation is implicit here).
    /// </returns>
    /// <remarks>
    /// This method adds a <see cref="Events.Created"/> domain event upon successful creation.
    /// Further validation (e.g., name uniqueness, address format) is typically handled by FluentValidation
    /// or application services.
    /// </remarks>
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
    /// Updates various mutable properties of the <see cref="StockLocation"/>.
    /// This method allows for partial updates; only non-null parameters are updated.
    /// </summary>
    /// <param name="name">The new internal system name for the location.</param>
    /// <param name="presentation">The new human-readable display name for the location.</param>
    /// <param name="active">The new active status for the location.</param>
    /// <param name="address1">The new primary address line.</param>
    /// <param name="address2">The new secondary address line.</param>
    /// <param name="city">The new city name.</param>
    /// <param name="zipcode">The new postal code.</param>
    /// <param name="countryId">The new <see cref="Country"/> ID for this location.</param>
    /// <param name="stateId">The new <see cref="State"/> ID for this location.</param>
    /// <param name="phone">The new phone number.</param>
    /// <param name="company">The new company name.</param>
    /// <param name="publicMetadata">New public metadata. If null, existing is retained.</param>
    /// <param name="privateMetadata">New private metadata. If null, existing is retained.</param>
    /// <returns>
    /// An <see cref="ErrorOr{StockLocation}"/> result.
    /// Returns the updated <see cref="StockLocation"/> instance on success.
    /// </returns>
    /// <remarks>
    /// String values are trimmed before assignment. Null values for string properties
    /// mean the existing value is retained (allowing for partial updates without clearing data).
    /// The <c>UpdatedAt</c> timestamp is updated if any changes occur, and an <see cref="Events.Updated"/> domain event is added.
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
    /// Sets this stock location as the default inventory location.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{StockLocation}"/> result.
    /// Returns the updated <see cref="StockLocation"/> instance on success.
    /// </returns>
    /// <remarks>
    /// If this location is already default, no action is taken (idempotent).
    /// Other locations should be updated separately by an application service to remove their default status if needed,
    /// ensuring only one default location exists per store/system.
    /// An <see cref="Events.StockLocationMadeDefault"/> domain event is added.
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
    /// Soft-deletes this stock location.
    /// This operation is subject to several constraints to maintain data integrity and prevent issues with existing inventory operations.
    /// </summary>
    /// <param name="hasPendingShipments">Flag indicating if there are any pending shipments originating from this location.</param>
    /// <param name="hasActiveStockTransfers">Flag indicating if there are any active stock transfers (as source or destination) involving this location.</param>
    /// <param name="hasBackorderedInventoryUnits">Flag indicating if there are backordered inventory units assigned to this location.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Returns <see cref="Result.Deleted"/> on successful soft-deletion.
    /// Returns <see cref="Errors.HasReservedStock"/> if the location still has stock items with reserved quantities.
    /// Returns <see cref="Errors.HasStockItems"/> if the location still contains any stock items.
    /// Returns <see cref="Errors.HasPendingShipments"/> if there are pending shipments.
    /// Returns <see cref="Errors.HasActiveStockTransfers"/> if there are active stock transfers.
    /// Returns <see cref="Errors.HasBackorderedInventoryUnits"/> if there are backordered inventory units assigned.
    /// </returns>
    /// <remarks>
    /// To delete a location, all inventory must be moved out, all reservations fulfilled,
    /// and all related operations (shipments, transfers) must be completed or cancelled.
    /// The actual checks for `hasPendingShipments`, `hasActiveStockTransfers`, and `hasBackorderedInventoryUnits`
    /// should be performed by an application service before calling this method, as they often involve
    /// querying other aggregates or repositories.
    /// A <see cref="Events.Deleted"/> domain event is added.
    /// </remarks>
    public ErrorOr<Deleted> Delete(bool hasPendingShipments, bool hasActiveStockTransfers, bool hasBackorderedInventoryUnits)
    {
        if (StockItems.Any(si => si.QuantityReserved > 0))
            return Errors.HasReservedStock;

        if (StockItems.Any())
            return Errors.HasStockItems;

        if (hasPendingShipments)
        {
            return Errors.HasPendingShipments;
        }

        if (hasActiveStockTransfers)
        {
            return Errors.HasActiveStockTransfers;
        }

        if (hasBackorderedInventoryUnits)
        {
            return Errors.HasBackorderedInventoryUnits;
        }        // NEW: Check for pending transfers (as source or destination)
        // This would require access to StockTransfer repository
        // For now, document that this should be checked at application layer

        DeletedAt = DateTimeOffset.UtcNow;
        IsDeleted = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.Deleted(StockLocationId: Id));

        return Result.Deleted;
    }

    /// <summary>
    /// Restores a previously soft-deleted stock location, making it active again.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{StockLocation}"/> result.
    /// Returns the restored <see cref="StockLocation"/> instance on success.
    /// </returns>
    /// <remarks>
    /// If the location is not currently deleted, no action is taken (idempotent).
    /// Resets <c>DeletedAt</c> and <c>DeletedBy</c> fields.
    /// An <see cref="Events.Restored"/> domain event is added.
    /// </remarks>
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
    /// Retrieves an existing <see cref="StockItem"/> for a given <see cref="Variant"/> at this location,
    /// or creates a new <see cref="StockItem"/> if one does not already exist.
    /// </summary>
    /// <param name="variant">The product <see cref="Variant"/> for which to find or create stock.</param>
    /// <returns>
    /// An <see cref="ErrorOr{StockItem}"/> result.
    /// Returns the existing or newly created <see cref="StockItem"/> on success.
    /// Returns an error if <see cref="StockItem"/> creation fails.
    /// </returns>
    /// <remarks>
    /// If a new <see cref="StockItem"/> is created, it is added to the <see cref="StockItems"/> collection
    /// with initial quantities of zero.
    /// </remarks>
    public ErrorOr<StockItem> StockItemOrCreate(Variant variant)
    {
        var stockItem = StockItems.FirstOrDefault(predicate: si => si.VariantId == variant.Id);

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
    /// Decreases the quantity of a specific product <see cref="Variant"/> in stock at this location.
    /// This operation is typically used for outbound transfers or fulfilling orders.
    /// </summary>
    /// <param name="variant">The product <see cref="Variant"/> to unstock.</param>
    /// <param name="quantity">The amount to remove from stock. Must be a positive value.</param>
    /// <param name="originator">The originator of this stock movement (e.g., <see cref="StockMovement.MovementOriginator.StockTransfer"/>, <see cref="StockMovement.MovementOriginator.Order"/>).</param>
    /// <param name="originatorId">Optional: A unique identifier for the originating operation (e.g., StockTransfer ID, Order ID).</param>
    /// <returns>
    /// An <see cref="ErrorOr{Success}"/> result.
    /// Returns <see cref="Result.Success"/> if the unstock operation succeeds.
    /// Returns <see cref="Error.Validation"/> if the <paramref name="variant"/> is null or <paramref name="quantity"/> is not positive.
    /// Returns <see cref="Error.NotFound"/> if no stock is found for the variant.
    /// Returns <see cref="Error.Validation"/> if unstocking would violate reserved quantities for non-backorderable items.
    /// </returns>
    /// <remarks>
    /// For backorderable items, unstocking always succeeds, potentially leading to negative on-hand quantities.
    /// This method delegates the actual quantity adjustment to the <see cref="StockItem.Adjust(int, StockMovement.MovementOriginator, string?, Guid?)"/> method.
    /// </remarks>
    public ErrorOr<Success> Unstock(
        Variant? variant,
        int quantity,
        StockMovement.MovementOriginator originator,
        Guid? originatorId = null)
    {
        if (variant == null)
        {
            return Error.Validation(
                code: "StockLocation.VariantRequired",
                description: "Variant is required.");
        }

        if (quantity <= 0)
        {
            return Error.Validation(
                code: "StockLocation.InvalidQuantity",
                description: "Quantity must be positive.");
        }

        var stockItem = StockItems.FirstOrDefault(predicate: si => si.VariantId == variant.Id);

        if (stockItem == null)
        {
            return Error.NotFound(
                code: "StockLocation.StockItemNotFound",
                description: $"No stock found for variant {variant.Id} at location {Id}.");
        }

        // Check availability
        if (!stockItem.Backorderable)
        {
            var newOnHand = stockItem.QuantityOnHand - quantity;
            if (stockItem.QuantityReserved > newOnHand)
            {
                return Error.Validation(
                    code: "StockLocation.UnstockWouldViolateReservations",
                    description: $"Cannot unstock {quantity} units for variant {variant.Id}. " +
                                 $"Would leave {newOnHand} on hand but {stockItem.QuantityReserved} are reserved. " +
                                 $"Maximum unstock: {stockItem.QuantityOnHand - stockItem.QuantityReserved} units.");
            }
        }

        var result = stockItem.Adjust(
            quantity: -quantity,
            originator: originator,
            reason: "Unstock",
            originatorId: originatorId);

        return result.IsError ? result.FirstError : Result.Success;
    }

    /// <summary>
    /// Increases the quantity of a specific product <see cref="Variant"/> in stock at this location.
    /// This operation is typically used for inbound transfers or receiving new stock.
    /// </summary>
    /// <param name="variant">The product <see cref="Variant"/> to restock.</param>
    /// <param name="quantity">The amount to add to stock. Must be a positive value.</param>
    /// <param name="originator">The originator of this stock movement (e.g., <see cref="StockMovement.MovementOriginator.Supplier"/>, <see cref="StockMovement.MovementOriginator.StockTransfer"/>).</param>
    /// <param name="originatorId">Optional: A unique identifier for the originating operation (e.g., Supplier PO, StockTransfer ID).</param>
    /// <returns>
    /// An <see cref="ErrorOr{Success}"/> result.
    /// Returns <see cref="Result.Success"/> if the restock operation succeeds.
    /// Returns an error if the underlying <see cref="StockItem"/> creation or adjustment fails.
    /// </returns>
    /// <remarks>
    /// If a <see cref="StockItem"/> for the given <paramref name="variant"/> does not exist at this location,
    /// it will be created automatically.
    /// This method delegates the actual quantity adjustment to the <see cref="StockItem.Adjust(int, StockMovement.MovementOriginator, string?, Guid?)"/> method.
    /// </remarks>
    public ErrorOr<Success> Restock(
        Variant variant,
        int quantity,
        StockMovement.MovementOriginator originator,
        Guid? originatorId = null)
    {
        var stockItemResult = StockItemOrCreate(variant: variant);

        if (stockItemResult.IsError)
            return stockItemResult.FirstError;

        var result = stockItemResult.Value.Adjust(
            quantity: quantity,
            originator: originator,
            reason: "Restock",
            originatorId: originatorId); // Pass originatorId

        return result.IsError ? result.FirstError : Result.Success;
    }

    #endregion

    #region Business Logic: Invariants

    /// <summary>
    /// Validates the internal consistency and business invariants of the <see cref="StockLocation"/> and its <see cref="StockItem"/>s.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Success}"/> result.
    /// Returns <see cref="Result.Success"/> if all invariants are met.
    /// Returns specific <see cref="Errors"/> if any inconsistencies are found (e.g., negative quantities, invalid reservations).
    /// </returns>
    /// <remarks>
    /// This method is crucial for maintaining data integrity, especially after complex inventory operations.
    /// It checks for conditions like reserved quantity not exceeding on-hand quantity for non-backorderable items,
    /// and ensures no quantities are negative. It also verifies the validity of linked stores.
    /// </remarks>
    public ErrorOr<Success> ValidateInvariants()
    {
        // Check all stock items have consistent quantities
        foreach (var stockItem in StockItems)
        {
            if (stockItem.QuantityReserved > stockItem.QuantityOnHand && !stockItem.Backorderable)
            {
                return Error.Validation(
                    code: "StockLocation.InvalidStockItemState",
                    description: $"Stock item {stockItem.Id} has reserved ({stockItem.QuantityReserved}) " +
                                $"exceeding on-hand ({stockItem.QuantityOnHand}).");
            }

            if (stockItem.QuantityOnHand < 0)
            {
                return Error.Validation(
                    code: "StockLocation.NegativeQuantityOnHand",
                    description: $"Stock item {stockItem.Id} has negative quantity on hand.");
            }

            if (stockItem.QuantityReserved < 0)
            {
                return Error.Validation(
                    code: "StockLocation.NegativeQuantityReserved",
                    description: $"Stock item {stockItem.Id} has negative quantity reserved.");
            }
        }

        // Check linked stores are valid
        if (StoreStockLocations.Any(ssl => ssl.StoreId == Guid.Empty || ssl.StockLocationId != Id))
        {
            return Error.Validation(
                code: "StockLocation.InvalidStoreLinkage",
                description: "Invalid store linkages detected.");
        }

        return Result.Success;
    }

    #endregion

    #region Business Logic: Store Linkage

    /// <summary>
    /// Associates this stock location with a specific <see cref="Store"/> for multi-location retail operations.
    /// </summary>
    /// <param name="store">The <see cref="Store"/> aggregate to link with this location.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Success}"/> result.
    /// Returns <see cref="Result.Success"/> if the link operation succeeds.
    /// Returns <see cref="Error.Conflict"/> if the store is already linked to this location.
    /// Returns errors if the underlying <see cref="StoreStockLocation"/> creation fails.
    /// </returns>
    /// <remarks>
    /// This method creates a <see cref="StoreStockLocation"/> entry to represent the association.
    /// An <see cref="Events.LinkedToStockLocation"/> domain event is added.
    /// </remarks>
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
    /// Removes the association between this stock location and a specific <see cref="Store"/>.
    /// </summary>
    /// <param name="store">The <see cref="Store"/> aggregate to unlink from this location.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Success}"/> result.
    /// Returns <see cref="Result.Success"/> if the unlink operation succeeds.
    /// Returns <see cref="Error.NotFound"/> if the store is not currently linked to this location.
    /// </returns>
    /// <remarks>
    /// This method removes the corresponding <see cref="StoreStockLocation"/> entry.
    /// An <see cref="Events.UnlinkedFromStockLocation"/> domain event is added.
    /// </remarks>
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

    /// <summary>
    /// Defines domain events related to the lifecycle and state changes of a <see cref="StockLocation"/>.
    /// These events enable a decoupled architecture, allowing other services or bounded contexts to react
    /// to stock location-related changes.
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Raised when a new stock location is created.
        /// Purpose: Notifies the system that a new inventory storage point is available.
        /// </summary>
        /// <param name="StockLocationId">The unique identifier of the newly created stock location.</param>
        public sealed record Created(Guid StockLocationId) : DomainEvent;

        /// <summary>
        /// Raised when a stock location's properties are updated.
        /// Purpose: Signals that location details have changed, prompting dependent services to update records or caches.
        /// </summary>
        /// <param name="StockLocationId">The unique identifier of the updated stock location.</param>
        public sealed record Updated(Guid StockLocationId) : DomainEvent;

        /// <summary>
        /// Raised when a stock location is soft-deleted.
        /// Purpose: Indicates the location is no longer active for operations but remains in history.
        /// </summary>
        /// <param name="StockLocationId">The unique identifier of the soft-deleted stock location.</param>
        public sealed record Deleted(Guid StockLocationId) : DomainEvent;

        /// <summary>
        /// Raised when a stock location is restored from deletion.
        /// Purpose: Signals the location is active again for inventory operations.
        /// </summary>
        /// <param name="StockLocationId">The unique identifier of the restored stock location.</param>
        public sealed record Restored(Guid StockLocationId) : DomainEvent;

        /// <summary>
        /// Raised when this location is designated as the default inventory location.
        /// Purpose: Notifies the system of a change in default fulfillment preference.
        /// </summary>
        /// <param name="StockLocationId">The unique identifier of the stock location made default.</param>
        public sealed record StockLocationMadeDefault(Guid StockLocationId) : DomainEvent;

        /// <summary>
        /// Raised when a store is linked to this location.
        /// Purpose: Signals a new association for multi-location retail operations.
        /// </summary>
        /// <param name="StockLocationId">The unique identifier of the stock location.</param>
        /// <param name="StoreId">The unique identifier of the store that was linked.</param>
        public sealed record LinkedToStockLocation(Guid StockLocationId, Guid StoreId) : DomainEvent;

        /// <summary>
        /// Raised when a store is unlinked from this location.
        /// Purpose: Signals the removal of an association with a store.
        /// </summary>
        /// <param name="StockLocationId">The unique identifier of the stock location.</param>
        /// <param name="StoreId">The unique identifier of the store that was unlinked.</param>
        public sealed record UnlinkedFromStockLocation(Guid StockLocationId, Guid StoreId) : DomainEvent;
    }

    #endregion
}