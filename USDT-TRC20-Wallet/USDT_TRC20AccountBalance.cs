using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

namespace USDT_TRC20Transfer
{
    public class USDT_TRC20AccountBalance
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration? _configuration;

        /// <summary>
        /// Default wallet address
        /// </summary>
        public string WalletAddress { get; set; } = string.Empty;

        public USDT_TRC20AccountBalance()
        {
            _httpClient = new HttpClient();
            _configuration = USDT_TRC20Transfer.Program.Configuration;

            // Get default wallet address from configuration
            WalletAddress = _configuration?["DefaultWallet"] ?? "TCVb2hz7ULDn2LjsuJpUZCisr963hhXswF";
        }

        public USDT_TRC20AccountBalance(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = USDT_TRC20Transfer.Program.Configuration;

            // Get default wallet address from configuration
            WalletAddress = _configuration?["DefaultWallet"] ?? "TCVb2hz7ULDn2LjsuJpUZCisr963hhXswF";
        }

        /// <summary>
        /// Gets the USDT TRC20 balance for a specified wallet address with improved error handling and multiple API fallbacks
        /// </summary>
        /// <param name="address">The TRX wallet address</param>
        /// <returns>Balance in USDT</returns>
        public async Task<decimal> GetUsdtBalance(string address)
        {
            // Basit adres kontrolü
            if (string.IsNullOrEmpty(address) || !address.StartsWith("T") || address.Length != 34)
            {
                throw new ArgumentException("Geçersiz TRX adresi! Adres 'T' ile başlamalı ve 34 karakter olmalı.", nameof(address));
            }

            try
            {
                // Get configuration values
                string usdtContract = _configuration?["Contracts:USDT"] ?? "TR7NHqjeKQxGTCi8q8ZY4pL8otSzgjLj6t";
                string tronScanApiUrl = _configuration?["ApiEndpoints:TronScan"] ?? "https://apilist.tronscanapi.com/api/account/tokens";
                string tronGridApiUrl = _configuration?["ApiEndpoints:TronGrid:Mainnet"] ?? "https://api.trongrid.io";

                // Önce TronScan API'yi deneyelim
                string apiUrl = $"{tronScanApiUrl}?address={address}&start=0&limit=20&hidden=0&show=0&sortType=0&sortBy=0&token=";

                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(jsonResponse);

                    JToken? dataToken = json["data"];
                    if (dataToken != null)
                    {
                        foreach (JToken? token in dataToken)
                        {
                            if (token == null) continue;

                            string? tokenAddress = token["tokenId"]?.ToString();
                            string? tokenName = token["tokenName"]?.ToString();

                            // USDT'yi bul (hem contract hem de isim kontrolü)
                            if (tokenAddress == usdtContract || tokenName == "Tether USD")
                            {
                                string? balanceStr = token["balance"]?.ToString();
                                int decimals = token["tokenDecimal"]?.Value<int>() ?? 6;

                                if (balanceStr != null && decimal.TryParse(balanceStr, out decimal balance))
                                {
                                    return balance / (decimal)Math.Pow(10, decimals);
                                }
                            }
                        }
                    }
                }

                // TronScan başarısız olursa TronGrid'i dene
                apiUrl = $"{tronGridApiUrl}/v1/accounts/{address}/tokens";
                response = await _httpClient.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    JObject json = JObject.Parse(jsonResponse);

                    JToken? dataToken = json["data"];
                    if (dataToken != null)
                    {
                        foreach (JToken? token in dataToken)
                        {
                            if (token == null) continue;

                            string? tokenAddress = token["token_id"]?.ToString();

                            if (tokenAddress == usdtContract)
                            {
                                string? balanceStr = token["balance"]?.ToString();
                                if (balanceStr != null && decimal.TryParse(balanceStr, out decimal balance))
                                {
                                    return balance / 1_000_000m;
                                }
                            }
                        }
                    }
                }

                return 0; // Hiçbir API'den USDT bakiyesi alınamadı
            }
            catch (Exception ex)
            {
                throw new Exception($"USDT balance query failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the USDT TRC20 balance using the default wallet address
        /// </summary>
        /// <returns>Balance in USDT</returns>
        public async Task<decimal> GetUsdtBalance()
        {
            return await GetUsdtBalance(WalletAddress);
        }

        /// <summary>
        /// Prompts the user to enter a wallet address and validates it
        /// </summary>
        /// <returns>A valid TRX wallet address or null if validation fails</returns>
        public static string? PromptForWalletAddress()
        {
            Console.WriteLine("TRX cüzdan adresinizi girin:");
            string? walletAddress = Console.ReadLine();

            // Basit adres kontrolü
            if (string.IsNullOrEmpty(walletAddress) || !walletAddress.StartsWith("T") || walletAddress.Length != 34)
            {
                Console.WriteLine("Geçersiz TRX adresi! Adres 'T' ile başlamalı ve 34 karakter olmalı.");
                return null;
            }

            Console.WriteLine($"Bakiye sorgulanıyor: {walletAddress}");
            return walletAddress;
        }
    }
}