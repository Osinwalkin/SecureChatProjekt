const rsaKeyGenParams = {
    name: "RSA-OAEP",
    modulusLength: 2048,
    publicExponent: new Uint8Array([0x01, 0x00, 0x01]),
    hash: "SHA-256",
};

const rsaEncryptDecryptParams = { name: "RSA-OAEP" };

// --- AES-GCM Configuration ---
const aesGcmParams = (iv) => ({ name: "AES-GCM", iv: iv });

// --- Global Object ---
window.cryptoUtils = {

    // --- RSA Key Generation ---
    generateRsaKeyPair: async () => {
        try {
            // Generate the key pair using Web Crypto API
            const keyPair = await window.crypto.subtle.generateKey(
                rsaKeyGenParams,
                true,
                ["encrypt", "decrypt"]
            );

            // Export the public key to JWK format
            const publicKeyJwk = await window.crypto.subtle.exportKey("jwk", keyPair.publicKey);
            // Export the private key to JWK format
            const privateKeyJwk = await window.crypto.subtle.exportKey("jwk", keyPair.privateKey);

            // Return keys as JSON strings
            return {
                publicKey: JSON.stringify(publicKeyJwk),
                privateKey: JSON.stringify(privateKeyJwk)
            };
        } catch (error) {
            console.error("JS generateRsaKeyPair error:", error);
            throw error;
        }
    },

    // --- RSA Encryption ---
    encryptRsa: async (dataBytes, publicKeyJwkString) => {
        try {
            // Parse the JWK string back into a JavaScript object
            const publicKeyJwk = JSON.parse(publicKeyJwkString);
            // Import the JWK object into a usable CryptoKey
            const publicKey = await window.crypto.subtle.importKey(
                "jwk",               // Format of the key data
                publicKeyJwk,        // The JWK object
                rsaKeyGenParams,     // Algorithm parameters (must match generation)
                true,                // Key is extractable (not strictly needed for encrypt)
                ["encrypt"]          // Specify allowed usage
            );

            // Encrypt the data using the imported public key
            const encryptedBuffer = await window.crypto.subtle.encrypt(
                rsaEncryptDecryptParams, // Algorithm parameters
                publicKey,               // The CryptoKey
                dataBytes                // Data to encrypt (C# byte[] comes as Uint8Array)
            );

            // Return the result as a Uint8Array
            return new Uint8Array(encryptedBuffer);
        } catch (error) {
            console.error("JS encryptRsa error:", error);
            throw error;
        }
    },

    // --- RSA Decryption ---
    decryptRsa: async (encryptedBytes, privateKeyJwkString) => {
        try {
            // Parse the JWK string
            const privateKeyJwk = JSON.parse(privateKeyJwkString);
            // Import the private key JWK
            const privateKey = await window.crypto.subtle.importKey(
                "jwk",              // Format
                privateKeyJwk,       // JWK object
                rsaKeyGenParams,    // Algorithm parameters
                true,               // Extractable
                ["decrypt"]         // Usage
            );

            // Decrypt the data using the imported private key
            const decryptedBuffer = await window.crypto.subtle.decrypt(
                rsaEncryptDecryptParams, // Algorithm parameters
                privateKey,              // The CryptoKey
                encryptedBytes           // Data to decrypt (C# byte[] comes as Uint8Array)
            );

            // Return the result as a Uint8Array
            return new Uint8Array(decryptedBuffer);
        } catch (error) {
            console.error("JS decryptRsa error:", error);
            if (error.name === 'OperationError') {
                console.error("RSA Decryption failed. Check if the correct key was used or if the ciphertext is corrupted.");
            }
            throw error;
        }
    },

    // --- AES-GCM Encryption ---
    encryptAesGcm: async (dataBytes, keyBytes, ivBytes) => {
        try {
            // Import the raw AES key bytes into a usable CryptoKey
            const key = await window.crypto.subtle.importKey(
                "raw",          // Format of the key material is raw bytes
                keyBytes,       // The key material itself (Uint8Array)
                "AES-GCM",      // Algorithm identifier
                true,           // Key is extractable (can set to false if not needed later)
                ["encrypt"]     // Specify allowed usage
            );

            // Encrypt the data using the imported key and provided IV
            const encryptedBuffer = await window.crypto.subtle.encrypt(
                aesGcmParams(ivBytes), // Algorithm params including the unique IV
                key,                   // The CryptoKey
                dataBytes              // Data to encrypt (Uint8Array)
            );

            return new Uint8Array(encryptedBuffer);
        } catch (error) {
            console.error("JS encryptAesGcm error:", error);
            throw error;
        }
    },

    // --- AES-GCM Decryption ---
    decryptAesGcm: async (ciphertextBytes, keyBytes, ivBytes) => {
        try {
            // Import the raw AES key
            const key = await window.crypto.subtle.importKey(
                "raw", keyBytes, "AES-GCM", true, ["decrypt"]
            );

            // Decrypt the data. Web Crypto API automatically handles integrity verification.
            const decryptedBuffer = await window.crypto.subtle.decrypt(
                aesGcmParams(ivBytes), // Algorithm params including the IV used for encryption
                key,                   // The CryptoKey
                ciphertextBytes        // Ciphertext (+ auth tag) to decrypt
            );

            // Return the original plaintext as Uint8Array
            return new Uint8Array(decryptedBuffer);
        } catch (error) {
            console.error("JS decryptAesGcm error:", error);
            throw error;
        }
    }
};

console.log("cryptoUtils.js loaded");