using System.Text.Json.Serialization;

namespace ReSys.Core.Feature.Common.Security.Authorization.Claims.Models;

public record AuthorizeClaimData(
    [property: JsonPropertyName(name: "user_id")] string UserId,
    [property: JsonPropertyName(name: "user_name")] string UserName,
    [property: JsonPropertyName(name: "email")] string Email,
    [property: JsonPropertyName(name: "permissions")] IReadOnlyList<string> Permissions,
    [property: JsonPropertyName(name: "roles")] IReadOnlyList<string> Roles,
    [property: JsonPropertyName(name: "policies")] IReadOnlyList<string> Policies
);
