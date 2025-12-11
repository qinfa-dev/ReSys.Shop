using ReSys.Core.Domain.Identity.Users;

namespace ReSys.Core.Feature.Accounts.Profile;

public static partial class ProfileModule
{
    public static class Model
    {
        public record Param
        {
            public string Username { get; init; } = string.Empty;
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public DateTimeOffset? DateOfBirth { get; set; }
            public string? ProfileImagePath { get; set; }
        }

        public record Result
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; init; } = string.Empty;
            public string? PhoneNumber { get; init; }
            public string Username { get; init; } = string.Empty;
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public DateTimeOffset? DateOfBirth { get; set; }
            public string? ProfileImagePath { get; set; }
            public DateTimeOffset? LastSignInAt { get; set; }
            public string? LastSignInIp { get; set; }
        }

        public sealed class Validator : AbstractValidator<Param>
        {
            public Validator()
            {
                RuleFor(expression: x => x.Username)
                    .MustBeValidUsername(prefix: nameof(User),
                        field: nameof(Param.Username));

                RuleFor(expression: x => x.FirstName)
                    .MustBeValidName(prefix: nameof(User),
                        field: nameof(Param.FirstName))
                    .When(predicate: m => !string.IsNullOrWhiteSpace(value: m.FirstName));

                RuleFor(expression: x => x.LastName)
                    .MustBeValidName(prefix: nameof(User),
                        field: nameof(Param.LastName))
                    .When(predicate: m => !string.IsNullOrWhiteSpace(value: m.LastName));

                RuleFor(expression: x => x.DateOfBirth)
                    .MustBeValidDateTimeOffset(prefix: nameof(User),
                        field: nameof(User.DateOfBirth))
                    .MustBeInPastOptional(prefix: nameof(User),
                        field: nameof(User.DateOfBirth))
                    .When(predicate: m => m.DateOfBirth.HasValue);

                RuleFor(expression: x => x.ProfileImagePath)
                    .MustBeValidUri(prefix: nameof(User),
                        field: nameof(User.ProfileImagePath));

            }
        }
    }
}