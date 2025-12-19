using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using ReSys.Core.Common.Extensions;
using ReSys.Core.Domain.Catalog.OptionTypes;
using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Images;
using ReSys.Core.Domain.Catalog.PropertyTypes;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Inventories.Movements;
using ReSys.Core.Domain.Stores;
using ReSys.Infrastructure.Persistence.Contexts;

using Serilog;

namespace ReSys.Infrastructure.Seeders.Contexts;

public sealed class CatalogProductDataSeeder(IServiceProvider serviceProvider) : IDataSeeder
{
    private readonly ILogger _logger = Log.ForContext<CatalogProductDataSeeder>();
    private readonly Random _random = new();

    public int Order => 60; // Run after CatalogTaxonomyDataSeeder

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _logger.Information("Starting catalog Product data seeding.");

        try
        {
            // Retrieve necessary data from previous seeders
            var store = await dbContext.Set<Store>().FirstAsync(s => s.Code == "DEFAULT", cancellationToken);
            var colorOptionType = await dbContext.Set<OptionType>().Include(ot => ot.OptionValues).FirstAsync(ot => ot.Name == "color", cancellationToken);
            var sizeOptionType = await dbContext.Set<OptionType>().Include(ot => ot.OptionValues).FirstAsync(ot => ot.Name == "size", cancellationToken);
            var materialProperty = await dbContext.Set<PropertyType>().FirstAsync(pt => pt.Name == "material", cancellationToken);
            var weightProperty = await dbContext.Set<PropertyType>().FirstAsync(pt => pt.Name == "weight-g", cancellationToken);
            var ecoFriendlyProperty = await dbContext.Set<PropertyType>().FirstAsync(pt => pt.Name == "is-eco-friendly", cancellationToken);
            var descriptionProperty = await dbContext.Set<PropertyType>().FirstAsync(pt => pt.Name == "description", cancellationToken);
            var taxons = await dbContext.Set<Taxon>().ToListAsync(cancellationToken); // Removed .Where(t => t.Level > 0)
            var rootTaxon = await dbContext.Set<Taxon>().FirstAsync(t => t.ParentId == null, cancellationToken);

            var availableColors = colorOptionType.OptionValues.ToList();
            var availableSizes = sizeOptionType.OptionValues.ToList();

            // Ensure stock locations exist
            var stockLocation = await dbContext.Set<StockLocation>().FirstOrDefaultAsync(sl => sl.Name == "Main Warehouse".ToSlug(), cancellationToken);
            if (stockLocation == null)
            {
                var stockLocationResult = StockLocation.Create("Main Warehouse", "MW", true);
                if (stockLocationResult.IsError)
                {
                    _logger.Error("Failed to create default stock location: {Errors}", stockLocationResult.FirstError.ToString());
                    throw new InvalidOperationException("Failed to create default stock location.");
                }
                stockLocation = stockLocationResult.Value;
                dbContext.Set<StockLocation>().Add(stockLocation);
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.Information("Created default stock location: {Name}", stockLocation.Name);
            }

            // Create some products
            var productsToSeed = new List<ProductData>();
            productsToSeed.Add(new ProductData("Premium T-Shirt", "A high-quality, comfortable t-shirt.", new List<string> { "men's-apparel", "shirts", "t-shirts" }, true, true, 25.00m, 30.00m, "cotton", "180", "true"));
            productsToSeed.Add(new ProductData("Slim Fit Jeans", "Stylish jeans for a modern look.", new List<string> { "men's-apparel", "pants", "jeans" }, true, true, 55.00m, 65.00m, "denim", "700", "false"));
            productsToSeed.Add(new ProductData("Elegant Summer Dress", "Light and airy dress perfect for summer.", new List<string> { "women's-apparel", "dresses", "sundresses" }, true, false, 40.00m, 50.00m, "linen", "300", "true"));
            productsToSeed.Add(new ProductData("Running Sneakers", "Comfortable and supportive for your runs.", new List<string> { "footwear", "sneakers", "running-shoes" }, true, true, 70.00m, 80.00m, "mesh", "600", "false"));
            productsToSeed.Add(new ProductData("Leather Handbag", "A sophisticated accessory for any occasion.", new List<string> { "accessories", "bags", "handbags" }, false, true, 120.00m, 150.00m, "leather", "800", "false"));


            foreach (var productData in productsToSeed)
            {
                var existingProduct = await dbContext.Set<Product>().FirstOrDefaultAsync(p => p.Name == productData.Name.ToSlug(), cancellationToken);

                if (existingProduct != null)
                {
                    _logger.Information("Product '{ProductName}' already exists. Skipping.", productData.Name);
                    continue;
                }

                var productResult = Product.Create(
                    name: productData.Name,
                    description: productData.Description,
                    slug: productData.Name.ToSlug(),
                    isDigital: productData.IsDigital,
                    makeActiveAt: DateTimeOffset.UtcNow);

                if (productResult.IsError)
                {
                    _logger.Error("Failed to create product '{ProductName}': {Errors}", productData.Name, productResult.FirstError.Description);
                    continue;
                }

                var product = productResult.Value;

                // Store ProductData in PublicMetadata for later use by Variant seeder
                if (product.PublicMetadata == null)
                {
                    product.PublicMetadata = new Dictionary<string, object?>();
                }
                product.PublicMetadata["HasVariants"] = productData.HasVariants;
                product.PublicMetadata["BasePrice"] = productData.Price;
                product.PublicMetadata["BaseCompareAtPrice"] = productData.CompareAtPrice;
                product.PublicMetadata["WeightGrams"] = productData.WeightGrams;
                
                // Add to default store
                var storeProductResult = store.AddProduct(product);
                if (storeProductResult.IsError)
                {
                    _logger.Error("Failed to add product '{ProductName}' to store: {Errors}", productData.Name, storeProductResult.FirstError.Description);
                    // Decide if you want to continue or throw
                }

                // Add classifications (taxons)
                foreach (var taxonSlug in productData.TaxonSlugs)
                {
                    var taxon = taxons.FirstOrDefault(t => t.Name == taxonSlug);
                    if (taxon != null)
                    {
                        var classificationResult = product.AddClassification(Core.Domain.Catalog.Products.Classifications.Classification.Create(product.Id, taxon.Id).Value);
                        if (classificationResult.IsError)
                        {
                            _logger.Warning("Failed to add classification for product '{ProductName}' and taxon '{TaxonSlug}': {Errors}", productData.Name, taxonSlug, classificationResult.FirstError.Description);
                        }
                    }
                    else
                    {
                        _logger.Warning("Taxon with slug '{TaxonSlug}' not found for product '{ProductName}'.", taxonSlug, productData.Name);
                    }
                }

                // Add product property values
                product.AddProductProperty(Core.Domain.Catalog.Products.PropertyTypes.ProductPropertyType.Create(product.Id, materialProperty.Id, productData.Material).Value);
                product.AddProductProperty(Core.Domain.Catalog.Products.PropertyTypes.ProductPropertyType.Create(product.Id, weightProperty.Id, productData.WeightGrams).Value);
                product.AddProductProperty(Core.Domain.Catalog.Products.PropertyTypes.ProductPropertyType.Create(product.Id, ecoFriendlyProperty.Id, productData.IsEcoFriendly).Value);
                product.AddProductProperty(Core.Domain.Catalog.Products.PropertyTypes.ProductPropertyType.Create(product.Id, descriptionProperty.Id, productData.Description).Value);


                // Get the master variant
                var masterVariantResult = product.GetMaster();
                if (masterVariantResult.IsError)
                {
                    _logger.Error("Failed to get master variant for product '{ProductName}': {Errors}", productData.Name, masterVariantResult.FirstError.Description);
                    continue;
                }
                var masterVariant = masterVariantResult.Value;

                // Set prices for the master variant
                masterVariant.SetPrice(productData.Price, productData.CompareAtPrice, store.DefaultCurrency); // Changed store.Currency to store.DefaultCurrency

                // Add initial stock for master variant
                var stockItemResult = stockLocation.Restock(masterVariant, 100, StockMovement.MovementOriginator.Adjustment); // Changed to use Restock method, fully qualified StockMovement
                if (stockItemResult.IsError)
                {
                    _logger.Error("Failed to add stock for master variant of '{ProductName}': {Errors}", productData.Name, stockItemResult.FirstError.Description);
                }

                // Add images to product
                var productImageResult = ProductImage.Create(
                    url: $"https://picsum.photos/seed/{productData.Name.Replace(" ", "-")}/800/800",
                    alt: $"{productData.Name} image",
                    type: nameof(ProductImage.ProductImageType.Default),
                    contentType: "image/jpeg",
                    width: 800,
                    height: 800);

                if (productImageResult.IsError)
                {
                    _logger.Error("Failed to create primary image for product '{ProductName}': {Errors}", productData.Name, productImageResult.FirstError.Description);
                }
                else
                {
                    product.AddImage(productImageResult.Value);
                }
                
                dbContext.Set<Product>().Add(product);
                _logger.Information("Created product '{ProductName}' with ID: {ProductId}", product.Name, product.Id);
                // Ensure events are handled by the product aggregate itself before saving
                product.Activate(); // Activate the product
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.Information("Catalog Product data seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error(exception: ex,
                messageTemplate: "Catalog Product data seeding failed with exception: {ErrorMessage}",
                propertyValue: ex.Message);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private class ProductData
    {
        public string Name { get; }
        public string Description { get; }
        public List<string> TaxonSlugs { get; }
        public bool HasVariants { get; }
        public bool IsDigital { get; }
        public decimal Price { get; }
        public decimal CompareAtPrice { get; }
        public string Material { get; }
        public string WeightGrams { get; }
        public string IsEcoFriendly { get; }

        public ProductData(string name, string description, List<string> taxonSlugs, bool hasVariants, bool isDigital, decimal price, decimal compareAtPrice, string material, string weightGrams, string isEcoFriendly)
        {
            Name = name;
            Description = description;
            TaxonSlugs = taxonSlugs;
            HasVariants = hasVariants;
            IsDigital = isDigital;
            Price = price;
            CompareAtPrice = compareAtPrice;
            Material = material;
            WeightGrams = weightGrams;
            IsEcoFriendly = isEcoFriendly;
        }
    }
}
