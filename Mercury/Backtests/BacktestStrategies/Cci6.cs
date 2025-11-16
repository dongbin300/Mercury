using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci6 - CCI Momentum Acceleration Strategy
	/// 
	/// CCI 극값 반전 후 모멘텀 가속도 패턴으로 100점 도전
	/// 진입: CCI 극값 + 연속 가속도 증가 패턴
	/// 청산: 동적 조건 (트렌드 강도별 적응)
	/// 
	/// </summary>
	public class Cci6(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 18;
		public decimal ExtremeLevelHigh = 200m;
		public decimal ExtremeLevelLow = -150m;
		public decimal StrongMomentumThreshold = 50m;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(CciPeriod);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 3) return;
			
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			if (c3.Cci <= ExtremeLevelLow && 
				c2.Cci > c3.Cci && 
				c1.Cci > c2.Cci)
			{
				var momentum1 = c2.Cci - c3.Cci;
				var momentum2 = c1.Cci - c2.Cci;

				bool hasAcceleration = momentum2 > momentum1 && momentum2 > 10m;

				if (hasAcceleration)
				{
					var entry = c0.Quote.Open;
					EntryPosition(PositionSide.Long, c0, entry);
				}
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			var momentum = c1.Cci - c2.Cci;
			bool isStrongTrend = Math.Abs(momentum.Value) > StrongMomentumThreshold;

			if (isStrongTrend)
			{
				if (c1.Cci >= ExtremeLevelHigh)
				{
					var c0 = charts[i];
					ExitPosition(longPosition, c0, c0.Quote.Open);
				}
			}
			else
			{
				if (c1.Cci >= 0)
				{
					var c0 = charts[i];
					ExitPosition(longPosition, c0, c0.Quote.Open);
				}
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 3) return;
			
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			if (c3.Cci >= ExtremeLevelHigh && 
				c2.Cci < c3.Cci && 
				c1.Cci < c2.Cci)
			{
				var momentum1 = c3.Cci - c2.Cci;
				var momentum2 = c2.Cci - c1.Cci;

				bool hasAcceleration = momentum2 > momentum1 && momentum2 > 10m;

				if (hasAcceleration)
				{
					var entry = c0.Quote.Open;
					EntryPosition(PositionSide.Short, c0, entry);
				}
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			var momentum = c2.Cci - c1.Cci;
			bool isStrongTrend = Math.Abs(momentum.Value) > StrongMomentumThreshold;

			if (isStrongTrend)
			{
				if (c1.Cci <= ExtremeLevelLow)
				{
					var c0 = charts[i];
					ExitPosition(shortPosition, c0, c0.Quote.Open);
				}
			}
			else
			{
				if (c1.Cci <= 0)
				{
					var c0 = charts[i];
					ExitPosition(shortPosition, c0, c0.Quote.Open);
				}
			}
		}
	}
}