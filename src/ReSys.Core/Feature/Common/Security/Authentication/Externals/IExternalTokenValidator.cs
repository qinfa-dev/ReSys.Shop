using ErrorOr;

namespace ReSys.Core.Feature.Common.Security.Authentication.Externals;
public interface IExternalTokenValidator
{
    Task<ErrorOr<ExternalUserInfo>> ValidateTokenAsync(
        string provider,
        string? accessToken,
        string? idToken,
        string? authorizationCode,
        string? redirectUri,
        CancellationToken cancellationToken = default
    );
}