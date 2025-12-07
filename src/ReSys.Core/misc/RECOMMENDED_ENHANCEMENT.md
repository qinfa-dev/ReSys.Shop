# Recommended Enhancement: Reservation-Safe Unstock

## The Issue

The current `Unstock` method in `StockLocation.cs` validates:
- ? Quantity is positive
- ? Stock item exists
- ? Sufficient quantity available (considering backorderability)

But it **doesn't prevent** an edge case:
```
Current State:
  QuantityOnHand: 10
  QuantityReserved: 8
  CountAvailable: 2 (10 - 8)

Unstock request: 3 units ? PASSES (3 > 2 reserved)

After unstock:
  QuantityOnHand: 7
  QuantityReserved: 8 ? INVALID (Reserved > OnHand)
```

This violates the invariant: `QuantityReserved <= QuantityOnHand`

## Solution

Add validation after the availability check:

```csharp
// Location: src\ReSys.Core\Domain\Inventories\Locations\StockLocation.cs
// Method: Unstock (after line 870)

// ...existing availability check...
if (!stockItem.Backorderable && stockItem.CountAvailable < quantity)
{
    return Error.Validation(
        code: "StockLocation.InsufficientStock",
        description: $"Insufficient stock for variant {variant.Id}. " +
                     $"Available: {stockItem.CountAvailable}, Requested: {quantity}");
}

// ADD THIS:
// For non-backorderable items, ensure unstock doesn't invalidate reservations
if (!stockItem.Backorderable)
{
    var newOnHand = stockItem.QuantityOnHand - quantity;
    if (stockItem.QuantityReserved > newOnHand)
    {
        return Error.Validation(
            code: "StockLocation.UnstockWouldViolateReservations",
            description: $"Cannot unstock {quantity} units for variant {variant.Id}. " +
                         $"Would leave {newOnHand} on hand but {stockItem.QuantityReserved} are reserved. " +
                         $"Maximum unstock: {stockItem.QuantityOnHand - stockItem.QuantityReserved} units.");
    }
}

var result = stockItem.Adjust(
    quantity: -quantity,
    originator: originator,
    reason: "Unstock",
    stockTransferId: stockTransferId);
```

## Why This Matters

1. **Data Integrity**: Prevents invalid state
2. **Transfer Safety**: Stock transfers can't accidentally violate invariants
3. **Order Protection**: Reserved stock for orders is protected from transfers
4. **Clear Errors**: Users get specific feedback on what went wrong

## Impact

- **Scope**: Minimal - single method enhancement
- **Risk**: Very low - additional validation only
- **Testing**: Easy to add test cases for this edge case
- **Performance**: Negligible - 3 comparisons added

## Test Case Example

```csharp
[Fact]
public void Unstock_ShouldReturnError_WhenWouldViolateReservations()
{
    // Arrange
    var stockItem = CreateTestStockItem(locationId, variant, 
        quantityOnHand: 10, 
        quantityReserved: 8, 
        backorderable: false);
    
    var location = CreateTestStockLocation(locationId);
    location.StockItems.Add(stockItem);

    // Act
    var result = location.Unstock(
        variant: variant,
        quantity: 5,  // Would leave only 5, but 8 are reserved
        originator: StockMovement.MovementOriginator.Adjustment);

    // Assert
    result.IsError.Should().BeTrue();
    result.FirstError.Code.Should().Be("StockLocation.UnstockWouldViolateReservations");
}
```

## Implementation Recommendation

? **APPLY THIS ENHANCEMENT** - It prevents a real data integrity issue without significant effort.

The enhancement ensures the system maintains the invariant:
```
For non-backorderable items:
  QuantityReserved <= QuantityOnHand (always true)
```

