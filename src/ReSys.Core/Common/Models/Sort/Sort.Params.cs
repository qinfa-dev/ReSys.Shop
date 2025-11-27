namespace ReSys.Core.Common.Models.Sort;

// <Summary>
// Represents sorting parameters for queries.
// </Summary>
public record SortParams(string? SortBy = null, string? SortOrder = "asc") : ISortParam
{
    public string? SortBy { get; set; } = SortBy;
    public string? SortOrder { get; set; } = SortOrder;
    public bool IsValid => !string.IsNullOrWhiteSpace(value: SortBy);
    public bool IsDescending => string.Equals(a: SortOrder,
        b: "desc",
        comparisonType: StringComparison.OrdinalIgnoreCase);
}

public interface ISortParam
{
    public string? SortBy { get; set; }
    string? SortOrder { get; set; }
}