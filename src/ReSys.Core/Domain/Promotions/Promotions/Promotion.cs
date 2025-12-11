using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Orders.Adjustments;
using ReSys.Core.Domain.Promotions.Actions;
using ReSys.Core.Domain.Promotions.Audits;
using ReSys.Core.Domain.Promotions.Rules;

namespace ReSys.Core.Domain.Promotions.Promotions;

/// <summary>
/// Represents a promotional offer that can be applied to orders based on eligibility rules.
/// Supports multiple discount types, usage limits, and flexible scheduling.
/// </summary>
/// <remarks>
/// <para>
/// <b>Responsibility:</b>
/// Aggregate root for promotion-related business logic including creation, configuration,
/// rule management, activation/deactivation, and usage tracking.
/// </para>
/// 
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
/// <item><b>Promotion Types:</b> Order discounts, item discounts, free shipping, buy-x-get-y offers</item>
/// <item><b>Discount Types:</b> Percentage-based or fixed amount discounts</item>
/// <item><b>Eligibility Rules:</b> Multiple configurable rules (FirstOrder, ProductInclude/Exclude, MinimumQuantity, UserRole, etc.)</item>
/// <item><b>Time-Based:</b> Optional start and expiration dates</item>
/// <item><b>Usage Tracking:</b> Optional usage limit with counter</item>
/// <item><b>Coupon Codes:</b> Optional requirement for coupon code entry</item>
/// <item><b>Discount Caps:</b> Optional maximum discount amount per order</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>State Machine:</b>
/// <list type="bullet">
/// <item><b>Active:</b> Promotion flag can be toggled independently</item>
/// <item><b>Automatic Status:</b> Computed via IsActive property based on timing and usage limits</item>
/// <item><b>Lifecycle Events:</b> Created ? Updated (0..n times) ? Activated/Deactivated (0..n times) ? Used (0..n times)</item>
/// </list>
/// </para>
/// </remarks>
public sealed class Promotion : Aggregate, IHasUniqueName
{
    #region Constraints
    public static class Constraints
    {
        public const int MinNameLength = CommonInput.Constraints.NamesAndUsernames.NameMinLength;
        public const int NameMaxLength = 100;
        public const int MinCodeLength = 3;
        public const int CodeMaxLength = 50;
        public const int MinUsageLimit = 1;
        public const decimal MinOrderAmount = 0m;
        public const decimal MinDiscountValue = 0m;
        public const decimal MaxDiscountValue = 1000000m;
    }

    public enum PromotionType { None, OrderDiscount, ItemDiscount, FreeShipping, BuyXGetY }
    public enum DiscountType { Percentage, FixedAmount }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error Required => Error.Validation(
            code: "Promotion.Required",
            description: "Promotion is required.");

        public static Error NotFound(Guid id) => Error.NotFound(
            code: "Promotion.NotFound",
            description: $"Promotion with ID '{id}' was not found.");

        public static Error NoStarted => Error.Validation(
            code: "Promotion.NotStarted",
            description: "Promotion has not started yet.");

        public static Error NotActive => Error.Validation(
            code: "Promotion.NotActive",
            description: "Promotion is not currently active.");

        public static Error UsageLimitReached => Error.Validation(
            code: "Promotion.UsageLimitReached",
            description: "Promotion usage limit has been reached.");

        public static Error MinimumOrderNotMet(decimal minimum) => Error.Validation(
            code: "Promotion.MinimumOrderNotMet",
            description: $"Order total must be at least {minimum:C}.");

        public static Error InvalidCode => Error.Validation(
            code: "Promotion.InvalidCode",
            description: "Invalid promotion code.");

        public static Error Expired => Error.Validation(
            code: "Promotion.Expired",
            description: "Promotion has expired.");

        public static Error InvalidMinimumOrderAmount => Error.Validation(
            code: "Promotion.InvalidMinimumOrderAmount",
            description: "Minimum order amount must be non-negative.");

        public static Error InvalidMaximumDiscountAmount => Error.Validation(
            code: "Promotion.InvalidMaximumDiscountAmount",
            description: "Maximum discount amount must be non-negative.");

        public static Error InvalidUsageLimit => Error.Validation(
            code: "Promotion.InvalidUsageLimit",
            description: $"Usage limit must be at least {Constraints.MinUsageLimit}.");

        public static Error NameRequired => CommonInput.Errors.Required(prefix: nameof(Promotion), field: nameof(Name));

        public static Error NameTooLong => CommonInput.Errors.TooLong(prefix: nameof(Promotion), field: nameof(Name), maxLength: Constraints.NameMaxLength);
    }
    #endregion

    #region Properties
    /// <summary>Gets or sets the promotion name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional coupon code (auto-uppercased). Required if RequiresCouponCode is true.</summary>
    public string? PromotionCode { get; set; }

    /// <summary>Gets or sets the optional description of the promotion.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the optional minimum order amount required for the promotion to apply.</summary>
    public decimal? MinimumOrderAmount { get; set; }

    /// <summary>Gets or sets the optional maximum discount amount that can be applied by this promotion.</summary>
    public decimal? MaximumDiscountAmount { get; set; }

    /// <summary>Gets or sets the optional start date. Null means immediately available.</summary>
    public DateTimeOffset? StartsAt { get; set; }

    /// <summary>Gets or sets the optional expiration date. Null means no expiration.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Gets or sets the optional usage limit. Null means unlimited uses.</summary>
    public int? UsageLimit { get; set; }

    /// <summary>Gets or sets the current usage count.</summary>
    public int UsageCount { get; set; }

    /// <summary>Gets or sets whether the promotion is manually active (independent of dates and usage).</summary>
    public bool Active { get; set; } = true;

    /// <summary>Gets or sets whether a coupon code is required to apply this promotion.</summary>
    public bool RequiresCouponCode { get; set; }

    /// <summary>Gets or sets the promotion action that defines the discount/reward calculation.</summary>
    public PromotionAction? Action { get; set; }
    #endregion

    #region Relationships
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<PromotionRule> PromotionRules { get; set; } = new List<PromotionRule>();
    public ICollection<OrderAdjustment> OrderAdjustments { get; set; } = new List<OrderAdjustment>();
    public ICollection<PromotionUsage> PromotionUsages { get; set; } = new List<PromotionUsage>();
    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the promotion type based on the configured action.
    /// Returns PromotionType.None if no action is configured.
    /// </summary>
    public PromotionType Type => Action?.Type ?? PromotionType.None;

    /// <summary>
    /// Gets a value indicating whether the promotion is currently active and eligible to be applied.
    /// Considers manual active flag, start/expiration dates, and usage limits.
    /// </summary>
    public bool IsActive => Active &&
        (!StartsAt.HasValue || StartsAt <= DateTimeOffset.UtcNow) &&
        (!ExpiresAt.HasValue || ExpiresAt >= DateTimeOffset.UtcNow) &&
        (!UsageLimit.HasValue || UsageCount < UsageLimit);

    /// <summary>
    /// Gets a value indicating whether the promotion has expired (ExpiresAt is in the past).
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt < DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets a value indicating whether this promotion has a usage limit configured.
    /// </summary>
    public bool HasUsageLimit => UsageLimit.HasValue;

    /// <summary>
    /// Gets the number of remaining uses before reaching the usage limit.
    /// Returns int.MaxValue if no limit is configured.
    /// </summary>
    public int RemainingUsage => UsageLimit.HasValue 
        ? Math.Max(val1: 0, val2: UsageLimit.Value - UsageCount) 
        : int.MaxValue;

    #endregion

    #region Constructors
    private Promotion() { }
    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new promotion with the specified configuration.
    /// </summary>
    /// <param name="name">The promotion name (required, trimmed). Must be between MinNameLength and NameMaxLength characters.</param>
    /// <param name="action">The promotion action defining the discount/reward logic (required).</param>
    /// <param name="code">Optional coupon code (auto-trimmed and uppercased). Required if requiresCouponCode is true.</param>
    /// <param name="description">Optional description of the promotion (trimmed).</param>
    /// <param name="minimumOrderAmount">Optional minimum order amount for promotion eligibility.</param>
    /// <param name="maximumDiscountAmount">Optional cap on the maximum discount this promotion can apply.</param>
    /// <param name="startsAt">Optional start date. Null means immediately available.</param>
    /// <param name="expiresAt">Optional expiration date. Null means no expiration.</param>
    /// <param name="usageLimit">Optional maximum number of times this promotion can be used. Null means unlimited.</param>
    /// <param name="requiresCouponCode">Whether a coupon code is required to apply this promotion.</param>
    /// <returns>
    /// On success: A new Promotion instance.
    /// On failure: Validation errors for invalid input (negative amounts, invalid limits, etc.).
    /// </returns>
    public static ErrorOr<Promotion> Create(
        string name,
        PromotionAction action,
        string? code = null,
        string? description = null,
        decimal? minimumOrderAmount = null,
        decimal? maximumDiscountAmount = null,
        DateTimeOffset? startsAt = null,
        DateTimeOffset? expiresAt = null,
        int? usageLimit = null,
        bool requiresCouponCode = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Errors.NameRequired;
        }

        if (name.Length > Constraints.NameMaxLength)
        {
            return Errors.NameTooLong;
        }
        
        // Validate optional numeric parameters
        if (minimumOrderAmount.HasValue && minimumOrderAmount < Constraints.MinOrderAmount)
            return Errors.InvalidMinimumOrderAmount;

        if (maximumDiscountAmount.HasValue && maximumDiscountAmount < Constraints.MinDiscountValue)
            return Errors.InvalidMaximumDiscountAmount;

        if (usageLimit.HasValue && usageLimit < Constraints.MinUsageLimit)
            return Errors.InvalidUsageLimit;

        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Action = action,
            PromotionCode = code?.Trim().ToUpperInvariant(),
            Description = description?.Trim(),
            MinimumOrderAmount = minimumOrderAmount,
            MaximumDiscountAmount = maximumDiscountAmount,
            StartsAt = startsAt,
            ExpiresAt = expiresAt,
            UsageLimit = usageLimit,
            RequiresCouponCode = requiresCouponCode,
            Active = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        promotion.AddDomainEvent(
            domainEvent: new Events.Created(
                PromotionId: promotion.Id,
                Name: promotion.Name));

        return promotion;
    }

    #endregion

    #region Business Logic: Updates

    /// <summary>
    /// Updates the promotion with new values. Only non-null parameters are updated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Update Behavior:</b>
    /// <list type="bullet">
    /// <item>Only properties with non-null values are updated</item>
    /// <item>String values are trimmed; codes are uppercased</item>
    /// <item>Numeric values are validated before applying</item>
    /// <item>UpdatedAt timestamp is set only if changes are made</item>
    /// <item>Updated event is published only if changes are made</item>
    /// </list>
    /// </para>
    /// </remarks>
    public ErrorOr<Updated> Update(
        string? name = null,
        string? code = null,
        string? description = null,
        PromotionAction? action = null,
        decimal? minimumOrderAmount = null,
        decimal? maximumDiscountAmount = null,
        DateTimeOffset? startsAt = null,
        DateTimeOffset? expiresAt = null,
        int? usageLimit = null,
        bool? active = null,
        bool? requiresCouponCode = null)
    {
        bool changed = false;

        if (name != null && name != Name)
        {
            Name = name.Trim();
            changed = true;
        }

        if (code != null && code != PromotionCode)
        {
            PromotionCode = code.Trim().ToUpperInvariant();
            changed = true;
        }

        if (description != null && description != Description)
        {
            Description = description.Trim();
            changed = true;
        }

        if (action != null && action != Action)
        {
            Action = action;
            changed = true;
        }

        if (minimumOrderAmount.HasValue && minimumOrderAmount != MinimumOrderAmount)
        {
            if (minimumOrderAmount < Constraints.MinOrderAmount)
                return Errors.InvalidMinimumOrderAmount;
            MinimumOrderAmount = minimumOrderAmount;
            changed = true;
        }

        if (maximumDiscountAmount.HasValue && maximumDiscountAmount != MaximumDiscountAmount)
        {
            if (maximumDiscountAmount < Constraints.MinDiscountValue)
                return Errors.InvalidMaximumDiscountAmount;
            MaximumDiscountAmount = maximumDiscountAmount;
            changed = true;
        }

        if (startsAt.HasValue && startsAt != StartsAt)
        {
            StartsAt = startsAt;
            changed = true;
        }

        if (expiresAt.HasValue && expiresAt != ExpiresAt)
        {
            ExpiresAt = expiresAt;
            changed = true;
        }

        if (usageLimit.HasValue && usageLimit != UsageLimit)
        {
            if (usageLimit < Constraints.MinUsageLimit)
                return Errors.InvalidUsageLimit;
            UsageLimit = usageLimit;
            changed = true;
        }

        if (active.HasValue && active != Active)
        {
            Active = active.Value;
            changed = true;
        }

        if (requiresCouponCode.HasValue && requiresCouponCode != RequiresCouponCode)
        {
            RequiresCouponCode = requiresCouponCode.Value;
            changed = true;
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(domainEvent: new Events.Updated(PromotionId: Id));
        }

        return Result.Updated;
    }

    #endregion

    #region Business Logic: Usage Tracking

    /// <summary>
    /// Increments the promotion usage counter when the promotion is applied to an order.
    /// Should be called after promotion is successfully applied.
    /// </summary>
    /// <remarks>
    /// This method does not check if the usage limit has been reached.
    /// Validation of eligibility should be done before calling this method.
    /// </remarks>
    /// <returns>Success if usage count was incremented.</returns>
    public ErrorOr<Success> IncrementUsage()
    {
        UsageCount++;
        AddDomainEvent(
            domainEvent: new Events.UsageIncreased(
                PromotionId: Id,
                NewUsageCount: UsageCount));
        return Result.Success;
    }

    #endregion

    #region Business Logic: Validation

    /// <summary>
    /// Validates the promotion configuration for logical inconsistencies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Validation Rules:</b>
    /// <list type="bullet">
    /// <item>Start date must be before expiration date (if both are set)</item>
    /// <item>Coupon code must be provided if RequiresCouponCode is true</item>
    /// <item>Maximum discount cannot exceed minimum order amount (if both are set)</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <returns>Success if all validations pass, or list of validation errors.</returns>
    public ErrorOr<Success> Validate()
    {
        var errors = new List<Error>();

        // Validate date range
        if (StartsAt.HasValue && ExpiresAt.HasValue && StartsAt >= ExpiresAt)
        {
            errors.Add(item: Error.Validation(
                code: "Promotion.InvalidDateRange",
                description: "Start date must be before expiry date."));
        }

        // Validate coupon code requirement
        if (RequiresCouponCode && string.IsNullOrWhiteSpace(value: PromotionCode))
        {
            errors.Add(item: Error.Validation(
                code: "Promotion.CodeRequired",
                description: "Coupon code is required when RequiresCouponCode is true."));
        }

        // Validate discount cap vs minimum order
        if (MinimumOrderAmount.HasValue && MaximumDiscountAmount.HasValue &&
            MaximumDiscountAmount > MinimumOrderAmount)
        {
            errors.Add(item: Error.Validation(
                code: "Promotion.InvalidDiscountCap",
                description: "Maximum discount cannot exceed minimum order amount."));
        }

        return errors.Any() ? errors : Result.Success;
    }

    #endregion

    #region Business Logic: Rule Management

    /// <summary>
    /// Adds an eligibility rule to the promotion.
    /// </summary>
    /// <param name="rule">The rule to add (must not be null).</param>
    /// <returns>
    /// On success: Returns this promotion for method chaining.
    /// On failure: Validation error if rule is null or duplicate rule exists.
    /// </returns>
    /// <remarks>
    /// Prevents duplicate rules with the same type and value combination.
    /// </remarks>
    public ErrorOr<Promotion> AddRule(PromotionRule? rule)
    {
        if (rule == null)
        {
            return Error.Validation(
                code: "Promotion.RuleRequired",
                description: "Promotion rule cannot be null.");
        }

        if (PromotionRules.Any(
            predicate: r => r.Type == rule.Type && r.Value == rule.Value))
        {
            return Error.Conflict(
                code: "Promotion.DuplicateRule",
                description: "This rule already exists for this promotion.");
        }

        PromotionRules.Add(item: rule);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(
            domainEvent: new Events.RuleAdded(
                PromotionId: Id,
                RuleId: rule.Id));

        return this;
    }

    /// <summary>
    /// Removes an eligibility rule from the promotion.
    /// </summary>
    /// <param name="ruleId">The ID of the rule to remove.</param>
    /// <returns>
    /// On success: Returns this promotion for method chaining.
    /// On failure: NotFound error if rule with specified ID doesn't exist.
    /// </returns>
    public ErrorOr<Promotion> RemoveRule(Guid ruleId)
    {
        var rule = PromotionRules.FirstOrDefault(predicate: r => r.Id == ruleId);
        if (rule == null)
        {
            return PromotionRule.Errors.NotFound(id: ruleId);
        }

        PromotionRules.Remove(item: rule);
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(
            domainEvent: new Events.RuleRemoved(
                PromotionId: Id,
                RuleId: ruleId));

        return this;
    }

    #endregion

    #region Business Logic: Activation

    /// <summary>
    /// Activates the promotion (sets Active = true).
    /// Cannot activate an expired promotion.
    /// </summary>
    /// <returns>
    /// On success: Returns this promotion for method chaining.
    /// On failure: Expired error if promotion has already expired.
    /// </returns>
    public ErrorOr<Promotion> Activate()
    {
        if (Active)
            return this;

        if (IsExpired)
            return Errors.Expired;

        Active = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(
            domainEvent: new Events.Activated(
                PromotionId: Id));

        return this;
    }

    /// <summary>
    /// Deactivates the promotion (sets Active = false).
    /// </summary>
    /// <returns>Returns this promotion for method chaining.</returns>
    public ErrorOr<Promotion> Deactivate()
    {
        if (!Active)
            return this;

        Active = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(
            domainEvent: new Events.Deactivated(
                PromotionId: Id));

        return this;
    }

    #endregion

    #region Domain Events

    public static class Events
    {
        /// <summary>
        /// Raised when a new promotion is created.
        /// Used for audit logging, notification, or downstream processing.
        /// </summary>
        public sealed record Created(Guid PromotionId, string Name) : DomainEvent;

        /// <summary>
        /// Raised when a promotion's configuration is updated.
        /// Used for audit logging and cache invalidation.
        /// </summary>
        public sealed record Updated(Guid PromotionId) : DomainEvent;

        /// <summary>
        /// Raised when a promotion's usage count increases.
        /// Used for tracking usage metrics and enforcing limits.
        /// </summary>
        public sealed record UsageIncreased(Guid PromotionId, int NewUsageCount) : DomainEvent;

        /// <summary>
        /// Raised when a promotion is deleted.
        /// Used for cleanup and audit logging.
        /// </summary>
        public sealed record Deleted(Guid PromotionId) : DomainEvent;

        /// <summary>
        /// Raised when a promotion is successfully applied to an order.
        /// Used for analytics and reporting.
        /// </summary>
        public sealed record Used(Guid PromotionId, Guid OrderId) : DomainEvent;

        /// <summary>
        /// Raised when a promotion is activated.
        /// Used for notifications and status tracking.
        /// </summary>
        public sealed record Activated(Guid PromotionId) : DomainEvent;

        /// <summary>
        /// Raised when a promotion is deactivated.
        /// Used for notifications and status tracking.
        /// </summary>
        public sealed record Deactivated(Guid PromotionId) : DomainEvent;

        /// <summary>
        /// Raised when an eligibility rule is added to a promotion.
        /// Used for rule change tracking and validation.
        /// </summary>
        public sealed record RuleAdded(Guid PromotionId, Guid RuleId) : DomainEvent;

        /// <summary>
        /// Raised when an eligibility rule is removed from a promotion.
        /// Used for rule change tracking and validation.
        /// </summary>
        public sealed record RuleRemoved(Guid PromotionId, Guid RuleId) : DomainEvent;
    }

    #endregion
}