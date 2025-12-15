using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Catalog.OptionTypes;

public static partial class OptionTypeModule
{
    private static class Annotations
    {
        // ---------------- Group Metadata ----------------
        public static ApiGroupMeta Group => new()
        {
            Name = "Admin.Catalog.OptionType",
            Tags = ["Option Type"],
            Summary = "Option Type Management API",
            Description = "Endpoints for managing catalog option types"
        };

        // ---------------- Endpoint Metadata ----------------
        public static ApiEndpointMeta Create => new()
        {
            Name = "Admin.Catalog.OptionType.Create",
            Summary = "Create a new option type",
            Description = "Creates a new catalog option type with the specified details.",
            ResponseType = typeof(ApiResponse<OptionTypeModule.Create.Result>),
            StatusCode = StatusCodes.Status201Created
        };

        public static ApiEndpointMeta Delete => new()
        {
            Name = "Admin.Catalog.OptionType.Delete",
            Summary = "Delete an option type",
            Description = "Deletes a catalog option type by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetById => new()
        {
            Name = "Admin.Catalog.OptionType.GetById",
            Summary = "Get option type details",
            Description = "Retrieves details of a specific catalog option type by ID.",
            ResponseType = typeof(ApiResponse<OptionTypeModule.Get.ById.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetPagedList => new()
        {
            Name = "Admin.Catalog.OptionType.GetPagedList",
            Summary = "Get paged list of option types",
            Description = "Retrieves a paginated list of catalog option types.",
            ResponseType = typeof(ApiResponse<List<OptionTypeModule.Get.PagedList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetSelectList => new()
        {
            Name = "Admin.Catalog.OptionType.GetSelectList",
            Summary = "Get selectable list of option types",
            Description = "Retrieves a simplified list of catalog option types for selection purposes.",
            ResponseType = typeof(ApiResponse<List<OptionTypeModule.Get.SelectList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Update => new()
        {
            Name = "Admin.Catalog.OptionType.Update",
            Summary = "Update an option type",
            Description = "Updates an existing catalog option type by ID.",
            ResponseType = typeof(ApiResponse<Update.Result>),
            StatusCode = StatusCodes.Status200OK
        };
    }
}