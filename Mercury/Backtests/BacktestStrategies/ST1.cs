using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

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
		protected override void InitIndicator(ChartPack chartPack, params decimal[] p)
		{
			chartPack.UseTripleSupertrend(10, 1.5, 20, 3, 60, 6);
			chartPack.UseEma(5, 20);
			chartPack.UseRsi(14);
			chartPack.UseAtrma(14);
			chartPack.UseVolumeSma();
			chartPack.UseMacd();
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c1.Supertrend1 > 0 && c1.Supertrend2 > 0 && c2.Quote.Close <= (decimal)c2.Ema2 && c1.Quote.Close > (decimal)c1.Ema2)
			{
				var entryPrice = c0.Quote.Open;
				var stopLoss = (decimal)c1.Ema2 * 0.995m; // 20EMA 0.5% 아래
				var takeProfit = GetMaxPrice(charts, 30, i) * 0.99m; // 직전 고점 1% 아래

				EntryPosition(PositionSide.Long, c0, entryPrice, stopLoss, takeProfit);
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
