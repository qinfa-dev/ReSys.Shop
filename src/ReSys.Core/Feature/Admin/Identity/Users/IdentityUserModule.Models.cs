using Mapster;

using ReSys.Core.Domain.Identity.Users;

namespace ReSys.Core.Feature.Admin.Identity.Users;

public static partial class IdentityUserModule
{
    public static class Models
    {
        // Request:
        public record Parameter
        {
            public string Email { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string? Password { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public DateTimeOffset? DateOfBirth { get; set; }
            public string? PhoneNumber { get; set; }
            public string? ProfileImagePath { get; set; }
            public bool EmailConfirmed { get; set; }
            public bool PhoneNumberConfirmed { get; set; }
        }

        // Validator:
        public sealed class ParameterValidator : AbstractValidator<Parameter>
        {
            public ParameterValidator()
            {
                RuleFor(expression: x => x.Email)
                    .NullableRequired(prefix: nameof(User),
                        field: nameof(Parameter.Email))
                    .MustBeValidEmail(prefix: nameof(User),
                        field: nameof(Parameter.Email));

                RuleFor(expression: x => x.UserName)
                    .MustBeValidUsername(prefix: nameof(User),
                        field: nameof(Parameter.UserName))
                    .When(predicate: x => !string.IsNullOrEmpty(value: x.UserName));

                RuleFor(expression: x => x.PhoneNumber)
                    .MustBeValidPhone(prefix: nameof(User),
                        field: nameof(Parameter.PhoneNumber))
                    .When(predicate: x => !string.IsNullOrEmpty(value: x.PhoneNumber));

                RuleFor(expression: x => x.FirstName)
                    .MustBeValidName(prefix: nameof(User),
                        field: nameof(Parameter.FirstName))
                    .When(predicate: x => !string.IsNullOrEmpty(value: x.FirstName));

                RuleFor(expression: x => x.LastName)
                    .MustBeValidName(prefix: nameof(User),
                        field: nameof(Parameter.LastName))
                    .When(predicate: x => !string.IsNullOrEmpty(value: x.LastName));

                RuleFor(expression: x => x.ProfileImagePath)
                    .MustBeValidUri(prefix: nameof(User),
                        field: nameof(Parameter.ProfileImagePath))
                    .When(predicate: x => !string.IsNullOrEmpty(value: x.ProfileImagePath));

                RuleFor(expression: x => x.DateOfBirth)
                    .MustBeInPastOptional(prefix: nameof(User),
                        field: nameof(Parameter.DateOfBirth))
                    .When(predicate: x => x.DateOfBirth.HasValue);
            }
        }

        // Result:
        public record SelectItem
        {
            public string Id { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string? FullName { get; set; }
        }

        public record ListItem
        {
            public string Id { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string? FullName { get; set; }
            public string Email { get; set; } = string.Empty;
            public string? PhoneNumber { get; set; }
            public bool EmailConfirmed { get; set; }
            public bool PhoneNumberConfirmed { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
        }

        public record Detail : ListItem
        {
            public DateTimeOffset? DateOfBirth { get; set; }
            public string? ProfileImagePath { get; set; }
            public DateTimeOffset? LastSignInAt { get; set; }
            public string? LastSignInIp { get; set; }
            public DateTimeOffset? CurrentSignInAt { get; set; }
            public string? CurrentSignInIp { get; set; }
            public int SignInCount { get; set; }
            public bool LockoutEnabled { get; set; }
            public DateTimeOffset? LockoutEnd { get; set; }
            public int AccessFailedCount { get; set; }
        }

        public record RoleItem
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }

        public record PermissionItem
        {
            public string Name { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        // Mapping:
        public sealed class Mapping : IRegister
        {
            public void Register(TypeAdapterConfig config)
            {
                // User -> SelectItem
                config.NewConfig<User, SelectItem>()
                    .Map(dest => dest.Id, src => src.Id)
                    .Map(dest => dest.UserName, src => src.UserName)
                    .Map(dest => dest.FullName, src => src.FullName);

                // User -> ListItem
                config.NewConfig<User, ListItem>()
                    .Map(dest => dest.Id, src => src.Id)
                    .Map(dest => dest.UserName, src => src.UserName)
                    .Map(dest => dest.FullName, src => src.FullName)
                    .Map(dest => dest.Email, src => src.Email)
                    .Map(dest => dest.PhoneNumber, src => src.PhoneNumber)
                    .Map(dest => dest.EmailConfirmed, src => src.EmailConfirmed)
                    .Map(dest => dest.PhoneNumberConfirmed, src => src.PhoneNumberConfirmed)
                    .Map(dest => dest.CreatedAt, src => src.CreatedAt)
                    .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

                // User -> Detail
                config.NewConfig<User, Detail>()
                    .Inherits<User, ListItem>()
                    .Map(dest => dest.DateOfBirth, src => src.DateOfBirth)
                    .Map(dest => dest.ProfileImagePath, src => src.ProfileImagePath)
                    .Map(dest => dest.LastSignInAt, src => src.LastSignInAt)
                    .Map(dest => dest.LastSignInIp, src => src.LastSignInIp)
                    .Map(dest => dest.CurrentSignInAt, src => src.CurrentSignInAt)
                    .Map(dest => dest.CurrentSignInIp, src => src.CurrentSignInIp)
                    .Map(dest => dest.SignInCount, src => src.SignInCount)
                    .Map(dest => dest.LockoutEnabled, src => src.LockoutEnabled)
                    .Map(dest => dest.LockoutEnd, src => src.LockoutEnd)
                    .Map(dest => dest.AccessFailedCount, src => src.AccessFailedCount);

                // Role -> RoleItem
                config.NewConfig<Core.Domain.Identity.Roles.Role, RoleItem>()
                    .Map(dest => dest.Id, src => src.Id)
                    .Map(dest => dest.Name, src => src.Name);
            }
        }
    }
}