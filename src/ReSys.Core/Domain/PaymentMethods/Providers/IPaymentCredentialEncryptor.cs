namespace ReSys.Core.Domain.PaymentMethods.Providers;

/// <summary>
/// Service for encrypting and decrypting payment provider credentials.
/// 
/// <para>
/// <strong>Purpose:</strong>
/// Ensures sensitive payment gateway credentials (API keys, secrets, webhooks) are never
/// stored or transmitted in plain text. Provides centralized encryption/decryption for
/// credential management across all payment providers.
/// </para>
/// 
/// <para>
/// <strong>Security Principles:</strong>
/// <list type="bullet">
/// <item><description>All provider credentials must be encrypted before storage</description></item>
/// <item><description>Credentials are only decrypted when needed for payment operations</description></item>
/// <item><description>Implementation must use strong encryption (AES-256 or better)</description></item>
/// <item><description>Encryption keys must be stored separately from encrypted data</description></item>
/// <item><description>Each environment (dev, staging, prod) should have unique keys</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <strong>Usage Pattern:</strong>
/// <code>
/// // When storing credentials (at PaymentMethod creation/update):
/// var plainCredentials = "sk_live_123456789";
/// var encrypted = encryptor.Encrypt(plainCredentials);
/// paymentMethod.PrivateMetadata["api_key"] = encrypted;
/// 
/// // When processing payments (provider initialization):
/// var encrypted = (string)paymentMethod.PrivateMetadata["api_key"];
/// var plainCredentials = encryptor.Decrypt(encrypted);
/// provider.Initialize(paymentMethod, encryptor); // Provider handles decryption
/// </code>
/// </para>
/// 
/// <para>
/// <strong>Implementation Note:</strong>
/// This interface should be implemented in ReSys.Infrastructure with platform-specific
/// encryption mechanisms (e.g., Azure Key Vault, AWS KMS, or Data Protection API).
/// </para>
/// </summary>
public interface IPaymentCredentialEncryptor
{
    /// <summary>
    /// Encrypts a plain text credential string.
    /// 
    /// <para>
    /// <strong>Input Security:</strong>
    /// Plain text credentials should be held in memory only as long as necessary.
    /// After calling Encrypt, caller should clear the original string from memory if possible.
    /// </para>
    /// 
    /// <para>
    /// <strong>Output Format:</strong>
    /// The returned encrypted string can be stored safely in database, logs, and files.
    /// It can only be decrypted with the corresponding Decrypt() call.
    /// </para>
    /// 
    /// <para>
    /// <strong>Determinism:</strong>
    /// Implementations may use randomization (IV, salt) which means encrypting the same
    /// plain text multiple times produces different outputs. This is the secure default.
    /// If deterministic encryption is needed, document this explicitly.
    /// </para>
    /// </summary>
    /// <param name="plainText">The credential to encrypt (API key, secret, token, etc.).</param>
    /// <returns>
    /// Encrypted credential string safe for storage in database.
    /// Format depends on implementation (base64-encoded, hex, etc.).
    /// </returns>
    /// <example>
    /// <code>
    /// var apiKey = "sk_live_abc123def456"; // From payment provider dashboard
    /// var encrypted = encryptor.Encrypt(apiKey);
    /// // encrypted might look like: "AQAAAEk2B3+lSYX3kLxJ5rVg..."
    /// </code>
    /// </example>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts an encrypted credential string.
    /// 
    /// <para>
    /// <strong>Output Security:</strong>
    /// The returned plain text credential is sensitive and should be:
    /// <list type="bullet">
    /// <item><description>Used immediately for payment operations</description></item>
    /// <item><description>Never logged or stored</description></item>
    /// <item><description>Cleared from memory when no longer needed</description></item>
    /// <item><description>Never transmitted over unencrypted channels</description></item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <strong>Error Handling:</strong>
    /// Should throw exception if decryption fails (wrong key, corrupted data, etc.).
    /// Caller should catch and handle cryptographic failures appropriately.
    /// </para>
    /// </summary>
    /// <param name="cipherText">The encrypted credential string (from database storage).</param>
    /// <returns>
    /// The plain text credential that can be used for payment operations.
    /// Must be treated as sensitive data.
    /// </returns>
    /// <exception cref="InvalidOperationException">If decryption fails (wrong key, corrupted data, etc.).</exception>
    /// <example>
    /// <code>
    /// var encrypted = paymentMethod.PrivateMetadata["api_key"] as string;
    /// var plainApiKey = encryptor.Decrypt(encrypted);
    /// 
    /// // Use plainApiKey immediately with provider...
    /// stripe.SetApiKey(plainApiKey);
    /// </code>
    /// </example>
    string Decrypt(string cipherText);
}
