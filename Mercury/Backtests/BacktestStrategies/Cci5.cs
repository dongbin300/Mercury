using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci5 - CCI Divergence Spring Strategy
	/// 
	/// CCI 극값 + 가격-CCI 미니다이버전스로 진입 정확도 극대화
	/// 진입: CCI 극값 터치 후 3캔들 다이버전스 패턴 확인
	/// 청산: CCI 제로라인 복귀 시 (단순화)
	/// 
	/// </summary>
	public class Cci5(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 16;
		public decimal ExtremeLevelHigh = 150m;
		public decimal ExtremeLevelLow = -150m;
		public decimal ZeroLevel = 0m;

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
				bool hasBullishDivergence = 
					c1.Quote.Low > c3.Quote.Low && 
					c1.Cci > c3.Cci;

				if (hasBullishDivergence)
				{
					var entry = c0.Quote.Open;
					EntryPosition(PositionSide.Long, c0, entry);
				}
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c1 = charts[i - 1];

			if (c1.Cci >= ZeroLevel)
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
				c1.Cci < c2.Cci)
			{
				bool hasBearishDivergence = 
					c1.Quote.High < c3.Quote.High && 
					c1.Cci < c3.Cci;

				if (hasBearishDivergence)
				{
					var entry = c0.Quote.Open;
					EntryPosition(PositionSide.Short, c0, entry);
				}
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c1 = charts[i - 1];

			if (c1.Cci <= ZeroLevel)
			{
				var c0 = charts[i];
				ExitPosition(shortPosition, c0, c0.Quote.Open);
			}
		}
	}
}