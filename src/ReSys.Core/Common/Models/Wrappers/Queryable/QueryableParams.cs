using Microsoft.AspNetCore.Mvc;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Pagination;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;

namespace ReSys.Core.Common.Models.Wrappers.Queryable;

/// <summary>
/// Composite interface that aggregates all supported query parameters into a single contract.
/// This includes parameters for searching, sorting, filtering, and paging.
/// </summary>
public interface IQueryableParams : ISearchParams, ISortParam, IQueryFilterParams, IPagingParam
{
}

/// <summary>
/// A concrete implementation of <see cref="IQueryableParams"/> designed for model binding in ASP.NET Core controllers.
/// It consolidates all common query string parameters into a single object.
/// </summary>
public class QueryableParams : IQueryableParams
{
    /// <summary>
    /// Gets or sets the number of records to return per page.
    /// Mapped from query string: <c>?pageSize=20</c>
    /// </summary>
    [FromQuery] public int? PageSize { get; set; }

    /// <summary>
    /// Gets or sets the zero-based index of the page to retrieve.
    /// Mapped from query string: <c>?pageIndex=0</c>
    /// </summary>
    [FromQuery] public int? PageIndex { get; set; }

    /// <summary>
    /// Gets or sets the field to sort the results by.
    /// Mapped from query string: <c>?sortBy=name</c>
    /// </summary>
    [FromQuery] public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets the sort order. Accepts "asc" for ascending or "desc" for descending.
    /// Mapped from query string: <c>?sortOrder=desc</c>
    /// </summary>
    [FromQuery] public string? SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the term to search for across specified or all string fields.
    /// Mapped from query string: <c>?searchTerm=laptop</c>
    /// </summary>
    [FromQuery] public string? SearchTerm { get; set; }

    /// <summary>
    /// Gets or sets the specific fields to search within. If not provided, all string properties are searched.
    /// Mapped from query string: <c>?searchFields=name&searchFields=description</c>
    /// </summary>
    [FromQuery] public string[]? SearchFields { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the search should be case-sensitive. Defaults to false.
    /// Mapped from query string: <c>?caseSensitive=true</c>
    /// </summary>
    [FromQuery] public bool? CaseSensitive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the search should perform an exact match. Defaults to false.
    /// Mapped from query string: <c>?exactMatch=true</c>
    /// </summary>
    [FromQuery] public bool? ExactMatch { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the search should match from the start of the string. Defaults to false.
    /// Mapped from query string: <c>?startsWith=true</c>
    /// </summary>
    [FromQuery] public bool? StartsWith { get; set; }

    /// <summary>
    /// Gets or sets the raw filter string used for advanced, dynamic filtering.
    /// See the documentation for the Filter system for format details.
    /// Mapped from query string: <c>?filters=price[gte]=1000&amp;category.name[eq]=Electronics</c>
    /// </summary>
    [FromQuery] public string? Filters { get; set; }
}

