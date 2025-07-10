using Binance.Net.Objects.Models.Spot;

namespace Mercury.Charts
{
	public class TradePack(string symbol)
	{
		public string Symbol { get; set; } = symbol;
		public IList<BinanceAggregatedTrade> Trades { get; set; } = [];
		public int CurrentIndex { get; set; } = 0;
		public BinanceAggregatedTrade CurrentTrade => Trades[CurrentIndex];

		public void AddTrade(BinanceAggregatedTrade trade)
		{
			Trades.Add(trade);
		}

		public BinanceAggregatedTrade Select()
		{
			CurrentIndex = 0;
			return CurrentTrade;
		}

		public BinanceAggregatedTrade Select(int year, int month, int day)
		{
			var trade = Trades.First(x => x.TradeTime.Year == year && x.TradeTime.Month == month && x.TradeTime.Day == day) ?? throw new Exception("No Aggregated Trade");
			CurrentIndex = Trades.IndexOf(trade);
			return CurrentTrade;
		}

		public BinanceAggregatedTrade Next()
		{
			CurrentIndex++;
			return CurrentTrade;
		}
	}
}
