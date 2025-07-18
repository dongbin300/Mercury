using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// LSMA
	/// RSI 40라인을 골든 크로스 이후, 3봉 이내에 LSMA 10이 30을 골든 크로스하면 매수
	/// LSMA 교차점 SL
	/// SLTP 비율 1:2
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class Lsma1(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public decimal sltprate = 2.0m;
		public decimal th = 4m;
		public decimal rsith = 40;

		protected override void InitIndicator(ChartPack chartPack, params decimal[] p)
		{
			chartPack.UseLsma(10, 30);
			chartPack.UseRsi(14);
			chartPack.UseMaAngles();
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];
			var c4 = charts[i - 4];

			if (c2.Lsma1 < c2.Lsma2 && c1.Lsma1 > c1.Lsma2 &&
				((c2.Rsi1 < rsith && c1.Rsi1 > rsith) || (c3.Rsi1 < rsith && c2.Rsi1 > rsith) || (c4.Rsi1 < rsith && c3.Rsi1 > rsith)))
			{
				var crossPrice = GetCrossPrice(c2.Lsma1.Value, c2.Lsma2.Value, c1.Lsma1.Value, c1.Lsma2.Value);
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
