using System.Text.RegularExpressions;

using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Entities;

namespace ReSys.Core.Domain.Identity.Permissions;

/// <summary>
/// Represents a standalone access permission in the system using a three-segment name:
/// {area}.{resource}.{action}.
/// The resource segment can contain dots (e.g., "role.users" in "admin.role.users.assign").
/// Supports description, value, and assignment to users/roles.
/// </summary>
public sealed class AccessPermission : AuditableEntity
{
    #region Constraints

    public static class Constraints
    {
        public const int MinSegments = 3;
        public const int MinSegmentLength = 1;
        public const int MaxSegmentLength = 64;
        public static readonly int MinNameLength = MinSegments * MinSegmentLength + (MinSegments - 1);
        public static readonly int MaxNameLength = 255; // Allow longer names for multi-segment resources
        public const int MaxDisplayNameLength = CommonInput.Constraints.Text.TitleMaxLength;
        public const int MaxDescriptionLength = CommonInput.Constraints.Text.DescriptionMaxLength;
        public const int MaxValueLength = CommonInput.Constraints.Text.ShortTextMaxLength;
        public const string SegmentAllowedPattern = @"^[a-z0-9]+(?:[-_][a-z0-9]+)*$";
        public const string ValueAllowedPattern = @"^[a-zA-Z0-9:_./-]+$";
    }

    /// <summary>
    /// Categorizes which subjects an access permission applies to.
    /// </summary>
    public enum PermissionCategory
    {
        /// <summary>
        /// No specific category; the permission does not target a user or role explicitly.
        /// </summary>
        None = 0,

        /// <summary>
        /// Permission that applies to an individual user.
        /// </summary>
        User,

        /// <summary>
        /// Permission that applies to a role (a group of users).
        /// </summary>
        Role,

        /// <summary>
        /// Permission that applies to both users and roles.
        /// </summary>
        Both
    }
    #endregion

    #region Errors

    public static class Errors
    {
        #region Basic AccessPermission Errors

        public static Error NotFound => Error.NotFound(
            code: "AccessPermission.NotFound",
            description: $"Access permission was not found");
        #endregion

        #region Format and Structure Errors
        public static Error InvalidFormat => Error.Validation(
            code: "AccessPermission.InvalidFormat",
            description: $"Access permission does not follow the required format: area.resource.action (minimum {Constraints.MinSegments} segments). " +
                         $"Each segment must be {Constraints.MinSegmentLength}-{Constraints.MaxSegmentLength} characters and match the pattern '{Constraints.SegmentAllowedPattern}'.");

        public static Error NameRequired => CommonInput.Errors.Required(prefix: nameof(AccessPermission),
            field: nameof(Name));
        public static Error NameTooShort => CommonInput.Errors.TooShort(prefix: nameof(AccessPermission),
            field: nameof(Name),
            minLength: Constraints.MinNameLength);
        public static Error NameTooLong => CommonInput.Errors.TooLong(prefix: nameof(AccessPermission),
            field: nameof(Name),
            maxLength: Constraints.MaxNameLength);

        public static Error AreaRequired => CommonInput.Errors.Required(prefix: nameof(AccessPermission),
            field: nameof(Area));
        public static Error ResourceRequired => CommonInput.Errors.Required(prefix: nameof(AccessPermission),
            field: nameof(Resource));
        public static Error ActionRequired => CommonInput.Errors.Required(prefix: nameof(AccessPermission),
            field: nameof(Action));

        public static Error DisplayNameTooLong => CommonInput.Errors.TooLong(prefix: nameof(AccessPermission),
            field: nameof(DisplayName),
            maxLength: Constraints.MaxDisplayNameLength);
        public static Error DescriptionTooLong => CommonInput.Errors.TooLong(prefix: nameof(AccessPermission),
            field: nameof(Description),
            maxLength: Constraints.MaxDescriptionLength);

        public static Error ValueTooLong => CommonInput.Errors.TooLong(prefix: nameof(AccessPermission),
            field: nameof(Value),
            maxLength: Constraints.MaxValueLength);
        public static Error ValueInvalidFormat => CommonInput.Errors.InvalidPattern(prefix: nameof(AccessPermission),
            field: nameof(Value),
            formatDescription: Constraints.ValueAllowedPattern);

        public static Error AlreadyExists(string name) => Error.Conflict(
            code: "AccessPermission.AlreadyExists",
            description: $"Access permission '{name}' already exists");
        #endregion

    }

    #endregion

    #region Properties

    /// <summary>
    /// Full permission name (e.g., "admin.user.create")
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Permission area segment (e.g., "admin")
    /// </summary>
    public string Area { get; set; } = null!;

    /// <summary>
    /// Permission resource segment (e.g., "user")
    /// </summary>
    public string Resource { get; set; } = null!;

    /// <summary>
    /// Permission action segment (e.g., "create")
    /// </summary>
    public string Action { get; set; } = null!;

    /// <summary>
    /// Human-readable display name (e.g., "Create User")
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Detailed description (e.g., "Create User in Admin area")
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Permission value for assignment (e.g., "admin:user:create")
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Permission category (User, Role, Both, None)
    /// </summary>
    public PermissionCategory? Category { get; set; } = PermissionCategory.Both;


    #endregion

    #region Constructors

    private AccessPermission() { }

    #endregion

    #region Factory Methods

    public static ErrorOr<AccessPermission> Create(
        string area,
        string resource,
        string action,
        string? displayName = null,
        string? description = null,
        string? value = null,
        PermissionCategory category = PermissionCategory.Both)
    {
        // Validate segments
        if (string.IsNullOrWhiteSpace(area))
            return Errors.AreaRequired;
        if (string.IsNullOrWhiteSpace(resource))
            return Errors.ResourceRequired;
        if (string.IsNullOrWhiteSpace(action))
            return Errors.ActionRequired;

        string trimmedArea = area.Trim().ToLowerInvariant();
        string trimmedResource = resource.Trim().ToLowerInvariant();
        string trimmedAction = action.Trim().ToLowerInvariant();

        // Validate individual segment parts (resource can contain dots)
        if (!IsValidSegment(trimmedArea) || !IsValidSegment(trimmedAction))
            return Errors.InvalidFormat;

        // Validate resource (can be multi-part like "role.users")
        if (!IsValidResourceSegment(trimmedResource))
            return Errors.InvalidFormat;

        string name = $"{trimmedArea}.{trimmedResource}.{trimmedAction}";

        if (name.Length > Constraints.MaxNameLength)
            return Errors.NameTooLong;

        // Validate display name
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            if (displayName.Length > Constraints.MaxDisplayNameLength)
                return CommonInput.Errors.TooLong(nameof(AccessPermission),
                    nameof(DisplayName),
                    Constraints.MaxDisplayNameLength);
        }

        // Validate description
        if (!string.IsNullOrWhiteSpace(description))
        {
            if (description.Length > Constraints.MaxDescriptionLength)
                return CommonInput.Errors.TooLong(nameof(AccessPermission),
                    nameof(Description),
                    Constraints.MaxDescriptionLength);
        }

        // Validate value
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (value.Length > Constraints.MaxValueLength)
                return CommonInput.Errors.TooLong(nameof(AccessPermission),
                    nameof(Value),
                    Constraints.MaxValueLength);
            if (!Regex.IsMatch(value, Constraints.ValueAllowedPattern))
                return CommonInput.Errors.InvalidPattern(nameof(AccessPermission),
                    nameof(Value),
                    Constraints.ValueAllowedPattern);
        }

        AccessPermission accessPermission = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Area = trimmedArea,
            Resource = trimmedResource,
            Action = trimmedAction,
            DisplayName = displayName?.Trim() ?? GenerateDisplayName(trimmedArea, trimmedResource, trimmedAction),
            Description = description?.Trim() ?? GenerateDescription(trimmedArea, trimmedResource, trimmedAction),
            Value = value?.Trim(),
            Category = category,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "System"
        };

        return accessPermission;
    }

    public static ErrorOr<AccessPermission> Create(
        string name,
        string? displayName = null,
        string? description = null,
        string? value = null,
        PermissionCategory category = PermissionCategory.Both,
        string? createdBy = null)
    {
        (string Area, string Resource, string Action)? parsed = ParseAccessPermissionName(accessPermissionName: name);
        if (parsed == null)
            return Errors.InvalidFormat;

        return Create(
            area: parsed.Value.Area,
            resource: parsed.Value.Resource,
            action: parsed.Value.Action,
            displayName: displayName,
            description: description,
            value: value,
            category: category);
    }

    #endregion

    #region Business Logic

    public ErrorOr<AccessPermission> Update(
        string? displayName = null,
        string? description = null,
        string? value = null,
        PermissionCategory? category = null,
        string? updatedBy = null)
    {
        bool changed = false;

        if (displayName != null && displayName != DisplayName)
        {
            if (!string.IsNullOrWhiteSpace(value: displayName))
            {
                if (displayName.Length > Constraints.MaxDisplayNameLength)
                    return CommonInput.Errors.TooLong(prefix: nameof(AccessPermission),
                        field: nameof(DisplayName),
                        maxLength: Constraints.MaxDisplayNameLength);
            }
            DisplayName = displayName.Trim();
            changed = true;
        }

        if (description != null && description != Description)
        {
            if (!string.IsNullOrWhiteSpace(value: description))
            {
                if (description.Length > Constraints.MaxDescriptionLength)
                    return CommonInput.Errors.TooLong(prefix: nameof(AccessPermission),
                        field: nameof(Description),
                        maxLength: Constraints.MaxDescriptionLength);
            }
            Description = description.Trim();
            changed = true;
        }

        if (value != null && value != Value)
        {
            if (!string.IsNullOrWhiteSpace(value: value))
            {
                if (value.Length > Constraints.MaxValueLength)
                    return CommonInput.Errors.TooLong(prefix: nameof(AccessPermission),
                        field: nameof(Value),
                        maxLength: Constraints.MaxValueLength);
                if (!Regex.IsMatch(input: value, pattern: Constraints.ValueAllowedPattern))
                    return CommonInput.Errors.InvalidPattern(prefix: nameof(AccessPermission),
                        field: nameof(Value),
                        formatDescription: Constraints.ValueAllowedPattern);
            }
            Value = value.Trim();
            changed = true;
        }

        if (category.HasValue && category != Category)
        {
            Category = category.Value;
            changed = true;
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            UpdatedBy = updatedBy;
        }

        return this;
    }

    #endregion

    #region Static Helpers

    public static string GenerateDisplayName(string area, string resource, string action)
    {
        string resourceDisplay = FormatResourceDisplayName(resource);
        string actionDisplay = FormatDisplayName(name: action);
        return $"{actionDisplay} {resourceDisplay}";
    }

    public static string GenerateDescription(string area, string resource, string action)
    {
        string areaDisplay = FormatDisplayName(name: area);
        string resourceDisplay = FormatResourceDisplayName(resource);
        string actionDisplay = FormatDisplayName(name: action);
        return $"{actionDisplay} {resourceDisplay} in {areaDisplay} area";
    }

    private static string FormatDisplayName(string name)
    {
        if (string.IsNullOrEmpty(value: name)) return string.Empty;
        return char.ToUpperInvariant(c: name[index: 0]) + name[1..];
    }

    private static string FormatResourceDisplayName(string resource)
    {
        if (string.IsNullOrEmpty(value: resource)) return string.Empty;

        // Handle multi-part resources like "role.users" -> "Role Users"
        string[] parts = resource.Split('.');
        return string.Join(" ", parts.Select(p => FormatDisplayName(p)));
    }

    private static bool IsValidSegment(string segment)
    {
        return segment.Length >= Constraints.MinSegmentLength &&
               segment.Length <= Constraints.MaxSegmentLength &&
               Regex.IsMatch(input: segment, pattern: Constraints.SegmentAllowedPattern);
    }

    private static bool IsValidResourceSegment(string resource)
    {
        // Resource can contain dots, so validate each part
        string[] parts = resource.Split('.');
        return parts.Length > 0 && parts.All(IsValidSegment);
    }

    public static bool IsValidAccessPermissionName(string accessPermissionName)
    {
        if (string.IsNullOrWhiteSpace(value: accessPermissionName))
            return false;

        string trimmed = accessPermissionName.Trim();
        if (trimmed.Length < Constraints.MinNameLength || trimmed.Length > Constraints.MaxNameLength)
            return false;

        string[] parts = trimmed.Split(separator: '.');

        // Must have at least 3 segments (area, resource, action)
        if (parts.Length < Constraints.MinSegments)
            return false;

        // Validate all segments
        foreach (string part in parts)
        {
            if (!IsValidSegment(part.ToLowerInvariant()))
                return false;
        }

        return true;
    }

    public static (string Area, string Resource, string Action)? ParseAccessPermissionName(string accessPermissionName)
    {
        if (!IsValidAccessPermissionName(accessPermissionName: accessPermissionName))
            return null;

        string[] parts = accessPermissionName.Trim().Split(separator: '.');

        if (parts.Length < 3)
            return null;

        // First segment is area
        string area = parts[0].ToLowerInvariant();

        // Last segment is action
        string action = parts[^1].ToLowerInvariant();

        // Everything in between is resource (can be multi-part like "role.users")
        string resource = string.Join(separator: ".", values: parts.Skip(count: 1).Take(count: parts.Length - 2)).ToLowerInvariant();

        return (area, resource, action);
    }

    #endregion
}