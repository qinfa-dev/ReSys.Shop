namespace ReSys.Core.Common.Constants;

/// <summary>
/// Centralized store configuration options for ReSys.Shop.
/// These settings control various business logic behaviors across the platform.
/// 
/// Ported from Spree Commerce configuration system.
/// </summary>
public static class StoreConfiguration
{
    /// <summary>
    /// Determines whether the checkout process should continue if a payment gateway error occurs.
    /// When false, the checkout will halt and display an error to the customer.
    /// </summary>
    /// <remarks>
    /// Default: false (strict payment validation)
    /// Configuration Key: AllowCheckoutOnGatewayError
    /// </remarks>
    public const bool AllowCheckoutOnGatewayError = false;

    /// <summary>
    /// Determines whether a phone number is required for address forms.
    /// When true, customers must provide a phone number in their address details.
    /// </summary>
    /// <remarks>
    /// Default: false (phone is optional)
    /// Configuration Key: AddressRequiresPhone
    /// </remarks>
    public const bool AddressRequiresPhone = false;

    /// <summary>
    /// Determines if an alternative phone number should be present for the shipping address during checkout.
    /// When true, an additional phone field appears specifically for shipping address.
    /// </summary>
    /// <remarks>
    /// Default: false
    /// Configuration Key: AlternativeShippingPhone
    /// </remarks>
    public const bool AlternativeShippingPhone = false;

    /// <summary>
    /// Determines if the confirmation step is always included in the checkout process,
    /// regardless of the payment method used.
    /// </summary>
    /// <remarks>
    /// Default: false (confirmation may be skipped for certain payment methods)
    /// Configuration Key: AlwaysIncludeConfirmStep
    /// </remarks>
    public const bool AlwaysIncludeConfirmStep = false;

    /// <summary>
    /// Determines whether payments are automatically captured from the payment gateway.
    /// When true, payment is immediately charged. When false, payment is only authorized and must be manually captured.
    /// </summary>
    /// <remarks>
    /// Default: true (payments are auto-captured)
    /// Configuration Key: AutoCapture
    /// </remarks>
    public const bool AutoCapture = true;

    /// <summary>
    /// Determines if payment for each shipment should be automatically captured when the shipment is dispatched.
    /// Also makes the shipment ready when payment is authorized.
    /// </summary>
    /// <remarks>
    /// Default: false
    /// Configuration Key: AutoCaptureOnDispatch
    /// Requires: Active payment processing with shipment tracking
    /// </remarks>
    public const bool AutoCaptureOnDispatch = false;

    /// <summary>
    /// Determines whether a "Company" field displays on address forms during checkout.
    /// When true, customers can enter a company name in their address.
    /// </summary>
    /// <remarks>
    /// Default: false (company field hidden)
    /// Configuration Key: Company
    /// </remarks>
    public const bool Company = false;

    /// <summary>
    /// Determines if a new store credit allocation is created anytime store credit is added.
    /// When false, the store credit amount is updated in place without creating a new allocation.
    /// </summary>
    /// <remarks>
    /// Default: false (update amount in place)
    /// Configuration Key: CreditToNewAllocation
    /// Affects: Store credit management and history tracking
    /// </remarks>
    public const bool CreditToNewAllocation = false;

    /// <summary>
    /// Determines if the built-in SKU uniqueness validation is disabled.
    /// When true, duplicate SKUs are allowed across products.
    /// </summary>
    /// <remarks>
    /// Default: false (SKU validation enabled)
    /// Configuration Key: DisableSkuValidation
    /// Warning: Disabling this may cause inventory tracking issues
    /// </remarks>
    public const bool DisableSkuValidation = false;

    /// <summary>
    /// Determines if Store presence validation for Products and Payment Methods is disabled.
    /// When true, products and payment methods can exist without being associated with a store.
    /// </summary>
    /// <remarks>
    /// Default: false (store presence validation enabled)
    /// Configuration Key: DisableStorePresenceValidation
    /// Warning: Disabling this may cause data integrity issues in multi-store environments
    /// </remarks>
    public const bool DisableStorePresenceValidation = false;

    /// <summary>
    /// Determines if an exchange shipment is automatically kicked off upon return authorization save.
    /// This enables expedited exchanges for customers.
    /// </summary>
    /// <remarks>
    /// Default: false
    /// Configuration Key: ExpeditedExchanges
    /// Requirements: Payment profiles must be supported on your gateway and a configured delayed job handler
    /// </remarks>
    public const bool ExpeditedExchanges = false;

    /// <summary>
    /// The number of days the customer has to return their item after an expedited exchange is shipped
    /// to avoid being charged for the replacement.
    /// </summary>
    /// <remarks>
    /// Default: 14 days
    /// Configuration Key: ExpeditedExchangesDaysWindow
    /// Related: ExpeditedExchanges setting
    /// </remarks>
    public const int ExpeditedExchangesDaysWindow = 14;

    /// <summary>
    /// Determines if inventory should be restocked when an order is canceled or returned.
    /// When true, cancellations and returns increase available inventory. When false, inventory is not restored.
    /// </summary>
    /// <remarks>
    /// Default: true (inventory is restocked)
    /// Configuration Key: RestockInventory
    /// Important: Affects inventory accuracy and financial reconciliation
    /// </remarks>
    public const bool RestockInventory = true;

    /// <summary>
    /// The number of days after purchase within which a customer can initiate a return.
    /// After this period, returns are not eligible.
    /// </summary>
    /// <remarks>
    /// Default: 365 days (1 year)
    /// Configuration Key: ReturnEligibilityNumberOfDays
    /// Business Rule: Affects return request validation
    /// </remarks>
    public const int ReturnEligibilityNumberOfDays = 365;

    /// <summary>
    /// Determines if products without a price are shown in the storefront and Storefront API.
    /// When false, only products with prices are displayed to customers.
    /// </summary>
    /// <remarks>
    /// Default: false (unpruced products hidden)
    /// Configuration Key: ShowProductsWithoutPrice
    /// Related: Catalog visibility and product availability rules
    /// </remarks>
    public const bool ShowProductsWithoutPrice = false;

    /// <summary>
    /// Determines if inventory levels should be tracked when products are purchased at checkout.
    /// When true, InventoryUnit objects are created for each purchased product, tracking quantity.
    /// </summary>
    /// <remarks>
    /// Default: true (inventory is tracked)
    /// Configuration Key: TrackInventoryLevels
    /// Important: Essential for accurate stock management
    /// Affects: Inventory.InventoryUnit creation and stock accuracy
    /// </remarks>
    public const bool TrackInventoryLevels = true;
}
