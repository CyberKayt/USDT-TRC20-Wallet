using Nethereum.Signer;
using SimpleBase;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Verifies TRON (TRC20) addresses using private keys
/// </summary>
public class TronAddressVerifier
{
    /// <summary>
    /// Generates a TRON address from a private key and verifies if it matches a provided address
    /// </summary>
    /// <param name="privateKeyHex">The private key in hex format (64 characters)</param>
    /// <param name="addressToVerify">Optional address to verify against the generated address</param>
    /// <returns>Tuple containing the private key, public key, generated address, and match result</returns>
    public static (string PrivateKey, string PublicKey, string GeneratedAddress, bool IsMatch)
        GenerateAndVerifyTronAddress(string privateKeyHex, string addressToVerify = "")
    {
        // Private key doğrulama
        if (string.IsNullOrWhiteSpace(privateKeyHex))
            throw new ArgumentException("Private key boş olamaz");

        if (privateKeyHex.Length != 64 || !Regex.IsMatch(privateKeyHex, "^[0-9A-Fa-f]{64}$"))
            throw new ArgumentException("Geçersiz private key formatı. 64 karakter hex olmalıdır.");

        // Public key üretimi
        var ecKey = new EthECKey(privateKeyHex);
        byte[] pubKey = ecKey.GetPubKey();
        string publicKeyHex = BitConverter.ToString(pubKey).Replace("-", "");

        // TRC20 adres üretimi
        string tronAddress = GenerateTronAddress(pubKey);

        // Adres doğrulama
        bool isMatch = false;
        if (!string.IsNullOrWhiteSpace(addressToVerify))
        {
            isMatch = string.Equals(tronAddress, addressToVerify, StringComparison.OrdinalIgnoreCase);
        }

        return (privateKeyHex, publicKeyHex, tronAddress, isMatch);
    }

    /// <summary>
    /// Generates a TRON address from a public key
    /// </summary>
    /// <param name="publicKey">The public key in byte array format</param>
    /// <returns>TRON address in Base58 format</returns>
    private static string GenerateTronAddress(byte[] publicKey)
    {
        // 04 prefix'ini kaldır
        byte[] pubKeyRaw = publicKey.Skip(1).ToArray();

        // Keccak256 hash hesapla
        byte[] hash = CalculateKeccak256(pubKeyRaw);

        // Son 20 byte'ı al
        byte[] addressBytes = hash.Skip(12).ToArray();

        // TRON adres öneki (0x41) ekle
        byte[] tronAddress = new byte[21];
        tronAddress[0] = 0x41;
        Buffer.BlockCopy(addressBytes, 0, tronAddress, 1, 20);

        // Checksum hesapla
        byte[] checksum = CalculateDoubleSha256(tronAddress);
        byte[] checksumBytes = checksum.Take(4).ToArray();

        // Base58 encode
        byte[] finalBytes = new byte[25];
        Buffer.BlockCopy(tronAddress, 0, finalBytes, 0, 21);
        Buffer.BlockCopy(checksumBytes, 0, finalBytes, 21, 4);

        return SimpleBase.Base58.Bitcoin.Encode(finalBytes);
    }

    /// <summary>
    /// Calculates the Keccak-256 hash of the input data
    /// </summary>
    /// <param name="data">Input data to hash</param>
    /// <returns>32-byte Keccak hash</returns>
    private static byte[] CalculateKeccak256(byte[] data)
    {
        var keccak = new Nethereum.Util.Sha3Keccack();
        return keccak.CalculateHash(data);
    }

    /// <summary>
    /// Calculates double SHA-256 hash (applies SHA-256 twice)
    /// </summary>
    /// <param name="data">Input data to hash</param>
    /// <returns>32-byte double SHA-256 hash</returns>
    private static byte[] CalculateDoubleSha256(byte[] data)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] firstHash = sha256.ComputeHash(data);
            return sha256.ComputeHash(firstHash);
        }
    }
}