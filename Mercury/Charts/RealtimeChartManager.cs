using Binance.Net.Enums;

using Mercury.Apis;

namespace Mercury.Charts
{
	public class RealtimeChartManager
	{
		public static List<RealtimeChart> RealtimeCharts { get; set; } = new();

		public static void Init()
		{
			foreach (var symbol in LocalApi.SymbolNames)
			//foreach (var symbol in new List<string> { "BTCUSDT", "ETHUSDT", "SOLUSDT" })
			{
				var quotes = BinanceRestApi.GetQuotes(symbol, KlineInterval.FiveMinutes, null, null, 15);
				foreach (var quote in quotes)
				{
					var _realtimeChart = RealtimeCharts.Find(c => c.Symbol.Equals(symbol));
					if (_realtimeChart == null)
					{
						RealtimeCharts.Add(new RealtimeChart(symbol, new List<Quote> { quote }));
					}
					else
					{
						_realtimeChart.UpdateQuote(quote);
					}
				}
				BinanceSocketApi.GetKlineUpdatesAsync2(symbol, KlineInterval.FiveMinutes);
			}
		}

		public static void UpdateRealtimeChart(string symbol, Quote quote)
		{
			//quote.Date = new System.DateTime(quote.Date.Year, quote.Date.Month, quote.Date.Day, quote.Date.Hour, quote.Date.Minute / 5 * 5, quote.Date.Second);
			var _realtimeChart = RealtimeCharts.Find(c => c.Symbol.Equals(symbol));
			if (_realtimeChart == null)
			{
				RealtimeCharts.Add(new RealtimeChart(symbol, new List<Quote> { quote }));
			}
			else
			{
				_realtimeChart.UpdateQuote(quote);
				_realtimeChart.CalculateIndicators();
			}
		}
	}
}
