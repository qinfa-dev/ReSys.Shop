using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Admin.Settings.PaymentMethods;

public static partial class PaymentMethodModule
{
    private static class Annotations
    {
        public static ApiGroupMeta Group => new()
        {
            Name = "Admin.Setting.PaymentMethod",
            Tags = ["Payment Method Management"],
            Summary = "Payment Method Management API",
            Description = "Endpoints for managing global payment methods."
        };

        public static ApiEndpointMeta Create => new()
        {
            Name = "Admin.Setting.PaymentMethod.Create",
            Summary = "Create a new payment method",
            Description = "Creates a new global payment method.",
            ResponseType = typeof(ApiResponse<Create.Result>),
            StatusCode = StatusCodes.Status201Created
        };

        public static ApiEndpointMeta Update => new()
        {
            Name = "Admin.Setting.PaymentMethod.Update",
            Summary = "Update a payment method",
            Description = "Updates an existing global payment method by ID.",
            ResponseType = typeof(ApiResponse<Update.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Delete => new()
        {
            Name = "Admin.Setting.PaymentMethod.Delete",
            Summary = "Delete a payment method",
            Description = "Soft deletes a global payment method by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Restore => new()
        {
            Name = "Admin.Setting.PaymentMethod.Restore",
            Summary = "Restore a payment method",
            Description = "Restores a soft-deleted payment method by ID.",
            ResponseType = typeof(ApiResponse<Restore.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetById => new()
        {
            Name = "Admin.Setting.PaymentMethod.GetById",
            Summary = "Get payment method details",
            Description = "Retrieves details of a specific global payment method by ID.",
            ResponseType = typeof(ApiResponse<Get.ById.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetPagedList => new()
        {
            Name = "Admin.Setting.PaymentMethod.GetPagedList",
            Summary = "Get paged list of payment methods",
            Description = "Retrieves a paginated list of global payment methods.",
            ResponseType = typeof(ApiResponse<PagedList<Get.PagedList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetSelectList => new()
        {
            Name = "Admin.Setting.PaymentMethod.GetSelectList",
            Summary = "Get selectable list of payment methods",
            Description = "Retrieves a simplified list of global payment methods for selection purposes.",
            ResponseType = typeof(ApiResponse<PagedList<Get.SelectList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };
    }
}