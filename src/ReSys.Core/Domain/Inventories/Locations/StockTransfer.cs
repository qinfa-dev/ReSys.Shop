using ErrorOr;

using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Stocks;
using ReSys.Core.Domain.Orders;

namespace ReSys.Core.Domain.Inventories.Locations;

/// <summary>
/// Represents and orchestrates the movement of stock, either between two <see cref="StockLocation"/>s
/// (a transfer) or from an external vendor into a <see cref="StockLocation"/> (a receipt).
/// This aggregate manages the complex process of validating inventory, executing movements,
/// and recording corresponding stock movement history.
/// </summary>
/// <remarks>
/// <para>
/// <b>Responsibility:</b>
/// Coordinates stock transfers and receipts, ensuring that all affected <see cref="StockItem"/>s
/// are properly updated and comprehensive movement history is maintained. It acts as a central
/// orchestrator for complex inventory changes involving multiple locations and items.
/// </para>
///
/// <para>
/// <b>Supported Operations:</b>
/// <list type="bullet">
/// <item><b>Transfer:</b> Moves stock from a <c>SourceLocation</c> to a <c>DestinationLocation</c>.</item>
/// <item><b>Receive:</b> Records the receipt of new stock from an external supplier into a <c>DestinationLocation</c> (no source).</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Process Flow:</b>
/// <list type="number">
/// <item>Creation of a transfer or receipt request specifying variants and quantities.</item>
/// <item>Pre-validation to ensure quantities are positive and stock is available (for transfers).</item>
/// <item>Execution of <see cref="StockLocation.Unstock(Variant?, int, StockMovement.MovementOriginator, Guid?)"/> from the source (for transfers)
/// or <see cref="StockLocation.Restock(Variant, int, StockMovement.MovementOriginator, Guid?)"/> to the destination (for both transfers and receipts).</item>
/// <item>Recording of movement history with a transfer reference.</item>
/// <item>Publishing of domain events for audit trail and integration with other systems.</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Error Handling:</b>
/// The `Transfer` and `Receive` methods accumulate all validation and execution errors, returning them
/// together. This allows callers to identify all issues before attempting a retry or corrective action,
/// and facilitates transactional rollback at the application service level.
/// </para>
/// </remarks>
public sealed class StockTransfer : Aggregate
{
    #region Constraints
    /// <summary>
    /// Defines constraints and constant values specific to <see cref="StockTransfer"/> properties.
    /// These constraints ensure the validity of transfer-related data.
    /// </summary>
    public static class Constraints
    {
        /// <summary>Maximum allowed length for the auto-generated transfer <see cref="StockTransfer.Number"/>.</summary>
        public const int NumberMaxLength = 50;
        /// <summary>Maximum allowed length for an optional <see cref="StockTransfer.Reference"/> code.</summary>
        public const int ReferenceMaxLength = 255;
    }
    #endregion

    #region Errors
    /// <summary>
    /// Defines domain error scenarios specific to <see cref="StockTransfer"/> operations.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error indicating that no variants with positive quantities were specified for the transfer or receipt.
        /// </summary>
        public static Error NoVariants =>
            Error.Validation(
                code: "StockTransfer.NoVariants",
                description: "At least one variant with positive quantity must be specified.");

        /// <summary>
        /// Error indicating that the source and destination locations specified for a transfer are the same.
        /// </summary>
        public static Error SourceEqualsDestination =>
            Error.Validation(
                code: "StockTransfer.SourceEqualsDestination",
                description: "Source and destination locations cannot be the same.");

        /// <summary>
        /// Error indicating that there is insufficient stock of a variant at the source location for a transfer.
        /// </summary>
        /// <param name="variantId">The unique identifier of the variant with insufficient stock.</param>
        /// <param name="available">The quantity currently available at the source location.</param>
        /// <param name="requested">The quantity requested for transfer.</param>
        public static Error InsufficientStock(Guid variantId, int available, int requested) =>
            Error.Validation(
                code: "StockTransfer.InsufficientStock",
                description: $"Variant {variantId}: Only {available} units available, but {requested} units requested.");

        /// <summary>
        /// Error indicating that a specified variant could not be found.
        /// </summary>
        /// <param name="variantId">The unique identifier of the variant that was not found.</param>
        public static Error VariantNotFound(Guid variantId) =>
            Error.NotFound(
                code: "StockTransfer.VariantNotFound",
                description: $"Variant with ID '{variantId}' was not found.");

        /// <summary>
        /// Error indicating that a specified stock location could not be found.
        /// </summary>
        /// <param name="locationId">The unique identifier of the stock location that was not found.</param>
        public static Error StockLocationNotFound(Guid locationId) =>
            Error.NotFound(
                code: "StockTransfer.StockLocationNotFound",
                description: $"Stock location with ID '{locationId}' was not found.");

        /// <summary>
        /// Error indicating that a requested stock transfer record could not be found.
        /// </summary>
        /// <param name="id">The unique identifier of the stock transfer that was not found.</param>
        public static Error NotFound(Guid id) =>
            Error.NotFound(
                code: "StockTransfer.NotFound",
                description: $"Stock transfer with ID '{id}' was not found.");

        /// <summary>
        /// Error indicating that a quantity specified for a stock transfer or receipt is invalid (e.g., non-positive).
        /// </summary>
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

        // PHASE 1: VALIDATE ALL OPERATIONS (no mutations)
        var validationErrors = new List<Error>();
        var stockItemsToProcess = new List<(Variant variant, int quantity, StockItem sourceItem)>();

        foreach (var (variant, quantity) in variantsByQuantity)
        {
            // Validate quantity
            if (quantity <= 0)
            {
                validationErrors.Add(item: Error.Validation(
                    code: "StockTransfer.InvalidQuantity",
                    description: $"Transfer quantity for variant {variant.Id} must be positive."));
                continue;
            }

            // Get or create stock item at source (validation phase)
            var sourceStockItemResult = sourceLocation.StockItemOrCreate(variant: variant);
            if (sourceStockItemResult.IsError)
            {
                validationErrors.Add(item: sourceStockItemResult.FirstError);
                continue;
            }

            var sourceStockItem = sourceStockItemResult.Value;

            // Check sufficient stock in source (unless backorderable)
            if (!sourceStockItem.Backorderable && sourceStockItem.CountAvailable < quantity)
            {
                validationErrors.Add(item: Errors.InsufficientStock(
                    variantId: variant.Id,
                    available: sourceStockItem.CountAvailable,
                    requested: quantity));
                continue;
            }

            // Validate destination can receive (create stock item if needed)
            var destStockItemResult = destinationLocation.StockItemOrCreate(variant: variant);
            if (destStockItemResult.IsError)
            {
                validationErrors.Add(item: destStockItemResult.FirstError);
                continue;
            }

            stockItemsToProcess.Add((variant, quantity, sourceStockItem));
        }

        // Return all errors if any validation failed
        if (validationErrors.Any())
            return validationErrors;

        // PHASE 2: EXECUTE ALL OPERATIONS (all validated, now mutate)
        // NOTE: Caller must wrap this in a database transaction!
        var executionErrors = new List<Error>();

        foreach (var (variant, quantity, sourceStockItem) in stockItemsToProcess)
        {
            // Unstock from source
            var unstockResult = sourceLocation.Unstock(
                variant: variant,
                quantity: quantity,
                originator: StockMovement.MovementOriginator.StockTransfer,
                originatorId: Id);

            if (unstockResult.IsError)
            {
                executionErrors.Add(item: unstockResult.FirstError);
                // CRITICAL: In production, this should trigger transaction rollback
                continue;
            }

            // Restock to destination
            var restockResult = destinationLocation.Restock(
                variant: variant,
                quantity: quantity,
                originator: StockMovement.MovementOriginator.StockTransfer,
                originatorId: Id);

            if (restockResult.IsError)
            {
                executionErrors.Add(item: restockResult.FirstError);
                // CRITICAL: Transaction rollback needed here
                continue;
            }
        }

        if (executionErrors.Any())
        {
            // In a real system with proper transaction management,
            // this would trigger a rollback of all stock changes
            return Error.Failure(
                code: "StockTransfer.PartialFailure",
                description: "Transfer partially failed. Manual intervention may be required. " +
                            "Errors: " + string.Join(", ", executionErrors.Select(e => e.Description)));
        }

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

        // PHASE 1: VALIDATE
        var validationErrors = new List<Error>();
        var variantsToProcess = new List<(Variant variant, int quantity)>();

        foreach (var (variant, quantity) in variantsByQuantity)
        {
            if (quantity <= 0)
            {
                validationErrors.Add(item: Error.Validation(
                    code: "StockTransfer.InvalidQuantity",
                    description: $"Receive quantity for variant {variant.Id} must be positive."));
                continue;
            }

            // Validate destination can create stock item
            var stockItemResult = destinationLocation.StockItemOrCreate(variant: variant);
            if (stockItemResult.IsError)
            {
                validationErrors.Add(item: stockItemResult.FirstError);
                continue;
            }

            variantsToProcess.Add((variant, quantity));
        }

        if (validationErrors.Any())
            return validationErrors;

        // PHASE 2: EXECUTE
        var executionErrors = new List<Error>();

        foreach (var (variant, quantity) in variantsToProcess)
        {
            var restockResult = destinationLocation.Restock(
                variant: variant,
                quantity: quantity,
                originator: StockMovement.MovementOriginator.Supplier,
                originatorId: Id);

            if (restockResult.IsError)
            {
                executionErrors.Add(item: restockResult.FirstError);
            }
        }

        if (executionErrors.Any())
            return executionErrors;

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