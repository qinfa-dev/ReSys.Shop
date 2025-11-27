using System.Linq.Expressions;

namespace ReSys.Core.Common.Models.Search;

/// <summary>
/// Fluent builder for search operations.
/// </summary>
public sealed class SearchBuilder<T>
{
    private readonly IQueryable<T> _query;
    private readonly string _searchTerm;
    private readonly List<Expression<Func<T, string>>> _searchFields = [];
    private SearchParams _params = new();

    internal SearchBuilder(IQueryable<T> query, string searchTerm)
    {
        _query = query;
        _searchTerm = searchTerm;
    }

    public SearchBuilder<T> In(Expression<Func<T, string>> field)
    {
        _searchFields.Add(item: field);
        return this;
    }

    public SearchBuilder<T> In(params Expression<Func<T, string>>[] fields)
    {
        _searchFields.AddRange(collection: fields);
        return this;
    }

    public SearchBuilder<T> CaseSensitive(bool caseSensitive = true)
    {
        _params = _params with { CaseSensitive = caseSensitive };
        return this;
    }

    public SearchBuilder<T> ExactMatch(bool exactMatch = true)
    {
        _params = _params with { ExactMatch = exactMatch };
        return this;
    }

    public SearchBuilder<T> StartsWith(bool startsWith = true)
    {
        _params = _params with { StartsWith = startsWith };
        return this;
    }

    public IQueryable<T> Execute()
    {
        if (string.IsNullOrWhiteSpace(value: _searchTerm))
            return _query;

        if (_searchFields.Count != 0)
        {
            return _query.ApplySearch(searchTerm: _searchTerm,
                searchExpressions: _searchFields.ToArray());
        }

        return _query.ApplySearch(searchParams: new SearchParams(SearchTerm: _searchTerm,
            SearchFields: null,
            CaseSensitive: _params.CaseSensitive,
            ExactMatch: _params.ExactMatch,
            StartsWith: _params.StartsWith));
    }

    public static implicit operator Func<IQueryable<T>>(SearchBuilder<T> builder) => builder.Execute;
}