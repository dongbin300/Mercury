using System;
using System.Collections.Generic;
using System.Linq;

using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	public class Ci200(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals)
		: Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 20;
		public int IchimokuTenkan = 9;
		public int IchimokuKijun = 26;
		public int IchimokuSenkouB = 52;
		public int AtrPeriod = 14;
		public int VolumeSmaPeriod = 30;

		public decimal VolumeMultiplier = 0.9m;
		public decimal MinAtrRatio = 0.0012m;
		public decimal SlAtrMultiplier = 1.2m;
		public decimal TrailAtrMultiplier = 0.8m;
		public decimal Tp1 = 0.008m;
		public decimal Tp2 = 0.015m;
		public decimal MaxLossPerTrade = 0.02m;
		public int MinBarsBetweenEntries = 2;
		public int MaxHoldBars = 120;

		private readonly Dictionary<string, DateTime> _lastEntryTime = new();

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkan, IchimokuKijun, IchimokuSenkouB);
			chartPack.UseAtr(AtrPeriod);
			chartPack.UseVolumeSma(VolumeSmaPeriod);
			chartPack.UseSma(8);
		}

		private bool CanEnter(string symbol, DateTime now)
		{
			if (!_lastEntryTime.ContainsKey(symbol)) return true;
			var last = _lastEntryTime[symbol];
			return (now - last).TotalMinutes >= MinBarsBetweenEntries;
		}

		private void RecordEntry(string symbol, DateTime time)
		{
			_lastEntryTime[symbol] = time;
		}

		private int BarsSince(DateTime entryTime, DateTime current, TimeSpan barSpan)
		{
			var diff = current - entryTime;
			return (int)Math.Floor(diff.TotalMinutes / barSpan.TotalMinutes);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 4) return;

			var c1 = charts[i - 1];
			if (!CanEnter(symbol, c1.DateTime)) return;

			if (c1.Quote == null) return;
			if (c1.Cci == null) return;
			if (c1.Atr == null || c1.Atr <= 0) return;
			if (c1.VolumeSma == null) return;
			if (c1.IcLeadingSpan1 == null || c1.IcLeadingSpan2 == null) return;
			if (c1.IcConversion == null || c1.IcBase == null) return;

			var c2 = charts[i - 2];

			decimal atrRatio = c1.Atr.Value / c1.Quote.Close;
			if (atrRatio < MinAtrRatio) return;

			bool ichiBull = c1.IcLeadingSpan1 > c1.IcLeadingSpan2;
			if (!ichiBull) return;

			bool priceAboveCloud = c1.Quote.Close > c1.IcLeadingSpan1 || c1.Quote.Close > c1.IcLeadingSpan2;
			bool cciSpike = c1.Cci > 100m;
			bool cciRecovery = c1.Cci > c2.Cci && c2.Cci < 0m && c1.Cci > 0m;
			bool priceAboveShortSma = c1.Quote.Close > c1.Sma1;
			bool volOk = c1.VolumeSma > 0 && c1.Quote.Volume > c1.VolumeSma * VolumeMultiplier;

			if ((cciSpike || cciRecovery) && (priceAboveCloud || priceAboveShortSma) && volOk)
			{
				decimal sl = Math.Min(c1.Quote.Close * (1 - MaxLossPerTrade), c1.Quote.Close - c1.Atr.Value * SlAtrMultiplier);
				EntryPosition(PositionSide.Long, c1, c1.Quote.Close, sl);
				RecordEntry(symbol, c1.DateTime);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPos)
		{
			if (i < 2) return;

			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			if (c1.Quote == null) return;
			if (c1.Atr == null) return;

			int barsHeld = BarsSince(longPos.Time, c1.DateTime, charts[1].DateTime - charts[0].DateTime);
			if (barsHeld >= MaxHoldBars)
			{
				ExitPosition(longPos, c1, c1.Quote.Close);
				return;
			}

			var price = c1.Quote.Close;

			if (price >= longPos.EntryPrice * (1 + Tp1) && longPos.Stage == 0)
			{
				TakeProfitHalf(longPos, price);
				return;
			}
			if (price >= longPos.EntryPrice * (1 + Tp2))
			{
				TakeProfitHalf2(longPos, c1);
				return;
			}

			bool cciRev = c1.Cci < 50m && c1.Cci < c2.Cci;
			bool priceBelow = price < c1.Sma1;
			bool tBreak = price < longPos.EntryPrice - (c1.Atr.Value * TrailAtrMultiplier);

			if (cciRev || priceBelow || tBreak)
			{
				ExitPosition(longPos, c1, price);
				return;
			}

			bool hardStop = price <= longPos.EntryPrice * (1 - MaxLossPerTrade);
			if (hardStop)
			{
				ExitPosition(longPos, c1, price);
				return;
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 4) return;

			var c1 = charts[i - 1];
			if (!CanEnter(symbol, c1.DateTime)) return;

			if (c1.Quote == null) return;
			if (c1.Cci == null) return;
			if (c1.Atr == null || c1.Atr <= 0) return;
			if (c1.VolumeSma == null) return;
			if (c1.IcLeadingSpan1 == null || c1.IcLeadingSpan2 == null) return;
			if (c1.IcConversion == null || c1.IcBase == null) return;

			var c2 = charts[i - 2];

			decimal atrRatio = c1.Atr.Value / c1.Quote.Close;
			if (atrRatio < MinAtrRatio) return;

			bool ichiBear = c1.IcLeadingSpan1 < c1.IcLeadingSpan2;
			if (!ichiBear) return;

			bool priceBelowCloud = c1.Quote.Close < c1.IcLeadingSpan1 || c1.Quote.Close < c1.IcLeadingSpan2;
			bool cciSpike = c1.Cci < -100m;
			bool cciRecovery = c1.Cci < c2.Cci && c2.Cci > 0m && c1.Cci < 0m;
			bool priceBelowShortSma = c1.Quote.Close < c1.Sma1;
			bool volOk = c1.VolumeSma > 0 && c1.Quote.Volume > c1.VolumeSma * VolumeMultiplier;

			if ((cciSpike || cciRecovery) && (priceBelowCloud || priceBelowShortSma) && volOk)
			{
				decimal sl = Math.Max(c1.Quote.Close * (1 + MaxLossPerTrade), c1.Quote.Close + c1.Atr.Value * SlAtrMultiplier);
				EntryPosition(PositionSide.Short, c1, c1.Quote.Close, sl);
				RecordEntry(symbol, c1.DateTime);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPos)
		{
			if (i < 2) return;

			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			if (c1.Quote == null) return;
			if (c1.Atr == null) return;

			int barsHeld = BarsSince(shortPos.Time, c1.DateTime, charts[1].DateTime - charts[0].DateTime);
			if (barsHeld >= MaxHoldBars)
			{
				ExitPosition(shortPos, c1, c1.Quote.Close);
				return;
			}

			var price = c1.Quote.Close;

			if (price <= shortPos.EntryPrice * (1 - Tp1) && shortPos.Stage == 0)
			{
				TakeProfitHalf(shortPos, price);
				return;
			}
			if (price <= shortPos.EntryPrice * (1 - Tp2))
			{
				TakeProfitHalf2(shortPos, c1);
				return;
			}

			bool cciRev = c1.Cci > -50m && c1.Cci > c2.Cci;
			bool priceAbove = price > c1.Sma1;
			bool tBreak = price > shortPos.EntryPrice + (c1.Atr.Value * TrailAtrMultiplier);

			if (cciRev || priceAbove || tBreak)
			{
				ExitPosition(shortPos, c1, price);
				return;
			}

			bool hardStop = price >= shortPos.EntryPrice * (1 + MaxLossPerTrade);
			if (hardStop)
			{
				ExitPosition(shortPos, c1, price);
				return;
			}
		}
	}
}
