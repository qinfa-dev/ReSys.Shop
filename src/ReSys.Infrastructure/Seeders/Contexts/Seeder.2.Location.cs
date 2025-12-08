using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using ReSys.Core.Domain.Location.Countries;
using ReSys.Core.Domain.Location.States;
using ReSys.Infrastructure.Persistence.Contexts;

using Serilog;

namespace ReSys.Infrastructure.Seeders.Contexts;

public sealed class LocationDataSeeder(IServiceProvider serviceProvider) : IDataSeeder
{
    private readonly ILogger _logger = Log.ForContext<LocationDataSeeder>();

    public int Order => 2; // Run after IdentityDataSeeder

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        _logger.Information("Starting location (countries + states/provinces) seeding...");

        await SeedVietnamAsync(dbContext, cancellationToken);
        await SeedUnitedStatesAsync(dbContext, cancellationToken);

        _logger.Information("Location seeding completed successfully.");
    }

    private async Task SeedVietnamAsync(ApplicationDbContext db, CancellationToken ct)
    {
        const string iso = "VN";
        const string iso3 = "VNM";
        const string name = "Vietnam";

        var country = await EnsureCountryAsync(db, name, iso, iso3, ct);

        var existingStateNames = await db.Set<State>()
            .Where(s => s.CountryId == country.Id)
            .Select(s => s.Name)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, ct);

        var vietnamProvinces = GetVietnamProvinces();
        var newStates = vietnamProvinces
            .Where(p => !existingStateNames.Contains(p.Name))
            .Select(p => State.Create(p.Name, p.Abbr, country.Id).Value)
            .ToList();

        if (newStates.Any())
        {
            await db.Set<State>().AddRangeAsync(newStates, ct);
            await db.SaveChangesAsync(ct);
            _logger.Information("Added {Count} provinces/cities for Vietnam (post-2025 mergers)", newStates.Count);
        }
        else
        {
            _logger.Information("All Vietnam provinces already seeded");
        }
    }

    private async Task SeedUnitedStatesAsync(ApplicationDbContext db, CancellationToken ct)
    {
        const string iso = "US";
        const string iso3 = "USA";
        const string name = "United States";

        var country = await EnsureCountryAsync(db, name, iso, iso3, ct);

        var existingStateNames = await db.Set<State>()
            .Where(s => s.CountryId == country.Id)
            .Select(s => s.Name)
            .ToHashSetAsync(StringComparer.OrdinalIgnoreCase, ct);

        var usStates = GetUsStates(); // Full 50 states + DC (no territories)
        var newStates = usStates
            .Where(p => !existingStateNames.Contains(p.Name))
            .Select(p => State.Create(p.Name, p.Abbr, country.Id).Value)
            .ToList();

        if (newStates.Any())
        {
            await db.Set<State>().AddRangeAsync(newStates, ct);
            await db.SaveChangesAsync(ct);
            _logger.Information("Added {Count} US states", newStates.Count);
        }
        else
        {
            _logger.Information("All US states already seeded");
        }
    }

    private async Task<Country> EnsureCountryAsync(ApplicationDbContext db, string name, string iso, string iso3, CancellationToken ct)
    {
        var country = await db.Set<Country>()
            .FirstOrDefaultAsync(c => c.Iso == iso, ct);

        if (country == null)
        {
            country = Country.Create(name, iso, iso3).Value;
            await db.Set<Country>().AddAsync(country, ct);
            await db.SaveChangesAsync(ct);
            _logger.Information("Created country: {CountryName} ({Iso})", name, iso);
        }

        return country;
    }

    // ==================================================================
    // DATA SOURCES (no external API call needed)
    // ==================================================================

    // Extracted directly from the provided SQL insert (34 units as of late 2025)
    // Using 'name' from SQL for State.Name (Vietnamese with accents); Abbr remains null
    private static IEnumerable<(string Name, string Abbr)> GetVietnamProvinces() =>
    [
        // Provinces (28)
        ("Cao Bằng", "CBG"),
        ("Lạng Sơn", "LSN"),
        ("Phú Thọ", "PTO"),
        ("Quảng Ninh", "QNH"),
        ("Thái Nguyên", "TNG"),
        ("Tuyên Quang", "TQG"),
        ("Lào Cai", "LCI"),
        ("Điện Biên", "DBN"),
        ("Lai Châu", "LCU"),
        ("Sơn La", "SLA"),
        ("Bắc Ninh", "BNH"),
        ("Hưng Yên", "HYN"),
        ("Ninh Bình", "NBH"),
        ("Hà Tĩnh", "HTH"),
        ("Nghệ An", "NAN"),
        ("Quảng Trị", "QTI"),
        ("Thanh Hóa", "THO"),
        ("Đắk Lắk", "DLA"),
        ("Gia Lai", "GLA"),
        ("Lâm Đồng", "LDG"),
        ("Khánh Hòa", "KHA"),
        ("Quảng Ngãi", "QNG"),
        ("Đồng Nai", "DNA"),
        ("Tây Ninh", "TNI"),
        ("An Giang", "AGI"),
        ("Cà Mau", "CMA"),
        ("Đồng Tháp", "DTP"),
        ("Vĩnh Long", "VLG"),

        // Centrally-run municipalities (6)
        ("Hà Nội", "HN"),
        ("Hải Phòng", "HPG"),
        ("Huế", "HUE"),
        ("Đà Nẵng", "DNG"),
        ("Hồ Chí Minh City", "HCM"),
        ("Cần Thơ", "CTH")
    ];

    // Full 50 states + DC (most common in e-commerce; no territories)
    private static IEnumerable<(string Name, string Abbr)> GetUsStates() =>
    [
        ("Alabama", "AL"),
        ("Alaska", "AK"),
        ("Arizona", "AZ"),
        ("Arkansas", "AR"),
        ("California", "CA"),
        ("Colorado", "CO"),
        ("Connecticut", "CT"),
        ("Delaware", "DE"),
        ("Florida", "FL"),
        ("Georgia", "GA"),
        ("Hawaii", "HI"),
        ("Idaho", "ID"),
        ("Illinois", "IL"),
        ("Indiana", "IN"),
        ("Iowa", "IA"),
        ("Kansas", "KS"),
        ("Kentucky", "KY"),
        ("Louisiana", "LA"),
        ("Maine", "ME"),
        ("Maryland", "MD"),
        ("Massachusetts", "MA"),
        ("Michigan", "MI"),
        ("Minnesota", "MN"),
        ("Mississippi", "MS"),
        ("Missouri", "MO"),
        ("Montana", "MT"),
        ("Nebraska", "NE"),
        ("Nevada", "NV"),
        ("New Hampshire", "NH"),
        ("New Jersey", "NJ"),
        ("New Mexico", "NM"),
        ("New York", "NY"),
        ("North Carolina", "NC"),
        ("North Dakota", "ND"),
        ("Ohio", "OH"),
        ("Oklahoma", "OK"),
        ("Oregon", "OR"),
        ("Pennsylvania", "PA"),
        ("Rhode Island", "RI"),
        ("South Carolina", "SC"),
        ("South Dakota", "SD"),
        ("Tennessee", "TN"),
        ("Texas", "TX"),
        ("Utah", "UT"),
        ("Vermont", "VT"),
        ("Virginia", "VA"),
        ("Washington", "WA"),
        ("West Virginia", "WV"),
        ("Wisconsin", "WI"),
        ("Wyoming", "WY"),
        ("District of Columbia", "DC")
    ];

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}