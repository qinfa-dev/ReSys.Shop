using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Catalog.OptionValues;

public static partial class OptionValueModule
{
    private static class Annotations
    {
        // ---------------- Group Metadata ----------------
        public static ApiGroupMeta Group => new()
        {
            Name = "Admin.Catalog.OptionValue",
            Tags = ["Option Value"],
            Summary = "Option Value Management API",
            Description = "Endpoints for managing catalog option values"
        };

        // ---------------- Endpoint Metadata ----------------
        public static ApiEndpointMeta Create => new()
        {
            Name = "Admin.Catalog.OptionValue.Create",
            Summary = "Create a new option value",
            Description = "Creates a new catalog option value with the specified details.",
            ResponseType = typeof(ApiResponse<OptionValueModule.Create.Result>),
            StatusCode = StatusCodes.Status201Created
        };

        public static ApiEndpointMeta Delete => new()
        {
            Name = "Admin.Catalog.OptionValue.Delete",
            Summary = "Delete an option value",
            Description = "Deletes a catalog option value by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetById => new()
        {
            Name = "Admin.Catalog.OptionValue.GetById",
            Summary = "Get option value details",
            Description = "Retrieves details of a specific catalog option value by ID.",
            ResponseType = typeof(ApiResponse<OptionValueModule.Get.ById.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetPagedList => new()
        {
            Name = "Admin.Catalog.OptionValue.GetPagedList",
            Summary = "Get paged list of option values",
            Description = "Retrieves a paginated list of catalog option values.",
            ResponseType = typeof(ApiResponse<List<OptionValueModule.Get.PagedList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetSelectList => new()
        {
            Name = "Admin.Catalog.OptionValue.GetSelectList",
            Summary = "Get selectable list of option values",
            Description = "Retrieves a simplified list of catalog option values for selection purposes.",
            ResponseType = typeof(ApiResponse<List<OptionValueModule.Get.SelectList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Update => new()
        {
            Name = "Admin.Catalog.OptionValue.Update",
            Summary = "Update an option value",
            Description = "Updates an existing catalog option value by ID.",
            ResponseType = typeof(ApiResponse<Update.Result>),
            StatusCode = StatusCodes.Status200OK
        };
    }
}