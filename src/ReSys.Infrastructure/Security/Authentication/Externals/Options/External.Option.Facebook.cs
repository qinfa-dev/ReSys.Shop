using Microsoft.Extensions.Options;

namespace ReSys.Infrastructure.Security.Authentication.Externals.Options;

public sealed class FacebookOption : IValidateOptions<FacebookOption>
{
    public const string Section = "Authentication:Facebook";

    public string AppId { get; init; } = string.Empty;
    public string AppSecret { get; init; } = string.Empty;

    public ValidateOptionsResult Validate(string? name, FacebookOption options)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(value: options.AppId))
        {
            errors.Add(item: "AppId is required.");
        }

        if (string.IsNullOrWhiteSpace(value: options.AppSecret))
        {
            errors.Add(item: "AppSecret is required.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures: errors);
    }
}