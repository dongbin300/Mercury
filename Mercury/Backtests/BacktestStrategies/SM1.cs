using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;
using Mercury.Extensions;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// SqueezeMomentum
	/// 
	/// signal 값이 1에서 2로 바뀌는순간 direction이 +면 롱포지션 진입 -면 숏포지션 진입
	/// 진입후에 direction 값이 바뀌는 순간 포지션종료
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class SM1(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		/// <summary>
		/// Interval이 다른 차트팩
		/// </summary>
		public Dictionary<string, List<ChartInfo>> Charts2 { get; set; } = [];

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseSqueezeMomentum((int)p[0], (double)p[1], (int)p[2], (double)p[3], true);
			chartPack.UseAdx();
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
			var c2 = charts[i - 2];
			var d1 = Charts2[symbol].GetLatestChartBefore(c1.DateTime);

			if (c2.SmSignal == 1 && c1.SmSignal == 2 && c1.SmDirection > 0 && d1.TrendRiderTrend == 1 && c1.Adx > 25)
			{
				EntryPosition(PositionSide.Long, c0, c0.Quote.Open);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c2.SmDirection != c1.SmDirection)
			{
				ExitPosition(longPosition, c1, c1.Quote.Close);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var d1 = Charts2[symbol].GetLatestChartBefore(c1.DateTime);

			if (c2.SmSignal == 1 && c1.SmSignal == 2 && c1.SmDirection < 0 && d1.TrendRiderTrend == -1 && c1.Adx > 25)
			{
				EntryPosition(PositionSide.Short, c0, c0.Quote.Open);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c2.SmDirection != c1.SmDirection)
			{
				ExitPosition(shortPosition, c1, c1.Quote.Close);
			}
		}
	}
}
