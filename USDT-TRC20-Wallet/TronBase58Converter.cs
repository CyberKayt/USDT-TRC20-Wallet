using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace USDT_TRC20Transfer
{
    /// <summary>
    /// Utility class for converting between TRON Base58Check addresses and hexadecimal format
    /// </summary>
    public static class TronBase58Converter
    {
        // Base58 alphabet used by Bitcoin and TRON
        private static readonly string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        
        /// <summary>
        /// Converts a Base58Check encoded TRON address to hexadecimal string
        /// </summary>
        /// <param name="base58String">Base58Check encoded TRON address (starting with 'T')</param>
        /// <returns>Hexadecimal representation including 41 prefix</returns>
        public static string Base58CheckToHex(string base58String)
        {
            if (string.IsNullOrEmpty(base58String))
                throw new ArgumentException("Input string cannot be null or empty");

            if (!base58String.StartsWith("T"))
                throw new ArgumentException("TRON address must start with 'T'");
                
            // Decode Base58
            byte[] decoded = DecodeBase58(base58String);
            
            if (decoded.Length < 4)
                throw new ArgumentException("Invalid Base58Check string - too short");

            // Split payload and checksum
            byte[] payload = decoded.Take(decoded.Length - 4).ToArray();
            byte[] checksum = decoded.Skip(decoded.Length - 4).ToArray();
            
            // Validate that the first byte is 0x41 (TRON network prefix)
            if (payload.Length < 1 || payload[0] != 0x41)
                throw new ArgumentException($"Invalid TRON address - incorrect network prefix: 0x{payload[0]:X2}");
                
            // Verify checksum
            byte[] hash = DoubleSha256(payload);
            byte[] expectedChecksum = hash.Take(4).ToArray();
            
            if (!checksum.SequenceEqual(expectedChecksum))
                throw new ArgumentException("Invalid Base58Check string - checksum mismatch");
            
            // Convert to hex
            return BitConverter.ToString(payload).Replace("-", "").ToLower();
        }
        
        /// <summary>
        /// Converts a hexadecimal string to Base58Check TRON address
        /// </summary>
        /// <param name="hex">Hexadecimal string (with or without '41' prefix)</param>
        /// <returns>Base58Check encoded TRON address (starting with 'T')</returns>
        public static string HexToBase58Check(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                throw new ArgumentException("Hex string cannot be null or empty");
            
            // Remove 0x prefix if present
            if (hex.StartsWith("0x"))
                hex = hex.Substring(2);
            
            // Add 41 prefix if missing
            if (!hex.StartsWith("41"))
                hex = "41" + hex;
            
            // Validate hex string
            if (!IsValidHexString(hex))
                throw new ArgumentException("Invalid hex string format");
            
            // Convert hex to bytes
            byte[] payload = new byte[hex.Length / 2];
            for (int i = 0; i < payload.Length; i++)
            {
                string byteValue = hex.Substring(i * 2, 2);
                payload[i] = Convert.ToByte(byteValue, 16);
            }
            
            // Calculate checksum
            byte[] hash = DoubleSha256(payload);
            byte[] checksum = hash.Take(4).ToArray();
            
            // Combine payload and checksum
            byte[] combined = new byte[payload.Length + 4];
            Array.Copy(payload, 0, combined, 0, payload.Length);
            Array.Copy(checksum, 0, combined, payload.Length, 4);
            
            // Encode with Base58
            return EncodeBase58(combined);
        }
        
        /// <summary>
        /// Validates a TRON address format and checksum
        /// </summary>
        /// <param name="tronAddress">TRON address in Base58Check format</param>
        /// <returns>True if address is valid</returns>
        public static bool ValidateTronAddress(string tronAddress)
        {
            if (string.IsNullOrEmpty(tronAddress))
                return false;

            if (!tronAddress.StartsWith("T") || tronAddress.Length != 34)
                return false;
                
            try
            {
                // Decode Base58
                byte[] decoded = DecodeBase58(tronAddress);
                
                if (decoded.Length != 25) // 21 bytes address + 4 bytes checksum
                    return false;
                
                // Validate network prefix
                if (decoded[0] != 0x41)
                    return false;
                    
                // Extract address and checksum
                byte[] addressPortion = new byte[21];
                byte[] checksumPortion = new byte[4];
                
                Buffer.BlockCopy(decoded, 0, addressPortion, 0, 21);
                Buffer.BlockCopy(decoded, 21, checksumPortion, 0, 4);
                
                // Verify checksum
                byte[] expectedChecksum = DoubleSha256(addressPortion).Take(4).ToArray();
                return checksumPortion.SequenceEqual(expectedChecksum);
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Extracts the 20-byte address portion (without the 41 prefix) from a TRON address
        /// </summary>
        /// <param name="tronAddress">TRON address in Base58Check format</param>
        /// <returns>20-byte address as hex string without prefix</returns>
        public static string ExtractAddressBytesFromBase58(string tronAddress)
        {
            if (!ValidateTronAddress(tronAddress))
                throw new ArgumentException("Invalid TRON address");
                
            // Decode the Base58 address
            byte[] decoded = DecodeBase58(tronAddress);
            
            // Extract only the 20-byte address part (skip the prefix byte)
            byte[] addressOnly = new byte[20];
            Buffer.BlockCopy(decoded, 1, addressOnly, 0, 20);
            
            // Return as hex
            return BitConverter.ToString(addressOnly).Replace("-", "").ToLower();
        }
        
        /// <summary>
        /// Creates a TRON address from a 20-byte ETH-style address
        /// </summary>
        /// <param name="ethAddress">ETH address (20 bytes)</param>
        /// <returns>TRON address in Base58Check format</returns>
        public static string CreateTronAddressFromEthAddress(string ethAddress)
        {
            if (string.IsNullOrEmpty(ethAddress))
                throw new ArgumentException("ETH address cannot be null or empty");
                
            // Remove 0x prefix if present
            if (ethAddress.StartsWith("0x"))
                ethAddress = ethAddress.Substring(2);
                
            // Validate length
            if (ethAddress.Length != 40)
                throw new ArgumentException("ETH address must be exactly 40 hex characters (20 bytes)");
                
            // Add TRON prefix
            string tronHex = "41" + ethAddress;
            
            // Convert to Base58Check
            return HexToBase58Check(tronHex);
        }
        
        /// <summary>
        /// Performs double SHA256 hash
        /// </summary>
        private static byte[] DoubleSha256(byte[] data)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash1 = sha256.ComputeHash(data);
                byte[] hash2 = sha256.ComputeHash(hash1);
                return hash2;
            }
        }
        
        /// <summary>
        /// Decodes a Base58 string to a byte array
        /// </summary>
        private static byte[] DecodeBase58(string base58)
        {
            if (string.IsNullOrEmpty(base58))
                return Array.Empty<byte>();
                
            // Count leading '1' characters
            int leadingOnes = base58.TakeWhile(c => c == '1').Count();
            
            // Convert the Base58 string to a base-10 BigInteger
            BigInteger value = 0;
            for (int i = 0; i < base58.Length; i++)
            {
                int digit = Base58Alphabet.IndexOf(base58[i]);
                
                if (digit < 0)
                    throw new ArgumentException($"Invalid Base58 character: {base58[i]}");
                    
                value = value * 58 + digit;
            }
            
            // Convert the BigInteger to byte array
            byte[] bytes;
            
            if (value == 0)
                bytes = Array.Empty<byte>();
            else
            {
                bytes = value.ToByteArray();
                // Reverse the array (BigInteger is little-endian, we need big-endian)
                Array.Reverse(bytes);
                
                // Remove sign byte if present
                if (bytes.Length > 1 && bytes[0] == 0 && (bytes[1] & 0x80) != 0)
                    bytes = bytes.Skip(1).ToArray();
            }
            
            // Add leading zeros (each leading '1' in Base58 represents a 0x00 byte)
            byte[] result = new byte[leadingOnes + bytes.Length];
            Array.Copy(bytes, 0, result, leadingOnes, bytes.Length);
            
            return result;
        }
        
        /// <summary>
        /// Encodes a byte array to Base58 string
        /// </summary>
        private static string EncodeBase58(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;
                
            // Count leading zero bytes
            int zeroCount = data.TakeWhile(b => b == 0).Count();
            
            // Convert bytes to BigInteger (big endian)
            BigInteger value = 0;
            for (int i = zeroCount; i < data.Length; i++)
            {
                value = value * 256 + data[i];
            }
            
            // Convert BigInteger to Base58 characters
            StringBuilder result = new StringBuilder();
            
            while (value > 0)
            {
                value = BigInteger.DivRem(value, 58, out BigInteger remainder);
                result.Insert(0, Base58Alphabet[(int)remainder]);
            }
            
            // Add leading '1' characters for each leading zero byte
            result.Insert(0, new string('1', zeroCount));
            
            return result.ToString();
        }
        
        /// <summary>
        /// Validates if a string contains only valid hexadecimal characters
        /// </summary>
        private static bool IsValidHexString(string hexString)
        {
            if (string.IsNullOrEmpty(hexString))
                return false;
                
            return Regex.IsMatch(hexString, "^[0-9a-fA-F]+$");
        }
    }
}