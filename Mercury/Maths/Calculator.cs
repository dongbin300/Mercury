using Binance.Net.Enums;

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
	}
}
