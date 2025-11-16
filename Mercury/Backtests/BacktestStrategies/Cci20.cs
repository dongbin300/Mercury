using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci20 - Ultimate Simplicity Return
	/// 
	/// 모든 실험 후 최종 결론: 극도의 단순화
	/// Cci9보다도 더 단순하게
	/// 진입: 2캔들 반전만 확인
	/// 청산: 즉시 제로라인
	/// 
	/// </summary>
	public class Cci20(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 15;
		public decimal ExtremeLevelHigh = 150m;
		public decimal ExtremeLevelLow = -150m;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(CciPeriod);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;
			
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c2.Cci <= ExtremeLevelLow && 
				c1.Cci > c2.Cci)
			{
				var entry = c0.Quote.Open;
				EntryPosition(PositionSide.Long, c0, entry);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c1 = charts[i - 1];

			if (c1.Cci >= 0)
			{
				var c0 = charts[i];
				ExitPosition(longPosition, c0, c0.Quote.Open);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;
			
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c2.Cci >= ExtremeLevelHigh && 
				c1.Cci < c2.Cci)
			{
				var entry = c0.Quote.Open;
				EntryPosition(PositionSide.Short, c0, entry);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c1 = charts[i - 1];

			if (c1.Cci <= 0)
			{
				var c0 = charts[i];
				ExitPosition(shortPosition, c0, c0.Quote.Open);
			}
		}
	}
}