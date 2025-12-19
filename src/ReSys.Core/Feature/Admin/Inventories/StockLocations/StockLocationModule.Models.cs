using Mapster;

using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Location; // For AddressConstraints (assuming it's here or similar)

namespace ReSys.Core.Feature.Admin.Inventories.StockLocations;

public static partial class StockLocationModule
{
    public static class Models
    {
        public record Parameter : IHasParameterizableName, IHasUniqueName, IHasMetadata, IAddress
        {
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public bool Active { get; set; } = true;
            public bool Default { get; set; }
            public Guid? CountryId { get; set; }
            public Guid? StateId { get; set; }
            public string? Address1 { get; set; }
            public string? Address2 { get; set; }
            public string? City { get; set; }
            public string? ZipCode { get; set; }
            public string? Phone { get; set; }
            public string? Company { get; set; }
            public IDictionary<string, object?>? PublicMetadata { get; set; }
            public IDictionary<string, object?>? PrivateMetadata { get; set; }
        }

        public sealed class ParameterValidator : AbstractValidator<Parameter>
        {
            public ParameterValidator()
            {
                const string prefix = nameof(StockLocation);

                this.AddParameterizableNameRules(prefix: prefix);
                this.AddMetadataSupportRules(prefix: prefix);

                RuleFor(expression: x => x.Address1)
                    .NullableRequired(prefix: prefix,
                        field: nameof(Parameter.Address1))
                    .MaxLength(max: AddressConstraints.Address1MaxLength,
                        prefix: prefix,
                        field: nameof(Parameter.Address1));

                RuleFor(expression: x => x.Address2)
                    .MaxLength(max: AddressConstraints.Address2MaxLength,
                        prefix: prefix,
                        field: nameof(Parameter.Address2));

                RuleFor(expression: x => x.City)
                    .NullableRequired(prefix: prefix,
                        field: nameof(Parameter.City))
                    .MaxLength(max: AddressConstraints.CityMaxLength,
                        prefix: prefix,
                        field: nameof(Parameter.City));

                RuleFor(expression: x => x.ZipCode)
                    .NullableRequired(prefix: prefix,
                        field: nameof(Parameter.ZipCode))
                    .MustBeValidZipCode(prefix: prefix,
                        field: nameof(Parameter.ZipCode));

                RuleFor(expression: x => x.Phone)
                    .MustBeValidPhone(prefix: prefix,
                        field: nameof(Parameter.Phone));


                RuleFor(expression: x => x.Company)
                    .MaxLength(max: AddressConstraints.CompanyMaxLength,
                        prefix: prefix,
                        field: nameof(Parameter.Company));

                var countryIdRequired = CommonInput.Errors.Required(prefix: prefix,
                    field: nameof(Parameter.CountryId)); // Corrected from UserAddress.CountryId
                RuleFor(expression: x => x.CountryId)
                    .NotEmpty()
                    .WithErrorCode(errorCode: countryIdRequired.Code)
                    .WithMessage(errorMessage: countryIdRequired.Description);
            }
        }

        public record SelectItem
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public bool Active { get; set; }
            public bool Default { get; set; }
        }

        public record ListItem
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public bool Active { get; set; }
            public bool Default { get; set; }
            public string? City { get; set; }
            public string? CountryName { get; set; }
            public string? StateName { get; set; }
            public int StockItemCount { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
        }

        public record Detail : ListItem
        {
            public string? Address1 { get; set; }
            public string? Address2 { get; set; }
            public string? Zipcode { get; set; }
            public string? Phone { get; set; }
            public string? Company { get; set; }
            public IDictionary<string, object?>? PublicMetadata { get; set; }
            public IDictionary<string, object?>? PrivateMetadata { get; set; }
        }

        public sealed class Mapping : IRegister
        {
            public void Register(TypeAdapterConfig config)
            {
                config.NewConfig<StockLocation, SelectItem>()
                    .Map(member: dest => dest.Id, source: src => src.Id)
                    .Map(member: dest => dest.Name, source: src => src.Name)
                    .Map(member: dest => dest.Presentation, source: src => src.Presentation)
                    .Map(member: dest => dest.Active, source: src => src.Active)
                    .Map(member: dest => dest.Default, source: src => src.Default);

                config.NewConfig<StockLocation, ListItem>()
                    .Map(member: dest => dest.CountryName, source: src => src.Country!.Name)
                    .Map(member: dest => dest.StateName, source: src => src.State!.Name)
                    .Map(member: dest => dest.StockItemCount, source: src => src.StockItems.Count);

                config.NewConfig<StockLocation, Detail>()
                    .Inherits<StockLocation, ListItem>();
            }
        }
    }
}
