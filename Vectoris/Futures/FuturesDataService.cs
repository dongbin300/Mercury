using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Spot;

using System.Globalization;

using Vectoris.Charts.Core;
using Vectoris.Extensions;

namespace Vectoris.Futures;

public partial class FuturesDataService
{
	public static List<string> SymbolNames = [];

	#region Initialize
	public static void Init()
	{
		SymbolNames = GetSymbolNames();
	}
	#endregion

	#region Symbol
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

	[System.Text.RegularExpressions.GeneratedRegex(@"(\d{4}-\d{2}-\d{2})")]
	private static partial System.Text.RegularExpressions.Regex DateRegex();
	public static DateTime GetDate(string fileName)
	{
		var match = DateRegex().Match(fileName);
		if (match.Success)
		{
			return DateTime.ParseExact(match.Groups[1].Value, "yyyy-MM-dd", null);
		}
		throw new ArgumentException($"날짜를 찾을 수 없습니다: {fileName}");
	}

	public static string GetStartDateFileName(string symbol) =>
		new DirectoryInfo(Paths.BinanceFuturesData.Down("1m", symbol)).GetFiles("*.csv").OrderBy(x => x.Name).First().Name;

	public static string GetEndDateFileName(string symbol) =>
		new DirectoryInfo(Paths.BinanceFuturesData.Down("1m", symbol)).GetFiles("*.csv").OrderByDescending(x => x.Name).First().Name;

	public static DateTime GetStartDate(string symbol) =>
		GetDate(GetStartDateFileName(symbol));

	public static DateTime GetEndDate(string symbol) =>
		GetDate(GetEndDateFileName(symbol));
	#endregion

	#region Chart
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
