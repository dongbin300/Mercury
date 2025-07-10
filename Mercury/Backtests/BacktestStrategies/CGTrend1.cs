using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;
using Mercury.Extensions;
using Mercury.Maths;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// CG Trend
	/// 
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class CGTrend1(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		private SmartRandom r = new();

		public decimal RsiL = 40;
		public decimal RsiH = 60;
		public decimal RsiL2 = 60;
		public decimal RsiH2 = 40;

		public decimal RRange = 5;

		/// <summary>
		/// Interval이 다른 차트팩
		/// </summary>
		public Dictionary<string, List<ChartInfo>> Charts2 { get; set; } = [];

		protected override void InitIndicator(ChartPack chartPack, params decimal[] p)
		{
			chartPack.UseRsi(14);
		}

		/// <summary>
		/// Interval이 다른 차트팩 계산
		/// </summary>
		/// <param name="chartPacks"></param>
		public void InitIndicator2(List<ChartPack> chartPacks)
		{
			foreach (var chartPack in chartPacks)
			{
				chartPack.UseTrendRider();
				Charts2.Add(chartPack.Symbol, [.. chartPack.Charts]);
			}
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var trend = Charts2["BTCUSDT"].GetLatestChartBefore(c1.DateTime.AddDays(-1)).TrendRiderTrend;

			// 지정가 진입시 진입하는 캔들의 저가가 시가보다 0.03%이상 낮다면 진입 성공
			// 진입 실패하면 다음 캔들로 넘어가서 진입조건 확인 후 지정가 진입 시도
			if (trend > 0 && c1.Rsi1 < r.NextDecimal(RsiL - RRange, RsiL + RRange))
			{
				var entryPrice = GetAdjustedPrice(PositionSide.Long, c0, true);
				if (IsOrderFilled(PositionSide.Long, c0, entryPrice, true))
				{
					EntryPosition(PositionSide.Long, c0, entryPrice);
				}
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var trend = Charts2["BTCUSDT"].GetLatestChartBefore(c1.DateTime.AddDays(-1)).TrendRiderTrend;

			if (trend < 0 && c1.Rsi1 > r.NextDecimal(RsiH - RRange, RsiH + RRange))
			{
				var entryPrice = GetAdjustedPrice(PositionSide.Short, c0, true);
				if (IsOrderFilled(PositionSide.Short, c0, entryPrice, true))
				{
					EntryPosition(PositionSide.Short, c0, entryPrice);
				}
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			// 지정가 청산시 청산하는 캔들의 고가가 시가보다 0.03%이상 높다면 청산 성공
			// 청산 실패하면 다음 캔들로 넘어가서 청산조건 확인 후 지정가 청산 시도
			if (c1.Rsi1 > r.NextDecimal(RsiL2 - RRange, RsiL2 + RRange))
			{
				var exitPrice = GetAdjustedPrice(PositionSide.Long, c0, false);
				if (IsOrderFilled(PositionSide.Long, c0, exitPrice, false))
				{
					ExitPosition(longPosition, c0, exitPrice);
				}
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			if (c1.Rsi1 < r.NextDecimal(RsiH2 - RRange, RsiH2 + RRange))
			{
				var exitPrice = GetAdjustedPrice(PositionSide.Short, c0, false);
				if (IsOrderFilled(PositionSide.Short, c0, exitPrice, false))
				{
					ExitPosition(shortPosition, c0, exitPrice);
				}
			}
		}

	}
}
