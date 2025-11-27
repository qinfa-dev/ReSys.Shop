namespace ReSys.Core.Common.Models.Pagination;

// <Summary>
// Represents paging parameters for queries.
// </Summary>
public interface IPagingParam
{
    int? PageSize { get; set; }
    int? PageIndex { get; set; }
}

// <Summary>
// Represents paging parameters for queries, implementing IPagingParam.
// </Summary>
public record PagingParams(int? PageSize = null, int? PageIndex = null) : IPagingParam
{
    public int? PageSize { get; set; } = PageSize;
    public int? PageIndex { get; set; } = PageIndex;
}