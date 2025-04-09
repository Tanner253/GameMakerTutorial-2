using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine; // Required for Debug.Log

// Provides methods for encrypting and decrypting data using AES.
public static class EncryptionUtility
{
    // IMPORTANT: Generate secure random keys using a separate tool/script
    // and paste the Base64 encoded strings here.
    private const string KeyBase64 = "MqYj5ntIQxHSVZFIu0CSJY8z2sYHgEqRHMCB+valvZQ=";
    private const string IVBase64 = "OupgECIG7wJCOZCHtwkaXA==";

    private static byte[] _key;
    private static byte[] _iv;

    // Static constructor to decode Base64 strings once
    static EncryptionUtility()
    {
        try
        {
            _key = Convert.FromBase64String(KeyBase64);
            _iv = Convert.FromBase64String(IVBase64);

            // Validate lengths after decoding
            if (_key.Length != 32)
            {
                Debug.LogError($"Encryption Setup Error: Decoded key length is {_key.Length} bytes, but must be 32.");
                _key = null; // Invalidate key if wrong length
            }
            if (_iv.Length != 16)
            {
                Debug.LogError($"Encryption Setup Error: Decoded IV length is {_iv.Length} bytes, but must be 16.");
                _iv = null; // Invalidate IV if wrong length
            }
        }
        catch (FormatException ex)
        {
            Debug.LogError($"Encryption Setup Error: Could not decode Base64 key/IV. Ensure you pasted the correct Base64 strings. Error: {ex.Message}");
            _key = null;
            _iv = null;
        }
    }

    // Encrypts a plain text string and returns the encrypted data as a byte array.
    public static byte[] Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            Debug.LogError("Encryption Error: Plain text is null or empty.");
            return null;
        }
        // Use the decoded key/iv bytes
        if (_key == null || _iv == null)
        {
            Debug.LogError("Encryption Error: Key or IV is invalid due setup error. Cannot encrypt.");
            return null;
        }

        byte[] encrypted;
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = _key;
            aesAlg.IV = _iv;
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Padding = PaddingMode.PKCS7;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(plainText);
                    }
                    encrypted = msEncrypt.ToArray();
                }
            }
        }
        return encrypted;
    }

    // Decrypts a byte array (encrypted data) and returns the original plain text string.
    public static string Decrypt(byte[] cipherText)
    {
        if (cipherText == null || cipherText.Length <= 0)
        {
             Debug.LogError("Decryption Error: Cipher text is null or empty.");
            return null;
        }
        // Use the decoded key/iv bytes
        if (_key == null || _iv == null)
        {
            Debug.LogError("Decryption Error: Key or IV is invalid due setup error. Cannot decrypt.");
            return null;
        }

        string plaintext = null;
        try
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = _key;
                aesAlg.IV = _iv;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
        catch (CryptographicException cryptoEx)
        {
            Debug.LogError($"Decryption Error (CryptographicException): {cryptoEx.Message}. Data might be corrupt, tampered with, or key/IV mismatch.");
            plaintext = null;
        }
        catch (Exception ex)
        {
             Debug.LogError($"Decryption Error (General Exception): {ex.Message}");
             plaintext = null;
        }
        return plaintext;
    }
} 