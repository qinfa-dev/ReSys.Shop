using Mapster;

using ReSys.Core.Domain.Identity.UserAddresses;
using ReSys.Core.Domain.Location;

namespace ReSys.Core.Feature.Accounts.Addresses;

public  static partial class AddressModule
{
    public static class Model
    {
        #region Request

        public record Param
        {
            public required string FirstName { get; set; }
            public required string LastName { get; set; }
            public string? Company { get; set; }
            public required string Address1 { get; set; }
            public string? Address2 { get; set; }
            public required string City { get; set; }
            public required string Zipcode { get; set; }
            public required string Phone { get; set; }
            public string? StateName { get; set; }
            public string? Label { get; set; }
            public bool QuickCheckout { get; set; }
            public bool IsDefault { get; set; }
            public AddressType Type { get; set; }
            public required Guid CountryId { get; set; }
            public Guid? StateId { get; set; }
        }

        public sealed class ParamValidator : AbstractValidator<Param>
        {
            public ParamValidator()
            {
                RuleFor(expression: x => x.FirstName)
                    .MustBeValidName(prefix: nameof(UserAddress),
                        field: nameof(Param.FirstName));

                RuleFor(expression: x => x.LastName)
                    .MustBeValidName(prefix: nameof(UserAddress),
                        field: nameof(Param.LastName));

                RuleFor(expression: x => x.Address1)
                    .NullableRequired(prefix: nameof(UserAddress),
                        field: nameof(Param.Address1))
                    .MaxLength(max: AddressConstraints.Address1MaxLength,
                        prefix: nameof(UserAddress),
                        field: nameof(Param.Address1));

                RuleFor(expression: x => x.Address2)
                    .MaxLength(max: AddressConstraints.Address2MaxLength,
                        prefix: nameof(UserAddress),
                        field: nameof(Param.Address2));

                RuleFor(expression: x => x.City)
                    .NullableRequired(prefix: nameof(UserAddress),
                        field: nameof(Param.City))
                    .MaxLength(max: AddressConstraints.CityMaxLength,
                        prefix: nameof(UserAddress),
                        field: nameof(Param.City));

                RuleFor(expression: x => x.Zipcode)
                    .NullableRequired(prefix: nameof(UserAddress),
                        field: nameof(Param.Zipcode))
                    .MustBeValidZipCode(prefix: nameof(UserAddress),
                        field: nameof(Param.Zipcode));

                RuleFor(expression: x => x.Phone)
                    .MustBeValidPhoneRequired(prefix: nameof(UserAddress),
                        field: nameof(Param.Phone));

                RuleFor(expression: x => x.Label)
                    .MaxLength(max: UserAddress.UserAddressConstraints.LabelMaxLength,
                        prefix: nameof(UserAddress),
                        field: nameof(Param.Label));

                RuleFor(expression: x => x.Company)
                    .MaxLength(max: AddressConstraints.CompanyMaxLength,
                        prefix: nameof(UserAddress),
                        field: nameof(Param.Company));

                var countryIdRequired = CommonInput.Errors.Required(prefix: nameof(UserAddress),
                    field: nameof(UserAddress.CountryId));
                RuleFor(expression: x => x.CountryId)
                    .NotEmpty()
                    .WithErrorCode(errorCode: countryIdRequired.Code)
                    .WithMessage(errorMessage: countryIdRequired.Description);
            }
        }

        #endregion

        #region Response
        public record ListItem
        {
            public Guid Id { get; set; }
            public string FirstName { get; set; } = null!;
            public string LastName { get; set; } = null!;
            public string Address1 { get; set; } = null!;
            public string City { get; set; } = null!;
            public string Zipcode { get; set; } = null!;
            public string Phone { get; set; } = null!;
            public string? StateName { get; set; }
            public string? Label { get; set; }
            public bool QuickCheckout { get; set; }
            public bool IsDefault { get; set; }
            public AddressType Type { get; set; }
            public Guid CountryId { get; set; }
            public Guid? StateId { get; set; }
            public string? Company { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
        }

        public record Detail : ListItem
        {
            public string? Address2 { get; set; }
            public string? CountryName { get; set; }
            public string? UserId { get; set; }
        }

        public record SelectItem
        {
            public Guid Id { get; set; }
            public string Label { get; set; } = null!;
            public string AddressSummary { get; set; } = null!;
        }
        public class Mapping : IRegister
        {
            public void Register(TypeAdapterConfig config)
            {
                config.NewConfig<UserAddress, ListItem>()
                    .Map(member: dest => dest.Id,
                        source: src => src.Id)
                    .Map(member: dest => dest.FirstName,
                        source: src => src.FirstName)
                    .Map(member: dest => dest.LastName,
                        source: src => src.LastName)
                    .Map(member: dest => dest.Address1,
                        source: src => src.Address1)
                    .Map(member: dest => dest.City,
                        source: src => src.City)
                    .Map(member: dest => dest.Zipcode,
                        source: src => src.Zipcode)
                    .Map(member: dest => dest.Phone,
                        source: src => src.Phone)
                    .Map(member: dest => dest.StateName,
                        source: src => src.State != null
                            ? src.State.Name
                            : null)
                    .Map(member: dest => dest.Label,
                        source: src => src.Label)
                    .Map(member: dest => dest.QuickCheckout,
                        source: src => src.QuickCheckout)
                    .Map(member: dest => dest.IsDefault,
                        source: src => src.IsDefault)
                    .Map(member: dest => dest.Type,
                        source: src => src.Type)
                    .Map(member: dest => dest.CountryId,
                        source: src => src.CountryId)
                    .Map(member: dest => dest.StateId,
                        source: src => src.StateId)
                    .Map(member: dest => dest.Company,
                        source: src => src.Company)
                    .Map(member: dest => dest.CreatedAt,
                        source: src => src.CreatedAt)
                    .Map(member: dest => dest.UpdatedAt,
                        source: src => src.UpdatedAt);

                config.NewConfig<UserAddress, Detail>()
                    .Inherits<UserAddress, ListItem>()
                    .Map(member: dest => dest.Address2,
                        source: src => src.Address2)
                    .Map(member: dest => dest.CountryName,
                        source: src => src.Country != null
                            ? src.Country.Name
                            : null)
                    .Map(member: dest => dest.UserId,
                        source: src => src.UserId);

                config.NewConfig<UserAddress, SelectItem>()
                    .Map(member: dest => dest.Id,
                        source: src => src.Id)
                    .Map(member: dest => dest.Label,
                        source: src => src.Label ?? $"{src.Address1}, {src.City}")
                    .Map(member: dest => dest.AddressSummary,
                        source: src => $"{src.Address1}, {src.City}, {src.Zipcode}");
            }
        }
        #endregion
    }

}
