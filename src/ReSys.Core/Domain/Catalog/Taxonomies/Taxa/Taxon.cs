using ErrorOr;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Common.Extensions;
using ReSys.Core.Domain.Catalog.Products.Classifications;
using ReSys.Core.Domain.Catalog.Taxonomies.Images;
using ReSys.Core.Domain.Catalog.Taxonomies.Rules;
using ReSys.Core.Domain.Promotions.Rules;

namespace ReSys.Core.Domain.Catalog.Taxonomies.Taxa;

/// <summary>
/// Represents a single node in a hierarchical taxonomy structure for categorizing products.
/// Taxons can be manually managed or automatically populated via rule-based product assignment.
/// Uses nested set model (Lft/Rgt/Depth) for efficient hierarchical queries.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Catalog Domain:</strong>
/// Taxons form the backbone of product categorization and discovery:
/// <list type="bullet">
/// <item>
/// <term>Hierarchical Categories</term>
/// <description>Taxons form parent-child relationships (e.g., Apparel → Men's → Shirts)</description>
/// </item>
/// <item>
/// <term>Product Association</term>
/// <description>Products are classified into taxons via Classification entity</description>
/// </item>
/// <item>
/// <term>Storefront Navigation</term>
/// <description>Taxons drive category navigation trees and breadcrumbs</description>
/// </item>
/// <item>
/// <term>Automatic Membership</term>
/// <description>Automatic taxons populate based on rules (e.g., Best Sellers, New Arrivals)</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Nested Set Model (Advanced Pattern):</strong>
/// Taxons use nested set algorithm for efficient tree queries using Lft/Rgt/Depth values:
/// <code>
/// Tree Structure:           Nested Set Values:
///        Root                    Lft=1, Rgt=22
///       /  |  \
///      /   |   \             A: Lft=2, Rgt=7
///     A    B    C            B: Lft=8, Rgt=15
///    / \   |   / \           C: Lft=16, Rgt=21
///   A1 A2  B1 C1 C2          
/// 
/// Query Benefits:
/// ✅ Get all descendants: WHERE Lft > parent.Lft AND Rgt &lt; parent.Rgt
/// ✅ Check if ancestor: WHERE Lft &lt; node.Lft AND Rgt > node.Rgt
/// ✅ Count descendants: (node.Rgt - node.Lft - 1) / 2
/// ✅ Leaf check: node.Rgt = node.Lft + 1
/// </code>
/// </para>
///
/// <para>
/// <strong>Manual vs Automatic Taxons:</strong>
/// <list type="bullet">
/// <item>
/// <term>Manual Taxon (Automatic = false)</term>
/// <description>Products manually assigned to this category by editors</description>
/// </item>
/// <item>
/// <term>Automatic Taxon (Automatic = true)</term>
/// <description>Products automatically assigned based on rules (e.g., sales count, rating, date)</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Automatic Taxon Rules:</strong>
/// When Automatic = true, products are evaluated against defined rules:
/// <code>
/// Automatic Taxon: "Best Sellers"
/// Rules:
///   - Sales count > 1000
///   - Average rating >= 4.5 stars
/// Match Policy: "all" (all rules must pass)
/// 
/// Products matching BOTH rules appear in "Best Sellers"
/// When product sales drop below 1000, it's automatically removed
/// </code>
/// </para>
///
/// <para>
/// <strong>Sort Order Control:</strong>
/// Automatic taxons can specify how products are sorted within them:
/// <list type="bullet">
/// <item>manual - Editor-determined order via Position</item>
/// <item>best-selling - Products with highest sales first</item>
/// <item>name-a-z - Alphabetical by name</item>
/// <item>price-low-to-high - Lowest price first</item>
/// <item>newest-first - Newest products first</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>URL Permalinks:</strong>
/// <code>
/// Taxon hierarchy:  Apparel → Men's → Shirts → T-Shirts
/// Permalink:        /apparel/mens/shirts/t-shirts
/// URL slug:         "t-shirts" (from Name normalized)
/// </code>
/// </para>
///
/// <para>
/// <strong>Key Invariants:</strong>
/// <list type="bullet">
/// <item>A taxon CANNOT be its own parent (self-reference prevention)</item>
/// <item>Parent must belong to the same taxonomy</item>
/// <item>Only one root taxon per taxonomy</item>
/// <item>Cannot delete taxon with children (must reparent first)</item>
/// <item>Taxons are soft-deletable for history retention</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Concerns Implemented:</strong>
/// <list type="bullet">
/// <item><strong>IHasParameterizableName</strong> - Name + Presentation flexibility</item>
/// <item><strong>IHasPosition</strong> - Ordering within parent category</item>
/// <item><strong>IHasSeoMetadata</strong> - MetaTitle, MetaDescription, MetaKeywords for SEO</item>
/// <item><strong>IHasUniqueName</strong> - Name uniqueness within taxonomy</item>
/// <item><strong>IHasMetadata</strong> - PublicMetadata, PrivateMetadata storage</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Typical Usage - Manual Category:</strong>
/// <code>
/// // 1. Create root taxon
/// var apparel = Taxon.Create(
///     taxonomyId: mainCatalog.Id,
///     name: "Apparel",
///     parentId: null);  // Root node
/// 
/// // 2. Add child categories
/// var mens = Taxon.Create(
///     taxonomyId: mainCatalog.Id,
///     name: "Men's",
///     parentId: apparel.Id);
/// 
/// // 3. Assign products
/// product.AddClassification(mens);
/// 
/// // 4. Save (nested set values updated)
/// await dbContext.SaveChangesAsync();
/// </code>
/// </para>
///
/// <para>
/// <strong>Typical Usage - Automatic Category:</strong>
/// <code>
/// // 1. Create automatic taxon
/// var bestSellers = Taxon.Create(
///     taxonomyId: mainCatalog.Id,
///     name: "Best Sellers",
///     automatic: true,
///     rulesMatchPolicy: "all",
///     sortOrder: "best-selling");
/// 
/// // 2. Add rules (system evaluates automatically)
/// var rule1 = TaxonRule.Create(condition: "sales > 1000");
/// bestSellers.AddRule(rule1);
/// 
/// // 3. Products matching rules auto-assigned
/// // When product sales drop, it's auto-removed
/// // When new product hits 1000 sales, it's auto-added
/// </code>
/// </para>
/// </remarks>
public sealed class Taxon :
    Aggregate,
    IHasParameterizableName,
    IHasPosition,
    IHasSeoMetadata,
    IHasUniqueName,
    IHasMetadata
{
    #region Constraints
    /// <summary>
    /// Defines allowable values and constraints for taxon operations.
    /// </summary>
    public static class Constraints
    {
        /// <summary>
        /// Supported image MIME types for category images.
        /// Current: JPEG, PNG, GIF, WebP (modern formats, smaller file sizes)
        /// </summary>
        public static readonly string[] ImageContentTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];

        /// <summary>
        /// Valid sort order values for automatic taxons.
        /// Determines how products appear within the category:
        /// <list type="table">
        /// <item>
        /// <term>manual</term>
        /// <description>Editor-determined order via Position value</description>
        /// </item>
        /// <item>
        /// <term>best-selling</term>
        /// <description>Products with highest sales count first</description>
        /// </item>
        /// <item>
        /// <term>name-a-z, name-z-a</term>
        /// <description>Alphabetical by product name</description>
        /// </item>
        /// <item>
        /// <term>price-low-to-high, price-high-to-low</term>
        /// <description>Sorted by lowest variant price</description>
        /// </item>
        /// <item>
        /// <term>newest-first, oldest-first</term>
        /// <description>Sorted by product creation date</description>
        /// </item>
        /// </list>
        /// </summary>
        public static readonly string[] SortOrders =
        [
            "manual", "best-selling", "name-a-z", "name-z-a",
            "price-high-to-low", "price-low-to-high", "newest-first", "oldest-first"
        ];

        /// <summary>
        /// Rule match policies for automatic taxons.
        /// Determines how multiple rules are evaluated:
        /// <list type="bullet">
        /// <item>
        /// <term>all</term>
        /// <description>ALL rules must pass (AND logic). Product assigned only if every rule condition is true.</description>
        /// </item>
        /// <item>
        /// <term>any</term>
        /// <description>ANY rule can pass (OR logic). Product assigned if at least one rule condition is true.</description>
        /// </item>
        /// </list>
        /// Example: "Best Sellers" with policy="all" requires BOTH high sales AND high rating.
        /// Example: "Sales Or Featured" with policy="any" includes products with high sales OR featured status.
        /// </summary>
        public static readonly string[] RulesMatchPolicies = ["all", "any"];
    }
    #endregion

    #region Errors
    /// <summary>
    /// Domain error definitions for taxon operations.
    /// Returned via ErrorOr pattern for railway-oriented error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Occurs when attempting to set a taxon as its own parent.
        /// Prevention: Cannot create hierarchical loops.
        /// </summary>
        public static Error SelfParenting => Error.Validation(code: "Taxon.SelfParenting", description: "A taxon cannot be its own parent.");
        
        /// <summary>
        /// Occurs when parent taxon belongs to a different taxonomy.
        /// Prevention: Taxonomies must remain isolated hierarchies.
        /// Example: Cannot add a parent from "Size" taxonomy to "Color" taxonomy.
        /// </summary>
        public static Error ParentTaxonomyMismatch => Error.Validation(code: "Taxon.ParentTaxonomyMismatch", description: "Parent must belong to the same taxonomy.");
        
        /// <summary>
        /// Occurs when attempting to create a root taxon when one already exists.
        /// Prevention: Only one root taxon per taxonomy (only one taxon with no parent).
        /// </summary>
        public static Error RootConflict => Error.Conflict(code: "Taxon.RootConflict", description: "This taxonomy already has a root taxon.");
        
        /// <summary>
        /// Occurs when attempting to delete a taxon that has child categories.
        /// Prevention: Cannot leave orphaned categories; must reparent children first.
        /// Resolution: Move children to different parent or delete them individually.
        /// </summary>
        public static Error HasChildren => Error.Validation(code: "Taxon.HasChildren", description: "Cannot delete a taxon that has children.");
        
        /// <summary>
        /// Occurs when referenced taxon cannot be found in database.
        /// Typical causes: ID doesn't exist, taxon was deleted, query for wrong taxonomy.
        /// </summary>
        public static Error NotFound(Guid id) => Error.NotFound(code: "Taxon.NotFound", description: $"Taxon with ID '{id}' was not found.");
    }
    #endregion

    #region Core Properties
    /// <summary>
    /// Internal system name. Used for identification, typically lowercase with hyphens (e.g., "mens-clothing").
    /// Must be unique within the taxonomy.
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// Display name shown to customers. Can differ from Name for better UX (e.g., Name="mens-clothing", Presentation="Men's Clothing").
    /// Supports branding, special formatting, emojis, etc.
    /// </summary>
    public string Presentation { get; set; } = null!;
    
    /// <summary>
    /// Optional detailed description of the category. Shown on category landing pages for improved SEO and UX.
    /// Supports rich content hints (markdown/HTML) depending on frontend implementation.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// URL-friendly permalink for storefront navigation.
    /// Example: "/apparel/mens/clothing" or "/categories/mens-clothing"
    /// Automatically maintained based on hierarchy and name. Regenerated on name/parent changes.
    /// </summary>
    public string Permalink { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable path for breadcrumbs. Space-separated category names up the hierarchy.
    /// Example: "Apparel Men's Clothing" for use in navigation UI.
    /// </summary>
    public string PrettyName { get; set; } = string.Empty;
    
    /// <summary>
    /// When true, this category is excluded from frontend navigation displays.
    /// Useful for: archived categories, draft categories, internal-only categories.
    /// Products in hidden categories are still browsable via direct URL or internal links.
    /// </summary>
    public bool HideFromNav { get; set; }
    
    /// <summary>
    /// Positional ordering within parent category. Lower values appear first.
    /// Updated by editors to reorder categories. Especially used when SortOrder="manual".
    /// Typical range: 0-999 (position increments by 10 for easy reordering).
    /// </summary>
    public int Position { get; set; }
    #endregion

    #region Nested Set Properties
    /// <summary>
    /// Left boundary value in nested set model. Used for efficient hierarchical queries.
    /// All descendants have Lft values greater than parent's Lft.
    /// </summary>
    public int Lft { get; set; }
    
    /// <summary>
    /// Right boundary value in nested set model. Used for efficient hierarchical queries.
    /// All descendants have Rgt values less than parent's Rgt.
    /// Enables efficient queries: WHERE Lft > parent.Lft AND Rgt &lt; parent.Rgt
    /// </summary>
    public int Rgt { get; set; }
    
    /// <summary>
    /// Nesting depth in hierarchy. Root=0, direct children=1, grandchildren=2, etc.
    /// Useful for limiting recursion depth in queries and UI rendering (max nesting display).
    /// </summary>
    public int Depth { get; set; }
    #endregion

    #region Automatic Taxon Properties
    /// <summary>
    /// When true, this is an automatic taxon (system-managed category).
    /// Products are automatically added/removed based on TaxonRules.
    /// When false, this is a manual taxon (editor-managed category).
    /// </summary>
    public bool Automatic { get; set; }
    
    /// <summary>
    /// For automatic taxons: rule matching logic.
    /// "all" = ALL rules must pass (AND logic) - stricter membership criteria
    /// "any" = ANY rule can pass (OR logic) - looser membership criteria
    /// Example: "Best Sellers" with "all" requires high sales AND high rating.
    /// </summary>
    public string RulesMatchPolicy { get; set; } = "all";
    
    /// <summary>
    /// Sort order for products within this taxon.
    /// Valid values: manual, best-selling, name-a-z, name-z-a, price-low-to-high, price-high-to-low, newest-first, oldest-first
    /// Automatic taxons typically use algorithmic sorts (best-selling, newest, etc.) rather than manual.
    /// </summary>
    public string SortOrder { get; set; } = "manual";
    
    /// <summary>
    /// When true, product list regeneration should be triggered after changes affecting product membership.
    /// This flag is controlled only by rule/automation changes, not by cosmetic changes (images, SEO, names).
    /// Cleared after regeneration is processed.
    /// </summary>
    public bool MarkedForRegenerateTaxonProducts { get; set; }
    #endregion

    #region SEO Properties
    /// <summary>
    /// Custom HTML title tag for this category page. Used by search engines and displayed in browser tabs.
    /// Recommended: 50-60 characters. If empty, uses Name as fallback.
    /// </summary>
    public string? MetaTitle { get; set; }
    
    /// <summary>
    /// Meta description tag for search engines. Shown below link in search results.
    /// Recommended: 150-160 characters. Include primary keywords and clear value proposition.
    /// </summary>
    public string? MetaDescription { get; set; }
    
    /// <summary>
    /// Comma-separated keywords for this category. Used by search engines for relevance.
    /// Example: "men's clothing, apparel, shirts, pants, jackets"
    /// </summary>
    public string? MetaKeywords { get; set; }
    #endregion

    #region Metadata
    /// <summary>
    /// Public metadata: Custom attributes visible/editable in admin UI and potentially exposed via APIs.
    /// Use for: campaign tags, seasonal flags, marketing attributes, custom categorization, etc.
    /// Example: { "campaign": "holiday-2024", "featured": true, "section": "promotion" }
    /// </summary>
    public IDictionary<string, object?>? PublicMetadata { get; set; } = new Dictionary<string, object?>();
    
    /// <summary>
    /// Private metadata: Custom attributes visible only to admins and backend systems.
    /// Use for: internal notes, migration data, integration markers, business rules, etc.
    /// Example: { "legacy_id": "cat-12345", "import_source": "shopify", "rule_config": {...} }
    /// Never exposed via public APIs.
    /// </summary>
    public IDictionary<string, object?>? PrivateMetadata { get; set; } = new Dictionary<string, object?>();
    #endregion

    #region Relationships
    /// <summary>
    /// Reference to parent Taxonomy. Foreign key to Taxonomy aggregate.
    /// Links this taxon to its taxonomy hierarchy (e.g., "Product Categories", "Colors", "Sizes").
    /// </summary>
    public Guid TaxonomyId { get; set; }
    
    /// <summary>
    /// Navigation property: the parent Taxonomy aggregate.
    /// </summary>
    public Taxonomy Taxonomy { get; set; } = null!;
    
    /// <summary>
    /// Optional reference to parent Taxon for hierarchical structure.
    /// Null indicates this is a root taxon.
    /// </summary>
    public Guid? ParentId { get; set; }
    
    /// <summary>
    /// Navigation property: parent taxon (if not root).
    /// Enables upward navigation in hierarchy.
    /// </summary>
    public Taxon? Parent { get; set; }
    
    /// <summary>
    /// Collection of direct child taxons.
    /// Enables downward navigation in hierarchy. Lazy-loaded on access.
    /// </summary>
    public ICollection<Taxon> Children { get; set; } = new List<Taxon>();
    
    /// <summary>
    /// Category images (thumbnails, banners, hero images, etc.).
    /// Multiple images with different "Type" values allow storefront to select appropriate image.
    /// Types: "default" (primary), "square" (thumbnail), "banner" (large hero), etc.
    /// </summary>
    public ICollection<TaxonImage> TaxonImages { get; set; } = new List<TaxonImage>();
    
    /// <summary>
    /// Product classifications linking products to this taxon (many-to-many).
    /// Represents product membership in this category (manual assignment).
    /// </summary>
    public ICollection<Classification> Classifications { get; set; } = new List<Classification>();
    
    /// <summary>
    /// Rules defining automatic product membership (for automatic taxons).
    /// Products matching all/any rules (based on RulesMatchPolicy) are auto-assigned.
    /// </summary>
    public ICollection<TaxonRule> TaxonRules { get; set; } = new List<TaxonRule>();
    
    /// <summary>
    /// Promotion rules that reference this taxon (e.g., "discount applies to this category").
    /// </summary>
    public ICollection<PromotionRuleTaxon> PromotionRuleTaxons { get; set; } = new List<PromotionRuleTaxon>();
    #endregion

    #region Computed Properties
    /// <summary>
    /// True if this is a root taxon (no parent). Only root taxon has ParentId == null.
    /// Used to identify top-level categories in taxonomy.
    /// </summary>
    public bool IsRoot => ParentId == null;
    
    /// <summary>
    /// SEO-friendly title for page. Falls back to Name if MetaTitle is empty.
    /// Used in HTML title tag and browser tab.
    /// </summary>
    public string SeoTitle => !string.IsNullOrWhiteSpace(value: MetaTitle) ? MetaTitle : Name;

    /// <summary>
    /// Primary category image (Type="default"). Used as the main thumbnail/icon.
    /// Returns first matching or null if no default image assigned.
    /// </summary>
    public TaxonImage? Image => TaxonImages.FirstOrDefault(predicate: a => a.Type == "default");
    
    /// <summary>
    /// Square format image (Type="square"). Often used for grid/thumbnail displays.
    /// Typically 1:1 aspect ratio, optimized for small displays.
    /// </summary>
    public TaxonImage? SquareImage => TaxonImages.FirstOrDefault(predicate: a => a.Type == "square");
    
    /// <summary>
    /// Best available image for page builder. Prefers square (if available) then falls back to primary.
    /// Page builders often need square images for uniform layouts.
    /// </summary>
    public TaxonImage? PageBuilderImage => SquareImage ?? Image;
    
    /// <summary>
    /// True if this is a manual (not automatic) taxon.
    /// Manual taxons: products manually assigned by editors.
    /// </summary>
    public bool IsManual => !Automatic;
    
    /// <summary>
    /// True if sort order is "manual" (editor-determined positioning).
    /// False for algorithmic sorts (best-selling, alphabetical, etc.).
    /// </summary>
    public bool IsManualSortOrder => SortOrder == "manual";
    #endregion

    #region Constructors
    private Taxon() { }
    #endregion

    #region Factory
    /// <summary>
    /// Factory method for creating a new Taxon.
    /// Initializes nested set values to 0,0 (requires database-side computation during save).
    /// Raises Created domain event.
    /// </summary>
    /// <remarks>
    /// <strong>Usage Examples:</strong>
    /// <code>
    /// // Create a root category (no parent)
    /// var apparel = Taxon.Create(
    ///     taxonomyId: mainCatalog.Id,
    ///     name: "apparel",
    ///     parentId: null,  // Root node
    ///     presentation: "Apparel");
    /// 
    /// // Create a manual subcategory
    /// var mens = Taxon.Create(
    ///     taxonomyId: mainCatalog.Id,
    ///     name: "mens-clothing",
    ///     parentId: apparel.Id,
    ///     presentation: "Men's Clothing",
    ///     position: 10);
    /// 
    /// // Create an automatic "Best Sellers" category
    /// var bestSellers = Taxon.Create(
    ///     taxonomyId: mainCatalog.Id,
    ///     name: "best-sellers",
    ///     parentId: null,
    ///     presentation: "Best Sellers",
    ///     automatic: true,
    ///     rulesMatchPolicy: "all",
    ///     sortOrder: "best-selling");
    /// </code>
    /// </remarks>
    public static ErrorOr<Taxon> Create(
        Guid taxonomyId,
        string name,
        Guid? parentId,
        string? presentation = null,
        string? description = null,
        int position = 0,
        bool hideFromNav = false,
        bool automatic = false,
        string? rulesMatchPolicy = null,
        string? sortOrder = null,
        string? metaTitle = null,
        string? metaDescription = null,
        string? metaKeywords = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        (name, presentation) = HasParameterizableName.NormalizeParams(name: name, presentation: presentation);

        var taxon = new Taxon
        {
            Id = Guid.NewGuid(),
            Name = name,
            Presentation = presentation ?? name,
            TaxonomyId = taxonomyId,
            ParentId = parentId,
            Description = description,
            Position = position,
            HideFromNav = hideFromNav,
            Automatic = automatic,
            RulesMatchPolicy = automatic ? (rulesMatchPolicy ?? "all") : "all",
            SortOrder = automatic ? (sortOrder ?? "manual") : "manual",
            MetaTitle = metaTitle,
            MetaDescription = metaDescription,
            MetaKeywords = metaKeywords,
            Lft = 0,
            Rgt = 0,
            Depth = 0,
            PublicMetadata = publicMetadata ?? new Dictionary<string, object?>(),
            PrivateMetadata = privateMetadata ?? new Dictionary<string, object?>(),
            MarkedForRegenerateTaxonProducts = false
        };

        taxon.CreatedAt = DateTimeOffset.UtcNow;
        taxon.RegeneratePermalinkAndPrettyName(parentPermalink: null, parentPrettyName: null);

        taxon.AddDomainEvent(domainEvent: new Events.Created(TaxonId: taxon.Id, TaxonomyId: taxon.TaxonomyId));
        return taxon;
    }
    #endregion

    #region Business Logic - Hierarchy Management
    /// <summary>
    /// Update taxon properties. Intelligently tracks changes to regenerate products only when rules/automation change.
    /// Cosmetic changes (name, SEO, images, metadata, position) do NOT trigger product regeneration.
    /// ONLY automation/rule changes set MarkedForRegenerateTaxonProducts flag.
    /// </summary>
    /// <remarks>
    /// <strong>Product Regeneration Strategy:</strong>
    /// <list type="bullet">
    /// <item>
    /// <term>Changes that DON'T regenerate:</term>
    /// <description>name, presentation, description, position, images, SEO, metadata, hideFromNav, parent move</description>
    /// </item>
    /// <item>
    /// <term>Changes that DO regenerate:</term>
    /// <description>automatic flag, rulesMatchPolicy, sortOrder (if precomputed/cached)</description>
    /// </item>
    /// </list>
    /// Regeneration is accomplished by domain events (Events.RegenerateProducts) that handlers consume.
    /// </remarks>
    public ErrorOr<Taxon> Update(
        string? name = null,
        string? presentation = null,
        Guid? parentId = null,
        string? description = null,
        int? position = null,
        bool? hideFromNav = null,
        bool? automatic = null,
        string? rulesMatchPolicy = null,
        string? sortOrder = null,
        string? metaTitle = null,
        string? metaDescription = null,
        string? metaKeywords = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        (name, presentation) = HasParameterizableName.NormalizeParams(name: name, presentation: presentation);

        var nameOrPresentationChanged = false;
        var changed = false;
        // Name / Presentation
        if (!string.IsNullOrEmpty(value: name) && name != Name)
        {
            Name = name;
            nameOrPresentationChanged = true;
            changed = true;
        }

        if (!string.IsNullOrEmpty(value: presentation) && presentation != Presentation)
        {
            Presentation = presentation;
            nameOrPresentationChanged = true;
            changed = true;
        }

        // Parent change (moves hierarchy). This may or may not affect product membership depending on domain rules.
        if (parentId != ParentId)
        {
            var setParentResult = SetParent(newParentId: parentId, newIndex: position ?? Position);
            if (setParentResult.IsError)
                return setParentResult.Errors;
            changed = true;
        }

        // Metadata
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

        // Other simple scalar fields
        if (description != null && description != Description) { Description = description; changed = true; }
        if (position.HasValue && position.Value != Position) { Position = position.Value; changed = true; }
        if (hideFromNav.HasValue && hideFromNav.Value != HideFromNav) { HideFromNav = hideFromNav.Value; changed = true; }
        if (metaTitle != null && metaTitle != MetaTitle) { MetaTitle = metaTitle; changed = true; }
        if (metaDescription != null && metaDescription != MetaDescription) { MetaDescription = metaDescription; changed = true; }
        if (metaKeywords != null && metaKeywords != MetaKeywords) { MetaKeywords = metaKeywords; changed = true; }

        // Automatic / rules-policy / sort-order: these affect product membership and should mark for regeneration.
        var finalAutomatic = automatic ?? Automatic;
        var finalRulesMatchPolicy = finalAutomatic ? (rulesMatchPolicy ?? RulesMatchPolicy) : "all";
        var finalSortOrder = finalAutomatic ? (sortOrder ?? SortOrder) : "manual";

        if (automatic.HasValue && automatic.Value != Automatic)
        {
            Automatic = automatic.Value;
            MarkedForRegenerateTaxonProducts = true;
            changed = true;
        }

        if (finalRulesMatchPolicy != RulesMatchPolicy)
        {
            RulesMatchPolicy = finalRulesMatchPolicy;
            MarkedForRegenerateTaxonProducts = true;
            changed = true;
        }

        // Note: SortOrder should only force regeneration if your product listing is precomputed / cached
        if (finalSortOrder != SortOrder)
        {
            SortOrder = finalSortOrder;
            // Optionally regenerate products when sort order changes — controlled by domain decision.
            // If your system caches ordered product lists per taxon, enable regeneration; otherwise skip.
            // MarkedForRegenerateTaxonProducts = true;
            changed = true;
        }

        // If something changed, raise Updated
        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(domainEvent: new Events.Updated(TaxonId: Id, TaxonomyId: TaxonomyId, NameOrPresentationChanged: nameOrPresentationChanged));
        }

        // Emit RegenerateProducts only when the flag is set (rule/automation related changes).
        if (MarkedForRegenerateTaxonProducts)
        {
            AddDomainEvent(domainEvent: new Events.RegenerateProducts(TaxonId: Id));
            MarkedForRegenerateTaxonProducts = false; // reset
        }

        return this;
    }

    public ErrorOr<Taxon> SetParent(Guid? newParentId, int newIndex)
    {
        if (Id == newParentId)
            return Errors.SelfParenting;

        var oldParentId = ParentId;
        ParentId = newParentId;
        this.SetPosition(position: newIndex);

        AddDomainEvent(domainEvent: new Events.Moved(TaxonId: Id, TaxonomyId: TaxonomyId, OldParentId: oldParentId, NewParentId: newParentId, NewIndex: Position));
        return this;
    }

    public ErrorOr<Taxon> UpdateNestedSet(int lft, int rgt, int depth)
    {
        Lft = lft;
        Rgt = rgt;
        Depth = depth;
        return this;
    }

    public ErrorOr<Taxon> RegeneratePermalinkAndPrettyName(string? parentPermalink, string? parentPrettyName)
    {
        Permalink = GeneratePermalink(parentPermalink: parentPermalink);
        PrettyName = GeneratePrettyName(parentPrettyName: parentPrettyName);
        return this;
    }

    public ErrorOr<Deleted> Delete()
    {
        if (Children.Any())
            return Errors.HasChildren;

        AddDomainEvent(domainEvent: new Events.Deleted(TaxonId: Id, TaxonomyId: TaxonomyId));
        return Result.Deleted;
    }

    public ErrorOr<Success> SetChildIndex(int index)
    {
        Position = index;
        return Result.Success;
    }
    #endregion

    #region Helpers
    #region Rules
    /// <summary>
    /// Adds a pre-created TaxonRule to this taxon. Adding rules triggers product regeneration.
    /// </summary>
    public ErrorOr<TaxonRule> AddTaxonRule(TaxonRule? rule)
    {
        if (rule is null)
            return TaxonRule.Errors.Required;

        if (rule.TaxonId != Id)
            return TaxonRule.Errors.TaxonMismatch(id: rule.TaxonId, taxonId: Id);

        if (TaxonRules.Any(predicate: r =>
                r.Type == rule.Type &&
                r.Value == rule.Value &&
                r.MatchPolicy == rule.MatchPolicy &&
                r.PropertyName == rule.PropertyName))
        {
            return TaxonRule.Errors.Duplicate;
        }

        TaxonRules.Add(item: rule);
        // Mark & emit regeneration so downstream consumers respond immediately to rule changes
        MarkedForRegenerateTaxonProducts = true;
        AddDomainEvent(domainEvent: new Events.Updated(TaxonId: Id, TaxonomyId: TaxonomyId, NameOrPresentationChanged: false));
        AddDomainEvent(domainEvent: new Events.RegenerateProducts(TaxonId: Id));
        MarkedForRegenerateTaxonProducts = false;

        return rule;
    }

    public ErrorOr<Taxon> RemoveRule(Guid ruleId)
    {
        var rule = TaxonRules.FirstOrDefault(predicate: r => r.Id == ruleId);
        if (rule == null)
            return TaxonRule.Errors.NotFound(id: ruleId);

        TaxonRules.Remove(item: rule);
        MarkedForRegenerateTaxonProducts = true;
        AddDomainEvent(domainEvent: new Events.RegenerateProducts(TaxonId: Id));
        MarkedForRegenerateTaxonProducts = false;

        return this;
    }
    #endregion

    #region Image
    public ErrorOr<Success> AddImage(TaxonImage image)
    {
        var existingImage = TaxonImages.FirstOrDefault(predicate: a => a.Type == image.Type);
        if (existingImage != null)
            TaxonImages.Remove(item: existingImage);

        TaxonImages.Add(item: image);
        AddDomainEvent(domainEvent: new Events.Updated(TaxonId: Id, TaxonomyId: TaxonomyId, NameOrPresentationChanged: false));
        return Result.Success;
    }

    public ErrorOr<Success> RemoveImage(Guid id)
    {
        var image = TaxonImages.FirstOrDefault(predicate: a => a.Id == id);
        if (image == null)
            return Error.NotFound(code: "TaxonImage.NotFound", description: $"Image with id '{id}' not found");

        TaxonImages.Remove(item: image);
        AddDomainEvent(domainEvent: new Events.Updated(TaxonId: Id, TaxonomyId: TaxonomyId, NameOrPresentationChanged: false));
        return Result.Success;
    }
    #endregion

    #region Hierarchy
    public void AddChild(Taxon? child)
    {
        if (child == null || Children.Contains(item: child))
            return;

        if (child.Parent != null && child.Parent != this)
        {
            child.Parent.Children.Remove(item: child);
            child.AddDomainEvent(domainEvent: new Events.Moved(
                TaxonId: child.Id,
                TaxonomyId: child.TaxonomyId,
                OldParentId: child.ParentId,
                NewParentId: Id,
                NewIndex: child.Position));
        }

        Children.Add(item: child);
        child.Parent = this;
        child.ParentId = Id;
        child.AddDomainEvent(domainEvent: new Events.Moved(
            TaxonId: child.Id,
            TaxonomyId: child.TaxonomyId,
            OldParentId: null,
            NewParentId: Id,
            NewIndex: child.Position));
    }

    public void RemoveChild(Taxon? child)
    {
        if (child == null || !Children.Contains(item: child))
            return;

        Children.Remove(item: child);
        child.Parent = null;
        child.ParentId = null;
        child.AddDomainEvent(domainEvent: new Events.Moved(
            TaxonId: child.Id,
            TaxonomyId: child.TaxonomyId,
            OldParentId: Id,
            NewParentId: null,
            NewIndex: child.Position));
    }
    #endregion

    private string GeneratePermalink(string? parentPermalink)
    {
        var slug = string.IsNullOrWhiteSpace(value: Name) ? "unnamed" : Name.ToSlug();
        return string.IsNullOrWhiteSpace(value: parentPermalink) ? slug : $"{parentPermalink.TrimEnd(trimChar: '/')}/{slug}";
    }

    private string GeneratePrettyName(string? parentPrettyName)
    {
        var presentation = string.IsNullOrWhiteSpace(value: Presentation) ? Name : Presentation;
        return string.IsNullOrWhiteSpace(value: parentPrettyName) ? presentation : $"{parentPrettyName} -> {presentation}";
    }
    #endregion

    #region Events
    public static class Events
    {
        public record Created(Guid TaxonId, Guid TaxonomyId) : DomainEvent;
        public record Updated(Guid TaxonId, Guid TaxonomyId, bool NameOrPresentationChanged) : DomainEvent;
        public record Deleted(Guid TaxonId, Guid TaxonomyId) : DomainEvent;
        public record Moved(Guid TaxonId, Guid TaxonomyId, Guid? OldParentId, Guid? NewParentId, int NewIndex) : DomainEvent;
        public record RegenerateProducts(Guid TaxonId) : DomainEvent;
    }
    #endregion
}

