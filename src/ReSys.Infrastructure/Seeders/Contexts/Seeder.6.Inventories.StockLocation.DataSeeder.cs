using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using ReSys.Core.Common.Extensions;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Infrastructure.Persistence.Contexts;

using Serilog;

namespace ReSys.Infrastructure.Seeders.Contexts;

public sealed class StockLocationDataSeeder(IServiceProvider serviceProvider) : IDataSeeder
{
    private readonly ILogger _logger = Log.ForContext<StockLocationDataSeeder>();

    public int Order => 6; // Run after CatalogTaxonomyDataSeeder, before CatalogProductDataSeeder (which is order 60)

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _logger.Information("Starting StockLocation data seeding.");

        try
        {
            await EnsureStockLocationsExistAsync(dbContext, cancellationToken);

            _logger.Information("StockLocation data seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error(exception: ex,
                messageTemplate: "StockLocation data seeding failed with exception: {ErrorMessage}",
                propertyValue: ex.Message);
            throw;
        }
    }

    private async Task EnsureStockLocationsExistAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var mainWarehouse = await dbContext.Set<StockLocation>().FirstOrDefaultAsync(sl => sl.Name == "Main Warehouse".ToSlug(), cancellationToken);

        if (mainWarehouse == null)
        {
            var stockLocationResult = StockLocation.Create("Main Warehouse", "MW", true);
            if (stockLocationResult.IsError)
            {
                _logger.Error("Failed to create Main Warehouse stock location: {Errors}", stockLocationResult.FirstError.ToString());
                throw new InvalidOperationException("Failed to create Main Warehouse stock location.");
            }
            mainWarehouse = stockLocationResult.Value;
            dbContext.Set<StockLocation>().Add(mainWarehouse);
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.Information("Created default stock location: {Name}", mainWarehouse.Name);
        }
        else
        {
            _logger.Information("Default stock location '{Name}' already exists.", mainWarehouse.Name);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
