using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	public class TS1(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public decimal ProfitRatio { get; set; }

		protected override void InitIndicator(ChartPack chartPack, params decimal[] p)
		{
			chartPack.UseTripleSupertrend((int)p[0], (double)p[1], (int)p[2], (double)p[3], (int)p[4], (double)p[5]);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			var slPrice = Math.Abs(c1.Supertrend2 ?? 0);
			var tpPrice = c1.Quote.Close + (c1.Quote.Close - slPrice) * ProfitRatio; // 1:1.5

			if(c1.Supertrend1 > 0 && c2.Supertrend1 < 0 && c1.Supertrend2 > 0 && c1.Supertrend3 > 0)
			{
				EntryPosition(PositionSide.Long, c0, c0.Quote.Open, slPrice, tpPrice);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			//if(c1.Supertrend3 < 0)
			//{
			//	ExitPosition(longPosition, c0, c0.Quote.Open);
			//}

			if (longPosition.Stage == 0 && c1.Quote.Close <= longPosition.StopLossPrice)
			{
				ExitPosition(longPosition, c1, c1.Quote.Close);
				//StopLoss(longPosition, c1);
			}
			else if (longPosition.Stage == 0 && c1.Quote.High >= longPosition.TakeProfitPrice)
			{
				TakeProfitHalf(longPosition);
			}
			if (longPosition.Stage == 1 && c1.Supertrend2 < 0)
			{
				TakeProfitHalf2(longPosition, c0);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			var slPrice = Math.Abs(c1.Supertrend2 ?? 0);
			var tpPrice = c1.Quote.Close - (slPrice - c1.Quote.Close) * ProfitRatio;

			if (c1.Supertrend1 < 0 && c2.Supertrend1 > 0 && c1.Supertrend2 < 0 && c1.Supertrend3 < 0)
			{
				EntryPosition(PositionSide.Short, c0, c0.Quote.Open, slPrice, tpPrice);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			//if (c1.Supertrend3 > 0)
			//{
			//	ExitPosition(shortPosition, c0, c0.Quote.Open);
			//}

			if (shortPosition.Stage == 0 && c1.Quote.Close >= shortPosition.StopLossPrice)
			{
				ExitPosition(shortPosition, c1, c1.Quote.Close);
				//StopLoss(shortPosition, c1);
			}
			else if (shortPosition.Stage == 0 && c1.Quote.Low <= shortPosition.TakeProfitPrice)
			{
				TakeProfitHalf(shortPosition);
			}
			if (shortPosition.Stage == 1 && c1.Supertrend2 > 0)
			{
				TakeProfitHalf2(shortPosition, c0);
			}
		}
	}
}
