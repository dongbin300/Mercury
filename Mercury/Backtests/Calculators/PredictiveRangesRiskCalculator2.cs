using Binance.Net.Enums;

using Mercury.Charts;

namespace Mercury.Backtests.Calculators
{
	public class PredictiveRangesRiskCalculator2(string symbol, List<ChartInfo> charts, int gridCount, decimal riskMargin)
	{
		public decimal Money = 100;
		public string Symbol { get; set; } = symbol;
		public List<ChartInfo> Charts { get; set; } = charts;
		public int GridCount { get; set; } = gridCount;
		/// <summary>
		/// Upper, Lower 밖 허용 범위
		/// 10%까지 허용이면 0.1로 설정
		/// </summary>
		public decimal RiskMargin { get; set; } = riskMargin;

		public List<ChartInfo> Run(int startIndex, int? leverage = null)
		{
			var isFirst = true;
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

				// Predictive Ranges 값이 바뀌면
				if (prevAverage != average || isFirst)
				{
					isFirst = false;
					// 레버리지 정수로 고정
					var minOfMaxLeverages = (int)Math.Min(
						CalculateMaxLeverage(PositionSide.Long, upper2, lower2, price, GridCount, RiskMargin),
						CalculateMaxLeverage(PositionSide.Short, upper2, lower2, price, GridCount, RiskMargin)
						);
					Charts[i].PredictiveRangesMaxLeverage = minOfMaxLeverages;
				}
				else
				{
					Charts[i].PredictiveRangesMaxLeverage = Charts[i - 1].PredictiveRangesMaxLeverage;
				}
				var leverageForLiquidationPrice = leverage == null ? Charts[i].PredictiveRangesMaxLeverage : leverage.Value;
				//Charts[i].LiquidationPriceLong = CalculateLiquidationPrices(PositionSide.Long, upper2, lower2, price, GridCount, leverageForLiquidationPrice);
				//Charts[i].LiquidationPriceShort = CalculateLiquidationPrices(PositionSide.Short, upper2, lower2, price, GridCount, leverageForLiquidationPrice);

				prevAverage = average;
			}

			return Charts;
		}

		public decimal CalculateMaxLeverage(PositionSide side, decimal upper, decimal lower, decimal entry, int gridCount, decimal riskMargin)
		{
			decimal seed = 1_000_000;
			decimal lowerLimit = lower * (1 - riskMargin);
			decimal upperLimit = upper * (1 + riskMargin);
			var tradeAmount = seed / gridCount;
			var gridInterval = (upper - lower) / (gridCount - 1);
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

			return seed / Math.Abs(loss);
		}

		public decimal CalculateLiquidationPrices(PositionSide side, decimal upper, decimal lower, decimal entry, int gridCount, int leverage)
		{
			if (leverage == 0)
			{
				return 0m;
			}

			decimal seed = 1_000_000;
			var tradeAmount = seed / gridCount * leverage;
			var gridInterval = (upper - lower) / (gridCount - 1);
			var coinQuantity = 0m;
			var amount = 0m;

			switch (side)
			{
				case PositionSide.Long:
					{
						for (decimal price = lower; price <= entry; price += gridInterval)
						{
							coinQuantity += tradeAmount / price;
							amount += tradeAmount;
							seed -= tradeAmount / leverage;
						}

						if (coinQuantity == 0)
						{
							return 0m;
						}

						var margin = amount / leverage;
						var average = amount / coinQuantity;

						return average * (1 - 1 / (decimal)leverage + coinQuantity / (leverage * (margin + seed)));
					}

				case PositionSide.Short:
					{
						for (decimal price = upper; price >= entry; price -= gridInterval)
						{
							coinQuantity += tradeAmount / price;
							amount += tradeAmount;
							seed -= tradeAmount / leverage;
						}

						if (coinQuantity == 0)
						{
							return 0m;
						}

						var margin = amount / leverage;
						var average = amount / coinQuantity;

						return average * (1 + 1 / (decimal)leverage - coinQuantity / (leverage * (margin + seed)));
					}

				default:
					return 0m;
			}

		}
	}
}
