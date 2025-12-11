using Mapster;

using MapsterMapper;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Identity.Roles;
using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Accounts.Common;
using ReSys.Core.Feature.Accounts.Profile;
using ReSys.Core.Feature.Common.Notification.Interfaces;
using ReSys.Core.Feature.Common.Security.Authorization.Roles;

namespace ReSys.Core.Feature.Accounts.Auth.Internals;

public static partial class InternalModule
{
    public static class Register
    {
        public sealed record Param
        {
            public string? UserName { get; set; }
            public string Email { get; set; } = string.Empty;
            public string? FirstName { get; set; } = string.Empty;
            public string? LastName { get; set; } = string.Empty;
            public string? PhoneNumber { get; set; }
            public string ConfirmPassword { get; set; } = null!;
            public string Password { get; set; } = null!;
            public DateTimeOffset? DateOfBirth { get; set; }

        }

        public sealed record Result : ProfileModule.Model.Result;
        public record Command(Param Param) : ICommand<Result>;

        public class ParamValidator : AbstractValidator<Param>
        {
            public ParamValidator()
            {
                RuleFor(expression: x => x.Email)
                    .NullableRequired(prefix: nameof(User),
                        field: nameof(Param.Email))
                    .MustBeValidEmail(prefix: nameof(User),
                        field: nameof(Param.Email));

                RuleFor(expression: x => x.UserName)
                    .MustBeValidUsername(prefix: nameof(User),
                        field: nameof(Param.UserName))
                    .When(predicate: x => !string.IsNullOrEmpty(value: x.UserName));

                RuleFor(expression: x => x.PhoneNumber)
                    .MustBeValidPhone(prefix: nameof(User),
                        field: nameof(Param.PhoneNumber))
                    .When(predicate: x => !string.IsNullOrEmpty(value: x.PhoneNumber));

                RuleFor(expression: x => x.Password)
                    .NullableRequired(prefix: nameof(User),
                        field: nameof(Param.Password))
                    .MustBeValidPhone(prefix: nameof(User),
                        field: nameof(Param.Password));

                var confirmPasswordMismatch = Error.Validation(
                    code: $"{nameof(User)}.PasswordsDoNotMatch",
                    description: "Password and confirm password do not match.");

                RuleFor(expression: x => x.ConfirmPassword)
                    .NullableRequired(prefix: nameof(User),
                        field: nameof(Param.ConfirmPassword))
                    .Equal(expression: x => x.Password)
                    .WithErrorCode(errorCode: confirmPasswordMismatch.Code)
                    .WithMessage(errorMessage: confirmPasswordMismatch.Description);

                RuleFor(expression: x => x.FirstName)
                    .MustBeValidName(prefix: nameof(User),
                        field: nameof(Param.FirstName))
                    .When(predicate: x => !string.IsNullOrEmpty(value: x.FirstName));

                RuleFor(expression: x => x.LastName)
                    .MustBeValidName(prefix: nameof(User),
                        field: nameof(Param.LastName))
                    .When(predicate: x => !string.IsNullOrEmpty(value: x.LastName));

                RuleFor(expression: x => x.DateOfBirth)
                    .MustBeInPastOptional(prefix: nameof(User),
                        field: nameof(Param.DateOfBirth))
                    .When(predicate: x => x.DateOfBirth.HasValue);
            }
        }

        public sealed class CommandHandler(
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IMapper mapper) : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
            {
                // Check: user already exists by email
                Param param = request.Param;
                User? existingUser = await userManager.FindByEmailAsync(email: param.Email);
                if (existingUser != null)
                {
                    Serilog.Log.Warning(messageTemplate: "User with email {Email} already exists",
                        propertyValue: param.Email);
                    return User.Errors.EmailAlreadyExists(email: param.Email);
                }

                // Check: user already exists by username
                if (!string.IsNullOrWhiteSpace(value: param.UserName))
                {
                    existingUser = await userManager.FindByNameAsync(userName: param.UserName);
                    if (existingUser != null)
                    {
                        Serilog.Log.Warning(messageTemplate: "User with username {UserName} already exists",
                            propertyValue: param.UserName);
                        return User.Errors.UserNameAlreadyExists(userName: param.UserName);
                    }
                }

                // Check: phone number already exists if provided
                if (!string.IsNullOrWhiteSpace(value: param.PhoneNumber))
                {
                    User? existingUserByPhone = await userManager.Users
                        .FirstOrDefaultAsync(predicate: u => u.PhoneNumber == param.PhoneNumber,
                            cancellationToken: cancellationToken);
                    if (existingUserByPhone != null)
                    {
                        Serilog.Log.Warning(messageTemplate: "User with phone number {PhoneNumber} already exists",
                            propertyValue: param.PhoneNumber);
                        return User.Errors.PhoneNumberAlreadyExists(phoneNumber: param.PhoneNumber);

                    }
                }

                // Check: current user role is init
                if (!await roleManager.RoleExistsAsync(roleName: DefaultRole.Customer))
                {
                    Serilog.Log.Warning(messageTemplate: "Role {Role} not found",
                        propertyValue: DefaultRole.Customer);
                    return Role.Errors.DefaultRoleNotFound;
                }

                // Create: new user
                ErrorOr<User> createResult = User.Create(
                    email: param.Email,
                    userName: param.UserName,
                    emailConfirmed: false,
                    firstName: param.FirstName,
                    lastName: param.LastName,
                    phoneNumber: param.PhoneNumber,
                    phoneNumberConfirmed: false);

                if (createResult.IsError)
                    return createResult.Errors;
                var user = createResult.Value;

                // Set: password
                IdentityResult passwordResult = await userManager.CreateAsync(user: user,
                    password: param.Password);
                if (!passwordResult.Succeeded)
                    return passwordResult.Errors.ToApplicationResult(prefix: "CreateUserFailed");

                // Assign: default customer role
                IdentityResult roleResult = await userManager.AddToRoleAsync(user: user,
                    role: DefaultRole.Customer);
                if (!roleResult.Succeeded)
                {
                    // Rollback: user creation if role assignment fails
                    await userManager.DeleteAsync(user: user);
                    Serilog.Log.Error(messageTemplate: "Failed to assign role {Role} to user {UserId}: {Errors}",
                        propertyValue0: DefaultRole.Customer,
                        propertyValue1: user.Id,
                        propertyValue2: roleResult.Errors);
                    return roleResult.Errors.ToApplicationResult(prefix: "AssignRoleFailed");
                }

                // Log: user registration
                Serilog.Log.Information(messageTemplate: "User {UserId} registered successfully with email {Email}",
                    propertyValue0: user.Id,
                    propertyValue1: user.Email);

                // Return: user ID
                return mapper.Adapt<Result>();
            }
        }

        public sealed class EventHandler(
            UserManager<User> userManager,
            INotificationService notificationService,
            IConfiguration configuration) : IDomainEventHandler<User.Events.UserRegistered>
        {
            public async Task Handle(User.Events.UserRegistered notification, CancellationToken cancellationToken)
            {
                try
                {
                    User? user = await userManager.FindByIdAsync(userId: notification.UserId);
                    if (user == null)
                    {
                        Serilog.Log.Warning(
                            messageTemplate: "User with ID {UserId} not found for sending confirmation email",
                            propertyValue: notification.UserId);
                        return;
                    }

                    bool emailSuccess = false;
                    bool phoneSuccess = false;
                    bool hasPhone = !string.IsNullOrWhiteSpace(value: user.PhoneNumber);

                    // Send: confirmation email
                    ErrorOr<Success> emailResult = await userManager.GenerateAndSendConfirmationEmailAsync(
                        notificationService: notificationService,
                        configuration: configuration,
                        user: user,
                        cancellationToken: cancellationToken);

                    if (emailResult.IsError)
                    {
                        Serilog.Log.Error(messageTemplate: "Failed to send confirmation email to {Email}: {Errors}",
                            propertyValue0: user.Email,
                            propertyValue1: emailResult.Errors);
                    }
                    else
                    {
                        emailSuccess = true;
                        Serilog.Log.Information(
                            messageTemplate: "Confirmation email sent successfully to {Email} for user {UserId}",
                            propertyValue0: user.Email,
                            propertyValue1: notification.UserId);
                    }

                    // Send: phone confirmation if phone number exists
                    if (hasPhone)
                    {
                        ErrorOr<Success> phoneResult = await userManager.GenerateAndSendConfirmationSmsAsync(
                            notificationService: notificationService,
                            configuration: configuration,
                            user: user,
                            cancellationToken: cancellationToken);

                        if (phoneResult.IsError)
                        {
                            Serilog.Log.Error(
                                messageTemplate: "Failed to send confirmation SMS to {PhoneNumber}: {Errors}",
                                propertyValue0: user.PhoneNumber,
                                propertyValue1: phoneResult.Errors);
                        }
                        else
                        {
                            phoneSuccess = true;
                            Serilog.Log.Information(
                                messageTemplate:
                                "Confirmation SMS sent successfully to {PhoneNumber} for user {UserId}",
                                propertyValue0: user.PhoneNumber,
                                propertyValue1: notification.UserId);
                        }
                    }
                    else
                    {
                        phoneSuccess = true; // No phone to confirm, consider as success
                    }

                    // Rollback: user creation if both confirmations fail
                    if (!emailSuccess && !phoneSuccess)
                    {
                        IdentityResult deleteResult = await userManager.DeleteAsync(user: user);
                        if (deleteResult.Succeeded)
                        {
                            Serilog.Log.Warning(
                                messageTemplate: "User {UserId} deleted due to failed confirmation send",
                                propertyValue: notification.UserId);
                        }
                        else
                        {
                            Serilog.Log.Error(
                                messageTemplate:
                                "Failed to delete user {UserId} after confirmation send failure. Manual cleanup required",
                                propertyValue: notification.UserId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(exception: ex,
                        messageTemplate:
                        "Unexpected error occurred while handling user registration event for user {UserId}",
                        propertyValue: notification.UserId);
                    throw;
                }
            }
        }
    }
}