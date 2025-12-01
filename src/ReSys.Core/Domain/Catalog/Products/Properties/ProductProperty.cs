using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Catalog.Properties;

namespace ReSys.Core.Domain.Catalog.Products.Properties;

/// <summary>
/// Junction entity for Product-Property association.
/// Represents a specific property value for a product.
/// Supports translations for value and acts as list for positioning.
/// </summary>
public sealed class ProductProperty :
    AuditableEntity,
    IHasPosition,
    IHasFilterParam
{
    #region Constraints

    public static class Constraints
    {
        public const int MaxValueLength = CommonInput.Constraints.Text.LongTextMaxLength;
        public const int MaxFilterParamLength = CommonInput.Constraints.SlugsAndVersions.SlugMaxLength;
        public const string FilterParamPattern = CommonInput.Constraints.SlugsAndVersions.SlugPattern;
        public const int MaxPosition = int.MaxValue;
    }

    #endregion

    #region Errors

    public static class Errors
    {
        public static Error NotFound(Guid id) => CommonInput.Errors.NotFound(prefix: nameof(ProductProperty), field: id.ToString());

        public static Error UnexpectedError(string operation) => Error.Unexpected(
            code: $"ProductProperty.Unexpected.{operation}",
            description: $"An unexpected error occurred during {operation} operation");
    }

    #endregion

    #region Properties

    public Guid ProductId { get; set; }
    public Guid PropertyId { get; set; }
    public int Position { get; set; }

    private string _value = string.Empty;
    public string Value
    {
        get => _value;
        set => _value = value.Trim();
    }

    public string? FilterParam { get; set; }

    #endregion

    #region Relationships

    public Product Product { get; set; } = null!;
    public Property Property { get; set; } = null!;

    #endregion

    #region Computed Properties

    public bool IsFilterable => Property.Filterable;

    #endregion

    #region Factory Methods

    public static ErrorOr<ProductProperty> Create(
        Guid productId,
        Guid propertyId,
        string value,
        int position = 0,
        bool filterable = false,
        string? filterParam = null)
    {
        var productProperty = new ProductProperty
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            PropertyId = propertyId,
            Value = value,
            Position = Math.Max(val1: 0, val2: Math.Min(val1: position, val2: Constraints.MaxPosition)),
            FilterParam = filterParam,
        };

        // If filterParam is not provided and property is filterable, generate it from the value
        if (filterable && string.IsNullOrWhiteSpace(value: filterParam))
        {
            productProperty.SetFilterParam(propertyExpression: p => p.Value);
        }

        return productProperty;
    }

    #endregion

    #region Business Logic

    public ErrorOr<ProductProperty> Update(
        string? value = null,
        int? position = null,
        string? filterParam = null)
    {
        bool changed = false;

        // Update value
        if (value is not null && value != Value)
        {
            Value = value;
            changed = true;
        }

        // Update filterParam
        if (filterParam is not null && filterParam != FilterParam)
        {
            FilterParam = filterParam;
            changed = true;
        }
        else if (value is not null && Property.Filterable)
        {
            // If value changed and filterParam was not explicitly provided, regenerate filterParam
            this.SetFilterParam(propertyExpression: p => p.Value);
            changed = true;
        }

        // Update position
        if (position.HasValue && position != Position)
        {
            Position = Math.Max(val1: 0, val2: Math.Min(val1: position.Value, val2: Constraints.MaxPosition));
            changed = true;
        }

        if (changed)
        {
            // UpdatedAt is handled by the base class/ORM
        }

        return this;
    }

    public ErrorOr<Deleted> Delete()
    {
        return Result.Deleted;
    }

    #endregion
}