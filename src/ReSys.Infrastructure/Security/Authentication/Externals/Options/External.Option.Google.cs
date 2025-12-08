using Microsoft.Extensions.Options;

namespace ReSys.Infrastructure.Security.Authentication.Externals.Options;

public sealed class GoogleOption : IValidateOptions<GoogleOption>
{
    public const string Section = "Authentication:Google";

    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;

    public ValidateOptionsResult Validate(string? name, GoogleOption options)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(value: options.ClientId))
        {
            errors.Add(item: "ClientId is required.");
        }

        if (string.IsNullOrWhiteSpace(value: options.ClientSecret))
        {
            errors.Add(item: "ClientSecret is required.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures: errors);
    }
}