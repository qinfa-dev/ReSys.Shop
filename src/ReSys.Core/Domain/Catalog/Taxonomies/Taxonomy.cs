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
    /// Gets or sets the internal system name for the taxonomy.
    /// This is a slug-like format (lowercase with hyphens, e.g., "product-categories", "brands", "seasonal-tags").
    /// It is used for identification and must be unique within its associated <see cref="Store"/>.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the human-readable display name shown to customers and administrators.
    /// This can differ significantly from <see cref="Name"/> for better user experience and localization.
    /// Examples: <c>Name="product-categories"</c> → <c>Presentation="Product Categories"</c>.
    /// Can include special formatting like emojis: "🏷️ Sale Tags", "👥 Vendors".
    /// </summary>
    public string Presentation { get; set; } = null!;

    /// <summary>
    /// Gets or sets the positional ordering of this taxonomy among other taxonomies within the store.
    /// Lower values typically appear first in navigation UIs and management panels.
    /// Typical range: 0-999 (often increments by 10 for easy insertion: 10, 20, 30...).
    /// Editors can reorder taxonomies via this field.
    /// </summary>
    public int Position { get; set; }

    #endregion

    #region Relationships
    /// <summary>
    /// Gets or sets the unique identifier of the <see cref="Store"/> this taxonomy belongs to.
    /// This is a foreign key reference to the parent <see cref="Store"/> aggregate, indicating
    /// that taxonomies are store-specific.
    /// A <c>null</c> value (though typically enforced as non-nullable via EF Core) might imply a global/shared taxonomy.
    /// </summary>
    public Guid? StoreId { get; set; }
    
    /// <summary>
    /// Gets or sets the navigation property to the parent <see cref="Store"/> aggregate.
    /// Provides access to store details and configuration.
    /// </summary>
    public Store Store { get; set; } = null!;
    
    /// <summary>
    /// Gets the computed root <see cref="Taxon"/> of this taxonomy's hierarchy.
    /// This returns the single <see cref="Taxon"/> with <see cref="Taxon.ParentId"/> = <c>null</c>.
    /// Every taxonomy is expected to have exactly one root taxon, which serves as the entry point
    /// to traverse the tree of categories.
    /// Returns <c>null</c> if the taxonomy is empty (no taxons created yet), an unusual state.
    /// </summary>
    public Taxon? Root => Taxons.FirstOrDefault(predicate: t => t.ParentId == null);
    
    /// <summary>
    /// Gets or sets the flat collection of all <see cref="Taxon"/>s belonging to this taxonomy.
    /// This includes the root and all its descendants at all levels.
    /// For efficient hierarchical traversal and querying, the nested set properties
    /// (<c>Lft</c>, <c>Rgt</c>, <c>Depth</c>) on individual <see cref="Taxon"/>s should be used.
    /// </summary>
    public ICollection<Taxon> Taxons { get; set; } = new List<Taxon>();

    #endregion

    #region Metadata
    /// <summary>
    /// Gets or sets public metadata: custom attributes visible to administrators and potentially exposed via public APIs.
    /// Use for: campaign taxonomy labels, featured taxonomy flags, display hints, UI configuration.
    /// Example: <c>{ "campaign": "holiday-2024", "featured": true, "icon": "🛍️", "sort_by": "custom" }</c>.
    /// </summary>
    public IDictionary<string, object?>? PublicMetadata { get; set; } = new Dictionary<string, object?>();
    
    /// <summary>
    /// Gets or sets private metadata: custom attributes visible only to administrators and backend systems.
    /// Use for: internal notes, migration tracking, system flags, integration markers.
    /// Example: <c>{ "legacy_id": "tax-12345", "source": "old-system", "needs_review": false }</c>.
    /// This data is NEVER exposed via public APIs or customer-facing interfaces.
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
    /// Updates the mutable properties of the taxonomy.
    /// This method allows for partial updates; only provided parameters will be changed.
    /// </summary>
    /// <param name="storeId">The new unique identifier of the <see cref="Store"/> this taxonomy belongs to. If null, the existing store ID is retained.</param>
    /// <param name="name">The new internal system name for the taxonomy. If null, the existing name is retained.</param>
    /// <param name="presentation">The new human-readable display name. If null, the existing presentation is retained.</param>
    /// <param name="position">The new positional ordering. If null, the existing position is retained.</param>
    /// <param name="publicMetadata">New public metadata. If null, the existing public metadata is retained.</param>
    /// <param name="privateMetadata">New private metadata. If null, the existing private metadata is retained.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Taxonomy}"/> result.
    /// Returns the updated <see cref="Taxonomy"/> instance on success.
    /// Returns one of the <see cref="Errors"/> if validation fails (e.g., name too long).
    /// </returns>
    /// <remarks>
    /// This method updates various properties of the taxonomy.
    /// <para>
    /// If changes occur, the <c>UpdatedAt</c> timestamp is updated, and an <see cref="Events.Updated"/>
    /// domain event is added. The event includes a <c>NameChanged</c> flag to help subscribers
    /// (e.g., search indexers, cache invalidators) respond appropriately if the taxonomy's name
    /// or presentation (which is tied to uniqueness) has been modified.
    /// </para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var taxonomy = GetTaxonomyById(taxonomyId);
    /// var updateResult = taxonomy.Update(
    ///     presentation: "Updated Product Categories",
    ///     position: 5);
    /// 
    /// if (updateResult.IsError)
    /// {
    ///     Console.WriteLine($"Error updating taxonomy: {updateResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Taxonomy '{taxonomy.Name}' updated successfully.");
    /// }
    /// </code>
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
            changed = true;
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
    /// Deletes this taxonomy from the system.
    /// This operation is only permitted if the taxonomy contains no <see cref="Taxon"/>s (or only its root taxon).
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Returns <see cref="Result.Deleted"/> on successful deletion.
    /// Returns <see cref="Errors.HasTaxons"/> if the taxonomy still contains associated taxons.
    /// </returns>
    /// <remarks>
    /// To maintain data integrity, a taxonomy cannot be deleted if it still contains an active category structure.
    /// All associated taxons (except potentially the root, which is implicitly removed with the taxonomy)
    /// must be deleted or reparented before the taxonomy itself can be deleted.
    /// <para>
    /// A <see cref="Events.Deleted"/> domain event is raised upon successful deletion.
    /// </para>
    /// <strong>Deletion Constraints:</strong>
    /// Taxonomy can only be deleted if <c>Taxons.Count</c> is 0 or 1 (empty or root-only).
    /// <para>
    /// <strong>Typical Workflow for Deletion:</strong>
    /// <code>
    /// // 1. Attempt to delete (will fail if taxons exist)
    /// var deleteResult = taxonomy.Delete();  // Returns HasTaxons error if categories exist
    /// 
    /// // 2. First, remove all taxons:
    /// foreach (var taxon in taxonomy.Taxons.Where(t => t.ParentId != null).ToList())
    /// {
    ///     var removeTaxonResult = taxon.Delete();
    ///     dbContext.Taxons.Remove(taxon);
    /// }
    /// await dbContext.SaveChangesAsync(ct);
    /// 
    /// // 3. Now deletion succeeds
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
    /// Defines domain events related to the lifecycle and state changes of a <see cref="Taxonomy"/>.
    /// These events are crucial for enabling a decoupled, event-driven architecture, allowing
    /// other services or bounded contexts to react to taxonomy-related changes.
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Raised when a new taxonomy is created (e.g., "Product Categories" for a store).
        /// Handlers might build the initial root taxon, update store taxonomy caches, or notify integrations.
        /// </summary>
        /// <param name="TaxonomyId">The unique identifier of the newly created taxonomy.</param>
        /// <param name="Name">The internal system name of the new taxonomy.</param>
        /// <param name="Presentation">The human-readable display name of the new taxonomy.</param>
        public record Created(Guid TaxonomyId, string Name, string Presentation) : DomainEvent;
        
        /// <summary>
        /// Raised when taxonomy metadata or configuration is updated.
        /// </summary>
        /// <param name="TaxonomyId">The unique identifier of the updated taxonomy.</param>
        /// <param name="Name">The current internal system name of the taxonomy.</param>
        /// <param name="Presentation">The current human-readable display name of the taxonomy.</param>
        /// <param name="NameChanged">True if the Name or Presentation property has changed, indicating a need for cache invalidation or re-indexing.</param>
        public record Updated(Guid TaxonomyId, string Name, string Presentation, bool NameChanged) : DomainEvent;
        
        /// <summary>
        /// Raised when a taxonomy is deleted.
        /// This event is only raised if the taxonomy contains no taxons (or only its root taxon).
        /// Handlers might remove the taxonomy from search indexes, clear caches, or update store navigation.
        /// </summary>
        /// <param name="TaxonomyId">The unique identifier of the deleted taxonomy.</param>
        public record Deleted(Guid TaxonomyId) : DomainEvent;
    }

    #endregion
}
