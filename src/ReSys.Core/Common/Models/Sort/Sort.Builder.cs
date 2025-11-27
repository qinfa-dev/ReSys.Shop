namespace ReSys.Core.Common.Models.Sort;
/// <summary>
/// Fluent builder for sorting operations.
/// </summary>
public sealed class SortBuilder<T>
{
    private readonly IQueryable<T> _query;
    private readonly List<ISortParam> _sortParams = [];

    internal SortBuilder(IQueryable<T> query)
    {
        _query = query;
    }

    public SortBuilder<T> By(string field, string order = "asc")
    {
        _sortParams.Add(item: new SortParams(SortBy: field,
            SortOrder: order));
        return this;
    }

    public SortBuilder<T> ByDescending(string field)
    {
        return By(field: field,
            order: "desc");
    }

    public SortBuilder<T> ThenBy(string field, string order = "asc")
    {
        return By(field: field,
            order: order);
    }

    public SortBuilder<T> ThenByDescending(string field)
    {
        return By(field: field,
            order: "desc");
    }

    public IQueryable<T> Execute()
    {
        return _query.ApplySort(sortParams: _sortParams.ToArray());
    }

    public static implicit operator Func<IQueryable<T>>(SortBuilder<T> builder) => builder.Execute;
}

