using ErrorOr;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ReSys.Core.Feature.Common.Security.Authentication.Externals.Interfaces;
using ReSys.Core.Feature.Common.Security.Authentication.Externals.Models;

namespace ReSys.Infrastructure.Security.Authentication.Externals.Services;

public sealed class CompositeExternalTokenValidator(
    IServiceProvider serviceProvider,
    ILogger<CompositeExternalTokenValidator> logger)
    : IExternalTokenValidator
{
    public async Task<ErrorOr<ExternalUserTransfer>> ValidateTokenAsync(
        string provider,
        string? accessToken,
        string? idToken,
        string? authorizationCode,
        string? redirectUri,
        CancellationToken cancellationToken = default)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(value: provider))
        {
            return Error.Validation(code: "Provider.Required",
                description: "Provider is required");
        }

        string normalizedProvider = provider.ToLowerInvariant();

        IExternalTokenValidator? validator = normalizedProvider switch
        {
            "google" => serviceProvider.GetService<GoogleTokenValidator>(),
            "facebook" => serviceProvider.GetService<FacebookTokenValidator>(),
            _ => null
        };

        if (validator == null)
        {
            logger.LogWarning(message: "No validator found for provider: {Provider}",
                args: provider);
            return Error.NotFound(code: "Provider.ValidatorNotFound",
                description: $"No validator configured for provider '{provider}'. Supported providers: google, facebook");
        }

        try
        {
            logger.LogDebug(message: "Validating token for provider: {Provider}",
                args: provider);
            ErrorOr<ExternalUserTransfer> result = await validator.ValidateTokenAsync(provider: provider,
                accessToken: accessToken,
                idToken: idToken,
                authorizationCode: authorizationCode,
                redirectUri: redirectUri,
                cancellationToken: cancellationToken);

            if (result.IsError)
            {
                logger.LogWarning(message: "Token validation failed for provider {Provider}: {Errors}",
                    args:
                    [
                        provider,
                        string.Join(separator: ", ",
                            values: result.Errors.Select(selector: e => e.Description))
                    ]);
            }
            else
            {
                logger.LogDebug(message: "Token validation successful for provider {Provider}, user: {Email}",
                    args:
                    [
                        provider,
                        result.Value.Email
                    ]);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning(message: "Token validation cancelled for provider {Provider}",
                args: provider);
            return Error.Failure(code: "Token.ValidationCancelled",
                description: "Token validation was cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(exception: ex,
                message: "External token validation failed for provider {Provider}",
                args: provider);
            return Error.Failure(code: "Token.ValidationError",
                description: $"Token validation failed for provider '{provider}': {ex.Message}");
        }
    }
}