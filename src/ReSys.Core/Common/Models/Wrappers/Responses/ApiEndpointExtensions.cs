using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ReSys.Core.Common.Models.Wrappers.Responses;

/// <summary>
/// Metadata for grouping related API endpoints.
/// </summary>
public sealed record ApiGroupAnnotation
{
    public required string Route { get; init; }
    public required string Name { get; init; }
    public required string[] Tags { get; init; }
    public required string Summary { get; init; }
    public required string Description { get; init; }
}

/// <summary>
/// Metadata for an individual API endpoint.
/// </summary>
public sealed record ApiEndpointAnnotation
{
    public required string Route { get; init; }
    public required string Name { get; init; }
    public required string Summary { get; init; }
    public required string Description { get; init; }
    public required Type ResponseType { get; init; }
    public required int StatusCode { get; init; }
}

/// <summary>
/// Extension methods for applying API annotations to routes and endpoints.
/// </summary>
public static class ApiAnnotationExtensions
{
    /// <summary>
    /// Applies group-level metadata (name, tags, summary, description) to a RouteGroupBuilder.
    /// </summary>
    public static RouteGroupBuilder ApplyGroup(this RouteGroupBuilder group, ApiGroupAnnotation meta)
    {
        return group
            .WithName(endpointName: meta.Name)
            .WithTags(tags: meta.Tags)
            .WithSummary(summary: meta.Summary)
            .WithDescription(description: meta.Description);
    }

    /// <summary>
    /// Applies endpoint-level metadata (name, summary, description, responses) to a RouteHandlerBuilder.
    /// </summary>
    public static RouteHandlerBuilder ApplyEndpoint(
        this RouteHandlerBuilder builder,
        ApiEndpointAnnotation meta,
        bool includeCommonProblemResponses = true)
    {
        builder = builder
            .WithName(endpointName: meta.Name)
            .WithSummary(summary: meta.Summary)
            .WithDescription(description: meta.Description)
            .Produces(statusCode: meta.StatusCode, responseType: meta.ResponseType);

        if (includeCommonProblemResponses)
        {
            builder = builder
                .ProducesProblem(statusCode: StatusCodes.Status400BadRequest)
                .ProducesProblem(statusCode: StatusCodes.Status401Unauthorized)
                .ProducesProblem(statusCode: StatusCodes.Status403Forbidden)
                .ProducesProblem(statusCode: StatusCodes.Status404NotFound)
                .ProducesProblem(statusCode: StatusCodes.Status409Conflict)
                .ProducesProblem(statusCode: StatusCodes.Status500InternalServerError);
        }

        return builder;
    }
}
