using Mercury.Extensions;

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

		/// <summary>
		/// BTCUSDT-prices-2020-01-01.csv 에서 2020-01-01 추출
		/// </summary>
		/// <param name="fileName"></param>
		public static DateTime GetDatePriceCsvFileName(string fileName)
		{
			var match = System.Text.RegularExpressions.Regex.Match(fileName, @"(\d{4}-\d{2}-\d{2})");
			if (match.Success)
			{
				return DateTime.ParseExact(match.Groups[1].Value, "yyyy-MM-dd", null);
			}
			throw new ArgumentException($"날짜를 찾을 수 없습니다: {fileName}");
		}
	}
}
