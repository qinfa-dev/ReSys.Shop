using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Accounts.Common;
using ReSys.Core.Feature.Common.Notification.Interfaces;
using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;

namespace ReSys.Core.Feature.Accounts.Phone;

public static partial class PhoneModule
{
    public static class Change
    {
        public sealed record Param(string NewPhone);
        public sealed record Result(string ConfirmMessage)
        {
            public static Result PhoneChangeInitiated => new(ConfirmMessage: "If the phone number is valid and not already in use, a confirmation code has been sent to the new phone number.");
        }
        public sealed record Command(Param Param) : ICommand<Result>;

        public sealed class ParamValidator : AbstractValidator<Param>
        {
            public ParamValidator()
            {
                RuleFor(expression: x => x.NewPhone)
                    .MustBeValidPhone(prefix: nameof(User),
                        field: nameof(Param.NewPhone));
            }
        }
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

                // Check: new phone is different from current phone
                if (string.Equals(a: user.PhoneNumber,
                        b: param.NewPhone,
                        comparisonType: StringComparison.OrdinalIgnoreCase))
                    return Error.Validation(code: "ChangePhone.SamePhone",
                        description: "The new phone number must be different from the current phone number.");

                // Check: new phone is not already in use by another user
                IQueryable<User> existingUserQuery = userManager.Users.Where(predicate: u => u.PhoneNumber == param.NewPhone && u.Id != user.Id);
                User? existingUser = await existingUserQuery.FirstOrDefaultAsync(cancellationToken: cancellationToken);
                if (existingUser != null)
                    return User.Errors.PhoneNumberAlreadyExists(phoneNumber: param.NewPhone);

                // Generate verification code for the new phone number
                string code = await userManager.GenerateChangePhoneNumberTokenAsync(user: user,
                    phoneNumber: param.NewPhone);

                // Debug: Log the generated code (remove in production)
                Serilog.Log.Debug(
                    messageTemplate: "Generated phone change token for user {UserId} to new phone {NewPhone}: {Code}",
                    propertyValue0: userId,
                    propertyValue1: param.NewPhone,
                    propertyValue2: code);

                // Send SMS verification to new phone number
                ErrorOr<Success> sendSmsResult = await userManager.GenerateAndSendConfirmationSmsAsync(
                    notificationService: notificationService,
                    configuration: configuration,
                    user: user,
                    newPhoneNumber: param.NewPhone,
                    cancellationToken: cancellationToken);

                if (sendSmsResult.IsError)
                {
                    Serilog.Log.Warning(
                        messageTemplate: "Failed to send phone change verification to {NewPhone} for user {UserId}: {Errors}",
                        propertyValue0: param.NewPhone,
                        propertyValue1: userId,
                        propertyValue2: string.Join(separator: ", ",
                            values: sendSmsResult.Errors.Select(selector: e => e.Description)));
                    return sendSmsResult.Errors;
                }

                Serilog.Log.Information(
                    messageTemplate: "Phone change verification sent for user {UserId} from {CurrentPhone} to {NewPhone}",
                    propertyValue0: userId,
                    propertyValue1: user.PhoneNumber,
                    propertyValue2: param.NewPhone);

                return Result.PhoneChangeInitiated;
            }
        }
    }
}
