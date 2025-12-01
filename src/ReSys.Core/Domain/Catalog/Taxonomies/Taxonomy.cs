using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;
using ReSys.Core.Domain.Stores;

namespace ReSys.Core.Domain.Catalog.Taxonomies;

/// <summary>
/// Represents a taxonomy: a grouping container for hierarchical taxons (categories).
/// Examples: "Categories" (products → apparel → mens → shirts), "Brands" (Nike, Adidas), "Tags" (new, sale, featured).
/// Each taxonomy is store-specific and contains a tree of taxons with a single root.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Taxonomy vs Taxon:</strong>
/// <list type="bullet">
/// <item>
/// <term>Taxonomy</term>
/// <description>The container/grouping (e.g., "Product Categories", "Brands")</description>
/// </item>
/// <item>
/// <term>Taxon</term>
/// <description>Individual nodes within the taxonomy (e.g., "Apparel", "Men's", "Shirts")</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Taxonomy Hierarchy Example:</strong>
/// <code>
/// Taxonomy: "Product Categories"
/// └─ Root Taxon: "Products"
///    ├─ Apparel (Taxon)
///    │  ├─ Men's (Taxon)
///    │  │  ├─ Shirts (Taxon)
///    │  │  └─ Pants (Taxon)
///    │  └─ Women's (Taxon)
///    │     ├─ Dresses (Taxon)
///    │     └─ Accessories (Taxon)
///    ├─ Electronics (Taxon)
///    └─ Books (Taxon)
/// </code>
/// </para>
///
/// <para>
/// <strong>Store-Specific:</strong>
/// Each taxonomy belongs to a specific store. Different stores can have different taxonomies or category structures.
/// </para>
///
/// <para>
/// <strong>Single Root Taxon:</strong>
/// Each taxonomy has exactly one root taxon (taxon with ParentId = null).
/// This ensures a connected, consistent tree structure.
/// </para>
///
/// <para>
/// <strong>Use Cases:</strong>
/// <list type="bullet">
/// <item>Primary category navigation (e.g., "Shop by Category")</item>
/// <item>Faceted search filters</item>
/// <item>Breadcrumb navigation trails</item>
/// <item>Product classification systems</item>
/// <item>Brand hierarchies</item>
/// <item>Tag-based organization</item>
/// </list>
/// </para>
/// </remarks>
public sealed class Taxonomy :
   Aggregate,
   IHasParameterizableName,
   IHasPosition,
   IHasMetadata,
   IHasUniqueName
{
    #region Errors
    /// <summary>
    /// Domain error definitions for taxonomy operations and validation.
    /// Returned via ErrorOr pattern for railway-oriented error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Occurs when attempting to create taxonomy without a name.
        /// Prevention: Taxonomy name is required for identification and reference.
        /// Resolution: Provide a non-empty, unique name like "product-categories" or "brands".
        /// </summary>
        public static Error TaxonomyRequired => CommonInput.Errors.Required(prefix: nameof(Taxonomy));
        
        /// <summary>
        /// Occurs when attempting to delete a taxonomy that has child taxons.
        /// Prevention: Cannot delete taxonomies with associated hierarchical data.
        /// Ensures data integrity: orphaned taxons would have no container.
        /// Resolution: Delete or reparent all taxons first, then delete empty taxonomy.
        /// </summary>
        public static Error HasTaxons => Error.Validation(
            code: "Taxonomy.HasTaxons",
            description: "Cannot delete a taxonomy with associated taxons. Delete or move all taxons first.");
        
        /// <summary>
        /// Occurs when referenced taxonomy ID cannot be found in database.
        /// Typical causes: ID doesn't exist, taxonomy was deleted, query for wrong store.
        /// Resolution: Verify taxonomy ID, check it belongs to correct store, ensure not soft-deleted.
        /// </summary>
        public static Error NotFound(Guid id) => Error.NotFound(
            code: "Taxonomy.NotFound",
            description: $"Taxonomy with ID '{id}' was not found.");
        
        /// <summary>
        /// Occurs when name or presentation is empty or whitespace-only.
        /// Prevention: Name required for identification; cannot use blank strings.
        /// Resolution: Provide meaningful name (e.g., "categories", "product-brands", "seasonal-tags").
        /// </summary>
        public static Error NameRequired => CommonInput.Errors.Required(
            prefix: nameof(Taxonomy),
            field: nameof(Name));
    }

    #endregion

    #region Core Properties

    /// <summary>
    /// Internal system name (slug-like format). Used for identification and must be unique within store.
    /// Convention: lowercase with hyphens, URL-safe, no special characters.
    /// Examples: "product-categories", "brands", "seasonal-tags", "vendor-names"
    /// Used in code/APIs: `taxonomy.Name == "product-categories"`
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Human-readable display name shown to customers and admins.
    /// Can differ significantly from Name for better UX and localization.
    /// Examples: Name="product-categories" → Presentation="Product Categories"
    /// Used on storefront: category navigation, admin UI, breadcrumbs
    /// Can include special formatting: "🏷️ Sale Tags", "👥 Vendors", "Product Categories"
    /// </summary>
    public string Presentation { get; set; } = null!;

    /// <summary>
    /// Positional ordering of this taxonomy among other taxonomies within the store.
    /// Lower values appear first in navigation UIs and management panels.
    /// Typical range: 0-999 (increments by 10 for easy insertion: 10, 20, 30...)
    /// Example: Categories (10), Brands (20), Tags (30)
    /// Editors can reorder taxonomies via this field.
    /// </summary>
    public int Position { get; set; }

    #endregion

    #region Relationships
    /// <summary>
    /// Store this taxonomy belongs to.
    /// Foreign key reference to parent Store aggregate.
    /// Taxonomies are store-specific: each store can have unique category structures.
    /// Example: Store A has 3 taxonomies (Categories, Brands, Tags); Store B has 2 taxonomies
    /// Null indicates global/shared taxonomy (rare, typically null for multi-store systems).
    /// </summary>
    public Guid? StoreId { get; set; }
    
    /// <summary>
    /// Navigation property: parent store aggregate.
    /// Provides access to store details and configuration.
    /// Used to filter taxonomies: store.Taxonomies.Where(t => t.Name == "categories")
    /// </summary>
    public Store Store { get; set; } = null!;
    
    /// <summary>
    /// Computed property: root taxon of this taxonomy hierarchy.
    /// Returns the single taxon with ParentId = null (entry point to the tree).
    /// Every taxonomy MUST have exactly one root taxon.
    /// Used to traverse hierarchy: Root.Children to access top-level categories.
    /// Example: For "Categories" taxonomy, Root might be named "Products" containing Apparel, Electronics, etc.
    /// Returns null if taxonomy is empty (unusual state, should not occur in normal operations).
    /// </summary>
    public Taxon? Root => Taxons.FirstOrDefault(predicate: t => t.ParentId == null);
    
    /// <summary>
    /// Flat collection of ALL taxons in this taxonomy (entire hierarchy).
    /// Includes root and all descendants at all levels.
    /// Use nested set properties (Lft/Rgt/Depth) on individual taxons to traverse hierarchy efficiently.
    /// Not typically used directly; instead access via Root or specific taxon queries.
    /// Example: For "Categories" taxonomy with 50 taxons total, Taxons.Count == 50
    /// </summary>
    public ICollection<Taxon> Taxons { get; set; } = new List<Taxon>();

    #endregion

    #region Metadata
    /// <summary>
    /// Public metadata: custom attributes visible to admins and potentially exposed via public APIs.
    /// Use for: campaign taxonomy labels, featured taxonomy flags, display hints, UI configuration.
    /// Example: { "campaign": "holiday-2024", "featured": true, "icon": "🛍️", "sort_by": "custom" }
    /// Visible in: Admin UI, API responses, integration systems.
    /// </summary>
    public IDictionary<string, object?>? PublicMetadata { get; set; } = new Dictionary<string, object?>();
    
    /// <summary>
    /// Private metadata: custom attributes visible only to admins and backend systems.
    /// Use for: internal notes, migration tracking, system flags, integration markers.
    /// Example: { "legacy_id": "tax-12345", "source": "old-system", "needs_review": false }
    /// Visible in: Admin UI, backend logs, internal systems.
    /// NEVER exposed: public APIs, customer-facing interfaces.
    /// </summary>
    public IDictionary<string, object?>? PrivateMetadata { get; set; } = new Dictionary<string, object?>();

    #endregion

    #region Constructors

    private Taxonomy() { }

    #endregion

    #region Factory
    /// <summary>
    /// Factory method for creating a new taxonomy container within a store.
    /// Initializes taxonomy with name, presentation, and optional metadata.
    /// Raises Created domain event for downstream processing (search index, cache invalidation, etc.).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Factory Pattern Rationale:</strong>
    /// Factory ensures all taxonomies are created consistently with proper initialization.
    /// Centralizes validation: names normalized, positions validated, events raised automatically.
    /// </para>
    ///
    /// <para>
    /// <strong>Name vs Presentation:</strong>
    /// <list type="bullet">
    /// <item>
    /// <term>Name</term>
    /// <description>Technical identifier (slug format). Immutable, unique per store. Used in code and APIs.</description>
    /// </item>
    /// <item>
    /// <term>Presentation</term>
    /// <description>Human-readable display name. Mutable, can be localized. Shown to customers/admins.</description>
    /// </item>
    /// </list>
    /// Example: Name="product-categories" → Presentation="Product Categories"
    /// </para>
    ///
    /// <para>
    /// <strong>Typical Usage:</strong>
    /// <code>
    /// // Create primary product taxonomy
    /// var categoriesResult = Taxonomy.Create(
    ///     storeId: store.Id,
    ///     name: "product-categories",
    ///     presentation: "Product Categories",
    ///     position: 10);
    /// 
    /// if (categoriesResult.IsError)
    ///     return Problem(categoriesResult.FirstError.Description);
    /// 
    /// var categories = categoriesResult.Value;
    /// store.AddTaxonomy(categories);
    /// 
    /// // Create brand taxonomy with metadata
    /// var brandsResult = Taxonomy.Create(
    ///     storeId: store.Id,
    ///     name: "brands",
    ///     presentation: "👥 Brand Partners",
    ///     position: 20,
    ///     publicMetadata: new { featured = true, icon = "👥" },
    ///     privateMetadata: new { source = "supplier-system", synced = DateTime.UtcNow });
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Parameters:</strong>
    /// - storeId: Required. The store this taxonomy belongs to (multi-tenant).
    /// - name: Required. Technical identifier (slug). Will be normalized to lowercase/hyphens.
    /// - presentation: Required. Human-readable display name. Can include emojis or special formatting.
    /// - position: Optional (default 0). Display order among other taxonomies. Use multiples of 10 for easy insertion.
    /// - publicMetadata: Optional. Visible in APIs, use for UI hints and campaign tags.
    /// - privateMetadata: Optional. Internal system data, never exposed publicly.
    /// </para>
    /// </remarks>
    public static ErrorOr<Taxonomy> Create(
        Guid storeId,
       string name,
       string presentation,
       int position = 0,
       IDictionary<string, object?>? publicMetadata = null,
       IDictionary<string, object?>? privateMetadata = null)
    {
        (name, presentation) = HasParameterizableName.NormalizeParams(name: name, presentation: presentation);

        var taxonomy = new Taxonomy
        {
            StoreId = storeId,
            Name = name,
            Presentation = presentation,
            Position = Math.Max(val1: 0, val2: position),
            PublicMetadata = publicMetadata ?? new Dictionary<string, object?>(),
            PrivateMetadata = privateMetadata ?? new Dictionary<string, object?>(),
        };

        taxonomy.AddDomainEvent(domainEvent: new Events.Created(TaxonomyId: taxonomy.Id, Name: taxonomy.Name, Presentation: taxonomy.Presentation));
        return taxonomy;
    }

    #endregion

    #region Business Logic - Update & Delete
    /// <summary>
    /// Update taxonomy metadata while maintaining structural integrity.
    /// Supports updating: name, presentation, position, and both metadata collections.
    /// Raises Updated domain event when changes occur.
    /// Tracks whether name/presentation changed for cache invalidation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Update Strategy:</strong>
    /// Pass null for fields to leave unchanged. Only provided values are updated.
    /// Timestamps (UpdatedAt) automatically set by concern behavior.
    /// </para>
    ///
    /// <para>
    /// <strong>Typical Usage:</strong>
    /// <code>
    /// // Update presentation (UI display)
    /// var updateResult = taxonomy.Update(
    ///     presentation: "🏷️ Product Categories");  // Emoji added for visual appeal
    /// 
    /// // Reorder taxonomies
    /// var reorderResult = taxonomy.Update(position: 15);
    /// 
    /// // Update metadata for campaign
    /// var campaignResult = taxonomy.Update(
    ///     publicMetadata: new { featured = true, campaign = "holiday-2024" },
    ///     privateMetadata: new { updated_by_user = "admin123", reason = "holiday refresh" });
    /// 
    /// // Change name (rarely done - breaks existing references)
    /// var renameResult = taxonomy.Update(
    ///     name: "seasonal-categories",
    ///     presentation: "Seasonal Categories");  // Usually update both together
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Audit Trail:</strong>
    /// Every update raises domain event with change flags.
    /// NameChanged flag helps subscribers invalidate caches/indexes if name/presentation changed.
    /// </para>
    /// </remarks>
    public ErrorOr<Taxonomy> Update(
        Guid? storeId= null,
       string? name = null,
       string? presentation = null,
       int? position = null,
       IDictionary<string, object?>? publicMetadata = null,
       IDictionary<string, object?>? privateMetadata = null)
    {
        bool nameChanged = false;
        bool changed = false;
        (name, presentation) = HasParameterizableName.NormalizeParams(name: name, presentation: presentation);
        if (!string.IsNullOrWhiteSpace(value: name) && Name != name)
        {
            Name = name;
            nameChanged = true;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(value: presentation) && Presentation != presentation)
        {
            Presentation = presentation;
            nameChanged = true;
        }

        if (position.HasValue && position.Value != Position)
        {
            Position = Math.Max(val1: 0, val2: position.Value);
            changed = true;
        }

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

        if (storeId.HasValue && storeId.Value != StoreId)
        {
            StoreId = storeId;
            changed = true;
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(domainEvent: new Events.Updated(TaxonomyId: Id, Name: Name, Presentation: Presentation, NameChanged: nameChanged));
        }

        return this;
    }

    /// <summary>
    /// Delete this taxonomy if it contains no taxons (or only root taxon).
    /// Raises Deleted domain event for cascade operations and cache invalidation.
    /// Data integrity: prevents deleting taxonomies with active category structure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Deletion Constraints:</strong>
    /// Taxonomy can only be deleted if Taxons.Count ≤ 1 (empty or root-only).
    /// Ensures no orphaned taxons remain after taxonomy deletion.
    /// If taxonomy has product categories, must reparent or delete all taxons first.
    /// </para>
    ///
    /// <para>
    /// <strong>Typical Workflow:</strong>
    /// <code>
    /// // Cannot delete taxonomy with categories
    /// var deleteResult = taxonomy.Delete();  // Returns HasTaxons error if categories exist
    /// 
    /// // First, remove all taxons:
    /// foreach (var taxon in taxonomy.Taxons.Where(t => t.ParentId != null))
    /// {
    ///     var removeTaxonResult = taxon.Delete();
    ///     dbContext.Taxons.Remove(taxon);
    /// }
    /// await dbContext.SaveChangesAsync(ct);
    /// 
    /// // Now deletion succeeds
    /// var finalDeleteResult = taxonomy.Delete();
    /// dbContext.Taxonomies.Remove(taxonomy);
    /// await dbContext.SaveChangesAsync(ct);
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Cascade Impact:</strong>
    /// Handlers for Deleted event can:
    /// - Remove taxonomy from search indexes
    /// - Clear cached category hierarchies
    /// - Update navigation menus
    /// - Audit trail: log who deleted what taxonomy
    /// </para>
    /// </remarks>
    public ErrorOr<Deleted> Delete()
    {
        // A taxonomy can only be deleted if it has no taxons other than its root.
        if (Taxons.Count > 1)
            return Errors.HasTaxons;

        AddDomainEvent(domainEvent: new Events.Deleted(TaxonomyId: Id));
        return Result.Deleted;
    }

    #endregion

    #region Events
    /// <summary>
    /// Domain events for taxonomy lifecycle: creation, updates, and deletion.
    /// Enables asynchronous processing and cross-domain communication.
    /// Events are published after SaveChangesAsync via EF Core interceptors.
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Raised when a new taxonomy is created (e.g., "Product Categories" for a store).
        /// Handlers: Build initial root taxon, update store taxonomy cache, notify integrations.
        /// </summary>
        public record Created(Guid TaxonomyId, string Name, string Presentation) : DomainEvent;
        
        /// <summary>
        /// Raised when taxonomy metadata or structure is updated.
        /// NameChanged flag indicates if name/presentation was modified (affecting caches/indexes).
        /// Handlers: Invalidate product search indexes if name changed, update UI caches, sync external systems.
        /// </summary>
        public record Updated(Guid TaxonomyId, string Name, string Presentation, bool NameChanged) : DomainEvent;
        
        /// <summary>
        /// Raised when a taxonomy is deleted.
        /// Only occurs if taxonomy has no associated taxons (fully emptied).
        /// Handlers: Remove from search indexes, clear caches, update store navigation, audit logging.
        /// </summary>
        public record Deleted(Guid TaxonomyId) : DomainEvent;
    }

    #endregion
}
