using Microsoft.AspNetCore.Identity;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;

namespace ReSys.Core.Feature.Accounts.Profile;

public static partial class ProfileModule
{
    public static class Get
    {
        public sealed record Result : Model.Result;

        public sealed record Query(string? UserId)
            : IQuery<Result>;

        public sealed class CommandHandler(
            UserManager<User> userManager,
            IUserContext userContext)
            : IQueryHandler<Query, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
            {
                if (!userContext.IsAuthenticated || string.IsNullOrEmpty(value: userContext.UserId))
                    return User.Errors.Unauthorized;

                string userId = request.UserId ?? userContext.UserId!;

                if (userId != userContext.UserId)
                    return User.Errors.Unauthorized;

                var user = await userManager.Users
                    .Where(predicate: u => u.Id == userId)
                    .Select(selector: u => new Result
                    {
                        Id = u.Id,
                        Username = u.UserName ?? string.Empty,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        DateOfBirth = u.DateOfBirth,
                        ProfileImagePath = u.ProfileImagePath,
                        Email = u.Email ?? string.Empty,
                        PhoneNumber = u.PhoneNumber,
                        LastSignInAt = u.LastSignInAt,
                        LastSignInIp = u.LastSignInIp,
                    })
                    .FirstOrDefaultAsync(cancellationToken: cancellationToken);

                return user is null
                    ? User.Errors.NotFound(credential: userId)
                    : user;
            }
        }
    }
}