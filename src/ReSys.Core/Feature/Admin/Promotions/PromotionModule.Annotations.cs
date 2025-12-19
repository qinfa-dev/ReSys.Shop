using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Admin.Promotions;

public static partial class PromotionModule
{
    private static class Annotations
    {
        public static ApiGroupMeta Group => new()
        {
            Name = "Admin.Promotion",
            Tags = ["Promotion Management"],
            Summary = "Promotion Management API",
            Description = "Endpoints for managing promotions and discount rules"
        };

        public static ApiEndpointMeta Create => new()
        {
            Name = "Admin.Promotion.Create",
            Summary = "Create a new promotion",
            Description = "Creates a new promotion with the specified details.",
            ResponseType = typeof(ApiResponse<Create.Result>),
            StatusCode = StatusCodes.Status201Created
        };

        public static ApiEndpointMeta Update => new()
        {
            Name = "Admin.Promotion.Update",
            Summary = "Update a promotion",
            Description = "Updates an existing promotion by ID.",
            ResponseType = typeof(ApiResponse<Update.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Delete => new()
        {
            Name = "Admin.Promotion.Delete",
            Summary = "Delete a promotion",
            Description = "Deletes a promotion by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetById => new()
        {
            Name = "Admin.Promotion.GetById",
            Summary = "Get promotion details",
            Description = "Retrieves details of a specific promotion by ID.",
            ResponseType = typeof(ApiResponse<Get.ById.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetPagedList => new()
        {
            Name = "Admin.Promotion.Get.Paged",
            Summary = "Get paged list of promotions",
            Description = "Retrieves a paginated list of promotions.",
            ResponseType = typeof(ApiResponse<PagedList<Get.PagedList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetSelectList => new()
        {
            Name = "Admin.Promotion.Get.Select",
            Summary = "Get selectable list of promotions",
            Description = "Retrieves a simplified list of promotions for selection purposes.",
            ResponseType = typeof(ApiResponse<PagedList<Get.SelectList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Activate => new()
        {
            Name = "Admin.Promotion.Activate",
            Summary = "Activate a promotion",
            Description = "Activates an inactive promotion.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Deactivate => new()
        {
            Name = "Admin.Promotion.Deactivate",
            Summary = "Deactivate a promotion",
            Description = "Deactivates an active promotion.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Validate => new()
        {
            Name = "Admin.Promotion.Validate",
            Summary = "Validate promotion",
            Description = "Validates promotion configuration and rules.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        // Rules Management
        public static class Rules
        {
            public static ApiEndpointMeta Get => new()
            {
                Name = "Admin.Promotion.Rules.Get",
                Summary = "Get promotion rules",
                Description = "Retrieves the rules for a promotion.",
                ResponseType = typeof(ApiResponse<PagedList<PromotionModule.Models.RuleItem>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Add => new()
            {
                Name = "Admin.Promotion.Rules.Add",
                Summary = "Add promotion rule",
                Description = "Adds a single rule to a promotion.",
                ResponseType = typeof(ApiResponse<PromotionModule.Rules.Add.Result>),
                StatusCode = StatusCodes.Status201Created
            };

            public static ApiEndpointMeta Update => new()
            {
  
                Name = "Admin.Promotion.Rules.Update",
                Summary = "Update promotion rule",
                Description = "Updates a specific rule for a promotion.",
                ResponseType = typeof(ApiResponse<PromotionModule.Rules.Update.Result>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Delete => new()
            {
                Name = "Admin.Promotion.Rules.Delete",
                Summary = "Delete promotion rule",
                Description = "Deletes a specific rule from a promotion.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static class Taxons
            {
                // Rule Taxons Management
                public static ApiEndpointMeta GetTaxons => new()
                {
                    Name = "Admin.Promotion.Rules.Taxons.Get",
                    Summary = "Get taxons for a promotion rule",
                    Description = "Retrieves the taxons associated with a specific promotion rule.",
                    ResponseType = typeof(ApiResponse<PagedList<Models.PromotionTaxonRuleItem>>),
                    StatusCode = StatusCodes.Status200OK
                };

                public static ApiEndpointMeta Manage => new()
                {
                    Name = "Admin.Promotion.Rules.Taxons.Manage",
                    Summary = "Synchronize taxons for a rule",
                    Description = "Reconciles the taxons associated with a promotion rule, adding new, removing old, and keeping existing.",
                    ResponseType = typeof(ApiResponse<Success>),
                    StatusCode = StatusCodes.Status200OK
                };
            }
            public static class Users
            {
                // Rule Users Management
                public static ApiEndpointMeta GetUsers => new()
                {
                    Name = "Admin.Promotion.Rules.Users.Get",
                    Summary = "Get users for a promotion rule",
                    Description = "Retrieves the users associated with a specific promotion rule.",
                    ResponseType = typeof(ApiResponse<PagedList<Models.PromotionUsersRuleItem>>),
                    StatusCode = StatusCodes.Status200OK
                };

                public static ApiEndpointMeta ManageUsers => new()
                {
                    Name = "Admin.Promotion.Rules.Users.Manage",
                    Summary = "Synchronize users for a rule",
                    Description = "Reconciles the users associated with a promotion rule, adding new, removing old, and keeping existing.",
                    ResponseType = typeof(ApiResponse<Success>),
                    StatusCode = StatusCodes.Status200OK
                };
            }
        }
        public static class Analytics
        {
            // Analytics & Testing
            public static ApiEndpointMeta GetStats => new()
            {
                Name = "Admin.Promotion.Stats",
                Summary = "Get promotion statistics",
                Description = "Retrieves usage statistics and performance metrics for a promotion.",
                ResponseType = typeof(ApiResponse<PromotionModule.Analytics.Stats.Get.Result>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Preview => new()
            {
                Name = "Admin.Promotion.Preview",
                Summary = "Preview promotion calculation",
                Description = "Tests promotion against a sample order without applying it.",
                ResponseType = typeof(ApiResponse<PromotionModule.Analytics.Preview.Result>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta GetHistory => new()
            {
                Name = "Admin.Promotion.History",
                Summary = "Get promotion audit history",
                Description = "Retrieves the change history and audit trail for a promotion.",
                ResponseType = typeof(ApiResponse<PagedList<PromotionModule.Analytics.History.Get.Result>>),
                StatusCode = StatusCodes.Status200OK
            };
        }
    }
}