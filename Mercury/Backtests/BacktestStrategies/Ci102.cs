using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	public class Ci102(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 28;
		public int IchimokuTenkan = 9;
		public int IchimokuKijun = 26;
		public int IchimokuSenkouB = 52;
		public int DemaPeriod = 34;
		public int AtrPeriod = 14;

		public double MinCciThreshold = 40;
		public double MaxCciThreshold = 120;
		public double CciStdMul = 1.5;      // StdDev * multiplier -> threshold
		public decimal VolumeMultiplier = 1.15m;
		public decimal MinAtrRatio = 0.003m;  // ATR/Close < this -> too quiet -> ignore signals

		public int MinBarsBetweenEntries = 3; // 최소 캔들 수 (candle count) / 구현 환경에 맞춰 조정

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkan, IchimokuKijun, IchimokuSenkouB);
			chartPack.UseDema(DemaPeriod);
			chartPack.UseAtr(AtrPeriod);
			chartPack.UseVolumeSma(30);
			chartPack.UseCciVolatilityThreshold(CciPeriod, 20, CciStdMul, MinCciThreshold, MaxCciThreshold);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// 1) adaptive CCI threshold
			bool cciTrigger = c1.Cci > c1.CciVolatilityThreshold;

			// 2) Ichimoku cloud relaxed filter: price above either LeadingSpan1 or LeadingSpan2
			bool cloudOk = (c1.Quote.Close > c1.IcLeadingSpan1) || (c1.Quote.Close > c1.IcLeadingSpan2);

			// 3) DEMA direction: DEMA1 > DEMA2 (if DEMA2 available) and positive slope
			bool demaAbove = c1.Dema1 > c2.Dema1; // indicates upward trend

			// 4) Volume filter
			var avgVol = c1.VolumeSma;
			bool volOk = avgVol > 0m && c1.Quote.Volume > avgVol * VolumeMultiplier;
			var atrRatio = c1.Atr / c1.Quote.Close;
			bool atrOk = atrRatio >= MinAtrRatio;

			// 6) Tenkan > Kijun confirmation
			bool tkBull = c1.IcConversion > c1.IcBase;

			if (cciTrigger && cloudOk && demaAbove && volOk && atrOk && tkBull)
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
				if (c1.Quote.Close >= longPosition.EntryPrice * 1.08m)
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
			if (c1.Dema1 < c2.Dema1 && c1.Quote.Close < c1.Dema1 ||
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
			bool cciTrigger = c1.Cci < -c1.CciVolatilityThreshold;

			// cloud relaxed: price below either span
			bool cloudOk = (c1.Quote.Close < c1.IcLeadingSpan1) || (c1.Quote.Close < c1.IcLeadingSpan2);

			// DEMA direction negative
			bool demaBelow = c1.Dema1 < c2.Dema1;

			// volume & atr filters
			var avgVol = c1.VolumeSma;
			bool volOk = avgVol > 0m && c1.Quote.Volume > avgVol * VolumeMultiplier;
			var atrRatio = c1.Atr / c1.Quote.Close;
			bool atrOk = atrRatio >= MinAtrRatio;

			// Tenkan < Kijun
			bool tkBear = c1.IcConversion < c1.IcBase;

			if (cciTrigger && cloudOk && demaBelow && volOk && atrOk && tkBear)
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
				if (c1.Quote.Close <= shortPosition.EntryPrice * 0.92m)
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
			if (c1.Dema1 > c2.Dema1 && c1.Quote.Close > c1.Dema1 ||
				c1.Quote.Close >= shortPosition.EntryPrice * 1.06m ||
				c1.Quote.Close > c1.IcBase && c1.Quote.Close > c1.Dema1)
			{
				ExitPosition(shortPosition, c1, c1.Quote.Close);
				return;
			}
		}
	}
}
