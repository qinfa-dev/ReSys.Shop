using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using ReSys.Core.Domain.Catalog.OptionTypes;
using ReSys.Infrastructure.Persistence.Contexts;

using Serilog;

namespace ReSys.Infrastructure.Seeders.Contexts;

public sealed class CatalogOptionTypeDataSeeder(IServiceProvider serviceProvider) : IDataSeeder
{
    private readonly ILogger _logger = Log.ForContext<CatalogOptionTypeDataSeeder>();

    public int Order => 3; // Run after IdentityDataSeeder and LocationDataSeeder

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _logger.Information("Starting catalog OptionType and OptionValue data seeding.");

        try
        {
            await EnsureOptionTypesAndValuesExistAsync(dbContext: dbContext,
                cancellationToken: cancellationToken);

            _logger.Information("Catalog OptionType and OptionValue data seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error(exception: ex,
                messageTemplate: "Catalog OptionType and OptionValue data seeding failed with exception: {ErrorMessage}",
                propertyValue: ex.Message);
            throw;
        }
    }

    private async Task EnsureOptionTypesAndValuesExistAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        // OptionType: Color
        var colorOptionTypeResult = OptionType.Create(
            name: "color",
            presentation: "Color",
            position: 0,
            filterable: true,
            publicMetadata: new Dictionary<string, object?> { { "displayType", "color-swatch" } });

        if (colorOptionTypeResult.IsError)
        {
            _logger.Error("Failed to create Color OptionType: {Errors}", colorOptionTypeResult.FirstError.Description);
            return;
        }

        var colorOptionType = colorOptionTypeResult.Value;
        var existingColorOptionType = await dbContext.Set<OptionType>()
            .Include(ot => ot.OptionValues)
            .FirstOrDefaultAsync(ot => ot.Name == colorOptionType.Name, cancellationToken);

        if (existingColorOptionType == null)
        {
            dbContext.Set<OptionType>().Add(colorOptionType);
            _logger.Information("Added new OptionType: {OptionTypeName}", colorOptionType.Name);
        }
        else
        {
            colorOptionType = existingColorOptionType; // Use existing one to add values
            _logger.Information("OptionType '{OptionTypeName}' already exists.", colorOptionType.Name);
        }

        // Add OptionValues for Color
        var colorValues = new List<(string Name, string Presentation, string Hex)>();
        colorValues.Add(("red", "Red", "#FF0000"));
        colorValues.Add(("blue", "Blue", "#0000FF"));
        colorValues.Add(("green", "Green", "#00FF00"));
        colorValues.Add(("black", "Black", "#000000"));
        colorValues.Add(("white", "White", "#FFFFFF"));

        foreach (var (name, presentation, hex) in colorValues)
        {
            if (!colorOptionType.OptionValues.Any(ov => ov.Name == name))
            {
                var optionValueResult = OptionValue.Create(
                    optionTypeId: colorOptionType.Id,
                    name: name,
                    presentation: presentation,
                    publicMetadata: new Dictionary<string, object?> { { "hexColor", hex } });

                if (optionValueResult.IsError)
                {
                    _logger.Error("Failed to create OptionValue '{OptionValueName}' for OptionType '{OptionTypeName}': {Errors}",
                        name, colorOptionType.Name, optionValueResult.FirstError.Description);
                    continue;
                }
                colorOptionType.AddOptionValue(optionValueResult.Value);
                _logger.Information("Added new OptionValue '{OptionValueName}' to OptionType '{OptionTypeName}'", name, colorOptionType.Name);
            }
        }

        // OptionType: Size
        var sizeOptionTypeResult = OptionType.Create(
            name: "size",
            presentation: "Size",
            position: 1,
            filterable: true);

        if (sizeOptionTypeResult.IsError)
        {
            _logger.Error("Failed to create Size OptionType: {Errors}", sizeOptionTypeResult.FirstError.Description);
            return;
        }

        var sizeOptionType = sizeOptionTypeResult.Value;
        var existingSizeOptionType = await dbContext.Set<OptionType>()
            .Include(ot => ot.OptionValues)
            .FirstOrDefaultAsync(ot => ot.Name == sizeOptionType.Name, cancellationToken);

        if (existingSizeOptionType == null)
        {
            dbContext.Set<OptionType>().Add(sizeOptionType);
            _logger.Information("Added new OptionType: {OptionTypeName}", sizeOptionType.Name);
        }
        else
        {
            sizeOptionType = existingSizeOptionType; // Use existing one to add values
            _logger.Information("OptionType '{OptionTypeName}' already exists.", sizeOptionType.Name);
        }

        // Add OptionValues for Size
        var sizeValues = new List<(string Name, string Presentation)>
        {
            ("xs", "XS"), ("s", "S"), ("m", "M"), ("l", "L"), ("xl", "XL"), ("xxl", "XXL")
        };

        foreach (var (name, presentation) in sizeValues)
        {
            if (!sizeOptionType.OptionValues.Any(ov => ov.Name == name))
            {
                var optionValueResult = OptionValue.Create(
                    optionTypeId: sizeOptionType.Id,
                    name: name,
                    presentation: presentation);

                if (optionValueResult.IsError)
                {
                    _logger.Error("Failed to create OptionValue '{OptionValueName}' for OptionType '{OptionTypeName}': {Errors}",
                        name, sizeOptionType.Name, optionValueResult.FirstError.Description);
                    continue;
                }
                sizeOptionType.AddOptionValue(optionValueResult.Value);
                _logger.Information("Added new OptionValue '{OptionValueName}' to OptionType '{OptionTypeName}'", name, sizeOptionType.Name);
            }
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
