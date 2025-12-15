using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Promotions.Promotions;

namespace ReSys.Core.Domain.Promotions.Usages;

/// <summary>
/// Audit log entry for promotion changes.
/// Tracks all modifications, activations, and usage of promotions.
/// </summary>
/// <summary>
/// Audit log entry for promotion changes.
/// Tracks all modifications, activations, and usage of promotions.
/// </summary>
/// <remarks>
/// <para>
/// <b>Responsibility:</b>
/// This entity represents a single audit event for a promotion, detailing the action performed,
/// the changes made, and contextual information about the user and request.
/// </para>
///
/// <para>
/// <b>Key Features:</b>
/// <list type="bullet">
/// <item><b>Action Tracking:</b> Records actions like Created, Updated, Activated, Deactivated, RuleAdded, Used.</item>
/// <item><b>User Context:</b> Stores UserId and UserEmail for accountability.</item>
/// <item><b>Request Context:</b> Includes IpAddress and UserAgent for request environment details.</item>
/// <item><b>State Snapshots:</b> Stores ChangesBefore and ChangesAfter as dictionaries for flexible change tracking.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class PromotionUsage : AuditableEntity<Guid>
{
    #region Properties
    /// <summary>
    /// Gets or sets the foreign key to the associated <see cref="Promotion"/>.
    /// </summary>
    public Guid PromotionId { get; set; }
    /// <summary>
    /// Gets or sets the action performed (e.g., Created, Updated, Activated, Deactivated, RuleAdded, Used).
    /// </summary>
    public string Action { get; set; } = string.Empty; // Created, Updated, Activated, Deactivated, RuleAdded, RuleRemoved, Used
    /// <summary>
    /// Gets or sets a detailed description of the audit event.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the ID of the user who performed the action, if available.
    /// </summary>
    public string? UserId { get; set; }
    /// <summary>
    /// Gets or sets the email of the user who performed the action, if available.
    /// </summary>
    public string? UserEmail { get; set; }
    /// <summary>
    /// Gets or sets the IP address from which the action originated.
    /// </summary>
    public string? IpAddress { get; set; }
    /// <summary>
    /// Gets or sets the client user-agent string associated with the action.
    /// </summary>
    public string? UserAgent { get; set; }
    /// <summary>
    /// Gets or sets a dictionary snapshot of the entity state before the action.
    /// </summary>
    public IDictionary<string, object?>? ChangesBefore { get; set; }
    /// <summary>
    /// Gets or sets a dictionary snapshot of the entity state after the action.
    /// </summary>
    public IDictionary<string, object?>? ChangesAfter { get; set; }
    /// <summary>
    /// Gets or sets additional contextual metadata for the audit entry.
    /// </summary>
    public IDictionary<string, object?>? Metadata { get; set; }
    #endregion

    #region Relationships
    /// <summary>
    /// Gets or sets the <see cref="Promotion"/> to which this audit log entry belongs.
    /// </summary>
    public Promotion Promotion { get; set; } = null!;
    #endregion

    #region Constructors
    /// <summary>
    /// Private constructor for ORM (Entity Framework Core) materialization.
    /// </summary>
    private PromotionUsage() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Creates a new <see cref="PromotionUsage"/> instance.
    /// </summary>
    /// <param name="promotionId">The ID of the promotion being audited.</param>
    /// <param name="action">The action performed (e.g., "Created", "Updated").</param>
    /// <param name="description">A detailed explanation of the audit event.</param>
    /// <param name="userId">Optional. The ID of the user who performed the action.</param>
    /// <param name="userEmail">Optional. The email of the user who performed the action.</param>
    /// <param name="ipAddress">Optional. The IP address from which the action originated.</param>
    /// <param name="userAgent">Optional. The user-agent string associated with the action.</param>
    /// <param name="changesBefore">Optional. Snapshot of the entity state before the action.</param>
    /// <param name="changesAfter">Optional. Snapshot of the entity state after the action.</param>
    /// <param name="metadata">Optional. Additional contextual metadata.</param>
    /// <returns>A new <see cref="PromotionUsage"/> instance.</returns>
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
