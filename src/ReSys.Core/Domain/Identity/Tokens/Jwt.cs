namespace ReSys.Core.Domain.Identity.Tokens;

/// <summary>
/// A static helper class providing constants and error definitions related to JSON Web Tokens (JWTs).
/// This class encapsulates common constraints and error conditions that arise during JWT generation,
/// validation, and processing within the identity and authentication system.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Identity Domain:</strong>
/// This class acts as a central point for defining the structural and validation rules
/// for JWTs, ensuring consistency and proper handling of authentication tokens.
/// </para>
///
/// <para>
/// <strong>Key Areas:</strong>
/// <list type="bullet">
/// <item><term>Token Structure</term><description>Defines expected parts and format.</description></item>
/// <item><term>Security Constraints</term><description>Specifies minimum secret sizes for cryptographic operations.</description></item>
/// <item><term>Validation Errors</term><description>Provides standardized error messages for common JWT-related failures.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class Jwt
{
    #region Constraints

    /// <summary>
    /// Defines constraints and constant values specific to JWT structure and security.
    /// </summary>
    public static class Constraints
    {
        /// <summary>The expected number of parts in a JWT (header, payload, signature).</summary>
        public const int TokenParts = 3;
        /// <summary>Minimum recommended secret size in bytes for HMAC-SHA256 (256 bits).</summary>
        public const int MinSecretBytes = 32;
        /// <summary>Recommended secret size in bytes for enhanced security (512 bits).</summary>
        public const int RecommendedSecretBytes = 64;
        /// <summary>Maximum allowed length for the JWT header.</summary>
        public const int MaxHeaderLength = CommonInput.Constraints.Text.LongTextMaxLength;
        /// <summary>Maximum allowed length for the JWT payload.</summary>
        public const int MaxPayloadLength = CommonInput.Constraints.Text.LongTextMaxLength;
        /// <summary>Maximum allowed total length for a JWT.</summary>
        public const int MaxTokenLength = CommonInput.Constraints.Text.LongTextMaxLength;
        /// <summary>Regex pattern for validating the basic structure of a JWT (Base64url encoded header.payload.signature).</summary>
        public const string TokenPattern = @"^([A-Za-z0-9-_]+\.){2}[A-Za-z0-9-_]+$"; // Base64url header.payload.signature
    }

    #endregion

    #region Errors

    /// <summary>
    /// Defines domain error scenarios specific to JWT operations.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>Error indicating that the JWT payload is empty.</summary>
        public static Error EmptyPayload => Error.Validation(code: "Jwt.EmptyPayload",
            description: "JWT payload is empty");
        /// <summary>Error indicating that the JWT signature is invalid or tampered with.</summary>
        public static Error InvalidSignature =>
            Error.Validation(code: "Jwt.InvalidSignature",
                description: "Token signature is invalid");
        /// <summary>Error indicating that the JWT header is missing the algorithm specification.</summary>
        public static Error MissingAlgorithm => Error.Validation(code: "Jwt.MissingAlgorithm",
            description: "JWT header missing algorithm");
        /// <summary>Error indicating an invalid JWT format (e.g., incorrect number of segments).</summary>
        public static Error InvalidFormat => Error.Validation(code: "Jwt.InvalidFormat",
            description: "Invalid token format");
        /// <summary>Error indicating that the JWT does not contain an expiration claim (exp).</summary>
        public static Error NoExpiration => Error.Validation(code: "Jwt.NoExpiration",
            description: "Token does not have an expiration claim");
        /// <summary>Error indicating a general failure during JWT validation.</summary>
        public static Error ValidationFailed => Error.Validation(code: "Jwt.ValidationFailed",
            description: "Token validation failed");
        /// <summary>Error indicating a failure to parse the JWT string.</summary>
        public static Error ParseFailed => Error.Failure(code: "Jwt.ParseFailed",
            description: "Failed to parse token");
        /// <summary>Error indicating a failure during JWT generation.</summary>
        public static Error GenerationFailed => Error.Failure(code: "Jwt.GenerationFailed",
            description: "Failed to generate JWT token");
        /// <summary>Error indicating a security token-related issue.</summary>
        public static Error SecurityTokenError => Error.Failure(code: "Jwt.SecurityTokenError",
            description: "Security token error");
        /// <summary>Error indicating that a valid user is required for token operations.</summary>
        public static Error InvalidUser => Error.Validation(code: "Jwt.InvalidUser",
            description: "Valid user is required");
        /// <summary>Error indicating a failure to extract the security principal from the token.</summary>
        public static Error PrincipalExtraction => Error.Failure(code: "Jwt.PrincipalExtraction",
            description: "Failed to extract principal from token");
        /// <summary>Error indicating a failure to extract claims from the token.</summary>
        public static Error ClaimsExtraction => Error.Failure(code: "Jwt.ClaimsExtraction",
            description: "Failed to extract claims from token");
    }
    #endregion
}