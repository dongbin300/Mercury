using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci1
	/// 
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class Cci1(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int EntryCci = -150;
		public int ExitCci = 200;


		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(20);
			chartPack.UseEma(8);
			//chartPack.UseBollingerBands();
			//chartPack.UseVolumeSma();
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			//var chart1h = GetCharts(KlineInterval.OneHour, symbol).GetChartsBefore(c0.DateTime, 5);
			//var chart15m = GetCharts(KlineInterval.FifteenMinutes, symbol).GetChartsBefore(c0.DateTime, 17);

			//bool IsCciBreakout(ChartInfo prev, ChartInfo current) => prev.Cci < -100 && current.Cci > -100;
			//bool IsConfirmed(List<ChartInfo> list, int startOffset, int count)
			//{
			//	for (int j = startOffset + count - 1; j > startOffset; j--)
			//	{
			//		if (IsCciBreakout(list[j - 1], list[j]))
			//			return true;
			//	}
			//	return false;
			//}

			bool isMainChartConfirmed = c2.Cci < EntryCci && c1.Cci > EntryCci;
			//bool is1hConfirmed = IsConfirmed(chart1h, 2, 3);
			//bool is15mConfirmed = IsConfirmed(chart15m, 2, 15);

			if (isMainChartConfirmed)
			{
				var entry = c0.Quote.Open;
				//var stopLoss = c1.Bb1Lower;
				//var takeProfit = c1.Bb1Upper;

				//if (stopLoss > entry)
				//	return;

				EntryPosition(PositionSide.Long, c0, entry);
				//EntryPositionOnlySize(PositionSide.Long, c0, entry, Seed / 2);
			}
		}


		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c2.Cci > ExitCci && c1.Cci < ExitCci)
			{
				ExitPosition(longPosition, c0, c0.Quote.Open);
				return;
			}

			//if (longPosition.Stage == 0 && c1.Quote.Low <= longPosition.StopLossPrice)
			//{
			//	StopLoss(longPosition, c1);
			//}
			//else if (longPosition.Stage == 0 && c1.Quote.High >= longPosition.TakeProfitPrice)
			//{
			//	TakeProfitHalf(longPosition);
			//}
			//if (longPosition.Stage == 1 && c1.Quote.Close < c1.Ema2)
			//{
			//	TakeProfitHalf2(longPosition, c0);
			//}

			//if (c1.Quote.Low <= longPosition.StopLossPrice)
			//{
			//	ExitPosition(longPosition, c0, longPosition.StopLossPrice);
			//	return;
			//}

			//if (c1.Quote.High >= longPosition.TakeProfitPrice)
			//{
			//	ExitPosition(longPosition, c0, longPosition.TakeProfitPrice);
			//	return;
			//}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			//var c0 = charts[i];
			//var c1 = charts[i - 1];
			//var c2 = charts[i - 2];

			//bool isMainChartConfirmed = c2.Cci > ExitCci && c1.Cci < ExitCci;

			//if (isMainChartConfirmed)
			//{
			//	var entry = c0.Quote.Open;
			//	EntryPosition(PositionSide.Short, c0, entry);
			//}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			//var c0 = charts[i];
			//var c1 = charts[i - 1];
			//var c2 = charts[i - 2];

			//if (c2.Cci < EntryCci && c1.Cci > EntryCci)
			//{
			//	ExitPosition(shortPosition, c0, c0.Quote.Open);
			//	return;
			//}
		}
	}
}
