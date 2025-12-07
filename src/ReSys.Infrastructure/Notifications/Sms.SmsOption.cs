using Microsoft.Extensions.Options;

namespace ReSys.Infrastructure.Notifications;

public sealed class SmsOptions : IValidateOptions<SmsOptions>
{
    public const string Section = "Notifications:SmsOptions";
    public bool EnableSmsNotifications { get; init; }
    public string DefaultSenderNumber { get; init; } = null!;

    public SinchConfig SinchConfig { get; init; } = null!;

    public ValidateOptionsResult Validate(string? name, SmsOptions options)
    {
        List<string> errors = [];

        // Validate that EnableSmsNotifications is true if DefaultSenderNumber is set
        if (options.EnableSmsNotifications)
        {
            if (string.IsNullOrEmpty(value: options.DefaultSenderNumber))
            {
                errors.Add(item: "DefaultSenderNumber must be provided when SMS notifications are enabled.");
            }
        }

        // Validate SinchConfig
        if (string.IsNullOrEmpty(value: options.SinchConfig.ProjectId))
        {
            errors.Add(item: "SinchConfig ProjectId is required.");
        }

        if (string.IsNullOrEmpty(value: options.SinchConfig.KeyId))
        {
            errors.Add(item: "SinchConfig KeyId is required.");
        }

        if (string.IsNullOrEmpty(value: options.SinchConfig.KeySecret))
        {
            errors.Add(item: "SinchConfig KeySecret is required.");
        }

        if (string.IsNullOrEmpty(value: options.SinchConfig.SenderPhoneNumber))
        {
            errors.Add(item: "SinchConfig SenderPhoneNumber is required.");
        }

        if (string.IsNullOrEmpty(value: options.SinchConfig.SmsRegion))
        {
            errors.Add(item: "SinchConfig SmsRegion is required.");
        }

        // Return validation result
        if (errors.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures: errors);
        }

        return ValidateOptionsResult.Success;
    }
}
public sealed class SinchConfig
{
    public string ProjectId { get; init; } = null!;
    public string KeyId { get; init; } = null!;
    public string KeySecret { get; init; } = null!;
    public string SenderPhoneNumber { get; init; } = null!;
    public string SmsRegion { get; init; } = null!; // e.g. "Us", "Eu"
}
