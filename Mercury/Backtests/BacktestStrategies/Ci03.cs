using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	public class Ci03(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 14;
		public int IchimokuTenkan = 9;
		public int IchimokuKijun = 26;
		public int IchimokuSenkou = 52;

		public decimal EntryLevel = -100m;
		public decimal ExitLevel = 50m;
		public decimal CciGradientThreshold = 20m;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = false;
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkan, IchimokuKijun, IchimokuSenkou);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;

			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c0 = charts[i];

			bool cciBelowAndRebound = c2.Cci <= EntryLevel && c1.Cci > c2.Cci;
			var cloudBottom = Math.Min(c1.IcLeadingSpan1.Value, c1.IcLeadingSpan2.Value);
			bool pricePenetratedCloudBottom = c2.Quote.Close <= cloudBottom && c1.Quote.Close > cloudBottom;
			var cciGradient = c1.Cci - c2.Cci;
			bool gradientStrong = cciGradient >= CciGradientThreshold;

			if (cciBelowAndRebound && pricePenetratedCloudBottom && gradientStrong)
			{
				DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c0 = charts[i];

			bool cciTurnDown = c2.Cci > ExitLevel && c1.Cci < c2.Cci;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;

			if (cciTurnDown || priceReenterCloud)
			{
				DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;

			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c0 = charts[i];

			bool cciAboveAndReboundDown = c2.Cci >= -EntryLevel && c1.Cci < c2.Cci;
			var cloudTop = Math.Max(c1.IcLeadingSpan1.Value, c1.IcLeadingSpan2.Value);
			bool pricePenetratedCloudTop = c2.Quote.Close >= cloudTop && c1.Quote.Close < cloudTop;
			var cciGradient = Math.Abs(c1.Cci.Value - c2.Cci.Value);
			bool gradientStrong = cciGradient >= CciGradientThreshold;

			if (cciAboveAndReboundDown && pricePenetratedCloudTop && gradientStrong)
			{
				DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c0 = charts[i];

			bool cciTurnUp = c2.Cci < -ExitLevel && c1.Cci > c2.Cci;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;

			if (cciTurnUp || priceReenterCloud)
			{
				DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
			}
		}
	}

	public class Ci04(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 14;
		public int IchimokuTenkan = 9;
		public int IchimokuKijun = 26;
		public int IchimokuSenkou = 52;

		public decimal CciNearZeroThreshold = 10m;
		public decimal BreakoutBufferTicks = 0m;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = false;
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkan, IchimokuKijun, IchimokuSenkou);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;

			var c2 = charts[i - 2];
			var c1 = charts[i - 1];
			var c0 = charts[i];

			bool cciNearZero = Math.Abs(c1.Cci.Value) <= CciNearZeroThreshold;
			bool priceInsideCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;
			var cloudTop = Math.Max(c1.IcLeadingSpan1.Value, c1.IcLeadingSpan2.Value);

			bool breakoutUp = priceInsideCloud && c2.Quote.Close <= cloudTop && c1.Quote.Close > cloudTop + BreakoutBufferTicks;

			if (cciNearZero && breakoutUp)
			{
				DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c1 = charts[i - 1];
			var c0 = charts[i];

			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;
			bool cciTurnDown = c1.Cci < 0 && c1.Cci < charts[i - 2].Cci;

			if (priceReenterCloud || cciTurnDown)
			{
				DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;

			var c2 = charts[i - 2];
			var c1 = charts[i - 1];
			var c0 = charts[i];

			bool cciNearZero = Math.Abs(c1.Cci.Value) <= CciNearZeroThreshold;
			bool priceInsideCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;
			var cloudBottom = Math.Min(c1.IcLeadingSpan1.Value, c1.IcLeadingSpan2.Value);

			bool breakoutDown = priceInsideCloud && c2.Quote.Close >= cloudBottom && c1.Quote.Close < cloudBottom - BreakoutBufferTicks;

			if (cciNearZero && breakoutDown)
			{
				DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c1 = charts[i - 1];
			var c0 = charts[i];

			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;
			bool cciTurnUp = c1.Cci > 0 && c1.Cci > charts[i - 2].Cci;

			if (priceReenterCloud || cciTurnUp)
			{
				DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
			}
		}
	}

	public class Ci06(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 14;
		public int IchimokuTenkan = 9;
		public int IchimokuKijun = 26;
		public int IchimokuSenkou = 52;

		public decimal EntryLevel = 0m;
		public decimal ExitLevel = 150m;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = false;
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkan, IchimokuKijun, IchimokuSenkou);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 3) return;

			var c2 = charts[i - 2];
			var c1 = charts[i - 1];
			var c0 = charts[i];

			bool tenkanCrossedUp = c2.IcConversion <= c2.IcBase && c1.IcConversion > c1.IcBase;
			bool cciRisingOrFlat = c1.Cci >= c2.Cci;
			var cloudBottom = Math.Min(c1.IcLeadingSpan1.Value, c1.IcLeadingSpan2.Value);
			bool nearCloudBottom = c1.Quote.Close <= cloudBottom * 1.02m;

			if (cciRisingOrFlat && tenkanCrossedUp && nearCloudBottom)
			{
				DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 3) return;

			var c2 = charts[i - 2];
			var c1 = charts[i - 1];
			var c0 = charts[i];

			bool tenkanCrossedDown = c2.IcConversion >= c2.IcBase && c1.IcConversion < c1.IcBase;
			bool cciFallingOrFlat = c1.Cci <= c2.Cci;
			var cloudTop = Math.Max(c1.IcLeadingSpan1.Value, c1.IcLeadingSpan2.Value);
			bool nearCloudTop = c1.Quote.Close >= cloudTop * 0.98m;

			if (cciFallingOrFlat && tenkanCrossedDown && nearCloudTop)
			{
				DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c1 = charts[i - 1];
			var c0 = charts[i];

			bool tenkanCrossDown = charts[i - 2].IcConversion >= charts[i - 2].IcBase && c1.IcConversion < c1.IcBase;
			bool cciTurnDown = charts[i - 2].Cci > c1.Cci;

			if (tenkanCrossDown || cciTurnDown || c1.GetIchimokuCloudPosition() != IchimokuCloudPosition.Above)
			{
				DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c1 = charts[i - 1];
			var c0 = charts[i];

			bool tenkanCrossUp = charts[i - 2].IcConversion <= charts[i - 2].IcBase && c1.IcConversion > c1.IcBase;
			bool cciTurnUp = charts[i - 2].Cci < c1.Cci;

			if (tenkanCrossUp || cciTurnUp || c1.GetIchimokuCloudPosition() != IchimokuCloudPosition.Below)
			{
				DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
			}
		}
	}


	public class Ci07(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 14;
		public int IchimokuTenkan = 9;
		public int IchimokuKijun = 26;
		public int IchimokuSenkou = 52;
		public int HigherTimeframeFactor = 4;
		public decimal EntryLevel = 0m;
		public decimal ExitLevel = 150m;
		public decimal HigherTimeframeWeight = 1.5m;
		public decimal BaseWeight = 1.0m;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = false;
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkan, IchimokuKijun, IchimokuSenkou);
		}

		private (decimal? conv, decimal? @base, decimal? lead1, decimal? lead2, decimal? close) BuildHigherTimeframeIchimoku(List<ChartInfo> charts, int factor)
		{
			if (charts == null || charts.Count < factor) return (null, null, null, null, null);
			int groups = charts.Count / factor;
			if (groups < Math.Max(IchimokuTenkan, Math.Max(IchimokuKijun, IchimokuSenkou))) return (null, null, null, null, null);

			var highAgg = new decimal[groups];
			var lowAgg = new decimal[groups];
			var closeAgg = new decimal[groups];

			for (int g = 0; g < groups; g++)
			{
				int start = g * factor;
				int end = start + factor - 1;
				decimal h = decimal.MinValue;
				decimal l = decimal.MaxValue;
				for (int k = start; k <= end; k++)
				{
					var q = charts[k].Quote;
					if (q.High > h) h = q.High;
					if (q.Low < l) l = q.Low;
				}
				highAgg[g] = h;
				lowAgg[g] = l;
				closeAgg[g] = charts[end].Quote.Close;
			}

			int last = groups - 1;

			decimal conv = (Enumerable.Range(Math.Max(0, last - IchimokuTenkan + 1), IchimokuTenkan).Select(idx => highAgg[idx]).Max()
						  + Enumerable.Range(Math.Max(0, last - IchimokuTenkan + 1), IchimokuTenkan).Select(idx => lowAgg[idx]).Min()) / 2m;

			decimal @base = (Enumerable.Range(Math.Max(0, last - IchimokuKijun + 1), IchimokuKijun).Select(idx => highAgg[idx]).Max()
						  + Enumerable.Range(Math.Max(0, last - IchimokuKijun + 1), IchimokuKijun).Select(idx => lowAgg[idx]).Min()) / 2m;

			decimal lead1 = (conv + @base) / 2m;
			decimal lead2 = (Enumerable.Range(Math.Max(0, last - IchimokuSenkou + 1), IchimokuSenkou).Select(idx => highAgg[idx]).Max()
							+ Enumerable.Range(Math.Max(0, last - IchimokuSenkou + 1), IchimokuSenkou).Select(idx => lowAgg[idx]).Min()) / 2m;

			return (conv, @base, lead1, lead2, closeAgg[last]);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciCrossUp = c2.Cci < EntryLevel && c1.Cci >= EntryLevel;
			bool tenkanAboveKijun = c1.IcConversion > c1.IcBase;
			bool priceAboveCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Above;

			var ht = BuildHigherTimeframeIchimoku(charts.Take(i).ToList(), HigherTimeframeFactor);
			bool higherSupports = false;
			if (ht.conv.HasValue && ht.@base.HasValue && ht.lead1.HasValue && ht.lead2.HasValue && ht.close.HasValue)
			{
				var cloudTop = Math.Max(ht.lead1.Value, ht.lead2.Value);
				higherSupports = ht.close.Value > cloudTop && ht.conv.Value > ht.@base.Value;
			}

			decimal weight = higherSupports ? HigherTimeframeWeight : BaseWeight;

			if (cciCrossUp && tenkanAboveKijun && priceAboveCloud)
			{
				DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, weight, 0m);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciTurnDown = c2.Cci > ExitLevel && c1.Cci < c2.Cci;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;

			if (cciTurnDown || priceReenterCloud)
			{
				DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciCrossDown = c2.Cci > -EntryLevel && c1.Cci <= -EntryLevel;
			bool tenkanBelowKijun = c1.IcConversion < c1.IcBase;
			bool priceBelowCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Below;

			var ht = BuildHigherTimeframeIchimoku(charts.Take(i).ToList(), HigherTimeframeFactor);
			bool higherSupports = false;
			if (ht.conv.HasValue && ht.@base.HasValue && ht.lead1.HasValue && ht.lead2.HasValue && ht.close.HasValue)
			{
				var cloudBottom = Math.Min(ht.lead1.Value, ht.lead2.Value);
				higherSupports = ht.close.Value < cloudBottom && ht.conv.Value < ht.@base.Value;
			}

			decimal weight = higherSupports ? HigherTimeframeWeight : BaseWeight;

			if (cciCrossDown && tenkanBelowKijun && priceBelowCloud)
			{
				DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, weight, 0m);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciTurnUp = c2.Cci < -ExitLevel && c1.Cci > c2.Cci;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;

			if (cciTurnUp || priceReenterCloud)
			{
				DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
			}
		}
	}

	public class Ci08(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 14;
		public int IchimokuTenkan = 9;
		public int IchimokuKijun = 26;
		public int IchimokuSenkou = 52;

		public decimal EntryLevel = 0m;
		public decimal ExitLevel = 150m;

		public decimal CciGradientThreshold = 30m;
		public decimal GradientDiffThreshold = 20m;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = false;
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkan, IchimokuKijun, IchimokuSenkou);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 3) return;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciCrossUp = c2.Cci < EntryLevel && c1.Cci >= EntryLevel;
			bool tenkanAboveKijun = c1.IcConversion > c1.IcBase;

			decimal cciGradient = c1.Cci.Value - c2.Cci.Value;
			decimal tenkanGradient = c1.IcConversion.Value - c2.IcConversion.Value;
			decimal diff = Math.Abs(cciGradient - tenkanGradient);

			if (cciCrossUp && tenkanAboveKijun && cciGradient >= CciGradientThreshold && diff < GradientDiffThreshold)
			{
				DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			decimal cciGradient = c1.Cci.Value - c2.Cci.Value;
			decimal tenkanGradient = c1.IcConversion.Value - c2.IcConversion.Value;
			decimal diff = Math.Abs(cciGradient - tenkanGradient);

			bool momentumFatigue = cciGradient < 0 || diff >= GradientDiffThreshold;
			bool cciTurnDown = c2.Cci > ExitLevel && c1.Cci < c2.Cci;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;

			if (momentumFatigue || cciTurnDown || priceReenterCloud)
			{
				DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 3) return;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciCrossDown = c2.Cci > -EntryLevel && c1.Cci <= -EntryLevel;
			bool tenkanBelowKijun = c1.IcConversion < c1.IcBase;

			decimal cciGradient = c1.Cci.Value - c2.Cci.Value;
			decimal tenkanGradient = c1.IcConversion.Value - c2.IcConversion.Value;
			decimal diff = Math.Abs(cciGradient - tenkanGradient);

			if (cciCrossDown && tenkanBelowKijun && cciGradient <= -CciGradientThreshold && diff < GradientDiffThreshold)
			{
				DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			decimal cciGradient = c1.Cci.Value - c2.Cci.Value;
			decimal tenkanGradient = c1.IcConversion.Value - c2.IcConversion.Value;
			decimal diff = Math.Abs(cciGradient - tenkanGradient);

			bool momentumFatigue = cciGradient > 0 || diff >= GradientDiffThreshold;
			bool cciTurnUp = c2.Cci < -ExitLevel && c1.Cci > c2.Cci;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;

			if (momentumFatigue || cciTurnUp || priceReenterCloud)
			{
				DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
			}
		}
	}

	public class Ci09(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 14;
		public int IchimokuTenkan = 9;
		public int IchimokuKijun = 26;
		public int IchimokuSenkou = 52;

		public decimal EntryLevel = 0m;
		public decimal ExitLevel = 150m;

		public decimal CloudThicknessThreshold = 5m;
		public decimal CciAmplitudeThreshold = 100m;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = false;
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkan, IchimokuKijun, IchimokuSenkou);
		}

		private decimal CloudThickness(ChartInfo c) => Math.Abs(c.IcLeadingSpan1.Value - c.IcLeadingSpan2.Value);

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciCrossUp = c2.Cci < EntryLevel && c1.Cci >= EntryLevel;
			bool tenkanAboveKijun = c1.IcConversion > c1.IcBase;
			bool priceAboveCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Above;

			decimal thickness = CloudThickness(c1);
			decimal amplitude = Math.Abs(c1.Cci.Value - c2.Cci.Value);
			bool elastic = thickness >= CloudThicknessThreshold && amplitude <= CciAmplitudeThreshold;

			if (cciCrossUp && tenkanAboveKijun && priceAboveCloud)
			{
				DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciTurnDown = c2.Cci > ExitLevel && c1.Cci < c2.Cci;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;

			decimal thickness = CloudThickness(c1);
			decimal amplitude = Math.Abs(c1.Cci.Value - c2.Cci.Value);
			bool elastic = thickness >= CloudThicknessThreshold && amplitude <= CciAmplitudeThreshold;

			if ((cciTurnDown && !elastic) || priceReenterCloud)
			{
				DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciCrossDown = c2.Cci > -EntryLevel && c1.Cci <= -EntryLevel;
			bool tenkanBelowKijun = c1.IcConversion < c1.IcBase;
			bool priceBelowCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Below;

			decimal thickness = CloudThickness(c1);
			decimal amplitude = Math.Abs(c1.Cci.Value - c2.Cci.Value);
			bool elastic = thickness >= CloudThicknessThreshold && amplitude <= CciAmplitudeThreshold;

			if (cciCrossDown && tenkanBelowKijun && priceBelowCloud)
			{
				DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciTurnUp = c2.Cci < -ExitLevel && c1.Cci > c2.Cci;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;

			decimal thickness = CloudThickness(c1);
			decimal amplitude = Math.Abs(c1.Cci.Value - c2.Cci.Value);
			bool elastic = thickness >= CloudThicknessThreshold && amplitude <= CciAmplitudeThreshold;

			if ((cciTurnUp && !elastic) || priceReenterCloud)
			{
				DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
			}
		}
	}

	public class Ci10(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 14;
		public int IchimokuTenkan = 9;
		public int IchimokuKijun = 26;
		public int IchimokuSenkou = 52;

		public decimal Alpha = 1.0m;
		public decimal Beta = 1.0m;
		public decimal Gamma = 1.0m;
		public decimal EThreshold = 0.0m;

		public decimal EntryLevel = 0m;
		public decimal ExitLevel = 150m;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = false;
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkan, IchimokuKijun, IchimokuSenkou);
		}

		private decimal ComputeEnergy(ChartInfo c1, ChartInfo c2)
		{
			decimal cciPrime = c1.Cci.Value - c2.Cci.Value;
			decimal deltaKijun = c1.IcBase.Value - c2.IcBase.Value;
			decimal thickness1 = Math.Abs(c1.IcLeadingSpan1.Value - c1.IcLeadingSpan2.Value);
			decimal thickness2 = Math.Abs(c2.IcLeadingSpan1.Value - c2.IcLeadingSpan2.Value);
			decimal deltaThickness = thickness1 - thickness2;
			return Alpha * cciPrime + Beta * deltaKijun + Gamma * deltaThickness;
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciCrossUp = c2.Cci < EntryLevel && c1.Cci >= EntryLevel;
			bool tenkanAboveKijun = c1.IcConversion > c1.IcBase;
			bool priceAboveCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Above;

			decimal E = ComputeEnergy(c1, c2);

			if (cciCrossUp && tenkanAboveKijun && priceAboveCloud && E > EThreshold)
			{
				DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			decimal E = ComputeEnergy(c1, c2);

			bool negativeEnergy = E < -EThreshold;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;

			if (negativeEnergy || priceReenterCloud)
			{
				DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciCrossDown = c2.Cci > -EntryLevel && c1.Cci <= -EntryLevel;
			bool tenkanBelowKijun = c1.IcConversion < c1.IcBase;
			bool priceBelowCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Below;

			decimal E = ComputeEnergy(c1, c2);

			if (cciCrossDown && tenkanBelowKijun && priceBelowCloud && E < -EThreshold)
			{
				DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			decimal E = ComputeEnergy(c1, c2);

			bool positiveEnergy = E > EThreshold;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;

			if (positiveEnergy || priceReenterCloud)
			{
				DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
			}
		}
	}

	public class Ci11(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 14;
		public int IchimokuTenkan = 9;
		public int IchimokuKijun = 26;
		public int IchimokuSenkou = 52;

		public decimal EntryLevel = 0m;
		public decimal ExitLevel = 150m;

		public decimal FractalTolerance = 0.25m;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = false;
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkan, IchimokuKijun, IchimokuSenkou);
		}

		private List<int> RecentCciZeroCrossIndexes(List<ChartInfo> charts, int maxLookback)
		{
			var idxs = new List<int>();
			int start = Math.Max(1, charts.Count - maxLookback);
			for (int j = start; j < charts.Count; j++)
			{
				if ((charts[j - 1].Cci < 0 && charts[j].Cci >= 0) || (charts[j - 1].Cci > 0 && charts[j].Cci <= 0))
					idxs.Add(j);
			}
			return idxs;
		}

		private bool Fractal1_3_9(List<int> idxs)
		{
			if (idxs == null || idxs.Count < 4) return false;
			int a = idxs[idxs.Count - 3] - idxs[idxs.Count - 4];
			int b = idxs[idxs.Count - 2] - idxs[idxs.Count - 3];
			int c = idxs[idxs.Count - 1] - idxs[idxs.Count - 2];
			if (a <= 0 || b <= 0 || c <= 0) return false;
			decimal r1 = (decimal)b / a;
			decimal r2 = (decimal)c / b;
			return Math.Abs(r1 - 3m) / 3m <= FractalTolerance && Math.Abs(r2 - 3m) / 3m <= FractalTolerance;
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 3) return;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciCrossUp = c2.Cci < EntryLevel && c1.Cci >= EntryLevel;
			bool tenkanAboveKijun = c1.IcConversion > c1.IcBase;
			bool priceAboveCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Above;

			var idxs = RecentCciZeroCrossIndexes(charts.Take(i + 1).ToList(), 200);
			bool fractalPattern = Fractal1_3_9(idxs);

			if (cciCrossUp && tenkanAboveKijun && priceAboveCloud && fractalPattern)
			{
				DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, 2.0m, 0m);
			}
			else if (cciCrossUp && tenkanAboveKijun && priceAboveCloud)
			{
				DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciTurnDown = c2.Cci > ExitLevel && c1.Cci < c2.Cci;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;

			if (cciTurnDown || priceReenterCloud)
			{
				DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 3) return;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciCrossDown = c2.Cci > -EntryLevel && c1.Cci <= -EntryLevel;
			bool tenkanBelowKijun = c1.IcConversion < c1.IcBase;
			bool priceBelowCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Below;

			var idxs = RecentCciZeroCrossIndexes(charts.Take(i + 1).ToList(), 200);
			bool fractalPattern = Fractal1_3_9(idxs);

			if (cciCrossDown && tenkanBelowKijun && priceBelowCloud && fractalPattern)
			{
				DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, 2.0m, 0m);
			}
			else if (cciCrossDown && tenkanBelowKijun && priceBelowCloud)
			{
				DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciTurnUp = c2.Cci < -ExitLevel && c1.Cci > c2.Cci;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;

			if (cciTurnUp || priceReenterCloud)
			{
				DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
			}
		}
	}

	public class Ci12(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 14;
		public int IchimokuTenkan = 9;
		public int IchimokuKijun = 26;
		public int IchimokuSenkou = 52;

		public decimal EntryLevel = 0m;
		public decimal ExitLevel = 150m;

		public int BiasLength = 20;
		public decimal SymmetryThreshold = 50m;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = false;
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkan, IchimokuKijun, IchimokuSenkou);
		}

		private decimal CciBias(List<ChartInfo> charts, int i, int length)
		{
			int start = Math.Max(0, i - length + 1);
			var slice = charts.Skip(start).Take(i - start + 1).Select(c => c.Cci);
			if (!slice.Any()) return 0m;
			return slice.Average().Value;
		}

		private decimal CloudSlope(List<ChartInfo> charts, int i, int length)
		{
			int start = Math.Max(0, i - length + 1);
			int end = i;
			if (end - start < 1) return 0m;
			decimal first = charts[start].IcLeadingSpan1.Value - charts[start].IcLeadingSpan2.Value;
			decimal last = charts[end].IcLeadingSpan1.Value - charts[end].IcLeadingSpan2.Value;
			return (last - first) / (end - start + 1);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < BiasLength) return;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciCrossUp = c2.Cci < EntryLevel && c1.Cci >= EntryLevel;
			bool tenkanAboveKijun = c1.IcConversion > c1.IcBase;
			bool priceAboveCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Above;

			decimal bias = CciBias(charts.Take(i + 1).ToList(), i - 1, BiasLength);
			decimal slope = CloudSlope(charts.Take(i + 1).ToList(), i - 1, BiasLength);

			bool symmetryBroken = Math.Abs(bias) - Math.Abs(slope) > SymmetryThreshold;

			if (cciCrossUp && tenkanAboveKijun && priceAboveCloud && !symmetryBroken)
			{
				DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			decimal bias = CciBias(charts.Take(i + 1).ToList(), i - 1, BiasLength);
			decimal slope = CloudSlope(charts.Take(i + 1).ToList(), i - 1, BiasLength);
			bool symmetryBroken = Math.Abs(bias) - Math.Abs(slope) > SymmetryThreshold;

			bool cciTurnDown = charts[i - 2].Cci > charts[i - 1].Cci && charts[i - 2].Cci > ExitLevel;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;

			if (symmetryBroken || cciTurnDown || priceReenterCloud)
			{
				DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < BiasLength) return;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciCrossDown = c2.Cci > -EntryLevel && c1.Cci <= -EntryLevel;
			bool tenkanBelowKijun = c1.IcConversion < c1.IcBase;
			bool priceBelowCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Below;

			decimal bias = CciBias(charts.Take(i + 1).ToList(), i - 1, BiasLength);
			decimal slope = CloudSlope(charts.Take(i + 1).ToList(), i - 1, BiasLength);
			bool symmetryBroken = Math.Abs(bias) - Math.Abs(slope) > SymmetryThreshold;

			if (cciCrossDown && tenkanBelowKijun && priceBelowCloud && !symmetryBroken)
			{
				DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			decimal bias = CciBias(charts.Take(i + 1).ToList(), i - 1, BiasLength);
			decimal slope = CloudSlope(charts.Take(i + 1).ToList(), i - 1, BiasLength);
			bool symmetryBroken = Math.Abs(bias) - Math.Abs(slope) > SymmetryThreshold;

			bool cciTurnUp = charts[i - 2].Cci < charts[i - 1].Cci && charts[i - 2].Cci < -ExitLevel;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;

			if (symmetryBroken || cciTurnUp || priceReenterCloud)
			{
				DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
			}
		}
	}

}
