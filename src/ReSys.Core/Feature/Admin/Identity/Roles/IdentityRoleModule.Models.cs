using Mapster;

using ReSys.Core.Domain.Identity.Roles;
using ReSys.Core.Domain.Identity.Roles.Claims;
using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Security.Authorization.Claims.Constants;

namespace ReSys.Core.Feature.Admin.Identity.Roles;

public static partial class IdentityRoleModule
{
    public static class Models
    {
        public record Parameter
        {
            public required string Name { get; init; }
            public string? DisplayName { get; init; }
            public string? Description { get; init; }
            public int Priority { get; init; } = 0;
            public bool IsSystemRole { get; init; } = false;
        }

        public sealed class ParameterValidator : AbstractValidator<Parameter>
        {
            public ParameterValidator()
            {
                const string prefix = nameof(Role);
                RuleFor(expression: x => x.Name)
                    .MustBeValidName(isRequired: true,
                        prefix: prefix,
                        field: nameof(Parameter.Name));

                RuleFor(expression: x => x.DisplayName)
                    .MustBeValidName(isRequired: false,
                        prefix: prefix,
                        field: nameof(Parameter.Name))
                    .When(predicate: x => !string.IsNullOrEmpty(value: x.DisplayName));

                RuleFor(expression: x => x.Description)
                    .MustBeValidInput(
                        isRequired: false,
                        prefix: prefix,
                        field: nameof(Parameter.Description),
                        maxLength: CommonInput.Constraints.Text.MaxLength)
                    .When(predicate: x => !string.IsNullOrEmpty(value: x.Description));


                RuleFor(expression: x => x.Priority)
                    .GreaterThanOrEqualTo(valueToCompare: 1)
                    .WithErrorCode(errorCode: CommonInput.Errors
                        .OutOfRange(prefix: prefix, field: nameof(Parameter.Priority), minValue: 1).Code)
                    .WithMessage(errorMessage: CommonInput.Errors
                        .OutOfRange(prefix: prefix, field: nameof(Parameter.Priority), minValue: 1).Description);
            }
        }

        public record SelectItem
        {
            public required string Id { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
        }

        public record UserItem
        {
            public required string Id { get; set; }
            public required string UserName { get; set; }
            public string? FullName { get; set; }
        }

        public record PermissionItem
        {
            public required string Name { get; set; }
            public required string DisplayName { get; set; }
            public string? Description { get; set; }
        }

        public record ListItem
        {
            public required string Id { get; set; }
            public required string Name { get; init; }
            public string? DisplayName { get; init; }
            public string? Description { get; init; }
            public int Priority { get; init; } = 0;
            public bool IsSystemRole { get; init; } = false;
            public bool IsDefault { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public string? CreatedBy { get; set; }

            public int UserCount { get; set; } = 0;
            public int PermissionCount { get; set; } = 0;
        }

        public record Detail : ListItem
        {
            public DateTimeOffset? UpdatedAt { get; set; }
            public string? UpdatedBy { get; set; }
        }

        public sealed class Mapping : IRegister
        {
            public void Register(TypeAdapterConfig config)
            {
                // Role: ListItem
                config.NewConfig<Role, ListItem>()
                    .Map(member: dest => dest.Id,
                        source: src => src.Id)
                    .Map(member: dest => dest.Name,
                        source: src => src.Name)
                    .Map(member: dest => dest.DisplayName,
                        source: src => src.DisplayName)
                    .Map(member: dest => dest.Description,
                        source: src => src.Description)
                    .Map(member: dest => dest.Priority,
                        source: src => src.Priority)
                    .Map(member: dest => dest.IsSystemRole,
                        source: src => src.IsSystemRole)
                    .Map(member: dest => dest.IsDefault,
                        source: src => src.IsDefault)
                    .Map(member: dest => dest.CreatedAt,
                        source: src => src.CreatedAt)
                    .Map(member: dest => dest.CreatedBy,
                        source: src => src.CreatedBy)
                    .Map(member: dest => dest.UserCount,
                        source: src => src.UserRoles.Select(m => m.UserId).Distinct().Count())
                    .Map(member: dest => dest.PermissionCount,
                        source: src => src.RoleClaims.Count(rc => rc.ClaimType == CustomClaim.Permission));

                // Role: Detail
                config.NewConfig<Role, Detail>()
                    .Inherits<Role, ListItem>()
                    .Map(member: dest => dest.UpdatedAt,
                        source: src => src.UpdatedAt)
                    .Map(member: dest => dest.UpdatedBy,
                        source: src => src.UpdatedBy);

                // Role: SelectItem
                config.NewConfig<Role, SelectItem>()
                    .Map(member: dest => dest.Id,
                        source: src => src.Id)
                    .Map(member: dest => dest.Name,
                        source: src => src.Name)
                    .Map(member: dest => dest.Description,
                        source: src => src.Description);

                // User to UserItem
                config.NewConfig<User, UserItem>()
                    .Map(dest => dest.Id, src => src.Id)
                    .Map(dest => dest.UserName, src => src.UserName)
                    .Map(dest => dest.FullName, src => src.FullName);

                // RoleClaim to PermissionItem
                config.NewConfig<RoleClaim, PermissionItem>()
                    .Map(dest => dest.Name, src => src.ClaimType)
                    .Map(dest => dest.DisplayName, src => src.ClaimType)
                    .Map(dest => dest.Description, src => src.ClaimValue);
            }
        }
    }
}