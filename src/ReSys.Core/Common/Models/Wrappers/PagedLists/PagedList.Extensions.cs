using ReSys.Core.Common.Models.Pagination;

namespace ReSys.Core.Common.Models.Wrappers.PagedLists;

/// <summary>
/// Extension methods for creating PagedList instances from IQueryable and IEnumerable sources using PagingParams.
/// </summary>
public static class PagedListExtensions
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;
    private const int MaxAllItemsLimit = 1000;

    #region IQueryable Extensions (Async)

    /// <summary>
    /// Creates a PagedList from an IQueryable using PagingParams with specified page size.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="pagingParams">The paging parameters.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <param name="fallbackDefaultPageSize">The page size (defaults to 10, max 100).</param>
    /// <returns>A PagedList containing the requested page of items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when query is null.</exception>
    public static async Task<PagedList<T>> ToPagedListAsync<T>(this IQueryable<T> query,
        IPagingParam? pagingParams,
        CancellationToken cancellationToken = default,
        int fallbackDefaultPageSize = DefaultPageSize)
    {
        ArgumentNullException.ThrowIfNull(argument: query);

        // Use pagingParams.PageSize if provided, otherwise use fallback
        int effectivePageSize = NormalizePageSize(pageSize: pagingParams?.PageSize ?? fallbackDefaultPageSize);
        (int effectivePageIndex, int effectivePageNumber) = CalculatePaginationValues(pagingParams: pagingParams);

        int totalCount = await query.CountAsync(cancellationToken: cancellationToken);
        int totalPages = CalculateTotalPages(totalCount: totalCount,
            pageSize: effectivePageSize);

        // Adjust page number if beyond total pages or if no data exists
        (effectivePageNumber, effectivePageIndex) = AdjustPageBounds(requestedPageNumber: effectivePageNumber,
            requestedPageIndex: effectivePageIndex,
            totalPages: totalPages);

        List<T> items = await query
            .Skip(count: effectivePageIndex * effectivePageSize)
            .Take(count: effectivePageSize)
            .ToListAsync(cancellationToken: cancellationToken);

        return new PagedList<T>(items: items,
            totalCount: totalCount,
            pageNumber: effectivePageNumber,
            pageSize: effectivePageSize);
    }

    /// <summary>
    /// Creates a PagedList from an IQueryable using default pagination or returns all items (capped) if no paging parameters are provided.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="pagingParams">The paging parameters.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <param name="maxAllItemsLimit">The maximum number of items to return when no pagination is applied.</param>
    /// <returns>A PagedList containing the requested page of items or all items (capped) if no paging is specified.</returns>
    /// <exception cref="ArgumentNullException">Thrown when query is null.</exception>
    public static async Task<PagedList<T>> ToPagedListOrAllAsync<T>(this IQueryable<T> query,
        IPagingParam? pagingParams,
        CancellationToken cancellationToken = default,
        int maxAllItemsLimit = MaxAllItemsLimit)
    {
        ArgumentNullException.ThrowIfNull(argument: query);

        if (pagingParams?.HasPagingValues() != true)
        {
            int effectiveLimit = Math.Min(val1: Math.Max(val1: maxAllItemsLimit,
                    val2: 1),
                val2: MaxAllItemsLimit);
            List<T> items = await query.Take(count: effectiveLimit).ToListAsync(cancellationToken: cancellationToken);
            return new PagedList<T>(items: items,
                totalCount: items.Count,
                pageNumber: 1,
                pageSize: effectiveLimit);
        }

        return await query.ToPagedListAsync(pagingParams: pagingParams,
            cancellationToken: cancellationToken,
            fallbackDefaultPageSize: DefaultPageSize);
    }
    #endregion

    #region IEnumerable Extensions (Synchronous)

    /// <summary>
    /// Creates a PagedList from an IEnumerable using PagingParams with specified page size.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="pagingParams">The paging parameters.</param>
    /// <param name="fallbackPageSize">The default page size to use when no paging params provided.</param>
    /// <returns>A PagedList containing the requested page of items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
    public static PagedList<T> ToPagedList<T>(
        this IEnumerable<T> source,
        IPagingParam? pagingParams,
        int fallbackPageSize = DefaultPageSize)
    {
        ArgumentNullException.ThrowIfNull(argument: source);

        List<T> sourceList = source.ToList();
        int totalCount = sourceList.Count;

        // Use pagingParams.PageSize if provided, otherwise use fallbackPageSize
        int effectivePageSize = NormalizePageSize(pageSize: pagingParams?.PageSize ?? fallbackPageSize);
        (int effectivePageIndex, int effectivePageNumber) = CalculatePaginationValues(pagingParams: pagingParams);

        int totalPages = CalculateTotalPages(totalCount: totalCount,
            pageSize: effectivePageSize);

        // Adjust page number if beyond total pages or if no data exists
        (effectivePageNumber, effectivePageIndex) = AdjustPageBounds(requestedPageNumber: effectivePageNumber,
            requestedPageIndex: effectivePageIndex,
            totalPages: totalPages);

        List<T> items = sourceList
            .Skip(count: effectivePageIndex * effectivePageSize)
            .Take(count: effectivePageSize)
            .ToList();

        return new PagedList<T>(items: items,
            totalCount: totalCount,
            pageNumber: effectivePageNumber,
            pageSize: effectivePageSize);
    }

    /// <summary>
    /// Creates an empty PagedList from an IEnumerable with specified pagination metadata.
    /// </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    /// <param name="source">The source enumerable.</param>
    /// <param name="pagingParams">The paging parameters.</param>
    /// <param name="fallbackPageSize">The default page size to use when no paging params provided.</param>
    /// <returns>An empty PagedList with the specified pagination metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when source is null.</exception>
    public static PagedList<T> ToEmptyPagedList<T>(
        this IEnumerable<T> source,
        IPagingParam? pagingParams,
        int fallbackPageSize = DefaultPageSize)
    {
        ArgumentNullException.ThrowIfNull(argument: source);

        int totalCount = source.Count();

        // Use pagingParams.PageSize if provided, otherwise use fallbackPageSize
        int effectivePageSize = NormalizePageSize(pageSize: pagingParams?.PageSize ?? fallbackPageSize);
        (int effectivePageIndex, int effectivePageNumber) = CalculatePaginationValues(pagingParams: pagingParams);

        int totalPages = CalculateTotalPages(totalCount: totalCount,
            pageSize: effectivePageSize);

        // Adjust page number if beyond total pages or if no data exists
        (effectivePageNumber, effectivePageIndex) = AdjustPageBounds(requestedPageNumber: effectivePageNumber,
            requestedPageIndex: effectivePageIndex,
            totalPages: totalPages);

        return new PagedList<T>(items:
            [
            ],
            totalCount: totalCount,
            pageNumber: effectivePageNumber,
            pageSize: effectivePageSize);
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Normalizes the page size to ensure it's within acceptable bounds.
    /// </summary>
    /// <param name="pageSize">The requested page size.</param>
    /// <returns>A normalized page size between 1 and MaxPageSize.</returns>
    private static int NormalizePageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(val1: pageSize,
            val2: MaxPageSize);

    /// <summary>
    /// Calculates effective page index (0-based) and page number (1-based) from paging parameters.
    /// </summary>
    /// <param name="pagingParams">The paging parameters.</param>
    /// <returns>A tuple containing the effective page index and page number.</returns>
    private static (int EffectivePageIndex, int EffectivePageNumber) CalculatePaginationValues(IPagingParam? pagingParams)
    {
        int effectivePageIndex = Math.Max(val1: pagingParams?.EffectivePageIndex() ?? 0,
            val2: 0);
        // Cap page index to prevent overflow when computing page number
        effectivePageIndex = Math.Min(val1: effectivePageIndex,
            val2: int.MaxValue - 1);
        int effectivePageNumber = effectivePageIndex + 1;
        return (effectivePageIndex, effectivePageNumber);
    }

    /// <summary>
    /// Calculates the total number of pages based on total count and page size.
    /// </summary>
    /// <param name="totalCount">The total number of items.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>The total number of pages.</returns>
    private static int CalculateTotalPages(int totalCount, int pageSize) =>
        pageSize > 0 ? (int)Math.Ceiling(a: totalCount / (double)pageSize) : 0;

    /// <summary>
    /// Adjusts page bounds to ensure valid page numbers and handles edge cases.
    /// </summary>
    /// <param name="requestedPageNumber">The requested page number (1-based).</param>
    /// <param name="requestedPageIndex">The requested page index (0-based).</param>
    /// <param name="totalPages">The total number of available pages.</param>
    /// <returns>A tuple containing the adjusted page number and page index.</returns>
    private static (int AdjustedPageNumber, int AdjustedPageIndex) AdjustPageBounds(
        int requestedPageNumber,
        int requestedPageIndex,
        int totalPages)
    {
        // If no data exists or no pages, always return page 1
        if (totalPages <= 0)
        {
            return (1, 0);
        }

        // If requested page is beyond available pages, return the last page
        if (requestedPageNumber > totalPages)
        {
            return (totalPages, totalPages - 1);
        }

        // If requested page is valid, use it as-is
        return (requestedPageNumber, requestedPageIndex);
    }

    #endregion
}