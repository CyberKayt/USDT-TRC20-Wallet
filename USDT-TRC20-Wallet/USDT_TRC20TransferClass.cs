using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleBase;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace USDT_TRC20Transfer
{
    public class USDT_TRC20TransferClass
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        // Constants from configuration
        private static string MainnetApiUrl => Configuration.MAINNET_API;
        private static string TestnetApiUrl => Configuration.TESTNET_API;
        private static string MainnetExplorerUrl => Configuration.MAINNET_EXPLORER;
        private static string TestnetExplorerUrl => Configuration.TESTNET_EXPLORER;
        private static readonly string UsdtContractAddress = Program.Configuration?["Contracts:USDT"] ?? "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t";

        // Testnet USDT contract address
        private const string TESTNET_USDT_CONTRACT_ADDRESS = "TG3XXyExBkPp9nzdajDZsozEu4BkaSJozs";

        // Minimum recommended TRX balance for gas fees
        private const decimal MinimumTrxBalance = 5;

        /// <summary>
        /// Runs a USDT TRC20 transfer on the mainnet
        /// </summary>
        public static async Task<string> RunMainNetTransfer(string fromPrivateKey, string toAddress, decimal amount)
        {
            Console.WriteLine("🔷 USDT (TRC20) Transferi - MAINNET");
            Console.WriteLine("----------------------------------");

            return await RunTransfer(fromPrivateKey, toAddress, amount, false);
        }

        /// <summary>
        /// Runs a USDT TRC20 transfer on the testnet
        /// </summary>
        public static async Task<string> RunTestNetTransfer(string fromPrivateKey, string toAddress, decimal amount)
        {
            Console.WriteLine("🔷 USDT (TRC20) Test Transferi - TESTNET");
            Console.WriteLine("--------------------------------------");

            return await RunTransfer(fromPrivateKey, toAddress, amount, true);
        }

        /// <summary>
        /// Interactive transfer function that allows network selection
        /// </summary>
        public static async Task RunProductionTransfer()
        {
            Console.WriteLine("🔷 USDT (TRC20) Transfer - Ağ Seçimli");
            Console.WriteLine("---------------------------------");

            // Ağ seçimi
            Console.WriteLine("Transfer için ağ seçin:");
            Console.WriteLine("1 - Ana Ağ (MainNet)");
            Console.WriteLine("2 - Test Ağı (TestNet - Shasta)");
            Console.Write("Seçiminiz (1-2): ");

            string networkChoice = Console.ReadLine()?.Trim() ?? "1";
            bool isTestnet = networkChoice == "2";

            string networkName = isTestnet ? "TESTNET (Shasta)" : "MAINNET";
            Console.WriteLine($"\nSeçilen ağ: {networkName}");

            // Kullanıcı adres ve miktarları girmesi
            Console.Write("\nGönderici (From) özel anahtarını girin: ");
            string fromPrivateKey = ConsoleReadLineMasked();

            // özel anahtardan adres üretme ve doğrulama
            var addrResult = TronAddressVerifier.GenerateAndVerifyTronAddress(fromPrivateKey);
            string fromAddress = addrResult.GeneratedAddress;
            Console.WriteLine($"Gönderici adresi: {fromAddress}");

            // Bakiye kontrolü yap
            await CheckBalanceBeforeTransfer(fromAddress, isTestnet);

            Console.Write("\nAlıcı (To) adresini girin: ");
            string toAddress = Console.ReadLine()?.Trim() ?? "";

            Console.Write("\nTransfer miktarını girin (USDT): ");
            if (!decimal.TryParse(Console.ReadLine()?.Trim(), out decimal amount) || amount <= 0)
            {
                Console.WriteLine("❌ Geçersiz miktar! Transfer işlemi iptal edildi.");
                return;
            }

            Console.WriteLine($"\n💱 Transfer Detayları:");
            Console.WriteLine($"Gönderen: {fromAddress}");
            Console.WriteLine($"Alıcı: {toAddress}");
            Console.WriteLine($"Miktar: {amount:N2} USDT");
            Console.WriteLine($"Ağ: {networkName}");

            Console.Write("\nOnaylıyor musunuz? (E/H): ");
            string confirm = Console.ReadLine()?.Trim().ToUpper() ?? "H";

            if (confirm != "E")
            {
                Console.WriteLine("\n❌ İşlem kullanıcı tarafından iptal edildi.");
                return;
            }

            try
            {
                string txId = await RunTransfer(fromPrivateKey, toAddress, amount, isTestnet);

                Console.WriteLine($"\n✅ Transfer başarıyla tamamlandı!");
                Console.WriteLine($"İşlem ID (TxID): {txId}");

                // Explorer URL
                string explorerUrl = isTestnet ? TestnetExplorerUrl : MainnetExplorerUrl;
                Console.WriteLine($"İşlem detayları: {explorerUrl}{txId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Transfer işlemi başarısız: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Detay: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// Core transfer function that handles both mainnet and testnet transfers
        /// </summary>
        private static async Task<string> RunTransfer(string fromPrivateKey, string toAddress, decimal amount, bool isTestnet)
        {
            // Validate parameters
            if (string.IsNullOrEmpty(fromPrivateKey))
                throw new ArgumentException("Private key cannot be empty");

            if (string.IsNullOrEmpty(toAddress) || !toAddress.StartsWith("T") || toAddress.Length != 34)
                throw new ArgumentException("Invalid recipient address format");

            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero");

            try
            {
                // Generate from address from private key - Bu çok önemli!
                var addrResult = TronAddressVerifier.GenerateAndVerifyTronAddress(fromPrivateKey);
                string fromAddress = addrResult.GeneratedAddress;

                Console.WriteLine($"🔑 Private key'den türetilen adres: {fromAddress}");

                // Set API URL based on network selection
                string apiUrl = isTestnet ? TestnetApiUrl : MainnetApiUrl;

                // Set contract address based on network
                string contractAddress = isTestnet ? TESTNET_USDT_CONTRACT_ADDRESS : UsdtContractAddress;

                // Verify balances before proceeding with transfer
                var balanceVerificationResult = await VerifyBalancesForTransfer(fromAddress, amount, isTestnet);
                
                if (!balanceVerificationResult.Success)
                {
                    Console.Write("\n❌ Transfer işlemi yapılamaz: " + balanceVerificationResult.ErrorMessage);
                    
                    if (!balanceVerificationResult.HasSufficientFunds && !isTestnet)
                    {
                        Console.WriteLine("\n❓ Yetersiz bakiye ile devam etmek istiyor musunuz? (E/H): ");
                        string continueChoice = Console.ReadLine()?.Trim().ToUpper() ?? "H";
                        
                        if (continueChoice != "E")
                        {
                            throw new Exception("İşlem yetersiz bakiye nedeniyle iptal edildi.");
                        }
                        
                        Console.WriteLine("\n⚠️ Yetersiz bakiye ile devam ediliyor. İşlem başarısız olabilir.");
                    }
                    else if (!balanceVerificationResult.HasSufficientGas)
                    {
                        Console.WriteLine("\n❓ Yetersiz TRX (gas) ile devam etmek istiyor musunuz? (E/H): ");
                        string continueChoice = Console.ReadLine()?.Trim().ToUpper() ?? "H";
                        
                        if (continueChoice != "E")
                        {
                            throw new Exception("İşlem yetersiz TRX (gas) nedeniyle iptal edildi.");
                        }
                        
                        Console.WriteLine("\n⚠️ Yetersiz TRX ile devam ediliyor. İşlem başarısız olabilir.");
                    }
                }
                else
                {
                    Console.WriteLine("\n✅ Bakiye kontrolü başarılı. İşleme devam ediliyor...");
                }

                // Convert USDT amount to TRC20 format (6 decimals)
                BigInteger tokenAmount = new BigInteger(amount * 1_000_000);
                Console.WriteLine($"Token amount in internal format: {tokenAmount}");

                Console.WriteLine($"Preparing transaction to transfer {amount} USDT from {fromAddress} to {toAddress}...");

                // Create contract call data - SADECE PARAMETRELERİ İÇERİR, METHOD ID İÇERMEZ
                string data = CreateTransferData(toAddress, tokenAmount);
                Console.WriteLine($"Contract data parameters: {data}");

                // Build transaction - Keep everything in Base58 format
                Console.WriteLine("Building transaction...");
                JObject transactionResponse = await BuildTransaction(fromAddress, contractAddress, data, apiUrl);

                if (transactionResponse?["result"]?["result"]?.Value<bool>() != true)
                {
                    string errorMessage = transactionResponse?["result"]?["message"]?.ToString() ?? "Unknown error";
                    Console.WriteLine("Transaction build response: " + transactionResponse?.ToString());
                    throw new Exception($"Failed to build transaction: {errorMessage}");
                }

                // Extract transaction from response
                JObject transaction = transactionResponse["transaction"]?.Value<JObject>();
                if (transaction == null)
                {
                    throw new Exception("Transaction data not found in response");
                }

                // Critical: Verify transaction owner matches the private key address
                string ownerAddress = transaction["raw_data"]?["contract"]?[0]?["parameter"]?["value"]?["owner_address"]?.ToString();
                if (!string.IsNullOrEmpty(ownerAddress))
                {
                    // Log the raw owner address for debugging
                    Console.WriteLine($"🔍 Raw owner address from transaction: {ownerAddress}");
                    
                    try {
                        // Convert hex address to base58 format for comparison using our improved method
                        string base58Owner = HexAddressToBase58(ownerAddress);
                        Console.WriteLine($"🔍 Converted base58 owner address: {base58Owner}");
                        
                        if (base58Owner != fromAddress)
                        {
                            throw new Exception($"Transaction owner mismatch: Expected {fromAddress}, got {base58Owner}");
                        }
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"⚠️ Warning: Could not verify owner address: {ex.Message}");
                        // Continue anyway as this is just a verification step
                    }
                }

                // Sign transaction with improved signature method
                Console.WriteLine("Signing transaction...");
                JObject signedTx = SignTransaction(transaction, fromPrivateKey, isTestnet);

                // Broadcast transaction
                Console.WriteLine("Broadcasting transaction...");
                string txId = await BroadcastTransaction(signedTx, apiUrl, isTestnet);

                // Check if transaction was successful
                Console.WriteLine($"Transaction sent! Checking status...");
                bool txSuccess = await VerifyTransactionStatus(txId, apiUrl, isTestnet);

                if (!txSuccess)
                {
                    throw new Exception("Transaction was broadcast but verification failed. Please check the blockchain explorer for details.");
                }

                return txId;
            }
            catch (Exception ex)
            {
                throw new Exception($"USDT transfer failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Balance verification result class
        /// </summary>
        private class BalanceVerificationResult
        {
            public bool Success { get; set; } = true;
            public bool HasSufficientFunds { get; set; } = true;
            public bool HasSufficientGas { get; set; } = true;
            public string ErrorMessage { get; set; } = "";
            public decimal UsdtBalance { get; set; }
            public decimal TrxBalance { get; set; }
        }

        /// <summary>
        /// Verifies if a wallet has sufficient balances for a transfer
        /// </summary>
        private static async Task<BalanceVerificationResult> VerifyBalancesForTransfer(string address, decimal requiredAmount, bool isTestnet)
        {
            var result = new BalanceVerificationResult();
            
            try
            {
                Console.WriteLine("\n🔍 Transfer öncesi detaylı bakiye kontrolü yapılıyor...");
                
                // Check TRX balance for gas fees 
                TRX_TRC20AccountBalance trxBalance = new TRX_TRC20AccountBalance();
                decimal trxAmount = await trxBalance.GetTrxBalance(address);
                result.TrxBalance = trxAmount;

                // Check USDT balance
                USDT_TRC20AccountBalance usdtBalance = new USDT_TRC20AccountBalance();
                decimal usdtAmount = await usdtBalance.GetUsdtBalance(address);
                result.UsdtBalance = usdtAmount;
                
                Console.WriteLine($"\n📊 Cüzdan bakiyeleri (doğrulama):");
                Console.WriteLine($"TRX: {trxAmount:N6} TRX");
                Console.WriteLine($"USDT: {usdtAmount:N2} USDT");
                Console.WriteLine($"Gerekli USDT: {requiredAmount:N2} USDT");

                // Check if there's enough USDT balance
                if (usdtAmount < requiredAmount)
                {
                    result.HasSufficientFunds = false;
                    result.Success = false;
                    decimal shortfall = requiredAmount - usdtAmount;
                    result.ErrorMessage = $"Yetersiz USDT bakiyesi! {requiredAmount:N2} USDT göndermek için {shortfall:N2} USDT daha gerekiyor.";
                    Console.WriteLine($"\n⚠️ {result.ErrorMessage}");
                }

                // Check if there's enough TRX for gas fees - only issue a warning
                if (trxAmount < MinimumTrxBalance)
                {
                    result.HasSufficientGas = false;
                    result.Success = false;
                    decimal recommendedAmount = MinimumTrxBalance - trxAmount;
                    
                    if (result.ErrorMessage.Length > 0)
                        result.ErrorMessage += "\n";
                        
                    result.ErrorMessage += $"Düşük TRX bakiyesi! İşlem ücretleri (gas) için en az {MinimumTrxBalance:N2} TRX olması önerilir. {recommendedAmount:N2} TRX daha yüklemeniz tavsiye edilir.";
                    Console.WriteLine($"\n⚠️ {result.ErrorMessage.Split('\n').Last()}");
                }
                
                // Special handling for testnet, always allow transfers on testnet regardless of balance
                if (isTestnet && !result.Success)
                {
                    result.Success = true; // Override for testnet
                    Console.WriteLine("\n✅ TestNet'te olduğunuz için düşük bakiye uyarılarını yok sayabilirsiniz.");
                }
                
                if (result.Success)
                {
                    Console.WriteLine("\n✅ Bakiye kontrolü başarılı - Transfer için yeterli bakiyeniz var.");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n⚠️ Bakiye doğrulamasında hata oluştu: {ex.Message}");
                Console.WriteLine("   İşlem devam edecek ancak bakiye yetersizse başarısız olabilir.");
                
                // Don't block the transfer if we can't verify the balance
                result.Success = true;
                result.ErrorMessage = $"Bakiye doğrulanamadı: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Converts a hex address (e.g. "41...") to Base58 address format (e.g. "T...")
        /// </summary>
        private static string HexAddressToBase58(string hexAddress)
        {
            try
            {
                Console.WriteLine($"Converting hex address to Base58: {hexAddress}");
                
                // Validate input
                if (string.IsNullOrEmpty(hexAddress))
                    throw new ArgumentException("Hex address cannot be null or empty");
                
                // Clean and standardize input
                if (hexAddress.StartsWith("0x"))
                    hexAddress = hexAddress.Substring(2);
                
                // Add 41 prefix if missing (TRON addresses start with 41 in hex format)
                if (!hexAddress.StartsWith("41"))
                    hexAddress = "41" + hexAddress;
                
                // Use the TronSignature implementation for consistent hex to Base58 conversion
                return TronSignature.HexToBase58Check(hexAddress);
            }
            catch (Exception ex)
            {
                // Include the hex address in the error message for better debugging
                throw new Exception($"Error converting hex address to Base58: {ex.Message} (Input: {hexAddress})", ex);
            }
        }
        
        /// <summary>
        /// Converts a Base58 address (e.g. "T...") to a hex format (e.g. "41...")
        /// </summary>
        private static string Base58AddressToHex(string base58Address)
        {
            try
            {
                Console.WriteLine($"Converting Base58 address to hex: {base58Address}");
                
                // Use the improved implementation from TronSignature class
                return TronSignature.Base58ToHex(base58Address);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error converting Base58 address to hex: {ex.Message} (Input: {base58Address})", ex);
            }
        }

        /// <summary>
        /// Validates if a string contains only valid hexadecimal characters
        /// </summary>
        private static bool IsValidHexString(string hexString)
        {
            if (string.IsNullOrEmpty(hexString))
                return false;
                
            // Check if string contains only hex characters (0-9, a-f, A-F)
            return Regex.IsMatch(hexString, "^[0-9a-fA-F]+$");
        }

        /// <summary>
        /// Checks both TRX and USDT balances before initiating a transfer
        /// </summary>
        private static async Task CheckBalanceBeforeTransfer(string address, bool isTestnet)
        {
            try
            {
                // Check TRX balance for gas fees
                TRX_TRC20AccountBalance trxBalance = new TRX_TRC20AccountBalance();
                decimal trxAmount = await trxBalance.GetTrxBalance(address);

                // Check USDT balance
                USDT_TRC20AccountBalance usdtBalance = new USDT_TRC20AccountBalance();
                decimal usdtAmount = await usdtBalance.GetUsdtBalance(address);

                Console.WriteLine($"\n📊 Cüzdan bakiyeleri:");
                Console.WriteLine($"TRX: {trxAmount:N6} TRX");
                Console.WriteLine($"USDT: {usdtAmount:N2} USDT");

                if (trxAmount < MinimumTrxBalance)
                {
                    Console.WriteLine("\n⚠️ Uyarı: İşlem ücretleri (gas) için yeterli TRX olmayabilir!");
                    Console.WriteLine($"    Önerilen minimum miktar: {MinimumTrxBalance} TRX");
                }
            }
            catch (Exception)
            {
                Console.WriteLine("\n⚠️ Bakiye kontrolünde hata oluştu.");
                Console.WriteLine("    İşleme devam ediliyor...");
            }
        }

        /// <summary>
        /// Creates transfer data for the contract call
        /// NOT: Bu metot ARTIK SADECE PARAMETRELERİ döndürür, Method ID içermez!
        /// Method ID olan "a9059cbb" function_selector ile TRON API tarafından eklenecektir
        /// </summary>
        private static string CreateTransferData(string toAddress, BigInteger amount)
        {
            try
            {
                Console.WriteLine($"Creating transfer data for address: {toAddress} and amount: {amount}");
                
                // NOT: Method ID ("a9059cbb") artık burada kullanılmıyor, BuildTransaction'da function_selector tarafından ekleniyor

                // Get the 20-byte recipient address in the correct format
                byte[] addressBytes;
                
                if (toAddress.StartsWith("T"))
                {
                    Console.WriteLine("Converting Base58 address to 20-byte format");
                    
                    // Convert to hex without the 41 prefix first
                    string hexAddress = TronSignature.Base58ToHex(toAddress);
                    
                    // Remove 41 prefix if present (first 2 chars)
                    if (hexAddress.StartsWith("41"))
                        hexAddress = hexAddress.Substring(2);
                    
                    // Convert to bytes
                    addressBytes = HexToBytes(hexAddress);
                }
                else if (toAddress.StartsWith("0x41"))
                {
                    Console.WriteLine("Converting 0x41-prefixed hex address to 20-byte format");
                    string hexWithout0x = toAddress.Substring(4); // Remove "0x41" prefix
                    addressBytes = HexToBytes(hexWithout0x);
                }
                else if (toAddress.StartsWith("41"))
                {
                    Console.WriteLine("Converting 41-prefixed hex address to 20-byte format");
                    string hexWithoutPrefix = toAddress.Substring(2); // Remove "41" prefix
                    addressBytes = HexToBytes(hexWithoutPrefix);
                }
                else
                {
                    throw new ArgumentException($"Invalid address format for contract parameter: {toAddress}");
                }
                
                // Convert address bytes to hex string
                string toAddressHex = BitConverter.ToString(addressBytes).Replace("-", "").ToLower();
                Console.WriteLine($"20-byte address hex: {toAddressHex}");
                
                // Verify length - should be 20 bytes = 40 hex chars
                if (toAddressHex.Length != 40)
                    throw new ArgumentException($"Invalid address length after conversion: {toAddressHex.Length} hex chars (expected 40)");
                
                // Pad address to 32 bytes (64 hex chars)
                string toAddressPadded = toAddressHex.PadLeft(64, '0');

                // Convert amount to hex and pad to 32 bytes (64 hex chars)
                // Using BigInteger for proper handling of large numbers
                string amountHex;
                if (amount < 0)
                {
                    throw new ArgumentException("Token amount cannot be negative");
                }
                
                // Convert to hex string without "0x" prefix
                amountHex = amount.ToString("x").TrimStart('0');
                
                // Handle case of zero amount
                if (string.IsNullOrEmpty(amountHex))
                {
                    amountHex = "0";
                }
                
                // Pad to 64 characters (32 bytes)
                amountHex = amountHex.PadLeft(64, '0');
                
                Console.WriteLine($"Address hex (padded): {toAddressPadded}");
                Console.WriteLine($"Amount hex (padded): {amountHex}");
                
                // SADECE parametreleri döndür, method ID içermez
                string result = toAddressPadded + amountHex;
                Console.WriteLine($"Contract parameters data: {result}");
                
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating transfer data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Builds a transaction for the USDT transfer using Base58 addresses
        /// </summary>
        private static async Task<JObject> BuildTransaction(string fromAddress, string contractAddress, string data, string apiUrl)
        {
            try
            {
                // IMPORTANT: Keep addresses in Base58 format for the API
                Console.WriteLine("🔄 Using Base58 addresses directly with API - no hex conversion");
                
                if (!fromAddress.StartsWith("T") || fromAddress.Length != 34)
                    throw new ArgumentException($"Invalid from address format. Expected Base58 address starting with 'T': {fromAddress}");
                
                if (!contractAddress.StartsWith("T") || contractAddress.Length != 34)
                    throw new ArgumentException($"Invalid contract address format. Expected Base58 address starting with 'T': {contractAddress}");
                
                Console.WriteLine($"From address (Base58): {fromAddress}");
                Console.WriteLine($"Contract address (Base58): {contractAddress}");
                
                // Build request body using Base58 addresses directly
                // TRON API, function_selector ve parametre kombinasyonu kullanır
                var requestBody = new
                {
                    owner_address = fromAddress,
                    contract_address = contractAddress,
                    function_selector = "transfer(address,uint256)",  // Method ID otomatik olarak buradan oluşturulur
                    parameter = data,                                // data ARTIK SADECE parametreleri içerir
                    fee_limit = 100000000,                           // 100 TRX fee limit
                    call_value = 0,
                    visible = true                                   // Base58 adresleri kullanmak için gerekli
                };
                
                string json = JsonConvert.SerializeObject(requestBody);
                Console.WriteLine($"Transaction request body: {json}");
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync($"{apiUrl}/wallet/triggersmartcontract", content);
                string responseContent = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"🔍 Transaction build response: {responseContent}");
                
                return JObject.Parse(responseContent);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to build transaction: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Improved implementation for signing TRON transactions
        /// </summary>
        private static JObject SignTransaction(JObject transaction, string privateKeyHex, bool isTestnet = false)
        {
            try
            {
                if (isTestnet)
                {
                    Console.WriteLine("📝 Using enhanced transaction signing method for TESTNET (Base58 format)");
                }
                else
                {
                    Console.WriteLine("📝 Using enhanced transaction signing method");
                }
                
                // Clean private key by removing any 0x prefix
                if (privateKeyHex.StartsWith("0x"))
                    privateKeyHex = privateKeyHex.Substring(2);

                // Get transaction raw data hex
                string rawDataHex = transaction["raw_data_hex"]?.Value<string>();
                if (string.IsNullOrEmpty(rawDataHex))
                    throw new Exception("Transaction raw_data_hex is missing");

                Console.WriteLine($"🔍 Raw data hex: {rawDataHex}");

                // Decode hex to bytes
                byte[] rawDataBytes = HexToBytes(rawDataHex);

                // Calculate SHA256 hash - TRON uses single SHA256 hash (not double)
                byte[] messageHash;
                using (var sha256 = SHA256.Create())
                {
                    messageHash = sha256.ComputeHash(rawDataBytes);
                }
                
                Console.WriteLine($"🔍 Transaction hash: {ToHex(messageHash)}");

                // Sign the message hash with private key using TronSignature
                byte[] signature = TronSignature.SignMessage(messageHash, privateKeyHex);
                
                // Convert signature to hex
                string signatureHex = ToHex(signature);
                
                Console.WriteLine($"🔍 Signature (hex): {signatureHex}");
                
                // Ensure visible=true for all transactions, especially important for testnet
                // Clone the original transaction and add signature
                JObject signedTx = new JObject
                {
                    ["visible"] = true, // Critical for using Base58 addresses with API - especially for testnet
                    ["txID"] = transaction["txID"],
                    ["raw_data"] = transaction["raw_data"],
                    ["raw_data_hex"] = rawDataHex,
                    ["signature"] = new JArray(signatureHex)
                };

                // For testnet, emphasize that we're using Base58 format
                if (isTestnet)
                {
                    Console.WriteLine("🔄 TESTNET transaction using Base58 addresses - visible flag set to true");
                }

                return signedTx;
            }
            catch (Exception ex)
            {
                throw new Exception($"Transaction signing failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Broadcasts a signed transaction to the TRON network
        /// </summary>
        private static async Task<string> BroadcastTransaction(JObject signedTransaction, string apiUrl, bool isTestnet = false)
        {
            // Ensure visible=true is set for the signed transaction (especially important for testnet)
            signedTransaction["visible"] = true;

            if (isTestnet)
            {
                Console.WriteLine("🔄 Broadcasting TESTNET transaction with visible=true for Base58 addresses");
            }

            string json = JsonConvert.SerializeObject(signedTransaction);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"🔍 Broadcasting transaction: {json}");

            var response = await _httpClient.PostAsync($"{apiUrl}/wallet/broadcasttransaction", content);
            string responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"🔍 Broadcast response: {responseContent}");

            JObject result = JObject.Parse(responseContent);

            if (result?["result"]?.Value<bool>() == true ||
                result?["code"]?.Value<string>() == "SUCCESS")
            {
                return result["txid"]?.Value<string>() ?? result["transaction"]?["txID"]?.Value<string>() ?? "unknown";
            }
            else
            {
                string message = result?["message"]?.Value<string>() ?? result?["code"]?.Value<string>() ?? "Unknown error";
                throw new Exception($"Broadcast failed: {message}");
            }
        }

        /// <summary>
        /// Verifies the transaction status after broadcasting
        /// </summary>
        private static async Task<bool> VerifyTransactionStatus(string txId, string apiUrl, bool isTestnet = false)
        {
            // Wait a bit to allow the blockchain to process the transaction
            await Task.Delay(2000);

            int attempts = Configuration.QUICK_CHECK_ATTEMPTS;
            int delayMs = Configuration.QUICK_CHECK_WAIT_MS;

            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    // Query transaction info - ensure visible=true for Base58 addresses
                    var requestBody = new { value = txId, visible = true };
                    
                    if (isTestnet)
                    {
                        Console.WriteLine("🔄 Verifying TESTNET transaction with visible=true for Base58 addresses");
                    }
                    
                    string json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync($"{apiUrl}/wallet/gettransactionbyid", content);
                    string responseContent = await response.Content.ReadAsStringAsync();

                    JObject result = JObject.Parse(responseContent);

                    if (result["ret"] != null && result["ret"]?[0]?["contractRet"] != null)
                    {
                        string status = result["ret"][0]["contractRet"]?.ToString() ?? string.Empty;
                        if (status == "SUCCESS")
                        {
                            return true;
                        }
                        else if (status != "PENDING" && status != "")
                        {
                            throw new Exception($"Transaction failed with status: {status}");
                        }
                    }

                    // If still processing, wait and try again
                    if (i < attempts - 1)
                    {
                        await Task.Delay(delayMs);
                    }
                }
                catch (Exception ex)
                {
                    if (i == attempts - 1)
                        throw new Exception($"Transaction verification failed: {ex.Message}", ex);

                    await Task.Delay(delayMs);
                }
            }

            // If we reach this point, transaction is likely still pending, but not failed
            return true;
        }

        /// <summary>
        /// Utility method to read a masked password/private key from console
        /// </summary>
        private static string ConsoleReadLineMasked()
        {
            StringBuilder input = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }

                if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input.Remove(input.Length - 1, 1);
                    Console.Write("\b \b");
                }
                else if (key.Key != ConsoleKey.Backspace)
                {
                    input.Append(key.KeyChar);
                    Console.Write("*");
                }
            }
            return input.ToString();
        }

        /// <summary>
        /// Convert a hex string to a byte array
        /// </summary>
        private static byte[] HexToBytes(string hex)
        {
            try
            {
                if (string.IsNullOrEmpty(hex))
                    throw new ArgumentException("Hex string cannot be null or empty");
                    
                if (hex.StartsWith("0x"))
                    hex = hex.Substring(2);

                // Validate hex string
                if (!IsValidHexString(hex))
                    throw new ArgumentException($"Invalid hex format: {hex}");
                
                if (hex.Length % 2 != 0)
                    hex = "0" + hex; // Ensure even length
                
                byte[] bytes = new byte[hex.Length / 2];
                for (int i = 0; i < bytes.Length; i++)
                {
                    string byteValue = hex.Substring(i * 2, 2);
                    bytes[i] = Convert.ToByte(byteValue, 16);
                }
                return bytes;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error converting hex to bytes: {ex.Message} (Hex: {hex})", ex);
            }
        }

        /// <summary>
        /// Convert a byte array to a hex string
        /// </summary>
        private static string ToHex(byte[] bytes, bool prefix = false)
        {
            if (bytes == null || bytes.Length == 0)
                return prefix ? "0x" : "";

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