using Mercury.Charts;
using Mercury.Maths;
using Binance.Net.Enums;

namespace Mercury.Backtests
{
	public class GridRangeEstimator(string symbol, List<ChartInfo> charts)
	{
		public decimal Money = 100;
		public string Symbol { get; set; } = symbol;
		public List<ChartInfo> Charts { get; set; } = charts;

		public string Run(int startIndex)
		{
			var prevAverage = (decimal)Charts[startIndex].PredictiveRangesAverage;
			for (int i = startIndex; i < Charts.Count; i++)
			{
				var price = Charts[i].Quote.Close;
				var upper2 = (decimal)Charts[i].PredictiveRangesUpper2;
				var upper = (decimal)Charts[i].PredictiveRangesUpper;
				var average = (decimal)Charts[i].PredictiveRangesAverage;
				var lower = (decimal)Charts[i].PredictiveRangesLower;
				var lower2 = (decimal)Charts[i].PredictiveRangesLower2;

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
