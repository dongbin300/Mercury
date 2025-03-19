using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// DDUD - UU
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class Candle7(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		protected override void InitIndicator(ChartPack chartPack, params decimal[] p)
		{
			chartPack.UseRsi();
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];
			var c4 = charts[i - 4];

			if (
				c1.CandlestickType == CandlestickType.Bearish
				&& c2.CandlestickType == CandlestickType.Bullish
				&& c3.CandlestickType == CandlestickType.Bearish
				&& c4.CandlestickType == CandlestickType.Bearish
				&& c1.Rsi1 < 30
				)
			{
				EntryPosition(PositionSide.Long, c0, c1.Quote.Close);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			if (
				c1.CandlestickType == CandlestickType.Bullish
				&& c2.CandlestickType == CandlestickType.Bullish
				)
			{
				ExitPosition(longPosition, c1, c1.Quote.Close);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];
			var c4 = charts[i - 4];

			if (
				c1.CandlestickType == CandlestickType.Bullish
				&& c2.CandlestickType == CandlestickType.Bearish
				&& c3.CandlestickType == CandlestickType.Bullish
				&& c4.CandlestickType == CandlestickType.Bullish
				&& c1.Rsi1 > 70
				)
			{
				EntryPosition(PositionSide.Short, c0, c1.Quote.Close);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			if (
				c1.CandlestickType == CandlestickType.Bearish
				&& c2.CandlestickType == CandlestickType.Bearish
				)
			{
				ExitPosition(shortPosition, c1, c1.Quote.Close);
			}
		}
	}
}
