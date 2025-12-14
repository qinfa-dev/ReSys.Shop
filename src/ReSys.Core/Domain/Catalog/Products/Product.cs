using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Common.Extensions;
using ReSys.Core.Domain.Catalog.OptionTypes;
using ReSys.Core.Domain.Catalog.Products.Classifications;
using ReSys.Core.Domain.Catalog.Products.Images;
using ReSys.Core.Domain.Catalog.Products.OptionTypes;
using ReSys.Core.Domain.Catalog.Products.PropertyTypes;
using ReSys.Core.Domain.Catalog.Products.Reviews;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Catalog.PropertyTypes;
using ReSys.Core.Domain.Catalog.Taxonomies;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;
using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Stores;
using ReSys.Core.Domain.Stores.Products;

namespace ReSys.Core.Domain.Catalog.Products;

/// <summary>
/// Represents a product in the catalog system serving as the aggregate root for product-related operations.
/// Each product can have multiple variants with different options, manages images, properties, categories, and multi-store availability.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Catalog Domain:</strong>
/// The Product aggregate is the central entity orchestrating all product-related data:
/// <list type="bullet">
/// <item>
/// <term>Aggregate Root</term>
/// <description>Controls access to variants, images, options, properties, classifications, and store availability</description>
/// </item>
/// <item>
/// <term>Lifecycle Management</term>
/// <description>Transitions between Draft, Active, and Archived states with automatic date-based transitions</description>
/// </item>
/// <item>
/// <term>Variant Management</term>
/// <description>Each product must have a master variant; non-master variants support option combinations</description>
/// </item>
/// <item>
/// <term>Multi-Dimensional Discovery</term>
/// <description>Products discoverable by categories (taxons), properties, option types, and stores</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Product Status Lifecycle:</strong>
/// <list type="bullet">
/// <item>
/// <term>Draft</term>
/// <description>Initial state; product not visible on storefront, can be edited freely</description>
/// </item>
/// <item>
/// <term>Active</term>
/// <description>Product visible on storefront, available for purchase (if variants are purchasable)</description>
/// </item>
/// <item>
/// <term>Archived</term>
/// <description>Product no longer available, but order history retained (soft-deleted)</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Invariants:</strong>
/// <list type="bullet">
/// <item>A product MUST always have exactly one master variant (IsMaster = true)</item>
/// <item>Master variant CANNOT have option values (single default variant)</item>
/// <item>Master variant CANNOT be deleted (must exist for every product)</item>
/// <item>Products with completed orders CANNOT be deleted (soft delete only)</item>
/// <item>DiscontinueOn date CANNOT be before MakeActiveAt date</item>
/// <item>Name and Slug are required and unique within the catalog</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Pricing Strategy:</strong>
/// Prices are captured per-variant and per-currency (multi-currency support):
/// <list type="bullet">
/// <item>
/// <term>Master Variant</term>
/// <description>Typically has default pricing used when no specific variant selected</description>
/// </item>
/// <item>
/// <term>Non-Master Variants</term>
/// <description>Can have different prices for each currency, representing different product configurations</description>
/// </item>
/// <item>
/// <term>Price Capture</term>
/// <description>Prices captured at order time (stored in LineItem), not dynamic from variant prices</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Inventory Considerations:</strong>
/// <list type="bullet">
/// <item>Each variant tracks inventory independently across multiple stock locations</item>
/// <item>Master variant inventory often used as default/aggregate inventory for the product</item>
/// <item>Physical products: TrackInventory = true, Purchasable based on available stock</item>
/// <item>Digital products: TrackInventory = false, always purchasable (infinite inventory)</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Domain Events Raised:</strong>
/// Products raise domain events to signal significant state changes for event subscribers:
/// <list type="bullet">
/// <item><strong>Created</strong> - Raised when product is created with initial master variant</item>
/// <item><strong>ProductUpdated</strong> - Raised when core product properties are updated</item>
/// <item><strong>VariantAdded</strong> - Raised when a non-master variant is added</item>
/// <item><strong>VariantRemoved</strong> - Raised when a variant is deleted</item>
/// <item><strong>ImageAdded</strong> - Raised when product image added</item>
/// <item><strong>ImageRemoved</strong> - Raised when product image removed</item>
/// <item><strong>OptionTypeAdded</strong> - Raised when option type linked to product</item>
/// <item><strong>PropertyAdded</strong> - Raised when property linked to product</item>
/// <item><strong>ClassificationAdded</strong> - Raised when product classified in a taxon</item>
/// <item><strong>ClassificationRemoved</strong> - Raised when product removed from taxon</item>
/// <item><strong>Activated</strong> - Raised when product status changed to Active</item>
/// <item><strong>Discontinued</strong> - Raised when product discontinued</item>
/// <item><strong>Archived</strong> - Raised when product archived</item>
/// <item><strong>Deleted</strong> - Raised when product soft-deleted</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Concerns Implemented:</strong>
/// <list type="bullet">
/// <item><strong>IHasParameterizableName</strong> - Name + Presentation (display flexibility)</item>
/// <item><strong>IHasUniqueName</strong> - Name uniqueness enforcement in database</item>
/// <item><strong>IHasSlug</strong> - URL-friendly slug for SEO and discovery</item>
/// <item><strong>IHasSeoMetadata</strong> - MetaTitle, MetaDescription, MetaKeywords for SEO</item>
/// <item><strong>IHasMetadata</strong> - PublicMetadata (storefront visible) and PrivateMetadata (internal only)</item>
/// <item><strong>ISoftDeletable</strong> - Soft deletion (logical delete, retain history)</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Common Usage Pattern:</strong>
/// <code>
/// // 1. Create new product
/// var result = Product.Create(
///     name: "Premium T-Shirt",
///     slug: "premium-t-shirt",
///     description: "High-quality cotton t-shirt",
///     isDigital: false);
/// if (result.IsError) return Problem(result.FirstError);
/// var product = result.Value;
/// 
/// // 2. Add non-master variants (colors, sizes)
/// var blueResult = product.AddVariant(sku: "TS-001-BLU");
/// var redResult = product.AddVariant(sku: "TS-001-RED");
/// 
/// // 3. Assign to categories
/// var categoryResult = product.AddClassification(apparel_taxon);
/// 
/// // 4. Activate product
/// var activateResult = product.Activate();
/// 
/// // 5. Save changes
/// await dbContext.SaveChangesAsync();
/// </code>
/// </para>
/// </remarks>
public sealed class Product : Aggregate,
    IHasParameterizableName,
    IHasUniqueName,
    IHasMetadata,
    IHasSlug,
    IHasSeoMetadata,
    ISoftDeletable
{
    #region Constraints
    /// <summary>
    /// Defines constraints and validation boundaries for product properties.
    /// </summary>
    /// <remarks>
    /// These constants ensure data consistency and reasonable limits for product information.
    /// Constraints are referenced during validation in factory methods and update operations.
    /// </remarks>
    public static class Constraints
    {
        /// <summary>
        /// Maximum length for product name (e.g., "Premium Organic Cotton T-Shirt").
        /// Ensures names fit in UI displays and database VARCHAR fields.
        /// </summary>
        public const int NameMaxLength = CommonInput.Constraints.NamesAndUsernames.NameMaxLength;
        
        /// <summary>
        /// Maximum length for product description (e.g., "High-quality cotton grown...")
        /// Typically displayed in product detail pages, allows substantive product information.
        /// </summary>
        public const int DescriptionMaxLength = CommonInput.Constraints.Text.LongTextMaxLength;
        
        /// <summary>
        /// Maximum length for product slug (e.g., "premium-organic-cotton-t-shirt").
        /// Used in URLs for SEO-friendly product links.
        /// </summary>
        public const int SlugMaxLength = CommonInput.Constraints.SlugsAndVersions.SlugMaxLength;
        
        /// <summary>
        /// Valid product status values: Draft, Active, Archived.
        /// Used to validate status transitions.
        /// </summary>
        public static readonly string[] ValidStatuses = ["Draft", "Active", "Archived"];
    }

    /// <summary>
    /// Represents the lifecycle status of a product.
    /// </summary>
    /// <remarks>
    /// Products transition through a simple state machine:
    /// - Draft: Initial state, not visible to customers
    /// - Active: Visible and available for purchase
    /// - Archived: Retired product, kept for historical purposes
    /// </remarks>
    public enum ProductStatus
    {
        /// <summary>
        /// Product is in draft mode (work in progress).
        /// Not visible on storefront, available for editing.
        /// </summary>
        Draft = 0,
        
        /// <summary>
        /// Product is active and available for purchase.
        /// Visible on storefront if variants are purchasable and in stock.
        /// </summary>
        Active = 1,
        
        /// <summary>
        /// Product is archived (retired).
        /// Not available for purchase; kept for historical order tracking.
        /// </summary>
        Archived = 2
    }
    #endregion

    #region Errors
    /// <summary>
    /// Defines error scenarios specific to Product operations.
    /// </summary>
    /// <remarks>
    /// These errors represent validation failures, conflicts, or state transition violations.
    /// All errors follow ErrorOr pattern for explicit, type-safe error handling.
    /// </remarks>
    public static class Errors
    {
        /// <summary>
        /// Triggered when product name is missing or empty.
        /// Name is required for product identification and storefront display.
        /// </summary>
        public static Error NameRequired => CommonInput.Errors.Required(prefix: nameof(Product), field: nameof(Name));
        
        /// <summary>
        /// Triggered when product name exceeds maximum length constraint.
        /// </summary>
        public static Error NameTooLong => CommonInput.Errors.TooLong(prefix: nameof(Product), field: nameof(Name), maxLength: Constraints.NameMaxLength);
        
        /// <summary>
        /// Triggered when discontinue date is before product's activation date.
        /// </summary>
        public static Error DiscontinueOnBeforeMakeActiveAt(DateTimeOffset makeActiveAt) => 
            CommonInput.Errors.DateOffsetOutOfRange(prefix: nameof(Product), field: nameof(DiscontinueOn), min: makeActiveAt);
        
        /// <summary>
        /// Triggered when attempting to delete a product that has completed orders.
        /// Products with orders must be soft-deleted to preserve order history.
        /// </summary>
        public static Error CannotDeleteWithCompleteOrders => 
            Error.Conflict(code: "Product.CannotDeleteWithCompleteOrders", description: "Cannot delete product with completed orders.");
        
        /// <summary>
        /// Triggered when product cannot be found by ID.
        /// </summary>
        public static Error NotFound(Guid id) => 
            Error.NotFound(code: "Product.NotFound", description: $"Product with ID '{id}' was not found.");
        
        /// <summary>
        /// Triggered when attempting to delete a product with existing non-master variants.
        /// Delete non-master variants first, or soft-delete the entire product.
        /// </summary>
        public static Error HasVariants => 
            Error.Conflict(code: "Product.HasVariants", description: "Cannot delete product with existing variants.");
        
        /// <summary>
        /// Triggered when attempting to add an option type that is already linked to the product.
        /// Each option type can only be added once per product.
        /// </summary>
        public static Error OptionTypeAlreadyAdded => 
            Error.Conflict(code: "Product.OptionTypeAlreadyAdded", description: "Option type already added to product.");
        
        /// <summary>
        /// Triggered when attempting to add a property that is already linked to the product.
        /// Each property can only be added once per product.
        /// </summary>
        public static Error PropertyAlreadyAdded => 
            Error.Conflict(code: "Product.PropertyAlreadyAdded", description: "Property already added to product.");
        
        /// <summary>
        /// Triggered when product slug does not match required format (lowercase, hyphens, alphanumeric).
        /// Slug must be URL-safe for SEO and routing.
        /// </summary>
        public static Error SlugInvalidFormat => 
            CommonInput.Errors.InvalidSlug(prefix: nameof(Product), field: nameof(Slug));
        
        /// <summary>
        /// Triggered when product slug exceeds maximum length constraint.
        /// </summary>
        public static Error SlugTooLong => 
            CommonInput.Errors.TooLong(prefix: nameof(Product), field: nameof(Slug), maxLength: Constraints.SlugMaxLength);
        
        /// <summary>
        /// Triggered when product description exceeds maximum length constraint.
        /// </summary>
        public static Error DescriptionTooLong => 
            CommonInput.Errors.TooLong(prefix: nameof(Product), field: nameof(Description), maxLength: Constraints.DescriptionMaxLength);
        
        /// <summary>
        /// Triggered when product status is not one of the valid values (Draft, Active, Archived).
        /// </summary>
        public static Error InvalidStatus => 
            Error.Validation(code: "Product.InvalidStatus", description: $"Product status must be one of: {string.Join(separator: ", ", value: Constraints.ValidStatuses)}");
    }
    #endregion

    #region Properties
    /// <summary>
    /// The name of the product.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// The presentation name of the product.
    /// </summary>
    public string Presentation { get; set; } = string.Empty;
    /// <summary>
    /// The description of the product.
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// The URL-friendly slug for the product.
    /// </summary>
    public string Slug { get; set; } = string.Empty;
    /// <summary>
    /// The date and time when the product becomes available.
    /// </summary>
    public DateTimeOffset? AvailableOn { get; set; }
    /// <summary>
    /// The date and time when the product should be made active.
    /// </summary>
    public DateTimeOffset? MakeActiveAt { get; set; }
    /// <summary>
    /// The date and time when the product should be discontinued.
    /// </summary>
    public DateTimeOffset? DiscontinueOn { get; set; }
    /// <summary>
    /// The current status of the product (Draft, Active, Archived).
    /// </summary>
    public ProductStatus Status { get; set; } = ProductStatus.Draft;
    /// <summary>
    /// Indicates if the product is digital.
    /// </summary>
    public bool IsDigital { get; set; }
    /// <summary>
    /// The meta title for SEO.
    /// </summary>
    public string? MetaTitle { get; set; }
    /// <summary>
    /// The meta description for SEO.
    /// </summary>
    public string? MetaDescription { get; set; }
    /// <summary>
    /// The meta keywords for SEO.
    /// </summary>
    public string? MetaKeywords { get; set; }
    /// <summary>
    /// Public metadata associated with the product.
    /// </summary>
    public IDictionary<string, object?>? PublicMetadata { get; set; } = new Dictionary<string, object?>();
    /// <summary>
    /// Private metadata associated with the product.
    /// </summary>
    public IDictionary<string, object?>? PrivateMetadata { get; set; } = new Dictionary<string, object?>();
    /// <summary>
    /// Indicates if the product is marked for regenerating taxon products.
    /// </summary>
    public bool MarkedForRegenerateTaxonProducts { get; set; }
    #endregion

    #region Relationships
    /// <summary>
    /// Collection of variants associated with the product.
    /// </summary>
    public ICollection<Variant> Variants { get; set; } = new List<Variant>();
    /// <summary>
    /// Read-only collection of orders containing this product's variants.
    /// </summary>
    public IReadOnlyCollection<Order> Orders =>
        Variants.SelectMany(selector: v => v.LineItems.Select(selector: li => li.Order)).Distinct().ToList().AsReadOnly();
    /// <summary>
    /// Collection of images associated with the product.
    /// </summary>
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    /// <summary>
    /// Collection of product option types associated with the product.
    /// </summary>
    public ICollection<ProductOptionType> ProductOptionTypes { get; set; } = new List<ProductOptionType>();
    /// <summary>
    /// Collection of option types associated with the product.
    /// </summary>
    public ICollection<OptionType> OptionTypes => ProductOptionTypes.Select(selector: pot => pot.OptionType).ToList();
    /// <summary>
    /// Collection of product properties associated with the product.
    /// </summary>
    public ICollection<ProductPropertyType> ProductProperties { get; set; } = new List<ProductPropertyType>();
    /// <summary>
    /// Collection of properties associated with the product.
    /// </summary>
    public ICollection<PropertyType> Properties => ProductProperties.Select(selector: pp => pp.PropertyType).ToList();
    /// <summary>
    /// Collection of classifications (categories) associated with the product.
    /// </summary>
    public ICollection<Classification> Classifications { get; set; } = new List<Classification>();
    /// <summary>
    /// Collection of taxons (categories) associated with the product.
    /// </summary>
    public ICollection<Taxon> Taxons => Classifications.Select(selector: c => c.Taxon).ToList();
    /// <summary>
    /// Collection of taxonomies associated with the product.
    /// </summary>
    public ICollection<Taxonomy> Taxonomies => Taxons.Select(selector: t => t.Taxonomy).ToList();
    /// <summary>
    /// Collection of store products associated with the product.
    /// </summary>
    public ICollection<StoreProduct>? StoreProducts { get; set; } = new List<StoreProduct>();
    /// <summary>
    /// Collection of stores where the product is available.
    /// </summary>
    public ICollection<Store> Stores => StoreProducts?.Select(selector: sp => sp.Store).ToList() ?? [];
    /// <summary>
    /// Collection of reviews for the product.
    /// </summary>
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    #endregion

    #region Computed Properties
    /// <summary>
    /// Gets the master variant of the product.
    /// </summary>
    /// <returns>An <see cref="ErrorOr{Variant}"/> indicating success with the master variant, or an error if not found.</returns>
    public ErrorOr<Variant> GetMaster() =>
        Variants.FirstOrDefault(predicate: v => v.IsMaster) is { } m
            ? m
            : Error.NotFound(code: "Product.MasterMissing", description: "Master variant missing.");
    /// <summary>
    /// Indicates if the product has any non-master variants.
    /// </summary>
    public bool HasVariants => Variants.Any(predicate: v => !v.IsMaster);
    /// <summary>
    /// Indicates if the product is available for purchase.
    /// </summary>
    public bool Available => Status == ProductStatus.Active && !IsDeleted && (AvailableOn == null || AvailableOn <= DateTimeOffset.UtcNow);
    /// <summary>
    /// Gets the default variant for the product.
    /// </summary>
    public Variant DefaultVariant => GetDefaultVariant();
    /// <summary>
    /// Gets the default image for the product (first image by position).
    /// </summary>
    public ProductImage? DefaultImage => Images.OrderBy(keySelector: a => a.Position).FirstOrDefault();
    /// <summary>
    /// Gets the secondary image for the product (second image by position).
    /// </summary>
    public ProductImage? SecondaryImage => Images.OrderBy(keySelector: a => a.Position).Skip(count: 1).FirstOrDefault();
    /// <summary>
    /// Indicates if any variant of the product is purchasable.
    /// </summary>
    public bool Purchasable => DefaultVariant.Purchasable || Variants.Any(predicate: v => v.Purchasable);
    /// <summary>
    /// Indicates if any variant of the product is in stock.
    /// </summary>
    public bool InStock => DefaultVariant.InStock || Variants.Any(predicate: v => v.InStock);
    /// <summary>
    /// Indicates if any variant of the product is backorderable.
    /// </summary>
    public bool Backorderable => DefaultVariant.Backorderable || Variants.Any(predicate: v => v.Backorderable);
    /// <summary>
    /// Calculates the total quantity on hand across all variants.
    /// </summary>
    public double TotalOnHand =>
        Variants.All(v => v.TrackInventory)
            ? double.PositiveInfinity
            : Variants.Sum(selector: v => v.StockItems.Sum(selector: si => si.QuantityOnHand));
    /// <summary>
    /// Gets the main taxon (category) for the product.
    /// </summary>
    public Taxon? MainTaxon => Taxons.OrderByDescending(keySelector: t => t.Lft).FirstOrDefault();
    #endregion

    #region Soft Delete
    /// <summary>
    /// The date and time when the product was soft-deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }
    /// <summary>
    /// The user who soft-deleted the product.
    /// </summary>
    public string? DeletedBy { get; set; }
    /// <summary>
    /// Indicates if the product is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }
    #endregion

    #region Constructors
    /// <summary>
    /// Initializes a new instance of the <see cref="Product"/> class.
    /// Required for EF Core.
    /// </summary>
    private Product() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Creates a new <see cref="Product"/> instance.
    /// </summary>
    /// <param name="name">The name of the product.</param>
    /// <param name="presentation">The new presentation name of the product.</param>
    /// <param name="description">The description of the product.</param>
    /// <param name="slug">The URL-friendly slug for the product. If null or empty, it will be generated from the name.</param>
    /// <param name="metaTitle">The meta title for SEO.</param>
    /// <param name="metaDescription">The meta description for SEO.</param>
    /// <param name="metaKeywords">The meta keywords for SEO.</param>
    /// <param name="availableOn">The date and time when the product becomes available.</param>
    /// <param name="makeActiveAt">The date and time when the product should be made active.</param>
    /// <param name="discontinueOn">The date and time when the product should be discontinued.</param>
    /// <param name="isDigital">Indicates if the product is digital.</param>
    /// <param name="publicMetadata">Public metadata associated with the product.</param>
    /// <param name="privateMetadata">Private metadata associated with the product.</param>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the new product, or an error if validation fails.</returns>
    public static ErrorOr<Product> Create(
        string name,
        string? presentation = null,
        string? description = null,
        string? slug = null,
        string? metaTitle = null,
        string? metaDescription = null,
        string? metaKeywords = null,
        DateTimeOffset? availableOn = null,
        DateTimeOffset? makeActiveAt = null,
        DateTimeOffset? discontinueOn = null,
        bool isDigital = false,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        // Normalize name and presentation
        (name, presentation) = HasParameterizableName.NormalizeParams(name: name, presentation: presentation);

        // Validate Name
        if (string.IsNullOrWhiteSpace(value: name))
            return Errors.NameRequired;
        if (name.Length > Constraints.NameMaxLength)
            return Errors.NameTooLong;

        // Validate Description
        if (description?.Length > Constraints.DescriptionMaxLength)
            return Errors.DescriptionTooLong;

        // Normalize Slug
        var finalSlug = string.IsNullOrWhiteSpace(value: slug) ? name.ToSlug() : slug.ToSlug();
        if (!CommonInput.Constraints.SlugsAndVersions.SlugRegex.IsMatch(input: finalSlug))
            return Errors.SlugInvalidFormat;
        if (finalSlug.Length > Constraints.SlugMaxLength)
            return Errors.SlugTooLong;

        // Validate DiscontinueOn vs MakeActiveAt
        if (discontinueOn.HasValue && makeActiveAt.HasValue && discontinueOn.Value < makeActiveAt.Value)
            return Errors.DiscontinueOnBeforeMakeActiveAt(makeActiveAt: makeActiveAt.Value);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Presentation = presentation.Trim(), 
            Slug = finalSlug,
            Description = description?.Trim(),
            AvailableOn = availableOn,
            MakeActiveAt = makeActiveAt,
            DiscontinueOn = discontinueOn,
            Status = ProductStatus.Draft,
            IsDigital = isDigital,
            MetaTitle = metaTitle?.Trim(),
            MetaDescription = metaDescription?.Trim(),
            MetaKeywords = metaKeywords?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null, // Ensure UpdatedAt is null on creation
            PublicMetadata = publicMetadata != null ? new Dictionary<string, object?>(dictionary: publicMetadata) : new Dictionary<string, object?>(),
            PrivateMetadata = privateMetadata != null ? new Dictionary<string, object?>(dictionary: privateMetadata) : new Dictionary<string, object?>(),
        };

        var masterResult = Variant.Create(productId: product.Id, isMaster: true);
        if (masterResult.IsError)
            return masterResult.FirstError;
        product.Variants.Add(item: masterResult.Value);

        product.AddDomainEvent(domainEvent: new Events.Created(ProductId: product.Id));
        return product;
    }
    #endregion

    #region Business Logic
    /// <summary>
    /// Updates the properties of the product.
    /// </summary>
    /// <param name="name">The new name of the product.</param>
    /// <param name="presentation">The new presentation name of the product.</param>
    /// <param name="description">The new description of the product.</param>
    /// <param name="slug">The new URL-friendly slug for the product.</param>
    /// <param name="metaTitle">The new meta title for SEO.</param>
    /// <param name="metaDescription">The new meta description for SEO.</param>
    /// <param name="metaKeywords">The new meta keywords for SEO.</param>
    /// <param name="availableOn">The new date and time when the product becomes available.</param>
    /// <param name="makeActiveAt">The new date and time when the product should be made active.</param>
    /// <param name="discontinueOn">The new date and time when the product should be discontinued.</param>
    /// <param name="isDigital">The new digital status of the product.</param>
    /// <param name="publicMetadata">New public metadata associated with the product.</param>
    /// <param name="privateMetadata">New private metadata associated with the product.</param>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the updated product, or an error if validation fails.</returns>
    public ErrorOr<Product> Update(
        string? name = null,
        string? presentation = null,
        string? description = null,
        string? slug = null,
        string? metaTitle = null,
        string? metaDescription = null,
        string? metaKeywords = null,
        DateTimeOffset? availableOn = null,
        DateTimeOffset? makeActiveAt = null,
        DateTimeOffset? discontinueOn = null,
        bool? isDigital = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        bool changed = false;

        // Normalize name and presentation
        (name, presentation) = HasParameterizableName.NormalizeParams(name: name, presentation: presentation);

        // Validate and update Name
        if (!string.IsNullOrWhiteSpace(value: name))
        {
            if (name.Length > Constraints.NameMaxLength)
                return Errors.NameTooLong;
            if (name != Name)
            {
                Name = name.Trim();
                changed = true;
            }
        }

        // Validate and update Presentation
        if (!string.IsNullOrWhiteSpace(value: presentation))
        {
            if (presentation.Length > Constraints.NameMaxLength)
                return Errors.NameTooLong;
            if (presentation != Presentation)
            {
                Presentation = presentation.Trim();
                changed = true;
            }
        }

        // Validate and update Description
        if (description != null)
        {
            if (description.Length > Constraints.DescriptionMaxLength)
                return Errors.DescriptionTooLong;
            if (description != Description)
            {
                Description = description.Trim();
                changed = true;
            }
        }

        // Validate and update Slug
        var newSlug = string.IsNullOrWhiteSpace(value: slug) ? Name.ToSlug() : slug.ToSlug();
        if (!CommonInput.Constraints.SlugsAndVersions.SlugRegex.IsMatch(input: newSlug))
            return Errors.SlugInvalidFormat;
        if (newSlug.Length > Constraints.SlugMaxLength)
            return Errors.SlugTooLong;
        if (newSlug != Slug)
        {
            Slug = newSlug;
            changed = true;
        }

        // Update IsDigital
        if (isDigital.HasValue && isDigital.Value != IsDigital)
        {
            IsDigital = isDigital.Value;
            changed = true;
        }

        // Update MetaTitle
        if (metaTitle != null)
        {
            if (metaTitle != MetaTitle)
            {
                MetaTitle = metaTitle.Trim();
                changed = true;
            }
        }

        // Update MetaDescription
        if (metaDescription != null)
        {
            if (metaDescription != MetaDescription)
            {
                MetaDescription = metaDescription.Trim();
                changed = true;
            }
        }

        // Update MetaKeywords
        if (metaKeywords != null)
        {
            if (metaKeywords != MetaKeywords)
            {
                MetaKeywords = metaKeywords.Trim();
                changed = true;
            }
        }

        // Update AvailableOn
        if (availableOn != null)
        {
            if (availableOn != AvailableOn)
            {
                AvailableOn = availableOn;
                changed = true;
            }
        }

        // Update MakeActiveAt
        if (makeActiveAt != null)
        {
            if (makeActiveAt != MakeActiveAt)
            {
                MakeActiveAt = makeActiveAt;
                changed = true;
            }
        }

        // Update DiscontinueOn
        if (discontinueOn != null)
        {
            if (discontinueOn != DiscontinueOn)
            {
                DiscontinueOn = discontinueOn;
                changed = true;
            }
        }

        // Validate DiscontinueOn vs MakeActiveAt
        if (DiscontinueOn.HasValue && MakeActiveAt.HasValue && DiscontinueOn.Value < MakeActiveAt.Value)
            return Errors.DiscontinueOnBeforeMakeActiveAt(makeActiveAt: MakeActiveAt.Value);

        // Update PublicMetadata
        if (publicMetadata != null && !PublicMetadata.MetadataEquals(dict2: publicMetadata))
        {
            PublicMetadata = new Dictionary<string, object?>(dictionary: publicMetadata);
            changed = true;
        }

        // Update PrivateMetadata
        if (privateMetadata != null && !PrivateMetadata.MetadataEquals(dict2: privateMetadata))
        {
            PrivateMetadata = new Dictionary<string, object?>(dictionary: privateMetadata);
            changed = true;
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(domainEvent: new Events.ProductUpdated(ProductId: Id));
            TouchRelatedEntities();
        }

        return this;
    }

    /// <summary>
    /// Activates the product, changing its status to <see cref="ProductStatus.Active"/>.
    /// </summary>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the activated product, or the current product if already active.</returns>
    public ErrorOr<Product> Activate()
    {
        if (Status == ProductStatus.Active)
            return this;

        Status = ProductStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.ProductActivated(ProductId: Id));
        TouchRelatedEntities();
        return this;
    }

    /// <summary>
    /// Archives the product, changing its status to <see cref="ProductStatus.Archived"/>.
    /// </summary>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the archived product, or the current product if already archived.</returns>
    public ErrorOr<Product> Archive()
    {
        if (Status == ProductStatus.Archived)
            return this;

        Status = ProductStatus.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.ProductArchived(ProductId: Id));
        TouchRelatedEntities();
        return this;
    }

    /// <summary>
    /// Sets the product status to <see cref="ProductStatus.Draft"/>.
    /// </summary>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the drafted product, or the current product if already in draft status.</returns>
    public ErrorOr<Product> Draft()
    {
        if (Status == ProductStatus.Draft)
            return this;

        Status = ProductStatus.Draft;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.ProductDrafted(ProductId: Id));
        TouchRelatedEntities();
        return this;
    }

    /// <summary>
    /// Discontinues the product, setting its <see cref="DiscontinueOn"/> date to now and changing its status to <see cref="ProductStatus.Archived"/>.
    /// </summary>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the discontinued product, or the current product if already discontinued.</returns>
    public ErrorOr<Product> Discontinue()
    {
        if (Discontinued)
            return this;

        DiscontinueOn = DateTimeOffset.UtcNow;
        Status = ProductStatus.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.ProductDiscontinued(ProductId: Id));
        TouchRelatedEntities();
        return this;
    }

    /// <summary>
    /// Adds a new asset (image) to the product.
    /// </summary>
    /// <param name="asset">The <see cref="ProductImage"/> to add.</param>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the updated product, or an error if validation fails.</returns>
    public ErrorOr<Product> AddImage(ProductImage? asset)
    {
        // Guard: null asset
        if (asset == null)
            return ProductImage.Errors.Required;

        // Validate asset parameters
        var validationErrors = asset.ValidateParams(prefix: nameof(ProductImage));
        if (validationErrors.Any())
            return validationErrors.First();

        // Guard: duplicate asset type
        if (Images.Any(predicate: a => a.Type == asset.Type))
            return ProductImage.Errors.AlreadyExists(productId: Id, variantId: null, type: asset.Type);

        // Set position and add asset
        var maxPosition = Images.Any() ? Images.Max(selector: a => a.Position) : 0;
        asset.SetPosition(position: maxPosition + 1);
        Images.Add(item: asset);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.ProductImageAdded(ProductId: Id, ImageId: asset.Id));

        return this;
    }

    /// <summary>
    /// Removes an asset (image) from the product.
    /// </summary>
    /// <param name="assetId">The ID of the asset to remove.</param>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the updated product, or an error if the asset is not found.</returns>
    public ErrorOr<Product> RemoveImage(Guid assetId)
    {
        var asset = Images.FirstOrDefault(predicate: a => a.Id == assetId);
        if (asset == null)
            return ProductImage.Errors.NotFound(id: assetId); // Use ProductImage.Errors.NotFound

        Images.Remove(item: asset);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.ProductImageRemoved(ProductId: Id, ImageId: asset.Id));

        return this;
    }

    /// <summary>
    /// Updates an existing asset (image) of the product.
    /// </summary>
    /// <param name="assetId">The ID of the asset to update.</param>
    /// <param name="url">The new URL of the asset.</param>
    /// <param name="altText">The new alt text for the asset.</param>
    /// <param name="position">The new position of the asset.</param>
    /// <param name="type">The new type of the asset.</param>
    /// <param name="contentType">The new content type of the asset.</param>
    /// <param name="width">The new width of the asset.</param>
    /// <param name="height">The new height of the asset.</param>
    /// <param name="dimensionsUnit">The new dimensions unit of the asset.</param>
    /// <param name="publicMetadata">New public metadata for the asset.</param>
    /// <param name="privateMetadata">New private metadata for the asset.</param>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the updated product, or an error if the asset is not found or update fails.</returns>
    public ErrorOr<Product> UpdateAsset(
        Guid assetId,
        string? url = null,
        string? altText = null,
        int? position = null,
        string? type = null,
        string? contentType = null,
        int? width = null,
        int? height = null,
        string? dimensionsUnit = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        var asset = Images.FirstOrDefault(predicate: a => a.Id == assetId);
        if (asset == null)
            return ProductImage.Errors.NotFound(id: assetId); // Use ProductImage.Errors.NotFound

        var updateResult = asset.Update(url: url, alt: altText, position: position, type: type, contentType: contentType, width: width, height: height, dimensionsUnit: dimensionsUnit, publicMetadata: publicMetadata, privateMetadata: privateMetadata);
        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.ProductAssetUpdated(ProductId: Id, AssetId: assetId));
        return this;
    }

    /// <summary>
    /// Adds an option type to the product.
    /// </summary>
    /// <param name="productOptionType">The <see cref="ProductOptionType"/> to add.</param>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the updated product, or an error if validation fails.</returns>
    public ErrorOr<Product> AddOptionType(ProductOptionType? productOptionType)
    {
        // Guard: null productOptionType
        if (productOptionType == null)
            return ProductOptionType.Errors.Required;

        // Guard: duplicate option type
        if (ProductOptionTypes.Any(predicate: pot => pot.OptionTypeId == productOptionType.OptionTypeId))
            return ProductOptionType.Errors.AlreadyLinked;

        ProductOptionTypes.Add(item: productOptionType);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.ProductOptionTypeAdded(ProductId: Id, ProductOptionTypeId: productOptionType.Id));

        return this;
    }

    /// <summary>
    /// Removes an option type from the product.
    /// </summary>
    /// <param name="optionTypeId">The ID of the option type to remove.</param>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the updated product, or an error if the option type is not found.</returns>
    public ErrorOr<Product> RemoveOptionType(Guid optionTypeId)
    {
        var productOptionType = ProductOptionTypes.FirstOrDefault(predicate: pot => pot.OptionTypeId == optionTypeId);
        if (productOptionType == null)
            return ProductOptionType.Errors.NotFound(id: optionTypeId);

        ProductOptionTypes.Remove(item: productOptionType);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.ProductOptionTypeRemoved(ProductId: Id, OptionTypeId: optionTypeId));

        return this;
    }

    /// <summary>
    /// Adds a category (classification) to the product.
    /// </summary>
    /// <param name="classification">The <see cref="Classification"/> to add.</param>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the updated product, or an error if validation fails.</returns>
    public ErrorOr<Product> AddClassification(Classification? classification)
    {
        // Guard: null classification
        if (classification == null)
            return CommonInput.Errors.Null(nameof(Classifications));

        // Guard: duplicate classification
        if (Classifications.Any(predicate: c => c.TaxonId == classification.TaxonId))
            return Classification.Errors.AlreadyLinked(productId: Id, taxonId: classification.TaxonId);

        Classifications.Add(item: classification);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.ProductCategoryAdded(ProductId: Id, TaxonId: classification.TaxonId));

        return this;
    }

    /// <summary>
    /// Removes a classification from the product.
    /// </summary>
    /// <param name="taxonId">The ID of the taxon (classification) to remove.</param>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the updated product, or an error if the classification is not found.</returns>
    public ErrorOr<Product> RemoveClassification(Guid taxonId)
    {
        var classification = Classifications.FirstOrDefault(predicate: c => c.TaxonId == taxonId);
        if (classification == null)
            return Classification.Errors.NotFound(id: taxonId);

        Classifications.Remove(item: classification);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.ProductCategoryRemoved(ProductId: Id, TaxonId: taxonId));

        return this;
    }


    /// <summary>
    /// Increments the view count for the product by raising a <see cref="Events.ProductViewed"/> event.
    /// </summary>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the product.</returns>
    public ErrorOr<Product> IncrementViewCount()
    {
        AddDomainEvent(domainEvent: new Events.ProductViewed(ProductId: Id));
        return this;
    }

    /// <summary>
    /// Increments the add to cart count for the product by raising a <see cref="Events.ProductAddedToCart"/> event.
    /// </summary>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the product.</returns>
    public ErrorOr<Product> IncrementAddToCartCount()
    {
        AddDomainEvent(domainEvent: new Events.ProductAddedToCart(ProductId: Id));
        return this;
    }

    /// <summary>
    /// Adds a new variant to the product.
    /// </summary>
    /// <param name="variant">The <see cref="Variant"/> to add.</param>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the updated product, or an error if validation fails.</returns>
    public ErrorOr<Product> AddVariant(Variant? variant)
    {
        // Guard: null variant
        if (variant == null)
            return Error.Validation(code: "Product.InvalidVariant", description: "Variant cannot be null.");

        Variants.Add(item: variant);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.VariantAdded(VariantId: variant.Id, ProductId: Id));

        return this;
    }

    /// <summary>
    /// Sets the master variant out of stock by adjusting its stock items to zero.
    /// </summary>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the updated product, or an error if stock adjustment fails.</returns>
    public ErrorOr<Product> SetMasterOutOfStock()
    {
        var masterResult = GetMaster();
        if (masterResult.IsError)
        {
            return masterResult.FirstError;
        }
        var masterVariant = masterResult.Value;

        foreach (var stockItem in masterVariant.StockItems)
        {
            var currentCount = stockItem.QuantityOnHand;
            if (currentCount > 0)
            {
                var adjustResult = stockItem.Adjust(quantity: -currentCount, originator: StockMovement.MovementOriginator.Adjustment, reason: "Master variant out of stock");
                if (adjustResult.IsError)
                    return adjustResult.FirstError;
            }
        }

        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.ProductUpdated(ProductId: Id));
        return this;
    }

    /// <summary>
    /// Clears the stock and prices for the master variant.
    /// </summary>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the updated product, or an error if stock adjustment fails.</returns>
    public ErrorOr<Product> ClearMasterStockAndPrices()
    {
        var masterResult = GetMaster();
        if (masterResult.IsError)
        {
            return masterResult.FirstError;
        }
        var masterVariant = masterResult.Value;

        // Clear stock by adjusting to 0
        foreach (var stockItem in masterVariant.StockItems.ToList())
        {
            var currentCount = stockItem.QuantityOnHand;
            if (currentCount > 0)
            {
                var adjustResult = stockItem.Adjust(quantity: -currentCount, originator: StockMovement.MovementOriginator.Adjustment, reason: "Clearing master stock");
                if (adjustResult.IsError)
                    return adjustResult.FirstError;
            }
        }

        // Clear prices
        foreach (var price in masterVariant.Prices.ToList())
        {
            masterVariant.Prices.Remove(item: price);
        }


        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.ProductUpdated(ProductId: Id));
        return this;
    }
    /// <summary>
    /// Adds a product property to the product.
    /// </summary>
    /// <param name="productProperty">The <see cref="ProductPropertyType"/> to add.</param>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the updated product, or an error if validation fails.</returns>
    public ErrorOr<Product> AddProductProperty(ProductPropertyType? productProperty)
    {
        // Guard: null productProperty
        if (productProperty == null)
            return Error.Validation(code: "Product.ProductProperty.Null", description: "Product property cannot be null.");

        // Guard: duplicate property
        if (ProductProperties.Any(predicate: pp => pp.PropertyId == productProperty.PropertyId))
            return Error.Conflict(code: "Product.Property.Duplicate", description: $"Property with ID '{productProperty.PropertyId}' already exists.");

        ProductProperties.Add(item: productProperty);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.ProductPropertyAdded(ProductId: Id, ProductPropertyId: productProperty.Id));

        return this;
    }

    /// <summary>
    /// Removes a product property from the product.
    /// </summary>
    /// <param name="propertyId">The ID of the property to remove.</param>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the updated product, or an error if the property is not found.</returns>
    public ErrorOr<Product> RemoveProperty(Guid propertyId)
    {
        var productProperty = ProductProperties.FirstOrDefault(predicate: pp => pp.PropertyId == propertyId);
        if (productProperty == null)
            return ProductPropertyType.Errors.NotFound(id: propertyId);

        ProductProperties.Remove(item: productProperty);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.ProductPropertyRemoved(ProductId: Id, PropertyId: propertyId));

        return this;
    }

    /// <summary>
    /// Sets the value of an existing product property, or creates a new one if it doesn't exist.
    /// </summary>
    /// <param name="propertyId">The ID of the property to set.</param>
    /// <param name="value">The new value for the property.</param>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the updated product, or an error if validation fails.</returns>
    public ErrorOr<Product> SetPropertyValue(Guid propertyId, string value)
    {
        var productProperty = ProductProperties.FirstOrDefault(predicate: pp => pp.PropertyId == propertyId);

        if (productProperty is not null)
        {
            var updateResult = productProperty.Update(value: value);
            if (updateResult.IsError)
                return updateResult.FirstError;
        }
        else
        {
            var createResult = ProductPropertyType.Create(productId: Id, propertyId: propertyId, value: value);
            if (createResult.IsError)
                return createResult.FirstError;

            ProductProperties.Add(item: createResult.Value);
            AddDomainEvent(domainEvent: new Events.ProductPropertyAdded(ProductId: Id, ProductPropertyId: createResult.Value.Id));
        }

        UpdatedAt = DateTimeOffset.UtcNow;
        return this;
    }

    /// <summary>
    /// Removes a variant from the product.
    /// </summary>
    /// <param name="variantId">The ID of the variant to remove.</param>
    /// <returns>An <see cref="ErrorOr{Product}"/> indicating success with the updated product, or an error if the variant is not found.</returns>
    public ErrorOr<Product> RemoveVariant(Guid variantId)
    {
        var variant = Variants.FirstOrDefault(predicate: v => v.Id == variantId);
        if (variant == null)
            return Variant.Errors.NotFound(id: variantId);

        Variants.Remove(item: variant);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.VariantRemoved(VariantId: variantId, ProductId: Id));

        return this;
    }

    /// <summary>
    /// Soft-deletes the product.
    /// </summary>
    /// <returns>An <see cref="ErrorOr{Deleted}"/> indicating success, or an error if the product cannot be deleted.</returns>
    public ErrorOr<Deleted> Delete()
    {
        // Guard: Cannot delete product with completed orders
        if (Orders.Any(predicate: o => o.CompletedAt.HasValue))
            return Errors.CannotDeleteWithCompleteOrders;

        DeletedAt = DateTimeOffset.UtcNow;
        IsDeleted = true;
        Status = ProductStatus.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.ProductDeleted(ProductId: Id));
        TouchRelatedEntities();
        return Result.Deleted;
    }

    /// <summary>
    /// Checks if the product is currently on sale for a given currency.
    /// </summary>
    /// <param name="currency">The currency to check for sale prices. If null, checks for any currency.</param>
    /// <returns>True if the product is on sale, false otherwise.</returns>
    public bool OnSale(string? currency = null)
    {
        return Variants.Any(predicate: v => v.Prices.Any(predicate: p => p.Currency == currency && p.CompareAtAmount > p.Amount));
    }

    /// <summary>
    /// Gets the lowest price of the product across all variants for a given currency.
    /// </summary>
    /// <param name="currency">The currency to consider. If null, considers all currencies.</param>
    /// <returns>The lowest price, or null if no prices are found.</returns>
    public decimal? LowestPrice(string? currency = null)
    {
        return Variants.SelectMany(selector: v => v.Prices).Where(predicate: p => p.Currency == currency).Min(selector: p => p.Amount);
    }

    /// <summary>
    /// Checks if the product's price varies across its variants for a given currency.
    /// </summary>
    /// <param name="currency">The currency to consider. If null, considers all currencies.</param>
    /// <returns>True if prices vary, false otherwise.</returns>
    public bool PriceVaries(string? currency)
    {
        var amounts = Variants.SelectMany(selector: v => v.Prices).Where(predicate: p => p.Currency == currency && p.Amount.HasValue).Select(selector: p => p.Amount).Distinct();
        return amounts.Count() > 1;
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Gets the default variant for the product.
    /// </summary>
    /// <returns>The default <see cref="Variant"/>.</returns>
    private Variant GetDefaultVariant()
    {

        var purchasable = Variants.FirstOrDefault(predicate: v => v.Purchasable);
        if (purchasable != null) return purchasable;

        var masterResult = GetMaster();
        if (masterResult.IsError)
        {
            throw new InvalidOperationException(message: $"Product with Id {Id} is missing a master variant required for DefaultVariant.");
        }
        var masterVariant = masterResult.Value;

        return Variants.Where(predicate: v => !v.IsMaster).OrderBy(keySelector: v => v.Position).FirstOrDefault() ?? masterVariant;
    }

    /// <summary>
    /// Touches related entities by raising a <see cref="Events.ProductTouchTaxons"/> event.
    /// </summary>
    private void TouchRelatedEntities()
    {
        AddDomainEvent(domainEvent: new Events.ProductTouchTaxons(ProductId: Id, TaxonIds: Taxons.Select(selector: t => t.Id).ToList(), TaxonomyIds: Taxonomies.Select(selector: t => t.Id).ToList()));
    }

    /// <summary>
    /// Indicates if the product is discontinued.
    /// </summary>
    public bool Discontinued => DiscontinueOn.HasValue && DiscontinueOn <= DateTimeOffset.UtcNow;
    /// <summary>
    /// Indicates if the product can be supplied (i.e., any variant can be supplied).
    /// </summary>
    public bool CanSupply => Variants.Any(predicate: v => v.CanSupply);
    /// <summary>
    /// Indicates if the product is backordered (i.e., any variant is backordered).
    /// </summary>
    public bool Backordered => Variants.Any(predicate: v => v.Backordered);
    #endregion

    #region Events
    public static class Events
    {
        /// <summary>
        /// Purpose: Notify that a product has been created.
        /// </summary>
        /// <param name="ProductId">The ID of the created product.</param>
        public sealed record Created(Guid ProductId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a product has been updated.
        /// </summary>
        /// <param name="ProductId">The ID of the updated product.</param>
        public sealed record ProductUpdated(Guid ProductId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a product has been activated.
        /// </summary>
        /// <param name="ProductId">The ID of the activated product.</param>
        public sealed record ProductActivated(Guid ProductId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a product has been archived.
        /// </summary>
        /// <param name="ProductId">The ID of the archived product.</param>
        public sealed record ProductArchived(Guid ProductId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a product has been drafted.
        /// </summary>
        /// <param name="ProductId">The ID of the drafted product.</param>
        public sealed record ProductDrafted(Guid ProductId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a product has been discontinued.
        /// </summary>
        /// <param name="ProductId">The ID of the discontinued product.</param>
        public sealed record ProductDiscontinued(Guid ProductId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a product has been deleted.
        /// </summary>
        /// <param name="ProductId">The ID of the deleted product.</param>
        public sealed record ProductDeleted(Guid ProductId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that an asset has been added to a product.
        /// </summary>
        /// <param name="ProductId">The ID of the product.</param>
        /// <param name="ImageId">The ID of the added image.</param>
        public sealed record ProductImageAdded(Guid ProductId, Guid ImageId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that an asset has been removed from a product.
        /// </summary>
        /// <param name="ProductId">The ID of the product.</param>
        /// <param name="ImageId">The ID of the removed image.</param>
        public sealed record ProductImageRemoved(Guid ProductId, Guid ImageId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that an asset has been updated on a product.
        /// </summary>
        /// <param name="ProductId">The ID of the product.</param>
        /// <param name="AssetId">The ID of the updated asset.</param>
        public sealed record ProductAssetUpdated(Guid ProductId, Guid AssetId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a property has been added to a product.
        /// </summary>
        /// <param name="ProductId">The ID of the product.</param>
        /// <param name="ProductPropertyId">The ID of the added product property.</param>
        public sealed record ProductPropertyAdded(Guid ProductId, Guid ProductPropertyId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a property has been removed from a product.
        /// </summary>
        /// <param name="ProductId">The ID of the product.</param>
        /// <param name="PropertyId">The ID of the removed property.</param>
        public sealed record ProductPropertyRemoved(Guid ProductId, Guid PropertyId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that an option type has been added to a product.
        /// </summary>
        /// <param name="ProductId">The ID of the product.</param>
        /// <param name="ProductOptionTypeId">The ID of the added product option type.</param>
        public sealed record ProductOptionTypeAdded(Guid ProductId, Guid ProductOptionTypeId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that an option type has been removed from a product.
        /// </summary>
        /// <param name="ProductId">The ID of the product.</param>
        /// <param name="OptionTypeId">The ID of the removed option type.</param>
        public sealed record ProductOptionTypeRemoved(Guid ProductId, Guid OptionTypeId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a category has been added to a product.
        /// </summary>
        /// <param name="ProductId">The ID of the product.</param>
        /// <param name="TaxonId">The ID of the added taxon (category).</param>
        public sealed record ProductCategoryAdded(Guid ProductId, Guid TaxonId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a category has been removed from a product.
        /// </summary>
        /// <param name="ProductId">The ID of the product.</param>
        /// <param name="TaxonId">The ID of the removed taxon (category).</param>
        public sealed record ProductCategoryRemoved(Guid ProductId, Guid TaxonId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a product has been added to a store.
        /// </summary>
        /// <param name="ProductId">The ID of the product.</param>
        /// <param name="StoreId">The ID of the store.</param>
        public sealed record ProductAddedToStore(Guid ProductId, Guid StoreId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that product store settings have been updated.
        /// </summary>
        /// <param name="ProductId">The ID of the product.</param>
        /// <param name="StoreId">The ID of the store.</param>
        public sealed record ProductStoreSettingsUpdated(Guid ProductId, Guid StoreId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a product has been removed from a store.
        /// </summary>
        /// <param name="ProductId">The ID of the product.</param>
        /// <param name="StoreId">The ID of the store.</param>
        public sealed record ProductRemovedFromStore(Guid ProductId, Guid StoreId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a product has been viewed.
        /// </summary>
        /// <param name="ProductId">The ID of the viewed product.</param>
        public sealed record ProductViewed(Guid ProductId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a product has been added to cart.
        /// </summary>
        /// <param name="ProductId">The ID of the product added to cart.</param>
        public sealed record ProductAddedToCart(Guid ProductId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a variant has been added to a product.
        /// </summary>
        /// <param name="VariantId">The ID of the added variant.</param>
        /// <param name="ProductId">The ID of the product.</param>
        public sealed record VariantAdded(Guid VariantId, Guid ProductId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a variant has been updated.
        /// </summary>
        /// <param name="VariantId">The ID of the updated variant.</param>
        /// <param name="ProductId">The ID of the product.</param>
        public sealed record VariantUpdated(Guid VariantId, Guid ProductId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a variant has been removed from a product.
        /// </summary>
        /// <param name="VariantId">The ID of the removed variant.</param>
        /// <param name="ProductId">The ID of the product.</param>
        public sealed record VariantRemoved(Guid VariantId, Guid ProductId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that related taxons should be updated.
        /// </summary>
        /// <param name="ProductId">The ID of the product.</param>
        /// <param name="TaxonIds">The IDs of the related taxons.</param>
        /// <param name="TaxonomyIds">The IDs of the related taxonomies.</param>
        public sealed record ProductTouchTaxons(Guid ProductId, List<Guid> TaxonIds, List<Guid> TaxonomyIds) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a product's price discount has changed.
        /// </summary>
        /// <param name="ProductId">The ID of the product.</param>
        public sealed record PriceDiscountChanged(Guid ProductId) : DomainEvent;
    }
    #endregion
}
