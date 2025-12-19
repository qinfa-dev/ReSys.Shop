using ReSys.Core.Common.Domain.Entities;

namespace ReSys.Core.Domain.Orders.History;

/// <summary>
/// Represents a single historical event or state transition in an order's lifecycle.
/// This entity provides a detailed, immutable audit trail for an order.
/// </summary>
public sealed class OrderHistory : AuditableEntity<Guid>
{
    #region Constraints
    public static class Constraints
    {
        public const int DescriptionMaxLength = 500;
        public const int TriggeredByMaxLength = 100;
    }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error OrderIdRequired => Error.Validation("OrderHistory.OrderIdRequired", "OrderId cannot be empty.");
        public static Error DescriptionRequired => Error.Validation("OrderHistory.DescriptionRequired", "Description cannot be empty.");
    }
    #endregion

    #region Properties
    /// <summary>
    /// Foreign key to the parent Order.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// A human-readable description of the event.
    /// e.g., "Order state changed from Cart to Address." or "Payment failed: Insufficient funds."
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// The state the order transitioned from. Null for the initial creation event.
    /// </summary>
    public Order.OrderState? FromState { get; private set; }

    /// <summary>
    /// The state the order transitioned to.
    /// </summary>
    public Order.OrderState ToState { get; private set; }
    
    /// <summary>
    /// Identifier for what triggered the event (e.g., "Customer", "System", "Admin:johndoe").
    /// </summary>
    public string? TriggeredBy { get; private set; }

    /// <summary>
    /// A flexible dictionary to store additional context about the event, stored as JSONB.
    /// e.g., { "paymentId": "guid", "errorCode": "5001" }
    /// </summary>
    public IDictionary<string, object?>? Context { get; private set; }
    #endregion

    #region Relationships
    /// <summary>
    /// Navigation property to the parent Order.
    /// </summary>
    public Order Order { get; private set; } = null!;
    #endregion

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private OrderHistory() { }

    /// <summary>
    /// Factory method to create a new OrderHistory entry.
    /// </summary>
    public static ErrorOr<OrderHistory> Create(
        Guid orderId,
        string description,
        Order.OrderState toState,
        Order.OrderState? fromState = null,
        string? triggeredBy = "System",
        IDictionary<string, object?>? context = null)
    {
        if (orderId == Guid.Empty)
            return Errors.OrderIdRequired;

        if (string.IsNullOrWhiteSpace(description))
            return Errors.DescriptionRequired;
        
        var history = new OrderHistory
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Description = description,
            FromState = fromState,
            ToState = toState,
            TriggeredBy = triggeredBy,
            Context = context,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return history;
    }
}
