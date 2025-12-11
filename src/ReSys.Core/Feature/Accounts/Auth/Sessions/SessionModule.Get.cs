using Microsoft.AspNetCore.Identity;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;
using ReSys.Core.Feature.Common.Security.Authorization.Claims.Models;
using ReSys.Core.Feature.Common.Security.Authorization.Claims.Providers;

namespace ReSys.Core.Feature.Accounts.Auth.Sessions;

public static partial class SessionModule
{
    public static class Get
    {
        public record Result
        {
            public string UserId { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? PhoneNumber { get; set; }
            public bool IsEmailConfirmed { get; set; }
            public bool IsPhoneNumberConfirmed { get; set; }
            public List<string> Roles { get; set; } = [];
            public List<string> Permissions { get; set; } = [];
        }

        public sealed record Query : IQuery<Result>;

        #region CommandHandler
        public sealed class CommandHandler(
            IUserContext userContext,
            UserManager<User> userManager,
            IAuthorizeClaimDataProvider userAuthorizationProvider) : IQueryHandler<Query, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
            {
                string? userId = userContext.UserId;
                bool isAuthenticated = userContext.IsAuthenticated;

                // Check: user is authenticated
                if (userId is null || !isAuthenticated)
                    return User.Errors.Unauthorized;

                Result? user = await userManager.Users
                    .Where(predicate: u => u.Id == userId)
                    .Select(selector: u => new Result
                    {
                        UserId = u.Id,
                        UserName = u.UserName ?? string.Empty,
                        Email = u.Email ?? string.Empty,
                        PhoneNumber = u.PhoneNumber,
                        IsEmailConfirmed = u.EmailConfirmed,
                        IsPhoneNumberConfirmed = u.PhoneNumberConfirmed,
                    })
                    .FirstOrDefaultAsync(cancellationToken: cancellationToken);

                if (user is null)
                    return User.Errors.NotFound(credential: userId);

                AuthorizeClaimData? authData = await userAuthorizationProvider.GetUserAuthorizationAsync(userId: userId);
                if (authData is null)
                    return User.Errors.Unauthorized;

                user.Roles = authData.Roles.ToList();
                user.Permissions = authData.Permissions.ToList();

                return user;
            }
        }
        #endregion
    }
}