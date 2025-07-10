using Binance.Net.Enums;
using Mercury.Extensions;

namespace Mercury.Maths
{
    public class Calculator
    {
        public static decimal InitialMargin(decimal price, decimal quantity, int leverage = 1)
        {
            return price * quantity / leverage;
        }

        public static decimal Pnl(PositionSide side, decimal entry, decimal exit, decimal quantity)
        {
            return side switch
            {
                PositionSide.Long => (exit - entry) * quantity,
                PositionSide.Short => -(exit - entry) * quantity,
                _ => 0
            };
        }

        public static decimal Roe(PositionSide side, decimal entry, decimal exit, int leverage = 1)
        {
            return side switch
            {
                PositionSide.Long => ((exit - entry) / entry * 100 * leverage).Round(2),
                PositionSide.Short => -((exit - entry) / entry * 100 * leverage).Round(2),
                _ => 0,
            };
        }

        public static decimal TargetPrice(PositionSide side, decimal entry, decimal targetRoe, int leverage = 1)
        {
            return side switch
            {
                PositionSide.Long => entry * (1 + targetRoe / leverage / 100),
                PositionSide.Short => entry * (1 - targetRoe / leverage / 100),
                _ => 0
            };
        }

        /// <summary>
        /// Only isolated(one-way) leverage type.
        /// Liquidation prices may vary by exchange.
        /// </summary>
        /// <param name="side"></param>
        /// <param name="entry"></param>
        /// <param name="quantity"></param>
        /// <param name="balance"></param>
        /// <param name="leverage"></param>
        /// <returns></returns>
        public static decimal LiquidationPrice(PositionSide side, decimal entry, decimal quantity, decimal balance, int leverage = 1)
        {
            return side switch
            {
                PositionSide.Long => (entry * quantity - balance) / quantity,
                PositionSide.Short => (entry * quantity + balance) / quantity,
                _ => 0
            };
        }

        /// <summary>
        /// Calculate Fee
        /// Fee: 0.04% => feeRate 0.0004
        /// </summary>
        /// <param name="entryPrice"></param>
        /// <param name="entryQuantity"></param>
        /// <param name="exitPrice"></param>
        /// <param name="exitQuantity"></param>
        /// <param name="feeRate"></param>
        /// <returns></returns>
        public static decimal Fee(decimal entryPrice, decimal entryQuantity, decimal exitPrice, decimal exitQuantity, decimal feeRate)
        {
            return (entryPrice * entryQuantity + exitPrice * exitQuantity) * feeRate;
        }

        /// <summary>
        /// Calculate Optimal Ranges Leverage
        /// </summary>
        /// <param name="upper"></param>
        /// <param name="lower"></param>
        /// <param name="entry"></param>
        /// <param name="gridCount"></param>
        /// <param name="riskMargin"></param>
        /// <returns></returns>
        public static decimal RangesLeverage(decimal upper, decimal lower, decimal entry, int gridCount, decimal riskMargin)
        {
            return Math.Min(
                RangesMaxLeverage(PositionSide.Long, upper, lower, entry, gridCount, riskMargin),
                RangesMaxLeverage(PositionSide.Short, upper, lower, entry, gridCount, riskMargin)
                );
        }

        /// <summary>
        /// Calculate Ranges Max Leverage
        /// </summary>
        /// <param name="side"></param>
        /// <param name="upper"></param>
        /// <param name="lower"></param>
        /// <param name="entry"></param>
        /// <param name="gridCount"></param>
        /// <param name="riskMargin"></param>
        /// <returns></returns>
		public static decimal RangesMaxLeverage(PositionSide side, decimal upper, decimal lower, decimal entry, int gridCount, decimal riskMargin)
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

		/// <summary>
		/// Calculate Maximum Drawdown(MDD)
		/// 0.0 ~ 1.0(0 ~ 100%)
		/// The Higher, The Worse
		/// </summary>
		/// <param name="assets"></param>
		/// <returns></returns>
		public static decimal Mdd(List<decimal> assets)
		{
			if (assets == null || assets.Count == 0)
            {
				return 0m;
			}

			decimal peak = assets[0];
			decimal mdd = 0m;

			foreach (var asset in assets)
			{
				if (asset > peak)
                {
					peak = asset;
				}

				decimal drawdown = (peak - asset) / peak;
				if (drawdown > mdd)
                {
					mdd = drawdown;
				}
			}

			return mdd;
		}

		/// <summary>
		/// Calculate Sharpe Ratio
		/// 무위험수익률(annualRiskFreeRate) 기본값은 연 3%
		/// </summary>
		/// <param name="assets"></param>
		/// <param name="annualRiskFreeRate"></param>
		/// <returns></returns>
		public static decimal SharpeRatio(List<decimal> assets, double annualRiskFreeRate = 0.03)
		{
			if (assets == null || assets.Count < 2)
				return 0m;

            // 1. 일별 수익률 계산
            List<double> dailyReturns = [];
			for (int i = 1; i < assets.Count; i++)
			{
				if (assets[i - 1] == 0) continue;
				double dailyReturn = (double)((assets[i] - assets[i - 1]) / assets[i - 1]);
				dailyReturns.Add(dailyReturn);
			}

			if (dailyReturns.Count == 0)
				return 0m;

            // 2. 무위험수익률(일간) 환산
            double dailyRiskFreeRate = annualRiskFreeRate / 365;

			// 3. 초과수익률 리스트
			var excessReturns = dailyReturns.Select(r => r - dailyRiskFreeRate).ToList();

			// 4. 평균, 표준편차
			double meanExcessReturn = excessReturns.Average();
			double stdDev = excessReturns.Count > 1
				? Math.Sqrt(excessReturns.Select(r => Math.Pow(r - meanExcessReturn, 2)).Sum() / (excessReturns.Count - 1))
				: 0.0001; // 0으로 나누기 방지

			// 5. Sharpe Ratio (연환산)
			double sharpeRatio = meanExcessReturn / stdDev * Math.Sqrt(365);

			return (decimal)sharpeRatio;
		}

		public static (decimal Level0, decimal Level236, decimal Level382, decimal Level500, decimal Level618, decimal Level786, decimal Level1000)	FibonacciRetracementLevels(decimal low, decimal high)
		{
			decimal[] ratios = { 0m, 0.236m, 0.382m, 0.5m, 0.618m, 0.786m, 1.0m };
			decimal range = high - low;

			decimal[] levels = [.. ratios.Select(r => low + range * r)];

			return (levels[0], levels[1], levels[2], levels[3], levels[4], levels[5], levels[6]);
		}
	}
}
