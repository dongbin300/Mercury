using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci10 - Adaptive Trailing Exit Revolution
	/// 
	/// 100점 돌파를 위한 혁명적 접근
	/// 진입: 검증된 CCI 극값 반전 (Cci9)
	/// 청산: 적응형 트레일링 - CCI 최고/최저점 추적 후 역추적 청산
	/// 
	/// </summary>
	public class Cci10(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 15;
		public decimal ExtremeLevelHigh = 150m;
		public decimal ExtremeLevelLow = -150m;
		public decimal TrailingPercent = 30m;

		private Dictionary<string, decimal> maxCciInPosition = [];
		private Dictionary<string, decimal> minCciInPosition = [];

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
				maxCciInPosition[symbol] = c1.Cci.Value;
				var entry = c0.Quote.Open;
				EntryPosition(PositionSide.Long, c0, entry);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c1 = charts[i - 1];
			
			if (!maxCciInPosition.ContainsKey(symbol))
				maxCciInPosition[symbol] = c1.Cci.Value;

			maxCciInPosition[symbol] = Math.Max(maxCciInPosition[symbol], c1.Cci.Value);

			var trailingThreshold = maxCciInPosition[symbol] * (100 - TrailingPercent) / 100;

			if (c1.Cci <= trailingThreshold)
			{
				var c0 = charts[i];
				ExitPosition(longPosition, c0, c0.Quote.Open);
				maxCciInPosition.Remove(symbol);
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
				minCciInPosition[symbol] = c1.Cci.Value;
				var entry = c0.Quote.Open;
				EntryPosition(PositionSide.Short, c0, entry);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c1 = charts[i - 1];
			
			if (!minCciInPosition.ContainsKey(symbol))
				minCciInPosition[symbol] = c1.Cci.Value;

			minCciInPosition[symbol] = Math.Min(minCciInPosition[symbol], c1.Cci.Value);

			var trailingThreshold = minCciInPosition[symbol] * (100 + TrailingPercent) / 100;

			if (c1.Cci >= trailingThreshold)
			{
				var c0 = charts[i];
				ExitPosition(shortPosition, c0, c0.Quote.Open);
				minCciInPosition.Remove(symbol);
			}
		}
	}
}