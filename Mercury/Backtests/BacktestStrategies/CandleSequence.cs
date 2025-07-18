using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// CandleSequence
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class CandleSequence(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public string entryCondition = string.Empty;
		public string exitCondition = string.Empty;
		public string entryCondition2 = string.Empty;
		public string exitCondition2 = string.Empty;
		public decimal minBody = 0m;

		protected override void InitIndicator(ChartPack chartPack, params decimal[] p)
		{
			entryCondition2 = ReverseCondition(entryCondition);
			exitCondition2 = ReverseCondition(exitCondition);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];

			if (IsTrueCandle(charts, i, entryCondition, minBody))
			{
				EntryPosition(PositionSide.Long, c0, c0.Quote.Open);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];

			if (IsTrueCandle(charts, i, exitCondition, minBody))
			{
				ExitPosition(longPosition, c0, c0.Quote.Open);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];

			if (IsTrueCandle(charts, i, entryCondition2, minBody))
			{
				EntryPosition(PositionSide.Short, c0, c0.Quote.Open);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];

			if (IsTrueCandle(charts, i, exitCondition2, minBody))
			{
				ExitPosition(shortPosition, c0, c0.Quote.Open);
			}
		}

		public static string ReverseCondition(string input)
		{
			return new string([.. input.Select(c => c == 'U' ? 'D' : c == 'D' ? 'U' : c)]);
		}

	}
}
