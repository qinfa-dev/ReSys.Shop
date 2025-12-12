using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Catalog.Taxons;

public static partial class TaxonModule
{
    private static class Annotations
    {
        public static ApiGroupMeta Group => new()
        {
            Name = "Admin.Catalog.Taxon",
            Tags = ["Taxon Management"],
            Summary = "Taxon Management API",
            Description = "Endpoints for managing catalog taxons"
        };

        public static ApiEndpointMeta Create => new()
        {
            Name = "Admin.Catalog.Taxon.Create",
            Summary = "Create a new taxon",
            Description = "Creates a new catalog taxon with the specified details.",
            ResponseType = typeof(ApiResponse<Create.Result>),
            StatusCode = StatusCodes.Status201Created
        };

        public static ApiEndpointMeta Update => new()
        {
            Name = "Admin.Catalog.Taxon.Update",
            Summary = "Update a taxon",
            Description = "Updates an existing catalog taxon by ID.",
            ResponseType = typeof(ApiResponse<Update.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Delete => new()
        {
            Name = "Admin.Catalog.Taxon.Delete",
            Summary = "Delete a taxon",
            Description = "Deletes a catalog taxon by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static class Get
        {
            public static ApiEndpointMeta ById => new()
            {
                Name = "Admin.Catalog.Taxon.Get.ById",
                Summary = "Get taxon details",
                Description = "Retrieves details of a specific catalog taxon by ID.",
                ResponseType = typeof(ApiResponse<TaxonModule.Get.ById.Result>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta PagedList => new()
            {
                Name = "Admin.Catalog.Taxon.Get.Paged",
                Summary = "Get paged list of taxons",
                Description = "Retrieves a paginated list of catalog taxons.",
                ResponseType = typeof(ApiResponse<PagedList<TaxonModule.Get.PagedList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta SelectList => new()
            {
                Name = "Admin.Catalog.Taxon.Get.Select",
                Summary = "Get selectable list of taxons",
                Description = "Retrieves a simplified list of catalog taxons for selection purposes.",
                ResponseType = typeof(ApiResponse<PagedList<TaxonModule.Get.SelectList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

        }

        public static class Images
        {
            public static ApiEndpointMeta Update => new()
            {
                Name = "Admin.Catalog.Taxon.Images.Update",
                Summary = "Synchronize taxon images",
                Description = "Fully synchronize (add/update/delete) taxon images in one request.",
                ResponseType = typeof(ApiResponse<List<TaxonModule.Images.Update.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Get => new()
            {
                Name = "Admin.Catalog.Taxon.Images.Get",
                Summary = "Synchronize taxon images",
                Description = "FRetrieves the images for an existing taxon.",
                ResponseType = typeof(ApiResponse<List<TaxonModule.Images.Get.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

        }


        public static class Hierarchy
        {
            public static ApiEndpointMeta GetTree => new()
            {
                Name = "Admin.Catalog.Taxon.GetTree",
                Summary = "Get taxon tree",
                Description = "Retrieves hierarchical tree structure of taxons.",
                ResponseType = typeof(ApiResponse<Models.TreeListItem>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta GetFlatList => new()
            {
                Name = "Admin.Catalog.Taxon.GetFlatList",
                Summary = "Get flat list of taxons",
                Description = "Retrieves a flattened paginated list of taxons with hierarchy indicators.",
                ResponseType = typeof(ApiResponse<PagedList<Models.FlatListItem>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Rebuild => new()
            {
                Name = "Admin.Catalog.Taxon.Rebuild",
                Summary = "Rebuild taxonomy hierarchy",
                Description = "Rebuilds nested sets and permalinks for a taxonomy.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Validate => new()
            {
                Name = "Admin.Catalog.Taxon.Validate",
                Summary = "Validate taxonomy hierarchy",
                Description = "Validates taxonomy hierarchy for cycles and invalid references.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };
        }

        public static class Rules
        {
            public static ApiEndpointMeta Get => new()
            {
                Name = "Admin.Catalog.Taxon.Rules.Get",
                Summary = "Get taxon rules",
                Description = "Retrieves the rules for an existing taxon.",
                ResponseType = typeof(ApiResponse<PagedList<Models.RuleItem>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Update => new()
            {
                Name = "Admin.Catalog.Taxon.Rules.Update",
                Summary = "Update taxon rules",
                Description = "Updates the rules for an existing taxon.",
                ResponseType = typeof(ApiResponse<TaxonModule.Rules.Update.Result>),
                StatusCode = StatusCodes.Status200OK
            };
        }

    }
}