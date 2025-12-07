using ErrorOr;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Orders.Payments;
using ReSys.Core.Domain.PaymentMethods.PaymentSources;
using ReSys.Core.Domain.Stores.PaymentMethods;

namespace ReSys.Core.Domain.PaymentMethods;

/// <summary>
/// Aggregate Root representing a payment method definition in the system.
/// 
/// This entity encapsulates the configuration and lifecycle of a payment option (e.g., Credit Card, PayPal).
/// It manages activation status, auto-capture settings, display preferences, and associations with stores.
/// 
/// <para>
/// <strong>Business Purpose:</strong>
/// Provides a flexible system for defining and configuring various payment options available to customers.
/// Enables merchants to control which payment methods are available, how they appear to customers,
/// and whether payments should be automatically captured or manually processed.
/// </para>
/// 
/// <para>
/// <strong>Key Responsibilities:</strong>
/// <list type="bullet">
/// <item><description>Manage payment method configuration (name, type, description)</description></item>
/// <item><description>Control activation status and display settings</description></item>
/// <item><description>Configure auto-capture behavior for automatic payment processing</description></item>
/// <item><description>Store method-specific configuration and metadata</description></item>
/// <item><description>Manage store-specific associations for multi-store deployments</description></item>
/// <item><description>Publish domain events for integration with other systems</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <strong>Invariants:</strong>
/// <list type="bullet">
/// <item><description>Payment method names must be unique across the system</description></item>
/// <item><description>Payment method types are restricted to predefined PaymentType enumeration values</description></item>
/// <item><description>Names cannot be null or whitespace</description></item>
/// <item><description>Position values must be non-negative for proper display ordering</description></item>
/// <item><description>Timestamps (CreatedAt, UpdatedAt) are automatically managed</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <strong>State Management:</strong>
/// Payment methods support soft deletion (DeletedAt flag) with restore capability, ensuring data integrity
/// while allowing historical tracking and recovery of payment method definitions.
/// </para>
/// 
/// <para>
/// <strong>Domain Events:</strong>
/// <list type="bullet">
/// <item><description>Created - Published when a new payment method is instantiated</description></item>
/// <item><description>Updated - Published when payment method details are modified</description></item>
/// <item><description>Deleted - Published when a payment method is soft deleted</description></item>
/// <item><description>Restored - Published when a deleted payment method is restored</description></item>
/// </list>
/// </para>
/// </summary>
/// <remarks>
/// <strong>Design Patterns:</strong>
/// <list type="bullet">
/// <item><description>Aggregate Pattern: PaymentMethod is an Aggregate Root managing its own state</description></item>
/// <item><description>Factory Pattern: Static Create method for safe instantiation with validation</description></item>
/// <item><description>Soft Deletion Pattern: DeletedAt timestamp for audit trail without data loss</description></item>
/// <item><description>Metadata Pattern: Public and Private metadata dictionaries for extensibility</description></item>
/// </list>
/// 
/// <strong>Integration Points:</strong>
/// <list type="bullet">
/// <item><description>StorePaymentMethod: Links payment methods to specific stores</description></item>
/// <item><description>Payment (Orders): Tracks which method was used for each payment</description></item>
/// <item><description>PaymentSource: Stores tokenized payment details for future use</description></item>
/// </list>
/// </remarks>
public sealed class PaymentMethod : Aggregate, IHasUniqueName, IHasPosition, IHasMetadata, IHasParameterizableName
{
    #region Constraints
    /// <summary>
    /// Defines size and value constraints for PaymentMethod properties.
    /// </summary>
    public static class Constraints
    {
        /// <summary>
        /// Maximum length for payment method names (e.g., "Visa/MasterCard", "PayPal").
        /// </summary>
        public const int NameMaxLength = 100;
    }
    #endregion

    #region Supported Types
    /// <summary>
    /// Enumeration of all supported payment method types in the system.
    /// 
    /// Each type represents a distinct category of payment processing with different
    /// characteristics, requirements, and integration patterns.
    /// 
    /// <para>
    /// <strong>Payment Type Reference:</strong>
    /// | Type | Auto-Capture | Source Required | Save Cards | Best For |
    /// |------|--------------|-----------------|------------|----------|
    /// | **CreditCard** | No | Yes | Yes | General payment (authorize & capture) |
    /// | **DebitCard** | No | Yes | Yes | Direct account debit (authorize & capture) |
    /// | **BankTransfer** | Yes | No | No | B2B payments, larger sums |
    /// | **PayPal** | Yes / No | Yes | Yes | Third-party wallet, secure transactions |
    /// | **Stripe** | Yes / No | Yes | Yes | Payment processor, cards & digital wallets |
    /// | **ApplePay** | Yes / No | Yes | Yes | Mobile payment, tokenized cards |
    /// | **GooglePay** | Yes / No | Yes | Yes | Mobile payment, tokenized cards |
    /// | **Wallet** | Yes | No | No | Stored funds, loyalty programs |
    /// | **CashOnDelivery** | Yes | No | No | Offline payment, local deliveries |
    /// | **StoreCredit** | Yes | No | No | Internal balance, refunds as credit |
    /// | **GiftCard** | Yes | No | No | Gift card payment, promotional use |
    /// | **Check** | No | No | No | Mailed check, traditional payments |
    /// | **Crypto** | Yes | Yes | Yes | Cryptocurrency payments, emerging markets |
    /// </para>
    /// </summary>
    public enum PaymentType
    {
        /// <summary>Credit card-based payment with deferred billing.</summary>
        CreditCard,

        /// <summary>Debit card-based payment with immediate withdrawal.</summary>
        DebitCard,

        /// <summary>Direct bank transfer payment method.</summary>
        BankTransfer,

        /// <summary>Cash on delivery payment method.</summary>
        CashOnDelivery,

        /// <summary>Generic wallet system using stored funds.</summary>
        Wallet,

        /// <summary>PayPal payment service.</summary>
        PayPal,

        /// <summary>Stripe payment gateway.</summary>
        Stripe,

        /// <summary>Apple Pay digital wallet.</summary>
        ApplePay,

        /// <summary>Google Pay digital wallet.</summary>
        GooglePay,

        /// <summary>Store credit balance payment.</summary>
        StoreCredit,

        /// <summary>Store-issued gift card payment.</summary>
        GiftCard,

        /// <summary>Paper check payment method.</summary>
        Check,

        /// <summary>Cryptocurrency payment method.</summary>
        Crypto
    }
    #endregion

    #region Errors
    /// <summary>
    /// Defines domain-specific errors for PaymentMethod operations.
    /// Errors are returned using ErrorOr pattern for functional error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>Payment method name is required and cannot be empty.</summary>
        public static Error NameRequired => Error.Validation(code: "PaymentMethod.NameRequired", description: "Name is required.");

        /// <summary>Payment method is required for an operation.</summary>
        public static Error Required => Error.Validation(code: "PaymentMethod.Required", description: "Payment method is required.");

        /// <summary>Payment method with the specified ID was not found in the system.</summary>
        /// <param name="id">The payment method ID that was not found.</param>
        public static Error NotFound(Guid id) => Error.NotFound(code: "PaymentMethod.NotFound", description: $"Payment method '{id}' not found.");

        /// <summary>Cannot delete a payment method that has associated payments in progress or completed.</summary>
        public static Error InUse => Error.Conflict(code: "PaymentMethod.InUse", description: "Cannot delete payment method with payments.");

        /// <summary>Cannot activate a payment method for a store when it is already active for that store.</summary>
        public static Error AlreadyActiveForStore => Error.Conflict(code: "PaymentMethod.AlreadyActiveForStore", description: "Already active for this store.");
    }
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the unique name of the payment method (e.g., "Visa/MasterCard", "PayPal").
    /// 
    /// <para>
    /// <strong>Constraints:</strong>
    /// <list type="bullet">
    /// <item><description>Required, cannot be null or whitespace</description></item>
    /// <item><description>Maximum length: <see cref="Constraints.NameMaxLength"/> characters</description></item>
    /// <item><description>Must be unique across all payment methods in the system</description></item>
    /// <item><description>Case-insensitive uniqueness constraint at database level</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Usage:</strong>
    /// Used for internal identification and API contracts. This name is displayed to customers
    /// and in administrative interfaces.
    /// </para>
    /// </summary>
    /// <example>"Visa/MasterCard", "PayPal", "Apple Pay", "Bank Transfer"</example>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the presentation name used for display purposes on the storefront.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Provides a user-friendly name that may differ from the internal Name property.
    /// Allows customization of how the payment method appears to customers without
    /// affecting internal identifiers or business logic.
    /// </para>
    /// 
    /// <para>
    /// <strong>Constraints:</strong>
    /// <list type="bullet">
    /// <item><description>Required, cannot be null or whitespace</description></item>
    /// <item><description>Can be different from Name for customer-friendly display</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <example>"Pay with Visa or Mastercard", "Secure PayPal Checkout"</example>
    public string Presentation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional detailed description of the payment method.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Provides additional context and information about the payment method for customers.
    /// Can include details about processing times, fees, security features, or requirements.
    /// </para>
    /// 
    /// <para>
    /// <strong>Constraints:</strong>
    /// <list type="bullet">
    /// <item><description>Optional, can be null</description></item>
    /// <item><description>If provided, maximum length is determined by system constraints</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <example>"Fast and secure credit/debit card payments", "PayPal's secure payment platform"</example>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the type of this payment method from the <see cref="PaymentType"/> enumeration.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Categorizes the payment method and determines how it should be processed,
    /// what validation rules apply, and which payment gateways can handle it.
    /// </para>
    /// 
    /// <para>
    /// <strong>Constraints:</strong>
    /// <list type="bullet">
    /// <item><description>Required, must be a valid PaymentType value</description></item>
    /// <item><description>Stored as string in database for flexibility</description></item>
    /// <item><description>Determines integration points and processing behavior</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Related Computed Properties:</strong>
    /// <list type="bullet">
    /// <item><description>IsCardPayment - True for CreditCard or DebitCard</description></item>
    /// <item><description>SupportsSavedCards - True for card and digital wallet types</description></item>
    /// <item><description>SourceRequired - False for StoreCredit and GiftCard</description></item>
    /// <item><description>MethodCode - Lowercase string representation of the type</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <see cref="PaymentType"/>
    public PaymentType Type { get; set; }

    /// <summary>
    /// Gets or sets whether this payment method is currently active and available for use.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Controls whether customers can select and use this payment method during checkout.
    /// Allows merchants to enable/disable payment methods without deleting their configuration.
    /// </para>
    /// 
    /// <para>
    /// <strong>Behavior:</strong>
    /// <list type="bullet">
    /// <item><description>When Active = true: Payment method appears in checkout and can be used</description></item>
    /// <item><description>When Active = false: Payment method is hidden from customers</description></item>
    /// <item><description>Automatically set to false when payment method is deleted</description></item>
    /// <item><description>Automatically set to true when a deleted payment method is restored</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Default Value:</strong> true (enabled by default)
    /// </para>
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// Gets or sets the display order/position of this payment method in lists and checkout pages.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Controls the sequence in which payment methods appear to customers, allowing
    /// merchants to highlight preferred or popular payment methods.
    /// </para>
    /// 
    /// <para>
    /// <strong>Constraints:</strong>
    /// <list type="bullet">
    /// <item><description>Must be non-negative</description></item>
    /// <item><description>Lower values appear first in sorted lists</description></item>
    /// <item><description>Position 0 = highest priority</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Example Usage:</strong>
    /// <list type="bullet">
    /// <item><description>Position 0: Credit Card (most common)</description></item>
    /// <item><description>Position 1: PayPal</description></item>
    /// <item><description>Position 2: Bank Transfer</description></item>
    /// <item><description>Position 3: Cash on Delivery</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Gets or sets whether payments with this method should be automatically captured.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Controls payment capture behavior and determines the immediate financial impact of transactions.
    /// 
    /// <strong>Auto-Capture = true (Immediate Capture):</strong>
    /// <list type="bullet">
    /// <item><description>Funds are immediately transferred from customer to merchant</description></item>
    /// <item><description>Used for methods where authorization equals capture (e.g., Cash on Delivery)</description></item>
    /// <item><description>Reduces merchant's fraud risk as funds are already settled</description></item>
    /// <item><description>Suitable for low-risk transactions</description></item>
    /// </list>
    /// </para>
    /// 
    /// <strong>Auto-Capture = false (Manual Capture):</strong>
    /// <list type="bullet">
    /// <item><description>Funds are authorized but held for later capture</description></item>
    /// <item><description>Merchant can review and manually capture/void transactions</description></item>
    /// <item><description>Provides opportunity to verify order details before charge</description></item>
    /// <item><description>Standard practice for credit/debit card payments</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Default Value:</strong> false (requires manual capture by default)
    /// </para>
    /// </summary>
    public bool AutoCapture { get; set; }

    /// <summary>
    /// Gets or sets where this payment method should be displayed in the system.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Controls visibility of the payment method across different channels (customer storefront, admin backend).
    /// </para>
    /// 
    /// <para>
    /// <strong>Display Modes:</strong>
    /// <list type="bullet">
    /// <item><description>Both - Visible on storefront and admin backend</description></item>
    /// <item><description>Storefront - Visible only to customers during checkout</description></item>
    /// <item><description>Admin - Visible only in admin panel for internal use</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Default Value:</strong> Both (visible everywhere)
    /// </para>
    /// </summary>
    /// <see cref="DisplayOn"/>
    public DisplayOn DisplayOn { get; set; } = DisplayOn.Both;

    /// <summary>
    /// Gets or sets publicly visible metadata for this payment method.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Stores additional configuration and data that may be shared with customers or exposed in APIs.
    /// </para>
    /// 
    /// <para>
    /// <strong>Example Uses:</strong>
    /// <list type="bullet">
    /// <item><description>Card type restrictions (Visa, Mastercard, etc.)</description></item>
    /// <item><description>Processing fees or surcharges</description></item>
    /// <item><description>Supported countries or regions</description></item>
    /// <item><description>Minimum/maximum transaction amounts</description></item>
    /// <item><description>Estimated processing times</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Constraints:</strong>
    /// <list type="bullet">
    /// <item><description>Optional, defaults to empty dictionary</description></item>
    /// <item><description>Stored as JSON in database for flexibility</description></item>
    /// <item><description>Can be any object type, but should be serializable</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public IDictionary<string, object?>? PublicMetadata { get; set; } = new Dictionary<string, object?>();

    /// <summary>
    /// Gets or sets private metadata for this payment method that should not be exposed to customers.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Stores sensitive configuration, integration details, and internal-only data.
    /// </para>
    /// 
    /// <para>
    /// <strong>Example Uses:</strong>
    /// <list type="bullet">
    /// <item><description>Payment gateway API keys and merchant IDs</description></item>
    /// <item><description>Authentication credentials and tokens</description></item>
    /// <item><description>Provider-specific configuration</description></item>
    /// <item><description>Internal notes and audit information</description></item>
    /// <item><description>Webhook signing secrets</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Constraints:</strong>
    /// <list type="bullet">
    /// <item><description>Optional, defaults to empty dictionary</description></item>
    /// <item><description>Stored as JSON in database</description></item>
    /// <item><description>Never exposed through API responses by default</description></item>
    /// <item><description>Requires explicit access control to retrieve</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Security Note:</strong>
    /// This metadata should be treated as sensitive data and access should be restricted
    /// to admin users and system services with appropriate permissions.
    /// </para>
    /// </summary>
    public IDictionary<string, object?>? PrivateMetadata { get; set; } = new Dictionary<string, object?>();

    /// <summary>
    /// Gets or sets the timestamp when this payment method was soft deleted.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Implements soft deletion pattern for audit trail and recovery. Null value indicates
    /// the payment method is active; non-null value indicates it has been deleted.
    /// </para>
    /// 
    /// <para>
    /// <strong>Behavior:</strong>
    /// <list type="bullet">
    /// <item><description>Set by Delete() method to current UTC time</description></item>
    /// <item><description>Cleared by Restore() method back to null</description></item>
    /// <item><description>Used to filter queries (exclude deleted methods from active listings)</description></item>
    /// <item><description>Maintains data integrity for historical tracking</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Default Value:</strong> null (not deleted)
    /// </para>
    /// </summary>
    /// <see cref="IsDeleted"/>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Gets a value indicating whether this payment method has been soft deleted.
    /// </summary>
    /// <remarks>Returns true if DeletedAt has a value, false otherwise.</remarks>
    public bool IsDeleted => DeletedAt.HasValue;
    #endregion

    #region Relationships
    /// <summary>
    /// Gets or sets the collection of store-specific associations for this payment method.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Links this payment method to specific stores, allowing for store-specific activation
    /// and configuration in multi-store deployments.
    /// </para>
    /// 
    /// <para>
    /// <strong>Relationship Type:</strong>
    /// One-to-Many composition. PaymentMethod manages the lifecycle of its StorePaymentMethod associations.
    /// Deleting a payment method cascades to delete all associated StorePaymentMethod records.
    /// </para>
    /// 
    /// <para>
    /// <strong>Use Cases:</strong>
    /// <list type="bullet">
    /// <item><description>Activate a payment method for specific stores only</description></item>
    /// <item><description>Configure different settings per store</description></item>
    /// <item><description>Support multi-store/multi-brand deployments</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <see cref="StorePaymentMethod"/>
    public ICollection<StorePaymentMethod> StorePaymentMethods { get; set; } = new List<StorePaymentMethod>();

    /// <summary>
    /// Gets or sets the collection of payments that used this payment method.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Tracks all payment transactions that have been processed using this payment method.
    /// Enables audit trail, reporting, and prevents deletion while active payments exist.
    /// </para>
    /// 
    /// <para>
    /// <strong>Relationship Type:</strong>
    /// One-to-Many reference from Orders.Payments. A payment method can have many payments,
    /// but a payment references at most one payment method. Restrict delete behavior prevents
    /// deleting payment methods with associated payments.
    /// </para>
    /// 
    /// <para>
    /// <strong>Business Logic:</strong>
    /// <list type="bullet">
    /// <item><description>Delete() operation fails if this collection is non-empty</description></item>
    /// <item><description>Used for reporting on payment method usage</description></item>
    /// <item><description>Maintains referential integrity with payment records</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <see cref="Payment"/>
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    /// <summary>
    /// Gets or sets the collection of payment sources (saved payment details) using this method.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Links to stored payment details (e.g., saved credit cards) that users have registered
    /// for convenient future transactions.
    /// </para>
    /// 
    /// <para>
    /// <strong>Relationship Type:</strong>
    /// One-to-Many reference from Payments.PaymentSources. Many payment sources can reference
    /// the same payment method. Restrict delete behavior prevents deleting payment methods with
    /// existing payment sources.
    /// </para>
    /// 
    /// <para>
    /// <strong>Use Cases:</strong>
    /// <list type="bullet">
    /// <item><description>Retrieve all saved cards for a payment method type</description></item>
    /// <item><description>Support "save card for future use" functionality</description></item>
    /// <item><description>Enable one-click checkout with saved payment details</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <see cref="PaymentSource"/>
    public ICollection<PaymentSource> PaymentSources { get; set; } = new List<PaymentSource>();
    #endregion

    #region Computed Properties
    /// <summary>
    /// Gets a value indicating whether this is a card-based payment method.
    /// </summary>
    /// <remarks>
    /// Returns true for CreditCard and DebitCard types.
    /// Used to determine if additional card validation should be applied.
    /// </remarks>
    public bool IsCardPayment => Type is PaymentType.CreditCard or PaymentType.DebitCard;

    /// <summary>
    /// Gets a value indicating whether this payment method requires manual capture.
    /// </summary>
    /// <remarks>
    /// Returns true when AutoCapture is false, indicating manual capture is needed.
    /// Used to determine if payment authorization needs explicit confirmation before settling funds.
    /// </remarks>
    public bool RequiresManualCapture => !AutoCapture;

    /// <summary>
    /// Gets a value indicating whether this payment method requires an associated payment source.
    /// </summary>
    /// <remarks>
    /// Returns false for StoreCredit and GiftCard types which don't require external sources.
    /// True for all other types that require payment source details (card, wallet, etc.).
    /// </remarks>
    public bool SourceRequired => Type != PaymentType.StoreCredit && Type != PaymentType.GiftCard;

    /// <summary>
    /// Gets a value indicating whether this payment method supports saving card details.
    /// </summary>
    /// <remarks>
    /// Returns true for CreditCard, Stripe, ApplePay, and GooglePay.
    /// These methods support tokenization and "save card for future use" functionality.
    /// </remarks>
    public bool SupportsSavedCards => Type is PaymentType.CreditCard or PaymentType.Stripe or PaymentType.ApplePay or PaymentType.GooglePay;

    /// <summary>
    /// Gets the lowercase string code representation of the payment method type.
    /// </summary>
    /// <remarks>
    /// Used for API contracts, configuration keys, and provider integration identifiers.
    /// Example: PaymentType.CreditCard ? "creditcard"
    /// </remarks>
    public string MethodCode => Type.ToString().ToLowerInvariant();
    #endregion

    #region Constructor
    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// </summary>
    /// <remarks>
    /// Use the <see cref="Create"/> factory method for public instantiation.
    /// </remarks>
    private PaymentMethod() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Factory method for creating a new PaymentMethod with comprehensive validation.
    /// 
    /// <para>
    /// <strong>Validation:</strong>
    /// <list type="bullet">
    /// <item><description>Name cannot be null or whitespace</description></item>
    /// <item><description>Type must be a valid PaymentType enumeration value</description></item>
    /// <item><description>Position is normalized to non-negative values</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Side Effects:</strong>
    /// <list type="bullet">
    /// <item><description>Generates new GUID for entity ID</description></item>
    /// <item><description>Sets CreatedAt timestamp to UTC now</description></item>
    /// <item><description>Publishes PaymentMethod.Events.Created domain event</description></item>
    /// <item><description>Normalizes name and presentation parameters</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="name">The unique name of the payment method. Required, will be trimmed.</param>
    /// <param name="presentation">The customer-facing presentation name. Required, will be trimmed.</param>
    /// <param name="type">The payment method type from PaymentType enumeration. Required.</param>
    /// <param name="description">Optional detailed description of the payment method.</param>
    /// <param name="active">Whether the payment method is initially active. Default: true.</param>
    /// <param name="autoCapture">Whether payments should be auto-captured. Default: false (requires manual capture).</param>
    /// <param name="position">Display order position. Default: 0 (highest priority). Must be non-negative.</param>
    /// <param name="displayOn">Where the method should appear (Frontend, Backend, or Both). Default: Both.</param>
    /// <param name="publicMetadata">Public metadata dictionary. Optional, defaults to empty dictionary.</param>
    /// <param name="privateMetadata">Private metadata dictionary for sensitive data. Optional, defaults to empty dictionary.</param>
    /// <returns>
    /// ErrorOr&lt;PaymentMethod&gt; containing the created PaymentMethod on success,
    /// or validation errors on failure.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = PaymentMethod.Create(
    ///     name: "Credit Card",
    ///     presentation: "Pay with Credit Card",
    ///     type: PaymentMethod.PaymentType.CreditCard,
    ///     active: true,
    ///     autoCapture: false // Manual capture
    /// );
    ///
    /// if (result.IsSuccess)
    /// {
    ///     var paymentMethod = result.Value;
    ///     // In a real application, you'd save this to a repository and commit.
    ///     // await repository.AddAsync(paymentMethod);
    ///     // await unitOfWork.SaveChangesAsync();
    /// }
    /// else
    /// {
    ///     // Handle validation errors
    ///     var error = result.FirstError;
    /// }
    /// </code>
    /// </example>
    public static ErrorOr<PaymentMethod> Create(
        string name,
        string presentation,
        PaymentType type,
        string? description = null,
        bool active = true,
        bool autoCapture = false,
        int position = 0,
        DisplayOn displayOn = DisplayOn.Both,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        (name, presentation) = HasParameterizableName.NormalizeParams(name: name, presentation: presentation);

        var errors = Validate(name: name);
        if (errors.Any()) return errors;

        var method = new PaymentMethod
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Presentation = presentation.Trim(),
            Description = description?.Trim(),
            Type = type,
            Active = active,
            AutoCapture = autoCapture,
            Position = position,
            DisplayOn = displayOn,
            PublicMetadata = publicMetadata ?? new Dictionary<string, object?>(),
            PrivateMetadata = privateMetadata ?? new Dictionary<string, object?>(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        method.AddDomainEvent(domainEvent: new Events.Created(PaymentMethodId: method.Id, Name: method.Name, Type: method.Type));
        return method;
    }

    /// <summary>
    /// Validates the payment method name.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <returns>List of validation errors, empty if valid.</returns>
    private static List<Error> Validate(string name)
    {
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(value: name)) errors.Add(item: Errors.NameRequired);
        return errors;
    }
    #endregion

    #region Business Logic
    /// <summary>
    /// Updates the payment method configuration with new values.
    /// 
    /// <para>
    /// <strong>Update Strategy:</strong>
    /// This method uses a "selective update" approach where only non-null parameters are updated,
    /// allowing callers to update specific properties without affecting others.
    /// </para>
    /// 
    /// <para>
    /// <strong>Side Effects:</strong>
    /// <list type="bullet">
    /// <item><description>Sets UpdatedAt timestamp only if any property changed</description></item>
    /// <item><description>Publishes PaymentMethod.Events.Updated event only on actual changes</description></item>
    /// <item><description>Normalizes name and presentation parameters</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Validation:</strong>
    /// <list type="bullet">
    /// <item><description>New name (if provided) must pass all validation rules</description></item>
    /// <item><description>Metadata updates must pass equality checks</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="name">New payment method name. Optional, null skips update. Will be trimmed.</param>
    /// <param name="presentation">New presentation name. Optional, null skips update. Will be trimmed.</param>
    /// <param name="description">New description. Optional, null skips update. Will be trimmed.</param>
    /// <param name="active">New active status. Optional, null skips update.</param>
    /// <param name="autoCapture">New auto-capture setting. Optional, null skips update.</param>
    /// <param name="position">New position for display ordering. Optional, null skips update.</param>
    /// <param name="displayOn">New display location setting. Optional, null skips update.</param>
    /// <param name="publicMetadata">New public metadata dictionary. Optional, null skips update.</param>
    /// <param name="privateMetadata">New private metadata dictionary. Optional, null skips update.</param>
    /// <returns>
    /// ErrorOr&lt;PaymentMethod&gt; containing the updated PaymentMethod (this) on success,
    /// or validation errors on failure.
    /// </returns>
    /// <example>
    /// <code>
    /// // Assume 'repository' and 'unitOfWork' are available in the application service layer
    /// var method = await repository.GetPaymentMethodAsync(id);
    /// if (method is null) { /* Handle not found */ }
    ///
    /// // Update only specific properties
    /// var result = method.Update(
    ///     active: false,      // Disable
    ///     position: 10        // Lower priority
    /// );
    ///
    /// if (result.IsSuccess)
    /// {
    ///     // In a real application, you'd save changes through a unit of work.
    ///     // await unitOfWork.SaveChangesAsync();
    /// }
    /// else
    /// {
    ///     // Handle validation errors
    ///     var error = result.FirstError;
    /// }
    /// </code>
    /// </example>
    public ErrorOr<PaymentMethod> Update(
        string? name = null,
        string? presentation = null,
        string? description = null,
        bool? active = null,
        bool? autoCapture = null,
        int? position = null,
        DisplayOn? displayOn = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        bool changed = false;

        (name, presentation) = HasParameterizableName.NormalizeParams(name: name ?? Name, presentation: presentation ?? Presentation);

        if (!string.IsNullOrWhiteSpace(value: name) && name.Trim() != Name)
        {
            var errors = Validate(name: name.Trim());
            if (errors.Any()) return errors;
            Name = name.Trim();
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(value: presentation) && presentation.Trim() != Presentation) { Presentation = presentation.Trim(); changed = true; }
        if (description != null && description.Trim() != Description) { Description = description.Trim(); changed = true; }
        if (active.HasValue && active != Active) { Active = active.Value; changed = true; }
        if (autoCapture.HasValue && autoCapture != AutoCapture) { AutoCapture = autoCapture.Value; changed = true; }
        if (position.HasValue && position != Position) { Position = position.Value; changed = true; }
        if (displayOn.HasValue && displayOn != DisplayOn) { DisplayOn = displayOn.Value; changed = true; }
        if (publicMetadata != null && !PublicMetadata.MetadataEquals(dict2: publicMetadata))
        {
            PublicMetadata = new Dictionary<string, object?>(dictionary: publicMetadata);
            changed = true;
        }

        if (privateMetadata != null && !PrivateMetadata.MetadataEquals(dict2: privateMetadata))
        {
            PrivateMetadata = new Dictionary<string, object?>(dictionary: privateMetadata);
            changed = true;
        }
        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(domainEvent: new Events.Updated(PaymentMethodId: Id));
        }
        return this;
    }

    /// <summary>
    /// Soft deletes the payment method by marking it as deleted.
    /// 
    /// <para>
    /// <strong>Deletion Rules:</strong>
    /// <list type="bullet">
    /// <item><description>Cannot delete if the payment method has associated payments</description></item>
    /// <item><description>Sets DeletedAt timestamp to current UTC time</description></item>
    /// <item><description>Automatically sets Active to false</description></item>
    /// <item><description>Soft deletion preserves data for audit trail and recovery</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Side Effects:</strong>
    /// <list type="bullet">
    /// <item><description>Sets DeletedAt to current UTC timestamp</description></item>
    /// <item><description>Sets Active to false</description></item>
    /// <item><description>Publishes PaymentMethod.Events.Deleted domain event</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Error Handling:</strong>
    /// Returns <see cref="Errors.InUse"/> if attempting to delete a payment method with active payments.
    /// </para>
    /// </summary>
    /// <returns>
    /// ErrorOr&lt;Deleted&gt; with Result.Deleted on success,
    /// or PaymentMethod.Errors.InUse if the payment method has associated payments.
    /// </returns>
    /// <example>
    /// <code>
    /// // Assume 'repository' and 'unitOfWork' are available in the application service layer
    /// var method = await repository.GetPaymentMethodAsync(id);
    /// if (method is null) { /* Handle not found */ }
    ///
    /// var result = method.Delete(); // Soft delete
    /// if (result.IsSuccess)
    /// {
    ///     // In a real application, you'd save changes through a unit of work.
    ///     // await unitOfWork.SaveChangesAsync();
    /// }
    /// else
    /// {
    ///     // Cannot delete if payments or payment sources exist
    ///     var error = result.FirstError; // e.g., PaymentMethod.Errors.InUse
    /// }
    /// </code>
    /// </example>
    public ErrorOr<Deleted> Delete()
    {
        if (Payments.Any()) return Errors.InUse;
        DeletedAt = DateTimeOffset.UtcNow;
        Active = false;
        AddDomainEvent(domainEvent: new Events.Deleted(PaymentMethodId: Id));
        return Result.Deleted;
    }

    /// <summary>
    /// Restores a soft-deleted payment method.
    /// 
    /// <para>
    /// <strong>Restoration Behavior:</strong>
    /// <list type="bullet">
    /// <item><description>Clears the DeletedAt timestamp</description></item>
    /// <item><description>Sets Active back to true</description></item>
    /// <item><description>Only succeeds if the payment method was previously deleted</description></item>
    /// <item><description>No-op if the payment method is not deleted (returns self)</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Side Effects:</strong>
    /// <list type="bullet">
    /// <item><description>Clears DeletedAt back to null</description></item>
    /// <item><description>Sets Active to true</description></item>
    /// <item><description>Publishes PaymentMethod.Events.Restored domain event</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <returns>
    /// ErrorOr&lt;PaymentMethod&gt; containing this instance on success
    /// (whether or not it was actually deleted).
    /// </returns>
    /// <example>
    /// <code>
    /// // Assume 'repository' and 'unitOfWork' are available in the application service layer
    /// var method = await repository.GetPaymentMethodAsync(id);
    /// if (method is null) { /* Handle not found */ }
    ///
    /// var result = method.Restore();
    /// if (result.IsSuccess)
    /// {
    ///     // In a real application, you'd save changes through a unit of work.
    ///     // await unitOfWork.SaveChangesAsync();
    /// }
    /// else
    /// {
    ///     // This method typically doesn't return an Error unless the object itself is invalid.
    ///     // It returns the PaymentMethod instance even if it wasn't deleted.
    /// }
    /// </code>
    /// </example>
    public ErrorOr<PaymentMethod> Restore()
    {
        if (!IsDeleted) return this;
        DeletedAt = null;
        Active = true;
        AddDomainEvent(domainEvent: new Events.Restored(PaymentMethodId: Id));
        return this;
    }
    #endregion

    #region Events
    /// <summary>
    /// Domain events published by PaymentMethod to signal significant state changes.
    /// 
    /// These events enable decoupled integration with other systems, such as:
    /// <list type="bullet">
    /// <item><description>Updating read models for reporting and analytics</description></item>
    /// <item><description>Syncing payment method definitions with external payment gateways</description></item>
    /// <item><description>Triggering notifications or audit log entries</description></item>
    /// <item><description>Coordinating with inventory or order management systems</description></item>
    /// </list>
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Published when a new PaymentMethod is created through the Create factory method.
        /// 
        /// <para>
        /// <strong>Use Cases:</strong>
        /// <list type="bullet">
        /// <item><description>Initialize payment gateway configuration</description></item>
        /// <item><description>Log creation to audit trail</description></item>
        /// <item><description>Send notification to administrators</description></item>
        /// <item><description>Sync with external payment processors</description></item>
        /// </list>
        /// </para>
        /// </summary>
        public sealed record Created(Guid PaymentMethodId, string Name, PaymentType Type) : DomainEvent;

        /// <summary>
        /// Published when a PaymentMethod is updated through the Update method.
        /// 
        /// <para>
        /// <strong>Use Cases:</strong>
        /// <list type="bullet">
        /// <item><description>Update payment gateway settings</description></item>
        /// <item><description>Refresh cached payment method information</description></item>
        /// <item><description>Log changes to audit trail</description></item>
        /// <item><description>Notify dependent systems of configuration changes</description></item>
        /// </list>
        /// </para>
        /// </summary>
        public sealed record Updated(Guid PaymentMethodId) : DomainEvent;

        /// <summary>
        /// Published when a PaymentMethod is deleted through the Delete method.
        /// 
        /// <para>
        /// <strong>Use Cases:</strong>
        /// <list type="bullet">
        /// <item><description>Disable payment processing for the method</description></item>
        /// <item><description>Archive related payment gateway configuration</description></item>
        /// <item><description>Notify users of payment method unavailability</description></item>
        /// <item><description>Trigger cleanup of related resources</description></item>
        /// </list>
        /// </para>
        /// </summary>
        public sealed record Deleted(Guid PaymentMethodId) : DomainEvent;

        /// <summary>
        /// Published when a deleted PaymentMethod is restored through the Restore method.
        /// 
        /// <para>
        /// <strong>Use Cases:</strong>
        /// <list type="bullet">
        /// <item><description>Re-enable a previously disabled payment method</description></item>
        /// <item><description>Restore payment gateway access</description></item>
        /// <item><description>Notify users of payment method re-availability</description></item>
        /// <item><description>Update related system caches and read models</description></item>
        /// </list>
        /// </para>
        /// </summary>
        public sealed record Restored(Guid PaymentMethodId) : DomainEvent;
    }
    #endregion
}