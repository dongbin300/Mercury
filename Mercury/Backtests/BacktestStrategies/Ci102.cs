using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	public class Ci102 : Backtester
	{
		public Ci102(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals)
			: base(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals) { }

		// ----- Parameters (튜닝 가능) -----
		public int CciPeriod = 28;
		public int IchimokuTenkan = 9;
		public int IchimokuKijun = 26;
		public int IchimokuSenkouB = 52;
		public int DemaPeriod = 34;
		public int AtrPeriod = 14;

		// adaptive thresholds / multipliers
		public double MinCciThreshold = 40;
		public double MaxCciThreshold = 120;
		public double CciStdMul = 1.5;      // StdDev * multiplier -> threshold
		public decimal VolumeMultiplier = 1.15m;
		public decimal MinAtrRatio = 0.003m;  // ATR/Close < this -> too quiet -> ignore signals

		// re-entry and timing
		public int MinBarsBetweenEntries = 3; // 최소 캔들 수 (candle count) / 구현 환경에 맞춰 조정

		// ----- Init Indicators -----
		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkan, IchimokuKijun, IchimokuSenkouB);
			chartPack.UseDema(DemaPeriod);
			chartPack.UseDemaSlope(DemaPeriod);
			chartPack.UseAtr(AtrPeriod);
			chartPack.UseVolumeSma(30);
			chartPack.UseCciVolatilityThreshold(CciPeriod, 20, CciStdMul, MinCciThreshold, MaxCciThreshold);
		}

		// -------------------------
		// Helper utilities
		// -------------------------
		//private decimal ComputeAdaptiveCciThreshold(List<ChartInfo> charts, int i, int lookback = 20)
		//{
		//	// computes stddev of CCI over last 'lookback' bars ending at i-1
		//	int endIndex = i - 1;
		//	int startIndex = Math.Max(0, endIndex - lookback + 1);
		//	var sample = new List<decimal>();
		//	for (int idx = startIndex; idx <= endIndex; idx++)
		//	{
		//		if (charts[idx].Cci != null) sample.Add((decimal)charts[idx].Cci);
		//	}
		//	if (sample.Count < 5) return (MinCciThreshold + MaxCciThreshold) / 2m; // fallback
		//	decimal avg = sample.Average();
		//	decimal sumsq = sample.Select(x => (x - avg) * (x - avg)).Sum();
		//	decimal std = (decimal)Math.Sqrt((double)(sumsq / sample.Count));
		//	decimal thr = std * CciStdMul;
		//	if (thr < MinCciThreshold) thr = MinCciThreshold;
		//	if (thr > MaxCciThreshold) thr = MaxCciThreshold;
		//	return thr;
		//}

		//private decimal ComputeAtrRatio(List<ChartInfo> charts, int i, int period)
		//{
		//	// ATR calculation using True Ranges ending at i-1
		//	int endIndex = i - 1;
		//	int startIndex = Math.Max(1, endIndex - period + 1); // need prev close for TR
		//	var trs = new List<decimal>();
		//	for (int idx = startIndex; idx <= endIndex; idx++)
		//	{
		//		var hi = charts[idx].Quote.High;
		//		var lo = charts[idx].Quote.Low;
		//		var prevClose = charts[idx - 1].Quote.Close;
		//		decimal tr = (decimal)Math.Max((double)(hi - lo), Math.Max((double)Math.Abs(hi - prevClose), (double)Math.Abs(lo - prevClose)));
		//		trs.Add((decimal)tr);
		//	}
		//	if (trs.Count == 0) return decimal.MaxValue;
		//	decimal atr = trs.Average();
		//	decimal lastClose = charts[endIndex].Quote.Close;
		//	if (lastClose <= 0) return decimal.MaxValue;
		//	return atr / lastClose;
		//}

		//private decimal AverageVolume(List<ChartInfo> charts, int i, int lookback = 30)
		//{
		//	int endIndex = i - 1;
		//	int startIndex = Math.Max(0, endIndex - lookback + 1);
		//	var slice = charts.Skip(startIndex).Take(endIndex - startIndex + 1).Select(x => x.Quote.Volume).ToArray();
		//	if (slice.Length == 0) return 0m;
		//	return slice.Average();
		//}

		//// DEMA slope helper (uses Dema1 field that ChartInfo is expected to provide)
		//private decimal DemaSlope(List<ChartInfo> charts, int i)
		//{
		//	// slope = DEMA[i-1] - DEMA[i-2]
		//	var c1 = charts[i - 1];
		//	var c2 = charts[Math.Max(0, i - 2)];
		//	if (c1.Dema1 == null || c2.Dema1 == null) return 0m;
		//	return c1.Dema1.Value - c2.Dema1.Value;
		//}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// 1) adaptive CCI threshold
			var thr = c1.CciVolatilityThreshold;
			bool cciTrigger = c1.Cci > thr;

			// 2) Ichimoku cloud relaxed filter: price above either LeadingSpan1 or LeadingSpan2
			bool cloudOk = (c1.Quote.Close > c1.IcLeadingSpan1) || (c1.Quote.Close > c1.IcLeadingSpan2);

			// 3) DEMA direction: DEMA1 > DEMA2 (if DEMA2 available) and positive slope
			bool demaAbove = c1.Dema1 > c2.Dema1; // indicates upward trend
			bool slopePos = c1.DemaSlope > 0m;
			var demaDir = demaAbove && slopePos;

			// 4) Volume filter
			var avgVol = c1.VolumeSma;
			bool volOk = avgVol > 0m && c1.Quote.Volume > avgVol * VolumeMultiplier;
			var atrRatio = c1.Atr / c1.Quote.Close;
			bool atrOk = atrRatio >= MinAtrRatio;

			// 6) Tenkan > Kijun confirmation
			bool tkBull = c1.IcConversion > c1.IcBase;

			if (cciTrigger && cloudOk && demaDir && volOk && atrOk && tkBull)
			{
				EntryPosition(PositionSide.Long, c1, c1.Quote.Close);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			// 1) Partial take at moderate profit or CCI overbought
			if (longPosition.Stage == 0)
			{
				if (c1.Cci > (c1.CciVolatilityThreshold * 0.9m) || 
					c1.Quote.Close >= longPosition.EntryPrice * 1.08m)
				{
					TakeProfitHalf(longPosition, c1.Quote.Close);
					return;
				}
			}
			else // Stage 1: remaining position
			{
				bool cciDown = c1.Cci < c2.Cci && c2.Cci < c3.Cci && c1.Quote.Close < c2.Quote.Close;

				if (cciDown ||
					c1.IcConversion < c1.IcBase)
				{
					TakeProfitHalf2(longPosition, c1);
					return;
				}
			}

			// trailing: if dema slope flips negative -> exit
			// hard stop-loss: -6% (adaptive for entry distance)
			// kijun break + dema fail (safety combo)
			if (c1.DemaSlope < 0m && c1.Quote.Close < c1.Dema1 ||
				c1.Quote.Close <= longPosition.EntryPrice * 0.94m ||
				c1.Quote.Close < c1.IcBase && c1.Quote.Close < c1.Dema1)
			{
				ExitPosition(longPosition, c1, c1.Quote.Close);
				return;
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// adaptive CCI negative threshold
			var thr = c1.CciVolatilityThreshold;
			bool cciTrigger = c1.Cci < -thr;

			// cloud relaxed: price below either span
			bool cloudOk = (c1.Quote.Close < c1.IcLeadingSpan1) || (c1.Quote.Close < c1.IcLeadingSpan2);

			// DEMA direction negative
			bool demaBelow = c1.Dema1 < c2.Dema1;
			bool slopeNeg = c1.DemaSlope < 0m;
			var demaDir = demaBelow && slopeNeg;

			// volume & atr filters
			var avgVol = c1.VolumeSma;
			bool volOk = avgVol > 0m && c1.Quote.Volume > avgVol * VolumeMultiplier;
			var atrRatio = c1.Atr / c1.Quote.Close;
			bool atrOk = atrRatio >= MinAtrRatio;

			// Tenkan < Kijun
			bool tkBear = c1.IcConversion < c1.IcBase;

			if (cciTrigger && cloudOk && demaDir && volOk && atrOk && tkBear)
			{
				EntryPosition(PositionSide.Short, c1, c1.Quote.Close);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			// partial take
			if (shortPosition.Stage == 0)
			{
				if (c1.Cci < -(c1.CciVolatilityThreshold * 0.9m) ||
					c1.Quote.Close <= shortPosition.EntryPrice * 0.92m)
				{
					TakeProfitHalf(shortPosition, c1.Quote.Close);
					return;
				}
			}
			else
			{
				bool cciUp = c1.Cci > c2.Cci && c2.Cci > c3.Cci && c1.Quote.Close > c2.Quote.Close;

				if (cciUp ||
					c1.IcConversion > c1.IcBase)
				{
					TakeProfitHalf2(shortPosition, c1);
					return;
				}
			}

			// trailing: dema slope flips positive -> exit
			// hard stop-loss
			// kijun break + dema fail (safety)
			if (c1.DemaSlope > 0m && c1.Quote.Close > c1.Dema1 ||
				c1.Quote.Close >= shortPosition.EntryPrice * 1.06m ||
				c1.Quote.Close > c1.IcBase && c1.Quote.Close > c1.Dema1)
			{
				ExitPosition(shortPosition, c1, c1.Quote.Close);
				return;
			}
		}
	}

}
