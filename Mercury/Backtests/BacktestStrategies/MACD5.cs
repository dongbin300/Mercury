using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Data;
using Mercury.Enums;
using Mercury.Maths;

namespace Mercury.Backtests.BacktestStrategies
{
	public class MACD5(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int AdxThreshold;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			var macd1 = MacdTable.GetValues((int)p[0]);
			var macd2 = MacdTable.GetValues((int)p[1]);

			chartPack.UseMacd(macd1.Item1, macd1.Item2, macd1.Item3, macd2.Item1, macd2.Item2, macd2.Item3);
			chartPack.UseAdx();
			chartPack.UseSupertrend(10, 1.5);
			chartPack.UseEma((int)p[2]);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var minPrice = GetMinPrice(charts, 14, i);

			var tpPrice = c1.Ema1 ?? 0;
			var slPrice = minPrice - (tpPrice - minPrice) * 0.1m;
			var tpPer = Calculator.Roe(PositionSide.Long, c0.Quote.Open, tpPrice);
			if (IsPowerGoldenCross(charts, 14, i, c1.Macd) && IsPowerGoldenCross2(charts, 14, i, c1.Macd2) && tpPer > 1.0m)
			{
				EntryPosition(PositionSide.Long, c0, c0.Quote.Open, slPrice, tpPrice);
			}
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
			if (longPosition.Stage == 1 && c1.Supertrend1 < 0)
			{
				TakeProfitHalf2(longPosition, c0);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var maxPrice = GetMaxPrice(charts, 14, i);

			var tpPrice = c1.Ema1 ?? 0;
			var slPrice = maxPrice + (maxPrice - tpPrice) * 0.1m;
			var tpPer = Calculator.Roe(PositionSide.Short, c0.Quote.Open, tpPrice);
			if (IsPowerDeadCross(charts, 14, i, c1.Macd) && IsPowerDeadCross2(charts, 14, i, c1.Macd2) && tpPer > 1.0m)
			{
				EntryPosition(PositionSide.Short, c0, c0.Quote.Open, slPrice, tpPrice);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (shortPosition.Stage == 0 && c1.Quote.High >= shortPosition.StopLossPrice)
			{
				StopLoss(shortPosition, c1);
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

		bool IsPowerGoldenCross(List<ChartInfo> charts, int lookback, int index, decimal? currentMacd = null)
		{
			// Starts at charts[index - 1]
			for (int i = 0; i < lookback; i++)
			{
				var c0 = charts[index - 1 - i];
				var c1 = charts[index - 2 - i];

				if (currentMacd == null)
				{
					if (c0.Macd < 0 && c0.Macd > c0.MacdSignal && c1.Macd < c1.MacdSignal && c0.Adx > AdxThreshold && c0.Supertrend1 > 0)
					{
						return true;
					}
				}
				else
				{
					if (c0.Macd < 0 && c0.Macd > c0.MacdSignal && c1.Macd < c1.MacdSignal && c0.Adx > AdxThreshold && c0.Supertrend1 > 0 && c0.Macd < currentMacd)
					{
						return true;
					}
				}
			}
			return false;
		}

		bool IsPowerGoldenCross2(List<ChartInfo> charts, int lookback, int index, decimal? currentMacd = null)
		{
			// Starts at charts[index - 1]
			for (int i = 0; i < lookback; i++)
			{
				var c0 = charts[index - 1 - i];
				var c1 = charts[index - 2 - i];

				if (currentMacd == null)
				{
					if (c0.Macd2 < 0 && c0.Macd2 > c0.MacdSignal2 && c1.Macd2 < c1.MacdSignal2 && c0.Adx > AdxThreshold && c0.Supertrend1 > 0)
					{
						return true;
					}
				}
				else
				{
					if (c0.Macd2 < 0 && c0.Macd2 > c0.MacdSignal2 && c1.Macd2 < c1.MacdSignal2 && c0.Adx > AdxThreshold && c0.Supertrend1 > 0 && c0.Macd2 < currentMacd)
					{
						return true;
					}
				}
			}
			return false;
		}

		bool IsPowerDeadCross(List<ChartInfo> charts, int lookback, int index, decimal? currentMacd = null)
		{
			// Starts at charts[index - 1]
			for (int i = 0; i < lookback; i++)
			{
				var c0 = charts[index - 1 - i];
				var c1 = charts[index - 2 - i];

				if (currentMacd == null)
				{
					if (c0.Macd > 0 && c0.Macd < c0.MacdSignal && c1.Macd > c1.MacdSignal && c0.Adx > AdxThreshold && c0.Supertrend1 < 0)
					{
						return true;
					}
				}
				else
				{
					if (c0.Macd > 0 && c0.Macd < c0.MacdSignal && c1.Macd > c1.MacdSignal && c0.Adx > AdxThreshold && c0.Supertrend1 < 0 && c0.Macd > currentMacd)
					{
						return true;
					}
				}
			}
			return false;
		}

		bool IsPowerDeadCross2(List<ChartInfo> charts, int lookback, int index, decimal? currentMacd = null)
		{
			// Starts at charts[index - 1]
			for (int i = 0; i < lookback; i++)
			{
				var c0 = charts[index - 1 - i];
				var c1 = charts[index - 2 - i];

				if (currentMacd == null)
				{
					if (c0.Macd2 > 0 && c0.Macd2 < c0.MacdSignal2 && c1.Macd2 > c1.MacdSignal2 && c0.Adx > AdxThreshold && c0.Supertrend1 < 0)
					{
						return true;
					}
				}
				else
				{
					if (c0.Macd2 > 0 && c0.Macd2 < c0.MacdSignal2 && c1.Macd2 > c1.MacdSignal2 && c0.Adx > AdxThreshold && c0.Supertrend1 < 0 && c0.Macd2 > currentMacd)
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}
