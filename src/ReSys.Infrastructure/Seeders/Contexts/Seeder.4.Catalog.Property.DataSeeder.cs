using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Catalog.PropertyTypes;
using ReSys.Infrastructure.Persistence.Contexts;

using Serilog;

namespace ReSys.Infrastructure.Seeders.Contexts;

public sealed class CatalogPropertyDataSeeder(IServiceProvider serviceProvider) : IDataSeeder
{
    private readonly ILogger _logger = Log.ForContext<CatalogPropertyDataSeeder>();

    public int Order => 4; // Run after CatalogOptionTypeDataSeeder

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _logger.Information("Starting catalog Property data seeding.");

        try
        {
            await EnsurePropertiesExistAsync(dbContext: dbContext,
                cancellationToken: cancellationToken);

            _logger.Information("Catalog Property data seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error(exception: ex,
                messageTemplate: "Catalog Property data seeding failed with exception: {ErrorMessage}",
                propertyValue: ex.Message);
            throw;
        }
    }

    private async Task EnsurePropertiesExistAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var propertiesToSeed = new List<PropertyType>
        {
            PropertyType.Create(
                name: "material",
                presentation: "Material",
                kind: PropertyType.PropertyKind.ShortText,
                filterable: true,
                displayOn: DisplayOn.Storefront,
                position: 20).Value,
            PropertyType.Create(
                name: "weight-g",
                presentation: "Weight (g)",
                kind: PropertyType.PropertyKind.Number,
                filterable: false,
                displayOn: DisplayOn.BackEnd,
                position: 30).Value,
            PropertyType.Create(
                name: "is-eco-friendly",
                presentation: "Eco-Friendly",
                kind: PropertyType.PropertyKind.Boolean,
                filterable: true,
                displayOn: DisplayOn.Both,
                position: 40).Value,
            PropertyType.Create(
                name: "description",
                presentation: "Description",
                kind: PropertyType.PropertyKind.LongText,
                filterable: false,
                displayOn: DisplayOn.Storefront,
                position: 50).Value,
        };

        foreach (var property in propertiesToSeed)
        {
            var existingProperty = await dbContext.Set<PropertyType>()
                .FirstOrDefaultAsync(p => p.Name == property.Name, cancellationToken);

            if (existingProperty == null)
            {
                dbContext.Set<PropertyType>().Add(property);
                _logger.Information("Added new Property: {PropertyName}", property.Name);
            }
            else
            {
                _logger.Information("Property '{PropertyName}' already exists.", property.Name);
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
