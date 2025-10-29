using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using USDT_TRC20Transfer;
using System.Text;
using System.Linq;
using System.Security.Cryptography;

namespace USDT_TRC20Transfer
{
    class Program
    {
        // Initialize with empty configuration to avoid CS8618
        public static IConfiguration Configuration { get; private set; } = new ConfigurationBuilder().Build();

        static async Task Main(string[] args)
        {
            // Set up configuration first but don't show output
            ConfigureServices(false);

            // Immediately show the main menu
            await RunMainMenu();
        }

        private static async Task RunMainMenu()
        {
            bool exitProgram = false;

            while (!exitProgram)
            {
                // Clear the console and display the menu right away
                Console.Clear();
                Console.WriteLine("🔷 TRON (TRX/TRC20) İşlem Menüsü 🔷");
                Console.WriteLine("===================================");
                Console.WriteLine("1 - TRC20 Private Key'den Adres Doğrulama");
                Console.WriteLine("2 - TRX Bakiye Sorgulama");
                Console.WriteLine("3 - USDT (TRC20) Bakiye Sorgulama");
                Console.WriteLine("4 - Gerçek USDT (TRC20) Transferi (MainNet)");
                Console.WriteLine("5 - Test USDT (TRC20) Transferi (TestNet)");
                Console.WriteLine("6 - USDT (TRC20) Ağ Seçimli Transfer");
                Console.WriteLine("7 - Test Fonksiyonları Çalıştır");
                Console.WriteLine("8 - TRON Adres Dönüşüm Testi (Kapsamlı)");
                Console.WriteLine("9 - Ayarlar ve Bilgi");
                Console.WriteLine("0 - Çıkış");
                Console.WriteLine("===================================");
                Console.Write("Seçiminiz: ");

                string choice = Console.ReadLine()?.Trim() ?? "";

                // Clear screen for action
                Console.Clear();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await Task.Run(() => VerifyTronAddressFromPrivateKey());
                            break;
                        case "2":
                            await CheckTrxBalance();
                            break;
                        case "3":
                            await CheckUsdtBalance();
                            break;
                        case "4":
                            // Gerçek USDT Transferi (MainNet) - doğrudan MainNet kullanır
                            await RunMainNetTransfer();
                            break;
                        case "5":
                            // Test USDT Transferi (TestNet) - doğrudan TestNet kullanır
                            await RunTestNetTransferWithSignatureCheck();
                            break;
                        case "6":
                            // Ağ seçimli USDT transferi
                            await USDT_TRC20TransferClass.RunProductionTransfer();
                            break;
                        case "7":
                            // Test Fonksiyonları
                            await RunTestFunctions();
                            break;
                        case "8":
                            // Kapsamlı TRON adres dönüşüm testi
                            await TronAddressTest.RunTest();
                            break;
                        case "9":
                            // Show the application settings and info
                            ShowConfigurationInfo();
                            break;
                        case "0":
                            exitProgram = true;
                            Console.WriteLine("Program kapatılıyor...");
                            break;
                        default:
                            Console.WriteLine("❌ Geçersiz seçim! Lütfen tekrar deneyin.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Hata: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"   Ayrıntı: {ex.InnerException.Message}");
                    }
                }

                if (!exitProgram)
                {
                    Console.WriteLine("\nAna menüye dönmek için bir tuşa basın...");
                    Console.ReadKey();
                }
            }
        }

        private static void ConfigureServices(bool showOutput = true)
        {
            try
            {
                // Build configuration
                var builder = new ConfigurationBuilder();
                var basePath = Directory.GetCurrentDirectory();

                // In .NET 9, you don't need SetBasePath anymore
                builder.AddJsonFile(Path.Combine(basePath, "appsettings.json"), optional: false, reloadOnChange: true)
                       .AddEnvironmentVariables();

                Configuration = builder.Build();

                if (showOutput)
                    Console.WriteLine($"Configuration loaded from {Path.Combine(basePath, "appsettings.json")}");
            }
            catch (Exception ex)
            {
                if (showOutput)
                    Console.WriteLine($"Error loading configuration: {ex.Message}");
                
                // Create a default configuration to prevent null references
                Configuration = new ConfigurationBuilder().Build();
            }
        }

        // Show configuration info and settings
        private static void ShowConfigurationInfo()
        {
            Console.WriteLine("🔧 Uygulama Ayarları ve Bilgi");
            Console.WriteLine("----------------------------");
            
            try
            {
                Console.WriteLine("\nAPI Bağlantı Adresleri:");
                Console.WriteLine($"MainNet API: {Configuration["MAINNET_API"]}");
                Console.WriteLine($"TestNet API: {Configuration["TESTNET_API"]}");
                
                Console.WriteLine("\nBlokzincir Tarayıcıları:");
                Console.WriteLine($"MainNet Explorer: {Configuration["MAINNET_EXPLORER"]}");
                Console.WriteLine($"TestNet Explorer: {Configuration["TESTNET_EXPLORER"]}");
                
                Console.WriteLine("\nVarsayılan Adres:");
                Console.WriteLine($"Varsayılan Cüzdan: {Configuration["DefaultWallet"]}");
                
                Console.WriteLine("\nKontrat Adresleri:");
                Console.WriteLine($"USDT Kontrat Adresi: {Configuration["Contracts:USDT"]}");
                
                Console.WriteLine("\nUygulama Bilgisi:");
                Console.WriteLine("Sürüm: 1.0.0");
                Console.WriteLine("SDK Sürümü: .NET 9.0");
                Console.WriteLine("Geliştirici: Örnek Yazılım Ltd. Şti.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Ayar bilgileri gösterilirken hata oluştu: {ex.Message}");
            }
        }

        // TronAddressVerifier ile private key'den adres doğrulama işlemini gerçekleştirir
        private static void VerifyTronAddressFromPrivateKey()
        {
            Console.WriteLine("🔐 TRC20 Private Key'den Adres Doğrulama");
            Console.WriteLine("----------------------------------------");

            Console.Write("Private Key girin (64 karakter hex): ");
            string privateKey = Console.ReadLine()?.Trim() ?? "";

            Console.Write("Doğrulanacak adres (opsiyonel): ");
            string addressToVerify = Console.ReadLine()?.Trim() ?? "";

            var result = TronAddressVerifier.GenerateAndVerifyTronAddress(privateKey, addressToVerify);

            Console.WriteLine("\n🔹 Sonuçlar:");
            Console.WriteLine($"Public Key: {result.PublicKey}");
            Console.WriteLine($"Üretilen TRC20 Adresi: {result.GeneratedAddress}");

            if (!string.IsNullOrEmpty(addressToVerify))
            {
                Console.WriteLine($"Doğrulama Sonucu: {(result.IsMatch ? "✅ Adres eşleşiyor!" : "❌ Adres eşleşmiyor!")}");
            }
        }

        // TRX_TRCAccountBalance ile TRX bakiye sorgulama işlemini gerçekleştirir
        private static async Task CheckTrxBalance()
        {
            Console.WriteLine("💰 TRX Bakiye Sorgulama");
            Console.WriteLine("------------------------");

            string? walletAddress = PromptForWalletAddress();
            if (string.IsNullOrEmpty(walletAddress))
            {
                Console.WriteLine("Varsayılan cüzdan adresi kullanılacak.");
            }

            try
            {
                Console.WriteLine($"\nTRX bakiyesi sorgulanıyor ({(string.IsNullOrEmpty(walletAddress) ? "varsayılan adres" : walletAddress)}), lütfen bekleyin...");

                var trxBalanceService = new TRX_TRC20AccountBalance();
                if (!string.IsNullOrEmpty(walletAddress))
                {
                    trxBalanceService.WalletAddress = walletAddress;
                }
                else
                {
                    // Use default wallet from configuration
                    string? defaultWallet = Configuration["DefaultWallet"];
                    trxBalanceService.WalletAddress = defaultWallet ?? trxBalanceService.WalletAddress;
                }

                decimal balance = await trxBalanceService.GetTrxBalance();

                Console.WriteLine($"\n💲 TRX Bakiyeniz: {balance:N6} TRX");
                Console.WriteLine($"   Kullanılan adres: {trxBalanceService.WalletAddress}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ TRX bakiye sorgulaması başarısız: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Ayrıntı: {ex.InnerException.Message}");
                }
            }
        }

        // USDT_TRC20AccountBalance ile USDT bakiye sorgulama işlemini gerçekleştirir
        private static async Task CheckUsdtBalance()
        {
            Console.WriteLine("💵 USDT (TRC20) Bakiye Sorgulama");
            Console.WriteLine("--------------------------------");

            string? walletAddress = PromptForWalletAddress();
            if (string.IsNullOrEmpty(walletAddress))
            {
                Console.WriteLine("Varsayılan cüzdan adresi kullanılacak.");
            }

            try
            {
                Console.WriteLine($"\nUSDT bakiyesi sorgulanıyor ({(string.IsNullOrEmpty(walletAddress) ? "varsayılan adres" : walletAddress)}), lütfen bekleyin...");

                var usdtBalanceService = new USDT_TRC20AccountBalance();
                if (!string.IsNullOrEmpty(walletAddress))
                {
                    usdtBalanceService.WalletAddress = walletAddress;
                }
                else
                {
                    // Use default wallet from configuration
                    string? defaultWallet = Configuration["DefaultWallet"];
                    usdtBalanceService.WalletAddress = defaultWallet ?? usdtBalanceService.WalletAddress;
                }

                decimal balance = await usdtBalanceService.GetUsdtBalance();

                Console.WriteLine($"\n💲 USDT (TRC20) Bakiyeniz: {balance:N2} USDT");
                Console.WriteLine($"   Kullanılan adres: {usdtBalanceService.WalletAddress}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ USDT bakiye sorgulaması başarısız: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Ayrıntı: {ex.InnerException.Message}");
                }
            }
        }

        // Mainnet USDT Transfer
        private static async Task RunMainNetTransfer()
        {
            Console.WriteLine("🔷 USDT (TRC20) Transferi - MAINNET");
            Console.WriteLine("----------------------------------");

            Console.Write("Gönderici (From) özel anahtarını girin: ");
            string fromPrivateKey = ConsoleReadLineMasked();
            
            Console.Write("Alıcı (To) adresini girin: ");
            string toAddress = Console.ReadLine()?.Trim() ?? "";
            
            Console.Write("Transfer miktarını girin (USDT): ");
            if (!decimal.TryParse(Console.ReadLine()?.Trim(), out decimal amount) || amount <= 0)
            {
                Console.WriteLine("❌ Geçersiz miktar! Transfer işlemi iptal edildi.");
                return;
            }
            
            // özel anahtardan adres üretme
            var addrResult = TronAddressVerifier.GenerateAndVerifyTronAddress(fromPrivateKey);
            string fromAddress = addrResult.GeneratedAddress;
            
            Console.WriteLine($"\n💱 Transfer Detayları:");
            Console.WriteLine($"Gönderen: {fromAddress}");
            Console.WriteLine($"Alıcı: {toAddress}");
            Console.WriteLine($"Miktar: {amount:N2} USDT");
            Console.WriteLine($"Ağ: MAINNET");
            
            Console.Write("\nOnaylıyor musunuz? (E/H): ");
            string confirm = Console.ReadLine()?.Trim().ToUpper() ?? "H";
            
            if (confirm != "E")
            {
                Console.WriteLine("\n❌ İşlem kullanıcı tarafından iptal edildi.");
                return;
            }
            
            try
            {
                string txId = await USDT_TRC20TransferClass.RunMainNetTransfer(fromPrivateKey, toAddress, amount);
                
                Console.WriteLine($"\n✅ Transfer başarıyla tamamlandı!");
                Console.WriteLine($"İşlem ID (TxID): {txId}");
                Console.WriteLine($"İşlem detayları: {Configuration["MAINNET_EXPLORER"]}{txId}");
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
        
        // Testnet USDT Transfer with Signature Check
        private static async Task RunTestNetTransferWithSignatureCheck()
        {
            Console.WriteLine("🔷 USDT (TRC20) Test Transferi - TESTNET (Shasta) - İmza Doğrulama ile");
            Console.WriteLine("----------------------------------------------------------------------");
            
            Console.Write("Gönderici (From) özel anahtarını girin: ");
            string fromPrivateKey = ConsoleReadLineMasked();
            
            // İmza doğrulama işlemi
            Console.WriteLine("\n🔐 Özel anahtar ile imza doğrulaması yapılıyor...");
            bool signatureValid = await Task.Run(() => PerformSignatureVerification(fromPrivateKey));
            
            if (!signatureValid)
            {
                Console.WriteLine("\n⚠️ İmza doğrulaması başarısız oldu. İşleme devam etmek istiyor musunuz? (E/H): ");
                string continueAnyway = Console.ReadLine()?.Trim().ToUpper() ?? "H";
                
                if (continueAnyway != "E")
                {
                    Console.WriteLine("\n❌ İşlem iptal edildi.");
                    return;
                }
                
                Console.WriteLine("\n⚠️ Uyarıya rağmen işleme devam ediliyor. İşlem başarısız olabilir!");
            }
            else
            {
                Console.WriteLine("\n✅ İmza doğrulaması başarılı. İşleme devam ediliyor.");
            }
            
            // Adres oluşturma
            var addrResult = TronAddressVerifier.GenerateAndVerifyTronAddress(fromPrivateKey);
            string fromAddress = addrResult.GeneratedAddress;
            Console.WriteLine($"\n📬 Gönderici adresi: {fromAddress}");
            
            Console.Write("Alıcı (To) adresini girin: ");
            string toAddress = Console.ReadLine()?.Trim() ?? "";
            
            Console.Write("Transfer miktarını girin (USDT): ");
            if (!decimal.TryParse(Console.ReadLine()?.Trim(), out decimal amount) || amount <= 0)
            {
                Console.WriteLine("❌ Geçersiz miktar! Transfer işlemi iptal edildi.");
                return;
            }
            
            Console.WriteLine($"\n💱 Transfer Detayları:");
            Console.WriteLine($"Gönderen: {fromAddress}");
            Console.WriteLine($"Alıcı: {toAddress}");
            Console.WriteLine($"Miktar: {amount:N2} USDT");
            Console.WriteLine($"Ağ: TESTNET (Shasta)");
            
            Console.Write("\nOnaylıyor musunuz? (E/H): ");
            string confirm = Console.ReadLine()?.Trim().ToUpper() ?? "H";
            
            if (confirm != "E")
            {
                Console.WriteLine("\n❌ İşlem kullanıcı tarafından iptal edildi.");
                return;
            }
            
            try
            {
                string txId = await USDT_TRC20TransferClass.RunTestNetTransfer(fromPrivateKey, toAddress, amount);
                
                Console.WriteLine($"\n✅ Transfer başarıyla tamamlandı!");
                Console.WriteLine($"İşlem ID (TxID): {txId}");
                Console.WriteLine($"İşlem detayları: {Configuration["TESTNET_EXPLORER"]}{txId}");
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

        // Özel anahtar ile imza doğrulaması yapan metot
        private static bool PerformSignatureVerification(string privateKey)
        {
            try
            {
                Console.WriteLine("🔐 İmza doğrulama işlemi başlatılıyor...");
                Console.WriteLine("Bu işlem, TRON ağına gönderilecek imzaların doğru şekilde oluşturulabildiğini test eder.");
                
                // İmza test mesajı - transfer için özelleştirilmiş
                string testMessage = $"TRON_TESTNET_TRANSFER_VERIFICATION_{DateTime.UtcNow:yyyy-MM-dd}";
                Console.WriteLine($"Test mesajı: \"{testMessage}\"");
                
                // İmza testi yapılıyor
                bool signatureResult = TronSignature.TestSignature(privateKey, testMessage);
                
                if (signatureResult)
                {
                    Console.WriteLine("\n✅ İmza doğrulaması BAŞARILI!");
                    Console.WriteLine("Özel anahtarınız ile geçerli bir TRON imzası oluşturabilirsiniz.");
                    Console.WriteLine("Transfer işlemi için imza doğrulaması geçildi.");
                }
                else
                {
                    Console.WriteLine("\n❌ İmza doğrulaması BAŞARISIZ!");
                    Console.WriteLine("Özel anahtarınız ile geçerli bir TRON imzası oluşturulamadı.");
                    Console.WriteLine("Bu durum transfer sırasında sorunlara yol açabilir.");
                    Console.WriteLine("Özel anahtarınızı kontrol edin ve tekrar deneyin.");
                }
                
                return signatureResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ İmza doğrulama sırasında bir hata oluştu: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Ayrıntı: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        // Test fonksiyonları için kullanılacak demo fonksiyonları
        private static async Task RunTestFunctions()
        {
            Console.WriteLine("🧪 Test Fonksiyonları");
            Console.WriteLine("---------------------");
            Console.WriteLine("1 - İmza Testi (Signature Test)");
            Console.WriteLine("2 - Adres Doğrulama Testi");
            Console.WriteLine("3 - TRON Adres Dönüşüm Testi (Basit)");
            Console.WriteLine("4 - TRON Adres Dönüşüm Testi (Kapsamlı)");
            Console.WriteLine("5 - Base58 Karakter Doğrulama Testi");
            Console.WriteLine("6 - API Bağlantı Testi");
            Console.WriteLine("7 - Bakiye Sorgulama Testi");
            Console.WriteLine("8 - Transaction Hash Testi");
            Console.WriteLine("0 - Geri Dön");
            Console.Write("\nSeçiminiz: ");

            string choice = Console.ReadLine()?.Trim() ?? "";

            switch (choice)
            {
                case "1":
                    await Task.Run(() => RunSignatureTest());
                    break;
                case "2":
                    await Task.Run(() => RunAddressVerificationTest());
                    break;
                case "3":
                    await Task.Run(() => RunAddressConversionTest());
                    break;
                case "4":
                    await TronAddressTest.RunTest();
                    break;
                case "5":
                    await Task.Run(() => RunBase58ValidationTest());
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("\nBu test fonksiyonu henüz uygulanmamıştır.");
                    Console.WriteLine("İlgili testler daha sonraki sürümlerde eklenecektir.");
                    break;
            }

            // Add a minimal await operation to avoid CS1998 warning
            await Task.Delay(1);
            return;
        }

        // Base58 karakter doğrulama testi
        private static void RunBase58ValidationTest()
        {
            Console.WriteLine("🔍 Base58 Karakter Doğrulama Testi");
            Console.WriteLine("----------------------------------");
            
            Console.Write("TRON adresini girin (T ile başlayan): ");
            string tronAddress = Console.ReadLine()?.Trim() ?? "";
            
            if (string.IsNullOrEmpty(tronAddress))
            {
                Console.WriteLine("❌ Adres boş olamaz!");
                return;
            }
            
            try
            {
                // Check for illegal Base58 characters
                string base58Chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
                for (int i = 0; i < tronAddress.Length; i++)
                {
                    char c = tronAddress[i];
                    if (base58Chars.IndexOf(c) < 0)
                    {
                        Console.WriteLine($"\n❌ Geçersiz Base58 karakteri: '{c}' pozisyon {i}'de!");
                        Console.WriteLine("   TRON adresleri şu karakterleri içeremez: 0, O, I, l, +, /");
                        return;
                    }
                }
                
                Console.WriteLine("\n✅ Adres geçerli Base58 karakterleri içeriyor.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Base58 doğrulama hatası: {ex.Message}");
            }
        }

        // İmza testi fonksiyonu
        private static void RunSignatureTest()
        {
            Console.WriteLine("🔐 TRON İmza Testi (Signature Test)");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Bu test, imzalama işleminin doğru çalıştığını doğrular.");
            Console.WriteLine("Transfer işlemi yapmadan önce imza mekanizmasını kontrol etmek için kullanılabilir.");
            Console.WriteLine();
            
            Console.Write("Private Key girin (64 karakter hex): ");
            string privateKey = ConsoleReadLineMasked();
            
            if (string.IsNullOrEmpty(privateKey))
            {
                Console.WriteLine("❌ Private key boş olamaz!");
                return;
            }
            
            Console.Write("Test mesajı girin (opsiyonel, boş bırakılırsa 'TEST MESSAGE' kullanılacak): ");
            string testMessage = Console.ReadLine()?.Trim() ?? "";
            
            Console.WriteLine("\nİmza testi başlatılıyor...");
            
            // Özel anahtardan adres türetme (doğrulama için)
            var addrResult = TronAddressVerifier.GenerateAndVerifyTronAddress(privateKey);
            Console.WriteLine($"📬 Türetilen adres: {addrResult.GeneratedAddress}");
            
            // İmza testi
            bool testResult;
            if (string.IsNullOrEmpty(testMessage))
            {
                testResult = TronSignature.TestSignature(privateKey);
            }
            else
            {
                testResult = TronSignature.TestSignature(privateKey, testMessage);
            }
            
            // Sonuç gösterimi
            Console.WriteLine();
            if (testResult)
            {
                Console.WriteLine("\n✅ İMZA TESTİ BAŞARILI!");
                Console.WriteLine("İmzalama işlemi doğru şekilde çalışıyor ve doğrulanabiliyor.");
                Console.WriteLine("Bu sonuç, transfer işlemlerinin başarılı olma olasılığını artırır.");
                Console.WriteLine("\nImza özelliklerinin özeti:");
                Console.WriteLine("- TRON imzaları 65 byte uzunluğundadır");
                Console.WriteLine("- R ve S bileşenleri 32'şer byte");
                Console.WriteLine("- V bileşeni 0 veya 1 olmalıdır (Ethereum'da 27 veya 28)");
            }
            else
            {
                Console.WriteLine("\n❌ İMZA TESTİ BAŞARISIZ!");
                Console.WriteLine("İmzalama işlemi sırasında bir sorun oluştu.");
                Console.WriteLine("Lütfen private key'in doğru olduğundan emin olun.");
                Console.WriteLine("Eğer hata devam ederse aşağıdaki önlemleri deneyin:");
                Console.WriteLine("1. Private key'in 64 karakter uzunluğunda olduğundan emin olun");
                Console.WriteLine("2. Private key'in geçerli bir TRON özel anahtarı olduğunu doğrulayın");
                Console.WriteLine("3. Nethereum.Signer kütüphanesinin güncel olduğunu kontrol edin");
            }
        }
        
        // TRON adres doğrulama testi
        private static void RunAddressVerificationTest()
        {
            Console.WriteLine("🔍 TRON Adres Doğrulama Testi");
            Console.WriteLine("-----------------------------");
            
            Console.Write("TRON adresini girin (T ile başlayan): ");
            string tronAddress = Console.ReadLine()?.Trim() ?? "";
            
            if (string.IsNullOrEmpty(tronAddress))
            {
                Console.WriteLine("❌ Adres boş olamaz!");
                return;
            }
            
            try
            {
                // Adres formatını kontrol et
                if (!tronAddress.StartsWith("T") || tronAddress.Length != 34)
                {
                    Console.WriteLine("❌ Geçersiz TRON adres formatı! Adres 'T' ile başlamalı ve 34 karakter uzunluğunda olmalıdır.");
                    return;
                }
                
                // Base58 formatında çözümle
                byte[] decoded = SimpleBase.Base58.Bitcoin.Decode(tronAddress).ToArray();
                
                Console.WriteLine($"\nBase58 çözümleme sonucu ({decoded.Length} byte): {BitConverter.ToString(decoded).Replace("-", "")}");
                
                if (decoded.Length != 25)
                {
                    Console.WriteLine($"❌ Hatalı adres uzunluğu: {decoded.Length} byte (beklenen: 25 byte)");
                    return;
                }
                
                // İlk byte 0x41 (65) olmalı
                if (decoded[0] != 0x41)
                {
                    Console.WriteLine($"❌ Hatalı adres öneki: 0x{decoded[0]:X2} (beklenen: 0x41)");
                    return;
                }
                
                // Adres ve checksum kısımlarını ayır
                byte[] addressPortion = new byte[21];
                byte[] checksumPortion = new byte[4];
                
                Buffer.BlockCopy(decoded, 0, addressPortion, 0, 21);
                Buffer.BlockCopy(decoded, 21, checksumPortion, 0, 4);
                
                // Checksum doğrulama
                byte[] expectedChecksum;
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] firstHash = sha256.ComputeHash(addressPortion);
                    byte[] secondHash = sha256.ComputeHash(firstHash);
                    expectedChecksum = secondHash.Take(4).ToArray();
                }
                
                bool checksumValid = expectedChecksum.SequenceEqual(checksumPortion);
                
                Console.WriteLine($"Adres bölümü (21 byte): {BitConverter.ToString(addressPortion).Replace("-", "")}");
                Console.WriteLine($"Checksum bölümü (4 byte): {BitConverter.ToString(checksumPortion).Replace("-", "")}");
                Console.WriteLine($"Hesaplanan checksum: {BitConverter.ToString(expectedChecksum).Replace("-", "")}");
                Console.WriteLine($"Checksum doğrulaması: {(checksumValid ? "✅ Geçerli" : "❌ Geçersiz")}");
                
                if (checksumValid)
                {
                    Console.WriteLine($"\n✅ {tronAddress} geçerli bir TRON adresidir.");
                }
                else
                {
                    Console.WriteLine($"\n❌ {tronAddress} geçerli bir TRON adresi DEĞİLDİR!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Adres doğrulama hatası: {ex.Message}");
            }
        }
        
        // TRON adres dönüşüm testi (basit versiyon)
        private static void RunAddressConversionTest()
        {
            Console.WriteLine("🔄 TRON Adres Dönüşüm Testi (Basit)");
            Console.WriteLine("-----------------------------------");
            Console.WriteLine("Bu test, Base58 ve Hex formatları arasında dönüşümü test eder.");
            Console.WriteLine();
            
            // Önceden belirtilen adres testi için
            string defaultBase58 = "TDTpihBx5tAUhviF9GeGqFdmfJHc4Pd6Xc";
            string expectedHex = "412651d70c64be830cbeda3aebe274567fe19ebb0a";
            
            Console.WriteLine($"Test edilecek örnek:");
            Console.WriteLine($"Base58: {defaultBase58}");
            Console.WriteLine($"Beklenen Hex: {expectedHex}");
            
            try
            {
                string actualHex = TronSignature.Base58ToHex(defaultBase58);
                
                Console.WriteLine($"\nSonuç:");
                Console.WriteLine($"Üretilen Hex: {actualHex}");
                
                bool isCorrect = actualHex.Equals(expectedHex, StringComparison.OrdinalIgnoreCase);
                
                Console.WriteLine($"Doğrulama: {(isCorrect ? "✅ BAŞARILI! Beklenen ile eşleşiyor." : "❌ BAŞARISIZ! Beklenen hex ile eşleşmiyor.")}");
                
                // Özel adres testi
                Console.WriteLine("\n--------------------------------");
                Console.WriteLine("Kendi adresinizle test yapmak ister misiniz? (E/H)");
                string choice = Console.ReadLine()?.Trim().ToUpper() ?? "H";
                
                if (choice == "E")
                {
                    Console.Write("\nTRON adresini girin (T ile başlayan): ");
                    string customAddress = Console.ReadLine()?.Trim() ?? "";
                    
                    if (!string.IsNullOrEmpty(customAddress))
                    {
                        string customHex = TronSignature.Base58ToHex(customAddress);
                        Console.WriteLine($"Adres: {customAddress}");
                        Console.WriteLine($"Hex Karşılığı: {customHex}");
                        
                        // Tersine dönüşüm
                        Console.WriteLine("\nTersine dönüşüm testi yapılıyor...");
                        string convertedBack = TronSignature.HexToBase58Check(customHex);
                        Console.WriteLine($"Yeniden Base58'e dönüştürülen adres: {convertedBack}");
                        Console.WriteLine($"Orijinal adresle eşleşme: {(convertedBack == customAddress ? "✅ EVET" : "❌ HAYIR")}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Dönüşüm hatası: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Detay: {ex.InnerException.Message}");
                }
            }
        }

        // Kullanıcıdan cüzdan adresi girmesini ister ve doğrular
        private static string? PromptForWalletAddress()
        {
            Console.WriteLine("👛 Cüzdan adresinizi girin (T ile başlayan 34 karakterlik adres)");
            Console.WriteLine("   Boş bırakırsanız varsayılan adres kullanılacaktır.");
            Console.Write("➤ ");

            string address = Console.ReadLine()?.Trim() ?? "";

            if (string.IsNullOrEmpty(address))
            {
                return null;
            }

            // Basit adres kontrolü
            if (!address.StartsWith("T") || address.Length != 34)
            {
                Console.WriteLine("❌ Geçersiz TRX cüzdan adresi! Adres 'T' ile başlamalı ve 34 karakter olmalıdır.");
                Console.WriteLine("   Varsayılan adres kullanılacak.");
                return null;
            }

            return address;
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
    }
}