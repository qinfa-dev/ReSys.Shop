using ErrorOr;

using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Stocks;

namespace ReSys.Core.Domain.Inventories.Locations;

/// <summary>
/// Orchestrates the movement of stock between two locations or receipt of stock from external vendors.
/// Manages the complex process of validating inventory and creating corresponding movement records.
/// </summary>
/// <remarks>
/// <para>
/// <b>Responsibility:</b>
/// Coordinates stock transfers between locations and receipts from suppliers, ensuring that
/// all affected StockItems are properly updated and movement history is maintained.
/// </para>
/// 
/// <para>
/// <b>Supported Operations:</b>
/// <list type="bullet">
/// <item><b>Transfer:</b> Move stock from one location to another (requires source and destination)</item>
/// <item><b>Receive:</b> Receive new stock from an external supplier (only requires destination)</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Process Flow:</b>
/// <list type="number">
/// <item>Create transfer or receive request with variants and quantities</item>
/// <item>Validate quantities are positive and stock is available (for transfers)</item>
/// <item>Execute unstock from source (for transfers) or restock to destination (for both)</item>
/// <item>Record movement history with transfer reference</item>
/// <item>Publish domain events for audit trail</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Error Handling:</b>
/// Accumulates all errors and returns them together, allowing callers to see all issues
/// before attempting a partial retry.
/// </para>
/// </remarks>
public sealed class StockTransfer : Aggregate
{
    #region Constraints
    public static class Constraints
    {
        public const int NumberMaxLength = 50;
        public const int ReferenceMaxLength = 255;
    }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error NoVariants =>
            Error.Validation(
                code: "StockTransfer.NoVariants",
                description: "At least one variant with positive quantity must be specified.");

        public static Error SourceEqualsDestination =>
            Error.Validation(
                code: "StockTransfer.SourceEqualsDestination",
                description: "Source and destination locations cannot be the same.");

        public static Error InsufficientStock(Guid variantId, int available, int requested) =>
            Error.Validation(
                code: "StockTransfer.InsufficientStock",
                description: $"Variant {variantId}: Only {available} units available, but {requested} units requested.");

        public static Error VariantNotFound(Guid variantId) =>
            Error.NotFound(
                code: "StockTransfer.VariantNotFound",
                description: $"Variant with ID '{variantId}' was not found.");

        public static Error StockLocationNotFound(Guid locationId) =>
            Error.NotFound(
                code: "StockTransfer.StockLocationNotFound",
                description: $"Stock location with ID '{locationId}' was not found.");

        public static Error NotFound(Guid id) =>
            Error.NotFound(
                code: "StockTransfer.NotFound",
                description: $"Stock transfer with ID '{id}' was not found.");

        public static Error InvalidQuantity =>
            Error.Validation(
                code: "StockTransfer.InvalidQuantity",
                description: "Quantity must be positive.");
    }
    #endregion

    #region Properties
    /// <summary>Gets the source location ID (null for supplier receipts).</summary>
    public Guid? SourceLocationId { get; set; }

    /// <summary>Gets the destination location ID (required).</summary>
    public Guid DestinationLocationId { get; set; }

    /// <summary>Gets the auto-generated transfer number for reference.</summary>
    public string Number { get; set; } = default!;

    /// <summary>Gets the optional reference code (e.g., purchase order number, shipment ID).</summary>
    public string? Reference { get; set; }
    #endregion

    #region Relationships
    public StockLocation? SourceLocation { get; set; }
    public StockLocation DestinationLocation { get; set; } = null!;
    public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();
    #endregion

    #region Constructors
    private StockTransfer() { }
    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new stock transfer or receipt request.
    /// </summary>
    /// <param name="destinationLocationId">The location receiving the stock (required).</param>
    /// <param name="sourceLocationId">The location sending the stock (null for supplier receipts).</param>
    /// <param name="reference">Optional reference code (PO number, supplier ID, etc.).</param>
    /// <returns>
    /// On success: A new StockTransfer instance with auto-generated transfer number.
    /// On failure: Error if source and destination are the same location.
    /// </returns>
    /// <remarks>
    /// Transfer numbers are automatically generated with format "T" + sequential number.
    /// </remarks>
    public static ErrorOr<StockTransfer> Create(
        Guid destinationLocationId,
        Guid? sourceLocationId = null,
        string? reference = null)
    {
        if (sourceLocationId.HasValue && sourceLocationId == destinationLocationId)
            return Errors.SourceEqualsDestination;

        var transfer = new StockTransfer
        {
            Id = Guid.NewGuid(),
            Number = NumberGenerator.Generate(prefix: "T"),
            SourceLocationId = sourceLocationId,
            DestinationLocationId = destinationLocationId,
            Reference = reference,
            CreatedAt = DateTimeOffset.UtcNow
        };

        transfer.AddDomainEvent(
            domainEvent: new Events.StockTransferCreated(
                TransferId: transfer.Id,
                Number: transfer.Number,
                SourceLocationId: sourceLocationId,
                DestinationLocationId: destinationLocationId));

        return transfer;
    }

    #endregion

    #region Business Logic: Updates

    /// <summary>
    /// Updates transfer locations and reference information.
    /// </summary>
    /// <param name="destinationLocationId">The new destination location ID.</param>
    /// <param name="sourceLocationId">The new source location ID (null for supplier receipts).</param>
    /// <param name="reference">The new reference code.</param>
    /// <returns>
    /// On success: This transfer instance.
    /// On failure: Error if source and destination are the same.
    /// </returns>
    public ErrorOr<StockTransfer> Update(
        Guid destinationLocationId,
        Guid? sourceLocationId = null,
        string? reference = null)
    {
        bool changed = false;

        if (sourceLocationId.HasValue && sourceLocationId == destinationLocationId)
            return Errors.SourceEqualsDestination;

        if (SourceLocationId != sourceLocationId)
        {
            SourceLocationId = sourceLocationId;
            changed = true;
        }

        if (DestinationLocationId != destinationLocationId)
        {
            DestinationLocationId = destinationLocationId;
            changed = true;
        }

        if (Reference != reference)
        {
            Reference = reference;
            changed = true;
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(domainEvent: new Events.StockTransferUpdated(TransferId: Id));
        }

        return this;
    }

    #endregion

    #region Business Logic: Stock Transfer

    /// <summary>
    /// Executes a stock transfer from one location to another.
    /// </summary>
    /// <param name="sourceLocation">The source location (must match SourceLocationId).</param>
    /// <param name="destinationLocation">The destination location (must match DestinationLocationId).</param>
    /// <param name="variantsByQuantity">Dictionary of variants and quantities to transfer (all quantities must be positive).</param>
    /// <returns>
    /// On success: Success result and domain event published.
    /// On failure: List of errors (one or more variants may have failed validation or stock checks).
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Transfer Process for Each Variant:</b>
    /// <list type="number">
    /// <item>Validate quantity is positive</item>
    /// <item>Get or create StockItem at source location</item>
    /// <item>Check source has sufficient stock (unless backorderable)</item>
    /// <item>Unstock from source location</item>
    /// <item>Restock to destination location</item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <b>Error Handling:</b>
    /// All variants are validated before any stock movement occurs. If any variant fails,
    /// a list of all errors is returned without performing the transfer.
    /// </para>
    /// </remarks>
    public ErrorOr<Success> Transfer(
        StockLocation sourceLocation,
        StockLocation destinationLocation,
        IReadOnlyDictionary<Variant, int>? variantsByQuantity)
    {
        if (variantsByQuantity == null || !variantsByQuantity.Any())
            return Errors.NoVariants;

        if (sourceLocation.Id != SourceLocationId)
            return Errors.StockLocationNotFound(locationId: sourceLocation.Id);

        if (destinationLocation.Id != DestinationLocationId)
            return Errors.StockLocationNotFound(locationId: destinationLocation.Id);

        var errors = new List<Error>();

        foreach (var (variant, quantity) in variantsByQuantity)
        {
            // Validate quantity
            if (quantity <= 0)
            {
                errors.Add(item: Error.Validation(
                    code: "StockTransfer.InvalidQuantity",
                    description: $"Transfer quantity for variant {variant.Id} must be positive."));
                continue;
            }

            // Get or create stock item at source
            var sourceStockItemResult = sourceLocation.StockItemOrCreate(variant: variant);
            if (sourceStockItemResult.IsError)
            {
                errors.Add(item: sourceStockItemResult.FirstError);
                continue;
            }

            var sourceStockItem = sourceStockItemResult.Value;

            // Check sufficient stock in source (unless backorderable)
            if (!sourceStockItem.Backorderable && sourceStockItem.QuantityOnHand < quantity)
            {
                errors.Add(item: Errors.InsufficientStock(
                    variantId: variant.Id,
                    available: sourceStockItem.QuantityOnHand,
                    requested: quantity));
                continue;
            }

            // Unstock from source
            var unstockResult = sourceLocation.Unstock(
                variant: variant,
                quantity: quantity,
                originator: StockMovement.MovementOriginator.StockTransfer,
                stockTransferId: Id);
            if (unstockResult.IsError)
            {
                errors.Add(item: unstockResult.FirstError);
                continue;
            }

            // Restock to destination
            var restockResult = destinationLocation.Restock(
                variant: variant,
                quantity: quantity,
                originator: StockMovement.MovementOriginator.StockTransfer,
                stockTransferId: Id);
            if (restockResult.IsError)
            {
                errors.Add(item: restockResult.FirstError);
            }
        }

        if (errors.Any())
            return errors;

        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(
            domainEvent: new Events.StockTransferred(
                TransferId: Id,
                SourceLocationId: SourceLocationId,
                DestinationLocationId: DestinationLocationId,
                VariantsByQuantity: variantsByQuantity.ToDictionary(
                    keySelector: kvp => kvp.Key.Id,
                    elementSelector: kvp => kvp.Value)));

        return Result.Success;
    }

    #endregion

    #region Business Logic: Stock Receipt

    /// <summary>
    /// Executes a stock receipt from an external supplier (no source location).
    /// </summary>
    /// <param name="destinationLocation">The location receiving the stock (must match DestinationLocationId).</param>
    /// <param name="variantsByQuantity">Dictionary of variants and quantities to receive (all quantities must be positive).</param>
    /// <returns>
    /// On success: Success result and domain event published.
    /// On failure: List of errors if any variants have issues.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Receipt Process for Each Variant:</b>
    /// <list type="number">
    /// <item>Validate quantity is positive</item>
    /// <item>Get or create StockItem at destination location</item>
    /// <item>Restock at destination location with Supplier originator</item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <b>Supplier Movement Originator:</b>
    /// Unlike inter-location transfers, supplier receipts are marked with MovementOriginator.Supplier
    /// to distinguish them in the movement history.
    /// </para>
    /// </remarks>
    public ErrorOr<Success> Receive(
        StockLocation destinationLocation,
        IReadOnlyDictionary<Variant, int>? variantsByQuantity)
    {
        if (variantsByQuantity == null || !variantsByQuantity.Any())
            return Errors.NoVariants;

        if (destinationLocation.Id != DestinationLocationId)
            return Errors.StockLocationNotFound(locationId: destinationLocation.Id);

        var errors = new List<Error>();

        foreach (var (variant, quantity) in variantsByQuantity)
        {
            // Validate quantity
            if (quantity <= 0)
            {
                errors.Add(item: Error.Validation(
                    code: "StockTransfer.InvalidQuantity",
                    description: $"Receive quantity for variant {variant.Id} must be positive."));
                continue;
            }

            // Restock to destination with Supplier originator
            var restockResult = destinationLocation.Restock(
                variant: variant,
                quantity: quantity,
                originator: StockMovement.MovementOriginator.Supplier,
                stockTransferId: Id);

            if (restockResult.IsError)
            {
                errors.Add(item: restockResult.FirstError);
            }
        }

        if (errors.Any())
            return errors;

        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(
            domainEvent: new Events.StockReceived(
                TransferId: Id,
                DestinationLocationId: DestinationLocationId,
                VariantsByQuantity: variantsByQuantity.ToDictionary(
                    keySelector: kvp => kvp.Key.Id,
                    elementSelector: kvp => kvp.Value)));

        return Result.Success;
    }

    #endregion

    #region Business Logic: Lifecycle

    /// <summary>
    /// Deletes this stock transfer record.
    /// </summary>
    public ErrorOr<Deleted> Delete()
    {
        AddDomainEvent(domainEvent: new Events.StockTransferDeleted(TransferId: Id));
        return Result.Deleted;
    }

    #endregion

    #region Domain Events

    public static class Events
    {
        /// <summary>
        /// Raised when a new stock transfer is created.
        /// </summary>
        public sealed record StockTransferCreated(
            Guid TransferId,
            string Number,
            Guid? SourceLocationId,
            Guid DestinationLocationId) : DomainEvent;

        /// <summary>
        /// Raised when stock is successfully transferred between locations.
        /// </summary>
        public sealed record StockTransferred(
            Guid TransferId,
            Guid? SourceLocationId,
            Guid DestinationLocationId,
            IReadOnlyDictionary<Guid, int> VariantsByQuantity) : DomainEvent;

        /// <summary>
        /// Raised when stock is successfully received from a supplier.
        /// </summary>
        public sealed record StockReceived(
            Guid TransferId,
            Guid DestinationLocationId,
            IReadOnlyDictionary<Guid, int> VariantsByQuantity) : DomainEvent;

        /// <summary>
        /// Raised when a stock transfer is deleted.
        /// </summary>
        public sealed record StockTransferDeleted(Guid TransferId) : DomainEvent;

        /// <summary>
        /// Raised when a stock transfer's properties are updated.
        /// </summary>
        public sealed record StockTransferUpdated(Guid TransferId) : DomainEvent;
    }

    #endregion
}