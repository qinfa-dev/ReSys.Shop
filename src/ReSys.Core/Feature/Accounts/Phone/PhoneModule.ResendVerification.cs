using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Accounts.Common;
using ReSys.Core.Feature.Common.Notification.Interfaces;
using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;

namespace ReSys.Core.Feature.Accounts.Phone;


public static partial class PhoneModule
{
    public static class ResendVerification
    {
        #region Records
        public sealed record Param(string PhoneNumber);
        public sealed record Result(string Message)
        {
            public static Result Default(string phoneNumber) => new(Message: $"A new verification SMS has been sent to {phoneNumber}.");
        }
        public sealed record Command(Param Param) : ICommand<Result>;
        #endregion

        #region Validators
        public sealed class ParamValidator : AbstractValidator<Param>
        {

            public ParamValidator()
            {
                RuleFor(expression: x => x.PhoneNumber)
                    .MustBeValidPhone(prefix: nameof(User),
                        field: nameof(Param.PhoneNumber));
            }
        }
        public sealed class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(expression: x => x.Param)
                    .SetValidator(validator: new ParamValidator());
            }
        }
        #endregion

        #region CommandHandler
        public sealed class CommandHandler(
            UserManager<User> userManager,
            IUserContext userContext,
            INotificationService notificationService,
            IConfiguration configuration)
            : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
            {
                // Load: user context
                string? userId = userContext.UserId;
                bool isAuthenticated = userContext.IsAuthenticated;

                // Check: user is authenticated
                if (userId is null || !isAuthenticated)
                    return User.Errors.Unauthorized;

                // Check: user exists
                User? user = await userManager.FindByIdAsync(userId: userId);
                if (user is null)
                    return User.Errors.NotFound(credential: userId);

                Param param = request.Param;

                // Check: phone is not already in use by another user
                IQueryable<User> existingUserQuery = userManager.Users.Where(predicate: u => u.PhoneNumber == param.PhoneNumber && u.Id != user.Id);
                User? existingUser = await existingUserQuery.FirstOrDefaultAsync(cancellationToken: cancellationToken);
                if (existingUser != null)
                    return User.Errors.PhoneNumberAlreadyExists(phoneNumber: param.PhoneNumber);

                // Send: phone verification SMS
                ErrorOr<Success> sendSmsResult = await userManager.GenerateAndSendConfirmationSmsAsync(
                    notificationService: notificationService,
                    configuration: configuration,
                    user: user,
                    newPhoneNumber: param.PhoneNumber,
                    cancellationToken: cancellationToken);

                if (sendSmsResult.IsError)
                {
                    Serilog.Log.Warning(
                        messageTemplate: "Failed to resend phone verification to {PhoneNumber} for user {UserId}: {Errors}",
                        propertyValue0: param.PhoneNumber,
                        propertyValue1: userId,
                        propertyValue2: string.Join(separator: ", ",
                            values: sendSmsResult.Errors.Select(selector: e => e.Description)));
                    return sendSmsResult.Errors;
                }

                Serilog.Log.Information(messageTemplate: "Phone verification resent for user {UserId} to {PhoneNumber}",
                    propertyValue0: userId,
                    propertyValue1: param.PhoneNumber);

                return Result.Default(phoneNumber: param.PhoneNumber);
            }
        }
        #endregion
    }
}
