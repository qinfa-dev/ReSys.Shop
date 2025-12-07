# Migration Guide: Current to Enhanced Inventory Location Design

## Overview

This guide provides step-by-step instructions to migrate from the current simple location design to the enhanced multi-tiered strategy.

**Timeline**: 4 weeks  
**Team Size**: 2-3 developers  
**Risk Level**: Low (feature flagged and backward compatible)  
**Testing**: ~30-40 test cases

---

## Pre-Migration Checklist

- [ ] Review and approve enhanced design with team
- [ ] Get stakeholder buy-in on timeline
- [ ] Schedule knowledge transfer session
- [ ] Backup production database
- [ ] Create feature branch: `feature/inventory-location-enhancement`
- [ ] Set up monitoring/alerting for new metrics
- [ ] Prepare rollback procedure
- [ ] Document current business rules

---

## Phase 1: Database & Entity Structure (Days 1-2)

### Step 1.1: Create Database Migration

```csharp
// Migrations/20251203_AddLocationEnhancements.cs

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReSys.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add LocationType column (nullable initially for backward compatibility)
            migrationBuilder.AddColumn<int>(
                name: "LocationType",
                table: "StockLocations",
                type: "int",
                nullable: true);

            // Add Priority column
            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "StockLocations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Add IsActive column
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "StockLocations",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Add Capabilities as JSON column
            migrationBuilder.AddColumn<string>(
                name: "Capabilities",
                table: "StockLocations",
                type: "jsonb",
                nullable: true);

            // Migrate existing data: Set all locations to Warehouse type (safest default)
            migrationBuilder.Sql(@"
                UPDATE ""StockLocations""
                SET 
                    ""LocationType"" = 0,  -- Warehouse
                    ""Capabilities"" = json_build_object(
                        'CanFulfillOnline', true,
                        'CanFulfillInStore', false,
                        'CanReceiveShipments', true,
                        'CanProcessReturns', true,
                        'MaxDailyOrders', 10000,
                        'SupportedServices', json_build_array()
                    )
                WHERE ""LocationType"" IS NULL
            ");

            // Make LocationType non-nullable
            migrationBuilder.AlterColumn<int>(
                name: "LocationType",
                table: "StockLocations",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // Add index for efficient queries
            migrationBuilder.CreateIndex(
                name: "IX_StockLocations_Type_Priority",
                table: "StockLocations",
                columns: new[] { "LocationType", "Priority" });

            // Create StoreStockLocations bridge table
            migrationBuilder.CreateTable(
                name: "StoreStockLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ServiceLevel = table.Column<string>(type: "text", nullable: false, defaultValue: "Standard"),
                    AvailableFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AvailableUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoreStockLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoreStockLocations_StockLocations_StockLocationId",
                        column: x => x.StockLocationId,
                        principalTable: "StockLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes on bridge table
            migrationBuilder.CreateIndex(
                name: "IX_StoreStockLocations_StoreId_StockLocationId",
                table: "StoreStockLocations",
                columns: new[] { "StoreId", "StockLocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoreStockLocations_Priority",
                table: "StoreStockLocations",
                column: "Priority");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StoreStockLocations");

            migrationBuilder.DropIndex(
                name: "IX_StockLocations_Type_Priority",
                table: "StockLocations");

            migrationBuilder.DropColumn(
                name: "LocationType",
                table: "StockLocations");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "StockLocations");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "StockLocations");

            migrationBuilder.DropColumn(
                name: "Capabilities",
                table: "StockLocations");
        }
    }
}
```

### Step 1.2: Add New Properties to StockLocation Entity

```csharp
// In ReSys.Core/Domain/Inventory/Locations/StockLocation.cs

public sealed class StockLocation : Aggregate<Guid>, 
    IAddress, 
    IHasParameterizableName, 
    IHasUniqueName, 
    IHasMetadata,
    ISoftDeletable
{
    #region NEW: Location Type and Capabilities
    
    public LocationType Type { get; private set; } = LocationType.Warehouse;
    
    public LocationCapabilities Capabilities { get; private set; } = null!;
    
    public int Priority { get; private set; } = 0;
    
    public bool IsActive { get; private set; } = true;
    
    #endregion
    
    #region NEW: Store Associations
    
    public ICollection<StoreStockLocation> StoreStockLocations { get; private set; } 
        = new List<StoreStockLocation>();
    
    public ICollection<Store> Stores => StoreStockLocations
        .Where(ssl => ssl.IsActive)
        .Select(ssl => ssl.Store)
        .ToList();
    
    #endregion
    
    // Keep existing properties and methods...
    
    #region Enums
    
    public enum LocationType
    {
        Warehouse = 0,
        RetailStore = 1,
        FulfillmentCenter = 2,
        DropShip = 3,
        CrossDock = 4
    }
    
    #endregion
    
    #region Updated Factory Method
    
    public static ErrorOr<StockLocation> Create(
        string name,
        string? presentation = null,
        LocationType type = LocationType.Warehouse,
        LocationCapabilities? capabilities = null,
        int priority = 0,
        bool isActive = true,
        bool isDefaultForType = false,
        Guid? countryId = null,
        string? address1 = null,
        string? address2 = null,
        string? city = null,
        string? zipcode = null,
        Guid? stateId = null,
        string? phone = null,
        string? company = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        (name, presentation) = HasParameterizableName.NormalizeParams(
            name: name, 
            presentation: presentation);

        // Default capabilities based on location type if not provided
        capabilities ??= GetDefaultCapabilities(type);

        var location = new StockLocation
        {
            Id = Guid.NewGuid(),
            Name = name,
            Presentation = presentation,
            Type = type,
            Capabilities = capabilities,
            Priority = priority,
            IsActive = isActive,
            Active = isActive,
            Default = false,  // Keep for backward compatibility, will deprecate
            CreatedAt = DateTimeOffset.UtcNow,
            CountryId = countryId,
            Address1 = address1?.Trim(),
            Address2 = address2?.Trim(),
            City = city?.Trim(),
            Zipcode = zipcode?.Trim(),
            StateId = stateId,
            Phone = phone?.Trim(),
            Company = company?.Trim(),
            PublicMetadata = publicMetadata,
            PrivateMetadata = privateMetadata
        };

        location.AddDomainEvent(new Events.Created(StockLocationId: location.Id));
        return location;
    }
    
    private static LocationCapabilities GetDefaultCapabilities(LocationType type)
    {
        return type switch
        {
            LocationType.Warehouse => new LocationCapabilities
            {
                CanFulfillOnline = true,
                CanFulfillInStore = false,
                CanReceiveShipments = true,
                CanProcessReturns = true,
                MaxDailyOrders = 10000,
                SupportedServices = new List<string> { "Standard", "Express", "NextDay", "TwoDay" }
            },
            LocationType.RetailStore => new LocationCapabilities
            {
                CanFulfillOnline = true,  // BOPIS
                CanFulfillInStore = true,
                CanReceiveShipments = true,
                CanProcessReturns = true,
                MaxDailyOrders = 500,
                SupportedServices = new List<string> { "LocalPickup", "Standard" }
            },
            LocationType.FulfillmentCenter => new LocationCapabilities
            {
                CanFulfillOnline = true,
                CanFulfillInStore = false,
                CanReceiveShipments = true,
                CanProcessReturns = false,
                MaxDailyOrders = 5000,
                SupportedServices = new List<string> { "Standard", "Express" }
            },
            LocationType.DropShip => new LocationCapabilities
            {
                CanFulfillOnline = true,
                CanFulfillInStore = false,
                CanReceiveShipments = false,
                CanProcessReturns = false,
                MaxDailyOrders = 3000,
                SupportedServices = new List<string> { "Standard" }
            },
            LocationType.CrossDock => new LocationCapabilities
            {
                CanFulfillOnline = false,
                CanFulfillInStore = false,
                CanReceiveShipments = true,
                CanProcessReturns = false,
                MaxDailyOrders = 0,
                SupportedServices = new List<string>()
            },
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
    
    #endregion
}
```

### Step 1.3: Create Bridge Entity

```csharp
// ReSys.Core/Domain/Inventory/Locations/StoreStockLocation.cs

namespace ReSys.Core.Domain.Inventory.Locations;

public sealed class StoreStockLocation : Entity<Guid>
{
    public Guid StoreId { get; private set; }
    public Guid StockLocationId { get; private set; }
    public bool IsPrimary { get; private set; }
    public int Priority { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string ServiceLevel { get; private set; } = "Standard";
    
    // Time-based availability
    public DateTimeOffset? AvailableFrom { get; private set; }
    public DateTimeOffset? AvailableUntil { get; private set; }
    
    public DateTimeOffset AssignedAt { get; private set; }

    // Navigation
    public StockLocation StockLocation { get; private set; } = null!;
    public Store Store { get; private set; } = null!;

    private StoreStockLocation() { }

    public static ErrorOr<StoreStockLocation> Create(
        Guid storeId,
        Guid stockLocationId,
        bool isPrimary = false,
        int priority = 0,
        string serviceLevel = "Standard",
        DateTimeOffset? availableFrom = null,
        DateTimeOffset? availableUntil = null)
    {
        if (storeId == Guid.Empty)
            return Error.Validation("StoreStockLocation.InvalidStore", "Store ID is required");

        if (stockLocationId == Guid.Empty)
            return Error.Validation("StoreStockLocation.InvalidLocation", "Location ID is required");

        return new StoreStockLocation
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            StockLocationId = stockLocationId,
            IsPrimary = isPrimary,
            Priority = priority,
            IsActive = true,
            ServiceLevel = serviceLevel,
            AvailableFrom = availableFrom,
            AvailableUntil = availableUntil,
            AssignedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public ErrorOr<StoreStockLocation> MakePrimary()
    {
        if (IsPrimary)
            return this;

        IsPrimary = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        return this;
    }

    public ErrorOr<StoreStockLocation> Deactivate()
    {
        if (!IsActive)
            return this;

        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        return this;
    }
}
```

### Step 1.4: EF Core Configuration

```csharp
// ReSys.Infrastructure/Persistence/Inventory/Configurations/StockLocationConfiguration.cs

namespace ReSys.Infrastructure.Persistence.Inventory.Configurations;

public class StockLocationConfiguration : IEntityTypeConfiguration<StockLocation>
{
    public void Configure(EntityTypeBuilder<StockLocation> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Type)
            .HasConversion<int>()
            .HasDefaultValue(StockLocation.LocationType.Warehouse);
        
        builder.Property(e => e.Priority)
            .HasDefaultValue(0);
        
        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);
        
        // JSON column for capabilities
        builder.Property(e => e.Capabilities)
            .HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                v => JsonSerializer.Deserialize<LocationCapabilities>(v, new JsonSerializerOptions()) 
                    ?? LocationCapabilities.CreateDefault())
            .HasColumnType("jsonb");
        
        // Relationships
        builder.HasMany<StoreStockLocation>()
            .WithOne(ssl => ssl.StockLocation)
            .HasForeignKey(ssl => ssl.StockLocationId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(e => new { e.Type, e.Priority })
            .HasDatabaseName("IX_StockLocations_Type_Priority");
        
        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_StockLocations_IsActive");
    }
}

// ReSys.Infrastructure/Persistence/Inventory/Configurations/StoreStockLocationConfiguration.cs

public class StoreStockLocationConfiguration : IEntityTypeConfiguration<StoreStockLocation>
{
    public void Configure(EntityTypeBuilder<StoreStockLocation> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.ServiceLevel)
            .HasDefaultValue("Standard");
        
        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);
        
        // Unique constraint: One store-location link
        builder.HasIndex(e => new { e.StoreId, e.StockLocationId })
            .IsUnique()
            .HasDatabaseName("IX_StoreStockLocations_StoreId_StockLocationId");
        
        builder.HasIndex(e => e.Priority)
            .HasDatabaseName("IX_StoreStockLocations_Priority");
    }
}
```

### Step 1.5: Create Value Object for Capabilities

```csharp
// ReSys.Core/Domain/Inventory/Locations/LocationCapabilities.cs

namespace ReSys.Core.Domain.Inventory.Locations;

public sealed class LocationCapabilities : ValueObject
{
    public bool CanFulfillOnline { get; init; }
    public bool CanFulfillInStore { get; init; }
    public bool CanReceiveShipments { get; init; }
    public bool CanProcessReturns { get; init; }
    public int MaxDailyOrders { get; init; }
    public List<string> SupportedServices { get; init; } = new();

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return CanFulfillOnline;
        yield return CanFulfillInStore;
        yield return CanReceiveShipments;
        yield return CanProcessReturns;
        yield return MaxDailyOrders;
        foreach (var service in SupportedServices.OrderBy(s => s))
            yield return service;
    }

    public static LocationCapabilities CreateDefault() => new()
    {
        CanFulfillOnline = true,
        CanFulfillInStore = false,
        CanReceiveShipments = true,
        CanProcessReturns = true,
        MaxDailyOrders = 10000,
        SupportedServices = new List<string>()
    };

    public bool CanFulfillOrder(string orderType) => orderType switch
    {
        "ONLINE" => CanFulfillOnline,
        "INSTORE" => CanFulfillInStore,
        _ => false
    };

    public bool SupportsService(string service) => 
        SupportedServices.Contains(service, StringComparer.OrdinalIgnoreCase);
}
```

### Step 1.6: Run Migration

```bash
# Terminal
cd src/ReSys.API
dotnet ef database update --project ../ReSys.Infrastructure --startup-project .
```

**Verification**:
```bash
# Check migration applied
SELECT VERSION FROM __EFMigrationsHistory 
WHERE MigrationName = 'AddLocationEnhancements';
```

---

## Phase 2: Service Layer Implementation (Days 3-5)

### Step 2.1: Create IDefaultLocationService

```csharp
// ReSys.Application/Inventory/Services/IDefaultLocationService.cs

namespace ReSys.Application.Inventory.Services;

public interface IDefaultLocationService
{
    Task<ErrorOr<StockLocation>> GetDefaultFulfillmentLocationAsync(
        Guid? storeId = null,
        string? orderType = null,
        CancellationToken cancellationToken = default);

    Task<StockLocation> GetDefaultReceivingLocationAsync(
        Guid? supplierId = null,
        CancellationToken cancellationToken = default);

    Task<List<StockLocation>> GetAvailableLocationsForFulfillmentAsync(
        Guid productVariantId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task<ErrorOr<Success>> AssignStoreToLocationAsync(
        Guid storeId,
        Guid stockLocationId,
        bool isPrimary = false,
        CancellationToken cancellationToken = default);
}
```

### Step 2.2: Implement Service

```csharp
// ReSys.Application/Inventory/Services/DefaultLocationService.cs

namespace ReSys.Application.Inventory.Services;

public class DefaultLocationService : IDefaultLocationService
{
    private readonly InventoryDbContext _dbContext;
    private readonly ILogger<DefaultLocationService> _logger;

    public DefaultLocationService(
        InventoryDbContext dbContext,
        ILogger<DefaultLocationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ErrorOr<StockLocation>> GetDefaultFulfillmentLocationAsync(
        Guid? storeId = null,
        string? orderType = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Strategy 1: Get store's primary location
            if (storeId.HasValue)
            {
                var storeLocation = await GetStorePrimaryLocationAsync(
                    storeId.Value, 
                    cancellationToken);

                if (storeLocation != null)
                {
                    _logger.LogInformation(
                        "Selected store's primary location {LocationId} for store {StoreId}",
                        storeLocation.Id, storeId.Value);
                    
                    return storeLocation;
                }
            }

            // Strategy 2: Get location by order type
            if (!string.IsNullOrEmpty(orderType))
            {
                var orderTypeLocation = await GetLocationByOrderTypeAsync(
                    orderType, 
                    cancellationToken);

                if (orderTypeLocation != null)
                {
                    _logger.LogInformation(
                        "Selected location {LocationId} for order type {OrderType}",
                        orderTypeLocation.Id, orderType);
                    
                    return orderTypeLocation;
                }
            }

            // Strategy 3: Get global default
            var defaultLocation = await GetGlobalDefaultLocationAsync(
                cancellationToken);

            if (defaultLocation != null)
            {
                _logger.LogInformation(
                    "Selected global default location {LocationId}",
                    defaultLocation.Id);
                
                return defaultLocation;
            }

            return Error.NotFound("Location.NoDefault", "No default location configured");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting default fulfillment location");
            return Error.Unexpected("LocationService.Error", ex.Message);
        }
    }

    private async Task<StockLocation?> GetStorePrimaryLocationAsync(
        Guid storeId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.StoreStockLocations
            .Include(ssl => ssl.StockLocation)
            .Where(ssl => ssl.StoreId == storeId)
            .Where(ssl => ssl.IsPrimary)
            .Where(ssl => ssl.IsActive)
            .Where(ssl => ssl.StockLocation.IsActive)
            .OrderBy(ssl => ssl.Priority)
            .Select(ssl => ssl.StockLocation)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<StockLocation?> GetLocationByOrderTypeAsync(
        string orderType,
        CancellationToken cancellationToken)
    {
        var locationType = MapOrderTypeToLocationType(orderType);

        return await _dbContext.StockLocations
            .Where(sl => sl.Type == locationType)
            .Where(sl => sl.IsActive)
            .OrderBy(sl => sl.Priority)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<StockLocation?> GetGlobalDefaultLocationAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.StockLocations
            .Where(sl => sl.IsActive)
            .OrderBy(sl => sl.Priority)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<StockLocation> GetDefaultReceivingLocationAsync(
        Guid? supplierId = null,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.StockLocations
            .Where(sl => sl.Type == StockLocation.LocationType.Warehouse)
            .Where(sl => sl.Capabilities.CanReceiveShipments)
            .Where(sl => sl.IsActive)
            .OrderBy(sl => sl.Priority)
            .FirstAsync(cancellationToken);
    }

    public async Task<List<StockLocation>> GetAvailableLocationsForFulfillmentAsync(
        Guid productVariantId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var locationsWithStock = await _dbContext.StockItems
            .Include(si => si.StockLocation)
            .Where(si => si.VariantId == productVariantId)
            .Where(si => si.CountAvailable >= quantity || si.Backorderable)
            .Where(si => si.StockLocation.IsActive)
            .Where(si => si.StockLocation.Capabilities.CanFulfillOnline)
            .Select(si => si.StockLocation)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (locationsWithStock.Any())
            return locationsWithStock;

        // Fallback: Return active fulfillment-capable locations
        return await _dbContext.StockLocations
            .Where(sl => sl.IsActive)
            .Where(sl => sl.Capabilities.CanFulfillOnline)
            .OrderBy(sl => sl.Priority)
            .Take(5)
            .ToListAsync(cancellationToken);
    }

    public async Task<ErrorOr<Success>> AssignStoreToLocationAsync(
        Guid storeId,
        Guid stockLocationId,
        bool isPrimary = false,
        CancellationToken cancellationToken = default)
    {
        // Check location exists
        var location = await _dbContext.StockLocations
            .FindAsync(new object[] { stockLocationId }, cancellationToken);

        if (location == null)
            return Error.NotFound("Location.NotFound", $"Location {stockLocationId} not found");

        // Check existing assignment
        var existingAssignment = await _dbContext.StoreStockLocations
            .FirstOrDefaultAsync(
                ssl => ssl.StoreId == storeId && ssl.StockLocationId == stockLocationId,
                cancellationToken);

        if (existingAssignment != null)
            return Error.Conflict("StoreLocation.AlreadyAssigned", "This assignment already exists");

        // If making primary, unset other primaries for this store
        if (isPrimary)
        {
            var otherPrimaries = await _dbContext.StoreStockLocations
                .Where(ssl => ssl.StoreId == storeId && ssl.IsPrimary)
                .ToListAsync(cancellationToken);

            foreach (var other in otherPrimaries)
            {
                other.Deactivate();  // Or use update
            }
        }

        // Create new assignment
        var assignmentResult = StoreStockLocation.Create(
            storeId: storeId,
            stockLocationId: stockLocationId,
            isPrimary: isPrimary);

        if (assignmentResult.IsError)
            return assignmentResult.Errors;

        await _dbContext.StoreStockLocations.AddAsync(
            assignmentResult.Value, 
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }

    private StockLocation.LocationType MapOrderTypeToLocationType(string orderType)
    {
        return orderType.ToUpperInvariant() switch
        {
            "ONLINE" => StockLocation.LocationType.FulfillmentCenter,
            "INSTORE_PICKUP" => StockLocation.LocationType.RetailStore,
            "INSTORE_PURCHASE" => StockLocation.LocationType.RetailStore,
            "DROPSHIP" => StockLocation.LocationType.DropShip,
            _ => StockLocation.LocationType.Warehouse
        };
    }
}
```

### Step 2.3: Register Services

```csharp
// ReSys.Core/DependencyInjection.cs (or Inventory Slice DI)

public static class InventoryServiceCollectionExtensions
{
    public static IServiceCollection AddInventoryServices(
        this IServiceCollection services)
    {
        // Default location service
        services.AddScoped<IDefaultLocationService, DefaultLocationService>();

        // Add caching
        services.AddMemoryCache();

        return services;
    }
}

// In Program.cs
builder.Services.AddInventoryServices();
```

---

## Phase 3: Rules Engine Implementation (Days 6-7)

### Step 3.1: Create Rules Interface

```csharp
// ReSys.Application/Inventory/Rules/ILocationPriorityRule.cs

namespace ReSys.Application.Inventory.Rules;

public interface ILocationPriorityRule
{
    /// <summary>Priority order: Higher numbers run first</summary>
    int Priority { get; }

    /// <summary>Whether this rule applies to the current context</summary>
    Task<bool> AppliesAsync(
        StockLocation location, 
        FulfillmentContext context);

    /// <summary>Calculate score for this location (0-100)</summary>
    Task<int> CalculateScoreAsync(
        StockLocation location, 
        FulfillmentContext context);
}

public class FulfillmentContext
{
    public Guid? CustomerId { get; set; }
    public Guid? StoreId { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string OrderType { get; set; } = "ONLINE";
    public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;
    public List<OrderItem> Items { get; set; } = new();

    public record OrderItem(Guid VariantId, int Quantity);
}
```

### Step 3.2: Implement Basic Rules

```csharp
// ReSys.Application/Inventory/Rules/StorePreferenceRule.cs

public class StorePreferenceRule : ILocationPriorityRule
{
    private readonly InventoryDbContext _dbContext;

    public int Priority => 100;  // Highest priority

    public StorePreferenceRule(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> AppliesAsync(
        StockLocation location, 
        FulfillmentContext context)
    {
        return context.StoreId.HasValue;
    }

    public async Task<int> CalculateScoreAsync(
        StockLocation location, 
        FulfillmentContext context)
    {
        if (!context.StoreId.HasValue)
            return 0;

        // Check if this location is primary for the store
        var isPrimary = await _dbContext.StoreStockLocations
            .AnyAsync(ssl => 
                ssl.StoreId == context.StoreId.Value &&
                ssl.StockLocationId == location.Id &&
                ssl.IsPrimary);

        return isPrimary ? 100 : 0;
    }
}

// ReSys.Application/Inventory/Rules/StockAvailabilityRule.cs

public class StockAvailabilityRule : ILocationPriorityRule
{
    private readonly InventoryDbContext _dbContext;

    public int Priority => 90;

    public StockAvailabilityRule(InventoryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> AppliesAsync(
        StockLocation location, 
        FulfillmentContext context)
    {
        return context.Items.Any();
    }

    public async Task<int> CalculateScoreAsync(
        StockLocation location, 
        FulfillmentContext context)
    {
        var totalScore = 0;

        foreach (var item in context.Items)
        {
            var stockItem = await _dbContext.StockItems
                .FirstOrDefaultAsync(si => 
                    si.VariantId == item.VariantId && 
                    si.StockLocationId == location.Id);

            if (stockItem == null)
            {
                totalScore += 0;
            }
            else if (stockItem.CountAvailable >= item.Quantity)
            {
                totalScore += 100;
            }
            else if (stockItem.Backorderable)
            {
                totalScore += 50;
            }
            else
            {
                totalScore += 10;
            }
        }

        return context.Items.Count > 0 
            ? totalScore / context.Items.Count 
            : 0;
    }
}

// ReSys.Application/Inventory/Rules/CapabilityMatchRule.cs

public class CapabilityMatchRule : ILocationPriorityRule
{
    public int Priority => 85;

    public async Task<bool> AppliesAsync(
        StockLocation location, 
        FulfillmentContext context)
    {
        return true;
    }

    public async Task<int> CalculateScoreAsync(
        StockLocation location, 
        FulfillmentContext context)
    {
        // Check if location can fulfill this order type
        if (!location.Capabilities.CanFulfillOrder(context.OrderType))
            return 0;

        return 100;
    }
}
```

### Step 3.3: Intelligent Selector

```csharp
// ReSys.Application/Inventory/Services/IntelligentLocationSelector.cs

public class IntelligentLocationSelector
{
    private readonly IEnumerable<ILocationPriorityRule> _rules;
    private readonly ILogger<IntelligentLocationSelector> _logger;

    public IntelligentLocationSelector(
        IEnumerable<ILocationPriorityRule> rules,
        ILogger<IntelligentLocationSelector> logger)
    {
        _rules = rules.OrderByDescending(r => r.Priority);
        _logger = logger;
    }

    public async Task<ErrorOr<StockLocation>> SelectBestLocationAsync(
        List<StockLocation> availableLocations,
        FulfillmentContext context,
        CancellationToken cancellationToken = default)
    {
        if (!availableLocations.Any())
            return Error.NotFound("Location.NoLocations", "No locations available");

        var scoredLocations = new List<(StockLocation Location, int Score, List<(string Rule, int Score)> Breakdown)>();

        foreach (var location in availableLocations)
        {
            var totalScore = 0;
            var ruleCount = 0;
            var breakdown = new List<(string, int)>();

            foreach (var rule in _rules)
            {
                if (await rule.AppliesAsync(location, context))
                {
                    var score = await rule.CalculateScoreAsync(location, context);
                    totalScore += score;
                    ruleCount++;
                    breakdown.Add((rule.GetType().Name, score));
                }
            }

            var averageScore = ruleCount > 0 ? totalScore / ruleCount : 0;
            scoredLocations.Add((location, averageScore, breakdown));
        }

        // Select best location
        var best = scoredLocations
            .OrderByDescending(sl => sl.Score)
            .ThenBy(sl => sl.Location.Priority)
            .FirstOrDefault();

        _logger.LogInformation(
            "Selected location {LocationId} (name: {LocationName}) with score {Score}\n" +
            "Rule breakdown: {Breakdown}",
            best.Location.Id,
            best.Location.Name,
            best.Score,
            string.Join(", ", best.Breakdown.Select(b => $"{b.Rule}:{b.Score}")));

        return best.Location;
    }
}
```

### Step 3.4: Register Rules

```csharp
// Program.cs

builder.Services.AddScoped<ILocationPriorityRule, StorePreferenceRule>();
builder.Services.AddScoped<ILocationPriorityRule, StockAvailabilityRule>();
builder.Services.AddScoped<ILocationPriorityRule, CapabilityMatchRule>();

builder.Services.AddScoped<IntelligentLocationSelector>();
```

---

## Phase 4: Integration & Testing (Days 8-10)

### Step 4.1: Feature Flag Setup

```csharp
// appsettings.json

{
  "FeatureFlags": {
    "UseIntelligentLocationSelection": false,  // Disabled by default
    "LocationSelectionShadowMode": true        // Log decisions without using them
  }
}

// Configuration class
public class FeatureFlags
{
    public bool UseIntelligentLocationSelection { get; set; }
    public bool LocationSelectionShadowMode { get; set; }
}
```

### Step 4.2: Handler Integration

```csharp
// Before: Current implementation
public class FulfillOrderHandler : IRequestHandler<FulfillOrderCommand, ErrorOr<OrderResponse>>
{
    public async Task<ErrorOr<OrderResponse>> Handle(FulfillOrderCommand request, CancellationToken ct)
    {
        var location = await _dbContext.StockLocations
            .FirstAsync(l => l.Default, ct);  // ← Old way
        
        return await CreateShipment(location, request, ct);
    }
}

// After: New implementation with feature flag
public class FulfillOrderHandler : IRequestHandler<FulfillOrderCommand, ErrorOr<OrderResponse>>
{
    private readonly IDefaultLocationService _locationService;
    private readonly IntelligentLocationSelector _selector;
    private readonly IOptions<FeatureFlags> _featureFlags;
    private readonly ILogger<FulfillOrderHandler> _logger;

    public async Task<ErrorOr<OrderResponse>> Handle(FulfillOrderCommand request, CancellationToken ct)
    {
        ErrorOr<StockLocation> locationResult;

        if (_featureFlags.Value.UseIntelligentLocationSelection)
        {
            // New intelligent selection
            var availableLocations = await _locationService
                .GetAvailableLocationsForFulfillmentAsync(
                    request.VariantId, 
                    request.Quantity, ct);

            var context = new FulfillmentContext
            {
                StoreId = request.StoreId,
                OrderType = "ONLINE",
                Items = new List<FulfillmentContext.OrderItem>
                {
                    new(request.VariantId, request.Quantity)
                }
            };

            locationResult = await _selector.SelectBestLocationAsync(
                availableLocations, 
                context, 
                ct);
        }
        else
        {
            // Old way (for now)
            locationResult = await _locationService.GetDefaultFulfillmentLocationAsync(
                storeId: request.StoreId,
                cancellationToken: ct);
        }

        if (locationResult.IsError)
            return locationResult.Errors;

        return await CreateShipment(locationResult.Value, request, ct);
    }
}
```

### Step 4.3: Unit Tests

```csharp
[TestFixture]
public class DefaultLocationServiceTests
{
    private DefaultLocationService _service;
    private Mock<InventoryDbContext> _dbContext;
    private IQueryable<StockLocation> _locationQueryable;
    private IQueryable<StoreStockLocation> _storeLocationQueryable;

    [SetUp]
    public void Setup()
    {
        _dbContext = new Mock<InventoryDbContext>();
        _service = new DefaultLocationService(_dbContext.Object, new Mock<ILogger<DefaultLocationService>>().Object);
    }

    [Test]
    public async Task GetDefaultFulfillmentLocation_WithStoreId_ReturnsPrimaryLocation()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var primaryLocation = CreateStockLocation("NYC Warehouse", priority: 1);
        
        var storeLocation = new StoreStockLocation
        {
            StoreId = storeId,
            StockLocationId = primaryLocation.Id,
            IsPrimary = true,
            StockLocation = primaryLocation
        };

        var storeLocationsData = new[] { storeLocation }.AsQueryable();
        var storeLocationsMock = storeLocationsData.BuildMockDbSet();

        _dbContext.Setup(x => x.StoreStockLocations)
            .Returns(storeLocationsMock.Object);

        // Act
        var result = await _service.GetDefaultFulfillmentLocationAsync(storeId);

        // Assert
        Assert.That(result.IsError, Is.False);
        Assert.That(result.Value.Id, Is.EqualTo(primaryLocation.Id));
    }

    [Test]
    public async Task GetDefaultFulfillmentLocation_NoStore_ReturnsGlobalDefault()
    {
        // Arrange
        var defaultLocation = CreateStockLocation("Central Warehouse", priority: 0);
        var locationsData = new[] { defaultLocation }.AsQueryable();
        var locationsMock = locationsData.BuildMockDbSet();

        _dbContext.Setup(x => x.StockLocations)
            .Returns(locationsMock.Object);

        // Act
        var result = await _service.GetDefaultFulfillmentLocationAsync();

        // Assert
        Assert.That(result.IsError, Is.False);
        Assert.That(result.Value.Id, Is.EqualTo(defaultLocation.Id));
    }

    private StockLocation CreateStockLocation(string name, int priority = 0)
    {
        return new StockLocation
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = StockLocation.LocationType.Warehouse,
            Priority = priority,
            IsActive = true
        };
    }
}

[TestFixture]
public class LocationSelectorRulesTests
{
    [Test]
    public async Task StockAvailabilityRule_LocationWithAllStock_HighestScore()
    {
        // Arrange
        var dbContext = new Mock<InventoryDbContext>();
        var rule = new StockAvailabilityRule(dbContext.Object);
        
        var location = new StockLocation { Id = Guid.NewGuid() };
        var variantId = Guid.NewGuid();
        var context = new FulfillmentContext
        {
            Items = new List<FulfillmentContext.OrderItem>
            {
                new(variantId, 10)
            }
        };

        var stockItem = new StockItem
        {
            VariantId = variantId,
            StockLocationId = location.Id,
            QuantityOnHand = 100,
            QuantityReserved = 20  // CountAvailable = 80
        };

        var stockItemsData = new[] { stockItem }.AsQueryable();
        var stockItemsMock = stockItemsData.BuildMockDbSet();

        dbContext.Setup(x => x.StockItems)
            .Returns(stockItemsMock.Object);

        // Act
        var score = await rule.CalculateScoreAsync(location, context);

        // Assert
        Assert.That(score, Is.EqualTo(100));
    }

    [Test]
    public async Task CapabilityMatchRule_LocationCannotFulfillOrder_ZeroScore()
    {
        // Arrange
        var rule = new CapabilityMatchRule();
        var location = new StockLocation
        {
            Capabilities = new LocationCapabilities
            {
                CanFulfillOnline = false,
                CanFulfillInStore = true
            }
        };

        var context = new FulfillmentContext { OrderType = "ONLINE" };

        // Act
        var score = await rule.CalculateScoreAsync(location, context);

        // Assert
        Assert.That(score, Is.EqualTo(0));
    }
}
```

### Step 4.4: Integration Tests

```csharp
[TestFixture]
public class IntelligentLocationSelectionIntegrationTests
{
    private InventoryDbContext _dbContext;
    private IntelligentLocationSelector _selector;

    [SetUp]
    public void Setup()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase("IntegrationTests")
            .Options;

        _dbContext = new InventoryDbContext(options);
        
        var rules = new ILocationPriorityRule[]
        {
            new StorePreferenceRule(_dbContext),
            new StockAvailabilityRule(_dbContext),
            new CapabilityMatchRule()
        };

        _selector = new IntelligentLocationSelector(
            rules, 
            new Mock<ILogger<IntelligentLocationSelector>>().Object);
    }

    [Test]
    public async Task SelectBestLocation_MultipleRules_SelectsHighestScoredLocation()
    {
        // Arrange: Create test data
        var nyWarehouse = new StockLocation
        {
            Id = Guid.NewGuid(),
            Name = "NY Warehouse",
            Type = StockLocation.LocationType.Warehouse,
            Priority = 1,
            IsActive = true
        };

        var centralWarehouse = new StockLocation
        {
            Id = Guid.NewGuid(),
            Name = "Central Warehouse",
            Type = StockLocation.LocationType.Warehouse,
            Priority = 10,
            IsActive = true
        };

        // ... Add to database and seed stock items

        // Act
        var context = new FulfillmentContext
        {
            StoreId = nyStoreId,  // Should prefer NY warehouse via store preference rule
            OrderType = "ONLINE"
        };

        var best = await _selector.SelectBestLocationAsync(
            new List<StockLocation> { nyWarehouse, centralWarehouse },
            context);

        // Assert
        Assert.That(best.Value.Id, Is.EqualTo(nyWarehouse.Id));
    }
}
```

---

## Phase 5: Data Migration & Rollout (Days 11-14)

### Step 5.1: Seed Store-Location Associations

```csharp
// Migrations/Seeder or Manual Script

public class StoreLocationSeeder
{
    public static async Task SeedAsync(InventoryDbContext dbContext)
    {
        // Get all stores
        var stores = await dbContext.Stores.ToListAsync();
        var defaultWarehouse = await dbContext.StockLocations
            .FirstAsync(l => l.Name == "Central Warehouse");

        foreach (var store in stores)
        {
            // Check if already seeded
            var existingAssignment = await dbContext.StoreStockLocations
                .FirstOrDefaultAsync(ssl => ssl.StoreId == store.Id);

            if (existingAssignment == null)
            {
                var assignment = StoreStockLocation.Create(
                    storeId: store.Id,
                    stockLocationId: defaultWarehouse.Id,
                    isPrimary: true,
                    priority: 0);

                if (assignment.IsError)
                    continue;

                await dbContext.StoreStockLocations.AddAsync(assignment.Value);
            }
        }

        await dbContext.SaveChangesAsync();
    }
}

// In Program.cs setup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await StoreLocationSeeder.SeedAsync(dbContext);
}
```

### Step 5.2: Enable Feature Flag Gradually

```csharp
// appsettings.json
{
  "FeatureFlags": {
    "UseIntelligentLocationSelection": false  // Day 1
  }
}

// Day 2: Manual override for testing
// appsettings.Production.json
{
  "FeatureFlags": {
    "UseIntelligentLocationSelection": true,
    "EnablementPercentage": 10  // 10% of orders
  }
}

// Add percentage-based feature flag
public class PercentageBasedFeatureFlag
{
    public static bool IsEnabled(int percentage)
    {
        return Random.Shared.Next(0, 100) < percentage;
    }
}

// In handler
if (_featureFlags.Value.UseIntelligentLocationSelection 
    && PercentageBasedFeatureFlag.IsEnabled(_featureFlags.Value.EnablementPercentage))
{
    // Use new system
}
```

### Step 5.3: Monitor & Verify

```csharp
// Metrics collection
public class LocationSelectionMetrics
{
    public string Method { get; set; }  // "Legacy" or "Enhanced"
    public Guid SelectedLocationId { get; set; }
    public string SelectedLocationName { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public List<(string RuleName, int Score)> RuleScores { get; set; }
    public FulfillmentOutcome Outcome { get; set; }  // Order shipped, cost, time
}

// Log metrics
_logger.LogInformation(
    "Location selection: Method={Method}, Location={LocationName}, " +
    "Score={Score}, ExecutionTime={ExecutionTimeMs}ms",
    metrics.Method,
    metrics.SelectedLocationName,
    metrics.RuleScores.Sum(r => r.Score),
    metrics.ExecutionTime.TotalMilliseconds);

// Compare old vs new
// - Average ship time
// - Average shipping cost
// - Customer satisfaction (delivery time)
// - Warehouse utilization
```

---

## Phase 6: Cleanup (Days 15-20)

###Step 6.1: Remove Old Code

```csharp
// After feature flag enabled 100%, remove:

// ❌ Old location selection from handlers
var location = await _dbContext.StockLocations
    .Where(l => l.Default)
    .FirstAsync(ct);

// ✅ All handlers now use:
var location = await _locationService.GetDefaultFulfillmentLocationAsync(...);
```

### Step 6.2: Deprecate Old Properties

```csharp
// Mark Default property as obsolete
[Obsolete("Use IDefaultLocationService and StoreStockLocation instead")]
public bool Default { get; private set; }
```

### Step 6.3: Documentation

```markdown
# Inventory Location Selection - Operational Guide

## Quick Reference

### Setting Up Store Locations

```sql
INSERT INTO StoreStockLocations (StoreId, StockLocationId, IsPrimary, Priority)
VALUES (nyc_store_id, nyc_warehouse_id, true, 1),
       (nyc_store_id, central_warehouse_id, false, 2);
```

### Checking Current Configuration

```csharp
// Get all warehouses for a store
var locations = await locationService.GetAvailableLocationsAsync(storeId);
```

## Rules Engine

The location selection uses a priority-based rules engine:

1. **Store Preference** (Priority 100): Prefer store's primary location
2. **Stock Availability** (Priority 90): Prefer locations with stock
3. **Capability Match** (Priority 85): Only use locations supporting order type
4. (More rules can be added)

## Monitoring

Check metrics in Application Insights:
- `location_selection_duration_ms`
- `location_selection_score`
- `location_selection_rule_application`
```

---

## Rollback Procedure

If issues detected:

```bash
# 1. Disable feature flag immediately
appsettings.json: UseIntelligentLocationSelection = false

# 2. Restart application

# 3. Verify reverting to old behavior
# Monitor log: "Selected default location via legacy method"

# 4. Investigate issue
#    Check ApplicationInsights metrics
#    Review rule scoring logs

# 5. After fix, re-enable gradually
appsettings.json: EnablementPercentage = 25  # Start at 25%

# 6. If issue persists, rollback migration
dotnet ef database update <PreviousMigration>
```

---

## Success Criteria

By end of Phase 4, validate:

- [ ] All tests passing
- [ ] Migration applied successfully
- [ ] Service methods returning correct locations
- [ ] Feature flag working (toggle enables/disables new behavior)
- [ ] Metrics collection working
- [ ] No errors in application logs
- [ ] 10-20ms average location selection time
- [ ] Scores make logical sense
- [ ] Operators can configure stores-locations via SQL or API

---

## Troubleshooting

### Problem: Location returns as null

```csharp
// Check: Are there any active locations?
SELECT COUNT(*) FROM StockLocations WHERE IsActive = true;

// Check: Are they properly typed?
SELECT * FROM StockLocations WHERE Type IS NULL;

// Fix: Ensure seed data or migration data population
```

### Problem: Rules engine taking too long

```csharp
// Add indexes
CREATE INDEX idx_stockitems_variant_location ON StockItems(VariantId, StockLocationId);
CREATE INDEX idx_store_locations ON StoreStockLocations(StoreId, IsPrimary);

// Add caching
services.AddMemoryCache();
services.AddScoped<ILocationCache, MemoryCacheLocationSelector>();
```

### Problem: Feature flag not working

```csharp
// Check configuration is loaded
var flags = serviceProvider.GetRequiredService<IOptions<FeatureFlags>>().Value;
Assert.That(flags.UseIntelligentLocationSelection, Is.True);

// Check handler is reading it
_logger.LogInformation("Feature flag value: {Value}", 
    _featureFlags.Value.UseIntelligentLocationSelection);
```

---

## Next Steps After Migration

1. **Develop Additional Rules** (Week 5+)
   - Proximity rule using distance APIs
   - Load balancing rule
   - Seasonal rules

2. **API Endpoints** (Week 6+)
   - GET /api/locations?storeId=&orderType=
   - PUT /api/stores/{storeId}/location/{locationId}
   - GET /api/locations/{locationId}/configuration

3. **UI for Operations** (Week 7+)
   - Store-location assignment UI
   - Rule monitoring dashboard
   - Location performance metrics

4. **Advanced Features** (Week 8+)
   - Time-based location activation
   - A/B testing of selection strategies
   - Machine learning-based optimization
