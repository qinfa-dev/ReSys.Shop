using Mapster;

using ReSys.Core.Feature.Common.Security.Authentication.Externals.Interfaces;
using ReSys.Core.Feature.Common.Security.Authentication.Externals.Models;

namespace ReSys.Core.Feature.Accounts.Auth.Externals;

public static partial class ExternalModule
{
    public static class VerifyExternalToken
    {
        internal const string Name = "Account.Authentication.External.Verify";
        internal const string Summary = "Verify external provider token";
        internal const string Description = "Validates an external provider token and returns user information without creating a session";

        public record Param(
            string? AccessToken = null,
            string? IdToken = null);

        public sealed class ParamValidator : AbstractValidator<Param>
        {
            public ParamValidator()
            {
                RuleFor(expression: x => x.AccessToken)
                    .NullableRequired(prefix: nameof(VerifyExternalToken),
                        field: nameof(Param.AccessToken));

                RuleFor(expression: x => x.IdToken)
                    .NullableRequired(prefix: nameof(VerifyExternalToken),
                        field: nameof(Param.IdToken));
            }
        }

        public sealed record Command(
            string? Provider,
            Param Param
        ) : ICommand<Result>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Provider)
                    .NotEmpty()
                    .WithErrorCode(errorCode: "Provider.Required")
                    .WithMessage(errorMessage: "Provider is required.");
                RuleFor(expression: x => x.Param)
                    .SetValidator(validator: new ParamValidator());
            }
        }
        public sealed record Result : ExternalUserTransfer;

        public sealed class CommandHandler(
            IExternalTokenValidator tokenValidator
        ) : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrWhiteSpace(value: request.Provider))
                    return CommonInput.Errors.Required(prefix: nameof(VerifyExternalToken),
                        field: nameof(Command.Provider));

                ErrorOr<ExternalUserTransfer> validationResult = await tokenValidator.ValidateTokenAsync(
                    provider: request.Provider,
                    accessToken: request.Param.AccessToken,
                    idToken: request.Param.IdToken,
                    authorizationCode: null, // No auth code for verification
                    redirectUri: null, // No redirect URI for verification
                    cancellationToken: cancellationToken
                );

                if (validationResult.IsError)
                {
                    return validationResult.Errors;
                }

                return validationResult.Value.Adapt<Result>();
            }
        }
    }
}