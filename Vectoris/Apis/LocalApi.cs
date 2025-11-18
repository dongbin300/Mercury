using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Spot;

using System.Globalization;

using Vectoris.Charts.Core;
using Vectoris.Extensions;

namespace Vectoris.Apis;

public class LocalApi
{
	public static List<string> FutureSymbolNames = [];

	#region Initialize
	public static void Init()
	{
		FutureSymbolNames = Futures.GetSymbolNames();
	}
	#endregion

	public class Futures
	{
		#region Market API
		public static List<string> GetSymbolNames()
		{
			var symbolFile = new DirectoryInfo(Paths.BinanceFuturesData).GetFiles("symbol_*.txt").OrderByDescending(x => x.LastAccessTime).FirstOrDefault() ?? default!;
			return [.. File.ReadAllLines(symbolFile.FullName)];
		}

		public static List<BinanceFuturesSymbol> GetSymbols()
		{
			var symbolFile = new DirectoryInfo(Paths.BinanceFuturesData).GetFiles("symbol_detail_*.csv").OrderDescending().FirstOrDefault() ?? default!;
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

				symbols.Add(new BinanceFuturesSymbol()
				{
					Name = d[0],
					LiquidationFee = d[1].ToDecimal(),
					ListingDate = listingDate,
					Filters =
					[
						new BinanceSymbolPriceFilter()
						{
							MaxPrice = d[3].ToDecimal(),
							MinPrice = d[4].ToDecimal(),
							TickSize = d[5].ToDecimal()
						},
						new BinanceSymbolLotSizeFilter()
						{
							MaxQuantity = d[6].ToDecimal(),
							MinQuantity = d[7].ToDecimal(),
							StepSize = d[8].ToDecimal()
						}
					],
					PricePrecision = d[9].ToInt(),
					QuantityPrecision = d[10].ToInt(),
					UnderlyingType = Enum.Parse<UnderlyingType>(d[11])
				});
			}

			return symbols;
		}
		#endregion

		#region Chart API
		public static List<Quote> GetDailyQuotes(string symbol, DateTime date)
		{
			try
			{
				var data = File.ReadAllLines(Paths.BinanceFuturesData.Down("1m", symbol, $"{symbol}_{date:yyyy-MM-dd}.csv"));

				var quotes = new List<Quote>();
				foreach (var d in data)
				{
					var e = d.Split(',');
					quotes.Add(new Quote(
						e[0].ToDateTime(),
						e[1].ToDecimal(),
						e[2].ToDecimal(),
						e[3].ToDecimal(),
						e[4].ToDecimal(),
						e[5].ToDecimal()
						));
				}

				return quotes;
			}
			catch (FileNotFoundException)
			{
				throw;
			}
		}
		#endregion
	}
}
