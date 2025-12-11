using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Extensions;
using ReSys.Core.Feature.Common.Security.Authentication.Tokens.Interfaces;

namespace ReSys.Core.Feature.Accounts.Auth.Sessions;

public static partial class LogOutModule
{
    public static class Single
    {
        public sealed record Param(string? RefreshToken);
        public sealed record Command(Param Param) : ICommand<Deleted>;

        public sealed class CommandHandler(
            IRefreshTokenService refreshTokenService,
            IHttpContextAccessor accessor,
            ILogger<CommandHandler> logger)
            : ICommandHandler<Command, Deleted>
        {
            private readonly IRefreshTokenService _refreshTokenService = refreshTokenService ?? throw new ArgumentNullException(paramName: nameof(refreshTokenService));
            private readonly IHttpContextAccessor _accessor = accessor ?? throw new ArgumentNullException(paramName: nameof(accessor));
            private readonly ILogger<CommandHandler> _logger = logger ?? throw new ArgumentNullException(paramName: nameof(logger));

            public async Task<ErrorOr<Deleted>> Handle(Command request, CancellationToken cancellationToken)
            {
                HttpContext? httpContext = _accessor.HttpContext;
                string ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                try
                {
                    string? refreshToken = request.Param.RefreshToken;
                    string? principalUserId = httpContext?.User.GetUserId();

                    if (!string.IsNullOrWhiteSpace(value: refreshToken))
                    {
                        ErrorOr<Success> revokeResult = await _refreshTokenService.RevokeTokenAsync(
                            rawToken: refreshToken,
                            ipAddress: ipAddress,
                            reason: "User logout",
                            cancellationToken: cancellationToken);

                        if (revokeResult.IsError)
                        {
                            _logger.LogWarning(
                                message: "Failed to revoke refresh token during logout from IP {IpAddress}: {Errors}",
                                args:
                                [
                                    ipAddress,
                                    string.Join(separator: ", ",
                                        values: revokeResult.Errors.Select(selector: e => e.Code))
                                ]);

                            if (revokeResult.Errors.Any(predicate: e => e.Type != ErrorType.NotFound && e.Type != ErrorType.Validation))
                            {
                                return revokeResult.Errors;
                            }
                        }
                        else
                        {
                            _logger.LogInformation(message: "Refresh token revoked during logout from IP {IpAddress}",
                                args: ipAddress);
                        }

                        if (!string.IsNullOrWhiteSpace(value: principalUserId))
                        {
                            _logger.LogInformation(message: "User {UserId} logged out from IP {IpAddress}",
                                args:
                                [
                                    principalUserId,
                                    ipAddress
                                ]);
                        }

                        return Result.Deleted;
                    }

                    if (!string.IsNullOrWhiteSpace(value: principalUserId))
                    {
                        ErrorOr<int> revokeAllResult = await _refreshTokenService.RevokeAllUserTokensAsync(
                            userId: principalUserId,
                            ipAddress: ipAddress,
                            reason: "User logout - all sessions",
                            exceptToken: null,
                            cancellationToken: cancellationToken);

                        if (revokeAllResult.IsError)
                        {
                            _logger.LogWarning(
                                message: "Failed to revoke all tokens for user {UserId} during logout from IP {IpAddress}: {Errors}",
                                args:
                                [
                                    principalUserId,
                                    ipAddress,
                                    string.Join(separator: ", ",
                                        values: revokeAllResult.Errors.Select(selector: e => e.Code))
                                ]);

                            if (revokeAllResult.Errors.Any(predicate: e => e.Type == ErrorType.Failure))
                            {
                                return revokeAllResult.Errors;
                            }
                        }
                        else
                        {
                            _logger.LogInformation(
                                message: "Revoked {Count} tokens for user {UserId} during logout from IP {IpAddress}",
                                args:
                                [
                                    revokeAllResult.Value,
                                    principalUserId,
                                    ipAddress
                                ]);
                        }

                        return Result.Deleted;
                    }

                    _logger.LogInformation(
                        message: "Logout requested with no refresh token and no authenticated user from IP {IpAddress}",
                        args: ipAddress);
                    return Result.Deleted;
                }
                catch (Exception ex)
                {
                    _logger.LogError(exception: ex,
                        message: "Logout failed from IP {IpAddress}",
                        args: ipAddress);

                    _logger.LogWarning(
                        message: "Logout completed with errors from IP {IpAddress}, returning success for UX",
                        args: ipAddress);
                    return Result.Deleted;
                }
            }
        }
    }
}