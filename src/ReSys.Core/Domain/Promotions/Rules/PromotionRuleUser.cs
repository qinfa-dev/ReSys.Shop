using ErrorOr;

using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Identity.Users;

namespace ReSys.Core.Domain.Promotions.Rules;

public sealed class PromotionRuleUser : AuditableEntity<Guid>
{
    #region Errors
    public static class Errors
    {
        public static Error NotFound(string userId) => Error.NotFound(code: "PromotionRuleUser.NotFound", description: $"Promotion rule user with id '{userId}' was not found.");
    }
    #endregion

    #region Properties
    public Guid PromotionRuleId { get; set; }
    public string UserId { get; set; } = null!;
    #endregion

    #region Relationships
    public PromotionRule PromotionRule { get; set; } = null!;
    public ApplicationUser ApplicationUser { get; set; } = null!;
    #endregion

    #region Constructors
    private PromotionRuleUser() { }
    #endregion

    #region Factory Methods
    public static ErrorOr<PromotionRuleUser> Create(Guid promotionRuleId, string userId)
    {
        var promotionRuleUser = new PromotionRuleUser
        {
            Id = Guid.NewGuid(),
            PromotionRuleId = promotionRuleId,
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return promotionRuleUser;
    }
    #endregion

    #region Business Logic
    public ErrorOr<Deleted> Delete()
    {
        return Result.Deleted;
    }
    #endregion
}