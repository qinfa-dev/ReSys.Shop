using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Extensions;
using ReSys.Core.Feature.Common.Security.Authentication.Tokens.Interfaces;

namespace ReSys.Core.Feature.Accounts.Auth.Sessions;

public static partial class LogOutModule
{
    public static class FromAll
    {
        public sealed record Param(string? RefreshToken);
        public sealed record Command(Param Param) : ICommand<Deleted>;

        public sealed class CommandHandler(
            IRefreshTokenService refreshTokenService,
            IHttpContextAccessor accessor,
            ILogger<CommandHandler> logger)
            : ICommandHandler<Command, Deleted>
        {
            public async Task<ErrorOr<Deleted>> Handle(Command request, CancellationToken cancellationToken)
            {
                var httpContext = accessor.HttpContext;
                var userId = httpContext?.User.GetUserId();
                var isAuthenticated = httpContext?.User.IsAuthenticated() ?? false;
                var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                if (string.IsNullOrWhiteSpace(value: userId) || !isAuthenticated)
                {
                    logger.LogWarning(message: "Unauthorized logout-all attempt from IP {IpAddress}",
                        args: ipAddress);
                    return User.Errors.Unauthorized;
                }

                try
                {
                    var exceptToken = request.Param.RefreshToken;
                    var reason = string.IsNullOrWhiteSpace(value: exceptToken)
                        ? "User requested logout from all devices including current session"
                        : "User requested logout from all other devices";

                    var revokeResult = await refreshTokenService.RevokeAllUserTokensAsync(
                        userId: userId,
                        ipAddress: ipAddress,
                        reason: reason,
                        exceptToken: exceptToken,
                        cancellationToken: cancellationToken);

                    if (revokeResult.IsError)
                    {
                        logger.LogError(
                            message: "Failed to revoke tokens for user {UserId} from IP {IpAddress}: {Errors}", args:
                            [userId, ipAddress, string.Join(separator: ", ",
                                values: revokeResult.Errors.Select(selector: e => e.Code))
                            ]);

                        return revokeResult.Errors;
                    }

                    var revokedCount = revokeResult.Value;

                    if (revokedCount > 0)
                    {
                        logger.LogInformation(
                            message: "User {UserId} logged out from {Count} session(s) from IP {IpAddress}. Current session preserved: {PreservedSession}", args: [userId, revokedCount, ipAddress, !string.IsNullOrWhiteSpace(value: exceptToken)]);
                    }
                    else
                    {
                        logger.LogInformation(
                            message: "User {UserId} logout-all command completed with no active sessions to revoke from IP {IpAddress}", args: [userId, ipAddress]);
                    }

                    return Result.Deleted;
                }
                catch (Exception ex)
                {
                    logger.LogError(exception: ex,
                        message: "Logout-all operation failed for user {UserId} from IP {IpAddress}", args: [userId, ipAddress]);
                    return Error.Failure(code: "LogoutAll.Failed",
                        description: "Failed to logout from all devices");
                }
            }
        }
    }
}