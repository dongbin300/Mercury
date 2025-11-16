using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci12 - Multi-Timeframe Precision Strategy
	/// 
	/// 100점 돌파를 위한 멀티 타임프레임 확인 시스템
	/// 진입: 빠른CCI + 느린CCI 동시 극값 확인
	/// 청산: 제로라인 청산 (검증된 단순함)
	/// 
	/// </summary>
	public class Cci12(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int FastCciPeriod = 15;
		public int SlowCciPeriod = 30;
		public decimal ExtremeLevelHigh = 150m;
		public decimal ExtremeLevelLow = -150m;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(FastCciPeriod, SlowCciPeriod);
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
				c1.Cci > c2.Cci &&
				c1.Cci2 <= ExtremeLevelLow)
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
			if (i < 3) return;
			
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			if (c3.Cci >= ExtremeLevelHigh && 
				c2.Cci < c3.Cci && 
				c1.Cci < c2.Cci &&
				c1.Cci2 >= ExtremeLevelHigh)
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