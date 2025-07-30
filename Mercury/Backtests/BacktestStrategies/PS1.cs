using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// PS1: 15분봉, 다중 이평선 크로스, 수수료 반영, 고빈도 개선 전략
	/// </summary>
	public class PS1 : Backtester
	{
		public int HoldBars = 1;
		public double StopLossRate = 0.003;
		public double TakeProfitRate = 0.007;
		public int[] SmaPeriods = [3, 5, 8, 13, 21, 34];

		public PS1(string reportFileName, decimal startMoney, int leverage,
			MaxActiveDealsType maxActiveDealsType, int maxActiveDeals)
			: base(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
		{
		}

		protected override void InitIndicator(ChartPack chartPack, params decimal[] p)
		{
			chartPack.UseSma(SmaPeriods.ToArray());
		}

		public decimal? GetSma(ChartInfo c, int idx)
		{
			return idx switch
			{
				0 => c.Sma1,
				1 => c.Sma2,
				2 => c.Sma3,
				3 => c.Sma4,
				4 => c.Sma5,
				5 => c.Sma6,
				_ => null
			};
		}

		public bool MultiSmaCross(List<ChartInfo> charts, int i, bool isLong)
		{
			int n = SmaPeriods.Length;
			for (int x = 0; x < n - 1; x++)
			{
				for (int y = x + 1; y < n; y++)
				{
					int shortIdx = x;
					int longIdx = y;
					int minPeriod = Math.Max(SmaPeriods[shortIdx], SmaPeriods[longIdx]);
					if (i < minPeriod) continue;

					var c0 = charts[i];
					var c1 = charts[i - 1];
					var smaShort0 = GetSma(c0, shortIdx);
					var smaLong0 = GetSma(c0, longIdx);
					var smaShort1 = GetSma(c1, shortIdx);
					var smaLong1 = GetSma(c1, longIdx);

					if (smaShort0 != null && smaLong0 != null && smaShort1 != null && smaLong1 != null)
					{
						if (isLong && smaShort1 <= smaLong1 && smaShort0 > smaLong0)
							return true;
						if (!isLong && smaShort1 >= smaLong1 && smaShort0 < smaLong0)
							return true;
					}
				}
			}
			return false;
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (Positions.Any(p => p.Symbol == symbol && p.Side == PositionSide.Long && p.ExitDateTime == null))
				return;
			if (!MultiSmaCross(charts, i, true)) return;

			var c0 = charts[i];
			decimal entryPrice = c0.Quote.Close * (decimal)(1 + FeeRate);
			decimal orderSize = Seed / MaxActiveDeals;
			decimal stopLoss = entryPrice * (decimal)(1 - StopLossRate);
			decimal takeProfit = entryPrice * (decimal)(1 + TakeProfitRate);

			EntryPositionOnlySize(PositionSide.Long, c0, entryPrice, orderSize, stopLoss, takeProfit);
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			decimal feeAdjHigh = c0.Quote.High * (decimal)(1 - FeeRate);
			decimal feeAdjLow = c0.Quote.Low * (decimal)(1 - FeeRate);

			if (feeAdjLow <= longPosition.StopLossPrice)
			{
				ExitPosition(longPosition, c0, longPosition.StopLossPrice);
			}
			else if (feeAdjHigh >= longPosition.TakeProfitPrice)
			{
				ExitPosition(longPosition, c0, longPosition.TakeProfitPrice);
			}
			else
			{
				DateTime entryTime = longPosition.Time;
				int entryIdx = charts.FindIndex(x => x.Quote.Date == entryTime);
				if (entryIdx >= 0 && i - entryIdx >= HoldBars)
				{
					ExitPosition(longPosition, c0, c0.Quote.Close * (decimal)(1 - FeeRate));
				}
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (Positions.Any(p => p.Symbol == symbol && p.Side == PositionSide.Short && p.ExitDateTime == null))
				return;
			if (!MultiSmaCross(charts, i, false)) return;

			var c0 = charts[i];
			decimal entryPrice = c0.Quote.Close * (decimal)(1 - FeeRate);
			decimal orderSize = Seed / MaxActiveDeals;
			decimal stopLoss = entryPrice * (decimal)(1 + StopLossRate);
			decimal takeProfit = entryPrice * (decimal)(1 - TakeProfitRate);

			EntryPositionOnlySize(PositionSide.Short, c0, entryPrice, orderSize, stopLoss, takeProfit);
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			decimal feeAdjHigh = c0.Quote.High * (decimal)(1 + FeeRate);
			decimal feeAdjLow = c0.Quote.Low * (decimal)(1 + FeeRate);

			if (feeAdjHigh >= shortPosition.StopLossPrice)
			{
				ExitPosition(shortPosition, c0, shortPosition.StopLossPrice);
			}
			else if (feeAdjLow <= shortPosition.TakeProfitPrice)
			{
				ExitPosition(shortPosition, c0, shortPosition.TakeProfitPrice);
			}
			else
			{
				DateTime entryTime = shortPosition.Time;
				int entryIdx = charts.FindIndex(x => x.Quote.Date == entryTime);
				if (entryIdx >= 0 && i - entryIdx >= HoldBars)
				{
					ExitPosition(shortPosition, c0, c0.Quote.Close * (decimal)(1 + FeeRate));
				}
			}
		}
	}
}
