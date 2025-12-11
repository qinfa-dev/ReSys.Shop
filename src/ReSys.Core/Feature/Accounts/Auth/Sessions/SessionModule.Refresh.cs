using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using ReSys.Core.Domain.Identity.Tokens;
using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Persistence.Interfaces;
using ReSys.Core.Feature.Common.Security.Authentication.Tokens.Interfaces;
using ReSys.Core.Feature.Common.Security.Authentication.Tokens.Models;

namespace ReSys.Core.Feature.Accounts.Auth.Sessions;

public static partial class SessionModule
{
    public static class Refresh
    {
        public sealed record Param(string RefreshToken, bool RememberMe = false);
        public sealed record Result : AuthenticationResult;

        public sealed record Command(Param Param) : ICommand<Result>;

        public sealed class ParamValidator : AbstractValidator<Param>
        {
            public ParamValidator()
            {
                RuleFor(expression: x => x.RefreshToken)
                    .Required(prefix: nameof(Refresh),
                        field: nameof(Param.RefreshToken));
            }
        }

        public sealed class CommandHandler(
            UserManager<User> userManager,
            IHttpContextAccessor httpContext,
            IUnitOfWork unitOfWork,
            IRefreshTokenService refreshTokenService,
            IJwtTokenService jwtTokenService,
            ILogger<CommandHandler> logger) : ICommandHandler<Command, Result>
        {
            private readonly IHttpContextAccessor _httpContext = httpContext ?? throw new ArgumentNullException(paramName: nameof(httpContext));
            private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(paramName: nameof(unitOfWork));
            private readonly IRefreshTokenService _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(paramName: nameof(refreshTokenService));
            private readonly ILogger<CommandHandler> _logger = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
            {
                string ipAddress = _httpContext.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                string refreshToken = request.Param.RefreshToken;
                bool rememberMe = request.Param.RememberMe;

                try
                {
                    ErrorOr<TokenResult> rotationResult = await _refreshTokenService.RotateRefreshTokenAsync(
                        rawCurrentToken: refreshToken,
                        ipAddress: ipAddress,
                        rememberMe: rememberMe,
                        cancellationToken: cancellationToken);
                    if (rotationResult.IsError)
                    {
                        _logger.LogWarning(message: "Token rotation failed from IP {IpAddress}: {Error}",
                            args:
                            [
                                ipAddress,
                                rotationResult.Errors.First()
                                    .Code
                            ]);
                        return rotationResult.Errors;
                    }

                    ErrorOr<RefreshTokenValidationResult> validationResult =
                        await _refreshTokenService.ValidateRefreshTokenAsync(token: rotationResult.Value.Token,
                            cancellationToken: cancellationToken);

                    if (validationResult.IsError)
                    {
                        // This shouldn't happen since we just created the token, but handle it
                        _logger.LogError(message: "Newly rotated token validation failed from IP {IpAddress}",
                            args: ipAddress);
                        return validationResult.Errors;
                    }

                    User user = validationResult.Value.User;

                    ErrorOr<bool> securityValidation = await ValidateUserSecurityAsync(user: user);
                    if (securityValidation.IsError)
                    {
                        // Revoke the newly issued token
                        await _refreshTokenService.RevokeTokenAsync(
                            rawToken: rotationResult.Value.Token,
                            ipAddress: ipAddress,
                            reason: "User security status changed",
                            cancellationToken: cancellationToken);

                        await LogSecurityEventAsync(userId: user.Id,
                            ipAddress: ipAddress,
                            eventName: "Refresh blocked",
                            details: securityValidation.Errors.First()
                                .Description);
                        return securityValidation.Errors;
                    }

                    // Generate new access token
                    ErrorOr<TokenResult> accessResult = await jwtTokenService.GenerateAccessTokenAsync(
                        applicationUser: user,
                        cancellationToken: cancellationToken);
                    if (accessResult.IsError)
                    {
                        // Revoke the newly issued refresh token since we can't issue access token
                        await _refreshTokenService.RevokeTokenAsync(
                            rawToken: rotationResult.Value.Token,
                            ipAddress: ipAddress,
                            reason: "Access token generation failed",
                            cancellationToken: cancellationToken);
                        return accessResult.Errors;
                    }

                    // Update user activity in a separate transaction
                    try
                    {
                        //await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
                        user.RecordSignIn(ipAddress: ipAddress);
                        await _unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
                        //await _unitOfWork.CommitTransactionAsync(cancellationToken: cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(exception: ex,
                            message: "Failed to record sign-in activity for user {UserId}",
                            args: user.Id);
                        //try { await _unitOfWork.RollbackTransactionAsync(cancellationToken: cancellationToken); }
                        //catch
                        //{
                        //    _logger.LogError(exception: ex, message: "Token refresh failed from IP {IpAddress}", args: ipAddress);
                        //    return RefreshToken.Errors.RotationFailed;
                        //}
                    }

                    await LogSecurityEventAsync(userId: user.Id,
                        ipAddress: ipAddress,
                        eventName: "Token refresh success",
                        details: $"RememberMe: {rememberMe}");

                    return new Result
                    {
                        AccessToken = accessResult.Value.Token,
                        AccessTokenExpiresAt = accessResult.Value.ExpiresAt,
                        RefreshToken = rotationResult.Value.Token,
                        RefreshTokenExpiresAt = rotationResult.Value.ExpiresAt,
                        TokenType = "Bearer"
                    };
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(exception: ex,
                        message: "Token rotation conflict from IP {IpAddress}",
                        args: ipAddress);
                    return Error.Conflict(code: "Refresh.ConcurrentUse",
                        description: "Token is being used elsewhere. Please authenticate again.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(exception: ex,
                        message: "Token refresh failed from IP {IpAddress}",
                        args: ipAddress);
                    return RefreshToken.Errors.RotationFailed;
                }
            }

            private async Task<ErrorOr<bool>> ValidateUserSecurityAsync(User user)
            {
                try
                {
                    // Check lockout status
                    if (await userManager.IsLockedOutAsync(user: user))
                    {
                        _logger.LogWarning(message: "Authentication blocked for locked user {UserId}",
                            args: user.Id);
                        return Error.Validation(code: "Refresh.UserLocked",
                            description: "Account is locked");
                    }

                    // Check if user is enabled/active (if you have such a field)
                    // if (!user.IsActive) return Error.Validation("Refresh.UserInactive", "Account is inactive");

                    // OPTIONAL: Check email confirmation only if your business rules require it
                    // Some apps allow authenticated users even without email confirmation
                    if (!user.EmailConfirmed)
                    {
                        _logger.LogWarning(message: "Authentication blocked for unconfirmed user {UserId}",
                            args: user.Id);
                        return Error.Validation(code: "Refresh.EmailNotConfirmed",
                            description: "Email must be confirmed");
                    }

                    // ADDED: Check two-factor authentication requirements if enabled
                    if (await userManager.GetTwoFactorEnabledAsync(user: user))
                    {
                        // If 2FA is enabled, you may want additional checks here
                        // depending on your 2FA implementation
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(exception: ex,
                        message: "Security validation failed for user {UserId}",
                        args: user.Id);
                    return Error.Failure(code: "Refresh.SecurityCheckFailed",
                        description: "Security validation failed");
                }
            }

            private Task LogSecurityEventAsync(string userId, string ipAddress, string eventName, string details)
            {
                // IMPROVED: Use structured logging
                _logger.LogInformation(
                    message: "Security Event: {EventName} | User: {UserId} | IP: {IpAddress} | Details: {Details}",
                    args:
                    [
                        eventName,
                        userId,
                        ipAddress,
                        details
                    ]);

                return Task.CompletedTask;
            }
        }
    }
}