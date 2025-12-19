// Added this import
using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
{
    private static class Annotations
    {
        public static ApiGroupMeta Group => new()
        {
            Name = "Admin.Store",
            Tags = ["Store Management"],
            Summary = "Store Management API",
            Description = "Endpoints for managing store entities."
        };

        public static ApiEndpointMeta Create => new()
        {
            Name = "Admin.Store.Create",
            Summary = "Create a new store",
            Description = "Creates a new store with the specified details.",
            ResponseType = typeof(ApiResponse<Create.Result>),
            StatusCode = StatusCodes.Status201Created
        };

        public static ApiEndpointMeta Update => new()
        {
            Name = "Admin.Store.Update",
            Summary = "Update store details",
            Description = "Updates an existing store by ID.",
            ResponseType = typeof(ApiResponse<Update.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta SetAddress => new()
        {
            Name = "Admin.Store.SetAddress",
            Summary = "Set store address",
            Description = "Sets the address details for an existing store.",
            ResponseType = typeof(ApiResponse<SetAddress.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta SetSocialLinks => new()
        {
            Name = "Admin.Store.SetSocialLinks",
            Summary = "Set store social links",
            Description = "Sets social media links for an existing store.",
            ResponseType = typeof(ApiResponse<SetSocialLinks.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta MakeDefault => new()
        {
            Name = "Admin.Store.MakeDefault",
            Summary = "Make store default",
            Description = "Sets a store as the default store for the system.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta ProtectWithPassword => new()
        {
            Name = "Admin.Store.ProtectWithPassword",
            Summary = "Protect store with password",
            Description = "Protects a store with a storefront password.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta RemovePasswordProtection => new()
        {
            Name = "Admin.Store.RemovePasswordProtection",
            Summary = "Remove store password protection",
            Description = "Removes password protection from a store.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };


        public static ApiEndpointMeta Delete => new()
        {
            Name = "Admin.Store.Delete",
            Summary = "Delete a store",
            Description = "Soft deletes a store by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Restore => new()
        {
            Name = "Admin.Store.Restore",
            Summary = "Restore a store",
            Description = "Restores a soft-deleted store by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetById => new()
        {
            Name = "Admin.Store.Get.ById",
            Summary = "Get store details",
            Description = "Retrieves details of a specific store by ID.",
            ResponseType = typeof(ApiResponse<Get.ById.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetPagedList => new()
        {
            Name = "Admin.Store.Get.PagedList",
            Summary = "Get paged list of stores",
            Description = "Retrieves a paginated list of stores.",
            ResponseType = typeof(ApiResponse<PagedList<Get.PagedList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static class PaymentMethods
        {
            public static ApiEndpointMeta Manage => new()
            {
                Name = "Admin.Stores.PaymentMethod.Manage",
                Summary = "Sync store payment methods",
                Description = "Replaces current payment methods with the provided list and returns the final applied state.",
                ResponseType = typeof(ApiResponse<IReadOnlyList<Models.ListItem>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta GetPagedList => new()
            {
                Name = "Admin.Stores.PaymentMethod.GetPagedList",
                Summary = "Get paged list of store payment methods",
                Description = "Retrieves a paginated list of payment methods linked to a store.",
                ResponseType = typeof(ApiResponse<PagedList<Models.ListItem>>),
                StatusCode = StatusCodes.Status200OK
            };
        }

        public static class ShippingMethods
        {
            public static ApiEndpointMeta Manage => new()
            {
                Name = "Admin.Stores.ShippingMethods.Manage",
                Summary = "Sync store shipping methods",
                Description =
                    "Replaces current shipping methods with the provided list and returns the final applied state.",
                ResponseType = typeof(ApiResponse<IReadOnlyList<Models.ListItem>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta GetPagedList => new()
            {
                Name = "Admin.Stores.ShippingMethods.GetPagedList",
                Summary = "Get paged list of store shipping methods",
                Description = "Retrieves a paginated list of shipping methods linked to a store.",
                ResponseType = typeof(ApiResponse<PagedList<Models.ListItem>>),
                StatusCode = StatusCodes.Status200OK
            };
        }

        public static class Products
        {
            public static readonly ApiEndpointMeta GetPagedList = new()
            {
                Name = "Admin.Stores.Products.GetPagedList",
                Summary = "Get paged list of store products",
                Description = "Retrieves a paginated list of products associated with stores, with optional filtering.",
                ResponseType = typeof(ApiResponse<PagedList<StoreModule.Get.PagedList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static readonly ApiEndpointMeta Add = new()
            {
                Name = "Admin.Stores.Products.Add",
                Summary = "Add product to store",
                Description = "Adds a single product to a specific store.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static readonly ApiEndpointMeta Update = new()
            {
                Name = "Admin.Stores.Products.Update",
                Summary = "Update product in store",
                Description = "Updates the settings (visibility, featured status) of a single product within a specific store.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static readonly ApiEndpointMeta Remove = new()
            {
                Name = "Admin.Stores.Products.Remove",
                Summary = "Remove product from store",
                Description = "Removes a single product from a specific store.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static readonly ApiEndpointMeta GetById = new()
            {
                Name = "Admin.Stores.Products.GetById",
                Summary = "Get store product details",
                Description = "Retrieves details of a specific product within a store by its StoreId and ProductId.",
                ResponseType = typeof(ApiResponse<StoreModule.Get.ById.Result>),
                StatusCode = StatusCodes.Status200OK
            };
        }
    }
}