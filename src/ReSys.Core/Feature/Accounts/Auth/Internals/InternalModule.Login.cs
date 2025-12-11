using Mapster;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Security.Authentication.Tokens.Interfaces;
using ReSys.Core.Feature.Common.Security.Authentication.Tokens.Models;

using Serilog;

namespace ReSys.Core.Feature.Accounts.Auth.Internals;

public static partial class InternalModule
{
    public static class Login
    {
        public sealed record Param(string Credential, string Password, bool RememberMe = false);
        public sealed record Result : AuthenticationResult;
        public sealed record Command(Param Param) : ICommand<Result>;

        public sealed class ParamValidator : AbstractValidator<Param>
        {
            public ParamValidator()
            {
                const string prefix = nameof(User);
                const string credentialField = nameof(Param.Credential);
                const string passwordField = nameof(Param.Password);

                var invalidCredential = CommonInput.Errors.InvalidPattern(prefix: nameof(User),
                    field: nameof(credentialField));
                RuleFor(expression: x => x.Credential)
                    .Required(prefix: prefix,
                        field: credentialField)
                    .Must(predicate: IsValidCredential)
                    .WithErrorCode(errorCode: invalidCredential.Code)
                    .WithMessage(errorMessage: invalidCredential.Description);

                RuleFor(expression: x => x.Password)
                    .Required(prefix: prefix,
                        field: passwordField)
                    .MustBeValidPassword(prefix: prefix,
                        field: passwordField);
            }

            private static bool IsValidCredential(string? credential)
            {
                if (string.IsNullOrWhiteSpace(value: credential))
                    return false;

                string input = credential.Trim();

                if (CommonInput.Constraints.NamesAndUsernames.UsernameRegex.IsMatch(input: input))
                    return true;

                if (CommonInput.Constraints.Email.Regex.IsMatch(input: input))
                    return true;

                if (CommonInput.Constraints.PhoneNumbers.E164Regex.IsMatch(input: input))
                    return true;

                return false;
            }
        }
        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Param).SetValidator(validator: new ParamValidator());
            }
        }

        public sealed class CommandHandler(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            IJwtTokenService jwtTokenService,
            IRefreshTokenService refreshTokenService,
            IHttpContextAccessor httpContextAccessor) : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
            {
                Param param = request.Param;
                string credential = param.Credential.Trim();

                // Determine credential type and find user
                User? user = null;
                if (CommonInput.Constraints.NamesAndUsernames.UsernameRegex.IsMatch(input: credential))
                {
                    user = await userManager.FindByNameAsync(userName: credential);
                }
                else if (CommonInput.Constraints.Email.Regex.IsMatch(input: credential))
                {
                    user = await userManager.FindByEmailAsync(email: credential);
                }
                else if (CommonInput.Constraints.PhoneNumbers.E164Regex.IsMatch(input: credential))
                {
                    user = await userManager.Users
                        .SingleOrDefaultAsync(predicate: u => u.PhoneNumber == credential,
                            cancellationToken: cancellationToken);
                }

                // Check: User existence
                if (user is null)
                {
                    Log.Warning(messageTemplate: "No user found for credential: {Credential}",
                        propertyValue: credential);
                    return User.Errors.NotFound(credential: credential);
                }

                // Check: User is not locked out
                SignInResult result = await signInManager.CheckPasswordSignInAsync(
                    user: user,
                    password: param.Password,
                    lockoutOnFailure: true);

                // Check: Sign-in result
                if (!result.Succeeded)
                {
                    if (result.IsLockedOut)
                    {
                        Log.Warning(messageTemplate: "Login attempt for locked user: {UserId}",
                            propertyValue: user.Id);
                        return User.Errors.LockedOut;
                    }
                    if (result.IsNotAllowed)
                    {
                        Log.Warning(messageTemplate: "Login attempt for unconfirmed user: {UserId}",
                            propertyValue: user.Id);
                        return User.Errors.EmailNotConfirmed;
                    }

                    Log.Warning(messageTemplate: "Invalid password for user: {UserId}",
                        propertyValue: user.Id);
                    return User.Errors.InvalidCredentials;
                }

                // Get: IP address and user-agent
                string ipAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // Generate: tokens (access + refresh)
                user.RecordSignIn(ipAddress: ipAddress);
                await userManager.UpdateAsync(user: user);

                ErrorOr<TokenResult> accessResult = await jwtTokenService.GenerateAccessTokenAsync(applicationUser: user,
                    cancellationToken: cancellationToken);
                if (accessResult.IsError)
                {
                    Log.Error(messageTemplate: "Access token generation failed for user {UserId}",
                        propertyValue: user.Id);
                    return accessResult.Errors;
                }

                ErrorOr<TokenResult> refreshResult = await refreshTokenService.GenerateRefreshTokenAsync(
                    userId: user.Id,
                    ipAddress: ipAddress,
                    rememberMe: param.RememberMe,
                    cancellationToken: cancellationToken);
                if (refreshResult.IsError)
                {
                    Log.Error(messageTemplate: "Refresh token generation failed for user {UserId}",
                        propertyValue: user.Id);
                    return refreshResult.Errors;
                }

                AuthenticationResult tokens = new AuthenticationResult
                {
                    AccessToken = accessResult.Value.Token,
                    AccessTokenExpiresAt = accessResult.Value.ExpiresAt,
                    RefreshToken = refreshResult.Value.Token,
                    RefreshTokenExpiresAt = refreshResult.Value.ExpiresAt,
                    TokenType = "Bearer"
                };

                return tokens.Adapt<Result>();
            }
        }
    }
}