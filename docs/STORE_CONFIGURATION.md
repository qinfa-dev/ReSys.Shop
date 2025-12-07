# Store Configuration Guide

This document describes the ReSys.Shop store configuration system, ported from Spree Commerce.

## Overview

Store configuration options control various business logic behaviors across the platform. These settings are centralized in:

- **Constants**: `src/ReSys.Core/Common/Constants/StoreConfiguration.cs` - Immutable defaults
- **Options**: `src/ReSys.Infrastructure/Configuration/StoreConfigurationOptions.cs` - Runtime configuration model
- **Configuration**: `appsettings.json` - Environment-specific overrides

## Configuration Options

| Configuration Key | Type | Default | Description |
|-------------------|------|---------|-------------|
| `AllowCheckoutOnGatewayError` | bool | `false` | Continues checkout even if payment gateway error fails |
| `AddressRequiresPhone` | bool | `false` | Requires phone number on address forms |
| `AlternativeShippingPhone` | bool | `false` | Shows alternative phone field for shipping address |
| `AlwaysIncludeConfirmStep` | bool | `false` | Always includes confirmation step in checkout |
| `AutoCapture` | bool | `true` | Automatically captures payments from gateway |
| `AutoCaptureOnDispatch` | bool | `false` | Auto-captures payment when shipment dispatches |
| `Company` | bool | `false` | Shows company field on address forms |
| `CreditToNewAllocation` | bool | `false` | Creates new allocation when store credit added |
| `DisableSkuValidation` | bool | `false` | Disables SKU uniqueness validation |
| `DisableStorePresenceValidation` | bool | `false` | Disables store presence validation |
| `ExpeditedExchanges` | bool | `false` | Enables expedited exchange shipments |
| `ExpeditedExchangesDaysWindow` | int | `14` | Days allowed for return after expedited exchange |
| `RestockInventory` | bool | `true` | Restocks inventory on cancel/return |
| `ReturnEligibilityNumberOfDays` | int | `365` | Days after purchase within which return is eligible |
| `ShowProductsWithoutPrice` | bool | `false` | Shows unpriced products in storefront/API |
| `TrackInventoryLevels` | bool | `true` | Tracks inventory when products purchased |

## Usage

### In appsettings.json

Override any default values by adding to your `appsettings.json`:

```json
{
  "StoreConfiguration": {
    "AddressRequiresPhone": true,
    "Company": true,
    "AutoCapture": false,
    "ReturnEligibilityNumberOfDays": 30
  }
}
```

### In Code - Via Dependency Injection

```csharp
using Microsoft.Extensions.Options;
using ReSys.Infrastructure.Configuration;

public class OrderCheckoutHandler
{
    private readonly IOptions<StoreConfigurationOptions> _config;

    public OrderCheckoutHandler(IOptions<StoreConfigurationOptions> config)
    {
        _config = config;
    }

    public async Task<Result> CheckoutAsync(Order order)
    {
        var allowGatewayError = _config.Value.AllowCheckoutOnGatewayError;
        var requiresPhone = _config.Value.AddressRequiresPhone;
        var autoCapture = _config.Value.AutoCapture;
        
        // Use configuration in business logic
        if (autoCapture)
        {
            await CapturePaymentAsync(order);
        }
    }
}
```

### In Program.cs - Registration

```csharp
using ReSys.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Register store configuration
builder.Services.AddStoreConfiguration(builder.Configuration);

// ... rest of configuration

var app = builder.Build();
```

### Using Constants Directly

For context-free access to defaults, use the constants:

```csharp
using ReSys.Core.Common.Constants;

public class InventoryService
{
    public void ProcessReturn(Order order)
    {
        // Use default configuration
        bool shouldRestock = StoreConfiguration.RestockInventory;
        
        if (shouldRestock)
        {
            RestockFromOrder(order);
        }
    }
}
```

## Environment-Specific Configuration

### Development (appsettings.Development.json)

```json
{
  "StoreConfiguration": {
    "AllowCheckoutOnGatewayError": true,
    "AddressRequiresPhone": false,
    "AutoCapture": false
  }
}
```

### Production (appsettings.Production.json)

```json
{
  "StoreConfiguration": {
    "AllowCheckoutOnGatewayError": false,
    "AddressRequiresPhone": true,
    "AutoCapture": true,
    "DisableSkuValidation": false,
    "RestockInventory": true
  }
}
```

## Implementation Patterns

### Pattern 1: Address Validation

```csharp
// In AddressValidator
public sealed class AddressValidator : AbstractValidator<Address>
{
    private readonly IOptions<StoreConfigurationOptions> _config;

    public AddressValidator(IOptions<StoreConfigurationOptions> config)
    {
        _config = config;

        if (_config.Value.AddressRequiresPhone)
        {
            RuleFor(x => x.Phone)
                .NotEmpty()
                .WithMessage("Phone number is required");
        }

        if (_config.Value.AlternativeShippingPhone)
        {
            RuleFor(x => x.AlternativePhone)
                .NotEmpty()
                .When(x => x.IsShippingAddress)
                .WithMessage("Alternative phone is required for shipping");
        }

        if (_config.Value.Company)
        {
            RuleFor(x => x.Company)
                .NotEmpty()
                .WithMessage("Company is required");
        }
    }
}
```

### Pattern 2: Inventory Management

```csharp
// In OrderReturnHandler
public sealed class ProcessReturnHandler : ICommandHandler<ProcessReturnCommand, Result>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IOptions<StoreConfigurationOptions> _config;

    public async Task<ErrorOr<Result>> Handle(ProcessReturnCommand command, CancellationToken ct)
    {
        var order = await _dbContext.Orders.FindAsync(command.OrderId, cancellationToken: ct);
        
        if (_config.Value.RestockInventory)
        {
            await RestockOrderItems(order, ct);
        }

        // Process return...
        await _dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
```

### Pattern 3: Payment Processing

```csharp
// In PaymentProcessorService
public sealed class PaymentProcessor
{
    private readonly IOptions<StoreConfigurationOptions> _config;
    private readonly IPaymentGateway _gateway;

    public async Task<Result> ProcessPaymentAsync(Order order, CancellationToken ct)
    {
        var payment = order.Payment;

        try
        {
            if (_config.Value.AutoCapture)
            {
                // Perform full purchase
                var result = await _gateway.PurchaseAsync(payment, ct);
                return result;
            }
            else
            {
                // Only authorize
                var result = await _gateway.AuthorizeAsync(payment, ct);
                return result;
            }
        }
        catch (Exception ex)
        {
            if (!_config.Value.AllowCheckoutOnGatewayError)
            {
                throw;
            }
            // Log but allow checkout to continue
            _logger.LogWarning(ex, "Gateway error ignored");
            return Result.Success();
        }
    }
}
```

## Notes

- **Tax Configuration**: Tax-related settings are handled separately in domain-specific tax configuration
- **Multi-Store**: Configuration applies globally; store-specific overrides can be implemented via a separate mechanism
- **Runtime Changes**: Changes to appsettings require application restart; for hot-reload, implement a configuration cache invalidation strategy
- **Validation**: All configuration values are validated against business rules during application startup

## Migration from Spree

The following Spree settings have been intentionally excluded (marked as "tax"):
- `tax_using_ship_address` - Moved to tax-specific configuration context

See related domain documentation:
- Orders: `src/ReSys.Core/Domain/Orders/README.md`
- Inventory: `src/ReSys.Core/Domain/Inventories/README.md`
- Payments: `src/ReSys.Core/Domain/Payments/README.md`
- Shipping: `src/ReSys.Core/Domain/Shipping/README.md`
