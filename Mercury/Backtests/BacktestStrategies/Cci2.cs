using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci2
	/// CCI의 BB크로스
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class Cci2(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 32;
		public decimal Deviation = 2.8m;


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

			if (longPosition.Stage == 0 && c1.Cci > c1.Bb1Upper)
			{
				TakeProfitHalf(longPosition, c0.Quote.Open);
				return;
			}
			else if (longPosition.Stage == 1 && c1.Cci < c1.Bb1Upper)
			{
				TakeProfitHalf2(longPosition, c0);
				return;
			}

			//if (c1.Quote.Low <= longPosition.StopLossPrice)
			//{
			//	ExitPosition(longPosition, c0, longPosition.StopLossPrice);
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
				var entry = c0.Quote.Open;
				EntryPosition(PositionSide.Short, c0, entry);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (shortPosition.Stage == 0 && c1.Cci < c1.Bb1Lower)
			{
				TakeProfitHalf(shortPosition, c0.Quote.Open);
				return;
			}
			else if (shortPosition.Stage == 1 && c1.Cci > c1.Bb1Lower)
			{
				TakeProfitHalf2(shortPosition, c0);
				return;
			}
		}
	}
}
