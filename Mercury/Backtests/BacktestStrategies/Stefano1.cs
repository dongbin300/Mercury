using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;
using Mercury.Maths;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Stefano
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class Stefano1(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public decimal sltprate = 2.0m;
		public decimal th = 4m;

		protected override void InitIndicator(ChartPack chartPack, params decimal[] p)
		{
			chartPack.UseEma(12, 26);
			chartPack.UseMaAngles();
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c2.Ema1 < c2.Ema2 && c1.Ema1 > c1.Ema2 && c1.JmaSlope >= th)
			{
				var crossPrice = GetCrossPrice(c2.Ema1.Value, c2.Ema2.Value, c1.Ema1.Value, c1.Ema2.Value);
				var entryPrice = c0.Quote.Open;
				var stopLossPrice = crossPrice;
				var takeProfitPrice = entryPrice + (entryPrice - stopLossPrice) * sltprate;

				EntryPosition(PositionSide.Long, c0, entryPrice, stopLossPrice, takeProfitPrice);
				//EntryPositionOnlySize(PositionSide.Long, c0, entryPrice, Seed, stopLossPrice, takeProfitPrice);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			//if (longPosition.Stage == 0 && c1.Quote.Low <= longPosition.StopLossPrice)
			//{
			//	StopLoss(longPosition, c1);
			//}
			//else if (longPosition.Stage == 0 && c1.Quote.High >= longPosition.TakeProfitPrice)
			//{
			//	TakeProfitHalf(longPosition);
			//}
			//if (longPosition.Stage == 1 && c1.Supertrend1 < 0)
			//{
			//	TakeProfitHalf2(longPosition, c0);
			//}


			//if (longPosition.Stage == 0 && c1.Quote.Low <= longPosition.StopLossPrice)
			//{
			//	StopLoss(longPosition, c1);
			//}
			//else if (longPosition.Stage == 0 && c1.Quote.High >= longPosition.TakeProfitPrice)
			//{
			//	TakeProfitHalf(longPosition);
			//}
			//if (longPosition.Stage == 1 && c1.Supertrend1 < 0)
			//{
			//	TakeProfitHalf2(longPosition, c0);
			//}


			if (c1.Quote.Low <= longPosition.StopLossPrice)
			{
				ExitPosition(longPosition, c0, longPosition.StopLossPrice);
				return;
			}

			if (c1.Quote.High >= longPosition.TakeProfitPrice)
			{
				ExitPosition(longPosition, c0, longPosition.TakeProfitPrice);
				return;
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
		}
	}
}
