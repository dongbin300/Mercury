using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci14 - CCI Micro-Optimization Strategy
	/// 
	/// Cci9 베이스 + 승률 향상을 위한 미세 개선
	/// 진입: CCI 극값 반전 + 가격 모멘텀 확인
	/// 청산: 적응형 청산 (CCI 강도별)
	/// 
	/// </summary>
	public class Cci14(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 15;
		public decimal ExtremeLevelHigh = 150m;
		public decimal ExtremeLevelLow = -150m;
		public decimal PriceMomentumThreshold = 1.5m;
		public decimal StrongCciThreshold = 100m;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(CciPeriod);
		}

		private bool HasPositivePriceMomentum(List<ChartInfo> charts, int index)
		{
			if (index < 3) return true;
			
			var c1 = charts[index - 1];
			var c2 = charts[index - 2];
			var c3 = charts[index - 3];
			
			var priceChange1 = (c1.Quote.Close - c2.Quote.Close) / c2.Quote.Close * 100;
			var priceChange2 = (c2.Quote.Close - c3.Quote.Close) / c3.Quote.Close * 100;
			
			return priceChange1 > PriceMomentumThreshold || priceChange2 > PriceMomentumThreshold;
		}

		private bool HasNegativePriceMomentum(List<ChartInfo> charts, int index)
		{
			if (index < 3) return true;
			
			var c1 = charts[index - 1];
			var c2 = charts[index - 2];
			var c3 = charts[index - 3];
			
			var priceChange1 = (c1.Quote.Close - c2.Quote.Close) / c2.Quote.Close * 100;
			var priceChange2 = (c2.Quote.Close - c3.Quote.Close) / c3.Quote.Close * 100;
			
			return priceChange1 < -PriceMomentumThreshold || priceChange2 < -PriceMomentumThreshold;
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
				HasPositivePriceMomentum(charts, i))
			{
				var entry = c0.Quote.Open;
				EntryPosition(PositionSide.Long, c0, entry);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c1 = charts[i - 1];

			if (Math.Abs(c1.Cci.Value) > StrongCciThreshold)
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
				c1.Cci < c2.Cci &&
				HasNegativePriceMomentum(charts, i))
			{
				var entry = c0.Quote.Open;
				EntryPosition(PositionSide.Short, c0, entry);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c1 = charts[i - 1];

			if (Math.Abs(c1.Cci.Value) > StrongCciThreshold)
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