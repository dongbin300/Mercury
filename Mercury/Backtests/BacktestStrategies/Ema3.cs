using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

using System.Security.Cryptography;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Ema3
	/// 
	/// 12, 26 골크
	/// sl atrv * 2
	/// tp atrv * 4
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class Ema3(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int Ema1Period = 12;
		public int Ema2Period = 26;
		public int Ema3Period = 200;


		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseEma(Ema1Period, Ema2Period, Ema3Period);
			chartPack.UseMaAngles();
			chartPack.UseAtr(14);
			chartPack.UseSupertrend(10, 2.5);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c2.Ema1 < c2.Ema2 && c1.Ema1 > c1.Ema2 && c1.Quote.Close > c1.Ema3 && c1.JmaSlope > 5)
			{
				var entry = c0.Quote.Open;
				var stopLoss = c1.Ema2 - c1.Atr * 0.1m;
				var takeProfit = entry + c1.Atr * 1.0m;
				EntryPosition(PositionSide.Long, c0, entry, stopLoss, takeProfit);
			}
			//else if (c1.Quote.Close < c1.Ema2 - c1.AtrVolume * 40m && c1.Quote.Close > c1.Ema3)
			//{
			//	var entry = c0.Quote.Open;
			//	var stopLoss = entry - c1.AtrVolume * 16m;
			//	var takeProfit = entry + c1.AtrVolume * 32m;
			//	EntryPosition(PositionSide.Long, c0, entry, stopLoss, takeProfit);
			//}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (longPosition.Stage == 0 && c1.Quote.Low <= longPosition.StopLossPrice)
			{
				StopLoss(longPosition, c1);
			}
			else if (longPosition.Stage == 0 && c1.Quote.High >= longPosition.TakeProfitPrice)
			{
				TakeProfitHalf(longPosition);
			}
			if (longPosition.Stage == 1 && c1.Quote.Close < c1.Ema2)
			{
				TakeProfitHalf2(longPosition, c0);
			}

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
