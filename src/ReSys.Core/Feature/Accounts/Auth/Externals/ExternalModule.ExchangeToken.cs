using Mapster;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Security.Authentication.Externals.Interfaces;
using ReSys.Core.Feature.Common.Security.Authentication.Externals.Models;
using ReSys.Core.Feature.Common.Security.Authentication.Tokens.Interfaces;
using ReSys.Core.Feature.Common.Security.Authentication.Tokens.Models;

namespace ReSys.Core.Feature.Accounts.Auth.Externals;

public static partial class ExternalModule
{
    public static class ExchangeToken
    {
        internal const string Name = "Account.Authentication.External.Exchange";
        internal const string Summary = "Exchange external provider token for application tokens";
        internal const string Description =
            "Accepts an external provider token (from frontend OAuth) and exchanges it for application JWT tokens using official provider SDKs for validation";

        public sealed record Param(
            string Provider,
            string? AccessToken = null,
            string? IdToken = null,
            string? AuthorizationCode = null,
            string? RedirectUri = null,
            bool RememberMe = false);

        public sealed record Command(string? Provider, Param Param) : ICommand<Result>;
        public sealed record Result : AuthenticationResult
        {
            public bool IsNewUser { get; set; }
            public bool IsNewLogin { get; set; }
            public UserProfile? UserProfile { get; set; }
        }
        public sealed record UserProfile
        {
            public string Email { get; init; } = null!;
            public string? FirstName { get; init; }
            public string? LastName { get; init; }
            public string? ProfilePictureUrl { get; init; }
            public bool EmailVerified { get; init; }
            public bool HasExternalLogins { get; init; }
            public string[] ExternalProviders { get; init; } = [];
            public Dictionary<string, string> AdditionalClaims { get; init; } = new();
        }

        public sealed class ParamValidator : AbstractValidator<Param>
        {
            public ParamValidator()
            {
                // Validate: ensure either access token, ID token, or authorization code is provided
                RuleFor(expression: x => x)
                    .Must(predicate: p =>
                        !string.IsNullOrWhiteSpace(value: p.AccessToken) ||
                        !string.IsNullOrWhiteSpace(value: p.IdToken) ||
                        !string.IsNullOrWhiteSpace(value: p.AuthorizationCode))
                    .WithErrorCode(errorCode: "Token.Required")
                    .WithMessage(errorMessage: "Either access token, ID token, or authorization code is required");
            }
        }
        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                // Validate: ensure parameter is not null
                RuleFor(expression: x => x.Param)
                    .NotNull()
                    .WithErrorCode(errorCode: "Request.Required")
                    .WithMessage(errorMessage: "Request is required");

                // Validate: use ParamValidator for nested parameter validation
                RuleFor(expression: x => x.Param)
                    .SetValidator(validator: new ParamValidator());
            }
        }

        public sealed class CommandHandler(
            UserManager<User> userManager,
            IJwtTokenService jwtTokenService,
            IRefreshTokenService refreshTokenService,
            IExternalTokenValidator tokenValidator,
            IExternalUserService externalUserService,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CommandHandler> logger
        ) : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
            {
                // Assign: command parameter to a local variable
                Param param = request.Param;
                // Generate: client IP address from HTTP context
                string ipAddress = GetClientIpAddress();

                // Catch: any exceptions during the token exchange process
                try
                {
                    // Log: debug information about token validation
                    logger.LogDebug(message: "Validating external token for provider: {Provider}",
                        args: param.Provider);
                    // Assign: ID token, falling back to access token or authorization code if not present
                    string? idToken = param.IdToken ?? param.AccessToken ?? param.AuthorizationCode;

                    // Call: external token validator service to validate the token
                    // Await: the asynchronous validation operation
                    var validationResult = await tokenValidator.ValidateTokenAsync(
                        provider: param.Provider,
                        accessToken: param.AccessToken,
                        idToken: idToken,
                        authorizationCode: param.AuthorizationCode,
                        redirectUri: param.RedirectUri,
                        cancellationToken: cancellationToken);

                    // Check: if token validation resulted in an error
                    if (validationResult.IsError)
                    {
                        // Log: warning about token validation failure
                        logger.LogWarning(message: "Token validation failed for provider {Provider}: {Errors}", args:
                        [param.Provider, string.Join(separator: ", ",
                                values: validationResult.Errors.Select(selector: e => e.Description))
                            ]);
                        // Recover: from validation error by returning the errors
                        return validationResult.Errors;
                    }

                    // Assign: external user information from the validation result
                    ExternalUserTransfer externalUserInfo = validationResult.Value;
                    // Log: debug information about successful token validation
                    logger.LogDebug(message: "Successfully validated token for user: {Email} from provider: {Provider}", args: [externalUserInfo.Email, param.Provider]);

                    // Call: external user service to find or create a user with external login
                    // Await: the asynchronous operation
                    var userResult = await externalUserService.FindOrCreateUserWithExternalLoginAsync(
                        externalUserTransfer: externalUserInfo,
                        provider: param.Provider,
                        cancellationToken: cancellationToken);

                    // Check: if finding or creating user resulted in an error
                    if (userResult.IsError)
                    {
                        // Log: error about user creation/finding failure
                        logger.LogError(message: "Failed to find or create user for provider {Provider}: {Errors}", args:
                        [param.Provider, string.Join(separator: ", ",
                                values: userResult.Errors.Select(selector: e => e.Description))
                            ]);
                        // Recover: from user creation/finding error by returning the errors
                        return userResult.Errors;
                    }

                    // Assign: user, isNewUser, and isNewLogin from the result
                    (User user, bool isNewUser, bool isNewLogin) = userResult.Value;

                    // Update: user's sign-in record
                    user.RecordSignIn(ipAddress: ipAddress);
                    // Update: user in the database
                    // Await: the asynchronous update operation
                    await userManager.UpdateAsync(user: user);

                    // Generate: access token for the user
                    // Await: the asynchronous token generation
                    var accessResult = await jwtTokenService.GenerateAccessTokenAsync(applicationUser: user,
                        cancellationToken: cancellationToken);
                    // Check: if access token generation resulted in an error
                    if (accessResult.IsError)
                    {
                        // Log: error about access token generation failure
                        logger.LogError(message: "Access token generation failed for user {UserId}",
                            args: user.Id);
                        // Recover: from access token generation error by returning the errors
                        return accessResult.Errors;
                    }

                    // Generate: refresh token for the user
                    // Await: the asynchronous token generation
                    var refreshResult = await refreshTokenService.GenerateRefreshTokenAsync(
                        userId: user.Id,
                        ipAddress: ipAddress,
                        rememberMe: param.RememberMe,
                        cancellationToken: cancellationToken);
                    // Check: if refresh token generation resulted in an error
                    if (refreshResult.IsError)
                    {
                        // Log: error about refresh token generation failure
                        logger.LogError(message: "Refresh token generation failed for user {UserId}",
                            args: user.Id);
                        // Recover: from refresh token generation error by returning the errors
                        return refreshResult.Errors;
                    }

                    // Create: a new AuthenticationResult object
                    AuthenticationResult tokens = new()
                    {
                        // Assign: access token and its expiration
                        AccessToken = accessResult.Value.Token,
                        AccessTokenExpiresAt = accessResult.Value.ExpiresAt,
                        // Assign: refresh token and its expiration
                        RefreshToken = refreshResult.Value.Token,
                        RefreshTokenExpiresAt = refreshResult.Value.ExpiresAt,
                        // Assign: token type
                        TokenType = "Bearer"
                    };

                    // Generate: user profile information
                    // Await: the asynchronous profile building
                    UserProfile userProfile = await BuildUserProfileAsync(user: user,
                        externalUserInfo: externalUserInfo,
                        cancellationToken: cancellationToken);

                    // Transform: AuthenticationResult to the specific Result type
                    Result result = tokens.Adapt<Result>();
                    // Assign: new user status
                    result.IsNewUser = isNewUser;
                    // Assign: new login status
                    result.IsNewLogin = isNewLogin;
                    // Assign: user profile
                    result.UserProfile = userProfile;

                    // Log: information about successful token exchange
                    logger.LogInformation(
                        message: "External token exchange successful for user {UserId} via {Provider}. NewUser: {IsNewUser}, NewLogin: {IsNewLogin}", args: [user.Id, param.Provider, isNewUser, isNewLogin]);

                    // Return: the successful result
                    return result;
                }
                // Catch: OperationCanceledException for cancellation scenarios
                catch (OperationCanceledException)
                {
                    // Log: warning about cancelled operation
                    logger.LogWarning(message: "External token exchange was cancelled for provider: {Provider}",
                        args: param.Provider);
                    // Recover: from cancellation by returning a specific error
                    return Error.Failure(code: "TokenExchange.Cancelled",
                        description: "Token exchange operation was cancelled");
                }
                // Catch: any other unexpected exceptions
                catch (Exception ex)
                {
                    // Log: error about unexpected exception
                    logger.LogError(exception: ex,
                        message: "Unexpected error during external token exchange for provider: {Provider}",
                        args: param.Provider);
                    // Recover: from unexpected error by returning a generic error
                    return Error.Failure(code: "TokenExchange.UnexpectedError",
                        description: "An unexpected error occurred during token exchange");
                }
            }

            // Generate: user profile based on application user and external user info
            private async Task<UserProfile> BuildUserProfileAsync(
                User user,
                ExternalUserTransfer externalUserInfo,
                CancellationToken cancellationToken)
            {
                // Catch: any exceptions during user profile building
                try
                {
                    // Call: external user service to get external logins
                    // Await: the asynchronous operation
                    var externalLogins = await externalUserService.GetExternalLoginsAsync(
                        userId: user.Id,
                        cancellationToken: cancellationToken);

                    // Transform: external logins to an array of provider names
                    string[] externalProviders = externalLogins
                        .Select(selector: l => l.LoginProvider.ToLowerInvariant())
                        .ToArray();

                    // Create: a new AccountProfile object
                    return new UserProfile
                    {
                        // Assign: user's email
                        Email = user.Email!,
                        // Assign: user's first name
                        FirstName = user.FirstName,
                        // Assign: user's last name
                        LastName = user.LastName,
                        // Assign: email verification status
                        EmailVerified = user.EmailConfirmed,
                        // Assign: profile picture URL from external info
                        ProfilePictureUrl = externalUserInfo.ProfilePictureUrl,
                        // Assign: whether the user has external logins
                        HasExternalLogins = externalLogins.Count > 0,
                        // Assign: array of external providers
                        ExternalProviders = externalProviders,
                        // Create: and Merge: additional claims into a dictionary
                        AdditionalClaims = new Dictionary<string, string>(collection: externalUserInfo.AdditionalClaims)
                        {
                            // Assign: user ID to additional claims
                            [key: "user_id"] = user.Id,
                            // Assign: username to additional claims
                            [key: "username"] = user.UserName ?? "",
                            // Assign: sign-in count to additional claims
                            [key: "sign_in_count"] = user.SignInCount.ToString(),
                            // Assign: last sign-in date to additional claims
                            [key: "last_sign_in"] = user.LastSignInAt?.ToString(format: "O") ?? "",
                            // Assign: external logins count to additional claims
                            [key: "external_logins_count"] = externalLogins.Count.ToString()
                        }
                    };
                }
                // Catch: any exceptions during profile building
                catch (Exception ex)
                {
                    // Log: warning about error during profile building
                    logger.LogWarning(exception: ex,
                        message: "Error building user profile for user {UserId}, using basic profile",
                        args: user.Id);

                    // Create: a basic AccountProfile object as a degraded fallback
                    return new UserProfile
                    {
                        // Assign: user's email
                        Email = user.Email!,
                        // Assign: user's first name
                        FirstName = user.FirstName,
                        // Assign: user's last name
                        LastName = user.LastName,
                        // Assign: email verification status
                        EmailVerified = user.EmailConfirmed,
                        // Assign: profile picture URL from external info
                        ProfilePictureUrl = externalUserInfo.ProfilePictureUrl,
                        // Assign: has external logins (assuming true if an error occurred here)
                        HasExternalLogins = true,
                        // Assign: empty array for external providers
                        ExternalProviders = [],
                        // Create: additional claims with error information
                        AdditionalClaims = new()
                        {
                            // Assign: user ID to additional claims
                            [key: "user_id"] = user.Id,
                            // Assign: error message to additional claims
                            [key: "error"] = "profile_build_error"
                        }
                    };
                }
            }

            // Acquire: client IP address from the HTTP context
            private string GetClientIpAddress()
            {
                // Acquire: current HTTP context
                var context = httpContextAccessor.HttpContext;
                // Check: if context is null, then Fallback: to "unknown"
                if (context == null) return "unknown";

                // Acquire: "X-Forwarded-For" header
                string? forwardedFor = context.Request.Headers[key: "X-Forwarded-For"].FirstOrDefault();
                // Check: if forwardedFor is not empty, then Transform: and return the first IP
                if (!string.IsNullOrWhiteSpace(value: forwardedFor))
                    return forwardedFor.Split(separator: ',')[0].Trim();

                // Acquire: "X-Real-IP" header
                string? realIp = context.Request.Headers[key: "X-Real-IP"].FirstOrDefault();
                // Check: if realIp is not empty, then Transform: and return it
                if (!string.IsNullOrWhiteSpace(value: realIp))
                    return realIp.Trim();

                // Acquire: remote IP address from connection
                string? remoteIp = context.Connection.RemoteIpAddress?.ToString();
                // Check: if remoteIp is not empty, then return it, else Fallback: to "unknown"
                return !string.IsNullOrWhiteSpace(value: remoteIp) ? remoteIp : "unknown";
            }
        }
    }
}