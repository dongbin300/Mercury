using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot;

using Mercury.Cryptos.Binance;
using Mercury.Extensions;

using System.Globalization;
using System.Text.RegularExpressions;

namespace Mercury.Apis
{
    public class LocalApi
	{
		public static List<string> SymbolNames = new();

		#region Initialize
		public static void Init()
		{
			SymbolNames = GetSymbolNames();
		}
		#endregion

		#region Market API
		public static List<string> GetSymbolNames()
		{
			var symbolFile = new DirectoryInfo(MercuryPath.BinanceFuturesData).GetFiles("symbol_*.txt").OrderByDescending(x => x.LastAccessTime).FirstOrDefault() ?? default!;
			return File.ReadAllLines(symbolFile.FullName).ToList();
		}

		public static List<BinanceFuturesSymbol> GetSymbols()
		{
			var symbolFile = new DirectoryInfo(MercuryPath.BinanceFuturesData).GetFiles("symbol_detail_*.csv").OrderDescending().FirstOrDefault() ?? default!;
			var data = File.ReadAllLines(symbolFile.FullName);

			var symbols = new List<BinanceFuturesSymbol>();
			for (int i = 1; i < data.Length; i++)
			{
				var item = data[i];
				var d = item.Split(',');

				if (!DateTime.TryParseExact(d[2], "yyyy-MM-dd ddd tt h:mm:ss", new CultureInfo("ko-KR"), DateTimeStyles.None, out var listingDate))
				{
					listingDate = new DateTime(1900, 1, 1);
				}

				symbols.Add(new BinanceFuturesSymbol(
					d[0],
					Convert.ToDecimal(d[1]),
					listingDate,
					Convert.ToDecimal(d[3]),
					Convert.ToDecimal(d[4]),
					Convert.ToDecimal(d[5]),
					Convert.ToDecimal(d[6]),
					Convert.ToDecimal(d[7]),
					Convert.ToDecimal(d[8]),
					Convert.ToInt32(d[9]),
					Convert.ToInt32(d[10]),
					(UnderlyingType)Enum.Parse(typeof(UnderlyingType), d[11])));
			}

			return symbols;
		}
		#endregion

		#region Chart API
		public static List<Quote> GetQuotesForOneDay(string symbol, DateTime startTime)
		{
			try
			{
				var data = File.ReadAllLines(MercuryPath.BinanceFuturesData.Down("1m", symbol, $"{symbol}_{startTime:yyyy-MM-dd}.csv"));

				var quotes = new List<Quote>();

				foreach (var d in data)
				{
					var e = d.Split(',');
					quotes.Add(new Quote
					{
						Date = DateTime.Parse(e[0]),
						Open = decimal.Parse(e[1]),
						High = decimal.Parse(e[2]),
						Low = decimal.Parse(e[3]),
						Close = decimal.Parse(e[4]),
						Volume = decimal.Parse(e[5])
					});
				}

				return quotes;
			}
			catch (FileNotFoundException)
			{
				throw;
			}
		}

		public static List<Quote> GetOneDayQuotes(string symbol)
		{
			try
			{
				var data = File.ReadAllLines(MercuryPath.BinanceFuturesData.Down("1D", $"{symbol}.csv"));

				var quotes = new List<Quote>();

				foreach (var d in data)
				{
					var e = d.Split(',');
					quotes.Add(new Quote
					{
						Date = DateTime.Parse(e[0]),
						Open = decimal.Parse(e[1]),
						High = decimal.Parse(e[2]),
						Low = decimal.Parse(e[3]),
						Close = decimal.Parse(e[4]),
						Volume = decimal.Parse(e[5])
					});
				}

				return quotes;
			}
			catch (FileNotFoundException)
			{
				throw;
			}
		}

		public static Dictionary<string, List<Quote>> GetAllOneDayQuotes()
		{
			try
			{
				var result = new Dictionary<string, List<Quote>>();

				var fileNames = Directory.GetFiles(MercuryPath.BinanceFuturesData.Down("1D"), "*.csv");
				foreach (var fileName in fileNames)
				{
					string symbol = fileName.GetOnlyFileName();
					result.Add(symbol, GetOneDayQuotes(symbol));
				}

				return result;
			}
			catch (FileNotFoundException)
			{
				throw;
			}
		}

		public static List<BinanceAggregatedTrade> GetOneDayTrades(string symbol, DateTime date)
		{
			try
			{
				var data = File.ReadAllLines(MercuryPath.BinanceFuturesData.Down("trade", symbol, $"{symbol}-aggTrades-{date:yyyy-MM-dd}.csv"));

				var trades = new List<BinanceAggregatedTrade>();

				foreach (var d in data)
				{
					var e = d.Split(',');
					trades.Add(new BinanceAggregatedTrade
					{
						Id = long.Parse(e[0]),
						Price = decimal.Parse(e[1]),
						Quantity = decimal.Parse(e[2]),
						FirstTradeId = long.Parse(e[3]),
						LastTradeId = long.Parse(e[4]),
						TradeTime = long.Parse(e[5]).TimeStampMillisecondsToDateTime(),
						BuyerIsMaker = bool.Parse(e[6])
					});
				}

				return trades;
			}
			catch (FileNotFoundException)
			{
				throw;
			}
		}
		#endregion

		#region Account API
		public static void GetSeed()
		{
			var data = File.ReadAllLines(MercuryPath.BinanceFuturesData.Down("SEED.txt"));
			Common.StartTime = DateTime.Parse(data[0]);
			Common.Seed = double.Parse(data[1]);
		}
		#endregion
	}
}
