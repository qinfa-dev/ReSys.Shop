using Mapster;

// Added
using ReSys.Core.Domain.Stores;
// Added

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
{
    public static class Models
    {
        public record Parameter : IHasParameterizableName, IHasMetadata, IHasSeoMetadata
        {
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public string? Code { get; set; }
            public string? Url { get; set; }
            public string? DefaultCurrency { get; set; }
            public string? DefaultLocale { get; set; }
            public string? Timezone { get; set; }
            public bool Default { get; set; }
            public string? MailFromAddress { get; set; }
            public string? CustomerSupportEmail { get; set; }
            public string? MetaTitle { get; set; }
            public string? MetaDescription { get; set; }
            public string? MetaKeywords { get; set; }
            public string? SeoTitle { get; set; }
            public bool? Available { get; set; }
            public bool? GuestCheckoutAllowed { get; set; }
            public IDictionary<string, object?>? PublicMetadata { get; set; }
            public IDictionary<string, object?>? PrivateMetadata { get; set; }
        }

        public sealed class ParameterValidator : AbstractValidator<Parameter>
        {
            public ParameterValidator()
            {
                const string prefix = nameof(Store);

                this.AddParameterizableNameRules(prefix: prefix);
                this.AddMetadataSupportRules(prefix: prefix);
                this.AddSeoMetaSupportRules(prefix: prefix);

                RuleFor(expression: x => x.Name)
                    .NotEmpty()
                    .MaximumLength(Store.Constraints.NameMaxLength);

                RuleFor(expression: x => x.Presentation)
                    .MaximumLength(Store.Constraints.PresentationMaxLength);

                RuleFor(expression: x => x.Code)
                    .NotEmpty()
                    .MaximumLength(Store.Constraints.CodeMaxLength);

                RuleFor(expression: x => x.Url)
                    .NotEmpty()
                    .MaximumLength(Store.Constraints.UrlMaxLength);

                RuleFor(expression: x => x.DefaultCurrency)
                    .Must(x => x is null || Store.Constraints.ValidCurrencies.Contains(x.ToUpperInvariant()))
                    .WithMessage($"Currency must be one of: {string.Join(", ", Store.Constraints.ValidCurrencies)}");
            }
        }

        public record AddressParameter
        {
            public string? Address1 { get; set; }
            public string? Address2 { get; set; }
            public string? City { get; set; }
            public string? Zipcode { get; set; }
            public string? Phone { get; set; }
            public string? Company { get; set; }
            public Guid? CountryId { get; set; }
            public Guid? StateId { get; set; }
        }

        public record SocialLinksParameter
        {
            public string? Facebook { get; set; }
            public string? Instagram { get; set; }
            public string? Twitter { get; set; }
        }

        public record PasswordProtectionParameter
        {
            public string Password { get; set; } = string.Empty;
        }

        public sealed class PasswordProtectionParameterValidator : AbstractValidator<PasswordProtectionParameter>
        {
            public PasswordProtectionParameterValidator()
            {
                RuleFor(expression: x => x.Password)
                    .NotEmpty()
                    .MaximumLength(Store.Constraints.PasswordMaxLength)
                    .WithMessage("Password cannot be null or empty and must not exceed {MaxLength} characters.");
            }
        }

        public record ListItem 
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
     
            public string DefaultCurrency { get; set; } = string.Empty;
     
            public bool Default { get; set; }
     
            public bool Available { get; set; }
     
            public bool IsDeleted { get; set; }
     
            public DateTimeOffset CreatedAt { get; set; }
     
            public DateTimeOffset? UpdatedAt { get; set; }
     
        }
        public record Detail : ListItem
        {
            public string DefaultLocale { get; set; } = string.Empty;
            public string Timezone { get; set; } = string.Empty;
            public string? MailFromAddress { get; set; }
            public string? CustomerSupportEmail { get; set; }
            public string? MetaTitle { get; set; }
            public string? MetaDescription { get; set; }
            public string? MetaKeywords { get; set; }
            public string? SeoTitle { get; set; }
            public bool GuestCheckoutAllowed { get; set; }
            public bool PasswordProtected { get; set; }
            public string? Facebook { get; set; }
            public string? Instagram { get; set; }
            public string? Twitter { get; set; }
            public string? Address1 { get; set; }
            public string? Address2 { get; set; }
            public string? City { get; set; }
            public string? Zipcode { get; set; }
            public string? Phone { get; set; }
            public string? Company { get; set; }
            public Guid? CountryId { get; set; }
            public string? CountryName { get; set; }
            public Guid? StateId { get; set; }
            public string? StateName { get; set; }
            public IDictionary<string, object?>? PublicMetadata { get; set; }
            public IDictionary<string, object?>? PrivateMetadata { get; set; }
            public int ActiveOrderCount { get; set; }
            public int CompletedOrderCount { get; set; }
            public int VisibleProductCount { get; set; }
            public bool IsConfigured { get; set; }
        }



        public sealed class Mapping : IRegister
        {
            public void Register(TypeAdapterConfig config)
            {
                config.NewConfig<Store, ListItem>()
                    .Map(dest => dest.DefaultCurrency, src => src.DefaultCurrency.ToUpperInvariant());

                config.NewConfig<Store, Detail>()
                    .Inherits<Store, ListItem>()
                    .Map(dest => dest.CountryName, src => src.Country!.Name, src => src.Country != null)
                    .Map(dest => dest.StateName, src => src.State!.Name, src => src.State != null);
            }
        }
    }
}