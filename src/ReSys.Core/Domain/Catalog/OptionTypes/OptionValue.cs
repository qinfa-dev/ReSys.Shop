using ErrorOr;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Catalog.Products.Variants;

namespace ReSys.Core.Domain.Catalog.OptionTypes;

/// <summary>
/// Represents a specific value or choice for an <see cref="OptionType"/> (e.g., "Red" for "Color", "Small" for "Size").
/// Option values are shared across products that utilize the same <see cref="OptionType"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Catalog Domain:</strong>
/// <list type="bullet">
/// <item>
/// <term>Specific Choice</term>
/// <description>Defines an actual selection within an option type (e.g., "Blue").</description>
/// </item>
/// <item>
/// <term>Variant Differentiation</term>
/// <description>Combinations of <see cref="OptionValue"/>s distinguish product <see cref="Catalog.Products.Variants.Variant"/>s (e.g., "Blue, Large T-Shirt").</description>
/// </item>
/// <item>
/// <term>Filterable</term>
/// <description>If the parent <see cref="OptionType"/> is filterable, these values appear as selectable filters.</description>
/// </item>
/// <item>
/// <term>Display & Ordering</term>
/// <description>Customizable presentation and position for UI display.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Attributes:</strong>
/// <list type="bullet">
/// <item>
/// <term>Name</term>
/// <description>Internal, unique identifier (e.g., "red", "small").</description>
/// </item>
/// <item>
/// <term>Presentation</term>
/// <description>Display name for customers (e.g., "Red", "Small").</description>
/// </item>
/// <item>
/// <term>Position</term>
/// <description>Order in which the value is displayed.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Concerns Implemented:</strong>
/// <list type="bullet">
/// <item><strong>IHasUniqueName</strong> - Ensures unique names within an <see cref="OptionType"/>.</item>
/// <item><strong>IHasParameterizableName</strong> - Provides flexibility for internal name vs. display presentation.</item>
/// <item><strong>IHasPosition</strong> - Defines display order.</item>
/// <item><strong>IHasMetadata</strong> - Allows for custom public and private data.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class OptionValue : Aggregate,
    IHasUniqueName,
    IHasParameterizableName,
    IHasPosition,
    IHasMetadata
{
    #region Errors

    /// <summary>
    /// Defines error scenarios specific to <see cref="OptionValue"/> operations.
    /// </summary>
    /// <remarks>
    /// These errors represent validation failures and state conflicts when managing option values.
    /// </remarks>
    public static class Errors
    {
        /// <summary>
        /// Triggered when an option value cannot be found by its ID.
        /// </summary>
        public static Error NotFound(Guid id) => Error.NotFound(code: "OptionValue.NotFound",
            description: $"Option value with ID '{id}' was not found.");

        /// <summary>
        /// Triggered when an attempt is made to create an option value with a name that already exists
        /// within the same <see cref="OptionType"/>.
        /// </summary>
        public static Error NameIsExist(string name) => Error.Conflict(code: "OptionValue.NameIsExist",
            description: $"Option value with name '{name}' already exists.");
    }

    #endregion

    #region Core Properties

    /// <summary>
    /// Gets or sets the internal unique name of the option value (e.g., "red", "small").
    /// This name is normalized (lowercase, no spaces) and unique within its <see cref="OptionType"/>.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable presentation name of the option value (e.g., "Red", "Small").
    /// This is used for display in the storefront and administration panels.
    /// </summary>
    public string Presentation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ordering position for this option value when displayed within its <see cref="OptionType"/>.
    /// Lower values typically appear first.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Gets or sets a dictionary of public metadata associated with this option value.
    /// This metadata is typically exposed via APIs and can be used for UI hints (e.g., hex code for a color value).
    /// </summary>
    public IDictionary<string, object?>? PublicMetadata { get; set; }

    /// <summary>
    /// Gets or sets a dictionary of private metadata associated with this option value.
    /// This metadata is for internal system use only and is not exposed publicly.
    /// </summary>
    public IDictionary<string, object?>? PrivateMetadata { get; set; }

    #endregion

    #region Relationships

    /// <summary>
    /// Gets or sets the unique identifier of the parent <see cref="OptionType"/> this value belongs to.
    /// </summary>
    public Guid OptionTypeId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the parent <see cref="OptionType"/>.
    /// </summary>
    public OptionType OptionType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of <see cref="VariantOptionValue"/> join entities that link
    /// this <see cref="OptionValue"/> to specific product <see cref="Catalog.Products.Variants.Variant"/>s.
    /// </summary>
    public ICollection<VariantOptionValue> VariantOptionValues { get; set; } = new List<VariantOptionValue>();

    #endregion

    #region Constructors

    /// <summary>
    /// Private constructor for ORM (Entity Framework Core) materialization.
    /// </summary>
    private OptionValue() { }

    #endregion

    #region Factory

    /// <summary>
    /// Factory method to create a new <see cref="OptionValue"/> instance.
    /// </summary>
    /// <param name="optionTypeId">The unique identifier of the parent <see cref="OptionType"/>.</param>
    /// <param name="name">The internal, unique name for the option value (e.g., "red", "blue"). Will be normalized.</param>
    /// <param name="presentation">The human-readable display name for customers (e.g., "Red", "Blue"). Defaults to <paramref name="name"/> if null.</param>
    /// <param name="position">The ordering position of this option value when displayed. Defaults to 0. Must be non-negative.</param>
    /// <param name="publicMetadata">Optional dictionary for public-facing metadata.</param>
    /// <param name="privateMetadata">Optional dictionary for internal-only metadata.</param>
    /// <returns>
    /// An <see cref="ErrorOr{OptionValue}"/> result.
    /// Returns the newly created <see cref="OptionValue"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method ensures that the <paramref name="name"/> and <paramref name="presentation"/> are normalized
    /// and that <paramref name="position"/> is non-negative.
    /// <para>
    /// A domain event for <see cref="ReSys.Core.Common.Domain.Events.Created"/> (inherited from <see cref="AuditableEntity"/>) is implicitly
    /// handled by the aggregate base class, signifying the creation of this new <see cref="OptionValue"/>.
    /// </para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// Guid colorOptionTypeId = Guid.NewGuid(); // Assume this is an existing OptionType ID
    /// var redOptionValueResult = OptionValue.Create(
    ///     optionTypeId: colorOptionTypeId,
    ///     name: "red",
    ///     presentation: "Red",
    ///     position: 1,
    ///     publicMetadata: new Dictionary&lt;string, object?&gt; { { "hexColor", "#FF0000" } });
    /// 
    /// if (redOptionValueResult.IsError)
    /// {
    ///     Console.WriteLine($"Error creating OptionValue: {redOptionValueResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     var redOptionValue = redOptionValueResult.Value;
    ///     Console.WriteLine($"Created OptionValue: {redOptionValue.Presentation}");
    /// }
    /// </code>
    /// </remarks>
    public static ErrorOr<OptionValue> Create(
        Guid optionTypeId,
        string name,
        string? presentation = null,
        int position = 0,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        (name, presentation) = HasParameterizableName.NormalizeParams(name: name, presentation: presentation);

        var optionValue = new OptionValue
        {
            Id = Guid.NewGuid(),
            OptionTypeId = optionTypeId,
            Name = name,
            Presentation = presentation,
            Position = Math.Max(val1: 0, val2: position),
            CreatedAt = DateTimeOffset.UtcNow,
            PrivateMetadata = privateMetadata ?? new Dictionary<string, object?>(),
            PublicMetadata = publicMetadata ?? new Dictionary<string, object?>(),
        };

        return optionValue;
    }

    #endregion

    #region Business Logic

    /// <summary>
    /// Updates the attributes of the <see cref="OptionValue"/>.
    /// </summary>
    /// <param name="name">The new internal name for the option value. If null, the existing name is retained.</param>
    /// <param name="presentation">The new human-readable presentation name. If null, the existing presentation is retained.</param>
    /// <param name="position">The new ordering position. If null, the existing position is retained.</param>
    /// <param name="publicMetadata">The new dictionary for public-facing metadata. If null, the existing public metadata is retained.</param>
    /// <param name="privateMetadata">The new dictionary for internal-only metadata. If null, the existing private metadata is retained.</param>
    /// <returns>
    /// An <see cref="ErrorOr{OptionValue}"/> result.
    /// Returns the updated <see cref="OptionValue"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method allows for partial updates of the option value's properties.
    /// It performs normalization for name and presentation and ensures position is non-negative.
    /// The <c>UpdatedAt</c> timestamp is automatically updated if any changes occur.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var optionValue = GetExistingOptionValue(); // Assume this retrieves an OptionValue
    /// var updateResult = optionValue.Update(
    ///     presentation: "Bright Red",
    ///     position: 10,
    ///     publicMetadata: new Dictionary&lt;string, object?&gt; { { "hexColor", "#FF0000" }, { "rgb", "255,0,0" } });
    /// 
    /// if (updateResult.IsError)
    /// {
    ///     Console.WriteLine($"Error updating OptionValue: {updateResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"OptionValue updated to: {optionValue.Presentation}");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public ErrorOr<OptionValue> Update(
        string? name = null,
        string? presentation = null,
        int? position = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        bool changed = false;

        if (name != null || presentation != null)
        {
            (name, presentation) =
                HasParameterizableName.NormalizeParams(name: name ?? Name, presentation: presentation ?? Presentation);
            if (!string.IsNullOrEmpty(value: name) && name != Name)
            {
                Name = name;
                changed = true;
            }

            if (presentation != Presentation)
            {
                Presentation = presentation;
                changed = true;
            }
        }

        if (position.HasValue && position != Position)
        {
            Position = Math.Max(val1: 0, val2: position.Value);
            changed = true;
        }

        if (publicMetadata != null && !PublicMetadata.MetadataEquals(dict2: publicMetadata))
        {
            PublicMetadata = new Dictionary<string, object?>(dictionary: publicMetadata);
            changed = true;
        }

        if (privateMetadata != null && !PrivateMetadata.MetadataEquals(dict2: privateMetadata))
        {
            PrivateMetadata = new Dictionary<string, object?>(dictionary: privateMetadata);
            changed = true;
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            // A domain event for OptionValue updated could be added here if needed.
        }

        return this;
    }

    /// <summary>
    /// Marks the <see cref="OptionValue"/> for logical deletion.
    /// In this context, deletion typically means removing it from the collection of its parent <see cref="OptionType"/>.
    /// Actual database removal or soft deletion would be handled at the aggregate or persistence level.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Always returns <see cref="Result.Deleted"/>, as the removal from the parent collection
    /// is managed by the <see cref="OptionType"/> aggregate.
    /// </returns>
    /// <remarks>
    /// This method signals that the option value should no longer be associated with its parent <see cref="OptionType"/>.
    /// The <see cref="OptionType.RemoveOptionValue(Guid)"/> method should be used to initiate the removal.
    /// </remarks>
    public ErrorOr<Deleted> Delete()
    {
        // This method primarily acts as a placeholder or to fulfill an interface.
        // The actual removal from the OptionType's collection is handled by OptionType.RemoveOptionValue.
        return Result.Deleted;
    }

    #endregion

}