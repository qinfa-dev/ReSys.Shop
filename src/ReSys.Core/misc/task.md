# CQRS Implementation Tasks

This file tracks the tasks related to the CQRS implementation that need to be addressed after the domain model and its tests are updated.

- [ ] Find the `FinalizeInventoryHandler` and update it to use the new `InventoryUnit.Create` method. The handler should now create a single `InventoryUnit` with the correct quantity, instead of multiple instances.
- [ ] Update any other command or event handlers that use the old `InventoryUnit.CreateForLineItem` method.
- [ ] Review all CQRS handlers that interact with the `InventoryUnit` to ensure they are correctly handling the new `Quantity` property (e.g., using `Sum(iu => iu.Quantity)` instead of `Count()`).
- [ ] Create an integration test to verify that optimistic concurrency is working correctly. This test should create a scenario where two users try to update the same entity at the same time, and assert that a `DbUpdateConcurrencyException` is thrown.
- [ ] Wrap the `StockTransfer.Transfer` method in a database transaction in the corresponding CQRS handler.
- [ ] Wrap the `ReturnItem.ProcessInventoryUnit` method in a database transaction in the corresponding CQRS handler.
- [ ] Wrap the `Order.Complete` operation in a database transaction in the corresponding CQRS handler.
- [ ] Implement CQRS Read Models: Create `OrderSummaryReadModel` and event handlers to populate it.
