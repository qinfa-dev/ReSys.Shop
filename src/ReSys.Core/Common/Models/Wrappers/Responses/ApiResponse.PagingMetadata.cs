using ReSys.Core.Common.Models.Wrappers.PagedLists;

namespace ReSys.Core.Common.Models.Wrappers.Responses;

/// <summary>
/// Metadata for paginated responses, providing information about the current page, total pages, and item counts.
/// This class is used within `ApiResponse<T>` to convey pagination details.
/// </summary>
public sealed class PaginationMetadata
{
    /// <summary>Gets the current page number (1-based).</summary>
    public int CurrentPage { get; private set; }

    /// <summary>Gets the total number of pages available.</summary>
    public int TotalPages { get; private set; }

    /// <summary>Gets the maximum number of items per page.</summary>
    public int PageSize { get; private set; }

    /// <summary>Gets the total number of items across all pages.</summary>
    public int TotalCount { get; private set; }

    /// <summary>Gets a value indicating whether there is a previous page.</summary>
    public bool HasPrevious { get; private set; }

    /// <summary>Gets a value indicating whether there is a next page.</summary>
    public bool HasNext { get; private set; }

    /// <summary>Gets the 1-based index of the first item on the current page.</summary>
    public int FirstItemIndex { get; private set; }

    /// <summary>Gets the 1-based index of the last item on the current page.</summary>
    public int LastItemIndex { get; private set; }

    /// <summary>Gets a value indicating whether the current page is the first page.</summary>
    public bool IsFirstPage { get; private set; }

    /// <summary>Gets a value indicating whether the current page is the last page.</summary>
    public bool IsLastPage { get; private set; }

    /// <summary>Gets a value indicating whether the collection is empty.</summary>
    public bool IsEmpty { get; private set; }

    /// <summary>
    /// Private constructor to enforce the use of factory methods for instantiation.
    /// </summary>
    private PaginationMetadata() { }

    /// <summary>
    /// Creates a <see cref="PaginationMetadata"/> instance from a <see cref="PagedList{T}"/>.
    /// </summary>
    /// <param name="pagedList">The paged list to create metadata from.</param>
    /// <returns>A new <see cref="PaginationMetadata"/> instance.</returns>
    public static PaginationMetadata FromPagedList<T>(PagedList<T> pagedList)
    {
        ArgumentNullException.ThrowIfNull(argument: pagedList);

        return new PaginationMetadata
        {
            CurrentPage = pagedList.PageNumber,
            PageSize = pagedList.PageSize,
            TotalCount = pagedList.TotalCount,
            TotalPages = pagedList.TotalPages,
            HasPrevious = pagedList.HasPreviousPage,
            HasNext = pagedList.HasNextPage,
            FirstItemIndex = pagedList.StartIndex,
            LastItemIndex = pagedList.EndIndex,
            IsFirstPage = pagedList.IsFirstPage,
            IsLastPage = pagedList.IsLastPage,
            IsEmpty = pagedList.IsEmpty
        };
    }

    /// <summary>
    /// Creates a <see cref="PaginationMetadata"/> instance with the specified pagination parameters.
    /// </summary>
    /// <param name="currentPage">The current page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    /// <returns>A new <see cref="PaginationMetadata"/> instance.</returns>
    public static PaginationMetadata Create(int currentPage, int pageSize, int totalCount)
    {
        if (totalCount < 0)
            throw new ArgumentOutOfRangeException(paramName: nameof(totalCount),
                message: "Total count cannot be negative.");
        if (pageSize < 0)
            throw new ArgumentOutOfRangeException(paramName: nameof(pageSize),
                message: "Page size cannot be negative.");
        if (totalCount > 0 && currentPage <= 0)
            throw new ArgumentOutOfRangeException(paramName: nameof(currentPage),
                message: "Current page must be a positive number.");

        if (totalCount == 0)
        {
            return new PaginationMetadata
            {
                CurrentPage = 0,
                PageSize = pageSize,
                TotalCount = 0,
                TotalPages = 0,
                HasPrevious = false,
                HasNext = false,
                FirstItemIndex = 0,
                LastItemIndex = 0,
                IsFirstPage = true,
                IsLastPage = true,
                IsEmpty = true
            };
        }

        int totalPages = (int)Math.Ceiling(a: totalCount / (double)pageSize);
        if (currentPage > totalPages)
        {
            currentPage = totalPages; // Cap current page at last page.
        }

        int startIndex = (currentPage - 1) * pageSize + 1;
        int endIndex = Math.Min(val1: startIndex + pageSize - 1,
            val2: totalCount);

        return new PaginationMetadata
        {
            CurrentPage = currentPage,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPrevious = currentPage > 1,
            HasNext = currentPage < totalPages,
            FirstItemIndex = startIndex,
            LastItemIndex = endIndex,
            IsFirstPage = currentPage == 1,
            IsLastPage = currentPage == totalPages,
            IsEmpty = false
        };
    }
}
