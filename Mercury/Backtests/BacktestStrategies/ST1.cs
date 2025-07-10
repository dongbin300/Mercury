using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;
using Mercury.Maths;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Supertrend with Ema
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class ST1(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int adxth = 30;

		protected override void InitIndicator(ChartPack chartPack, params decimal[] p)
		{
			chartPack.UseMacd(12, 26, 9, 9, 20, 7);
			chartPack.UseAdx();
			chartPack.UseSupertrend(10, 1.5);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			var minPrice = GetMinPrice(charts, 14, i);
			var maxPrice = GetMaxPrice(charts, 14, i);

			var slPrice = minPrice - (maxPrice - minPrice) * 0.1m;
			var tpPrice = maxPrice - (maxPrice - minPrice) * 0.1m;
			var slPer = Calculator.Roe(PositionSide.Long, c0.Quote.Open, slPrice);
			var tpPer = Calculator.Roe(PositionSide.Long, c0.Quote.Open, tpPrice);
			if (IsPowerGoldenCross(charts, 14, i, adxth, c1.Macd) && IsPowerGoldenCross2(charts, 14, i, adxth, c1.Macd2) && tpPer > 1.0m)
			{
				EntryPosition(PositionSide.Long, c0, c0.Quote.Open, slPrice, tpPrice);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (longPosition.Stage == 0 && c1.Quote.Low <= longPosition.StopLossPrice)
			{
				StopLoss(longPosition, c1);
			}
			else if (longPosition.Stage == 0 && c1.Quote.High >= longPosition.TakeProfitPrice)
			{
				TakeProfitHalf(longPosition);
			}
			if (longPosition.Stage == 1 && c1.Supertrend1 < 0)
			{
				TakeProfitHalf2(longPosition, c0);
			}


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


			//if (c1.Quote.Low <= longPosition.StopLossPrice)
			//{
			//	ExitPosition(longPosition, c0, longPosition.StopLossPrice);
			//	return;
			//}

			//if (c2.Supertrend1 > 0 && c1.Supertrend1 < 0)
			//{
			//	ExitPosition(longPosition, c0, c0.Quote.Open);
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
