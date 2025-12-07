using System.Text.RegularExpressions;

using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Entities;

namespace ReSys.Core.Domain.Identity.Permissions;

/// <summary>
/// Represents a granular access permission in the system, defined by a hierarchical three-segment name:
/// <c>{area}.{resource}.{action}</c>.
/// <para>
/// This entity allows for fine-grained control over user and role capabilities, enabling robust
/// Role-Based Access Control (RBAC) and policy-driven authorization.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// <strong>Permission Naming Convention:</strong>
/// <list type="bullet">
/// <item><term>Area</term><description>The top-level functional domain or module (e.g., "admin", "catalog", "order").</description></item>
/// <item><term>Resource</term><description>The specific entity or feature being acted upon. This segment can contain dots for nested resources (e.g., "user", "role.users").</description></item>
/// <item><term>Action</term><description>The operation being performed (e.g., "create", "view", "edit", "delete", "assign", "manage").</description></item>
/// </list>
/// Example: <c>"admin.user.create"</c> for creating users in the admin area, or <c>"order.status.update"</c> for updating order statuses.
/// </para>
///
/// <para>
/// <strong>Key Attributes:</strong>
/// <list type="bullet">
/// <item><term>Name</term><description>The full, unique permission string.</description></item>
/// <item><term>DisplayName</term><description>A human-readable label for the permission.</description></item>
/// <item><term>Description</term><description>A detailed explanation of what the permission allows.</description></item>
/// <item><term>Category</term><description>Defines whether the permission applies to users, roles, or both.</description></item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Concerns Implemented:</strong>
/// <list type="bullet">
/// <item><strong>AuditableEntity</strong> - Inherits auditing properties like <c>CreatedAt</c> and <c>UpdatedAt</c>.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class AccessPermission : AuditableEntity
{
    #region Constraints

    /// <summary>
    /// Defines constraints and constant values specific to <see cref="AccessPermission"/> naming and properties.
    /// </summary>
    public static class Constraints
    {
        /// <summary>Minimum number of segments required for an access permission name (area.resource.action).</summary>
        public const int MinSegments = 3;
        /// <summary>Minimum length allowed for an individual segment (area, resource part, action).</summary>
        public const int MinSegmentLength = 1;
        /// <summary>Maximum length allowed for an individual segment (area, resource part, action).</summary>
        public const int MaxSegmentLength = 64;
        /// <summary>Calculated minimum total length for a valid permission name, considering minimum segments and separators.</summary>
        public static readonly int MinNameLength = MinSegments * MinSegmentLength + (MinSegments - 1);
        /// <summary>Maximum total length for an access permission name. Allows for longer names for multi-segment resources.</summary>
        public static readonly int MaxNameLength = 255;
        /// <summary>Maximum length for the human-readable display name of the permission.</summary>
        public const int MaxDisplayNameLength = CommonInput.Constraints.Text.TitleMaxLength;
        /// <summary>Maximum length for the detailed description of the permission.</summary>
        public const int MaxDescriptionLength = CommonInput.Constraints.Text.DescriptionMaxLength;
        /// <summary>Maximum length for the optional custom value associated with the permission.</summary>
        public const int MaxValueLength = CommonInput.Constraints.Text.ShortTextMaxLength;
        /// <summary>Regex pattern for validating individual segments of the permission name (lowercase, alphanumeric, hyphens, underscores).</summary>
        public const string SegmentAllowedPattern = @"^[a-z0-9]+(?:[-_][a-z0-9]+)*$";
        /// <summary>Regex pattern for validating the optional custom value associated with the permission.</summary>
        public const string ValueAllowedPattern = @"^[a-zA-Z0-9:_./-]+$";
    }

    /// <summary>
    /// Categorizes the target subjects to which an access permission typically applies.
    /// This helps in filtering and presenting permissions appropriately in user interfaces.
    /// </summary>
    public enum PermissionCategory
    {
        /// <summary>
        /// No specific category; the permission does not explicitly target a user or role.
        /// Can be used for general system permissions or those managed by other means.
        /// </summary>
        None = 0,

        /// <summary>
        /// Permission that primarily applies to an individual user directly.
        /// </summary>
        User,

        /// <summary>
        /// Permission that primarily applies to a role (a group of users).
        /// </summary>
        Role,

        /// <summary>
        /// Permission that can apply to both individual users and roles.
        /// </summary>
        Both
    }
    #endregion

    #region Errors

    /// <summary>
    /// Defines domain error scenarios specific to <see cref="AccessPermission"/> operations and validation.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        #region Basic AccessPermission Errors

        /// <summary>
        /// Error indicating that a requested <see cref="AccessPermission"/> could not be found.
        /// </summary>
        public static Error NotFound => Error.NotFound(
            code: "AccessPermission.NotFound",
            description: $"Access permission was not found");
        #endregion

        #region Format and Structure Errors
        /// <summary>
        /// Error indicating that the access permission name does not follow the required <c>area.resource.action</c> format.
        /// </summary>
        public static Error InvalidFormat => Error.Validation(
            code: "AccessPermission.InvalidFormat",
            description: $"Access permission does not follow the required format: area.resource.action (minimum {Constraints.MinSegments} segments). " +
                         $"Each segment must be {Constraints.MinSegmentLength}-{Constraints.MaxSegmentLength} characters and match the pattern '{Constraints.SegmentAllowedPattern}'.");

        /// <summary>Error indicating that the permission name is missing or empty.</summary>
        public static Error NameRequired => CommonInput.Errors.Required(prefix: nameof(AccessPermission),
            field: nameof(Name));
        /// <summary>Error indicating that the permission name is too short.</summary>
        public static Error NameTooShort => CommonInput.Errors.TooShort(prefix: nameof(AccessPermission),
            field: nameof(Name),
            minLength: Constraints.MinNameLength);
        /// <summary>Error indicating that the permission name is too long.</summary>
        public static Error NameTooLong => CommonInput.Errors.TooLong(prefix: nameof(AccessPermission),
            field: nameof(Name),
            maxLength: Constraints.MaxNameLength);

        /// <summary>Error indicating that the 'area' segment of the permission name is missing or empty.</summary>
        public static Error AreaRequired => CommonInput.Errors.Required(prefix: nameof(AccessPermission),
            field: nameof(Area));
        /// <summary>Error indicating that the 'resource' segment of the permission name is missing or empty.</summary>
        public static Error ResourceRequired => CommonInput.Errors.Required(prefix: nameof(AccessPermission),
            field: nameof(Resource));
        /// <summary>Error indicating that the 'action' segment of the permission name is missing or empty.</summary>
        public static Error ActionRequired => CommonInput.Errors.Required(prefix: nameof(AccessPermission),
            field: nameof(Action));

        /// <summary>Error indicating that the display name exceeds the maximum allowed length.</summary>
        public static Error DisplayNameTooLong => CommonInput.Errors.TooLong(prefix: nameof(AccessPermission),
            field: nameof(DisplayName),
            maxLength: Constraints.MaxDisplayNameLength);
        /// <summary>Error indicating that the description exceeds the maximum allowed length.</summary>
        public static Error DescriptionTooLong => CommonInput.Errors.TooLong(prefix: nameof(AccessPermission),
            field: nameof(Description),
            maxLength: Constraints.MaxDescriptionLength);

        /// <summary>Error indicating that the custom value exceeds the maximum allowed length.</summary>
        public static Error ValueTooLong => CommonInput.Errors.TooLong(prefix: nameof(AccessPermission),
            field: nameof(Value),
            maxLength: Constraints.MaxValueLength);
        /// <summary>Error indicating that the custom value does not match the allowed format pattern.</summary>
        public static Error ValueInvalidFormat => CommonInput.Errors.InvalidPattern(prefix: nameof(AccessPermission),
            field: nameof(Value),
            formatDescription: Constraints.ValueAllowedPattern);

        /// <summary>Error indicating that an access permission with the given name already exists.</summary>
        /// <param name="name">The name of the conflicting access permission.</param>
        public static Error AlreadyExists(string name) => Error.Conflict(
            code: "AccessPermission.AlreadyExists",
            description: $"Access permission '{name}' already exists");
        #endregion

    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the full, unique name of the permission (e.g., "admin.user.create").
    /// This is the primary identifier for the permission.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the 'area' segment of the permission name (e.g., "admin").
    /// </summary>
    public string Area { get; set; } = null!;

    /// <summary>
    /// Gets or sets the 'resource' segment of the permission name (e.g., "user", "role.users").
    /// </summary>
    public string Resource { get; set; } = null!;

    /// <summary>
    /// Gets or sets the 'action' segment of the permission name (e.g., "create").
    /// </summary>
    public string Action { get; set; } = null!;

    /// <summary>
    /// Gets or sets a human-readable display name for the permission (e.g., "Create User").
    /// If not explicitly set during creation, it's automatically generated.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets a detailed description explaining what this permission allows (e.g., "Allows creating new users in the Admin area").
    /// If not explicitly set during creation, it's automatically generated.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets an optional custom value associated with the permission (e.g., "admin:user:create").
    /// This can be used for policy evaluation or external system integration.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="PermissionCategory"/> for this permission.
    /// This indicates whether the permission applies to individual users, roles, or both.
    /// Defaults to <see cref="PermissionCategory.Both"/>.
    /// </summary>
    public PermissionCategory? Category { get; set; } = PermissionCategory.Both;

    #endregion

    #region Constructors

    /// <summary>
    /// Private constructor for ORM (Entity Framework Core) materialization.
    /// </summary>
    private AccessPermission() { }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Factory method to create a new <see cref="AccessPermission"/> instance by specifying its
    /// constituent parts: area, resource, and action.
    /// This method performs validation on the provided segments and generates default display names/descriptions.
    /// </summary>
    /// <param name="area">The top-level functional domain or module (e.g., "admin", "catalog").</param>
    /// <param name="resource">The specific entity or feature (e.g., "user", "role.users").</param>
    /// <param name="action">The operation being performed (e.g., "create", "view").</param>
    /// <param name="displayName">Optional: a human-readable name. If null, automatically generated.</param>
    /// <param name="description">Optional: a detailed explanation. If null, automatically generated.</param>
    /// <param name="value">Optional: a custom value. If null, no custom value is set.</param>
    /// <param name="category">The <see cref="PermissionCategory"/> for this permission. Defaults to <see cref="PermissionCategory.Both"/>.</param>
    /// <returns>
    /// An <see cref="ErrorOr{AccessPermission}"/> result.
    /// Returns the newly created <see cref="AccessPermission"/> instance on success.
    /// Returns one of the <see cref="Errors"/> if validation fails (e.g., <see cref="Errors.InvalidFormat"/>, <see cref="Errors.AreaRequired"/>).
    /// </returns>
    /// <remarks>
    /// This method concatenates the <paramref name="area"/>, <paramref name="resource"/>, and <paramref name="action"/>
    /// to form the unique <see cref="Name"/> of the permission. It applies normalization (trimming, lower-casing)
    /// to the segments. Default <see cref="DisplayName"/> and <see cref="Description"/> are generated if not provided.
    /// </remarks>
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
            CreatedBy = "System" // Typically "System" for initial seeding
        };

        return accessPermission;
    }

    /// <summary>
    /// Factory method to create a new <see cref="AccessPermission"/> instance from a full permission name string.
    /// This method parses the name into its area, resource, and action segments and then calls
    /// the primary <see cref="Create(string, string, string, string?, string?, string?, PermissionCategory)"/> method.
    /// </summary>
    /// <param name="name">The full permission name string (e.g., "admin.user.create").</param>
    /// <param name="displayName">Optional: a human-readable name. If null, automatically generated.</param>
    /// <param name="description">Optional: a detailed explanation. If null, automatically generated.</param>
    /// <param name="value">Optional: a custom value. If null, no custom value is set.</param>
    /// <param name="category">The <see cref="PermissionCategory"/> for this permission. Defaults to <see cref="PermissionCategory.Both"/>.</param>
    /// <param name="createdBy">Optional: The identifier of the user who created this permission.</param>
    /// <returns>
    /// An <see cref="ErrorOr{AccessPermission}"/> result.
    /// Returns the newly created <see cref="AccessPermission"/> instance on success.
    /// Returns <see cref="Errors.InvalidFormat"/> if the permission name cannot be parsed.
    /// Returns errors from the underlying <see cref="Create(string, string, string, string?, string?, string?, PermissionCategory)"/> method if validation fails.
    /// </returns>
    /// <remarks>
    /// This overload is convenient for defining permissions using their canonical string representation.
    /// </remarks>
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

    /// <summary>
    /// Updates the mutable properties of the <see cref="AccessPermission"/>.
    /// The <see cref="Name"/> (area.resource.action) of the permission is immutable after creation.
    /// </summary>
    /// <param name="displayName">The new human-readable display name. If null, the existing display name is retained.</param>
    /// <param name="description">The new detailed description. If null, the existing description is retained.</param>
    /// <param name="value">The new custom value. If null, the existing value is retained.</param>
    /// <param name="category">The new <see cref="PermissionCategory"/>. If null, the existing category is retained.</param>
    /// <param name="updatedBy">The identifier of the user who updated this permission.</param>
    /// <returns>
    /// An <see cref="ErrorOr{AccessPermission}"/> result.
    /// Returns the updated <see cref="AccessPermission"/> instance on success.
    /// Returns errors if validation fails (e.g., <see cref="Errors.DisplayNameTooLong"/>, <see cref="Errors.ValueInvalidFormat"/>).
    /// </returns>
    /// <remarks>
    /// This method allows for partial updates and performs validation on the provided string lengths and format.
    /// The <c>UpdatedAt</c> timestamp and <c>UpdatedBy</c> fields are automatically updated if any changes occur.
    /// </remarks>
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

    /// <summary>
    /// Generates a human-readable display name for an access permission from its constituent parts.
    /// Example: For area="admin", resource="user", action="create" -> "Create User".
    /// </summary>
    /// <param name="area">The area segment of the permission.</param>
    /// <param name="resource">The resource segment of the permission.</param>
    /// <param name="action">The action segment of the permission.</param>
    /// <returns>A formatted display name string.</returns>
    public static string GenerateDisplayName(string area, string resource, string action)
    {
        string resourceDisplay = FormatResourceDisplayName(resource);
        string actionDisplay = FormatDisplayName(name: action);
        return $"{actionDisplay} {resourceDisplay}";
    }

    /// <summary>
    /// Generates a detailed description for an access permission from its constituent parts.
    /// Example: For area="admin", resource="user", action="create" -> "Create User in Admin area".
    /// </summary>
    /// <param name="area">The area segment of the permission.</param>
    /// <param name="resource">The resource segment of the permission.</param>
    /// <param name="action">The action segment of the permission.</param>
    /// <returns>A formatted description string.</returns>
    public static string GenerateDescription(string area, string resource, string action)
    {
        string areaDisplay = FormatDisplayName(name: area);
        string resourceDisplay = FormatResourceDisplayName(resource);
        string actionDisplay = FormatDisplayName(name: action);
        return $"{actionDisplay} {resourceDisplay} in {areaDisplay} area";
    }

    /// <summary>
    /// Formats a single segment string into a more readable display format (e.g., "create" -> "Create").
    /// Capitalizes the first letter.
    /// </summary>
    /// <param name="name">The segment string to format.</param>
    /// <returns>The formatted display string.</returns>
    private static string FormatDisplayName(string name)
    {
        if (string.IsNullOrEmpty(value: name)) return string.Empty;
        return char.ToUpperInvariant(c: name[index: 0]) + name[1..];
    }

    /// <summary>
    /// Formats a resource segment, potentially handling multi-part resources separated by dots.
    /// Example: "role.users" -> "Role Users".
    /// </summary>
    /// <param name="resource">The resource segment string.</param>
    /// <returns>The formatted resource display name.</returns>
    private static string FormatResourceDisplayName(string resource)
    {
        if (string.IsNullOrEmpty(value: resource)) return string.Empty;

        // Handle multi-part resources like "role.users" -> "Role Users"
        string[] parts = resource.Split('.');
        return string.Join(" ", parts.Select(p => FormatDisplayName(p)));
    }

    /// <summary>
    /// Validates if an individual permission segment (area, resource part, or action) conforms to the defined constraints.
    /// Checks length and allowed character pattern.
    /// </summary>
    /// <param name="segment">The segment string to validate.</param>
    /// <returns>True if the segment is valid, false otherwise.</returns>
    private static bool IsValidSegment(string segment)
    {
        return segment.Length >= Constraints.MinSegmentLength &&
               segment.Length <= Constraints.MaxSegmentLength &&
               Regex.IsMatch(input: segment, pattern: Constraints.SegmentAllowedPattern);
    }

    /// <summary>
    /// Validates if a resource segment (which can be multi-part, e.g., "role.users") conforms to the defined constraints.
    /// It validates each part of the resource segment individually.
    /// </summary>
    /// <param name="resource">The resource string to validate.</param>
    /// <returns>True if the resource segment is valid, false otherwise.</returns>
    private static bool IsValidResourceSegment(string resource)
    {
        // Resource can contain dots, so validate each part
        string[] parts = resource.Split('.');
        return parts.Length > 0 && parts.All(IsValidSegment);
    }

    /// <summary>
    /// Checks if a given string is a valid access permission name according to the defined format and constraints.
    /// </summary>
    /// <param name="accessPermissionName">The permission name string to validate (e.g., "admin.user.create").</param>
    /// <returns>True if the permission name is valid, false otherwise.</returns>
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

    /// <summary>
    /// Parses a valid access permission name string into its constituent parts: area, resource, and action.
    /// </summary>
    /// <param name="accessPermissionName">The permission name string to parse (e.g., "admin.user.create").</param>
    /// <returns>
    /// A nullable tuple containing the Area, Resource, and Action strings if parsing is successful and the name is valid;
    /// otherwise, returns null.
    /// </returns>
    /// <remarks>
    /// The resource segment can be multi-part (e.g., "role.users"), and this method correctly extracts it.
    /// </remarks>
    public static (string Area, string Resource, string Action)? ParseAccessPermissionName(string accessPermissionName)
    {
        if (!IsValidAccessPermissionName(accessPermissionName: accessPermissionName))
            return null;

        string[] parts = accessPermissionName.Trim().Split(separator: '.');

        if (parts.Length < 3) // Should already be caught by IsValidAccessPermissionName, but as a safeguard
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