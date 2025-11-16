using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// long entry
	/// CCI ++ 진입레벨 && 구름대위 && 일목전환점 > 일목기준점
	/// long exit
	/// CCI -- 청산레벨 || 일목후행스팬 < 종가 || 구름대안
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class Ci05(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals)
		: Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
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
			if (i < 2) return;

			var c2 = charts[i - 2];
			var c1 = charts[i - 1];
			var c0 = charts[i];

			bool cciCrossUp = c2.Cci < EntryLevel && c1.Cci >= EntryLevel;
			bool priceAboveCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Above;
			bool conversionAboveBase = c1.IcConversion > c1.IcBase;

			// 진입 조건: 단기 모멘텀 상승 + 구조적 강세(구름 상방) + 기준선 확인
			if (cciCrossUp && priceAboveCloud && conversionAboveBase)
			{
				DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c2 = charts[i - 2];
			var c1 = charts[i - 1];
			var c0 = charts[i];

			bool cciCrossDown = c2.Cci > ExitLevel && c1.Cci <= ExitLevel;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;
			bool chikouBelowPrice = c1.IcTrailingSpan.HasValue && c1.IcTrailingSpan <= c1.Quote.Close;

			// 청산 조건: 에너지 소진(Cci 하향돌파) 또는 구조적 약세(후행스팬 약세/구름 재진입)
			if (cciCrossDown || chikouBelowPrice || priceReenterCloud)
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

			bool cciCrossDown = c2.Cci > -EntryLevel && c1.Cci <= -EntryLevel;
			bool priceBelowCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Below;
			bool conversionBelowBase = c1.IcConversion < c1.IcBase;

			// 진입 조건: 단기 모멘텀 하락 + 구조적 약세(구름 하방) + 기준선 확인
			if (cciCrossDown && priceBelowCloud && conversionBelowBase)
			{
				DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, 1.0m, 0m);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c2 = charts[i - 2];
			var c1 = charts[i - 1];
			var c0 = charts[i];

			bool cciCrossUp = c2.Cci < -ExitLevel && c1.Cci >= -ExitLevel;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;
			bool chikouAbovePrice = c1.IcTrailingSpan.HasValue && c1.IcTrailingSpan >= c1.Quote.Close;

			// 청산 조건: 단기 반등 + 후행스팬 강세 + 구름 재진입
			if (cciCrossUp || chikouAbovePrice || priceReenterCloud)
			{
				DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
			}
		}
	}

	public class Ci05_2(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals)
		: Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 14;
		public int IchimokuTenkan = 9;
		public int IchimokuKijun = 26;
		public int IchimokuSenkou = 52;

		public decimal EntryLevel = 0m;
		public decimal BaseExitLevel = 150m;
		public int MaxHoldingBars = 20;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = false;
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkan, IchimokuKijun, IchimokuSenkou);
		}

		private decimal GetAdaptiveExitLevel(List<ChartInfo> charts, int i)
		{
			if (i < 2) return BaseExitLevel;

			var recentCci = charts.Skip(Math.Max(0, i - MaxHoldingBars)).Take(MaxHoldingBars).Select(c => c.Cci).ToList();
			var cciRange = recentCci.Max() - recentCci.Min();

			// CCI 변동폭이 클수록 ExitLevel 높임, 작으면 낮춤
			decimal adaptiveExit = BaseExitLevel + (cciRange.Value / 2m);
			return adaptiveExit;
		}

		private decimal GetAdaptiveLeverage(List<ChartInfo> charts, int i)
		{
			if (i < 2) return Leverage;

			var recentClose = charts.Skip(Math.Max(0, i - MaxHoldingBars)).Take(MaxHoldingBars).Select(c => c.Quote.Close).ToList();
			var volatility = (recentClose.Max() - recentClose.Min()) / recentClose.Average();

			// 변동성 높으면 레버리지 낮춤, 낮으면 레버리지 높임
			decimal adaptiveLev = Leverage * (1.0m - Math.Min(volatility * 2.0m, 0.7m));
			return Math.Max(1m, adaptiveLev);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;

			var c2 = charts[i - 2];
			var c1 = charts[i - 1];
			var c0 = charts[i];

			bool cciCrossUp = c2.Cci < EntryLevel && c1.Cci >= EntryLevel;
			bool priceAboveCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Above;
			bool conversionAboveBase = c1.IcConversion > c1.IcBase;

			if (cciCrossUp && priceAboveCloud && conversionAboveBase)
			{
				var adaptiveLev = GetAdaptiveLeverage(charts, i);
				DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, adaptiveLev, 0m);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c2 = charts[i - 2];
			var c1 = charts[i - 1];
			var c0 = charts[i];

			decimal adaptiveExit = GetAdaptiveExitLevel(charts, i);

			bool cciCrossDown = c2.Cci > adaptiveExit && c1.Cci <= adaptiveExit;
			bool chikouBelowPrice = c1.IcTrailingSpan.HasValue && c1.IcTrailingSpan <= c1.Quote.Close;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;

			if (cciCrossDown || chikouBelowPrice || priceReenterCloud)
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

			bool cciCrossDown = c2.Cci > -EntryLevel && c1.Cci <= -EntryLevel;
			bool priceBelowCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Below;
			bool conversionBelowBase = c1.IcConversion < c1.IcBase;

			if (cciCrossDown && priceBelowCloud && conversionBelowBase)
			{
				var adaptiveLev = GetAdaptiveLeverage(charts, i);
				DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, adaptiveLev, 0m);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c2 = charts[i - 2];
			var c1 = charts[i - 1];
			var c0 = charts[i];

			decimal adaptiveExit = GetAdaptiveExitLevel(charts, i);

			bool cciCrossUp = c2.Cci < -adaptiveExit && c1.Cci >= -adaptiveExit;
			bool chikouAbovePrice = c1.IcTrailingSpan.HasValue && c1.IcTrailingSpan >= c1.Quote.Close;
			bool priceReenterCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Inside;

			if (cciCrossUp || chikouAbovePrice || priceReenterCloud)
			{
				DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
			}
		}
	}
}
