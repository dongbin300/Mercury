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
		public int Ema1Period { get; set; } = 12;
		public int Ema2Period { get; set; } = 26;
		public int Ema3Period { get; set; } = 200;


		protected override void InitIndicator(ChartPack chartPack, params decimal[] p)
		{
			chartPack.UseCci(20);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c2.Cci < -100 && c1.Cci > -100)
			{
				var entry = c0.Quote.Open;
				EntryPosition(PositionSide.Long, c0, entry);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (c1.Cci > 200)
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
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
		}
	}
}
