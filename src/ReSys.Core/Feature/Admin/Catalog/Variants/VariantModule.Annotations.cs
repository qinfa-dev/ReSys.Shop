using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Admin.Catalog.Variants;

public static partial class VariantModule
{
    private static class Annotations
    {
        public static ApiGroupMeta Group => new()
        {
            Name = "Admin.Catalog.Variant",
            Tags = ["Variant Management"],
            Summary = "Variant Management API",
            Description = "Endpoints for managing product variants"
        };

        public static ApiEndpointMeta Create => new()
        {
            Name = "Admin.Catalog.Variant.Create",
            Summary = "Create a new variant",
            Description = "Creates a new product variant with the specified details.",
            ResponseType = typeof(ApiResponse<Create.Result>),
            StatusCode = StatusCodes.Status201Created
        };

        public static ApiEndpointMeta Update => new()
        {
            Name = "Admin.Catalog.Variant.Update",
            Summary = "Update a variant",
            Description = "Updates an existing variant by ID.",
            ResponseType = typeof(ApiResponse<Update.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Delete => new()
        {
            Name = "Admin.Catalog.Variant.Delete",
            Summary = "Delete a variant",
            Description = "Deletes a variant by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static class Get
        {
            public static ApiEndpointMeta ById => new()
            {
                Name = "Admin.Catalog.Variant.Get.ById",
                Summary = "Get variant details",
                Description = "Retrieves details of a specific variant by ID.",
                ResponseType = typeof(ApiResponse<VariantModule.Get.ById.Result>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta PagedList => new()
            {
                Name = "Admin.Catalog.Variant.Get.PagedList",
                Summary = "Get paged list of variants",
                Description = "Retrieves a paginated list of variants.",
                ResponseType = typeof(ApiResponse<PagedList<VariantModule.Get.PagedList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta SelectList => new()
            {
                Name = "Admin.Catalog.Variant.Get.Select",
                Summary = "Get selectable list of variants",
                Description = "Retrieves a simplified list of variants for selection purposes.",
                ResponseType = typeof(ApiResponse<PagedList<VariantModule.Get.SelectList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };
        }

       public static class Prices
       {
           public static ApiEndpointMeta Set => new()
           {
               Name = "Admin.Catalog.Variant.Prices.Set",
               Summary = "Set variant price",
               Description = "Sets the price for a variant in a specific currency.",
               ResponseType = typeof(ApiResponse<VariantModule.Prices.Set.Result>),
               StatusCode = StatusCodes.Status200OK
           };

           public static ApiEndpointMeta List => new()
           {
               Name = "Admin.Catalog.Variant.Prices.Get",
               Summary = "Get variant prices",
               Description = "Retrieves all prices for a variant.",
               ResponseType = typeof(ApiResponse<List<VariantModule.Prices.Get.Result>>),
               StatusCode = StatusCodes.Status200OK
           };

        }

        public static class OptionValues
        {
            public static ApiEndpointMeta Manage => new()
            {
                Name = "Admin.Catalog.Variant.OptionValues.Manage",
                Summary = "Manage variant option values",
                Description = "Add or remove option values from a variant.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Get => new()
            {
                Name = "Admin.Catalog.Variant.OptionValues.Get",
                Summary = "Get variant option values",
                Description = "Get option values from a variant.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };
        }

        public static ApiEndpointMeta Discontinue => new()
        {
            Name = "Admin.Catalog.Variant.Discontinue",
            Summary = "Discontinue a variant",
            Description = "Discontinues a variant by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };
    }
}