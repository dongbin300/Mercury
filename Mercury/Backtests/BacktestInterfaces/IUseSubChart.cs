using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Extensions;

namespace Mercury.Backtests.BacktestInterfaces
{
	public interface IUseSubChart
	{
		public KlineInterval SubInterval { get; set; }
		public Dictionary<string, List<ChartInfo>> SubCharts { get; set; }

		public IEnumerable<ChartInfo> GetSubChart(string symbol, DateTime startTime, KlineInterval mainChartInterval)
		{
			var endTime = startTime + mainChartInterval.ToTimeSpan() - TimeSpan.FromSeconds(1);
			return SubCharts[symbol].Where(d => d.DateTime >= startTime && d.DateTime <= endTime);
		}
	}
}
