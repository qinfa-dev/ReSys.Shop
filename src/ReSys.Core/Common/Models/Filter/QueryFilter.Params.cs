using Microsoft.AspNetCore.Mvc;

namespace ReSys.Core.Common.Models.Filter;
public interface IQueryFilterParams
{
    /// <summary>
    /// The raw filter string from the query parameters.
    /// </summary>
    string? Filters { get; set; }
}

public sealed record QueryFilterParams(string? Filters = null) : IQueryFilterParams
{
    [FromQuery] public string? Filters { get; set; } = Filters;
}
