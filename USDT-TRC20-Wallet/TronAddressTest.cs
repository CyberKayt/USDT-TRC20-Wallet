using System;
using System.Threading.Tasks;

namespace USDT_TRC20Transfer
{
    /// <summary>
    /// Test utility class for TRON address conversions
    /// </summary>
    public static class TronAddressTest
    {
        // Base58 alphabet used by Bitcoin and TRON
        private static readonly string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        /// <summary>
        /// Run a comprehensive test of TRON address conversions
        /// </summary>
        public static async Task RunTest()
        {
            Console.WriteLine("╔════════════════════════════════════════════╗");
            Console.WriteLine("║       TRON Address Conversion Tests        ║");
            Console.WriteLine("╚════════════════════════════════════════════╝");
            Console.WriteLine();

            // Test menu
            Console.WriteLine("Select a test option:");
            Console.WriteLine("1 - Run standard test with example address");
            Console.WriteLine("2 - Test with your own TRON address");
            Console.WriteLine("3 - Convert hex address to Base58");
            Console.WriteLine("4 - Validate address for illegal characters");
            Console.WriteLine("5 - Run all tests");
            Console.WriteLine("0 - Return to main menu");
            Console.Write("\nSelection: ");

            string choice = Console.ReadLine()?.Trim() ?? "4";

            switch (choice)
            {
                case "1":
                    await RunStandardTest();
                    break;
                case "2":
                    await RunCustomAddressTest();
                    break;
                case "3":
                    await RunHexToBase58Test();
                    break;
                case "4":
                    await RunBase58ValidationTest();
                    break;
                case "5":
                    await RunStandardTest();
                    await RunCustomAddressTest();
                    await RunHexToBase58Test();
                    await RunBase58ValidationTest();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid option. Running standard test...");
                    await RunStandardTest();
                    break;
            }

            // Add a small delay to ensure all async operations complete
            await Task.Delay(100);
            Console.WriteLine("\nPress any key to return...");
            Console.ReadKey();
        }

        /// <summary>
        /// Run the standard test with the example address
        /// </summary>
        private static async Task RunStandardTest()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════╗");
            Console.WriteLine("║       STANDARD ADDRESS TEST                ║");
            Console.WriteLine("╚════════════════════════════════════════════╝");
            Console.WriteLine();

            // Test with the example address
            string tronAddress = "TDTpihBx5tAUhviF9GeGqFdmfJHc4Pd6Xc";
            string expectedHex = "412651d70c64be830cbeda3aebe274567fe19ebb0a";
            
            Console.WriteLine($"Testing with sample address: {tronAddress}");
            Console.WriteLine($"Expected hex result:         {expectedHex}");
            
            // Test using all three methods
            Console.WriteLine("\n1. Using TronSignature.Base58ToHex method:");
            try
            {
                string hex1 = TronSignature.Base58ToHex(tronAddress);
                Console.WriteLine($"   Result: {hex1}");
                Console.WriteLine($"   Match: {(hex1.Equals(expectedHex, StringComparison.OrdinalIgnoreCase) ? "✅ PASS" : "❌ FAIL")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error: {ex.Message}");
            }
            
            Console.WriteLine("\n2. Using TronBase58Converter.Base58CheckToHex method:");
            try
            {
                string hex2 = TronBase58Converter.Base58CheckToHex(tronAddress);
                Console.WriteLine($"   Result: {hex2}");
                Console.WriteLine($"   Match: {(hex2.Equals(expectedHex, StringComparison.OrdinalIgnoreCase) ? "✅ PASS" : "❌ FAIL")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error: {ex.Message}");
            }
            
            Console.WriteLine("\n3. Using SimpleBase.Base58 in manual process:");
            try
            {
                // Decode Base58 and extract data manually
                var decoded = SimpleBase.Base58.Bitcoin.Decode(tronAddress).ToArray();
                
                // Log bytes in hex
                Console.WriteLine($"   Raw decoded bytes ({decoded.Length} bytes):");
                Console.WriteLine($"   {BitConverter.ToString(decoded).Replace("-", " ")}");
                
                if (decoded.Length != 25)
                {
                    Console.WriteLine($"   ❌ Invalid decoded length: {decoded.Length} (expected 25 bytes)");
                }
                else
                {
                    // Get address portion (first 21 bytes)
                    byte[] addressBytes = new byte[21];
                    Buffer.BlockCopy(decoded, 0, addressBytes, 0, 21);
                    
                    // Convert to hex and compare
                    string hex3 = BitConverter.ToString(addressBytes).Replace("-", "").ToLower();
                    Console.WriteLine($"   Result: {hex3}");
                    Console.WriteLine($"   Match: {(hex3.Equals(expectedHex, StringComparison.OrdinalIgnoreCase) ? "✅ PASS" : "❌ FAIL")}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error: {ex.Message}");
            }
            
            // Perform a round-trip test
            Console.WriteLine("\nRound-trip conversion test (hex → Base58 → hex):");
            try
            {
                // Convert hex to Base58
                string convertedBase58 = TronSignature.HexToBase58Check(expectedHex);
                Console.WriteLine($"   Original hex:        {expectedHex}");
                Console.WriteLine($"   Converted to Base58: {convertedBase58}");
                
                // Convert back to hex
                string convertedBackHex = TronSignature.Base58ToHex(convertedBase58);
                Console.WriteLine($"   Converted back to hex: {convertedBackHex}");
                
                // Compare all values
                bool base58Match = convertedBase58 == tronAddress;
                bool hexMatch = convertedBackHex.Equals(expectedHex, StringComparison.OrdinalIgnoreCase);
                
                Console.WriteLine($"   Base58 match: {(base58Match ? "✅ PASS" : "❌ FAIL")}");
                Console.WriteLine($"   Hex match:    {(hexMatch ? "✅ PASS" : "❌ FAIL")}");
                
                if (base58Match && hexMatch)
                {
                    Console.WriteLine("\n   ✅ ROUND-TRIP CONVERSION TEST PASSED!");
                }
                else
                {
                    Console.WriteLine("\n   ❌ ROUND-TRIP CONVERSION TEST FAILED!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error in round-trip test: {ex.Message}");
            }

            // Add a small delay to ensure all async operations complete
            await Task.Delay(10);
            
            Console.WriteLine("\nStandard test completed.");
        }

        /// <summary>
        /// Test with a user-provided TRON address
        /// </summary>
        private static async Task RunCustomAddressTest()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════╗");
            Console.WriteLine("║       CUSTOM ADDRESS TEST                  ║");
            Console.WriteLine("╚════════════════════════════════════════════╝");
            Console.WriteLine();
            
            Console.Write("Enter a TRON address to test (T...): ");
            string customAddress = Console.ReadLine()?.Trim() ?? "";
            
            if (string.IsNullOrEmpty(customAddress))
            {
                Console.WriteLine("❌ No address provided. Test aborted.");
                return;
            }
            
            try
            {
                // First check if the address is valid
                Console.WriteLine("\nValidating address format...");
                
                if (!customAddress.StartsWith("T"))
                {
                    Console.WriteLine("❌ Invalid address: Must start with T");
                    return;
                }
                
                if (customAddress.Length != 34)
                {
                    Console.WriteLine($"❌ Invalid address length: {customAddress.Length} (expected 34)");
                    return;
                }
                
                // Check if address is valid using TronBase58Converter
                if (!TronBase58Converter.ValidateTronAddress(customAddress))
                {
                    Console.WriteLine("❌ TRON address validation failed (Invalid checksum or format)");
                    return;
                }
                
                Console.WriteLine("✅ Address format is valid");
                Console.WriteLine("\nConverting Base58 address to hex...");
                
                // Test all conversion methods
                Console.WriteLine("Method 1 (TronSignature):");
                string hex1 = TronSignature.Base58ToHex(customAddress);
                Console.WriteLine($"  Result: {hex1}");
                
                Console.WriteLine("\nMethod 2 (TronBase58Converter):");
                string hex2 = TronBase58Converter.Base58CheckToHex(customAddress);
                Console.WriteLine($"  Result: {hex2}");
                
                // Check if results match
                bool resultsMatch = hex1.Equals(hex2, StringComparison.OrdinalIgnoreCase);
                Console.WriteLine($"\nBoth methods {(resultsMatch ? "produce the same result ✅" : "produced different results ❌")}");
                
                // Extract the 20-byte address (without 41 prefix)
                string addressOnly = TronBase58Converter.ExtractAddressBytesFromBase58(customAddress);
                Console.WriteLine($"\n20-byte address only (no prefix): {addressOnly}");
                
                // Perform round-trip conversion
                Console.WriteLine("\nRound-trip testing (Base58 -> Hex -> Base58):");
                string backToBase58 = TronSignature.HexToBase58Check(hex1);
                Console.WriteLine($"Original:  {customAddress}");
                Console.WriteLine($"Converted: {backToBase58}");
                Console.WriteLine($"Match: {(customAddress == backToBase58 ? "✅ PASS" : "❌ FAIL")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error testing address: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Detail: {ex.InnerException.Message}");
                }
            }

            // Add a small delay to ensure all async operations complete
            await Task.Delay(10);

            Console.WriteLine("\nCustom address test completed.");
        }

        /// <summary>
        /// Test converting a hex address to Base58
        /// </summary>
        private static async Task RunHexToBase58Test()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════╗");
            Console.WriteLine("║       HEX TO BASE58 TEST                   ║");
            Console.WriteLine("╚════════════════════════════════════════════╝");
            Console.WriteLine();
            
            Console.WriteLine("Enter a hex address to convert to Base58.");
            Console.WriteLine("The address can be in any of these formats:");
            Console.WriteLine("  - Raw 20-byte address (40 hex chars)");
            Console.WriteLine("  - With 41 prefix (42 hex chars)");
            Console.WriteLine("  - With 0x prefix (0x...)");
            Console.Write("\nHex address: ");
            
            string hexInput = Console.ReadLine()?.Trim() ?? "";
            
            if (string.IsNullOrEmpty(hexInput))
            {
                Console.WriteLine("❌ No hex address provided. Test aborted.");
                return;
            }
            
            try
            {
                // Clean up input
                if (hexInput.StartsWith("0x"))
                {
                    hexInput = hexInput.Substring(2);
                }
                
                // Check if we need to add the 41 prefix
                string hexToConvert = hexInput;
                if (!hexInput.StartsWith("41") && hexInput.Length == 40)
                {
                    hexToConvert = "41" + hexInput;
                    Console.WriteLine($"Added '41' prefix: {hexToConvert}");
                }
                
                // Validate hex string - should be even length and only hex chars
                if (!System.Text.RegularExpressions.Regex.IsMatch(hexToConvert, "^[0-9a-fA-F]+$"))
                {
                    Console.WriteLine("❌ Invalid hex format: Contains non-hexadecimal characters");
                    return;
                }
                
                if (hexToConvert.Length % 2 != 0)
                {
                    Console.WriteLine("❌ Invalid hex format: Length should be even");
                    return;
                }
                
                // Convert using both methods
                Console.WriteLine("\nMethod 1 (TronSignature):");
                string base58_1 = TronSignature.HexToBase58Check(hexToConvert);
                Console.WriteLine($"  Result: {base58_1}");
                
                Console.WriteLine("\nMethod 2 (TronBase58Converter):");
                string base58_2 = TronBase58Converter.HexToBase58Check(hexToConvert);
                Console.WriteLine($"  Result: {base58_2}");
                
                // Check if results match
                bool resultsMatch = base58_1 == base58_2;
                Console.WriteLine($"\nBoth methods {(resultsMatch ? "produce the same result ✅" : "produced different results ❌")}");
                
                // Perform round-trip conversion
                Console.WriteLine("\nRound-trip testing (Hex -> Base58 -> Hex):");
                string backToHex = TronSignature.Base58ToHex(base58_1);
                Console.WriteLine($"Original:  {hexToConvert.ToLower()}");
                Console.WriteLine($"Converted: {backToHex}");
                Console.WriteLine($"Match: {(hexToConvert.ToLower() == backToHex.ToLower() ? "✅ PASS" : "❌ FAIL")}");
                
                // Validate the generated address
                bool isValid = TronBase58Converter.ValidateTronAddress(base58_1);
                Console.WriteLine($"\nGenerated address validation: {(isValid ? "✅ Valid TRON address" : "❌ Invalid TRON address")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error in hex conversion: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Detail: {ex.InnerException.Message}");
                }
            }

            // Add a small delay to ensure all async operations complete
            await Task.Delay(10);

            Console.WriteLine("\nHex to Base58 test completed.");
        }

        /// <summary>
        /// Test for validating a TRON address for illegal characters in Base58 format
        /// which can cause transaction errors like "INVALID base58 String, Illegal character"
        /// </summary>
        private static async Task RunBase58ValidationTest()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════╗");
            Console.WriteLine("║     BASE58 CHARACTER VALIDATION TEST       ║");
            Console.WriteLine("╚════════════════════════════════════════════╝");
            Console.WriteLine();
            
            Console.WriteLine("This test checks for illegal characters in TRON addresses");
            Console.WriteLine("that can cause the error: \"INVALID base58 String, Illegal character\"");
            Console.WriteLine("");
            Console.WriteLine("Base58 format explicitly excludes these characters: 0OIl+/");
            Console.WriteLine(" - 0 (zero)");
            Console.WriteLine(" - O (capital o)");
            Console.WriteLine(" - I (capital i)");
            Console.WriteLine(" - l (lowercase L)");
            Console.WriteLine(" - + (plus)");
            Console.WriteLine(" - / (slash)");
            Console.WriteLine();
            
            Console.Write("Enter a TRON address to validate (T...): ");
            string address = Console.ReadLine()?.Trim() ?? "";
            
            if (string.IsNullOrEmpty(address))
            {
                Console.WriteLine("❌ No address provided. Test aborted.");
                return;
            }
            
            // Check overall format first
            Console.WriteLine("\nChecking basic address format...");
            if (!address.StartsWith("T"))
            {
                Console.WriteLine("❌ Invalid address: Must start with T");
                return;
            }
            
            if (address.Length != 34)
            {
                Console.WriteLine($"❌ Invalid address length: {address.Length} (expected 34)");
                return;
            }
            
            // Check for illegal characters
            Console.WriteLine("\nScanning for illegal characters in Base58...");
            
            bool hasIllegalChars = false;
            for (int i = 0; i < address.Length; i++)
            {
                char c = address[i];
                if (Base58Alphabet.IndexOf(c) < 0)
                {
                    Console.WriteLine($"❌ Illegal character '{c}' found at position {i}");
                    hasIllegalChars = true;
                    
                    // Suggest possible corrections
                    if (c == '0') // Zero is often confused with 'o'
                        Console.WriteLine($"   Suggestion: Replace '0' with 'o' at position {i}");
                    else if (c == 'O') // Capital O is often confused with 'o'
                        Console.WriteLine($"   Suggestion: Replace 'O' with 'o' at position {i}");
                    else if (c == 'I') // Capital I is often confused with '1'
                        Console.WriteLine($"   Suggestion: Replace 'I' with '1' at position {i}");
                    else if (c == 'l') // Lowercase l is often confused with '1'
                        Console.WriteLine($"   Suggestion: Replace 'l' with '1' at position {i}");
                }
            }
            
            if (!hasIllegalChars)
            {
                Console.WriteLine("✅ No illegal characters found");
                
                // Additional validation with full Base58Check validation
                try
                {
                    bool isValid = TronBase58Converter.ValidateTronAddress(address);
                    Console.WriteLine($"\nFull address validation (including checksum): {(isValid ? "✅ Valid" : "❌ Invalid")}");
                    
                    if (isValid)
                    {
                        // Get the hex representation
                        string hex = TronBase58Converter.Base58CheckToHex(address);
                        Console.WriteLine($"Hex representation: {hex}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Validation error: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("\n❌ This address contains illegal Base58 characters, which would cause");
                Console.WriteLine("   the error: \"INVALID base58 String, Illegal character\" during transaction.");
                Console.WriteLine("   Please correct the address using the suggestions above.");
            }
            
            await Task.Delay(10);
            Console.WriteLine("\nBase58 validation test completed.");
        }
    }
}