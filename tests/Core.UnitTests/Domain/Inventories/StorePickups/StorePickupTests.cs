using ReSys.Core.Domain.Inventories.StorePickups;

namespace Core.UnitTests.Domain.Inventories.StorePickups;

/// <summary>
/// Unit tests for the <see cref="StorePickup"/> aggregate.
/// Tests domain model behavior including factory methods, state transitions, and domain events.
/// </summary>
public sealed class StorePickupTests
{
    #region Factory Method Tests

    [Fact]
    public void Create_WithValidParameters_ReturnsSuccessfulPickup()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        // Act
        var result = StorePickup.Create(orderId, locationId);

        // Assert
        Assert.False(result.IsError);
        
        var pickup = result.Value;
        Assert.NotEqual(Guid.Empty, pickup.Id);
        Assert.Equal(orderId, pickup.OrderId);
        Assert.Equal(locationId, pickup.StockLocationId);
        Assert.Equal(StorePickup.PickupState.Pending, pickup.State);
        Assert.NotEmpty(pickup.PickupCode);
        Assert.True(pickup.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Create_WithEmptyOrderId_ReturnsError()
    {
        // Arrange
        var locationId = Guid.NewGuid();

        // Act
        var result = StorePickup.Create(Guid.Empty, locationId);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("StorePickup.InvalidOrder", result.FirstError.Code);
    }

    [Fact]
    public void Create_WithEmptyLocationId_ReturnsError()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        var result = StorePickup.Create(orderId, Guid.Empty);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("StorePickup.InvalidStockLocation", result.FirstError.Code);
    }

    [Fact]
    public void Create_WithFutureScheduledTime_ReturnsSuccessful()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var futureTime = DateTimeOffset.UtcNow.AddDays(1);

        // Act
        var result = StorePickup.Create(orderId, locationId, futureTime);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(futureTime, result.Value.ScheduledPickupTime);
    }

    [Fact]
    public void Create_WithPastScheduledTime_ReturnsError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pastTime = DateTimeOffset.UtcNow.AddDays(-1);

        // Act
        var result = StorePickup.Create(orderId, locationId, pastTime);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("StorePickup.InvalidScheduledTime", result.FirstError.Code);
    }

    [Fact]
    public void Create_GeneratesUniquePickupCodes()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        // Act
        var result1 = StorePickup.Create(orderId, locationId);
        var result2 = StorePickup.Create(orderId, locationId);

        // Assert
        Assert.False(result1.IsError);
        Assert.False(result2.IsError);
        Assert.NotEqual(result1.Value.PickupCode, result2.Value.PickupCode);
    }

    [Fact]
    public void Create_PublishesDomainEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        // Act
        var result = StorePickup.Create(orderId, locationId);

        // Assert
        Assert.False(result.IsError);
        
        var pickup = result.Value;
        Assert.True(pickup.HasUncommittedEvents());
        
        var @event = pickup.DomainEvents.First() as StorePickup.Events.Created;
        Assert.NotNull(@event);
        Assert.Equal(pickup.Id, @event.StorePickupId);
        Assert.Equal(orderId, @event.OrderId);
        Assert.Equal(locationId, @event.StockLocationId);
    }

    #endregion

    #region State Transition Tests - Mark Ready

    [Fact]
    public void MarkReady_FromPendingState_TransitionsToReady()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pickup = StorePickup.Create(orderId, locationId).Value;
        
        // Clear events from creation
        pickup.ClearDomainEvents();

        // Act
        var result = pickup.MarkReady();

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(StorePickup.PickupState.Ready, pickup.State);
        Assert.NotNull(pickup.ReadyAt);
        Assert.True(pickup.ReadyAt.Value <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void MarkReady_FromPendingState_PublishesReadyEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pickup = StorePickup.Create(orderId, locationId).Value;
        pickup.ClearDomainEvents(); // Clear creation events

        // Act
        var result = pickup.MarkReady();

        // Assert
        Assert.False(result.IsError);
        Assert.True(pickup.HasUncommittedEvents());
        
        var @event = pickup.DomainEvents.First() as StorePickup.Events.Ready;
        Assert.NotNull(@event);
        Assert.Equal(pickup.Id, @event.StorePickupId);
    }

    [Fact]
    public void MarkReady_FromReadyState_ReturnsError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pickup = StorePickup.Create(orderId, locationId).Value;
        pickup.MarkReady();

        // Act
        var result = pickup.MarkReady();

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("StorePickup.InvalidStateForReady", result.FirstError.Code);
    }

    [Fact]
    public void MarkReady_FromPickedUpState_ReturnsError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pickup = StorePickup.Create(orderId, locationId).Value;
        pickup.MarkReady();
        pickup.MarkPickedUp();

        // Act
        var result = pickup.MarkReady();

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("StorePickup.InvalidStateForReady", result.FirstError.Code);
    }

    #endregion

    #region State Transition Tests - Mark Picked Up

    [Fact]
    public void MarkPickedUp_FromReadyState_TransitionsToPickedUp()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pickup = StorePickup.Create(orderId, locationId).Value;
        pickup.MarkReady();

        // Act
        var result = pickup.MarkPickedUp();

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(StorePickup.PickupState.PickedUp, pickup.State);
        Assert.NotNull(pickup.PickedUpAt);
    }

    [Fact]
    public void MarkPickedUp_FromReadyState_PublishesPickedUpEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pickup = StorePickup.Create(orderId, locationId).Value;
        pickup.MarkReady();
        pickup.ClearDomainEvents(); // Clear ready event

        // Act
        var result = pickup.MarkPickedUp();

        // Assert
        Assert.False(result.IsError);
        
        var @event = pickup.DomainEvents.First() as StorePickup.Events.PickedUp;
        Assert.NotNull(@event);
    }

    [Fact]
    public void MarkPickedUp_FromPendingState_ReturnsError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pickup = StorePickup.Create(orderId, locationId).Value;

        // Act
        var result = pickup.MarkPickedUp();

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("StorePickup.InvalidStateForPickup", result.FirstError.Code);
    }

    #endregion

    #region State Transition Tests - Cancel

    [Fact]
    public void Cancel_FromPendingState_TransitionsToCancelled()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pickup = StorePickup.Create(orderId, locationId).Value;

        // Act
        var result = pickup.Cancel("Customer requested cancellation");

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(StorePickup.PickupState.Cancelled, pickup.State);
        Assert.NotNull(pickup.CancelledAt);
        Assert.Equal("Customer requested cancellation", pickup.CancellationReason);
    }

    [Fact]
    public void Cancel_FromReadyState_TransitionsToCancelled()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pickup = StorePickup.Create(orderId, locationId).Value;
        pickup.MarkReady();

        // Act
        var result = pickup.Cancel("Out of stock");

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(StorePickup.PickupState.Cancelled, pickup.State);
    }

    [Fact]
    public void Cancel_FromPickedUpState_ReturnsError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pickup = StorePickup.Create(orderId, locationId).Value;
        pickup.MarkReady();
        pickup.MarkPickedUp();

        // Act
        var result = pickup.Cancel();

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("StorePickup.CannotCancelPickedUp", result.FirstError.Code);
    }

    [Fact]
    public void Cancel_FromCancelledState_ReturnsError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pickup = StorePickup.Create(orderId, locationId).Value;
        pickup.Cancel("First cancellation");

        // Act
        var result = pickup.Cancel("Second cancellation");

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("StorePickup.AlreadyCancelled", result.FirstError.Code);
    }

    [Fact]
    public void Cancel_WithTooLongReason_ReturnsError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pickup = StorePickup.Create(orderId, locationId).Value;
        var tooLongReason = new string('x', StorePickup.Constraints.CancellationReasonMaxLength + 1);

        // Act
        var result = pickup.Cancel(tooLongReason);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("StorePickup.CancellationReason.TooLong", result.FirstError.Code);
    }

    [Fact]
    public void Cancel_PublishesCancelledEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pickup = StorePickup.Create(orderId, locationId).Value;
        pickup.ClearDomainEvents(); // Clear creation event

        // Act
        var result = pickup.Cancel("Cancellation reason");

        // Assert
        Assert.False(result.IsError);
        
        var @event = pickup.DomainEvents.First() as StorePickup.Events.Cancelled;
        Assert.NotNull(@event);
        Assert.Equal("Cancellation reason", @event.Reason);
    }

    #endregion

    #region Reschedule Tests

    [Fact]
    public void ReschedulePickup_WithFutureTime_UpdatesScheduledTime()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var originalTime = DateTimeOffset.UtcNow.AddDays(1);
        var pickup = StorePickup.Create(orderId, locationId, originalTime).Value;
        
        var newTime = DateTimeOffset.UtcNow.AddDays(2);

        // Act
        var result = pickup.ReschedulePickup(newTime);

        // Assert
        Assert.False(result.IsError);
        Assert.Equal(newTime, pickup.ScheduledPickupTime);
    }

    [Fact]
    public void ReschedulePickup_WithPastTime_ReturnsError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pickup = StorePickup.Create(orderId, locationId).Value;
        
        var pastTime = DateTimeOffset.UtcNow.AddDays(-1);

        // Act
        var result = pickup.ReschedulePickup(pastTime);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("StorePickup.InvalidScheduledTime", result.FirstError.Code);
    }

    [Fact]
    public void ReschedulePickup_WhenPickedUp_ReturnsError()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pickup = StorePickup.Create(orderId, locationId).Value;
        pickup.MarkReady();
        pickup.MarkPickedUp();
        
        var newTime = DateTimeOffset.UtcNow.AddDays(1);

        // Act
        var result = pickup.ReschedulePickup(newTime);

        // Assert
        Assert.True(result.IsError);
        Assert.Equal("StorePickup.CannotReschedule", result.FirstError.Code);
    }

    #endregion

    #region Query Property Tests

    [Fact]
    public void IsActive_WhenReadyAndNotExpired_ReturnsTrue()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pickup = StorePickup.Create(orderId, locationId).Value;
        pickup.MarkReady();

        // Act
        var isActive = pickup.IsActive;

        // Assert
        Assert.True(isActive);
    }

    [Fact]
    public void IsActive_WhenPending_ReturnsFalse()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pickup = StorePickup.Create(orderId, locationId).Value;

        // Act
        var isActive = pickup.IsActive;

        // Assert
        Assert.False(isActive);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void PickupCode_IsNeverEmpty()
    {
        // Arrange & Act
        var result = StorePickup.Create(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        Assert.False(result.IsError);
        Assert.NotEmpty(result.Value.PickupCode);
        Assert.True(result.Value.PickupCode.Length <= StorePickup.Constraints.PickupCodeMaxLength);
    }

    [Fact]
    public void MultipleStateTransitions_FollowValidPaths()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var pickup = StorePickup.Create(orderId, locationId).Value;

        // Act & Assert - Valid path: Pending -> Ready -> PickedUp
        Assert.False(pickup.MarkReady().IsError);
        Assert.False(pickup.MarkPickedUp().IsError);
        Assert.Equal(StorePickup.PickupState.PickedUp, pickup.State);
    }

    #endregion
}
