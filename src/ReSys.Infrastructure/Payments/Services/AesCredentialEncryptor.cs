using System.Security.Cryptography;
using System.Text;

using ReSys.Core.Feature.Common.Payments.Interfaces.Encryptor;

namespace ReSys.Infrastructure.Payments.Services;

/// <summary>
/// Implements IPaymentCredentialEncryptor using AES-256 encryption.
/// </summary>
public class AesCredentialEncryptor : IPaymentCredentialEncryptor
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    /// <summary>
    /// Initializes a new instance of the <see cref="AesCredentialEncryptor"/> class.
    /// In a real application, the key and IV would be securely loaded from configuration
    /// or a key management system. For this example, hardcoded values are used.
    /// </summary>
    public AesCredentialEncryptor()
    {
        // In a real application, load these securely
        // Example: Environment variable, Azure Key Vault, AWS KMS
        // DO NOT hardcode in production
        _key = Encoding.UTF8.GetBytes("ThisIsAStrongSecretKeyForAes256Encryption!"); // 32 bytes for AES-256
        _iv = Encoding.UTF8.GetBytes("ThisIsAStrongIv!"); // 16 bytes for AES

        if (_key.Length != 32) throw new ArgumentException("AES Key must be 32 bytes (256 bits).");
        if (_iv.Length != 16) throw new ArgumentException("AES IV must be 16 bytes (128 bits).");
    }

    /// <summary>
    /// Encrypts a plain text credential string using AES-256.
    /// </summary>
    /// <param name="plainText">The credential to encrypt.</param>
    /// <returns>Encrypted credential string (base64 encoded).</returns>
    public string Encrypt(string plainText)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = _key;
        aesAlg.IV = _iv;

        var encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using (var swEncrypt = new StreamWriter(csEncrypt))
        {
            swEncrypt.Write(plainText);
        }
        return Convert.ToBase64String(msEncrypt.ToArray());
    }

    /// <summary>
    /// Decrypts an encrypted credential string using AES-256.
    /// </summary>
    /// <param name="cipherText">The encrypted credential string (base64 encoded).</param>
    /// <returns>The plain text credential.</returns>
    /// <exception cref="InvalidOperationException">If decryption fails.</exception>
    public string Decrypt(string cipherText)
    {
        using var aesAlg = Aes.Create();
        aesAlg.Key = _key;
        aesAlg.IV = _iv;

        var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        using var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);
        return srDecrypt.ReadToEnd();
    }
}
