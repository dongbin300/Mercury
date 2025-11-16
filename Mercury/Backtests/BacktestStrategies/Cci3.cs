using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci3
	/// 
	/// CCI BB 하단을 골크시 매수 진입
	/// 매수진입한 파동의 CCI 저점대칭 돌파 시 정리 (+CCI BB 상단 이탈 시 1/2 정리)
	/// 
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class Cci3(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 32;
		public decimal Deviation = 2.8m;

		private Dictionary<string, decimal> minCcis = [];
		private Dictionary<string, decimal> maxCcis = [];

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(CciPeriod);
			chartPack.UseBollingerBands(CciPeriod, (double)Deviation, Extensions.IndicatorType.Cci);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c2.Cci < c2.Bb1Lower && c1.Cci > c1.Bb1Lower)
			{
				var minCci = GetMinCci(charts, 14, i) ?? c2.Cci.Value;
				if (minCci > 0)
				{
					return;
				}
				minCcis[symbol] = minCci;

				var entry = c0.Quote.Open;
				//var stopLoss = entry - c1.Atr;

				EntryPosition(PositionSide.Long, c0, entry);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (longPosition.Stage == 0 && c1.Cci >= -minCcis[symbol])
			{
				TakeProfitHalf(longPosition, c0.Quote.Open);
				return;
			}
			else if (longPosition.Stage == 1 && c1.Cci < c1.Bb1Upper)
			{
				TakeProfitHalf2(longPosition, c0);
				return;
			}

			//if (c1.Cci >= -minCcis[symbol])
			//{
			//	ExitPosition(longPosition, c0, c0.Quote.Open);
			//	return;
			//}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c2.Cci > c2.Bb1Upper && c1.Cci < c1.Bb1Upper)
			{
				var maxCci = GetMaxCci(charts, 14, i) ?? c2.Cci.Value;
				if (maxCci < 0)
				{
					return;
				}
				maxCcis[symbol] = maxCci;

				var entry = c0.Quote.Open;
				EntryPosition(PositionSide.Short, c0, entry);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (shortPosition.Stage == 0 && c1.Cci <= -maxCcis[symbol])
			{
				TakeProfitHalf(shortPosition, c0.Quote.Open);
				return;
			}
			else if (shortPosition.Stage == 1 && c1.Cci > c1.Bb1Lower)
			{
				TakeProfitHalf2(shortPosition, c0);
				return;
			}

			//if (c1.Cci <= -maxCcis[symbol])
			//{
			//	ExitPosition(shortPosition, c0, c0.Quote.Open);
			//	return;
			//}
		}
	}
}
