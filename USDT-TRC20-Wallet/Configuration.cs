using Microsoft.Extensions.Configuration;

namespace USDT_TRC20Transfer
{
    public static class Configuration
    {
        private static IConfiguration Config => USDT_TRC20Transfer.Program.Configuration;

        // API Endpoints from configuration
        public static string MAINNET_API => Config?["ApiEndpoints:TronGrid:Mainnet"] ?? "https://api.trongrid.io";
        public static string TESTNET_API => Config?["ApiEndpoints:TronGrid:Testnet"] ?? "https://api.shasta.trongrid.io";

        // Explorer URLs from configuration
        public static string MAINNET_EXPLORER => Config?["ExplorerUrls:Mainnet"] ?? "https://tronscan.org/#/transaction/";
        public static string TESTNET_EXPLORER => Config?["ExplorerUrls:Testnet"] ?? "https://shasta.tronscan.org/#/transaction/";

        // Constants from configuration
        public static long MIN_TRANSFER_AMOUNT 
        {
            get
            {
                if (long.TryParse(Config?["TransferSettings:MinTransferAmountSun"], out long value))
                    return value;
                return 1;
            }
        }
        
        public static long MAX_TRANSFER_AMOUNT
        {
            get
            {
                if (long.TryParse(Config?["TransferSettings:MaxTransferAmountSun"], out long value))
                    return value;
                return 1000000000000;
            }
        }
        
        public static int QUICK_CHECK_ATTEMPTS
        {
            get
            {
                if (int.TryParse(Config?["TransferSettings:QuickCheckAttempts"], out int value))
                    return value;
                return 3;
            }
        }
        
        public static int QUICK_CHECK_WAIT_MS
        {
            get
            {
                if (int.TryParse(Config?["TransferSettings:QuickCheckWaitMs"], out int value))
                    return value;
                return 5000;
            }
        }
        
        public static long SUN_TO_TRX
        {
            get
            {
                if (long.TryParse(Config?["TransferSettings:SunToTrx"], out long value))
                    return value;
                return 1000000;
            }
        }
    }
}