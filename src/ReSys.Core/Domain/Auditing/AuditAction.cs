namespace ReSys.Core.Domain.Auditing;

public static class AuditAction
{
    // CRUD Operations
    public const string Created = "Created";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";
    public const string SoftDeleted = "SoftDeleted";
    public const string Restored = "Restored";

    // Order Actions
    public const string OrderPlaced = "OrderPlaced";
    public const string OrderConfirmed = "OrderConfirmed";
    public const string OrderCanceled = "OrderCanceled";
    public const string OrderCompleted = "OrderCompleted";
    public const string OrderStateChanged = "OrderStateChanged";

    // Payment Actions
    public const string PaymentCaptured = "PaymentCaptured";
    public const string PaymentRefunded = "PaymentRefunded";
    public const string PaymentVoided = "PaymentVoided";
    public const string PaymentFailed = "PaymentFailed";

    // Shipment Actions
    public const string ShipmentCreated = "ShipmentCreated";
    public const string ShipmentShipped = "ShipmentShipped";
    public const string ShipmentDelivered = "ShipmentDelivered";
    public const string ShipmentCanceled = "ShipmentCanceled";

    // Inventory Actions
    public const string StockAdjusted = "StockAdjusted";
    public const string StockReserved = "StockReserved";
    public const string StockReleased = "StockReleased";
    public const string StockTransferred = "StockTransferred";

    // Product Actions
    public const string ProductPublished = "ProductPublished";
    public const string ProductUnpublished = "ProductUnpublished";
    public const string PriceChanged = "PriceChanged";
    public const string VariantAdded = "VariantAdded";

    // User Actions
    public const string UserRegistered = "UserRegistered";
    public const string UserLoggedIn = "UserLoggedIn";
    public const string UserLoggedOut = "UserLoggedOut";
    public const string PasswordChanged = "PasswordChanged";
    public const string EmailConfirmed = "EmailConfirmed";
    public const string RoleChanged = "RoleChanged";

    // Storefront Actions
    public const string StoreCreated = "StoreCreated";
    public const string StoreUpdated = "StoreUpdated";
    public const string ProductAddedToStore = "ProductAddedToStore";
    public const string ProductRemovedFromStore = "ProductRemovedFromStore";

    // Security Actions
    public const string AccessDenied = "AccessDenied";
    public const string PermissionGranted = "PermissionGranted";
    public const string PermissionRevoked = "PermissionRevoked";
    public const string SuspiciousActivity = "SuspiciousActivity";
}