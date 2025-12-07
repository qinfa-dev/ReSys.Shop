namespace ReSys.Core.Domain.Auditing;

/// <summary>
/// Provides a centralized collection of constant strings representing various auditable actions
/// performed within the application. These constants are used to categorize and describe events
/// recorded in audit logs, ensuring consistency and searchability of audit trails.
/// </summary>
/// <remarks>
/// This static class groups audit actions by functional area (e.g., CRUD, Order, Payment, Inventory, User).
/// Using these constants helps maintain a standardized vocabulary for auditing, making it easier to
/// filter, analyze, and report on system activities.
///
/// <strong>Example Usage:</strong>
/// <code>
/// // When creating an audit log entry
/// var auditLog = AuditLog.Create(
///     userId: "admin",
///     entityType: "Product",
///     entityId: productId,
///     action: AuditAction.Created,
///     description: "New product 'Laptop Pro' added.");
/// </code>
/// </remarks>
public static class AuditAction
{
    // CRUD Operations
    /// <summary>Indicates that a new entity was created.</summary>
    public const string Created = "Created";
    /// <summary>Indicates that an existing entity was updated.</summary>
    public const string Updated = "Updated";
    /// <summary>Indicates that an entity was permanently deleted.</summary>
    public const string Deleted = "Deleted";
    /// <summary>Indicates that an entity was logically soft-deleted.</summary>
    public const string SoftDeleted = "SoftDeleted";
    /// <summary>Indicates that a soft-deleted entity was restored.</summary>
    public const string Restored = "Restored";

    // Order Actions
    /// <summary>Indicates that a new order was placed by a customer.</summary>
    public const string OrderPlaced = "OrderPlaced";
    /// <summary>Indicates that an order was confirmed (e.g., after payment).</summary>
    public const string OrderConfirmed = "OrderConfirmed";
    /// <summary>Indicates that an order was canceled.</summary>
    public const string OrderCanceled = "OrderCanceled";
    /// <summary>Indicates that an order's fulfillment process was completed.</summary>
    public const string OrderCompleted = "OrderCompleted";
    /// <summary>Indicates that an order's state (e.g., from Pending to Processing) has changed.</summary>
    public const string OrderStateChanged = "OrderStateChanged";

    // Payment Actions
    /// <summary>Indicates that payment for an order was successfully captured.</summary>
    public const string PaymentCaptured = "PaymentCaptured";
    /// <summary>Indicates that a payment was refunded to the customer.</summary>
    public const string PaymentRefunded = "PaymentRefunded";
    /// <summary>Indicates that an authorized payment was voided before capture.</summary>
    public const string PaymentVoided = "PaymentVoided";
    /// <summary>Indicates that a payment attempt failed.</summary>
    public const string PaymentFailed = "PaymentFailed";

    // Shipment Actions
    /// <summary>Indicates that a new shipment was created for an order.</summary>
    public const string ShipmentCreated = "ShipmentCreated";
    /// <summary>Indicates that a shipment was dispatched to the carrier.</summary>
    public const string ShipmentShipped = "ShipmentShipped";
    /// <summary>Indicates that a shipment was delivered to the customer.</summary>
    public const string ShipmentDelivered = "ShipmentDelivered";
    /// <summary>Indicates that a shipment was canceled.</summary>
    public const string ShipmentCanceled = "ShipmentCanceled";

    // Inventory Actions
    /// <summary>Indicates that stock quantity for an item was adjusted (increased or decreased).</summary>
    public const string StockAdjusted = "StockAdjusted";
    /// <summary>Indicates that stock was reserved for an order or fulfillment.</summary>
    public const string StockReserved = "StockReserved";
    /// <summary>Indicates that previously reserved stock was released.</summary>
    public const string StockReleased = "StockReleased";
    /// <summary>Indicates that stock was transferred between locations.</summary>
    public const string StockTransferred = "StockTransferred";

    // Product Actions
    /// <summary>Indicates that a product was made visible/available for sale.</summary>
    public const string ProductPublished = "ProductPublished";
    /// <summary>Indicates that a product was hidden/made unavailable for sale.</summary>
    public const string ProductUnpublished = "ProductUnpublished";
    /// <summary>Indicates that a product's price was changed.</summary>
    public const string PriceChanged = "PriceChanged";
    /// <summary>Indicates that a new variant was added to a product.</summary>
    public const string VariantAdded = "VariantAdded";

    // User Actions
    /// <summary>Indicates that a new user account was registered.</summary>
    public const string UserRegistered = "UserRegistered";
    /// <summary>Indicates that a user successfully logged into the system.</summary>
    public const string UserLoggedIn = "UserLoggedIn";
    /// <summary>Indicates that a user logged out of the system.</summary>
    public const string UserLoggedOut = "UserLoggedOut";
    /// <summary>Indicates that a user's password was changed.</summary>
    public const string PasswordChanged = "PasswordChanged";
    /// <summary>Indicates that a user's email address was confirmed.</summary>
    public const string EmailConfirmed = "EmailConfirmed";
    /// <summary>Indicates that a user's role assignment was changed.</summary>
    public const string RoleChanged = "RoleChanged";

    // Storefront Actions
    /// <summary>Indicates that a new store was created.</summary>
    public const string StoreCreated = "StoreCreated";
    /// <summary>Indicates that store settings were updated.</summary>
    public const string StoreUpdated = "StoreUpdated";
    /// <summary>Indicates that a product was associated with a store.</summary>
    public const string ProductAddedToStore = "ProductAddedToStore";
    /// <summary>Indicates that a product was dissociated from a store.</summary>
    public const string ProductRemovedFromStore = "ProductRemovedFromStore";

    // Security Actions
    /// <summary>Indicates an attempt to access a resource was denied due to insufficient permissions.</summary>
    public const string AccessDenied = "AccessDenied";
    /// <summary>Indicates that a permission was granted to a role or user.</summary>
    public const string PermissionGranted = "PermissionGranted";
    /// <summary>Indicates that a permission was revoked from a role or user.</summary>
    public const string PermissionRevoked = "PermissionRevoked";
    /// <summary>Indicates that a suspicious activity or event was detected.</summary>
    public const string SuspiciousActivity = "SuspiciousActivity";
}