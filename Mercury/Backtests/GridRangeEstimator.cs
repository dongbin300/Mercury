using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Maths;

namespace Mercury.Backtests
{
	public class GridRangeEstimator(string symbol, List<ChartInfo> charts)
	{
		public decimal Money = 100;
		public string Symbol { get; set; } = symbol;
		public List<ChartInfo> Charts { get; set; } = charts;

		public string Run(int startIndex)
		{
			var prevAverage = Charts[startIndex].PredictiveRangesAverage ?? 0;
			for (int i = startIndex; i < Charts.Count; i++)
			{
				var price = Charts[i].Quote.Close;
				var upper2 = Charts[i].PredictiveRangesUpper2 ?? 0;
				var upper = Charts[i].PredictiveRangesUpper ?? 0;
				var average = Charts[i].PredictiveRangesAverage ?? 0;
				var lower = Charts[i].PredictiveRangesLower ?? 0;
				var lower2 = Charts[i].PredictiveRangesLower2 ?? 0;

				if (prevAverage != average) // Stoploss when different to previous average
				{
					var min = Math.Min(
						Calculator.Roe(PositionSide.Long, prevAverage, price) / 100,
						Calculator.Roe(PositionSide.Short, prevAverage, price) / 100);
					Money += Money * min;
				}
				else
				{
					Money += Money * (Charts[i].BodyLength / 100);
				}

				prevAverage = average;
			}

			return string.Empty;
		}
	}
}
