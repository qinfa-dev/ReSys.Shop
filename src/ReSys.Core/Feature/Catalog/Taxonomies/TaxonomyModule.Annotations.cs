using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Catalog.Taxonomies;

public static partial class TaxonomyModule
{
    private static class Annotations
    {
        // ---------------- Group Metadata ----------------
        public static ApiGroupMeta Group => new()
        {
            Name = "Admin.Catalog.Taxonomy",
            Tags = ["Taxonomy"],
            Summary = "Taxonomy Management API",
            Description = "Endpoints for managing catalog taxonomies"
        };

        // ---------------- Endpoint Metadata ----------------
        public static ApiEndpointMeta Create => new()
        {
            Name = "Admin.Catalog.Taxonomy.Create",
            Summary = "Create a new taxonomy",
            Description = "Creates a new catalog taxonomy with the specified details.",
            ResponseType = typeof(ApiResponse<TaxonomyModule.Create.Result>),
            StatusCode = StatusCodes.Status201Created
        };

        public static ApiEndpointMeta Delete => new()
        {
            Name = "Admin.Catalog.Taxonomy.Delete",
            Summary = "Delete a taxonomy",
            Description = "Deletes a catalog taxonomy by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static class Get
        {
            public static ApiEndpointMeta ById => new()
            {
                Name = "Admin.Catalog.Taxonomy.Get.ById",
                Summary = "Get taxonomy details",
                Description = "Retrieves details of a specific catalog taxonomy by ID.",
                ResponseType = typeof(ApiResponse<TaxonomyModule.Get.ById.Result>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta PagedList => new()
            {
                Name = "Admin.Catalog.Taxonomy.Get.PagedList",
                Summary = "Get paged list of taxonomies",
                Description = "Retrieves a paginated list of catalog taxonomies.",
                ResponseType = typeof(ApiResponse<List<TaxonomyModule.Get.PagedList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta SelectList => new()
            {
                Name = "Admin.Catalog.Taxonomy.GetSelectList",
                Summary = "Get selectable list of taxonomies",
                Description = "Retrieves a simplified list of catalog taxonomies for selection purposes.",
                ResponseType = typeof(ApiResponse<List<TaxonomyModule.Get.SelectList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };
        }
       
        public static ApiEndpointMeta Update => new()
        {
            Name = "Admin.Catalog.Taxonomy.Update",
            Summary = "Update a taxonomy",
            Description = "Updates an existing catalog taxonomy by ID.",
            ResponseType = typeof(ApiResponse<Update.Result>),
            StatusCode = StatusCodes.Status200OK
        };
    }
}