using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Promotions.Promotions;

namespace ReSys.Core.Domain.Promotions.Rules;

/// <summary>
/// Represents a rule that defines eligibility criteria for a promotion.
/// </summary>
/// <remarks>
/// <para>
/// <b>Responsibility:</b>
/// This aggregate defines a specific condition that must be met for a <see cref="Promotion"/> to be applicable to an <see cref="Order"/>.
/// It can be associated with <see cref="PromotionRuleTaxon"/>s (for category-based rules) and <see cref="PromotionRuleUser"/>s (for user-based rules)
/// to define more complex eligibility criteria.
/// </para>
///
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
/// <item><b>Rule Types:</b> Supports various types like FirstOrder, ProductInclude/Exclude, CategoryInclude/Exclude, MinimumQuantity, UserRole.</item>
/// <item><b>Evaluation:</b> Provides a method to evaluate if an <see cref="Order"/> satisfies the rule.</item>
/// <item><b>Associated Entities:</b> Can link to specific <see cref="Taxa"/> and <see cref="Users"/> for granular control.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class PromotionRule : Aggregate<Guid>
{
    #region Constraints
    /// <summary>
    /// Defines constraints for the <see cref="PromotionRule"/> entity.
    /// </summary>
    public static class Constraints
    {
        /// <summary>
        /// Maximum length for the rule's value.
        /// </summary>
        public const int ValueMaxLength =CommonInput.Constraints.Text.LongTextMaxLength;
    }

    /// <summary>
    /// Defines the types of conditions a promotion rule can evaluate.
    /// </summary>
    public enum RuleType { FirstOrder, ProductInclude, ProductExclude, CategoryInclude, CategoryExclude, MinimumQuantity, UserRole }
    #endregion

    #region Errors
    /// <summary>
    /// Defines domain error scenarios specific to <see cref="PromotionRule"/> operations.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error indicating that a requested promotion rule could not be found.
        /// </summary>
        /// <param name="id">The ID of the promotion rule that was not found.</param>
        public static Error NotFound(Guid id) => Error.NotFound(code: "PromotionRule.NotFound", description: $"Promotion rule with ID '{id}' was not found.");
        /// <summary>
        /// Error indicating that the rule's value is missing or empty.
        /// </summary>
        public static Error ValueRequired => CommonInput.Errors.Required(prefix: nameof(PromotionRule), field: nameof(Value));
        /// <summary>
        /// Error indicating that the rule's value exceeds the maximum allowed length.
        /// </summary>
        public static Error ValueTooLong => CommonInput.Errors.TooLong(prefix: nameof(PromotionRule), field: nameof(Value), maxLength: Constraints.ValueMaxLength);
        /// <summary>
        /// Error indicating an invalid rule type was provided.
        /// </summary>
        public static Error InvalidRuleType => CommonInput.Errors.InvalidValue(prefix: nameof(PromotionRule), field: nameof(Type));
    }
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the foreign key to the associated <see cref="Promotion"/>.
    /// </summary>
    public Guid PromotionId { get; set; }
    /// <summary>
    /// Gets or sets the type of the promotion rule (e.g., FirstOrder, ProductInclude).
    /// </summary>
    public RuleType Type { get; set; }
    /// <summary>
    /// Gets or sets the value associated with the rule (e.g., a product ID, a minimum quantity).
    /// </summary>
    public string Value { get; set; } = string.Empty;
    #endregion

    #region Relationships
    /// <summary>
    /// Gets or sets the <see cref="Promotion"/> to which this rule belongs.
    /// </summary>
    public Promotion Promotion { get; set; } = null!;
    /// <summary>
    /// Gets or sets the collection of <see cref="PromotionRuleTaxon"/>s associated with this rule.
    /// </summary>
    public ICollection<PromotionRuleTaxon> PromotionRuleTaxons { get; set; } = new List<PromotionRuleTaxon>();
    /// <summary>
    /// Gets or sets the collection of <see cref="PromotionRuleUser"/>s associated with this rule.
    /// </summary>
    public ICollection<PromotionRuleUser> PromotionRuleUsers { get; set; } = new List<PromotionRuleUser>();
    #endregion

    #region Constructors
    /// <summary>
    /// Private constructor for ORM (Entity Framework Core) materialization.
    /// </summary>
    private PromotionRule() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Creates a new <see cref="PromotionRule"/> instance.
    /// </summary>
    /// <param name="promotionId">The ID of the <see cref="Promotion"/> this rule belongs to.</param>
    /// <param name="type">The type of the rule.</param>
    /// <param name="value">The value associated with the rule.</param>
    /// <returns>
    /// An <see cref="ErrorOr{PromotionRule}"/> result.
    /// Returns the newly created <see cref="PromotionRule"/> instance on success.
    /// Returns validation errors if the type or value is invalid.
    /// </returns>
    public static ErrorOr<PromotionRule> Create(Guid promotionId, RuleType type, string value)
    {
        if (!Enum.IsDefined(typeof(RuleType), type))
        {
            return Errors.InvalidRuleType;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return Errors.ValueRequired;
        }

        if (value.Length > Constraints.ValueMaxLength)
        {
            return Errors.ValueTooLong;
        }

        var promotionRule = new PromotionRule
        {
            Id = Guid.NewGuid(),
            PromotionId = promotionId,
            Type = type,
            Value = value.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        promotionRule.AddDomainEvent(domainEvent: new Events.PromotionRuleCreated(Id: promotionRule.Id, PromotionId: promotionRule.PromotionId, Type: promotionRule.Type, Value: promotionRule.Value));

        return promotionRule;
    }
    #endregion

    #region Business Logic
    /// <summary>
    /// Updates the value of the promotion rule.
    /// </summary>
    /// <param name="value">The new value for the rule.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Updated}"/> result.
    /// Returns <see cref="Result.Updated"/> on successful update.
    /// </returns>
    public ErrorOr<Updated> Update(string? value = null)
    {
        bool changed = false;
        if (value != null && value != Value)
        {
            Value = value.Trim(); changed = true;
        }
        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(domainEvent: new Events.PromotionRuleUpdated(Id: Id, Value: Value));
        }
        return Result.Updated;
    }

    /// <summary>
    /// Evaluates whether the given <paramref name="order"/> satisfies this promotion rule.
    /// </summary>
    /// <param name="order">The order to evaluate against the rule.</param>
    /// <returns><see langword="true"/> if the order satisfies the rule; otherwise, <see langword="false"/>.</returns>
    public bool Evaluate(Order order)
    {
        return Type switch
        {
            RuleType.FirstOrder => order.User != null &&
                      !string.IsNullOrEmpty(value: order.UserId) &&
                      order.User.Orders.Count(predicate: o => o.IsComplete && o.Id != order.Id) == 0,

            RuleType.ProductInclude => order.LineItems.Any(predicate: li =>
                Guid.TryParse(input: Value, result: out var pid) && li.Variant.ProductId == pid),

            RuleType.ProductExclude => !order.LineItems.Any(predicate: li =>
                Guid.TryParse(input: Value, result: out var pid) && li.Variant.ProductId == pid),

            RuleType.CategoryInclude => PromotionRuleTaxons.Any(predicate: prt =>
                order.LineItems.Any(predicate: li => li.Variant.Product.Taxons.Any(predicate: t => t.Id == prt.TaxonId))),

            RuleType.CategoryExclude => !PromotionRuleTaxons.Any(predicate: prt =>
                order.LineItems.Any(predicate: li => li.Variant.Product.Taxons.Any(predicate: t => t.Id == prt.TaxonId))),

            RuleType.MinimumQuantity => int.TryParse(s: Value, result: out var minQuantity) && order.LineItems.Sum(selector: li => li.Quantity) >= minQuantity,

            RuleType.UserRole => order.User != null &&
                      !string.IsNullOrEmpty(value: order.UserId) &&
                      PromotionRuleUsers.Any(predicate: pru => pru.UserId == order.UserId),

            _ => false
        };
    }

    /// <summary>
    /// Deletes the promotion rule.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Returns <see cref="Result.Deleted"/> on successful deletion.
    /// </returns>
    public ErrorOr<Deleted>
    Delete()
    {
        AddDomainEvent(domainEvent: new Events.PromotionRuleDeleted(Id: Id));
        return Result.Deleted;
    }

    /// <summary>
    /// Adds a <see cref="Taxon"/> to this promotion rule.
    /// </summary>
    /// <param name="taxonId">The ID of the <see cref="Taxon"/> to add.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Success}"/> result.
    /// Returns <see cref="Result.Success"/> on successful addition.
    /// Returns <see cref="Error.Conflict"/> if the taxon is already added.
    /// </returns>
    public ErrorOr<Success> AddTaxon(Guid taxonId)
    {
        if (PromotionRuleTaxons.Any(predicate: prt => prt.TaxonId == taxonId))
        {
            return Error.Conflict(code: "PromotionRule.TaxonAlreadyAdded", description: $"Taxon with ID '{taxonId}' is already added to this promotion rule.");
        }

        var promotionRuleTaxonResult = PromotionRuleTaxon.Create(promotionRuleId: Id, taxonId: taxonId);

        if (promotionRuleTaxonResult.IsError) return promotionRuleTaxonResult.FirstError;



        PromotionRuleTaxons.Add(item: promotionRuleTaxonResult.Value);

        AddDomainEvent(domainEvent: new Events.TaxonAddedToRule(RuleId: Id, TaxonId: taxonId));

        return Result.Success;

    }

    /// <summary>
    /// Removes a <see cref="Taxon"/> from this promotion rule.
    /// </summary>
    /// <param name="taxonId">The ID of the <see cref="Taxon"/> to remove.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Success}"/> result.
    /// Returns <see cref="Result.Success"/> on successful removal.
    /// Returns <see cref="PromotionRuleTaxon.Errors.NotFound"/> if the taxon is not associated with this rule.
    /// </returns>
    public ErrorOr<Success> RemoveTaxon(Guid taxonId)
    {

        var promotionRuleTaxon = PromotionRuleTaxons.FirstOrDefault(predicate: prt => prt.TaxonId == taxonId);

        if (promotionRuleTaxon == null)
        {
            return PromotionRuleTaxon.Errors.NotFound(id: taxonId);
        }

        PromotionRuleTaxons.Remove(item: promotionRuleTaxon);
        AddDomainEvent(domainEvent: new Events.TaxonRemovedFromRule(RuleId: Id, TaxonId: taxonId));

        return Result.Success;

    }


    /// <summary>
    /// Adds a <see cref="User"/> to this promotion rule.
    /// </summary>
    /// <param name="userId">The ID of the user to add.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Success}"/> result.
    /// Returns <see cref="Result.Success"/> on successful addition.
    /// Returns <see cref="Error.Conflict"/> if the user is already added.
    /// </returns>
    public ErrorOr<Success> AddUser(string userId)

    {

        if (PromotionRuleUsers.Any(predicate: pru => pru.UserId == userId))

        {

            return Error.Conflict(code: "PromotionRule.UserAlreadyAdded", description: $"User with ID '{userId}' is already added to this promotion rule.");

        }



        var promotionRuleUserResult = PromotionRuleUser.Create(promotionRuleId: Id, userId: userId);

        if (promotionRuleUserResult.IsError) return promotionRuleUserResult.FirstError;



        PromotionRuleUsers.Add(item: promotionRuleUserResult.Value);

        AddDomainEvent(domainEvent: new Events.UserAddedToRule(RuleId: Id, UserId: userId));

        return Result.Success;

    }


    /// <summary>
    /// Removes a <see cref="User"/> from this promotion rule.
    /// </summary>
    /// <param name="userId">The ID of the user to remove.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Success}"/> result.
    /// Returns <see cref="Result.Success"/> on successful removal.
    /// Returns <see cref="PromotionRuleUser.Errors.NotFound"/> if the user is not associated with this rule.
    /// </returns>
    public ErrorOr<Success> RemoveUser(string userId)

    {

        var promotionRuleUser = PromotionRuleUsers.FirstOrDefault(predicate: pru => pru.UserId == userId);

        if (promotionRuleUser == null)

        {

            return PromotionRuleUser.Errors.NotFound(userId: userId);

        }

        PromotionRuleUsers.Remove(item: promotionRuleUser);

        AddDomainEvent(domainEvent: new Events.UserRemovedFromRule(RuleId: Id, UserId: userId));

        return Result.Success;

    }

    #endregion

    #region Events

    /// <summary>
    /// Defines domain events related to <see cref="PromotionRule"/> changes.
    /// </summary>
    public static class Events

    {

        /// <summary>
        /// Raised when a new promotion rule has been created.
        /// </summary>
        /// <param name="Id">The unique identifier of the new rule.</param>
        /// <param name="PromotionId">The ID of the promotion to which the rule belongs.</param>
        /// <param name="Type">The type of the created rule.</param>
        /// <param name="Value">The value of the created rule.</param>
        /// <remarks>
        /// Purpose: Notifies the system that a new promotion rule has been created.
        /// This event can be used for auditing, logging, or triggering other processes
        /// that depend on the creation of a promotion rule.
        /// </remarks>
        public record PromotionRuleCreated(Guid Id, Guid PromotionId, RuleType Type, string Value) : DomainEvent;

        /// <summary>
        /// Raised when an existing promotion rule has been updated.
        /// </summary>
        /// <param name="Id">The unique identifier of the updated rule.</param>
        /// <param name="Value">The new value of the updated rule.</param>
        /// <remarks>
        /// Purpose: Notifies the system that an existing promotion rule has been updated.
        /// This event can be used for auditing, logging, or triggering other processes
        /// that depend on the modification of a promotion rule.
        /// </remarks>
        public record PromotionRuleUpdated(Guid Id, string Value) : DomainEvent;

        /// <summary>
        /// Raised when a promotion rule has been deleted.
        /// </summary>
        /// <param name="Id">The unique identifier of the deleted rule.</param>
        /// <remarks>
        /// Purpose: Notifies the system that a promotion rule has been deleted.
        /// This event can be used for auditing, logging, or triggering other processes
        /// that depend on the removal of a promotion rule.
        /// </remarks>
        public record PromotionRuleDeleted(Guid Id) : DomainEvent;

        /// <summary>
        /// Raised when a taxon is added to a promotion rule.
        /// </summary>
        /// <param name="RuleId">The ID of the promotion rule.</param>
        /// <param name="TaxonId">The ID of the taxon added.</param>
        public record TaxonAddedToRule(Guid RuleId, Guid TaxonId) : DomainEvent;

        /// <summary>
        /// Raised when a taxon is removed from a promotion rule.
        /// </summary>
        /// <param name="RuleId">The ID of the promotion rule.</param>
        /// <param name="TaxonId">The ID of the taxon removed.</param>
        public record TaxonRemovedFromRule(Guid RuleId, Guid TaxonId) : DomainEvent;

        /// <summary>
        /// Raised when a user is added to a promotion rule.
        /// </summary>
        /// <param name="RuleId">The ID of the promotion rule.</param>
        /// <param name="UserId">The ID of the user added.</param>
        public record UserAddedToRule(Guid RuleId, string UserId) : DomainEvent;

        /// <summary>
        /// Raised when a user is removed from a promotion rule.
        /// </summary>
        /// <param name="RuleId">The ID of the promotion rule.</param>
        /// <param name="UserId">The ID of the user removed.</param>
        public record UserRemovedFromRule(Guid RuleId, string UserId) : DomainEvent;

    }

    #endregion


}

