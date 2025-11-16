using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// long entry: -80 아래에서 prediction이 ma를 golden cross
	/// long exit: atr(14) + st(10, 1.5)
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class MLMIP1(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public decimal ProfitRatio;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseMlmip((int)p[0], (int)p[1], (int)p[2], (int)p[3], (int)p[4]);
			chartPack.UseSupertrend(10, 1.5);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			var slPrice = c1.Quote.Close - (decimal)(c1.Atrma ?? 0);
			var tpPrice = c1.Quote.Close + (c1.Quote.Close - slPrice) * ProfitRatio; // 1:1.5

			if (c1.Prediction < -80 && c1.PredictionMa < -80 &&
				c2.Prediction < -80 && c2.PredictionMa < -80 &&
				c1.Prediction > c1.PredictionMa &&
				c2.Prediction < c2.PredictionMa)
			{
				EntryPosition(PositionSide.Long, c0, c0.Quote.Open, slPrice, tpPrice);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (longPosition.Stage == 0 && c1.Quote.Close <= longPosition.StopLossPrice)
			{
				ExitPosition(longPosition, c1, c1.Quote.Close);
			}
			else if (longPosition.Stage == 0 && c1.Quote.High >= longPosition.TakeProfitPrice)
			{
				TakeProfitHalf(longPosition);
			}
			if (longPosition.Stage == 1 && c1.Supertrend1 < 0)
			{
				TakeProfitHalf2(longPosition, c0);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			var slPrice = c1.Quote.Close + (decimal)(c1.Atrma ?? 0);
			var tpPrice = c1.Quote.Close - (slPrice - c1.Quote.Close) * ProfitRatio;

			if (c1.Prediction > 80 && c1.PredictionMa > 80 &&
				c2.Prediction > 80 && c2.PredictionMa > 80 &&
				c1.Prediction < c1.PredictionMa &&
				c2.Prediction > c2.PredictionMa)
			{
				EntryPosition(PositionSide.Short, c0, c0.Quote.Open, slPrice, tpPrice);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (shortPosition.Stage == 0 && c1.Quote.Close >= shortPosition.StopLossPrice)
			{
				ExitPosition(shortPosition, c1, c1.Quote.Close);
			}
			else if (shortPosition.Stage == 0 && c1.Quote.Low <= shortPosition.TakeProfitPrice)
			{
				TakeProfitHalf(shortPosition);
			}
			if (shortPosition.Stage == 1 && c1.Supertrend1 > 0)
			{
				TakeProfitHalf2(shortPosition, c0);
			}
		}
	}
}
