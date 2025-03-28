using Mercury.Extensions;

namespace Mercury
{
    public class MercuryPath
    {
        public static string Desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public static string Base = "D:\\Assets";
        public static string BinanceApiKey = Base.Down("binance_api.txt");
        public static string BinanceFuturesData = Base.Down("BinanceFuturesData");
        public static string BinanceFutures1m = BinanceFuturesData.Down("1m");
        public static string BinanceFutures1D = BinanceFuturesData.Down("1D");
        public static string Stock1D = Base.Down("StockData", "Quotes");
    }
}
