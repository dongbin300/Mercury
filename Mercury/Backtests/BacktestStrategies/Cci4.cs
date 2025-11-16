using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci4 - CCI Momentum Spring Strategy
	/// 
	/// CCI 극값(-200/+200)에서 3캔들 반전 패턴으로 "스프링 효과" 포착
	/// 진입: CCI 극값 터치 후 연속 2캔들 반전 시
	/// 청산: CCI 제로라인 복귀 또는 반대 극값 도달
	/// 
	/// </summary>
	public class Cci4(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 20;
		public decimal ExtremeLevelHigh = 200m;
		public decimal ExtremeLevelLow = -200m;
		public decimal ZeroLevel = 0m;

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
				c1.Cci > c2.Cci && 
				c0.Cci > c1.Cci)
			{
				var entry = c0.Quote.Open;
				EntryPosition(PositionSide.Long, c0, entry);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (c1.Cci >= ZeroLevel || c1.Cci >= ExtremeLevelHigh)
			{
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
				c1.Cci < c2.Cci && 
				c0.Cci < c1.Cci)
			{
				var entry = c0.Quote.Open;
				EntryPosition(PositionSide.Short, c0, entry);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (c1.Cci <= ZeroLevel || c1.Cci <= ExtremeLevelLow)
			{
				ExitPosition(shortPosition, c0, c0.Quote.Open);
			}
		}
	}
}