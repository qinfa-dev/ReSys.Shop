using MediatR;

namespace ReSys.Core.Feature.Common.Security.Authentication
{
    public sealed record UserLoggedInNotification(string UserId, string? AdhocId) : INotification;
}
