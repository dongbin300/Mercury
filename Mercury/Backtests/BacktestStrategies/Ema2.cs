using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;
using Mercury.Maths;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// 1. 피보나치 조정대
	/// - 365일이내 저가,고가 피보나치
	/// 
	/// 2. EMA
	/// - 30일이내 120 < 1 < 240 (5회이상)
	/// - 30일이내 120에 98~105% (3회이상)
	///	- 30일이내 전일종가대비고가 20%이상
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class Ema2(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int FibonacciPeriod = 365;
		public int BasePeriod = 30;
		public int Ema1Period = 120;
		public int Ema2Period = 240;
		public decimal NearLower = 0.98m;
		public decimal NearUpper = 1.05m;
		public decimal HighRange = 20m;


		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseEma(Ema1Period, Ema2Period);
			chartPack.UseSupertrend(10, 1.5);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (IsInEmaRange(charts, i - 1, BasePeriod, 5) &&
				IsNearEma120(charts, i - 1, BasePeriod, NearLower, NearUpper, 3) &&
				HasHighOverPrevClose(charts, i - 1, BasePeriod, HighRange))
			{
				var hl = GetHighLowInPeriod(charts, i - 1, FibonacciPeriod);
				var fr = Calculator.FibonacciRetracementLevels(hl.Low, hl.High);

				var fibLevels = new decimal[]
				{
					fr.Level0,
					fr.Level236,
					fr.Level382,
					fr.Level500,
					fr.Level618,
					fr.Level786,
					fr.Level1000
				};
				var zone = GetFibonacciZone(c0.Quote.Open, fibLevels);
				if (zone != null)
				{
					int lower = zone.Value.LowerIdx;
					int upper = zone.Value.UpperIdx;

					decimal stopLoss = fibLevels[lower]; // 하단 레벨
					decimal takeProfit = fibLevels[upper]; // 상단 레벨

					EntryPosition(PositionSide.Long, c0, c0.Quote.Open, stopLoss, takeProfit);
				}
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (longPosition.Stage == 0 && c1.Quote.Close <= longPosition.StopLossPrice)
			{
				ExitPosition(longPosition, c0, c1.Quote.Close); // 종가가 손절가 이하로 마감시
				//StopLoss(longPosition, c1);
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
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
		}

		private (int LowerIdx, int UpperIdx)? GetFibonacciZone(decimal price, decimal[] fibLevels)
		{
			for (int j = 0; j < fibLevels.Length - 1; j++)
			{
				if (price <= fibLevels[j] && price > fibLevels[j + 1])
					return (j + 1, j);

				if (price >= fibLevels[j] && price < fibLevels[j + 1])
					return (j, j + 1);
			}
			return null;
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="charts"></param>
		/// <param name="currentIndex"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		private (decimal High, decimal Low) GetHighLowInPeriod(List<ChartInfo> charts, int currentIndex, int period)
		{
			int from = Math.Max(0, currentIndex - period + 1);
			decimal high = decimal.MinValue;
			decimal low = decimal.MaxValue;

			for (int i = from; i <= currentIndex; i++)
			{
				var c = charts[i];
				if (c.Quote.High > high) high = c.Quote.High;
				if (c.Quote.Low < low) low = c.Quote.Low;
			}

			return (high, low);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="charts"></param>
		/// <param name="currentIndex"></param>
		/// <param name="lookback"></param>
		/// <param name="minCount"></param>
		/// <returns></returns>
		private bool IsInEmaRange(List<ChartInfo> charts, int currentIndex, int lookback, int minCount)
		{
			int count = 0;
			int from = Math.Max(0, currentIndex - lookback + 1);
			for (int i = from; i <= currentIndex; i++)
			{
				var c = charts[i];
				if (c.Ema1 == null || c.Ema2 == null) continue;
				if (c.Quote.Close > (decimal)c.Ema1 && c.Quote.Close < (decimal)c.Ema2)
					count++;
			}
			return count >= minCount;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="charts"></param>
		/// <param name="currentIndex"></param>
		/// <param name="lookback"></param>
		/// <param name="lower"></param>
		/// <param name="upper"></param>
		/// <param name="minCount"></param>
		/// <returns></returns>
		private bool IsNearEma120(List<ChartInfo> charts, int currentIndex, int lookback, decimal lower, decimal upper, int minCount)
		{
			int count = 0;
			int from = Math.Max(0, currentIndex - lookback + 1);
			for (int i = from; i <= currentIndex; i++)
			{
				var c = charts[i];
				if (c.Ema1 == null) continue;
				var ratio = c.Quote.Close / (decimal)c.Ema1;
				if (ratio >= lower && ratio <= upper)
					count++;
			}
			return count >= minCount;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="charts"></param>
		/// <param name="currentIndex"></param>
		/// <param name="lookback"></param>
		/// <param name="percent"></param>
		/// <returns></returns>
		private bool HasHighOverPrevClose(List<ChartInfo> charts, int currentIndex, int lookback = 30, decimal percent = 20)
		{
			int from = Math.Max(1, currentIndex - lookback + 1); // 0번째는 전일 종가가 없으니 1부터 시작
			decimal ratio = 1 + (percent / 100m);

			for (int i = from; i <= currentIndex; i++)
			{
				var prevClose = charts[i - 1].Quote.Close;
				var high = charts[i].Quote.High;
				if (prevClose == 0) continue; // 0으로 나누는 경우 방지
				if (high >= prevClose * ratio)
					return true;
			}
			return false;
		}
	}
}
