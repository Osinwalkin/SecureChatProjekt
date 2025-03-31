using System.Security.Cryptography;
using System.Text;
using System;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace SecureChatProjekt.Client
 
{

    // Helper class to map the JSON object returned by JS for RSA key pairs
    public class RsaKeyPairJwkDto
    {
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
    }

    public class CryptographyService
    {

        private readonly IJSRuntime _jsRuntime;

        public CryptographyService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        }

        // RSA Key Management (Uses JS Interop)
        public async Task<(string PublicKeyJwk, string PrivateKeyJwk)> GenerateRsaKeyPairAsync()
        {
            try
            {
                var keyPairDto = await _jsRuntime.InvokeAsync<RsaKeyPairJwkDto>("cryptoUtils.generateRsaKeyPair");
                if (keyPairDto == null || string.IsNullOrEmpty(keyPairDto.PublicKey) || string.IsNullOrEmpty(keyPairDto.PrivateKey))
                {
                    throw new InvalidOperationException("Received invalid key pair DTO from JavaScript.");
                }
                return (keyPairDto.PublicKey, keyPairDto.PrivateKey);
            }
            catch (JSException ex)
            {
                Console.WriteLine($"JS Interop Error (GenerateRsaKeyPairAsync): {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error (GenerateRsaKeyPairAsync): {ex.Message}");
                throw;
            }
        }

        // RSA Encryption (Uses JS Interop)
        public async Task<byte[]> EncryptWithRsaPublicKeyAsync(byte[] dataToEncrypt, string publicKeyJwk)
        {
            if (dataToEncrypt == null) throw new ArgumentNullException(nameof(dataToEncrypt));
            if (string.IsNullOrEmpty(publicKeyJwk)) throw new ArgumentNullException(nameof(publicKeyJwk));

            try
            {
                return await _jsRuntime.InvokeAsync<byte[]>("cryptoUtils.encryptRsa", dataToEncrypt, publicKeyJwk);
            }
            catch (JSException ex)
            {
                Console.WriteLine($"JS Interop Error (EncryptWithRsaPublicKeyAsync): {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error (EncryptWithRsaPublicKeyAsync): {ex.Message}");
                throw;
            }
        }

        // --- RSA Decryption (Uses JS Interop) ---
        public async Task<byte[]> DecryptWithRsaPrivateKeyAsync(byte[] dataToDecrypt, string privateKeyJwk)
        {
            if (dataToDecrypt == null) throw new ArgumentNullException(nameof(dataToDecrypt));
            if (string.IsNullOrEmpty(privateKeyJwk)) throw new ArgumentNullException(nameof(privateKeyJwk));

            try
            {
                return await _jsRuntime.InvokeAsync<byte[]>("cryptoUtils.decryptRsa", dataToDecrypt, privateKeyJwk);
            }
            catch (JSException ex)
            {
                Console.WriteLine($"JS Interop Error (DecryptWithRsaPrivateKeyAsync - possible wrong key/corrupt data): {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error (DecryptWithRsaPrivateKeyAsync): {ex.Message}");
                throw;
            }
        }

        // --- AES-GCM Encryption (Uses JS Interop) ---
        public async Task<byte[]> EncryptWithAesGcmAsync(byte[] dataToEncrypt, byte[] key, byte[] iv)
        {
            if (dataToEncrypt == null) throw new ArgumentNullException(nameof(dataToEncrypt));
            if (key == null || key.Length != 32) throw new ArgumentException("Key must be 256 bits (32 bytes).", nameof(key));
            if (iv == null || iv.Length != 12) throw new ArgumentException("IV must be 96 bits (12 bytes) for AES-GCM.", nameof(iv));

            try
            {
                // Call the JS function, passing byte arrays directly
                return await _jsRuntime.InvokeAsync<byte[]>("cryptoUtils.encryptAesGcm", dataToEncrypt, key, iv);
            }
            catch (JSException ex)
            {
                Console.WriteLine($"JS Interop Error (EncryptWithAesGcmAsync): {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error (EncryptWithAesGcmAsync): {ex.Message}");
                throw;
            }
        }

        // AES-GCM Decryption (Uses JS Interop)
        public async Task<byte[]> DecryptWithAesGcmAsync(byte[] dataToDecrypt, byte[] key, byte[] iv)
        {

            if (dataToDecrypt == null) throw new ArgumentNullException(nameof(dataToDecrypt));
            if (key == null || key.Length != 32) throw new ArgumentException("Key must be 256 bits (32 bytes).", nameof(key));
            if (iv == null || iv.Length != 12) throw new ArgumentException("IV must be 96 bits (12 bytes) for AES-GCM.", nameof(iv));

            try
            {

                return await _jsRuntime.InvokeAsync<byte[]>("cryptoUtils.decryptAesGcm", dataToDecrypt, key, iv);
            }
            catch (JSException ex) // Catch errors from JS, likely integrity failure
            {
                Console.WriteLine($"JS Interop Error (DecryptWithAesGcmAsync - INTEGRITY CHECK FAILED or wrong key/IV?): {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected Error (DecryptWithAesGcmAsync): {ex.Message}");
                throw;
            }
        }

        // Helpers (Keep these C# implementations)
        public byte[] GetBytes(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        public string GetString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        public string ToBase64(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        public byte[] FromBase64(string base64String)
        {
            return Convert.FromBase64String(base64String);
        }
    }
}
