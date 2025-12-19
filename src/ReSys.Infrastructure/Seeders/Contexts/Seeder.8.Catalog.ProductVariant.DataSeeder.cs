using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using ReSys.Core.Common.Extensions;
using ReSys.Core.Domain.Catalog.OptionTypes;
using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Images;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Inventories.Movements;
using ReSys.Core.Domain.Stores;
using ReSys.Infrastructure.Persistence.Contexts;

using Serilog;

namespace ReSys.Infrastructure.Seeders.Contexts;

public sealed class CatalogProductVariantDataSeeder(IServiceProvider serviceProvider) : IDataSeeder
{
    private readonly ILogger _logger = Log.ForContext<CatalogProductVariantDataSeeder>();
    private readonly Random _random = new();

    public int Order => 80; // Run after CatalogProductDataSeeder

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _logger.Information("Starting catalog Variant data seeding.");

        try
        {
            // Retrieve necessary data from previous seeders
            var store = await dbContext.Set<Store>().FirstAsync(s => s.Code == "DEFAULT", cancellationToken);
            var colorOptionType = await dbContext.Set<OptionType>().Include(ot => ot.OptionValues)
                .FirstAsync(ot => ot.Name == "color", cancellationToken);
            var sizeOptionType = await dbContext.Set<OptionType>().Include(ot => ot.OptionValues)
                .FirstAsync(ot => ot.Name == "size", cancellationToken);

            var availableColors = colorOptionType.OptionValues.ToList();
            var availableSizes = sizeOptionType.OptionValues.ToList();

            // Ensure stock locations exist
            var stockLocation = await dbContext.Set<StockLocation>()
                .FirstOrDefaultAsync(sl => sl.Name == "Main Warehouse".ToSlug(), cancellationToken);
            if (stockLocation == null)
            {
                // This should ideally be seeded by a dedicated StockLocation seeder
                // For now, re-create if not found (shouldn't happen if previous seeders ran)
                var stockLocationResult = StockLocation.Create("Main Warehouse", "MW", true);
                if (stockLocationResult.IsError)
                {
                    _logger.Error("Failed to create default stock location: {Errors}",
                        stockLocationResult.FirstError.ToString());
                    throw new InvalidOperationException("Failed to create default stock location.");
                }

                stockLocation = stockLocationResult.Value;
                dbContext.Set<StockLocation>().Add(stockLocation);
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.Information("Created default stock location: {Name}", stockLocation.Name);
            }

            // Get all products that were just seeded
            var products = await dbContext.Set<Product>()
                .Include(m => m.Classifications)
                .ThenInclude(m => m.Taxon)
                .ThenInclude(m=>m.Taxonomy)
                .Include(p => p.Variants)
                .ThenInclude(v => v.Prices)
                .Include(p => p.Variants)
                .ThenInclude(v => v.StockItems)
                .ToListAsync(cancellationToken);

            foreach (var product in products)
            {
                // Retrieve data from product's PublicMetadata
                bool hasVariants = product.HasVariants;
                var masterVariantResult = product.GetMaster();
                if (masterVariantResult.IsError)
                {
                    _logger.Error("Failed to get master variant for product '{ProductName}': {Errors}", product.Name, masterVariantResult.FirstError.Description);
                    continue; 
                }
                var masterVariant = masterVariantResult.Value;
                decimal basePrice = masterVariant.PriceIn(store.DefaultCurrency) ?? 0m;
                decimal baseCompareAtPrice = masterVariant.CompareAtPriceIn(store.DefaultCurrency) ?? 0m;
                string baseWeightGrams = "0"; // Default value
                if (product.PublicMetadata != null && product.PublicMetadata.TryGetValue("WeightGrams", out object? weightGramsObj))
                {
                    baseWeightGrams = (weightGramsObj as string) ?? "0";
                }

                if (hasVariants)
                {
                    foreach (var color in availableColors.Take(_random.Next(2, 4))) // 2-3 random colors
                    {
                        foreach (var size in availableSizes.Take(_random.Next(2, 4))) // 2-3 random sizes
                        {
                            var variantSku = $"{product.Name.ToSlug()}-{color.Name}-{size.Name}";
                            var nonMasterVariantResult = Variant.Create(
                                productId: product.Id,
                                sku: variantSku,
                                isMaster: false,
                                weight: Convert.ToDecimal(baseWeightGrams) / 1000m, // Convert to kg for variant
                                weightUnit: "kg");

                            if (nonMasterVariantResult.IsError)
                            {
                                _logger.Error(
                                    "Failed to create variant '{VariantSku}' for product '{ProductName}': {Errors}",
                                    variantSku, product.Name, nonMasterVariantResult.FirstError.Description);
                                continue;
                            }

                            var nonMasterVariant = nonMasterVariantResult.Value;

                            nonMasterVariant.SetPrice(basePrice + _random.Next(-5, 5),
                                baseCompareAtPrice + _random.Next(-5, 5), store.DefaultCurrency);
                            nonMasterVariant.AddOptionValue(color);
                            nonMasterVariant.AddOptionValue(size);

                            // Add stock for non-master variant
                            var nonMasterStockItemResult = stockLocation.Restock(nonMasterVariant, _random.Next(20, 50),
                                StockMovement.MovementOriginator.Adjustment); // Fully qualified StockMovement
                            if (nonMasterStockItemResult.IsError)
                            {
                                _logger.Error(
                                    "Failed to add stock for variant '{VariantSku}' of '{ProductName}': {Errors}",
                                    variantSku, product.Name, nonMasterStockItemResult.FirstError.Description);
                            }

                            // Add variant-specific image
                            var variantImageResult = ProductImage.Create(
                                url: $"https://picsum.photos/seed/{variantSku.Replace(" ", "-")}/600/600",
                                alt: $"{product.Name} {color.Presentation} {size.Presentation} image",
                                type: nameof(ProductImage.ProductImageType.Gallery),
                                contentType: "image/jpeg",
                                width: 600,
                                height: 600,
                                variantId: nonMasterVariant.Id);

                            if (variantImageResult.IsError)
                            {
                                _logger.Error("Failed to create image for variant '{VariantSku}': {Errors}", variantSku,
                                    variantImageResult.FirstError.Description);
                            }
                            else
                            {
                                nonMasterVariant.AddAsset(variantImageResult.Value);
                            }

                            // Product.AddVariant also adds a domain event, but we're already creating the variant directly here
                            // We need to ensure the relationship is established in EF Core.
                            product.Variants.Add(nonMasterVariant);
                        }
                    }
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.Information("Catalog Variant data seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error(exception: ex,
                messageTemplate: "Catalog Variant data seeding failed with exception: {ErrorMessage}",
                propertyValue: ex.Message);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}