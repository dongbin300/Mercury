namespace Mercury.Cryptos
{
    public class CryptoSymbol
    {
        public static DateTime GetDate(string fileName)
        {
            return DateTime.Parse(fileName.Split('_', '.')[1]);
        }

        public static string GetStartDateFileName(string symbol)
        {
            return new DirectoryInfo(MercuryPath.BinanceFuturesData.Down("1m", symbol))
                .GetFiles("*.csv")
                .OrderBy(x => x.Name)
                .First().Name;
        }

        public static string GetEndDateFileName(string symbol)
        {
            return new DirectoryInfo(MercuryPath.BinanceFuturesData.Down("1m", symbol))
                .GetFiles("*.csv")
                .OrderByDescending(x => x.Name)
                .First().Name;
        }

        public static DateTime GetStartDate(string symbol)
        {
            return GetDate(GetStartDateFileName(symbol));
        }

        public static DateTime GetEndDate(string symbol)
        {
            return GetDate(GetEndDateFileName(symbol));
        }
    }
}
