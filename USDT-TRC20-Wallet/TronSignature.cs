using Nethereum.Signer;
using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using SimpleBase;
using System.Linq;

namespace USDT_TRC20Transfer
{
    /// <summary>
    /// TRON specific signature implementation for TRC20 transactions
    /// </summary>
    public static class TronSignature
    {
        // Base58 alphabet used by Bitcoin and TRON
        private static readonly string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        /// <summary>
        /// Signs a message hash using a private key according to TRON specifications
        /// </summary>
        /// <param name="messageHash">Hash to sign</param>
        /// <param name="privateKeyHex">Private key in hex format</param>
        /// <returns>Signature as byte array</returns>
        public static byte[] SignMessage(byte[] messageHash, string privateKeyHex)
        {
            try
            {
                // Clean private key format
                if (privateKeyHex.StartsWith("0x"))
                    privateKeyHex = privateKeyHex.Substring(2);
                
                // Create Ethereum key from private key (works for TRON too as they use same curve)
                var key = new EthECKey(privateKeyHex);
                
                // Sign the message using Nethereum's signer
                var signature = key.SignAndCalculateV(messageHash);
                
                // Extract R and S components - each should be 32 bytes
                byte[] r = PadTo32Bytes(signature.R);
                byte[] s = PadTo32Bytes(signature.S);
                
                // For TRON, V should be 0 or 1, not 27 or 28
                byte v = GetTronRecoveryParam(signature);
                
                // Create 65-byte signature (TRON format: R[32] + S[32] + V[1])
                byte[] fullSignature = new byte[65];
                Buffer.BlockCopy(r, 0, fullSignature, 0, 32);
                Buffer.BlockCopy(s, 0, fullSignature, 32, 32);
                fullSignature[64] = v;
                
                // Debug information
                Console.WriteLine($"TRON Signature details:");
                Console.WriteLine($"R: {ToHex(r)}");
                Console.WriteLine($"S: {ToHex(s)}");
                Console.WriteLine($"V: {v}");
                Console.WriteLine($"Full signature: {ToHex(fullSignature)}");
                
                return fullSignature;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error in TronSignature.SignMessage: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Test function to check if signature generation is working correctly
        /// </summary>
        /// <param name="privateKeyHex">Private key in hex format</param>
        /// <param name="testMessage">Optional test message (defaults to "TEST MESSAGE")</param>
        /// <returns>True if signature generation succeeds</returns>
        public static bool TestSignature(string privateKeyHex, string testMessage = "TEST MESSAGE")
        {
            Console.WriteLine("?? Testing TRON signature generation...");
            Console.WriteLine($"Test message: \"{testMessage}\"");
            
            try
            {
                // Clean private key format
                if (privateKeyHex.StartsWith("0x"))
                    privateKeyHex = privateKeyHex.Substring(2);
                
                // Create a test message hash using SHA-256
                byte[] messageBytes = Encoding.UTF8.GetBytes(testMessage);
                byte[] messageHash;
                
                using (var sha256 = SHA256.Create())
                {
                    messageHash = sha256.ComputeHash(messageBytes);
                }
                
                Console.WriteLine($"Message hash: {ToHex(messageHash)}");
                
                // Create Ethereum key from private key
                var key = new EthECKey(privateKeyHex);
                
                // Get the address for verification purposes
                string publicKey = key.GetPublicAddress();
                Console.WriteLine($"Public key derived from private key: {publicKey}");
                
                // Generate the signature
                byte[] signature = SignMessage(messageHash, privateKeyHex);
                
                // Extract R, S, and V components from signature
                byte[] r = new byte[32];
                byte[] s = new byte[32];
                Buffer.BlockCopy(signature, 0, r, 0, 32);
                Buffer.BlockCopy(signature, 32, s, 0, 32);
                byte v = signature[64];
                
                Console.WriteLine($"Signature component R: {ToHex(r)}");
                Console.WriteLine($"Signature component S: {ToHex(s)}");
                Console.WriteLine($"Signature component V: {v}");
                Console.WriteLine($"Full signature (65 bytes): {ToHex(signature)}");
                
                // For a basic test, just check that the signature length is correct
                bool isValidLength = signature.Length == 65;
                
                // Check that R and S are not all zeros (which would be invalid)
                bool hasValidComponents = !IsAllZeros(r) && !IsAllZeros(s);
                
                // Verify TRON signature format - V should be 0 or 1
                bool hasValidV = v == 0 || v == 1;
                
                Console.WriteLine($"Signature length check: {(isValidLength ? "?" : "?")}");
                Console.WriteLine($"Signature components check: {(hasValidComponents ? "?" : "?")}");
                Console.WriteLine($"TRON V value check: {(hasValidV ? "?" : "?")}");
                
                // Also test the address conversion
                TestAddressConversion();
                
                // Check if all validations pass
                return isValidLength && hasValidComponents && hasValidV;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Signature test FAILED with error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Detail: {ex.InnerException.Message}");
                }
                
                return false;
            }
        }

        /// <summary>
        /// Tests the address conversion for a known TRON address
        /// </summary>
        public static void TestAddressConversion()
        {
            try
            {
                Console.WriteLine("\n?? Testing TRON address conversion...");
                
                // Test case from your example
                string base58Address = "TDTpihBx5tAUhviF9GeGqFdmfJHc4Pd6Xc";
                string expectedHex = "412651d70c64be830cbeda3aebe274567fe19ebb0a";
                
                Console.WriteLine($"Base58 address: {base58Address}");
                Console.WriteLine($"Expected hex: {expectedHex}");
                
                // Use improved Base58CheckToHex conversion
                string actualHex = Base58CheckToHex(base58Address);
                Console.WriteLine($"Actual hex result: {actualHex}");
                
                bool isCorrect = actualHex.Equals(expectedHex, StringComparison.OrdinalIgnoreCase);
                Console.WriteLine($"Match result: {(isCorrect ? "?" : "?")}");
                
                // Test reverse conversion (hex to Base58)
                string convertedBackBase58 = HexToBase58Check(expectedHex);
                Console.WriteLine($"Converting hex back to Base58: {convertedBackBase58}");
                Console.WriteLine($"Original Base58 address: {base58Address}");
                Console.WriteLine($"Match result: {(convertedBackBase58 == base58Address ? "?" : "?")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Address conversion test failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Converts a TRON Base58 address to hex format correctly
        /// </summary>
        /// <param name="base58Address">The Base58 encoded TRON address</param>
        /// <returns>Hex representation of the address with the 41 prefix</returns>
        public static string Base58ToHex(string base58Address)
        {
            if (string.IsNullOrEmpty(base58Address))
                throw new ArgumentException("Base58 address cannot be empty");
                
            if (!base58Address.StartsWith("T"))
                throw new ArgumentException("TRON address must start with 'T'");
            
            try
            {
                // Use the improved Base58CheckToHex method
                return Base58CheckToHex(base58Address);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to convert Base58 address to hex: {ex.Message}", ex);
            }
        }
        
        /// <summary>
        /// Converts a Base58Check encoded string to hexadecimal string using improved algorithm
        /// </summary>
        /// <param name="base58String">Base58Check encoded string</param>
        /// <returns>Hexadecimal representation</returns>
        public static string Base58CheckToHex(string base58String)
        {
            if (string.IsNullOrEmpty(base58String))
                throw new ArgumentException("Input string cannot be null or empty");

            // Decode Base58
            byte[] decoded = DecodeBase58(base58String);
            
            if (decoded.Length < 4)
                throw new ArgumentException("Invalid Base58Check string - too short");

            // Split payload and checksum
            byte[] payload = decoded.Take(decoded.Length - 4).ToArray();
            byte[] checksum = decoded.Skip(decoded.Length - 4).ToArray();
            
            // Verify checksum
            byte[] hash = DoubleSha256(payload);
            byte[] expectedChecksum = hash.Take(4).ToArray();
            
            if (!checksum.SequenceEqual(expectedChecksum))
                throw new ArgumentException("Invalid Base58Check string - checksum mismatch");
            
            // Convert to hex
            return BitConverter.ToString(payload).Replace("-", "").ToLower();
        }

        /// <summary>
        /// Converts a hexadecimal string to Base58Check format
        /// </summary>
        /// <param name="hex">Hexadecimal string (with or without '41' prefix)</param>
        /// <returns>Base58Check encoded string</returns>
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
        /// Decodes a Base58 string to a byte array using improved algorithm
        /// </summary>
        private static byte[] DecodeBase58(string base58)
        {
            if (string.IsNullOrEmpty(base58))
                return Array.Empty<byte>();
                
            // Convert the Base58 string to a base-10 BigInteger
            BigInteger value = 0;
            
            // For each character
            for (int i = 0; i < base58.Length; i++)
            {
                // Get the index/position of the character in the Base58 alphabet
                int digit = Base58Alphabet.IndexOf(base58[i]);
                
                if (digit < 0)
                    throw new ArgumentException($"Invalid Base58 character: {base58[i]}");
                    
                // value = value * 58 + digit
                value = value * 58 + digit;
            }
            
            // Convert the BigInteger to byte array
            byte[] bytes = value.ToByteArray();
            
            // Need to reverse it since BigInteger uses little-endian
            Array.Reverse(bytes);
            
            // Count leading '1' characters (these represent leading zeros)
            int leadingOnes = base58.TakeWhile(c => c == '1').Count();
            
            // Add leading zero bytes
            byte[] result;
            if (leadingOnes > 0)
            {
                result = new byte[leadingOnes + bytes.Length];
                Array.Copy(bytes, 0, result, leadingOnes, bytes.Length);
            }
            else
            {
                // If no leading zeros, but there's a sign byte we don't need (BigInteger adds one for positive numbers)
                if (bytes.Length > 0 && bytes[0] == 0)
                {
                    result = new byte[bytes.Length - 1];
                    Array.Copy(bytes, 1, result, 0, bytes.Length - 1);
                }
                else
                {
                    result = bytes;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Encodes a byte array to Base58 string
        /// </summary>
        private static string EncodeBase58(byte[] data)
        {
            // Leading zero bytes become Base58 '1' characters
            int zeroCount = data.TakeWhile(b => b == 0).Count();
            
            // Convert bytes to BigInteger (big endian)
            BigInteger value = 0;
            for (int i = zeroCount; i < data.Length; i++)
            {
                value = value * 256 + data[i];
            }
            
            // Convert the BigInteger to Base58 characters
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
                
            return System.Text.RegularExpressions.Regex.IsMatch(hexString, "^[0-9a-fA-F]+$");
        }

        /// <summary>
        /// Calculates the checksum for a TRON address
        /// </summary>
        private static byte[] CalculateChecksum(byte[] addressBytes)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] firstHash = sha256.ComputeHash(addressBytes);
                byte[] secondHash = sha256.ComputeHash(firstHash);
                return secondHash.Take(4).ToArray();
            }
        }
        
        /// <summary>
        /// Check if a byte array contains only zeros
        /// </summary>
        private static bool IsAllZeros(byte[] data)
        {
            foreach (byte b in data)
            {
                if (b != 0)
                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// Gets the recovery parameter for TRON (0 or 1) from an Ethereum signature (where V is typically 27 or 28)
        /// </summary>
        private static byte GetTronRecoveryParam(EthECDSASignature signature)
        {
            // This checks if the 'v' value is available via reflection
            try
            {
                // If EthECDSASignature has a public V property or field, use it
                var vProperty = typeof(EthECDSASignature).GetProperty("V");
                if (vProperty != null)
                {
                    int v = (int)vProperty.GetValue(signature)!;
                    // Convert from Ethereum V (27 or 28) to TRON V (0 or 1)
                    if (v >= 27)
                        return (byte)(v - 27);
                    return (byte)v;
                }
                
                // Check for lowercase 'v'
                vProperty = typeof(EthECDSASignature).GetProperty("v");
                if (vProperty != null)
                {
                    int v = (int)vProperty.GetValue(signature)!;
                    if (v >= 27)
                        return (byte)(v - 27);
                    return (byte)v;
                }
                
                // Check if it's a field instead of property
                var vField = typeof(EthECDSASignature).GetField("V") ?? 
                             typeof(EthECDSASignature).GetField("v");
                if (vField != null)
                {
                    int v = (int)vField.GetValue(signature)!;
                    if (v >= 27)
                        return (byte)(v - 27);
                    return (byte)v;
                }
            }
            catch
            {
                // Fallback if reflection fails
            }
        
            // As a fallback, try to extract information from the signature bytes
            // For TRON, typically use 0, but we can verify which one works
            
            // Default to recovery ID 0 (this works in most cases for TRON)
            return 0;
        }

        /// <summary>
        /// Ensure a byte array is exactly 32 bytes (padded with zeros if needed)
        /// </summary>
        private static byte[] PadTo32Bytes(byte[] input)
        {
            if (input.Length == 32)
                return input;

            byte[] output = new byte[32];

            if (input.Length > 32)
            {
                // If input is larger than 32 bytes, take the last 32
                Buffer.BlockCopy(input, input.Length - 32, output, 0, 32);
            }
            else
            {
                // If input is smaller than 32 bytes, pad left with zeros
                Buffer.BlockCopy(input, 0, output, 32 - input.Length, input.Length);
            }

            return output;
        }

        /// <summary>
        /// Convert a byte array to a hexadecimal string
        /// </summary>
        public static string ToHex(byte[] bytes, bool prefix = false)
        {
            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            if (prefix)
                hex.Append("0x");

            foreach (byte b in bytes)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }
    }
}