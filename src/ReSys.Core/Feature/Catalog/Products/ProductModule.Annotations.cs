using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Responses;
using ReSys.Core.Feature.Catalog.OptionTypes;
using ReSys.Core.Feature.Catalog.PropertyTypes;
using ReSys.Core.Feature.Catalog.Taxons;

namespace ReSys.Core.Feature.Catalog.Products;

public static partial class ProductModule
{
    private static class Annotations
    {
        public static ApiGroupMeta Group => new()
        {
            Name = "Admin.Catalog.Product",
            Tags = ["Product Management"],
            Summary = "Product Management API",
            Description = "Endpoints for managing catalog products"
        };

        public static ApiEndpointMeta Create => new()
        {
            Name = "Admin.Catalog.Product.Create",
            Summary = "Create a new product",
            Description = "Creates a new catalog product with the specified details.",
            ResponseType = typeof(ApiResponse<Create.Result>),
            StatusCode = StatusCodes.Status201Created
        };

        public static ApiEndpointMeta Update => new()
        {
            Name = "Admin.Catalog.Product.Update",
            Summary = "Update a product",
            Description = "Updates an existing catalog product by ID.",
            ResponseType = typeof(ApiResponse<Update.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Delete => new()
        {
            Name = "Admin.Catalog.Product.Delete",
            Summary = "Delete a product",
            Description = "Deletes a catalog product by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static class Get
        {
            public static ApiEndpointMeta ById => new()
            {
                Name = "Admin.Catalog.Product.GetById",
                Summary = "Get product details",
                Description = "Retrieves details of a specific catalog product by ID.",
                ResponseType = typeof(ApiResponse<ProductModule.Get.ById.Result>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta PagedList => new()
            {
                Name = "Admin.Catalog.Product.GetPagedList",
                Summary = "Get paged list of products",
                Description = "Retrieves a paginated list of catalog products.",
                ResponseType = typeof(ApiResponse<PagedList<ProductModule.Get.PagedList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta SelectList => new()
            {
                Name = "Admin.Catalog.Product.GetSelectList",
                Summary = "Get selectable list of products",
                Description = "Retrieves a simplified list of catalog products for selection purposes.",
                ResponseType = typeof(ApiResponse<PagedList<ProductModule.Get.SelectList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };
        }
        public static class Status
        {
            public static ApiEndpointMeta Activate => new()
            {
                Name = "Admin.Catalog.Product.Activate",
                Summary = "Activate a product",
                Description = "Activates a catalog product by ID.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Archive => new()
            {
                Name = "Admin.Catalog.Product.Archive",
                Summary = "Archive a product",
                Description = "Archives a catalog product by ID.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Draft => new()
            {
                Name = "Admin.Catalog.Product.Draft",
                Summary = "Set product to draft",
                Description = "Sets a catalog product to draft status by ID.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Discontinue => new()
            {
                Name = "Admin.Catalog.Product.Discontinue",
                Summary = "Discontinue a product",
                Description = "Discontinues a catalog product by ID.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };
        }

        public static class Images
        {
            public static ApiEndpointMeta Manage => new()
            {
                Name = "Admin.Catalog.Product.Images.Manage",
                Summary = "Update product images",
                Description = "Fully synchronize (add/update/delete) product images in one request.",
                ResponseType = typeof(ApiResponse<List<ProductModule.Images.Manage.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Upload => new()
            {
                Name = "Admin.Catalog.Product.Images.Upload",
                Summary = "Add product image",
                Description = "Add product image in one request.",
                ResponseType = typeof(ApiResponse<List<ProductModule.Images.Upload.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Edit => new()
            {
                Name = "Admin.Catalog.Product.Images.Edit",
                Summary = "Edit product images",
                Description = "Edit or change existing product image in one request.",
                ResponseType = typeof(ApiResponse<List<ProductModule.Images.Edit.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Remove => new()
            {
                Name = "Admin.Catalog.Product.Images.Remove",
                Summary = "Remove product image",
                Description = "Remove product image in one request.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Get => new()
            {
                Name = "Admin.Catalog.Product.Images.Get",
                Summary = "Get product images",
                Description = "Retrieves the images for an existing product.",
                ResponseType = typeof(ApiResponse<List<ProductModule.Images.GetList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

        }

        public static class Classifications
        {
            public static ApiEndpointMeta Manage => new()
            {
                Name = "Admin.Catalog.Product.Classifications.Manage",
                Summary = "Update product images",
                Description = "Fully synchronize (add/update/delete) product classifications in one request.",
                ResponseType = typeof(ApiResponse<List<TaxonModule.Models.SelectItem>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Get => new()
            {
                Name = "Admin.Catalog.Product.Classifications.Get",
                Summary = "Get product classifications",
                Description = "Retrieves all taxon classifications assigned to a product.",
                ResponseType = typeof(ApiResponse<List<TaxonModule.Models.SelectItem>>),
                StatusCode = StatusCodes.Status200OK
            };
        }

        public static class OptionTypes
        {
            public static ApiEndpointMeta Manage => new()
            {
                Name = "Admin.Catalog.Product.OptionTypes.Manage",
                Summary = "Update product images",
                Description = "Fully synchronize (add/update/delete) product classifications in one request.",
                ResponseType = typeof(ApiResponse<List<OptionTypeModule.Models.SelectItem>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Get => new()
            {
                Name = "Admin.Catalog.Product.OptionTypes.Get",
                Summary = "Get product classifications",
                Description = "Retrieves all taxon classifications assigned to a product.",
                ResponseType = typeof(ApiResponse<List<OptionTypeModule.Models.SelectItem>>),
                StatusCode = StatusCodes.Status200OK
            };
        }

        public static class Properties
        {
            public static ApiEndpointMeta Manage => new()
            {
                Name = "Admin.Catalog.Product.Properties.Manage",
                Summary = "Update product images",
                Description = "Fully synchronize (add/update/delete) product properties in one request.",
                ResponseType = typeof(ApiResponse<List<TaxonModule.Models.SelectItem>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Get => new()
            {
                Name = "Admin.Catalog.Product.Properties.Get",
                Summary = "Get product properties",
                Description = "Retrieves all properties assigned to a product.",
                ResponseType = typeof(ApiResponse<List<PropertyTypeModule.Get.SelectList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Add => new()
            {
                Name = "Admin.Catalog.Product.Properties.Add",
                Summary = "Set product properties",
                Description = "Add product property.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Edit => new()
            {
                Name = "Admin.Catalog.Product.Properties.Edit",
                Summary = "Edit product properties",
                Description = "Update product property.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };


            public static ApiEndpointMeta Remove => new()
            {
                Name = "Admin.Catalog.Product.Properties.Remove",
                Summary = "Remove product property",
                Description = "Removes a property from a product.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };
        }

    }
}