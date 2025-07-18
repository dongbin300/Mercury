using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Engulf1
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class Engulf1(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public decimal sltprate = 2.0m;

		protected override void InitIndicator(ChartPack chartPack, params decimal[] p)
		{
			chartPack.UseAtr();
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			if (c3.CandlestickType == CandlestickType.Bullish &&
				c2.CandlestickType == CandlestickType.Bearish &&
				c1.CandlestickType == CandlestickType.Bullish &&
				c3.Quote.Close - c3.Quote.Open > c1.Atr * 2.0m &&
				//c2.Quote.Close > (c3.Quote.Open + c3.Quote.Close) / 2 &&
				c1.Quote.Close > c2.Quote.Open
				)
			{
				var entryPrice = c0.Quote.Open;
				var stopLossPrice = entryPrice - c1.Atr * 1.0m;
				var takeProfitPrice = entryPrice + (entryPrice - stopLossPrice) * sltprate;

				EntryPosition(PositionSide.Long, c0, entryPrice, stopLossPrice, takeProfitPrice);
				//EntryPositionOnlySize(PositionSide.Long, c0, entryPrice, Seed, stopLossPrice, takeProfitPrice);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c1.Quote.Low <= longPosition.StopLossPrice)
			{
				ExitPosition(longPosition, c0, longPosition.StopLossPrice);
				return;
			}

			if (c1.Quote.High >= longPosition.TakeProfitPrice)
			{
				ExitPosition(longPosition, c0, longPosition.TakeProfitPrice);
				return;
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
		}
	}
}
