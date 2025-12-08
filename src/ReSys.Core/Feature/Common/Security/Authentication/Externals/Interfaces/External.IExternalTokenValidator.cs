using ErrorOr;

using ReSys.Core.Feature.Common.Security.Authentication.Externals.Models;

namespace ReSys.Core.Feature.Common.Security.Authentication.Externals.Interfaces;
public interface IExternalTokenValidator
{
    Task<ErrorOr<ExternalUserTransfer>> ValidateTokenAsync(
        string provider,
        string? accessToken,
        string? idToken,
        string? authorizationCode,
        string? redirectUri,
        CancellationToken cancellationToken = default
    );
}