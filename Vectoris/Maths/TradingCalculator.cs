using Binance.Net.Enums;

using Vectoris.Extensions;

namespace Vectoris.Maths;

public static class TradingCalculator
{
	private const decimal DefaultSeed = 1_000_000m;
	private const double DefaultRiskFreeRate = 0.03;

	/// <summary>
	/// Initial Margin = price × quantity ÷ leverage
	/// </summary>
	public static decimal InitialMargin(decimal price, decimal quantity, int leverage = 1)
		=> price * quantity / leverage;

	/// <summary>
	/// Profit & Loss
	/// </summary>
	public static decimal Pnl(PositionSide side, decimal entry, decimal exit, decimal quantity)
		=> side switch
		{
			PositionSide.Long => (exit - entry) * quantity,
			PositionSide.Short => (entry - exit) * quantity,
			_ => 0
		};

	/// <summary>
	/// Return on Equity (ROE, %)
	/// </summary>
	public static decimal Roe(PositionSide side, decimal entry, decimal exit, int leverage = 1)
	{
		if (entry == 0) return 0;

		var pct = ((exit - entry) / entry) * 100 * leverage;

		return side switch
		{
			PositionSide.Long => pct.Round(2),
			PositionSide.Short => (-pct).Round(2),
			_ => 0
		};
	}

	/// <summary>
	/// Target exit price to reach expected ROE(%)
	/// </summary>
	public static decimal TargetPrice(PositionSide side, decimal entry, decimal targetRoe, int leverage = 1)
		=> side switch
		{
			PositionSide.Long => entry * (1 + targetRoe / leverage / 100),
			PositionSide.Short => entry * (1 - targetRoe / leverage / 100),
			_ => 0
		};

	/// <summary>
	/// Liquidation price (Isolated & One-Way)
	/// </summary>
	public static decimal LiquidationPrice(PositionSide side, decimal entry, decimal quantity, decimal balance)
		=> side switch
		{
			PositionSide.Long => (entry * quantity - balance) / quantity,
			PositionSide.Short => (entry * quantity + balance) / quantity,
			_ => 0
		};

	/// <summary>
	/// Trading fee: (entryValue + exitValue) × feeRate
	/// </summary>
	public static decimal Fee(decimal entryPrice, decimal entryQty, decimal exitPrice, decimal exitQty, decimal feeRate)
		=> (entryPrice * entryQty + exitPrice * exitQty) * feeRate;

	/// <summary>
	/// Optimal leverage for grid range.
	/// </summary>
	public static decimal RangesLeverage(decimal upper, decimal lower, decimal entry, int gridCount, decimal riskMargin)
		=> Math.Min(
			RangesMaxLeverage(PositionSide.Long, upper, lower, entry, gridCount, riskMargin),
			RangesMaxLeverage(PositionSide.Short, upper, lower, entry, gridCount, riskMargin)
		);

	/// <summary>
	/// Maximum possible leverage for one side within range grid
	/// </summary>
	public static decimal RangesMaxLeverage(
		PositionSide side,
		decimal upper,
		decimal lower,
		decimal entry,
		int gridCount,
		decimal riskMargin)
	{
		decimal lowerLimit = lower * (1 - riskMargin);
		decimal upperLimit = upper * (1 + riskMargin);
		decimal tradeAmount = DefaultSeed / gridCount;
		decimal gridInterval = (upper - lower) / (gridCount - 1);

		decimal loss = 0;

		if (side == PositionSide.Long)
		{
			for (decimal p = lower; p <= entry; p += gridInterval)
			{
				var qty = tradeAmount / p;
				loss += (lowerLimit - p) * qty;
			}
		}
		else if (side == PositionSide.Short)
		{
			for (decimal p = upper; p >= entry; p -= gridInterval)
			{
				var qty = tradeAmount / p;
				loss += (p - upperLimit) * qty;
			}
		}

		return loss == 0 ? DefaultSeed : DefaultSeed / Math.Abs(loss);
	}

	/// <summary>
	/// Maximum Drawdown (0~1)
	/// </summary>
	public static decimal Mdd(IReadOnlyList<decimal> assets)
	{
		if (assets == null || assets.Count == 0)
			return 0m;

		decimal peak = assets[0];
		decimal maxDrawdown = 0m;

		foreach (var asset in assets)
		{
			if (asset > peak)
				peak = asset;

			if (peak > 0)
			{
				var dd = (peak - asset) / peak;
				if (dd > maxDrawdown)
					maxDrawdown = dd;
			}
		}

		return maxDrawdown;
	}

	/// <summary>
	/// Sharpe Ratio (Annualized)
	/// </summary>
	public static decimal SharpeRatio(IReadOnlyList<decimal> assets, double annualRiskFreeRate = DefaultRiskFreeRate)
	{
		if (assets == null || assets.Count < 2)
			return 0m;

		var returns = new List<double>(assets.Count - 1);

		for (int i = 1; i < assets.Count; i++)
		{
			if (assets[i - 1] == 0) continue;

			var r = (double)((assets[i] - assets[i - 1]) / assets[i - 1]);
			returns.Add(r);
		}

		if (returns.Count == 0)
			return 0m;

		double dailyRf = annualRiskFreeRate / 365;

		var excess = returns.Select(r => r - dailyRf).ToList();
		double mean = excess.Average();

		double variance = excess.Count > 1
			? excess.Select(r => Math.Pow(r - mean, 2)).Sum() / (excess.Count - 1)
			: 0.0001;

		double std = Math.Sqrt(variance);

		double sharpe = mean / std * Math.Sqrt(365);
		return (decimal)sharpe;
	}

	/// <summary>
	/// Fibonacci Retracement Levels
	/// </summary>
	public static FibonacciLevels FibonacciRetracement(decimal low, decimal high)
	{
		decimal range = high - low;

		return new FibonacciLevels(
			Level0: low,
			Level236: low + range * 0.236m,
			Level382: low + range * 0.382m,
			Level500: low + range * 0.5m,
			Level618: low + range * 0.618m,
			Level786: low + range * 0.786m,
			Level1000: high
		);
	}

	/// <summary>
	/// Structured Fibonacci result
	/// </summary>
	public readonly record struct FibonacciLevels(
		decimal Level0,
		decimal Level236,
		decimal Level382,
		decimal Level500,
		decimal Level618,
		decimal Level786,
		decimal Level1000
	);
}
