using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ReSys.Core.Common.Models.Wrappers.Responses;

/// <summary>
/// Metadata describing an API group (module).
/// </summary>
/// <remarks>
/// <para><b>Name</b>: Use PascalCase. Represents a domain or functional area. No spaces or special characters.</para>
/// <para><b>Tags</b>: Short, human-readable category labels. Prefer 1–3 tags.</para>
/// <para><b>Summary</b>: One sentence stating the group's main purpose. Avoid technical or internal details.</para>
/// <para><b>Description</b>: 2–3 concise sentences describing purpose, capabilities, and scope.</para>
/// </remarks>
public sealed record ApiGroupMeta
{
    public required string Name { get; init; }
    public required string[] Tags { get; init; }
    public required string Summary { get; init; }
    public required string Description { get; init; }
}


/// <summary>
/// Metadata describing an API endpoint.
/// </summary>
/// <remarks>
/// <para><b>Name</b>: Use PascalCase. Combine domain and action (e.g., GetProfile, CreateAccount). No spaces or special characters.</para>
/// <para><b>Summary</b>: One short sentence describing the primary action. Format: "<action> <resource>."</para>
/// <para><b>Description</b>: 1–3 sentences explaining what the endpoint does, what it returns, and when it is used.</para>
/// <para><b>ResponseType</b>: The primary success response model.</para>
/// <para><b>StatusCode</b>: The expected success status code (e.g., 200, 201, 204).</para>
/// </remarks>
public sealed record ApiEndpointMeta
{
    public required string Name { get; init; }
    public required string Summary { get; init; }
    public required string Description { get; init; }
    public required Type ResponseType { get; init; }
    public required int StatusCode { get; init; }
}

/// <summary>
/// Extension methods for applying API metadata to groups and endpoints.
/// </summary>
public static class ApiAnnotationExtensions
{
    private static readonly int[] DefaultProblemStatusCodes =
    {
        StatusCodes.Status400BadRequest,
        StatusCodes.Status401Unauthorized,
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound,
        StatusCodes.Status409Conflict,
        StatusCodes.Status500InternalServerError
    };
    /// <summary>
    /// Applies the provided <see cref="ApiGroupMeta"/> to a route group.
    /// </summary>
    public static RouteGroupBuilder UseGroupMeta(
        this RouteGroupBuilder group,
        ApiGroupMeta meta)
    {
        return group
            .WithName(meta.Name)
            .WithTags(meta.Tags)
            .WithSummary(meta.Summary)
            .WithDescription(meta.Description);
    }

    /// <summary>
    /// Applies the provided <see cref="ApiEndpointMeta"/> to a route handler.
    /// Optionally includes standard HTTP problem response types.
    /// </summary>
    public static RouteHandlerBuilder UseEndpointMeta(
        this RouteHandlerBuilder builder,
        ApiEndpointMeta meta,
        bool includeCommonProblems = true)
    {
        builder = builder
            .WithName(meta.Name)
            .WithSummary(meta.Summary)
            .WithDescription(meta.Description)
            .Produces(meta.StatusCode, meta.ResponseType);

        if (includeCommonProblems)
            builder = builder.AddCommonProblems();

        return builder;
    }

    /// <summary>
    /// Adds standard HTTP ProblemDetails responses to an endpoint.
    /// </summary>
    public static RouteHandlerBuilder AddCommonProblems(
        this RouteHandlerBuilder builder,
        int[]? statusCodes = null)
    {
        var codes = statusCodes ?? DefaultProblemStatusCodes;
        foreach (var status in codes)
            builder.ProducesProblem(status);

        return builder;
    }
}
