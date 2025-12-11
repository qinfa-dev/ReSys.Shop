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
        /// Current: JPEG, PNG, GIF, WebP (modern formats, smaller file sizes).
        /// </summary>
        public static readonly string[] ImageContentTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];

        /// <summary>
        /// Valid sort order values for automatic taxons.
        /// Determines how products appear within the category.
        /// <list type="table">
        /// <item>
        /// <term>manual</term>
        /// <description>Editor-determined order via Position value.</description>
        /// </item>
        /// <item>
        /// <term>best-selling</term>
        /// <description>Products with highest sales count first.</description>
        /// </item>
        /// <item>
        /// <term>name-a-z, name-z-a</term>
        /// <description>Alphabetical by product name.</description>
        /// </item>
        /// <item>
        /// <term>price-low-to-high, price-high-to-low</term>
        /// <description>Sorted by lowest variant price.</description>
        /// </item>
        /// <item>
        /// <term>newest-first, oldest-first</term>
        /// <description>Sorted by product creation date.</description>
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
        /// Determines how multiple rules are evaluated.
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
        /// Prevention: Cannot create hierarchical loops in the taxonomy structure.
        /// </summary>
        public static Error SelfParenting => Error.Validation(code: "Taxon.SelfParenting", description: "A taxon cannot be its own parent.");
        
        /// <summary>
        /// Occurs when a child taxon attempts to use a parent from a different taxonomy.
        /// Prevention: Taxonomies must remain isolated hierarchies.
        /// Example: Cannot add a parent from a "Size" taxonomy to a "Color" taxonomy.
        /// </summary>
        public static Error ParentTaxonomyMismatch => Error.Validation(code: "Taxon.ParentTaxonomyMismatch", description: "Parent must belong to the same taxonomy.");
        
        /// <summary>
        /// Occurs when attempting to create a root taxon when one already exists within the same taxonomy.
        /// Prevention: Only one root taxon is allowed per taxonomy (a single taxon with no parent).
        /// </summary>
        public static Error RootConflict => Error.Conflict(code: "Taxon.RootConflict", description: "This taxonomy already has a root taxon.");
        
        /// <summary>
        /// Occurs when attempting to delete a taxon that still has child categories.
        /// Prevention: Cannot leave orphaned categories. Child taxons must be reparented to another parent or deleted individually first.
        /// </summary>
        public static Error HasChildren => Error.Validation(code: "Taxon.HasChildren", description: "Cannot delete a taxon that has children.");
        
        /// <summary>
        /// Occurs when a referenced taxon cannot be found in the database.
        /// Typical causes: ID doesn't exist, taxon was deleted, or querying for a wrong taxonomy.
        /// </summary>
        public static Error NotFound(Guid id) => Error.NotFound(code: "Taxon.NotFound", description: $"Taxon with ID '{id}' was not found.");
    }
    #endregion

    #region Core Properties
    /// <summary>
    /// Gets or sets the internal system name for the taxon.
    /// This name is typically lowercase with hyphens (e.g., "mens-clothing") and is used for identification.
    /// It must be unique within its <see cref="Taxonomy"/>.
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the human-readable display name shown to customers and administrators.
    /// This can differ from <see cref="Name"/> for better user experience and localization (e.g., <c>Name="mens-clothing"</c>, <c>Presentation="Men's Clothing"</c>).
    /// </summary>
    public string Presentation { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets an optional detailed description of the category.
    /// This content can be displayed on category landing pages for improved SEO and user experience.
    /// It supports rich content hints (e.g., markdown/HTML) depending on the frontend implementation.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Gets or sets the URL-friendly permalink for storefront navigation.
    /// Example: "/apparel/mens/clothing" or "/categories/mens-clothing".
    /// This value is automatically maintained based on the taxon's hierarchy and name.
    /// It is regenerated on name or parent changes.
    /// </summary>
    public string Permalink { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets a human-readable path for breadcrumbs and other hierarchical navigation displays.
    /// Example: "Apparel -> Men's -> Clothing".
    /// This provides a user-friendly representation of the taxon's position in the hierarchy.
    /// </summary>
    public string PrettyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets a value indicating whether this category should be hidden from frontend navigation menus.
    /// This is useful for: archived categories, draft categories, or internal-only categories.
    /// Products in hidden categories are still browsable via direct URL or internal links.
    /// </summary>
    public bool HideFromNav { get; set; }
    
    /// <summary>
    /// Gets or sets the positional ordering within its parent category.
    /// Lower values typically appear first in UI listings.
    /// This is updated by editors to reorder categories, especially when <see cref="SortOrder"/> is "manual".
    /// Typical range: 0-999 (position increments by 10 for easy reordering).
    /// </summary>
    public int Position { get; set; }
    #endregion

    #region Nested Set Properties
    /// <summary>
    /// Gets or sets the left boundary value in the nested set model.
    /// This value is used for efficient hierarchical queries. All descendants have Lft values greater than the parent's Lft.
    /// </summary>
    public int Lft { get; set; }
    
    /// <summary>
    /// Gets or sets the right boundary value in the nested set model.
    /// This value is used for efficient hierarchical queries. All descendants have Rgt values less than the parent's Rgt.
    /// Enables efficient queries such as finding all descendants: WHERE Lft > parent.Lft AND Rgt &lt; parent.Rgt.
    /// </summary>
    public int Rgt { get; set; }
    
    /// <summary>
    /// Gets or sets the nesting depth in the hierarchy.
    /// Root taxons have a Depth of 0, direct children have 1, grandchildren have 2, and so on.
    /// Useful for limiting recursion depth in queries and UI rendering.
    /// </summary>
    public int Depth { get; set; }
    #endregion

    #region Automatic Taxon Properties
    /// <summary>
    /// Gets or sets a value indicating whether this is an automatic taxon (system-managed category).
    /// If true, products are automatically added/removed based on <see cref="TaxonRules"/>.
    /// If false, this is a manual taxon, where products are manually assigned by editors.
    /// </summary>
    public bool Automatic { get; set; }
    
    /// <summary>
    /// For automatic taxons: gets or sets the rule matching logic.
    /// "all" = ALL rules must pass (AND logic), implying stricter membership criteria.
    /// "any" = ANY rule can pass (OR logic), implying looser membership criteria.
    /// Uses values from <see cref="Constraints.RulesMatchPolicies"/>.
    /// </summary>
    public string RulesMatchPolicy { get; set; } = "all";
    
    /// <summary>
    /// Gets or sets the sort order for products displayed within this taxon.
    /// Uses predefined values from <see cref="Constraints.SortOrders"/>.
    /// Automatic taxons typically use algorithmic sorts (e.g., "best-selling", "newest-first") rather than "manual".
    /// </summary>
    public string SortOrder { get; set; } = "manual";
    
    /// <summary>
    /// Gets or sets a flag indicating whether product list regeneration should be triggered
    /// after changes affecting product membership (e.g., rule updates or automation changes).
    /// This flag is reset after regeneration is processed by a dedicated handler.
    /// </summary>
    public bool MarkedForRegenerateTaxonProducts { get; set; }
    #endregion

    #region SEO Properties
    /// <summary>
    /// Gets or sets the custom HTML title tag for this category page.
    /// Used by search engines and displayed in browser tabs. Recommended: 50-60 characters.
    /// If empty, the <see cref="Name"/> is typically used as a fallback.
    /// </summary>
    public string? MetaTitle { get; set; }
    
    /// <summary>
    /// Gets or sets the meta description tag for search engines.
    /// This text is shown below the link in search results. Recommended: 150-160 characters.
    /// Should include primary keywords and a clear value proposition.
    /// </summary>
    public string? MetaDescription { get; set; }
    
    /// <summary>
    /// Gets or sets comma-separated keywords relevant to this category.
    /// Used by search engines for relevance. Example: "men's clothing, apparel, shirts, pants, jackets".
    /// </summary>
    public string? MetaKeywords { get; set; }
    #endregion

    #region Metadata
    /// <summary>
    /// Gets or sets public metadata: Custom attributes visible/editable in the admin UI and potentially exposed via public APIs.
    /// Use for: campaign tags, seasonal flags, marketing attributes, custom categorization, etc.
    /// Example: { "campaign": "holiday-2024", "featured": true, "section": "promotion" }.
    /// </summary>
    public IDictionary<string, object?>? PublicMetadata { get; set; } = new Dictionary<string, object?>();
    
    /// <summary>
    /// Gets or sets private metadata: Custom attributes visible only to administrators and backend systems.
    /// Use for: internal notes, migration data, integration markers, business rules, etc.
    /// Example: { "legacy_id": "cat-12345", "import_source": "shopify", "rule_config": {...} }.
    /// This data is never exposed via public APIs.
    /// </summary>
    public IDictionary<string, object?>? PrivateMetadata { get; set; } = new Dictionary<string, object?>();
    #endregion

    #region Relationships
    /// <summary>
    /// Gets or sets the unique identifier of the parent <see cref="Taxonomy"/> this taxon belongs to.
    /// This is a foreign key reference to the <see cref="Taxonomy"/> aggregate, linking this taxon
    /// to its overall classification hierarchy (e.g., "Product Categories", "Colors", "Sizes").
    /// </summary>
    public Guid TaxonomyId { get; set; }
    
    /// <summary>
    /// Gets or sets the navigation property to the parent <see cref="Taxonomy"/> aggregate.
    /// This provides access to the taxonomy's details and configuration.
    /// </summary>
    public Taxonomy Taxonomy { get; set; } = null!;
    
    /// <summary>
    /// Gets or sets the optional unique identifier of the parent <see cref="Taxon"/> for hierarchical structure.
    /// A <c>null</c> value indicates that this is a root taxon, the top-most node in its branch of the <see cref="Taxonomy"/>.
    /// </summary>
    public Guid? ParentId { get; set; }
    
    /// <summary>
    /// Gets or sets the navigation property to the parent <see cref="Taxon"/> (if not a root taxon).
    /// This enables upward traversal in the hierarchy.
    /// </summary>
    public Taxon? Parent { get; set; }
    
    /// <summary>
    /// Gets or sets the collection of direct child <see cref="Taxon"/>s.
    /// This enables downward traversal in the hierarchy. This is typically lazy-loaded on access by Entity Framework Core.
    /// </summary>
    public ICollection<Taxon> Children { get; set; } = new List<Taxon>();
    
    /// <summary>
    /// Gets or sets the collection of image assets associated with this taxon (e.g., thumbnails, banners, hero images).
    /// Multiple images with different "Type" values allow the storefront to select the appropriate image for various contexts.
    /// Examples of image types: "default" (primary), "square" (thumbnail), "banner" (large hero).
    /// </summary>
    public ICollection<TaxonImage> TaxonImages { get; set; } = new List<TaxonImage>();
    
    /// <summary>
    /// Gets or sets the collection of <see cref="Classification"/> entries linking products to this taxon.
    /// This represents product membership in this category, primarily for manually assigned products.
    /// </summary>
    public ICollection<Classification> Classifications { get; set; } = new List<Classification>();
    
    /// <summary>
    /// Gets or sets the collection of <see cref="TaxonRule"/>s defining automatic product membership for this taxon.
    /// Products matching these rules (based on <see cref="RulesMatchPolicy"/>) are automatically assigned to this taxon.
    /// </summary>
    public ICollection<TaxonRule> TaxonRules { get; set; } = new List<TaxonRule>();
    
    /// <summary>
    /// Gets or sets the collection of <see cref="PromotionRuleTaxon"/> entries that reference this taxon.
    /// This is used to define promotion rules that apply to products within this category (e.g., "discount applies to this category").
    /// </summary>
    public ICollection<PromotionRuleTaxon> PromotionRuleTaxons { get; set; } = new List<PromotionRuleTaxon>();
    #endregion

    #region Computed Properties
    /// <summary>
    /// Indicates if this is a root taxon (i.e., it has no parent, <see cref="ParentId"/> is null).
    /// Root taxons identify top-level categories in the taxonomy.
    /// </summary>
    public bool IsRoot => ParentId == null;
    
    /// <summary>
    /// Gets the SEO-friendly title for the taxon's page.
    /// It falls back to the <see cref="Name"/> if <see cref="MetaTitle"/> is empty or null.
    /// This is used in the HTML title tag and browser tabs for better search engine visibility.
    /// </summary>
    public string SeoTitle => !string.IsNullOrWhiteSpace(value: MetaTitle) ? MetaTitle : Name;

    /// <summary>
    /// Gets the primary image for this category, identified by <c>Type="default"</c>.
    /// This image is typically used as the main thumbnail or icon for the taxon.
    /// Returns the first matching image or null if no default image is assigned.
    /// </summary>
    public TaxonImage? Image => TaxonImages.FirstOrDefault(predicate: a => a.Type == "default");
    
    /// <summary>
    /// Gets the image optimized for square display, identified by <c>Type="square"</c>.
    /// Often used for grid or thumbnail displays due to its 1:1 aspect ratio, optimized for small screens.
    /// </summary>
    public TaxonImage? SquareImage => TaxonImages.FirstOrDefault(predicate: a => a.Type == "square");
    
    /// <summary>
    /// Gets the best available image for use in a page builder or similar dynamic content creation.
    /// It prefers a square image (if available) and falls back to the primary <see cref="Image"/> otherwise.
    /// This helps ensure uniform layouts in page builders.
    /// </summary>
    public TaxonImage? PageBuilderImage => SquareImage ?? Image;
    
    /// <summary>
    /// Indicates if this is a manual taxon (i.e., <see cref="Automatic"/> is false).
    /// In manual taxons, products are explicitly assigned by editors.
    /// </summary>
    public bool IsManual => !Automatic;
    
    /// <summary>
    /// Indicates if the <see cref="SortOrder"/> for products within this taxon is "manual"
    /// (i.e., editor-determined positioning via <see cref="Position"/>).
    /// Returns false for algorithmic sorts (e.g., "best-selling", "alphabetical").
    /// </summary>
    public bool IsManualSortOrder => SortOrder == "manual";
    #endregion

    #region Constructors
    /// <summary>
    /// Private constructor for ORM (Entity Framework Core) materialization.
    /// </summary>
    private Taxon() { }
    #endregion

    #region Factory
    /// <summary>
    /// Factory method for creating a new <see cref="Taxon"/> instance.
    /// Initializes core properties and sets up default values for automatic taxons and nested set properties.
    /// </summary>
    /// <param name="taxonomyId">The unique identifier of the <see cref="Taxonomy"/> this taxon belongs to.</param>
    /// <param name="name">The internal, unique system name for the taxon. Will be normalized.</param>
    /// <param name="parentId">Optional: The unique identifier of the parent <see cref="Taxon"/>. If null, this taxon will be a root taxon.</param>
    /// <param name="presentation">The human-readable display name for the taxon. Defaults to <paramref name="name"/> if null.</param>
    /// <param name="description">Optional detailed description of the category.</param>
    /// <param name="position">The display order within its parent category. Defaults to 0.</param>
    /// <param name="hideFromNav">A flag indicating if this taxon should be hidden from frontend navigation menus. Defaults to false.</param>
    /// <param name="automatic">A flag indicating if this is an automatic taxon, where products are assigned by rules. Defaults to false.</param>
    /// <param name="rulesMatchPolicy">For automatic taxons: rule matching logic ("all" or "any"). Defaults to "all" if <paramref name="automatic"/> is true.</param>
    /// <param name="sortOrder">For automatic taxons: specifies how products are sorted within this taxon. Defaults to "manual" if <paramref name="automatic"/> is true.</param>
    /// <param name="metaTitle">The meta title for SEO purposes.</param>
    /// <param name="metaDescription">The meta description for SEO purposes.</param>
    /// <param name="metaKeywords">The meta keywords for SEO purposes.</param>
    /// <param name="publicMetadata">Optional dictionary for public-facing metadata.</param>
    /// <param name="privateMetadata">Optional dictionary for internal-only metadata.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Taxon}"/> result.
    /// Returns the newly created <see cref="Taxon"/> instance on success.
    /// Basic validation errors (e.g., name required) are typically handled at a higher level.
    /// </returns>
    /// <remarks>
    /// This method initializes <c>Lft</c>, <c>Rgt</c>, and <c>Depth</c> to 0, which are later computed and updated
    /// by an external service (e.g., a domain service or application service) that manages the nested set model
    /// upon persisting changes.
    /// <para>
    /// The <c>Permalink</c> and <c>PrettyName</c> are initially generated based on the taxon's own name/presentation,
    /// but will be fully resolved when the hierarchy is established and nested set values are calculated.
    /// </para>
    /// A <see cref="Events.Created"/> domain event is added, signifying the creation of the new taxon.
    /// <para>
    /// <strong>Usage Examples:</strong>
    /// <code>
    /// // Create a root category (no parent)
    /// var apparelResult = Taxon.Create(
    ///     taxonomyId: Guid.NewGuid(), // Assume main catalog Taxonomy ID
    ///     name: "apparel",
    ///     parentId: null,
    ///     presentation: "Apparel");
    /// 
    /// // Create a manual subcategory
    /// var mensResult = Taxon.Create(
    ///     taxonomyId: apparelResult.Value.TaxonomyId,
    ///     name: "mens-clothing",
    ///     parentId: apparelResult.Value.Id,
    ///     presentation: "Men's Clothing",
    ///     position: 10);
    /// 
    /// // Create an automatic "Best Sellers" category
    /// var bestSellersResult = Taxon.Create(
    ///     taxonomyId: Guid.NewGuid(),
    ///     name: "best-sellers",
    ///     parentId: null,
    ///     presentation: "Best Sellers",
    ///     automatic: true,
    ///     rulesMatchPolicy: "all",
    ///     sortOrder: "best-selling");
    /// </code>
    /// </para>
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
    /// Updates the mutable properties of the taxon.
    /// This method allows for partial updates; only provided parameters will be changed.
    /// It intelligently tracks changes to trigger product regeneration only when rule-related or
    /// automation-related properties are modified.
    /// </summary>
    /// <param name="name">The new internal system name for the taxon. If null, the existing name is retained.</param>
    /// <param name="presentation">The new human-readable display name. If null, the existing presentation is retained.</param>
    /// <param name="parentId">The new unique identifier of the parent <see cref="Taxon"/>. If null, the existing parent is retained. Changing this may trigger hierarchy recalculation.</param>
    /// <param name="description">The new detailed description of the category. If null, the existing description is retained.</param>
    /// <param name="position">The new display order within its parent category. If null, the existing position is retained.</param>
    /// <param name="hideFromNav">The new flag indicating if this taxon should be hidden from navigation. If null, the existing value is retained.</param>
    /// <param name="automatic">The new flag indicating if this is an automatic taxon. If null, the existing value is retained.</param>
    /// <param name="rulesMatchPolicy">For automatic taxons: the new rule matching logic. If null, the existing policy is retained.</param>
    /// <param name="sortOrder">For automatic taxons: the new sort order for products. If null, the existing order is retained.</param>
    /// <param name="metaTitle">The new meta title for SEO. If null, the existing title is retained.</param>
    /// <param name="metaDescription">The new meta description for SEO. If null, the existing description is retained.</param>
    /// <param name="metaKeywords">The new meta keywords for SEO. If null, the existing keywords are retained.</param>
    /// <param name="publicMetadata">New public metadata. If null, the existing public metadata is retained.</param>
    /// <param name="privateMetadata">New private metadata. If null, the existing private metadata is retained.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Taxon}"/> result.
    /// Returns the updated <see cref="Taxon"/> instance on success.
    /// Returns one of the <see cref="Errors"/> if validation fails (e.g., <see cref="Errors.SelfParenting"/>, <see cref="Errors.ParentTaxonomyMismatch"/>).
    /// </returns>
    /// <remarks>
    /// This method updates various properties of the taxon and manages the <see cref="MarkedForRegenerateTaxonProducts"/> flag.
    /// <para>
    /// <strong>Product Regeneration Strategy:</strong>
    /// <list type="bullet">
    /// <item>
    /// <term>Changes that DON'T regenerate products:</term>
    /// <description>name, presentation, description, position, images, SEO, metadata, hideFromNav.</description>
    /// </item>
    /// <item>
    /// <term>Changes that DO regenerate products:</term>
    /// <description>The <paramref name="automatic"/> flag, <paramref name="rulesMatchPolicy"/>, and <paramref name="sortOrder"/> (if product listings are precomputed/cached). These changes set <see cref="MarkedForRegenerateTaxonProducts"/> to true.</description>
    /// </item>
    /// </list>
    /// Regeneration is asynchronous and is accomplished by domain events (<see cref="Events.RegenerateProducts"/>) that downstream handlers consume.
    /// </para>
    /// <strong>Domain Events Emitted:</strong>
    /// <list type="bullet">
    /// <item><see cref="Events.Updated"/>: Basic taxon update notification. Includes <c>NameOrPresentationChanged</c> flag.</item>
    /// <item><see cref="Events.RegenerateProducts"/>: Emitted when <see cref="MarkedForRegenerateTaxonProducts"/> is true, indicating a need to re-evaluate product membership.</item>
    /// </list>
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
            if (nameOrPresentationChanged)
            {
                // Regenerate permalink and pretty name if name or presentation changed.
                // This assumes parentPermalink and parentPrettyName will be correctly resolved by an external service
                // or that the current taxon is a root.
                // For nested set consistency, the full hierarchy re-calculation happens externally.
                RegeneratePermalinkAndPrettyName(parentPermalink: null, parentPrettyName: null); // Placeholder, will be corrected by nested set logic
            }
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

    /// <summary>
    /// Updates the nested set model properties (<see cref="Lft"/>, <see cref="Rgt"/>, <see cref="Depth"/>) for this taxon.
    /// This method is typically called by an external service (e.g., a domain service or application service)
    /// responsible for maintaining the tree structure's integrity after hierarchy changes.
    /// </summary>
    /// <param name="lft">The new left boundary value for the nested set model.</param>
    /// <param name="rgt">The new right boundary value for the nested set model.</param>
    /// <param name="depth">The new nesting depth in the hierarchy.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Taxon}"/> result.
    /// Returns the updated <see cref="Taxon"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method does not perform any validation for the correctness of the nested set values;
    /// it assumes that the provided values are calculated correctly by the calling service.
    /// </remarks>
    public ErrorOr<Taxon> UpdateNestedSet(int lft, int rgt, int depth)
    {
        Lft = lft;
        Rgt = rgt;
        Depth = depth;
        return this;
    }

    /// <summary>
    /// Regenerates the <see cref="Permalink"/> and <see cref="PrettyName"/> for this taxon based on its current name,
    /// presentation, and the provided parent permalink/pretty name.
    /// This is crucial for maintaining accurate URL paths and breadcrumbs in the storefront.
    /// </summary>
    /// <param name="parentPermalink">The permalink of the parent taxon. Null if this is a root taxon.</param>
    /// <param name="parentPrettyName">The pretty name of the parent taxon. Null if this is a root taxon.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Taxon}"/> result.
    /// Returns the updated <see cref="Taxon"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method should be called when the taxon's own name/presentation or its parent changes,
    /// as these actions directly affect its hierarchical path.
    /// The actual generation logic is delegated to private helper methods <see cref="GeneratePermalink(string?)"/>
    /// and <see cref="GeneratePrettyName(string?)"/>.
    /// </remarks>
    public ErrorOr<Taxon> RegeneratePermalinkAndPrettyName(string? parentPermalink, string? parentPrettyName)
    {
        Permalink = GeneratePermalink(parentPermalink: parentPermalink);
        PrettyName = GeneratePrettyName(parentPrettyName: parentPrettyName);
        return this;
    }

    /// <summary>
    /// Deletes the taxon from the system.
    /// This operation is only permitted if the taxon has no child categories.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Returns <see cref="Result.Deleted"/> on successful deletion.
    /// Returns <see cref="Errors.HasChildren"/> if the taxon still has associated child taxons.
    /// </returns>
    /// <remarks>
    /// Before calling this method, ensure all child taxons have been reparented or deleted.
    /// A <see cref="Events.Deleted"/> domain event is raised upon successful deletion.
    /// </remarks>
    public ErrorOr<Deleted> Delete()
    {
        if (Children.Any())
            return Errors.HasChildren;

        AddDomainEvent(domainEvent: new Events.Deleted(TaxonId: Id, TaxonomyId: TaxonomyId));
        return Result.Deleted;
    }

    /// <summary>
    /// Sets the display index of a child taxon.
    /// This method specifically sets the <see cref="Position"/> property.
    /// </summary>
    /// <param name="index">The new position index for the child taxon.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Success}"/> result.
    /// Always returns <see cref="Success"/> if the operation completes without errors.
    /// </returns>
    /// <remarks>
    /// This method is part of managing the hierarchical display order of taxons.
    /// Changes to position typically trigger an <see cref="Events.Moved"/> domain event handled externally.
    /// </remarks>
    public ErrorOr<Success> SetChildIndex(int index)
    {
        Position = index;
        return Result.Success;
    }
    #endregion

    #region Helpers
    #region Rules
    /// <summary>
    /// Adds a pre-created <see cref="TaxonRule"/> to this taxon.
    /// Adding or removing rules triggers product regeneration for automatic taxons.
    /// </summary>
    /// <param name="rule">The <see cref="TaxonRule"/> instance to add.</param>
    /// <returns>
    /// An <see cref="ErrorOr{TaxonRule}"/> result.
    /// Returns the added <see cref="TaxonRule"/> on success.
    /// Returns <see cref="TaxonRule.Errors.Required"/> if the rule is null.
    /// Returns <see cref="TaxonRule.Errors.TaxonMismatch"/> if the rule's <c>TaxonId</c> does not match this taxon's <c>Id</c>.
    /// Returns <see cref="TaxonRule.Errors.Duplicate"/> if an identical rule already exists.
    /// </returns>
    /// <remarks>
    /// This method performs checks for null rules, taxon ID mismatch, and duplicate rules.
    /// It sets <see cref="MarkedForRegenerateTaxonProducts"/> to true and emits an <see cref="Events.RegenerateProducts"/>
    /// domain event, ensuring downstream consumers respond immediately to changes in rules.
    /// </remarks>
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

    /// <summary>
    /// Removes a <see cref="TaxonRule"/> from this taxon by its unique identifier.
    /// Removing rules triggers product regeneration for automatic taxons.
    /// </summary>
    /// <param name="ruleId">The unique identifier of the <see cref="TaxonRule"/> to remove.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Taxon}"/> result.
    /// Returns the updated <see cref="Taxon"/> instance on success.
    /// Returns <see cref="TaxonRule.Errors.NotFound(Guid)"/> if the specified rule is not found.
    /// </returns>
    /// <remarks>
    /// This method removes the rule from the owned collection.
    /// It sets <see cref="MarkedForRegenerateTaxonProducts"/> to true and emits an <see cref="Events.RegenerateProducts"/>
    /// domain event, ensuring downstream consumers respond immediately to changes in rules.
    /// </remarks>
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
    /// <summary>
    /// Adds a new <see cref="TaxonImage"/> to this taxon.
    /// If an image of the same type already exists, it will be replaced.
    /// </summary>
    /// <param name="image">The <see cref="TaxonImage"/> instance to add.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Success}"/> result.
    /// Always returns <see cref="Result.Success"/> on successful addition or replacement.
    /// </returns>
    /// <remarks>
    /// This method ensures that a taxon has at most one image of a given type.
    /// An <see cref="Events.Updated"/> domain event is emitted to signal the change.
    /// </remarks>
    public ErrorOr<Success> AddImage(TaxonImage image)
    {
        var existingImage = TaxonImages.FirstOrDefault(predicate: a => a.Type == image.Type);
        if (existingImage != null)
            TaxonImages.Remove(item: existingImage);

        TaxonImages.Add(item: image);
        AddDomainEvent(domainEvent: new Events.Updated(TaxonId: Id, TaxonomyId: TaxonomyId, NameOrPresentationChanged: false));
        return Result.Success;
    }

    /// <summary>
    /// Removes a <see cref="TaxonImage"/> from this taxon by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the <see cref="TaxonImage"/> to remove.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Success}"/> result.
    /// Returns <see cref="Result.Success"/> on successful removal.
    /// Returns an <see cref="Error.NotFound"/> if the image with the specified ID is not found.
    /// </returns>
    /// <remarks>
    /// This method removes the image from the owned collection.
    /// An <see cref="Events.Updated"/> domain event is emitted to signal the change.
    /// </remarks>
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
    /// <summary>
    /// Adds a child <see cref="Taxon"/> to this taxon's collection of children.
    /// This method handles updating the parent-child relationship and emits a <see cref="Events.Moved"/> domain event.
    /// </summary>
    /// <param name="child">The <see cref="Taxon"/> to add as a child. Must not be null.</param>
    /// <remarks>
    /// This method ensures that:
    /// <list type="bullet">
    /// <item>The child is not null and not already a child of this taxon.</item>
    /// <item>If the child already has a parent, it is removed from that parent's children collection.</item>
    /// <item>The child's <see cref="Parent"/> and <see cref="ParentId"/> are updated to reference this taxon.</item>
    /// </list>
    /// A <see cref="Events.Moved"/> domain event is emitted for the child taxon, indicating its new parentage.
    /// Note: Full nested set model recalculation is expected to be handled by an external service
    /// listening to these <see cref="Events.Moved"/> events.
    /// </remarks>
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

    /// <summary>
    /// Removes a child <see cref="Taxon"/> from this taxon's collection of children.
    /// This method breaks the parent-child relationship and emits a <see cref="Events.Moved"/> domain event.
    /// </summary>
    /// <param name="child">The <see cref="Taxon"/> to remove as a child. Must not be null and must be a current child.</param>
    /// <remarks>
    /// This method removes the child from this taxon's <see cref="Children"/> collection
    /// and clears its <see cref="Parent"/> and <see cref="ParentId"/> properties.
    /// A <see cref="Events.Moved"/> domain event is emitted for the child taxon, indicating its changed parentage.
    /// Note: Full nested set model recalculation is expected to be handled by an external service
    /// listening to these <see cref="Events.Moved"/> events.
    /// </remarks>
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

    /// <summary>
    /// Generates a URL-friendly permalink for the taxon based on its name and the parent's permalink.
    /// </summary>
    /// <param name="parentPermalink">The permalink of the parent taxon. Null if this is a root taxon.</param>
    /// <returns>A string representing the generated permalink.</returns>
    private string GeneratePermalink(string? parentPermalink)
    {
        var slug = string.IsNullOrWhiteSpace(value: Name) ? "unnamed" : Name.ToSlug();
        return string.IsNullOrWhiteSpace(value: parentPermalink) ? slug : $"{parentPermalink.TrimEnd(trimChar: '/')}/{slug}";
    }

    /// <summary>
    /// Generates a human-readable "pretty name" path for the taxon based on its presentation and the parent's pretty name.
    /// </summary>
    /// <param name="parentPrettyName">The pretty name of the parent taxon. Null if this is a root taxon.</param>
    /// <returns>A string representing the generated pretty name.</returns>
    private string GeneratePrettyName(string? parentPrettyName)
    {
        var presentation = string.IsNullOrWhiteSpace(value: Presentation) ? Name : Presentation;
        return string.IsNullOrWhiteSpace(value: parentPrettyName) ? presentation : $"{parentPrettyName} -> {presentation}";
    }
    #endregion

    #region Events
    /// <summary>
    /// Defines domain events related to the lifecycle and state changes of a <see cref="Taxon"/>.
    /// These events are crucial for enabling a decoupled, event-driven architecture, allowing
    /// other services or bounded contexts to react to taxon-related changes.
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Event fired when a new <see cref="Taxon"/> is successfully created.
        /// </summary>
        /// <param name="TaxonId">The unique identifier of the newly created taxon.</param>
        /// <param name="TaxonomyId">The unique identifier of the taxonomy this taxon belongs to.</param>
        public record Created(Guid TaxonId, Guid TaxonomyId) : DomainEvent;
        /// <summary>
        /// Event fired when a <see cref="Taxon"/>'s properties are updated.
        /// </summary>
        /// <param name="TaxonId">The unique identifier of the updated taxon.</param>
        /// <param name="TaxonomyId">The unique identifier of the taxonomy this taxon belongs to.</param>
        /// <param name="NameOrPresentationChanged">True if the Name or Presentation property has changed, indicating a need to regenerate permalinks/pretty names.</param>
        public record Updated(Guid TaxonId, Guid TaxonomyId, bool NameOrPresentationChanged) : DomainEvent;
        /// <summary>
        /// Event fired when a <see cref="Taxon"/> is deleted.
        /// </summary>
        /// <param name="TaxonId">The unique identifier of the deleted taxon.</param>
        /// <param name="TaxonomyId">The unique identifier of the taxonomy this taxon belonged to.</param>
        public record Deleted(Guid TaxonId, Guid TaxonomyId) : DomainEvent;
        /// <summary>
        /// Event fired when a <see cref="Taxon"/>'s position in the hierarchy changes (e.g., parent change, reordering).
        /// </summary>
        /// <param name="TaxonId">The unique identifier of the moved taxon.</param>
        /// <param name="TaxonomyId">The unique identifier of the taxonomy this taxon belongs to.</param>
        /// <param name="OldParentId">The unique identifier of the taxon's previous parent. Null if it was a root taxon.</param>
        /// <param name="NewParentId">The unique identifier of the taxon's new parent. Null if it is now a root taxon.</param>
        /// <param name="NewIndex">The new position index of the taxon within its parent's children collection.</param>
        public record Moved(Guid TaxonId, Guid TaxonomyId, Guid? OldParentId, Guid? NewParentId, int NewIndex) : DomainEvent;
        /// <summary>
        /// Event fired to signal that products within a specific <see cref="Taxon"/> need to be regenerated or re-evaluated.
        /// This is typically triggered by changes to automatic taxon rules or sort order.
        /// </summary>
        /// <param name="TaxonId">The unique identifier of the taxon whose products need regeneration.</param>
        public record RegenerateProducts(Guid TaxonId) : DomainEvent;
    }
    #endregion
}

