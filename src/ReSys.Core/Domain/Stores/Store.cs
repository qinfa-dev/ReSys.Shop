using ErrorOr;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Taxonomies;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Location;
using ReSys.Core.Domain.Location.Countries;
using ReSys.Core.Domain.Location.States;
using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.PaymentMethods;
using ReSys.Core.Domain.ShippingMethods;
using ReSys.Core.Domain.Stores.PaymentMethods;
using ReSys.Core.Domain.Stores.Products;
using ReSys.Core.Domain.Stores.ShippingMethods;
using ReSys.Core.Domain.Stores.StockLocations;

namespace ReSys.Core.Domain.Stores;

/// <summary>
/// Represents a storefront in a multi-store e-commerce system.
/// 
/// ## Role & Responsibility
/// The Store aggregate manages the complete configuration and setup of an individual sales channel, 
/// storefront, or brand within the application. Each Store is an independent business unit with:
/// - Distinct branding, pricing, and currency configuration
/// - Isolated order management and customer interactions
/// - Customized product catalog (products linked per store)
/// - Multi-warehouse fulfillment configuration
/// - Independent shipping and payment method offerings
/// - Store-specific SEO and contact information
/// 
/// ## Key Characteristics
/// - **Multi-Store Support**: System can manage multiple independent stores simultaneously
/// - **Product Isolation**: Products are shared globally but shown/hidden per store (via StoreProduct)
/// - **Inventory Isolation**: Stock locations are linked per store with priority ordering
/// - **Configuration Isolation**: Each store has independent payment methods, shipping options, etc.
/// - **Soft Deletion**: Stores are soft-deleted for audit trails and recovery capability
/// - **Domain Events**: All significant state changes raise events for integration with other contexts
/// 
/// ## Important Invariants
/// - A store name must be unique and between 1-100 characters
/// - A store code must be unique, uppercase, 1-50 characters
/// - A store URL must be unique and between 1-255 characters
/// - Currency must be one of the predefined valid currencies (USD, EUR, GBP, VND)
/// - The default store cannot be deleted without explicit override (admin action only)
/// - A store with active orders cannot be deleted
/// - Email addresses must be in valid format if provided
/// - Timezone must be a valid TimeZoneInfo identifier if provided
/// 
/// ## Concerns & Patterns
/// Implements: IHasMetadata, IHasUniqueName, IHasSeoMetadata, IAddress, IHasParameterizableName, ISoftDeletable
/// - Metadata (public/private) for extensibility
/// - Unique names for business identity
/// - SEO metadata for search engine optimization
/// - Address for store physical location
/// - Parameterizable names (Name + Presentation)
/// - Soft deletion for audit compliance
/// 
/// ## Related Aggregates & Entities
/// - StoreProduct: Links products to this store with visibility/featured settings
/// - StoreStockLocation: Links warehouse/inventory locations for fulfillment
/// - StoreShippingMethod: Links available shipping methods with store-specific costs
/// - StorePaymentMethod: Links available payment methods for checkout
/// - Order: Customer orders placed within this store
/// - Taxonomy: Product categories specific to this store
/// 
/// ## Domain Events
/// Publishes events for all significant state changes:
/// - StoreCreated, StoreUpdated, StoreDeleted, StoreRestored
/// - ProductAddedToStore, ProductRemovedFromStore, ProductSettingsUpdated
/// - StockLocationAddedToStore, StockLocationRemovedFromStore
/// - ShippingMethodAddedToStore, ShippingMethodRemovedFromStore
/// - PaymentMethodAddedToStore, PaymentMethodRemovedFromStore
/// - StoreMadeDefault, StorePasswordProtectionEnabled/Removed
/// 
/// ## Usage Examples
/// 
/// ### Create a new store
/// <code>
/// var storeResult = Store.Create(
///     name: "Fashion Outlet",
///     presentation: "Fashion Outlet Store",
///     code: "FASHION",
///     url: "fashion.example.com",
///     currency: "USD",
///     mailFromAddress: "orders@fashion.example.com",
///     customerSupportEmail: "support@fashion.example.com"
/// );
/// 
/// if (storeResult.IsError)
///     return Problem(storeResult.FirstError.Description);
/// 
/// var store = storeResult.Value;
/// dbContext.Stores.Add(store);
/// await dbContext.SaveChangesAsync(); // Events published automatically
/// </code>
/// 
/// ### Update store information
/// <code>
/// var updateResult = store.Update(
///     name: "Fashion Outlet Premium",
///     available: true,
///     metaTitle: "Shop Fashion | Premium Outlet",
///     metaDescription: "Premium fashion items at outlet prices"
/// );
/// 
/// if (updateResult.IsError)
///     return Problem(updateResult.FirstError.Description);
/// 
/// await dbContext.SaveChangesAsync();
/// </code>
/// 
/// ### Manage products in store
/// <code>
/// var addResult = store.AddProduct(product, visible: true, featured: true);
/// if (addResult.IsError)
///     return Problem(addResult.FirstError.Description);
/// 
/// var updateResult = store.UpdateProductSettings(product.Id, visible: false);
/// await dbContext.SaveChangesAsync();
/// </code>
/// 
/// ### Configure fulfillment
/// <code>
/// // Add warehouse with fulfillment priority
/// var addLocationResult = store.AddStockLocation(warehouse, priority: 1);
/// 
/// // Add shipping method for this store
/// var addShippingResult = store.AddShippingMethod(fastShipping, available: true, storeBaseCost: 15.00m);
/// 
/// // Add payment method
/// var addPaymentResult = store.AddPaymentMethod(creditCard, available: true);
/// 
/// await dbContext.SaveChangesAsync();
/// </code>
/// </summary>
public sealed class Store : Aggregate,
    IHasMetadata,
    IHasUniqueName,
    IHasSeoMetadata,
    IAddress,
    IHasParameterizableName,
    ISoftDeletable
{
    #region Constraints
    /// <summary>
    /// Business constraints and limits for store configuration.
    /// </summary>
    public static class Constraints
    {
        /// <summary>
        /// Maximum length for store name (e.g., "Fashion Outlet Premium").
        /// Used for UI display and search indexing.
        /// </summary>
        public const int NameMaxLength = 100;

        /// <summary>
        /// Maximum length for store presentation (alternative display name).
        /// Used for branding and marketing purposes.
        /// </summary>
        public const int PresentationMaxLength = 100;

        /// <summary>
        /// Maximum length for store URL (domain or path).
        /// Example: "shop.example.com" or "example.com/shop"
        /// </summary>
        public const int UrlMaxLength = 255;

        /// <summary>
        /// Maximum length for store code (uppercase identifier).
        /// Used for subdomain routing or programmatic reference.
        /// Example: "FASHION_OUTLET" = 15 chars
        /// </summary>
        public const int CodeMaxLength = 255;

        /// <summary>
        /// Maximum length for email addresses (mail-from and support).
        /// </summary>
        public const int EmailMaxLength = 255;

        /// <summary>
        /// Maximum length for password hashes (should be much longer than plain password).
        /// </summary>
        public const int PasswordMaxLength = 255;

        /// <summary>
        /// Maximum length for phone numbers.
        /// </summary>
        public const int PhoneMaxLength = 50;

        /// <summary>
        /// Maximum length for company legal name.
        /// </summary>
        public const int CompanyMaxLength = 255;

        /// <summary>
        /// Maximum length for individual address lines.
        /// </summary>
        public const int AddressMaxLength = 255;

        /// <summary>
        /// Maximum length for city/municipality names.
        /// </summary>
        public const int CityMaxLength = 100;

        /// <summary>
        /// Maximum length for postal/zip codes.
        /// </summary>
        public const int ZipcodeMaxLength = 20;

        /// <summary>
        /// Maximum length for social media profile URLs/handles.
        /// </summary>
        public const int SocialLinkMaxLength = 255;

        /// <summary>
        /// Valid currency codes for this system.
        /// Should be synchronized with payment processor and accounting systems.
        /// </summary>
        public static readonly string[] ValidCurrencies = ["USD", "EUR", "GBP", "VND"];

        /// <summary>
        /// Default currency used when store currency not specified.
        /// </summary>
        public const string DefaultCurrency = "USD";

        /// <summary>
        /// Default locale/language for store.
        /// </summary>
        public const string DefaultLocale = "en";

        /// <summary>
        /// Default timezone for store-based scheduling and reporting.
        /// </summary>
        public const string DefaultTimezone = "UTC";
    }
    #endregion

    #region Errors
    /// <summary>
    /// Domain-specific errors for store operations.
    /// </summary>
    public static class Errors
    {
        // Validation Errors - Store Properties
        /// <summary>Name is required when creating or updating store.</summary>
        public static Error NameRequired => Error.Validation("Store.NameRequired", "Store name is required.");
        /// <summary>Store name exceeds maximum length constraint.</summary>
        public static Error NameTooLong => Error.Validation("Store.NameTooLong", $"Store name must not exceed {Constraints.NameMaxLength} characters.");
        /// <summary>Presentation name exceeds maximum length constraint.</summary>
        public static Error PresentationTooLong => Error.Validation("Store.PresentationTooLong", $"Store presentation must not exceed {Constraints.PresentationMaxLength} characters.");
        /// <summary>Store code is required and must be unique.</summary>
        public static Error CodeRequired => Error.Validation("Store.CodeRequired", "Store code is required.");
        /// <summary>Store code exceeds maximum length constraint.</summary>
        public static Error CodeTooLong => Error.Validation("Store.CodeTooLong", $"Store code must not exceed {Constraints.CodeMaxLength} characters.");
        /// <summary>Store URL is required and must be unique.</summary>
        public static Error UrlRequired => Error.Validation("Store.UrlRequired", "Store URL is required.");
        /// <summary>Store URL exceeds maximum length constraint.</summary>
        public static Error UrlTooLong => Error.Validation("Store.UrlTooLong", $"Store URL must not exceed {Constraints.UrlMaxLength} characters.");
        /// <summary>Currency code is not in the list of supported currencies.</summary>
        public static Error InvalidCurrency => Error.Validation("Store.InvalidCurrency",
            $"Currency must be one of: {string.Join(", ", Constraints.ValidCurrencies)}");
        /// <summary>Email address format is invalid.</summary>
        public static Error InvalidMailFromAddress => Error.Validation("Store.InvalidMailFromAddress", "Mail-from address must be a valid email format.");
        /// <summary>Customer support email format is invalid.</summary>
        public static Error InvalidCustomerSupportEmail => Error.Validation("Store.InvalidCustomerSupportEmail", "Customer support email must be a valid email format.");
        /// <summary>Timezone is not a recognized timezone identifier.</summary>
        public static Error InvalidTimezone => Error.Validation("Store.InvalidTimezone", "Timezone must be a valid timezone identifier (e.g., 'America/New_York', 'UTC').");

        // Not Found Errors
        /// <summary>Store with specified ID does not exist.</summary>
        public static Error NotFound(Guid id) => Error.NotFound("Store.NotFound", $"Store with ID '{id}' was not found.");
        /// <summary>Store with specified code does not exist.</summary>
        public static Error NotFoundByCode(string code) => Error.NotFound("Store.NotFoundByCode", $"Store with code '{code}' was not found.");

        // Conflict/State Errors
        /// <summary>Cannot delete the default store (use MakeDefault on another store first).</summary>
        public static Error CannotDeleteDefaultStore => Error.Conflict("Store.CannotDeleteDefault", "Cannot delete the default store. Set another store as default first.");
        /// <summary>Cannot delete store with active orders. Complete or cancel orders first.</summary>
        public static Error HasActiveOrders => Error.Conflict("Store.HasActiveOrders", "Cannot delete store with active orders. Complete or cancel all orders first.");

        // Product Management Errors
        /// <summary>Product is already linked to this store.</summary>
        public static Error ProductAlreadyInStore => Error.Conflict("Store.ProductAlreadyAdded", "Product is already added to this store.");
        /// <summary>Product is not linked to this store.</summary>
        public static Error ProductNotInStore => Error.NotFound("Store.ProductNotFound", "Product is not associated with this store.");

        // Stock Location Management Errors
        /// <summary>Stock location is already linked to this store.</summary>
        public static Error StockLocationAlreadyAdded => Error.Conflict("Store.StockLocationAlreadyAdded", "Stock location already added to this store.");
        /// <summary>Stock location is not linked to this store.</summary>
        public static Error StockLocationNotFound => Error.NotFound("Store.StockLocationNotFound", "Stock location not found in store.");

        // Shipping Method Management Errors
        /// <summary>Shipping method is already linked to this store.</summary>
        public static Error ShippingMethodAlreadyAdded => Error.Conflict("Store.ShippingMethodAlreadyAdded", "Shipping method already added to this store.");
        /// <summary>Shipping method is not linked to this store.</summary>
        public static Error ShippingMethodNotFound => Error.NotFound("Store.ShippingMethodNotFound", "Shipping method not found in this store.");

        // Payment Method Management Errors
        /// <summary>Payment method is already linked to this store.</summary>
        public static Error PaymentMethodAlreadyAdded => Error.Conflict("Store.PaymentMethodAlreadyAdded", "Payment method already added to this store.");
        /// <summary>Payment method is not linked to this store.</summary>
        public static Error PaymentMethodNotFound => Error.NotFound("Store.PaymentMethodNotFound", "Payment method not found in this store.");

        // Null Reference Errors
        /// <summary>Product reference is null (cannot add null product).</summary>
        public static Error InvalidProduct => Error.Validation("Store.InvalidProduct", "Product cannot be null.");
        /// <summary>Stock location reference is null (cannot add null location).</summary>
        public static Error InvalidStockLocation => Error.Validation("Store.InvalidStockLocation", "Stock location cannot be null.");
        /// <summary>Shipping method reference is null (cannot add null method).</summary>
        public static Error InvalidShippingMethod => Error.Validation("Store.InvalidShippingMethod", "Shipping method cannot be null.");
        /// <summary>Payment method reference is null (cannot add null method).</summary>
        public static Error InvalidPaymentMethod => Error.Validation("Store.InvalidPaymentMethod", "Payment method cannot be null.");
        /// <summary>Password is required for password protection.</summary>
        public static Error InvalidPassword => Error.Validation("Store.InvalidPassword", "Password cannot be null or empty.");
    }
    #endregion

    #region Properties
    public string Name { get; set; } = string.Empty;
    public string Presentation { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public string DefaultCurrency { get; set; } = Constraints.DefaultCurrency;
    public string DefaultLocale { get; set; } = Constraints.DefaultLocale;
    public string Timezone { get; set; } = Constraints.DefaultTimezone;

    public bool Default { get; set; }

    public string? MailFromAddress { get; set; }
    public string? CustomerSupportEmail { get; set; }

    // SEO
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? SeoTitle { get; set; }

    // Availability & Security
    public bool Available { get; set; } = true;
    public bool GuestCheckoutAllowed { get; set; } = true;
    public bool PasswordProtected { get; set; }
    public string? StorefrontPassword { get; set; } // Hash in service layer

    // Social
    public string? Facebook { get; set; }
    public string? Instagram { get; set; }
    public string? Twitter { get; set; }

    // Address
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? Zipcode { get; set; }
    public string? Phone { get; set; }
    public string? Company { get; set; }

    // Location
    public Guid? CountryId { get; set; }
    public Guid? StateId { get; set; }

    // Soft Delete
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    // Metadata
    public IDictionary<string, object?>? PublicMetadata { get; set; } = new Dictionary<string, object?>();
    public IDictionary<string, object?>? PrivateMetadata { get; set; } = new Dictionary<string, object?>();
    #endregion

    #region Navigation Properties
    public Country? Country { get; set; }
    public State? State { get; set; }

    public ICollection<StoreProduct> StoreProducts { get; set; } = new List<StoreProduct>();
    public ICollection<Taxonomy> Taxonomies { get; set; } = new List<Taxonomy>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<StoreStockLocation> StoreStockLocations { get; set; } = new List<StoreStockLocation>();
    public ICollection<StoreShippingMethod> StoreShippingMethods { get; set; } = new List<StoreShippingMethod>();
    public ICollection<StorePaymentMethod> StorePaymentMethods { get; set; } = new List<StorePaymentMethod>();
    #endregion

    #region Computed Properties
    /// <summary>
    /// Gets all products associated with this store.
    /// </summary>
    public IReadOnlyCollection<Product> Products =>
        StoreProducts.Select(sp => sp.Product).ToList().AsReadOnly();

    /// <summary>
    /// Gets all stock locations linked to this store.
    /// </summary>
    public IReadOnlyCollection<StockLocation> StockLocations =>
        StoreStockLocations
            .OrderBy(sl => sl.Priority)
            .Select(sl => sl.StockLocation)
            .ToList()
            .AsReadOnly();

    /// <summary>
    /// Gets the primary stock location (highest priority).
    /// </summary>
    public StockLocation? PrimaryStockLocation =>
        StoreStockLocations
            .OrderBy(sl => sl.Priority)
            .FirstOrDefault()?.StockLocation;

    /// <summary>
    /// Gets available shipping methods for this store.
    /// </summary>
    public IReadOnlyCollection<ShippingMethod> AvailableShippingMethods =>
        StoreShippingMethods
            .Where(sm => sm.Available && sm.ShippingMethod != null)
            .Select(sm => sm.ShippingMethod!)
            .ToList()
            .AsReadOnly();

    /// <summary>
    /// Gets available payment methods for this store.
    /// </summary>
    public IReadOnlyCollection<PaymentMethod> AvailablePaymentMethods =>
        StorePaymentMethods
            .Where(pm => pm.Available && pm.PaymentMethod != null)
            .Select(pm => pm.PaymentMethod!)
            .ToList()
            .AsReadOnly();

    /// <summary>
    /// Gets the count of active orders.
    /// </summary>
    public int ActiveOrderCount =>
        Orders.Count(o => !o.IsComplete && !o.IsCanceled);

    /// <summary>
    /// Gets the count of completed orders.
    /// </summary>
    public int CompletedOrderCount =>
        Orders.Count(o => o.IsComplete);

    /// <summary>
    /// Gets the count of visible products in this store.
    /// </summary>
    public int VisibleProductCount =>
        StoreProducts.Count(sp => sp.Visible);

    /// <summary>
    /// Indicates if the store is fully configured and ready for business.
    /// </summary>
    public bool IsConfigured =>
        Available &&
        StoreStockLocations.Any() &&
        StoreShippingMethods.Any(sm => sm.Available) &&
        StorePaymentMethods.Any(pm => pm.Available);
    #endregion

    #region Constructors
    private Store() { } // EF Core
    #endregion

    #region Factory
    /// <summary>
    /// Creates a new store instance with validation.
    /// </summary>
    /// <remarks>
    /// Pre-conditions:
    /// - name must not be null/empty (1-100 characters)
    /// - If code is provided, must be 1-50 characters (auto-generated from name if not provided)
    /// - If url is provided, must be 1-255 characters (auto-generated from name if not provided)
    /// - currency must be one of: USD, EUR, GBP, VND (defaults to USD)
    /// - If provided, mailFromAddress and customerSupportEmail must be valid email format
    /// - If provided, timezone must be a valid timezone identifier
    /// - presentation defaults to name if not provided
    /// - locale defaults to 'en' if not provided
    /// - timezone defaults to 'UTC' if not provided
    /// 
    /// Post-conditions:
    /// - Store is created with StoreCreated event
    /// - Store.Available = true by default
    /// - Store.GuestCheckoutAllowed = true by default
    /// - Store.PasswordProtected = false by default
    /// - CreatedAt is set to UtcNow
    /// - IsDeleted is false
    /// 
    /// Side Effects:
    /// - Domain event StoreCreated is added (published on SaveChangesAsync)
    /// 
    /// Example:
    /// <code>
    /// var result = Store.Create(
    ///     name: "Main Store",
    ///     code: "MAIN",
    ///     url: "shop.example.com",
    ///     currency: "USD",
    ///     mailFromAddress: "orders@example.com",
    ///     customerSupportEmail: "support@example.com"
    /// );
    /// if (result.IsError) return Problem(result.FirstError.Description);
    /// var store = result.Value;
    /// </code>
    /// </remarks>
    public static ErrorOr<Store> Create(
        string name,
        string? presentation = null,
        string? code = null,
        string? url = null,
        string? currency = null,
        string? locale = null,
        string? timezone = null,
        string? mailFromAddress = null,
        string? customerSupportEmail = null,
        bool isDefault = false,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        // Normalize parameterized names (Name + Presentation)
        (name, presentation) = HasParameterizableName.NormalizeParams(name, presentation);

        // Generate code from name if not provided (e.g., "Fashion Store" -> "FASHION_STORE")
        code ??= GenerateStoreCode(name);

        // Generate URL from name if not provided (e.g., "Fashion Store" -> "fashion-store")
        url ??= name.ToLowerInvariant().Replace(" ", "-");

        // Perform comprehensive validation
        var errors = Validate(name, presentation, code, url, currency, timezone, mailFromAddress, customerSupportEmail);
        if (errors.Any()) return errors;

        // Create and initialize store aggregate
        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Presentation = presentation.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            Url = url.Trim().ToLowerInvariant(),
            DefaultCurrency = (currency ?? Constraints.DefaultCurrency).ToUpperInvariant(),
            DefaultLocale = locale ?? Constraints.DefaultLocale,
            Timezone = timezone ?? Constraints.DefaultTimezone,
            Default = isDefault,
            MailFromAddress = mailFromAddress?.Trim(),
            CustomerSupportEmail = customerSupportEmail?.Trim(),
            Available = true,
            GuestCheckoutAllowed = true,
            PasswordProtected = false,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.UtcNow,
            PrivateMetadata = privateMetadata ?? new Dictionary<string, object?>(),
            PublicMetadata = publicMetadata ?? new Dictionary<string, object?>(),
        };

        // Publish domain event for integration with other contexts
        store.AddDomainEvent(new Events.StoreCreated(store.Id, store.Name, store.Code));
        return store;
    }

    /// <summary>
    /// Generates a URL-safe store code from the store name.
    /// Converts to uppercase and replaces spaces with underscores, then truncates if needed.
    /// Example: "Fashion Store" -> "FASHION_STORE", "A Very Long Store Name" -> "A_VERY_LONG_"
    /// </summary>
    private static string GenerateStoreCode(string name)
    {
        var codeChars = name
            .ToUpperInvariant()
            .Replace(" ", "_")
            .Replace("-", "_")
            .Take(Constraints.CodeMaxLength);
        
        return new string(codeChars.ToArray());
    }

    /// <summary>
    /// Validates all store properties and returns any validation errors found.
    /// </summary>
    /// <returns>List of validation errors (empty if all validations pass)</returns>
    private static List<Error> Validate(
        string name,
        string presentation,
        string code,
        string url,
        string? currency,
        string? timezone,
        string? mailFromAddress,
        string? customerSupportEmail)
    {
        var errors = new List<Error>();

        // Name validation
        if (string.IsNullOrWhiteSpace(name))
            errors.Add(Errors.NameRequired);
        else if (name.Length > Constraints.NameMaxLength)
            errors.Add(Errors.NameTooLong);

        // Presentation validation
        if (presentation.Length > Constraints.PresentationMaxLength)
            errors.Add(Errors.PresentationTooLong);

        // Code validation
        if (string.IsNullOrWhiteSpace(code))
            errors.Add(Errors.CodeRequired);
        else if (code.Length > Constraints.CodeMaxLength)
            errors.Add(Errors.CodeTooLong);

        // URL validation
        if (string.IsNullOrWhiteSpace(url))
            errors.Add(Errors.UrlRequired);
        else if (url.Length > Constraints.UrlMaxLength)
            errors.Add(Errors.UrlTooLong);

        // Currency validation (must be one of the predefined valid currencies)
        if (currency is not null && !Constraints.ValidCurrencies.Contains(currency.ToUpperInvariant()))
            errors.Add(Errors.InvalidCurrency);

        // Timezone validation (verify it's a recognized timezone)
        if (!string.IsNullOrWhiteSpace(timezone) && !IsValidTimezone(timezone))
            errors.Add(Errors.InvalidTimezone);

        // Email validation for mail-from address
        if (!string.IsNullOrWhiteSpace(mailFromAddress) && !IsValidEmail(mailFromAddress))
            errors.Add(Errors.InvalidMailFromAddress);

        // Email validation for customer support address
        if (!string.IsNullOrWhiteSpace(customerSupportEmail) && !IsValidEmail(customerSupportEmail))
            errors.Add(Errors.InvalidCustomerSupportEmail);

        return errors;
    }

    /// <summary>
    /// Checks if a string is a valid email address format (basic validation).
    /// </summary>
    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if a string is a valid .NET timezone identifier.
    /// Examples: "UTC", "America/New_York", "Europe/London"
    /// </summary>
    private static bool IsValidTimezone(string? timezone)
    {
        if (string.IsNullOrWhiteSpace(timezone))
            return false;

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timezone);
            return true;
        }
        catch
        {
            return false;
        }
    }
    #endregion

    #region Business Logic - Core Updates

    /// <summary>
    /// Updates store configuration with provided values.
    /// Only parameters provided (not null) will be updated; others remain unchanged.
    /// </summary>
    /// <remarks>
    /// Pre-conditions:
    /// - Store must not be deleted (IsDeleted = false)
    /// - If name provided, must be 1-100 characters
    /// - If currency provided, must be one of: USD, EUR, GBP, VND
    /// - If timezone provided, must be valid timezone identifier
    /// 
    /// Post-conditions:
    /// - UpdatedAt timestamp is set to UtcNow
    /// - StoreUpdated domain event is published (if any changes made)
    /// - All properties that changed are tracked
    /// 
    /// Side Effects:
    /// - Updates aggregate version (via Aggregate base class)
    /// - Publishes StoreUpdated event if any property changed
    /// - No event published if no changes detected
    /// 
    /// Example:
    /// <code>
    /// var result = store.Update(
    ///     available: false,  // Close store for maintenance
    ///     metaTitle: "New SEO Title"
    /// );
    /// if (result.IsError) return Problem(result.FirstError.Description);
    /// await dbContext.SaveChangesAsync();
    /// </code>
    /// </remarks>
    /// <param name="name">New store name (optional)</param>
    /// <param name="presentation">New presentation name (optional)</param>
    /// <param name="url">New store URL (optional)</param>
    /// <param name="mailFromAddress">New mail-from email (optional)</param>
    /// <param name="customerSupportEmail">New support email (optional)</param>
    /// <param name="metaTitle">New SEO meta title (optional)</param>
    /// <param name="metaDescription">New SEO meta description (optional)</param>
    /// <param name="metaKeywords">New SEO keywords (optional)</param>
    /// <param name="seoTitle">New SEO title (optional)</param>
    /// <param name="available">New availability status (optional)</param>
    /// <param name="guestCheckoutAllowed">Allow guest checkout (optional)</param>
    /// <param name="timezone">New timezone (optional)</param>
    /// <param name="defaultLocale">New default locale (optional)</param>
    /// <param name="defaultCurrency">New default currency (optional)</param>
    /// <param name="publicMetadata">New public metadata (optional)</param>
    /// <param name="privateMetadata">New private metadata (optional)</param>
    /// <returns>Updated store or validation error</returns>
    public ErrorOr<Store> Update(
        string? name = null,
        string? presentation = null,
        string? url = null,
        string? mailFromAddress = null,
        string? customerSupportEmail = null,
        string? metaTitle = null,
        string? metaDescription = null,
        string? metaKeywords = null,
        string? seoTitle = null,
        bool? available = null,
        bool? guestCheckoutAllowed = null,
        string? timezone = null,
        string? defaultLocale = null,
        string? defaultCurrency = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        bool changed = false; // Added this line
        // Create a list to collect errors
        var errors = new List<Error>();

        (name, presentation) = HasParameterizableName.NormalizeParams(name, presentation);

        if (!string.IsNullOrWhiteSpace(name) && name.Trim() != Name)
        {
            if (name.Length > Constraints.NameMaxLength) errors.Add(Errors.NameTooLong);
            else Name = name.Trim();
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(presentation) && presentation.Trim() != Presentation)
        {
            if (presentation.Length > Constraints.PresentationMaxLength) errors.Add(Errors.PresentationTooLong);
            else Presentation = presentation.Trim();
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(url) && url.Trim().ToLowerInvariant() != Url)
        {
            if (url.Length > Constraints.UrlMaxLength) errors.Add(Errors.UrlTooLong);
            else Url = url.Trim().ToLowerInvariant();
            changed = true;
        }

        if (mailFromAddress is not null && mailFromAddress.Trim() != MailFromAddress)
        {
            MailFromAddress = mailFromAddress.Trim();
            changed = true;
        }

        if (customerSupportEmail is not null && customerSupportEmail.Trim() != CustomerSupportEmail)
        {
            CustomerSupportEmail = customerSupportEmail.Trim();
            changed = true;
        }

        if (metaTitle is not null && metaTitle.Trim() != MetaTitle)
        {
            MetaTitle = metaTitle.Trim();
            changed = true;
        }

        if (metaDescription is not null && metaDescription.Trim() != MetaDescription)
        {
            MetaDescription = metaDescription.Trim();
            changed = true;
        }

        if (metaKeywords is not null && metaKeywords.Trim() != MetaKeywords)
        {
            MetaKeywords = metaKeywords.Trim();
            changed = true;
        }

        if (seoTitle is not null && seoTitle.Trim() != SeoTitle)
        {
            SeoTitle = seoTitle.Trim();
            changed = true;
        }

        if (available.HasValue && available != Available)
        {
            Available = available.Value;
            changed = true;
        }

        if (guestCheckoutAllowed.HasValue && guestCheckoutAllowed != GuestCheckoutAllowed)
        {
            GuestCheckoutAllowed = guestCheckoutAllowed.Value;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(timezone) && timezone != Timezone)
        {
            Timezone = timezone;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(defaultLocale) && defaultLocale != DefaultLocale)
        {
            DefaultLocale = defaultLocale;
            changed = true;
        }

        // --- Currency validation fix ---
        if (!string.IsNullOrWhiteSpace(defaultCurrency))
        {
            if (!Constraints.ValidCurrencies.Contains(defaultCurrency.ToUpperInvariant()))
            {
                errors.Add(Errors.InvalidCurrency);
            }
            else if (defaultCurrency.ToUpperInvariant() != DefaultCurrency)
            {
                DefaultCurrency = defaultCurrency.ToUpperInvariant();
                changed = true;
            }
        }
        // --- End Currency validation fix ---

        if (publicMetadata is not null && !PublicMetadata.MetadataEquals(publicMetadata))
        {
            PublicMetadata = new Dictionary<string, object?>(publicMetadata);
            changed = true;
        }

        if (privateMetadata is not null && !PrivateMetadata.MetadataEquals(privateMetadata))
        {
            PrivateMetadata = new Dictionary<string, object?>(privateMetadata);
            changed = true;
        }
        
        if (errors.Any()) return errors;

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(new Events.StoreUpdated(Id));
        }

        return this;
    }

    /// <summary>
    /// Updates store physical address information.
    /// Only parameters provided (not null) will be updated; others remain unchanged.
    /// </summary>
    /// <remarks>
    /// Implements IAddress interface for consistent store location management.
    /// Used to set the store's physical location for display on checkout, contact pages, etc.
    /// 
    /// Pre-conditions:
    /// - None (all fields are optional)
    /// 
    /// Post-conditions:
    /// - Address fields are updated
    /// - UpdatedAt is set to UtcNow
    /// - StoreAddressUpdated event published if any changes made
    /// 
    /// Side Effects:
    /// - Publishes StoreAddressUpdated domain event (if changes detected)
    /// 
    /// Example:
    /// <code>
    /// var result = store.SetAddress(
    ///     address1: "123 Main Street",
    ///     city: "New York",
    ///     zipcode: "10001",
    ///     countryId: usCountryId,
    ///     phone: "+1-800-555-0123"
    /// );
    /// await dbContext.SaveChangesAsync();
    /// </code>
    /// </remarks>
    public ErrorOr<Store> SetAddress(
        string? address1 = null,
        string? address2 = null,
        string? city = null,
        string? zipcode = null,
        string? phone = null,
        string? company = null,
        Guid? countryId = null,
        Guid? stateId = null)
    {
        bool changed = false;

        if (address1 != Address1) { Address1 = address1?.Trim(); changed = true; }
        if (address2 != Address2) { Address2 = address2?.Trim(); changed = true; }
        if (city != City) { City = city?.Trim(); changed = true; }
        if (zipcode != Zipcode) { Zipcode = zipcode?.Trim(); changed = true; }
        if (phone != Phone) { Phone = phone?.Trim(); changed = true; }
        if (company != Company) { Company = company?.Trim(); changed = true; }
        if (countryId != CountryId) { CountryId = countryId; changed = true; }
        if (stateId != StateId) { StateId = stateId; changed = true; }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(new Events.StoreAddressUpdated(Id));
        }

        return this;
    }

    /// <summary>
    /// Updates store social media profiles and links.
    /// Only parameters provided (not null) will be updated; others remain unchanged.
    /// </summary>
    /// <remarks>
    /// Used to manage social media presence for store branding and customer engagement.
    /// 
    /// Example URLs:
    /// - Facebook: "https://facebook.com/yourstore"
    /// - Instagram: "@yourstore" or "https://instagram.com/yourstore"
    /// - Twitter: "@yourstore" or "https://twitter.com/yourstore"
    /// 
    /// Post-conditions:
    /// - Social profile URLs updated
    /// - UpdatedAt set to UtcNow
    /// - StoreSocialLinksUpdated event published if changes made
    /// 
    /// Example:
    /// <code>
    /// var result = store.SetSocialLinks(
    ///     facebook: "https://facebook.com/fashionstore",
    ///     instagram: "@fashionstore",
    ///     twitter: "@fashionstore"
    /// );
    /// await dbContext.SaveChangesAsync();
    /// </code>
    /// </remarks>
    public ErrorOr<Store> SetSocialLinks(
        string? facebook = null,
        string? instagram = null,
        string? twitter = null)
    {
        bool changed = false;

        if (facebook != Facebook) { Facebook = facebook?.Trim(); changed = true; }
        if (instagram != Instagram) { Instagram = instagram?.Trim(); changed = true; }
        if (twitter != Twitter) { Twitter = twitter?.Trim(); changed = true; }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(new Events.StoreSocialLinksUpdated(Id));
        }

        return this;
    }

    /// <summary>
    /// Marks this store as the default store for the system.
    /// Only one store should be marked as default.
    /// </summary>
    /// <remarks>
    /// Pre-conditions:
    /// - Other store's Default flag should be cleared first (application layer responsibility)
    /// 
    /// Post-conditions:
    /// - Store.Default = true
    /// - UpdatedAt set to UtcNow
    /// - StoreMadeDefault event published
    /// 
    /// Side Effects:
    /// - Publishes StoreMadeDefault event for system notification
    /// - This store will be used as fallback/default for system operations
    /// 
    /// Business Impact:
    /// - Cannot be deleted without override (See Delete method)
    /// - May affect order routing, email generation, and other system defaults
    /// 
    /// Example:
    /// <code>
    /// // Make this store the default
    /// var result = newStore.MakeDefault();
    /// 
    /// // In application layer, should clear previous default
    /// previousDefault.Default = false;
    /// 
    /// await dbContext.SaveChangesAsync();
    /// </code>
    /// </remarks>
    /// <returns>Updated store with Default flag set to true</returns>
    public ErrorOr<Store> MakeDefault()
    {
        if (Default) return this;

        Default = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.StoreMadeDefault(Id));
        return this;
    }
    
    /// <summary>
    /// Protects the storefront with a password (for private/beta stores).
    /// Password should be hashed in the application layer before persisting.
    /// </summary>
    /// <remarks>
    /// Pre-conditions:
    /// - password must not be null or empty
    /// - password should be pre-hashed by application layer
    /// - password should be bcrypt or similar strong hash
    /// 
    /// Post-conditions:
    /// - PasswordProtected = true
    /// - StorefrontPassword is set to the (hashed) password
    /// - UpdatedAt set to UtcNow
    /// - StorePasswordProtectionEnabled event published
    /// 
    /// Side Effects:
    /// - Customers will need password to access store
    /// - Publishes StorePasswordProtectionEnabled event
    /// - Application layer should handle password verification on checkout
    /// 
    /// Security Notes:
    /// - Never store plain text passwords
    /// - Use bcrypt, Argon2, or scrypt for hashing
    /// - Application layer should validate and hash password
    /// - Consider 2FA for production use
    /// 
    /// Example:
    /// <code>
    /// // In application/command layer:
    /// var hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword, 10);
    /// var result = store.ProtectWithPassword(hashedPassword);
    /// if (result.IsError) return Problem(result.FirstError.Description);
    /// await dbContext.SaveChangesAsync();
    /// </code>
    /// </remarks>
    /// <param name="password">The hashed password to protect the store</param>
    /// <returns>Updated store with password protection enabled</returns>
    public ErrorOr<Store> ProtectWithPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return Errors.InvalidPassword;

        PasswordProtected = true;
        StorefrontPassword = password; // Assumed to be hashed by application layer
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.StorePasswordProtectionEnabled(Id));
        return this;
    }

    /// <summary>
    /// Removes password protection from the storefront (makes it public).
    /// </summary>
    /// <remarks>
    /// Post-conditions:
    /// - PasswordProtected = false
    /// - StorefrontPassword = null
    /// - UpdatedAt set to UtcNow
    /// - StorePasswordProtectionRemoved event published
    /// 
    /// Side Effects:
    /// - Store becomes publicly accessible without password
    /// - Publishes StorePasswordProtectionRemoved event
    /// 
    /// Example:
    /// <code>
    /// var result = store.RemovePasswordProtection();
    /// await dbContext.SaveChangesAsync();
    /// </code>
    /// </remarks>
    /// <returns>Updated store with password protection removed</returns>
    public ErrorOr<Store> RemovePasswordProtection()
    {
        if (!PasswordProtected) return this;

        PasswordProtected = false;
        StorefrontPassword = null;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.StorePasswordProtectionRemoved(Id));
        return this;
    }

    #endregion

    #region Business Logic - Product Management

    public ErrorOr<Store> AddProduct(
        Product? product,
        bool visible = true,
        bool featured = false)
    {
        if (product is null) return Errors.InvalidProduct;
        if (StoreProducts.Any(sp => sp.ProductId == product.Id))
            return Errors.ProductAlreadyInStore;

        var link = StoreProduct.Create(
            Id,
            product.Id,
            visible,
            featured);

        if (link.IsError) return link.FirstError;

        StoreProducts.Add(link.Value);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.ProductAddedToStore(Id, product.Id));
        return this;
    }

    public ErrorOr<Store> RemoveProduct(Guid productId)
    {
        var link = StoreProducts.FirstOrDefault(sp => sp.ProductId == productId);
        if (link is null) return Errors.ProductNotInStore;

        StoreProducts.Remove(link);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.ProductRemovedFromStore(Id, productId));
        return this;
    }

    public ErrorOr<Store> UpdateProductSettings(
        Guid productId,
        bool? visible = null,
        bool? featured = null)
    {
        var link = StoreProducts.FirstOrDefault(sp => sp.ProductId == productId);
        if (link is null) return Errors.ProductNotInStore;

        var initialVisible = link.Visible;
        var initialFeatured = link.Featured;

        var updateResult = link.Update(visible, featured); // This updates link's properties
        if (updateResult.IsError) return updateResult.FirstError;

        // Only raise event if actual settings changed
        if (link.Visible != initialVisible || link.Featured != initialFeatured)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(new Events.ProductSettingsUpdated(Id, productId));
        }
        return this;
    }

    #endregion

    #region Business Logic - Stock Location Management

    public ErrorOr<Store> AddStockLocation(
        StockLocation? location,
        int priority = 1)
    {
        if (location is null) return Errors.InvalidStockLocation;
        if (StoreStockLocations.Any(x => x.StockLocationId == location.Id))
            return Errors.StockLocationAlreadyAdded;

        var link = StoreStockLocation.Create(
            location.Id,
            Id,
            priority);

        if (link.IsError) return link.FirstError;

        StoreStockLocations.Add(link.Value);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.StockLocationAddedToStore(Id, location.Id, priority));
        return this;
    }

    public ErrorOr<Store> RemoveStockLocation(Guid stockLocationId)
    {
        var link = StoreStockLocations.FirstOrDefault(sl => sl.StockLocationId == stockLocationId);
        if (link is null) return Errors.StockLocationNotFound;

        StoreStockLocations.Remove(link);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.StockLocationRemovedFromStore(Id, stockLocationId));
        return this;
    }

    public ErrorOr<Store> UpdateStockLocationPriority(
        Guid stockLocationId,
        int priority)
    {
        var link = StoreStockLocations.FirstOrDefault(sl => sl.StockLocationId == stockLocationId);
        if (link is null) return Errors.StockLocationNotFound;

        var initialPriority = link.Priority;

        var updateResult = link.UpdatePriority(priority);
        if (updateResult.IsError) return updateResult.FirstError;

        // Only raise event if actual settings changed
        if (link.Priority != initialPriority)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(new Events.StockLocationPriorityUpdated(Id, stockLocationId, priority));
        }
        return this;
    }

    #endregion

    #region Business Logic - Shipping Method Management

    public ErrorOr<Store> AddShippingMethod(
        ShippingMethod? method,
        bool available = true,
        decimal? storeBaseCost = null)
    {
        if (method is null) return Errors.InvalidShippingMethod;
        if (StoreShippingMethods.Any(x => x.ShippingMethodId == method.Id))
            return Errors.ShippingMethodAlreadyAdded;

        var link = StoreShippingMethod.Create(
            Id,
            method.Id,
            available,
            storeBaseCost);

        if (link.IsError) return link.FirstError;

        StoreShippingMethods.Add(link.Value);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.ShippingMethodAddedToStore(Id, method.Id));
        return this;
    }

    public ErrorOr<Store> RemoveShippingMethod(Guid shippingMethodId)
    {
        var link = StoreShippingMethods.FirstOrDefault(sm => sm.ShippingMethodId == shippingMethodId);
        if (link is null) return Errors.ShippingMethodNotFound;

        StoreShippingMethods.Remove(link);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.ShippingMethodRemovedFromStore(Id, shippingMethodId));
        return this;
    }

    public ErrorOr<Store> UpdateShippingMethodSettings(
        Guid shippingMethodId,
        bool? available = null,
        decimal? storeBaseCost = null)
    {
        var link = StoreShippingMethods.FirstOrDefault(sm => sm.ShippingMethodId == shippingMethodId);
        if (link is null) return Errors.ShippingMethodNotFound;

        var initialAvailable = link.Available;
        var initialStoreBaseCost = link.StoreBaseCost;

        var updateResult = link.Update(available, storeBaseCost);
        if (updateResult.IsError) return updateResult.FirstError;

        // Only raise event if actual settings changed
        if (link.Available != initialAvailable || link.StoreBaseCost != initialStoreBaseCost)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(new Events.ShippingMethodSettingsUpdated(Id, shippingMethodId));
        }
        return this;
    }

    #endregion

    #region Business Logic - Payment Method Management

    public ErrorOr<Store> AddPaymentMethod(
        PaymentMethod? method,
        bool available = true)
    {
        if (method is null) return Errors.InvalidPaymentMethod;
        if (StorePaymentMethods.Any(x => x.PaymentMethodId == method.Id))
            return Errors.PaymentMethodAlreadyAdded;

        var link = StorePaymentMethod.Create(
            Id,
            method.Id,
            available);

        if (link.IsError) return link.FirstError;

        StorePaymentMethods.Add(link.Value);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.PaymentMethodAddedToStore(Id, method.Id));
        return this;
    }

    public ErrorOr<Store> RemovePaymentMethod(Guid paymentMethodId)
    {
        var link = StorePaymentMethods.FirstOrDefault(pm => pm.PaymentMethodId == paymentMethodId);
        if (link is null) return Errors.PaymentMethodNotFound;

        StorePaymentMethods.Remove(link);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.PaymentMethodRemovedFromStore(Id, paymentMethodId));
        return this;
    }
    #endregion

    #region Business Logic - Deletion

    public ErrorOr<Deleted> Delete(bool force = false)
    {
        if (Default && !force) return Errors.CannotDeleteDefaultStore;
        if (!force && Orders.Any(o => !o.IsComplete && !o.IsCanceled))
            return Errors.HasActiveOrders;
        if (IsDeleted) return Result.Deleted;

        DeletedAt = DateTimeOffset.UtcNow;
        IsDeleted = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.StoreDeleted(Id));
        return Result.Deleted;
    }

    public ErrorOr<Store> Restore()
    {
        if (!IsDeleted) return this;

        DeletedAt = null;
        DeletedBy = null;
        IsDeleted = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.StoreRestored(Id));
        return this;
    }

    #endregion

    #region Domain Events
    public static class Events
    {
        /// <summary>
        /// Raised when a new store is created.
        /// </summary>
        public sealed record StoreCreated(Guid StoreId, string Name, string Code) : DomainEvent;

        /// <summary>
        /// Raised when store core properties are updated.
        /// </summary>
        public sealed record StoreUpdated(Guid StoreId) : DomainEvent;

        /// <summary>
        /// Raised when a store is set as the default store.
        /// </summary>
        public sealed record StoreMadeDefault(Guid StoreId) : DomainEvent;

        /// <summary>
        /// Raised when a store is soft-deleted.
        /// </summary>
        public sealed record StoreDeleted(Guid StoreId) : DomainEvent;

        /// <summary>
        /// Raised when a soft-deleted store is restored.
        /// </summary>
        public sealed record StoreRestored(Guid StoreId) : DomainEvent;

        /// <summary>
        /// Raised when store address information is updated.
        /// </summary>
        public sealed record StoreAddressUpdated(Guid StoreId) : DomainEvent;

        /// <summary>
        /// Raised when store social media links are updated.
        /// </summary>
        public sealed record StoreSocialLinksUpdated(Guid StoreId) : DomainEvent;

        /// <summary>
        /// Raised when password protection is enabled.
        /// </summary>
        public sealed record StorePasswordProtectionEnabled(Guid StoreId) : DomainEvent;

        /// <summary>
        /// Raised when password protection is removed.
        /// </summary>
        public sealed record StorePasswordProtectionRemoved(Guid StoreId) : DomainEvent;

        // Product Events
        /// <summary>
        /// Raised when a product is added to the store.
        /// </summary>
        public sealed record ProductAddedToStore(Guid StoreId, Guid ProductId) : DomainEvent;

        /// <summary>
        /// Raised when a product is removed from the store.
        /// </summary>
        public sealed record ProductRemovedFromStore(Guid StoreId, Guid ProductId) : DomainEvent;

        /// <summary>
        /// Raised when product visibility or featured settings are updated.
        /// </summary>
        public sealed record ProductSettingsUpdated(Guid StoreId, Guid ProductId) : DomainEvent;

        // Stock Location Events
        /// <summary>
        /// Raised when a stock location is linked to the store.
        /// </summary>
        public sealed record StockLocationAddedToStore(Guid StoreId, Guid StockLocationId, int Priority) : DomainEvent;

        /// <summary>
        /// Raised when a stock location is unlinked from the store.
        /// </summary>
        public sealed record StockLocationRemovedFromStore(Guid StoreId, Guid StockLocationId) : DomainEvent;

        /// <summary>
        /// Raised when stock location priority is updated.
        /// </summary>
        public sealed record StockLocationPriorityUpdated(Guid StoreId, Guid StockLocationId, int Priority) : DomainEvent;

        // Shipping Method Events
        /// <summary>
        /// Raised when a shipping method is added to the store.
        /// </summary>
        public sealed record ShippingMethodAddedToStore(Guid StoreId, Guid ShippingMethodId) : DomainEvent;

        /// <summary>
        /// Raised when a shipping method is removed from the store.
        /// </summary>
        public sealed record ShippingMethodRemovedFromStore(Guid StoreId, Guid ShippingMethodId) : DomainEvent;

        /// <summary>
        /// Raised when shipping method settings are updated.
        /// </summary>
        public sealed record ShippingMethodSettingsUpdated(Guid StoreId, Guid ShippingMethodId) : DomainEvent;

        // Payment Method Events
        /// <summary>
        /// Raised when a payment method is added to the store.
        /// </summary>
        public sealed record PaymentMethodAddedToStore(Guid StoreId, Guid PaymentMethodId) : DomainEvent;

        /// <summary>
        /// Raised when a payment method is removed from the store.
        /// </summary>
        public sealed record PaymentMethodRemovedFromStore(Guid StoreId, Guid PaymentMethodId) : DomainEvent;

        /// <summary>
        /// Raised when payment method settings are updated.
        /// </summary>
        public sealed record PaymentMethodSettingsUpdated(Guid StoreId, Guid PaymentMethodId) : DomainEvent;
    }
    #endregion
}