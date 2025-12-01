using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Options;

namespace ReSys.Core.Feature.Common.Systems.Options;
/// <summary>
/// Base class for shared system configuration options.
/// </summary>
public abstract class SystemOptionBase
{
    [Required, MinLength(length: 3)]
    public string SystemName { get; set; } = string.Empty;

    [Required, Url]
    public string BaseUrl { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string SupportEmail { get; set; } = string.Empty;

    [Required]
    public string DefaultPage { get; set; } = string.Empty;
}

/// <summary>
/// Generic validator for <see cref="SystemOptionBase"/> implementations.
/// </summary>
public sealed class SystemOptionValidator<TOption> : IValidateOptions<TOption>
    where TOption : SystemOptionBase
{
    public ValidateOptionsResult Validate(string? name, TOption? options)
    {
        if (options is null)
            return ValidateOptionsResult.Fail(failureMessage: "Options instance cannot be null.");

        List<ValidationResult> results = [];
        ValidationContext context = new(instance: options);

        if (!Validator.TryValidateObject(instance: options,
                validationContext: context,
                validationResults: results,
                validateAllProperties: true))
        {
            string[] errors = results.Select(selector: r => r.ErrorMessage ?? "Unknown validation error").ToArray();
            return ValidateOptionsResult.Fail(failures: errors);
        }

        return ValidateOptionsResult.Success;
    }
}