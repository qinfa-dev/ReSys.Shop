namespace ReSys.Core.Common.Models.Search;

/// <summary>
/// Interface for search parameters.
/// </summary>
public interface ISearchParams
{
    string? SearchTerm { get; }
    string[]? SearchFields { get; }
    bool? CaseSensitive { get; }
    bool? ExactMatch { get; }
    bool? StartsWith { get; }
}

/// <summary>
/// Search parameters for configuring search behavior.
/// </summary>
public record SearchParams(
    string? SearchTerm = null,
    string[]? SearchFields = null,
    bool? CaseSensitive = false,
    bool? ExactMatch = false,
    bool? StartsWith = false) : ISearchParams;