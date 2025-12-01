using ErrorOr;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Catalog.Products.Variants;

namespace ReSys.Core.Domain.Catalog.OptionTypes;
public sealed class OptionValue : Aggregate,
    IHasUniqueName,
    IHasParameterizableName,
    IHasPosition,
    IHasMetadata
{
    #region Errors
    public static class Errors
    {
        public static Error NotFound(Guid id) => Error.NotFound(code: "OptionValue.NotFound", description: $"Option value with ID '{id}' was not found.");
        public static Error NameIsExist(string name) => Error.Conflict(code: "OptionValue.NameIsExist", description: $"Option value with name '{name}' already exists.");
    }
    #endregion

    #region Core Properties
    public string Name { get; set; } = string.Empty;
    public string Presentation { get; set; } = string.Empty;
    public int Position { get; set; }
    public IDictionary<string, object?>? PublicMetadata { get; set; }
    public IDictionary<string, object?>? PrivateMetadata { get; set; }
    #endregion

    #region Relationships
    public Guid OptionTypeId { get; set; }
    public OptionType OptionType { get; set; } = null!;
    public ICollection<VariantOptionValue> VariantOptionValues { get; set; } = new List<VariantOptionValue>();
    #endregion

    #region Constructors
    private OptionValue() { }
    #endregion

    #region Factory
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
            (name, presentation) = HasParameterizableName.NormalizeParams(name: name ?? Name, presentation: presentation ?? Presentation);
            if (!string.IsNullOrEmpty(value: name) && name != Name)
            {
                Name = name;
                changed = true;
            }
            if (presentation != Presentation) { Presentation = presentation; changed = true; }
        }

        if (position.HasValue && position != Position) { Position = Math.Max(val1: 0, val2: position.Value); changed = true; }

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
        }

        return this;
    }

    public ErrorOr<Deleted> Delete()
    {
        return Result.Deleted;
    }
    #endregion

}
