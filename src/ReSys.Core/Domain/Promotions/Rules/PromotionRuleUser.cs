using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Identity.Users;

namespace ReSys.Core.Domain.Promotions.Rules;

/// <summary>
/// Represents a junction entity linking a <see cref="PromotionRule"/> to a specific <see cref="User"/>.
/// This is used for user-based promotion rules.
/// </summary>
/// <remarks>
/// This entity is an <see cref="AuditableEntity{Guid}"/> and tracks its creation and update timestamps.
/// It ensures that a promotion rule can target specific users or user roles.
/// </remarks>
public sealed class PromotionRuleUser : AuditableEntity<Guid>
{
    #region Errors
    /// <summary>
    /// Defines domain error scenarios specific to <see cref="PromotionRuleUser"/> operations.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error indicating that a requested promotion rule user could not be found.
        /// </summary>
        /// <param name="userId">The ID of the user that was not found in the rule.</param>
        public static Error NotFound(string userId) => Error.NotFound(code: "PromotionRuleUser.NotFound", description: $"Promotion rule user with id '{userId}' was not found.");
    }
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the foreign key to the associated <see cref="PromotionRule"/>.
    /// </summary>
    public Guid PromotionRuleId { get; set; }
    /// <summary>
    /// Gets or sets the foreign key to the associated <see cref="User"/>.
    /// </summary>
    public string UserId { get; set; } = null!;
    #endregion

    #region Relationships
    /// <summary>
    /// Gets or sets the <see cref="PromotionRule"/> to which this user association belongs.
    /// </summary>
    public PromotionRule PromotionRule { get; set; } = null!;
    /// <summary>
    /// Gets or sets the <see cref="User"/> associated with this rule.
    /// </summary>
    public User User { get; set; } = null!;
    #endregion

    #region Constructors
    /// <summary>
    /// Private constructor for ORM (Entity Framework Core) materialization.
    /// </summary>
    private PromotionRuleUser() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Creates a new <see cref="PromotionRuleUser"/> instance.
    /// </summary>
    /// <param name="promotionRuleId">The ID of the <see cref="PromotionRule"/>.</param>
    /// <param name="userId">The ID of the <see cref="User"/> to associate.</param>
    /// <returns>A new <see cref="PromotionRuleUser"/> instance.</returns>
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
    /// <summary>
    /// Marks the <see cref="PromotionRuleUser"/> for deletion.
    /// </summary>
    /// <returns><see cref="Result.Deleted"/> on success.</returns>
    public ErrorOr<Deleted> Delete()
    {
        return Result.Deleted;
    }
    #endregion
}