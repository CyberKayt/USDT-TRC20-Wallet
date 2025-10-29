using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

namespace USDT_TRC20Transfer
{
    public class TRX_TRC20AccountBalance
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration? _configuration;

        /// <summary>
        /// Default wallet address
        /// </summary>
        public string WalletAddress { get; set; } = string.Empty;

        public TRX_TRC20AccountBalance()
        {
            _httpClient = new HttpClient();
            _configuration = USDT_TRC20Transfer.Program.Configuration;

            // Get default wallet address from configuration
            WalletAddress = _configuration?["DefaultWallet"] ?? "TCVb2hz7ULDn2LjsuJpUZCisr963hhXswF";
        }

        public TRX_TRC20AccountBalance(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configuration = USDT_TRC20Transfer.Program.Configuration;

            // Get default wallet address from configuration
            WalletAddress = _configuration?["DefaultWallet"] ?? "TCVb2hz7ULDn2LjsuJpUZCisr963hhXswF";
        }

        /// <summary>
        /// Gets the TRX balance for a specified wallet address
        /// </summary>
        /// <param name="address">The TRX wallet address</param>
        /// <returns>Balance in TRX</returns>
        public async Task<decimal> GetTrxBalance(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("Address cannot be null or empty.", nameof(address));

            try
            {
                // Get API URL from configuration
                string baseApiUrl = _configuration?["ApiEndpoints:TronGrid:Mainnet"] ?? "https://api.trongrid.io";

                // TronGrid API
                string apiUrl = $"{baseApiUrl}/v1/accounts/{address}";

                HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API error: {response.StatusCode}");
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(jsonResponse);

                // Check if data array exists and has elements
                JToken? dataToken = json["data"];
                if (dataToken != null && dataToken.Type == JTokenType.Array)
                {
                    JArray dataArray = (JArray)dataToken;
                    if (dataArray.Count > 0 && dataArray[0] != null)
                    {
                        JToken? firstAccount = dataArray[0];
                        // Balance is in Sun, convert to TRX by dividing by 1,000,000
                        long balanceInSun = firstAccount?["balance"]?.Value<long>() ?? 0;
                        
                        // Get SunToTrx from configuration or use default
                        long sunToTrx = 1_000_000; // Default value
                        
                        if (_configuration?["TransferSettings:SunToTrx"] != null)
                        {
                            if (long.TryParse(_configuration["TransferSettings:SunToTrx"], out long configValue))
                            {
                                sunToTrx = configValue;
                            }
                        }
                        
                        return balanceInSun / (decimal)sunToTrx; // Convert Sun to TRX
                    }
                    else
                    {
                        Console.WriteLine("Uyarı: Cüzdan için veri bulunamadı. Hesap yeni oluşturulmuş olabilir.");
                    }
                }
                else
                {
                    Console.WriteLine("Uyarı: API'den beklenmeyen yanıt format. Veri dizi içermiyor.");
                }

                // Alternative approach for debugging
                Console.WriteLine($"API Yanıtı: {jsonResponse}");

                return 0; // No balance found
            }
            catch (Exception ex)
            {
                throw new Exception($"TRX balance sorgulama hatası: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the TRX balance using the default wallet address
        /// </summary>
        /// <returns>Balance in TRX</returns>
        public async Task<decimal> GetTrxBalance()
        {
            return await GetTrxBalance(WalletAddress);
        }
    }
}