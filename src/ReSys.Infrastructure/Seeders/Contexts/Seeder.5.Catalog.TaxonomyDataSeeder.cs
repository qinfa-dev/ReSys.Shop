using Bogus;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Extensions;
using ReSys.Core.Domain.Catalog.Taxonomies;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;
using ReSys.Core.Domain.Stores;
using ReSys.Infrastructure.Persistence.Contexts;

using Serilog;

namespace ReSys.Infrastructure.Seeders.Contexts;

public sealed class CatalogTaxonomyDataSeeder(IServiceProvider serviceProvider) : IDataSeeder
{
    private readonly ILogger _logger = Log.ForContext<CatalogTaxonomyDataSeeder>();
    private readonly Random _random = new();

    public int Order => 5; // Run after all other catalog seeders

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _logger.Information("Starting catalog Taxonomy and Taxon data seeding.");

        try
        {
            // 1. Ensure a default Store exists
            Store defaultStore = await EnsureDefaultStoreExistsAsync(dbContext, cancellationToken);

            // 2. Create a Taxonomy using the StoreId
            Taxonomy productCategoriesTaxonomy = await EnsureTaxonomyExistsAsync(dbContext, defaultStore.Id, cancellationToken);

            // 3. Create Taxons in a 3-level deep hierarchy
            await EnsureFashionTaxonsExistAsync(dbContext, productCategoriesTaxonomy, cancellationToken);

            _logger.Information("Catalog Taxonomy and Taxon data seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error(exception: ex,
                messageTemplate: "Catalog Taxonomy and Taxon data seeding failed with exception: {ErrorMessage}",
                propertyValue: ex.Message);
            throw;
        }
    }

    private async Task<Store> EnsureDefaultStoreExistsAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        Store? defaultStore = await dbContext.Set<Store>()
            .FirstOrDefaultAsync(s => s.Code == "DEFAULT", cancellationToken);

        if (defaultStore == null)
        {
            var storeResult = Store.Create(
                name: "Default Fashion Store",
                presentation: "The Main Fashion Hub",
                code: "DEFAULT",
                url: "www.fashion-shop.com",
                currency: "USD",
                mailFromAddress: "no-reply@fashion-shop.com",
                customerSupportEmail: "support@fashion-shop.com",
                isDefault: true);

            if (storeResult.IsError)
            {
                _logger.Error("Failed to create default store: {Errors}", storeResult.FirstError.Description);
                throw new InvalidOperationException("Failed to create default store.");
            }
            defaultStore = storeResult.Value;
            dbContext.Set<Store>().Add(defaultStore);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.Information("Created default store with ID: {StoreId}", defaultStore.Id);
        }
        else
        {
            _logger.Information("Default store with ID '{StoreId}' already exists.", defaultStore.Id);
        }
        return defaultStore;
    }

    private async Task<Taxonomy> EnsureTaxonomyExistsAsync(ApplicationDbContext dbContext, Guid storeId, CancellationToken cancellationToken)
    {
        Taxonomy? productCategoriesTaxonomy = await dbContext.Set<Taxonomy>()
            .FirstOrDefaultAsync(t => t.Name == "apparel-accessories" && t.StoreId == storeId, cancellationToken);

        if (productCategoriesTaxonomy == null)
        {
            var taxonomyResult = Taxonomy.Create(
                storeId: storeId,
                name: "apparel-accessories",
                presentation: "Apparel & Accessories",
                position: 0);

            if (taxonomyResult.IsError)
            {
                _logger.Error("Failed to create Apparel & Accessories Taxonomy: {Errors}", taxonomyResult.FirstError.Description);
                throw new InvalidOperationException("Failed to create Apparel & Accessories Taxonomy.");
            }
            productCategoriesTaxonomy = taxonomyResult.Value;
            taxonomyResult.Value.SetPublic("description",
                "The main product categories for fashion items and accessories.");
            dbContext.Set<Taxonomy>().Add(productCategoriesTaxonomy);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.Information("Created Apparel & Accessories Taxonomy with ID: {TaxonomyId}", productCategoriesTaxonomy.Id);
        }
        else
        {
            _logger.Information("Apparel & Accessories Taxonomy with ID '{TaxonomyId}' already exists.", productCategoriesTaxonomy.Id);
        }
        return productCategoriesTaxonomy;
    }

    private async Task EnsureFashionTaxonsExistAsync(ApplicationDbContext dbContext, Taxonomy taxonomy, CancellationToken cancellationToken)
    {
        // Define a Faker for generating fashion-related taxon data
        var taxonFaker = new Faker<TaxonData>()
            .RuleFor(td => td.Name, f => f.Commerce.Categories(1)[0])
            .RuleFor(td => td.Description, f => f.Lorem.Sentence());

        // The root taxon is automatically created by the Taxonomy.Created domain event.
        // We will now query for it.
        Taxon? rootTaxon = await dbContext.Set<Taxon>()
            .FirstOrDefaultAsync(t => t.TaxonomyId == taxonomy.Id && t.ParentId == null && t.Name == taxonomy.Name, cancellationToken);

        if (rootTaxon == null)
        {
            _logger.Error("Root Taxon for Taxonomy '{TaxonomyName}' (ID: {TaxonomyId}) was not found after taxonomy creation. The domain event handler might have failed to create it.",
                taxonomy.Name, taxonomy.Id);
            throw new InvalidOperationException($"Root Taxon for Taxonomy '{taxonomy.Name}' was not found.");
        }
        else
        {
            _logger.Information("Root Taxon for Taxonomy '{TaxonomyName}' found with ID: {TaxonId}", taxonomy.Name, rootTaxon.Id);
        }

        // --- Level 1 Taxons (Main Categories) ---
        var level1CategoryNames = new List<string> { "Men's Apparel", "Women's Apparel", "Kids & Baby", "Accessories", "Footwear" };
        var level1Taxons = new List<Taxon>();

        foreach (var categoryName in level1CategoryNames)
        {
            string nameSlug = categoryName.ToSlug();

            // First, check if a taxon with this name slug already exists in the exact hierarchical position
            var existingTaxon = await dbContext.Set<Taxon>()
                .FirstOrDefaultAsync(t => t.TaxonomyId == taxonomy.Id && t.ParentId == rootTaxon.Id && t.Name == nameSlug, cancellationToken);

            if (existingTaxon == null)
            {
                // If not found in the specific position, check if the name slug exists globally to prevent unique constraint violation
                var globallyExistingTaxon = await dbContext.Set<Taxon>()
                    .FirstOrDefaultAsync(t => t.Name == nameSlug, cancellationToken);

                if (globallyExistingTaxon != null)
                {
                    _logger.Warning("Taxon with name slug '{NameSlug}' already exists globally (ID: {ExistingTaxonId}) but not under the current parent/taxonomy. Skipping creation to prevent duplicate key error. Consider adjusting the data or database schema if this is unexpected.",
                        nameSlug, globallyExistingTaxon.Id);
                    continue; // Skip creating this taxon
                }

                var taxonResult = Taxon.Create(
                    taxonomyId: taxonomy.Id,
                    name: nameSlug,
                    parentId: rootTaxon.Id,
                    presentation: categoryName,
                    description: taxonFaker.Generate().Description);

                if (taxonResult.IsError)
                {
                    _logger.Error("Failed to create Level 1 Taxon '{TaxonName}': {Errors}", categoryName, taxonResult.FirstError.Description);
                    continue;
                }
                var newTaxon = taxonResult.Value;
                dbContext.Set<Taxon>().Add(newTaxon);
                level1Taxons.Add(newTaxon);
                _logger.Information("Created Level 1 Taxon '{TaxonName}' with ID: {TaxonId}", categoryName, newTaxon.Id);
            }
            else
            {
                level1Taxons.Add(existingTaxon);
                _logger.Information("Level 1 Taxon '{TaxonName}' already exists with ID: {TaxonId}", categoryName, existingTaxon.Id);
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        // --- Level 2 Taxons (Sub-Categories) ---
        var level2Taxons = new List<Taxon>();
        var level2CategoryMap = new Dictionary<string, List<string>>
        {
            {"men's-apparel", new List<string> {"Shirts", "Pants", "Outerwear", "Activewear", "Underwear"}},
            {"women's-apparel", new List<string> {"Dresses", "Skirts", "Blouses", "Jeans", "Lingerie", "Activewear"}},
            {"kids-baby", new List<string> {"Boys", "Girls", "Baby", "Toddler"}},
            {"accessories", new List<string> {"Bags", "Jewelry", "Hats & Caps", "Scarves & Gloves", "Belts"}},
            {"footwear", new List<string> {"Sneakers", "Boots", "Sandals", "Heels", "Flats"}}
        };

        foreach (var l1Taxon in level1Taxons)
        {
            if (level2CategoryMap.TryGetValue(l1Taxon.Name, out var subCategories))
            {
                int numChildren = _random.Next(1, 4); // 1 to 3 children
                for (int i = 0; i < numChildren && i < subCategories.Count; i++)
                {
                    string subCategoryName = subCategories[i];
                    string nameSlug = subCategoryName.ToSlug();

                    // First, check if a taxon with this name slug already exists in the exact hierarchical position
                    var existingTaxon = await dbContext.Set<Taxon>()
                        .FirstOrDefaultAsync(t => t.TaxonomyId == taxonomy.Id && t.ParentId == l1Taxon.Id && t.Name == nameSlug, cancellationToken);

                    if (existingTaxon == null)
                    {
                        // If not found in the specific position, check if the name slug exists globally to prevent unique constraint violation
                        var globallyExistingTaxon = await dbContext.Set<Taxon>()
                            .FirstOrDefaultAsync(t => t.Name == nameSlug, cancellationToken);

                        if (globallyExistingTaxon != null)
                        {
                            _logger.Warning("Taxon with name slug '{NameSlug}' already exists globally (ID: {ExistingTaxonId}) but not under parent '{ParentName}' in current taxonomy. Skipping creation to prevent duplicate key error.",
                                nameSlug, globallyExistingTaxon.Id, l1Taxon.Presentation);
                            continue; // Skip creating this taxon
                        }

                        var taxonResult = Taxon.Create(
                            taxonomyId: taxonomy.Id,
                            name: nameSlug,
                            parentId: l1Taxon.Id,
                            presentation: subCategoryName,
                            description: taxonFaker.Generate().Description);

                        if (taxonResult.IsError)
                        {
                            _logger.Error("Failed to create Level 2 Taxon '{TaxonName}': {Errors}", subCategoryName, taxonResult.FirstError.Description);
                            continue;
                        }
                        var newTaxon = taxonResult.Value;
                        dbContext.Set<Taxon>().Add(newTaxon);
                        level2Taxons.Add(newTaxon);
                        _logger.Information("Created Level 2 Taxon '{TaxonName}' under '{ParentName}' with ID: {TaxonId}", subCategoryName, l1Taxon.Presentation, newTaxon.Id);
                    }
                    else
                    {
                        level2Taxons.Add(existingTaxon);
                        _logger.Information("Level 2 Taxon '{TaxonName}' under '{ParentName}' already exists with ID: {TaxonId}", subCategoryName, l1Taxon.Presentation, existingTaxon.Id);
                    }
                }
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        // --- Level 3 Taxons (Specific Items) ---
        var level3CategoryMap = new Dictionary<string, List<string>>
        {
            {"shirts", new List<string> {"T-Shirts", "Dress Shirts", "Casual Shirts", "Polos"}},
            {"pants", new List<string> {"Jeans", "Chinos", "Dress Pants", "Cargo Pants"}},
            {"dresses", new List<string> {"Evening Gowns", "Cocktail Dresses", "Maxi Dresses", "Sundresses"}},
            {"sneakers", new List<string> {"Running Shoes", "High-Tops", "Casual Sneakers"}},
            {"bags", new List<string> {"Handbags", "Backpacks", "Totes", "Clutches"}}
        };

        foreach (var l2Taxon in level2Taxons)
        {
            if (level3CategoryMap.TryGetValue(l2Taxon.Name, out var specificItems))
            {
                int numChildren = _random.Next(1, 4); // 1 to 3 children
                for (int i = 0; i < numChildren && i < specificItems.Count; i++)
                {
                    string specificItemName = specificItems[i];
                    string nameSlug = specificItemName.ToSlug();

                    // First, check if a taxon with this name slug already exists in the exact hierarchical position
                    var existingTaxon = await dbContext.Set<Taxon>()
                        .FirstOrDefaultAsync(t => t.TaxonomyId == taxonomy.Id && t.ParentId == l2Taxon.Id && t.Name == nameSlug, cancellationToken);

                    if (existingTaxon == null)
                    {
                        // If not found in the specific position, check if the name slug exists globally to prevent unique constraint violation
                        var globallyExistingTaxon = await dbContext.Set<Taxon>()
                            .FirstOrDefaultAsync(t => t.Name == nameSlug, cancellationToken);

                        if (globallyExistingTaxon != null)
                        {
                            _logger.Warning("Taxon with name slug '{NameSlug}' already exists globally (ID: {ExistingTaxonId}) but not under parent '{ParentName}' in current taxonomy. Skipping creation to prevent duplicate key error.",
                                nameSlug, globallyExistingTaxon.Id, l2Taxon.Presentation);
                            continue; // Skip creating this taxon
                        }

                        var taxonResult = Taxon.Create(
                            taxonomyId: taxonomy.Id,
                            name: nameSlug,
                            parentId: l2Taxon.Id,
                            presentation: specificItemName,
                            description: taxonFaker.Generate().Description);

                        if (taxonResult.IsError)
                        {
                            _logger.Error("Failed to create Level 3 Taxon '{TaxonName}': {Errors}", specificItemName, taxonResult.FirstError.Description);
                            continue;
                        }
                        var newTaxon = taxonResult.Value;
                        dbContext.Set<Taxon>().Add(newTaxon);
                        _logger.Information("Created Level 3 Taxon '{TaxonName}' under '{ParentName}' with ID: {TaxonId}", specificItemName, l2Taxon.Presentation, newTaxon.Id);
                    }
                    else
                    {
                        _logger.Information("Level 3 Taxon '{TaxonName}' under '{ParentName}' already exists with ID: {TaxonId}", specificItemName, l2Taxon.Presentation, existingTaxon.Id);
                    }
                }
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // Helper class for Bogus to generate taxon data
    private class TaxonData
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}

