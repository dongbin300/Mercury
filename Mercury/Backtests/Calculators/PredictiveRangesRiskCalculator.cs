using Binance.Net.Enums;

using Mercury.Charts;

namespace Mercury.Backtests.Calculators
{
	public class PredictiveRangesRiskCalculator(string symbol, List<ChartInfo> charts, int gridCount)
	{
		public decimal Money = 100;
		public string Symbol { get; set; } = symbol;
		public List<ChartInfo> Charts { get; set; } = charts;
		public int GridCount { get; set; } = gridCount;

		public decimal Run(int startIndex)
		{
			var isFirst = true;
			var maxLeverages = new List<decimal>();
			var prevAverage = Charts[startIndex].PredictiveRangesAverage;
			for (int i = startIndex; i < Charts.Count; i++)
			{
				var date = Charts[i].DateTime;
				var price = Charts[i].Quote.Close;
				var upper2 = Charts[i].PredictiveRangesUpper2 ?? 0;
				var upper = Charts[i].PredictiveRangesUpper ?? 0;
				var average = Charts[i].PredictiveRangesAverage ?? 0;
				var lower = Charts[i].PredictiveRangesLower ?? 0;
				var lower2 = Charts[i].PredictiveRangesLower2 ?? 0;

				if (prevAverage != average || isFirst)
				{
					isFirst = false;
					var minOfMaxLeverages = Math.Min(
						CalculateMaxLeverage(PositionSide.Long, upper2, lower2, price, GridCount),
						CalculateMaxLeverage(PositionSide.Short, upper2, lower2, price, GridCount)
						);
					maxLeverages.Add(minOfMaxLeverages);
				}

				prevAverage = average;
			}
			var minLeverageOfRanges = maxLeverages.Min();

			return minLeverageOfRanges;
		}

		public decimal CalculateMaxLeverage(PositionSide side, decimal upper, decimal lower, decimal entry, int gridCount)
		{
			decimal seed = 1_000_000;
			decimal lowerLimit = lower * 0.9m;
			decimal upperLimit = upper * 1.1m;
			var tradeAmount = seed / gridCount;
			var gridInterval = (upper - lower) / (gridCount + 1);
			decimal loss = 0;

			if (side == PositionSide.Long)
			{
				for (decimal price = lower; price <= entry; price += gridInterval)
				{
					var coinCount = tradeAmount / price;
					loss += (lowerLimit - price) * coinCount;
				}
			}
			else if (side == PositionSide.Short)
			{
				for (decimal price = upper; price >= entry; price -= gridInterval)
				{
					var coinCount = tradeAmount / price;
					loss += (price - upperLimit) * coinCount;
				}
			}

			if (loss == 0)
			{
				return seed;
			}

			return seed / -loss;
		}
	}
}
