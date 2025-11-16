using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci8 - Volatility Expansion Filter Strategy
	/// 
	/// CCI 극값 반전 + ATR 기반 변동성 확장 필터
	/// 1000+ 거래 확보하면서 고품질 신호 포착
	/// 진입: CCI 극값 반전 + ATR 상승 추세
	/// 청산: 제로라인 복귀
	/// 
	/// </summary>
	public class Cci8(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 16;
		public decimal ExtremeLevelHigh = 150m;
		public decimal ExtremeLevelLow = -150m;
		public int AtrPeriod = 14;
		public decimal AtrGrowthThreshold = 1.1m;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(CciPeriod);
			chartPack.UseAtr(AtrPeriod);
		}

		private bool HasVolatilityExpansion(List<ChartInfo> charts, int index)
		{
			if (index < 2) return true;

			var currentAtr = charts[index - 1].Atr;
			var previousAtr = charts[index - 2].Atr;

			if (currentAtr == null || previousAtr == null) return true;

			return currentAtr > previousAtr * AtrGrowthThreshold;
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;
			
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c2.Cci <= ExtremeLevelLow && 
				c1.Cci > c2.Cci && 
				c1.Cci > ExtremeLevelLow)
			{
				if (HasVolatilityExpansion(charts, i))
				{
					var entry = c0.Quote.Open;
					EntryPosition(PositionSide.Long, c0, entry);
				}
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
				c1.Cci < c2.Cci && 
				c1.Cci < ExtremeLevelHigh)
			{
				if (HasVolatilityExpansion(charts, i))
				{
					var entry = c0.Quote.Open;
					EntryPosition(PositionSide.Short, c0, entry);
				}
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