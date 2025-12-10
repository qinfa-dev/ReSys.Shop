using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Promotions.Promotions;

namespace ReSys.Core.Domain.Promotions.Audits;

/// <summary>
/// Audit log entry for promotion changes.
/// Tracks all modifications, activations, and usage of promotions.
/// </summary>
public sealed class PromotionUsage : AuditableEntity<Guid>
{
    #region Properties
    public Guid PromotionId { get; set; }
    public string Action { get; set; } = string.Empty; // Created, Updated, Activated, Deactivated, RuleAdded, RuleRemoved, Used
    public string Description { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public IDictionary<string, object?>? ChangesBefore { get; set; }
    public IDictionary<string, object?>? ChangesAfter { get; set; }
    public IDictionary<string, object?>? Metadata { get; set; }
    #endregion

    #region Relationships
    public Promotion Promotion { get; set; } = null!;
    #endregion

    #region Constructors
    private PromotionUsage() { }
    #endregion

    #region Factory Methods
    public static PromotionUsage Create(
        Guid promotionId,
        string action,
        string description,
        string? userId = null,
        string? userEmail = null,
        string? ipAddress = null,
        string? userAgent = null,
        Dictionary<string, object?>? changesBefore = null,
        Dictionary<string, object?>? changesAfter = null,
        Dictionary<string, object?>? metadata = null)
    {
        return new PromotionUsage
        {
            Id = Guid.NewGuid(),
            PromotionId = promotionId,
            Action = action,
            Description = description,
            UserId = userId,
            UserEmail = userEmail,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ChangesBefore = changesBefore,
            ChangesAfter = changesAfter,
            Metadata = metadata,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
    #endregion
}
