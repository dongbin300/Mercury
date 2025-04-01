using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class TrendRiderTest(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		protected override void InitIndicator(ChartPack chartPack, params decimal[] p)
		{
			chartPack.UseTrendRider((int)p[0], (double)p[1], (int)p[2], (int)p[3], (int)p[4], (int)p[5]);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (c1.TrendRiderTrend == 1)
			{
				EntryPosition(PositionSide.Long, c0, c0.Quote.Open);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (c1.TrendRiderTrend != 1)
			{
				ExitPosition(longPosition, c1, c1.Quote.Close);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (c1.TrendRiderTrend == -1)
			{
				EntryPosition(PositionSide.Short, c0, c0.Quote.Open);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (c1.TrendRiderTrend != -1)
			{
				ExitPosition(shortPosition, c1, c1.Quote.Close);
			}
		}
	}
}
