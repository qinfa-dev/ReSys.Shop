using ErrorOr;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Identity.Users;

// Added for JsonSerializer

namespace ReSys.Core.Domain.Payments.PaymentSources;

/// <summary>
/// Represents a stored payment source (e.g., saved credit card) for a user.
/// 
/// <para>
/// <strong>Business Purpose:</strong>
/// Enables users to securely store and reuse payment method details for convenient,
/// repeatable transactions. Supports streamlined checkout flows and enhances user experience
/// by reducing repetitive data entry while maintaining security best practices.
/// </para>
/// 
/// <para>
/// <strong>Key Responsibilities:</strong>
/// <list type="bullet">
/// <item><description>Store tokenized/masked payment details (not raw sensitive data)</description></item>
/// <item><description>Track payment source ownership and association with users</description></item>
/// <item><description>Manage payment source metadata and configuration</description></item>
/// <item><description>Support default payment source selection for user convenience</description></item>
/// <item><description>Track creation and modification timestamps for audit purposes</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <strong>Security Considerations:</strong>
/// <list type="bullet">
/// <item><description>Should never store raw payment card numbers or sensitive PII</description></item>
/// <item><description>Only stores last 4 digits of cards for display purposes</description></item>
/// <item><description>Card brand and expiration info stored for verification, not secrets</description></item>
/// <item><description>Sensitive data (private metadata) should be encrypted at rest</description></item>
/// <item><description>Access should be restricted to authenticated users and admin staff</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <strong>Invariants:</strong>
/// <list type="bullet">
/// <item><description>Must be associated with a valid ApplicationUser</description></item>
/// <item><description>Must reference a valid PaymentMethod</description></item>
/// <item><description>Expiration month must be between 1 and 12 (if provided)</description></item>
/// <item><description>Expiration year must not be in the past (if provided)</description></item>
/// <item><description>Type, Last4, and Brand must adhere to maximum length constraints</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <strong>Lifecycle:</strong>
/// Created when a user saves payment details, updated when modifying details or default status,
/// deleted when a user removes a saved payment source or when the associated user is deleted
/// (cascading delete).
/// </para>
/// </summary>
/// <remarks>
/// <strong>Design Patterns:</strong>
/// <list type="bullet">
/// <item><description>Auditable Entity Pattern: Inherits from AuditableEntity&lt;Guid&gt; for timestamp tracking</description></item>
/// <item><description>Metadata Pattern: Public and Private metadata for extensibility</description></item>
/// <item><description>Factory Pattern: Static Create method for safe instantiation with validation</description></item>
/// <item><description>Soft Updates: Update method for selective property modification</description></item>
/// </list>
/// 
/// <strong>Relationship Ownership:</strong>
/// PaymentSource is a child entity of ApplicationUser. Its lifecycle is tied to the user
/// and will be deleted if the associated user is deleted (cascade behavior).
/// 
/// <strong>Related Types:</strong>
/// Owns no child entities but references ApplicationUser and PaymentMethod aggregates.
/// </remarks>
public sealed class PaymentSource : AuditableEntity<Guid>, IHasMetadata
{
    #region Constraints
    /// <summary>
    /// Defines size and value constraints for PaymentSource properties.
    /// </summary>
    public static class Constraints
    {
        /// <summary>
        /// Maximum length for payment source type identifiers (e.g., "CreditCard", "PayPal", "ApplePay").
        /// Allows for reasonable length while preventing excessive storage usage.
        /// </summary>
        public const int TypeMaxLength = 50;

        /// <summary>
        /// Length of the Last4 property storing the last 4 digits of payment instruments.
        /// Exactly 4 characters for card numbers, numeric but stored as string.
        /// </summary>
        public const int Last4MaxLength = 4;

        /// <summary>
        /// Maximum length for credit card brand identifiers (e.g., "Visa", "MasterCard", "American Express").
        /// </summary>
        public const int BrandMaxLength = 50;
    }
    #endregion

    #region Errors
    /// <summary>
    /// Defines domain-specific errors for PaymentSource operations.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Payment source with the specified ID was not found in the system.
        /// </summary>
        /// <param name="id">The payment source ID that was not found.</param>
        public static Error NotFound(Guid id) => Error.NotFound(code: "PaymentSource.NotFound", description: $"Payment source with ID '{id}' was not found.");

        /// <summary>
        /// Expiration date validation failed (invalid month, year in past, etc.).
        /// </summary>
        public static Error InvalidExpirationDate => Error.Validation(code: "PaymentSource.InvalidExpirationDate", description: "Expiration date is invalid.");
    }
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the ID of the user who owns this payment source.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Links this payment source to the specific user account.
    /// </para>
    /// 
    /// <para>
    /// <strong>Constraints:</strong>
    /// <list type="bullet">
    /// <item><description>Required, cannot be null or empty</description></item>
    /// <item><description>Must reference a valid ApplicationUser</description></item>
    /// <item><description>Foreign key constraint: Cascading delete if user is deleted</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Use Cases:</strong>
    /// <list type="bullet">
    /// <item><description>Retrieve all saved payment methods for a specific user</description></item>
    /// <item><description>Filter payment sources when displaying checkout options</description></item>
    /// <item><description>Enforce user-level security and access control</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ID of the PaymentMethod definition this source is based on.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Associates this payment source with a specific payment method type
    /// (e.g., "Credit Card", "PayPal", "ApplePay").
    /// </para>
    /// 
    /// <para>
    /// <strong>Constraints:</strong>
    /// <list type="bullet">
    /// <item><description>Required, must reference a valid PaymentMethod</description></item>
    /// <item><description>Foreign key constraint: Restrict delete if payment sources exist</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Use Cases:</strong>
    /// <list type="bullet">
    /// <item><description>Determine payment method capabilities and configuration</description></item>
    /// <item><description>Retrieve payment method settings for processing</description></item>
    /// <item><description>Filter sources by payment method type</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public Guid PaymentMethodId { get; set; }

    /// <summary>
    /// Gets or sets the type of this payment source.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Identifies what kind of payment instrument is stored (e.g., "CreditCard", "PayPal").
    /// Often specific to the payment processor or gateway being used.
    /// </para>
    /// 
    /// <para>
    /// <strong>Constraints:</strong>
    /// <list type="bullet">
    /// <item><description>Required, cannot be null or whitespace</description></item>
    /// <item><description>Maximum length: <see cref="Constraints.TypeMaxLength"/> characters</description></item>
    /// <item><description>Stored as string for flexibility across payment providers</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Example Values:</strong>
    /// "CreditCard", "DebitCard", "PayPal", "ApplePay", "GooglePay", "DigitalWallet"
    /// </para>
    /// </summary>
    /// <example>"CreditCard", "PayPal", "ApplePay"</example>
    public string Type { get; set; } = null!;

    /// <summary>
    /// Gets or sets the last 4 digits of the payment card (if applicable).
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Provides a way for users to identify which card they're using without exposing
    /// the full card number. Purely for display purposes and user convenience.
    /// </para>
    /// 
    /// <para>
    /// <strong>Constraints:</strong>
    /// <list type="bullet">
    /// <item><description>Optional, applicable mainly to card-based payment sources</description></item>
    /// <item><description>Maximum length: <see cref="Constraints.Last4MaxLength"/> characters (typically "1234")</description></item>
    /// <item><description>Should be numeric as a string, without special formatting</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Security Note:</strong>
    /// This is safe to display and should never contain more than the last 4 digits.
    /// Never store full card numbers in this field.
    /// </para>
    /// 
    /// <para>
    /// <strong>Example Values:</strong>
    /// "4242", "5555", "3782", "6011"
    /// </para>
    /// </summary>
    /// <example>"4242", "5555"</example>
    public string? Last4 { get; set; }

    /// <summary>
    /// Gets or sets the brand of the credit/debit card.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Identifies the card issuer for display and filtering purposes.
    /// Helps users distinguish between multiple saved cards of different brands.
    /// </para>
    /// 
    /// <para>
    /// <strong>Constraints:</strong>
    /// <list type="bullet">
    /// <item><description>Optional, applicable to card-based payment sources</description></item>
    /// <item><description>Maximum length: <see cref="Constraints.BrandMaxLength"/> characters</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Common Values:</strong>
    /// "Visa", "Mastercard", "American Express", "Discover", "Diners Club", "UnionPay"
    /// </para>
    /// </summary>
    /// <example>"Visa", "Mastercard", "American Express"</example>
    public string? Brand { get; set; }

    /// <summary>
    /// Gets or sets the expiration month of the card (1-12).
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Stores the month portion of card expiration for validation and display.
    /// </para>
    /// 
    /// <para>
    /// <strong>Constraints:</strong>
    /// <list type="bullet">
    /// <item><description>Optional, but typically provided with card payment sources</description></item>
    /// <item><description>Valid range: 1 to 12 (January to December)</description></item>
    /// <item><description>Must be combined with ExpirationYear to validate full expiration</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Validation Logic:</strong>
    /// If both ExpirationMonth and ExpirationYear are provided, the card is considered expired
    /// if the expiration date is before the current month/year.
    /// </para>
    /// </summary>
    /// <example>1 (January), 6 (June), 12 (December)</example>
    public int? ExpirationMonth { get; set; }

    /// <summary>
    /// Gets or sets the expiration year of the card.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Stores the 4-digit year portion of card expiration for validation and display.
    /// </para>
    /// 
    /// <para>
    /// <strong>Constraints:</strong>
    /// <list type="bullet">
    /// <item><description>Optional, typically provided with card payment sources</description></item>
    /// <item><description>Must be a valid 4-digit year (e.g., 2024, 2025)</description></item>
    /// <item><description>Cannot be in the past (relative to current date)</description></item>
    /// <item><description>Must be combined with ExpirationMonth for full validation</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Expiration Validation:</strong>
    /// Card is expired if: (ExpirationYear < CurrentYear) OR
    /// (ExpirationYear == CurrentYear AND ExpirationMonth < CurrentMonth)
    /// </para>
    /// </summary>
    /// <example>2024, 2025, 2026</example>
    public int? ExpirationYear { get; set; }

    /// <summary>
    /// Gets or sets whether this payment source is the user's default choice.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Indicates the user's preferred payment method, displayed first during checkout
    /// and selected by default. Improves user experience by reducing decision friction.
    /// </para>
    /// 
    /// <para>
    /// <strong>Behavior:</strong>
    /// <list type="bullet">
    /// <item><description>When IsDefault = true: Automatically selected in checkout</description></item>
    /// <item><description>When IsDefault = false: User must actively select this method</description></item>
    /// <item><description>Only one default per user per payment method type is recommended</description></item>
    /// <item><description>Enforcement of "single default" is typically done at application service layer</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Default Value:</strong> false (must be explicitly set as default)
    /// </para>
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets publicly visible metadata for this payment source.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Stores additional configuration and data that may be shared with the user
    /// or exposed through API responses.
    /// </para>
    /// 
    /// <para>
    /// <strong>Example Uses:</strong>
    /// <list type="bullet">
    /// <item><description>Friendly display names created by the user</description></item>
    /// <item><description>Usage frequency or last used date</description></item>
    /// <item><description>Associated billing address information</description></item>
    /// <item><description>Processing fee information</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Constraints:</strong>
    /// <list type="bullet">
    /// <item><description>Optional, can be null</description></item>
    /// <item><description>Stored as JSON for flexibility</description></item>
    /// <item><description>Should only contain non-sensitive data suitable for user viewing</description></item>
    /// </list>
    /// </para>
    /// </summary>
    public IDictionary<string, object?>? PublicMetadata { get; set; }

    /// <summary>
    /// Gets or sets private metadata for this payment source that should not be exposed to users.
    /// 
    /// <para>
    /// <strong>Purpose:</strong>
    /// Stores sensitive configuration, tokenization details, and internal-only data
    /// that should be accessible only to system administrators and backend services.
    /// </para>
    /// 
    /// <para>
    /// <strong>Example Uses:</strong>
    /// <list type="bullet">
    /// <item><description>Payment gateway tokens or vault IDs</description></item>
    /// <item><description>3D Secure enrollment status</description></item>
    /// <item><description>Fraud risk scores or assessment data</description></item>
    /// <item><description>Payment processor-specific metadata</description></item>
    /// <item><description>Internal notes or audit information</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Constraints:</strong>
    /// <list type="bullet">
    /// <item><description>Optional, can be null</description></item>
    /// <item><description>Stored as JSON in database</description></item>
    /// <item><description>Never exposed in API responses by default</description></item>
    /// <item><description>Requires elevated permissions to access or modify</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Security Note:</strong>
    /// This field should be treated as sensitive and access should be restricted
    /// to authenticated administrators and backend services only.
    /// </para>
    /// </summary>
    public IDictionary<string, object?>? PrivateMetadata { get; set; }
    #endregion

    #region Relationships
    /// <summary>
    /// Gets or sets the ApplicationUser who owns this payment source.
    /// 
    /// <para>
    /// <strong>Relationship Type:</strong>
    /// Many-to-one navigation property. Multiple PaymentSources can belong to a single user,
    /// but each PaymentSource belongs to exactly one user.
    /// </para>
    /// 
    /// <para>
    /// <strong>Delete Behavior:</strong>
    /// Cascade - PaymentSources are automatically deleted if the associated user is deleted.
    /// </para>
    /// 
    /// <para>
    /// <strong>Foreign Key:</strong>
    /// UserId property
    /// </para>
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the PaymentMethod definition that this source uses.
    /// 
    /// <para>
    /// <strong>Relationship Type:</strong>
    /// Many-to-one navigation property. Multiple PaymentSources can reference the same
    /// PaymentMethod (e.g., multiple saved credit cards).
    /// </para>
    /// 
    /// <para>
    /// <strong>Delete Behavior:</strong>
    /// Restrict - Prevents deleting a PaymentMethod if PaymentSources reference it.
    /// </para>
    /// 
    /// <para>
    /// <strong>Foreign Key:</strong>
    /// PaymentMethodId property
    /// </para>
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; } = null!;
    #endregion

    #region Constructors
    /// <summary>
    /// Private parameterless constructor for EF Core.
    /// </summary>
    /// <remarks>
    /// Use the <see cref="Create"/> factory method for public instantiation.
    /// </remarks>
    private PaymentSource() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Factory method for creating a new PaymentSource with comprehensive validation.
    /// 
    /// <para>
    /// <strong>Validation:</strong>
    /// <list type="bullet">
    /// <item><description>If expiration month/year are provided, validates they are not in the past</description></item>
    /// <item><description>Expiration month must be between 1 and 12</description></item>
    /// <item><description>All string properties are trimmed during creation</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Side Effects:</strong>
    /// <list type="bullet">
    /// <item><description>Generates new GUID for entity ID</description></item>
    /// <item><description>Sets CreatedAt timestamp to UTC now</description></item>
    /// <item><description>Trims all string properties</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="userId">The ID of the user who owns this payment source. Required.</param>
    /// <param name="paymentMethodId">The ID of the PaymentMethod definition. Required.</param>
    /// <param name="type">The type of payment source (e.g., "CreditCard", "PayPal"). Required, will be trimmed.</param>
    /// <param name="last4">The last 4 digits of a card number (if applicable). Optional, will be trimmed.</param>
    /// <param name="brand">The card brand (e.g., "Visa", "Mastercard"). Optional, will be trimmed.</param>
    /// <param name="expirationMonth">The expiration month (1-12). Optional, validated if provided.</param>
    /// <param name="expirationYear">The expiration year (4-digit). Optional, validated if provided.</param>
    /// <param name="isDefault">Whether this is the user's default payment source. Default: false.</param>
    /// <param name="publicMetadata">Public metadata dictionary. Optional, can be null.</param>
    /// <param name="privateMetadata">Private metadata dictionary for sensitive data. Optional, can be null.</param>
    /// <returns>
    /// ErrorOr&lt;PaymentSource&gt; containing the created PaymentSource on success,
    /// or validation error (InvalidExpirationDate) on failure.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = PaymentSource.Create(
    ///     userId: "user123",
    ///     paymentMethodId: paymentMethodGuid,
    ///     type: "CreditCard",
    ///     last4: "4242",
    ///     brand: "Visa",
    ///     expirationMonth: 12,
    ///     expirationYear: 2026,
    ///     isDefault: true,
    ///     publicMetadata: new { "nickname": "My Visa" },
    ///     privateMetadata: new { "token": "tok_xxxxx" }
    /// );
    /// 
    /// if (result.IsSuccess)
    /// {
    ///     var paymentSource = result.Value;
    ///     // Use paymentSource...
    /// }
    /// else
    /// {
    ///     // Handle validation errors
    /// }
    /// </code>
    /// </example>
    public static ErrorOr<PaymentSource> Create(
        string userId,
        Guid paymentMethodId,
        string type,
        string? last4 = null,
        string? brand = null,
        int? expirationMonth = null,
        int? expirationYear = null,
        bool isDefault = false,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        if (expirationMonth.HasValue && expirationYear.HasValue)
        {
            if (expirationMonth < 1 || expirationMonth > 12 || expirationYear < DateTime.UtcNow.Year ||
                (expirationYear == DateTime.UtcNow.Year && expirationMonth < DateTime.UtcNow.Month))
            {
                return Errors.InvalidExpirationDate;
            }
        }

        return new PaymentSource
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PaymentMethodId = paymentMethodId,
            Type = type.Trim(),
            Last4 = last4?.Trim(),
            Brand = brand?.Trim(),
            ExpirationMonth = expirationMonth,
            ExpirationYear = expirationYear,
            IsDefault = isDefault,
            PublicMetadata = publicMetadata,
            PrivateMetadata = privateMetadata,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
    #endregion

    #region Business Logic
    /// <summary>
    /// Updates the payment source with new values using selective update approach.
    /// 
    /// <para>
    /// <strong>Update Strategy:</strong>
    /// Only non-null parameters are updated, allowing callers to update specific properties
    /// without affecting others.
    /// </para>
    /// 
    /// <para>
    /// <strong>Side Effects:</strong>
    /// <list type="bullet">
    /// <item><description>Sets UpdatedAt timestamp to current UTC time when changes are made</description></item>
    /// <item><description>Normalizes string properties by trimming whitespace</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Validation:</strong>
    /// No validation performed in update (assumes validation happened at service layer).
    /// </para>
    /// </summary>
    /// <param name="last4">New last 4 digits. Optional, null skips update. Will be trimmed.</param>
    /// <param name="brand">New card brand. Optional, null skips update. Will be trimmed.</param>
    /// <param name="expirationMonth">New expiration month. Optional, null skips update.</param>
    /// <param name="expirationYear">New expiration year. Optional, null skips update.</param>
    /// <param name="isDefault">New default status. Optional, null skips update.</param>
    /// <param name="publicMetadata">New public metadata. Optional, null skips update.</param>
    /// <param name="privateMetadata">New private metadata. Optional, null skips update.</param>
    /// <returns>
    /// ErrorOr&lt;Updated&gt; with Result.Updated on success.
    /// </returns>
    /// <example>
    /// <code>
    /// var source = await repository.GetPaymentSourceAsync(id);
    /// 
    /// // Update only specific properties
    /// var result = source.Update(
    ///     isDefault: true, // Mark as default payment source
    ///     expirationMonth: 6 // Update expiration month
    /// );
    /// 
    /// if (result.IsSuccess)
    /// {
    ///     await repository.SaveAsync();
    /// }
    /// </code>
    /// </example>
    public ErrorOr<Updated> Update(
        string? last4 = null,
        string? brand = null,
        int? expirationMonth = null,
        int? expirationYear = null,
        bool? isDefault = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        bool changed = false;

        if (last4 != null && last4 != Last4) { Last4 = last4.Trim(); changed = true; }
        if (brand != null && brand != Brand) { Brand = brand.Trim(); changed = true; }
        if (expirationMonth.HasValue && expirationMonth != ExpirationMonth) { ExpirationMonth = expirationMonth; changed = true; }
        if (expirationYear.HasValue && expirationYear != ExpirationYear) { ExpirationYear = expirationYear; changed = true; }
        if (isDefault.HasValue && isDefault != IsDefault) { IsDefault = isDefault.Value; changed = true; }
        if (publicMetadata != null) { PublicMetadata = publicMetadata; changed = true; }
        if (privateMetadata != null) { PrivateMetadata = privateMetadata; changed = true; }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        return Result.Updated;
    }

    /// <summary>
    /// Deletes the payment source.
    /// 
    /// <para>
    /// <strong>Behavior:</strong>
    /// Marks the payment source as deleted in the domain model. The actual deletion
    /// is handled by the database layer.
    /// </para>
    /// 
    /// <para>
    /// <strong>Note:</strong>
    /// This is a simple domain operation with no complex business rules or validations.
    /// A payment source can be deleted at any time without constraints.
    /// </para>
    /// </summary>
    /// <returns>
    /// ErrorOr&lt;Deleted&gt; with Result.Deleted.
    /// </returns>
    /// <example>
    /// <code>
    /// var source = await repository.GetPaymentSourceAsync(id);
    /// 
    /// var result = source.Delete();
    /// if (result.IsSuccess)
    /// {
    ///     await repository.DeleteAsync(source);
    ///     await repository.SaveAsync();
    /// }
    /// </code>
    /// </example>
    public ErrorOr<Deleted> Delete()
    {
        return Result.Deleted;
    }
    #endregion
}
