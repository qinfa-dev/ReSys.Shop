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

        #region CRUD

        public static ApiEndpointMeta Create => new()
        {
            Name = "Admin.Catalog.Product.Create",
            Summary = "Create product",
            Description = "Creates a new catalog product.",
            ResponseType = typeof(ApiResponse<Create.Result>),
            StatusCode = StatusCodes.Status201Created
        };

        public static ApiEndpointMeta Update => new()
        {
            Name = "Admin.Catalog.Product.Update",
            Summary = "Update product",
            Description = "Updates an existing catalog product by ID.",
            ResponseType = typeof(ApiResponse<Update.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Delete => new()
        {
            Name = "Admin.Catalog.Product.Delete",
            Summary = "Delete product",
            Description = "Deletes a catalog product by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        #endregion

        #region Get

        public static class Get
        {
            public static ApiEndpointMeta ById => new()
            {
                Name = "Admin.Catalog.Product.GetById",
                Summary = "Get product details",
                Description = "Retrieves details of a specific product by ID.",
                ResponseType = typeof(ApiResponse<ProductModule.Get.ById.Result>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta PagedList => new()
            {
                Name = "Admin.Catalog.Product.GetPagedList",
                Summary = "Get paged products",
                Description = "Retrieves a paginated list of products.",
                ResponseType = typeof(ApiResponse<PagedList<ProductModule.Get.PagedList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta SelectList => new()
            {
                Name = "Admin.Catalog.Product.GetSelectList",
                Summary = "Get product select list",
                Description = "Retrieves a simplified selectable list of products.",
                ResponseType = typeof(ApiResponse<PagedList<ProductModule.Get.SelectList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };
        }

        #endregion

        #region Status

        public static class Status
        {
            public static ApiEndpointMeta Activate => new()
            {
                Name = "Admin.Catalog.Product.Activate",
                Summary = "Activate product",
                Description = "Activates a product.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Archive => new()
            {
                Name = "Admin.Catalog.Product.Archive",
                Summary = "Archive product",
                Description = "Archives a product.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Draft => new()
            {
                Name = "Admin.Catalog.Product.Draft",
                Summary = "Set product to draft",
                Description = "Sets a product to draft status.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Discontinue => new()
            {
                Name = "Admin.Catalog.Product.Discontinue",
                Summary = "Discontinue product",
                Description = "Discontinues a product.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };
        }

        #endregion

        #region Images

        public static class Images
        {
            public static ApiEndpointMeta Manage => new()
            {
                Name = "Admin.Catalog.Product.Images.Manage",
                Summary = "Manage product images",
                Description = "Fully synchronizes product images (add/update/remove).",
                ResponseType = typeof(ApiResponse<List<ProductModule.Images.Manage.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Upload => new()
            {
                Name = "Admin.Catalog.Product.Images.Upload",
                Summary = "Upload product image",
                Description = "Uploads a new product image.",
                ResponseType = typeof(ApiResponse<List<ProductModule.Images.Upload.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Edit => new()
            {
                Name = "Admin.Catalog.Product.Images.Edit",
                Summary = "Edit product image",
                Description = "Edits an existing product image.",
                ResponseType = typeof(ApiResponse<List<ProductModule.Images.Edit.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Remove => new()
            {
                Name = "Admin.Catalog.Product.Images.Remove",
                Summary = "Remove product image",
                Description = "Removes a product image.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Get => new()
            {
                Name = "Admin.Catalog.Product.Images.Get",
                Summary = "Get product images",
                Description = "Retrieves all images for a product.",
                ResponseType = typeof(ApiResponse<List<ProductModule.Images.GetList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };
        }

        #endregion

        #region Classifications

        public static class Classifications
        {
            public static ApiEndpointMeta Manage => new()
            {
                Name = "Admin.Catalog.Product.Classifications.Manage",
                Summary = "Manage product classifications",
                Description = "Fully synchronizes product taxon classifications (add/update/remove).",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Get => new()
            {
                Name = "Admin.Catalog.Product.Classifications.Get",
                Summary = "Get product classifications",
                Description = "Retrieves all taxon classifications assigned to a product.",
                ResponseType = typeof(ApiResponse<List<ProductModule.Classifications.Get.SelectList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };
        }

        #endregion

        #region OptionTypes

        public static class OptionTypes
        {
            public static ApiEndpointMeta Manage => new()
            {
                Name = "Admin.Catalog.Product.OptionTypes.Manage",
                Summary = "Manage product option types",
                Description = "Fully synchronizes product option types (add/update/remove).",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Get => new()
            {
                Name = "Admin.Catalog.Product.OptionTypes.Get",
                Summary = "Get product option types",
                Description = "Retrieves all option types assigned to a product.",
                ResponseType = typeof(ApiResponse<List<ProductModule.OptionTypes.Get.SelectList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };
        }

        #endregion

        #region Properties

        public static class Properties
        {
            public static ApiEndpointMeta Manage => new()
            {
                Name = "Admin.Catalog.Product.Properties.Manage",
                Summary = "Manage product properties",
                Description = "Fully synchronizes product properties (add/update/remove).",
                ResponseType = typeof(ApiResponse<List<ProductModule.PropertyType.Manage.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Get => new()
            {
                Name = "Admin.Catalog.Product.Properties.Get",
                Summary = "Get product properties",
                Description = "Retrieves all properties assigned to a product.",
                ResponseType = typeof(ApiResponse<List<ProductModule.PropertyType.Get.SelectList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };
        }

        #endregion
    }
}
