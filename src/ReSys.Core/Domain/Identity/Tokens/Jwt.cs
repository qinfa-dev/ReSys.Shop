using ErrorOr;

using ReSys.Core.Common.Constants;

namespace ReSys.Core.Domain.Identity.Tokens;

/// <summary>
/// Represents a JSON Web Token (JWT) entity for user authentication.
/// Manages token generation, validation, and claim extraction.
/// </summary>
public sealed class Jwt
{
    #region Constraints

    public static class Constraints
    {
        public const int TokenParts = 3;
        public const int MinSecretBytes = 32; // 256 bits for HMAC-SHA256
        public const int RecommendedSecretBytes = 64; // 512 bits recommended
        public const int MaxHeaderLength = CommonInput.Constraints.Text.LongTextMaxLength;
        public const int MaxPayloadLength = CommonInput.Constraints.Text.LongTextMaxLength;
        public const int MaxTokenLength = CommonInput.Constraints.Text.LongTextMaxLength;
        public const string TokenPattern = @"^([A-Za-z0-9-_]+\.){2}[A-Za-z0-9-_]+$"; // Base64url header.payload.signature
    }

    #endregion

    #region Errors

    public static class Errors
    {
        public static Error EmptyPayload => Error.Validation(code: "Jwt.EmptyPayload",
            description: "JWT payload is empty");
        public static Error InvalidSignature =>
            Error.Validation(code: "Jwt.InvalidSignature",
                description: "Token signature is invalid");
        public static Error MissingAlgorithm => Error.Validation(code: "Jwt.MissingAlgorithm",
            description: "JWT header missing algorithm");
        public static Error InvalidFormat => Error.Validation(code: "Jwt.InvalidFormat",
            description: "Invalid token format");
        public static Error NoExpiration => Error.Validation(code: "Jwt.NoExpiration",
            description: "Token does not have an expiration claim");
        public static Error ValidationFailed => Error.Validation(code: "Jwt.ValidationFailed",
            description: "Token validation failed");
        public static Error ParseFailed => Error.Failure(code: "Jwt.ParseFailed",
            description: "Failed to parse token");
        public static Error GenerationFailed => Error.Failure(code: "Jwt.GenerationFailed",
            description: "Failed to generate JWT token");
        public static Error SecurityTokenError => Error.Failure(code: "Jwt.SecurityTokenError",
            description: "Security token error");
        public static Error InvalidUser => Error.Validation(code: "Jwt.InvalidUser",
            description: "Valid user is required");
        public static Error PrincipalExtraction => Error.Failure(code: "Jwt.PrincipalExtraction",
            description: "Failed to extract principal from token");
        public static Error ClaimsExtraction => Error.Failure(code: "Jwt.ClaimsExtraction",
            description: "Failed to extract claims from token");
    }
    #endregion
}