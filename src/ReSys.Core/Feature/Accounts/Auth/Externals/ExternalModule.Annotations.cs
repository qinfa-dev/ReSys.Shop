using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.Responses;


namespace ReSys.Core.Feature.Accounts.Auth.Externals;

public static partial class ExternalModule
{
    public static class Annotations
    {
        // ---------------- Group Metadata ----------------
        public static readonly ApiGroupMeta Group = new()
        {
            Name = "Account.Authentication.External",
            Tags = ["External"],
            Summary = "External Authentication Management API",
            Description = "Endpoints for managing external authentication flows."
        };

        // ---------------- Endpoint Metadata ----------------
        public static readonly ApiEndpointMeta GetOAuthConfig = new()
        {
            Name = ExternalModule.GetOAuthConfig.Name,
            Summary = ExternalModule.GetOAuthConfig.Summary,
            Description = ExternalModule.GetOAuthConfig.Description,
            ResponseType = typeof(ApiResponse<ExternalModule.GetOAuthConfig.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static readonly ApiEndpointMeta GetExternalProviders = new()
        {
            Name = ExternalModule.GetExternalProviders.Name,
            Summary = ExternalModule.GetExternalProviders.Summary,
            Description = ExternalModule.GetExternalProviders.Description,
            ResponseType = typeof(ApiResponse<List<ExternalModule.GetExternalProviders.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static readonly ApiEndpointMeta ExchangeToken = new()
        {
            Name = ExternalModule.ExchangeToken.Name,
            Summary = ExternalModule.ExchangeToken.Summary,
            Description = ExternalModule.ExchangeToken.Description,
            ResponseType = typeof(ApiResponse<ExternalModule.ExchangeToken.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static readonly ApiEndpointMeta VerifyExternalToken = new()
        {
            Name = ExternalModule.VerifyExternalToken.Name,
            Summary = ExternalModule.VerifyExternalToken.Summary,
            Description = ExternalModule.VerifyExternalToken.Description,
            ResponseType = typeof(ApiResponse<ExternalModule.VerifyExternalToken.Result>),
            StatusCode = StatusCodes.Status200OK
        };
    }
}