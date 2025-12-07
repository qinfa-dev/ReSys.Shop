using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Promotions.Promotions;

namespace ReSys.Core.Domain.Promotions.Rules;

public sealed class PromotionRule : Aggregate<Guid>
{
    #region Constraints
    public static class Constraints
    {
        public const int ValueMaxLength =CommonInput.Constraints.Text.LongTextMaxLength;
    }

    public enum RuleType { FirstOrder, ProductInclude, ProductExclude, CategoryInclude, CategoryExclude, MinimumQuantity, UserRole }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error NotFound(Guid id) => Error.NotFound(code: "PromotionRule.NotFound", description: $"Promotion rule with ID '{id}' was not found.");
        public static Error ValueRequired => CommonInput.Errors.Required(prefix: nameof(PromotionRule), field: nameof(Value));
        public static Error ValueTooLong => CommonInput.Errors.TooLong(prefix: nameof(PromotionRule), field: nameof(Value), maxLength: Constraints.ValueMaxLength);
        public static Error InvalidRuleType => CommonInput.Errors.InvalidValue(prefix: nameof(PromotionRule), field: nameof(Type));
    }
    #endregion

    #region Properties
    public Guid PromotionId { get; set; }
    public RuleType Type { get; set; }
    public string Value { get; set; } = string.Empty;
    #endregion

    #region Relationships
    public Promotion Promotion { get; set; } = null!;
    public ICollection<PromotionRuleTaxon> PromotionRuleTaxons { get; set; } = new List<PromotionRuleTaxon>();
    public ICollection<PromotionRuleUser> PromotionRuleUsers { get; set; } = new List<PromotionRuleUser>();
    #endregion

    #region Constructors
    private PromotionRule() { }
    #endregion

    #region Factory Methods
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

    public ErrorOr<Deleted>
    Delete()
    {
        AddDomainEvent(domainEvent: new Events.PromotionRuleDeleted(Id: Id));
        return Result.Deleted;
    }

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

    public static class Events

    {

        /// <summary>

        /// Purpose: Notifies the system that a new promotion rule has been created.
        /// This event can be used for auditing, logging, or triggering other processes
        /// that depend on the creation of a promotion rule.
        /// </summary>

        public record PromotionRuleCreated(Guid Id, Guid PromotionId, RuleType Type, string Value) : DomainEvent;

        /// <summary>
        /// Purpose: Notifies the system that an existing promotion rule has been updated.
        /// This event can be used for auditing, logging, or triggering other processes
        /// that depend on the modification of a promotion rule.
        /// </summary>
        public record PromotionRuleUpdated(Guid Id, string Value) : DomainEvent;

        /// <summary>
        /// Purpose: Notifies the system that a promotion rule has been deleted.
        /// This event can be used for auditing, logging, or triggering other processes
        /// that depend on the removal of a promotion rule.
        /// </summary>

        public record PromotionRuleDeleted(Guid Id) : DomainEvent;

        public record TaxonAddedToRule(Guid RuleId, Guid TaxonId) : DomainEvent;

        public record TaxonRemovedFromRule(Guid RuleId, Guid TaxonId) : DomainEvent;

        public record UserAddedToRule(Guid RuleId, string UserId) : DomainEvent;

        public record UserRemovedFromRule(Guid RuleId, string UserId) : DomainEvent;

    }

    #endregion


}

