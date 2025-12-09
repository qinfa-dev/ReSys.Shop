using FluentAssertions;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Orders.Shipments;

namespace Core.UnitTests.Domain.Orders.Shipments;

public class ShipmentTests
{
    [Fact]
    public void Create_ShouldReturnShipment_WhenValidInputs()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var stockLocationId = Guid.NewGuid();

        // Act
        var result = Shipment.Create(orderId: orderId, stockLocationId: stockLocationId);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.OrderId.Should().Be(expected: orderId);
        result.Value.StockLocationId.Should().Be(expected: stockLocationId);
        result.Value.State.Should().Be(expected: Shipment.ShipmentState.Pending);
        result.Value.Number.Should().StartWith(expected: "S");
        result.Value.CreatedAt.Should().BeCloseTo(nearbyTime: DateTimeOffset.UtcNow, precision: TimeSpan.FromSeconds(seconds: 5));
        result.Value.DomainEvents.OfType<Shipment.Events.Created>().Should().ContainSingle();
    }

    [Fact]
    public void Ready_ShouldTransitionToReady_WhenPending()
    {
        // Arrange
        var shipment = Shipment.Create(orderId: Guid.NewGuid(), stockLocationId: Guid.NewGuid()).Value;

        // Act
        var result = shipment.Ready();

        // Assert
        result.IsError.Should().BeFalse();
        shipment.State.Should().Be(expected: Shipment.ShipmentState.Ready);
        shipment.UpdatedAt.Should().NotBeNull();
        ((Aggregate)shipment).DomainEvents.OfType<Shipment.Events.Ready>().Should().ContainSingle();
    }

    [Fact]
    public void Ship_ShouldTransitionToShipped_WhenReady()
    {
        // Arrange
        var shipment = Shipment.Create(orderId: Guid.NewGuid(), stockLocationId: Guid.NewGuid()).Value;
        shipment.Ready(); // Move to Ready state
        var trackingNumber = "TN12345";

        // Act
        var result = shipment.Ship(trackingNumber: trackingNumber);

        // Assert
        result.IsError.Should().BeFalse();
        shipment.State.Should().Be(expected: Shipment.ShipmentState.Shipped);
        shipment.TrackingNumber.Should().Be(expected: trackingNumber);
        shipment.ShippedAt.Should().NotBeNull();
        shipment.UpdatedAt.Should().NotBeNull();
        ((Aggregate)shipment).DomainEvents.OfType<Shipment.Events.Shipped>().Should().ContainSingle();
    }

    [Fact]
    public void Deliver_ShouldTransitionToDelivered_WhenShipped()
    {
        // Arrange
        var shipment = Shipment.Create(orderId: Guid.NewGuid(), stockLocationId: Guid.NewGuid()).Value;
        shipment.Ready();
        shipment.Ship(trackingNumber: "TN123");

        // Act
        var result = shipment.Deliver();

        // Assert
        result.IsError.Should().BeFalse();
        shipment.State.Should().Be(expected: Shipment.ShipmentState.Delivered);
        shipment.DeliveredAt.Should().NotBeNull();
        shipment.UpdatedAt.Should().NotBeNull();
        ((Aggregate)shipment).DomainEvents.OfType<Shipment.Events.Delivered>().Should().ContainSingle();
    }

    [Fact]
    public void Cancel_ShouldTransitionToCanceled_WhenNotShipped()
    {
        // Arrange
        var shipment = Shipment.Create(orderId: Guid.NewGuid(), stockLocationId: Guid.NewGuid()).Value; // Pending

        // Act
        var result = shipment.Cancel();

        // Assert
        result.IsError.Should().BeFalse();
        shipment.State.Should().Be(expected: Shipment.ShipmentState.Canceled);
        shipment.UpdatedAt.Should().NotBeNull();
        ((Aggregate)shipment).DomainEvents.OfType<Shipment.Events.Canceled>().Should().ContainSingle();
    }
}