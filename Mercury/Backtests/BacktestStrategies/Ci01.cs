using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// CI01 - CCI + Ichimoku Phase Resonance Strategy
	///
	/// === 핵심 개요 ===
	/// CCI(단기 모멘텀)과 Ichimoku(구조적 균형)의 위상 일치 구간에서 진입하는 전략.
	///
	/// === 핵심 개념 ===
	/// - CCI의 0선 돌파는 단기 에너지 트리거.
	/// - Ichimoku 전환선(Tenkan)이 기준선(Kijun)을 상향 돌파하고,
	///   가격이 구름대 위에 있을 때 중기 추세와 공명(Phase Resonance).
	/// - CCI 둔화 또는 구름대 재진입 시 관성 붕괴로 간주하고 청산.
	///
	/// === 주요 파라미터 ===
	/// - CciPeriod: CCI 계산 기간
	/// - EntryLevel: CCI 0선 돌파 기준
	/// - ExitLevel: CCI 둔화 감지 기준
	/// - IchimokuPeriods: (Tenkan, Kijun, Senkou) 기간
	///
	/// === 전략 요약 ===
	/// - 진입: CCI가 0선 상향 돌파 + Tenkan>Kijun + 가격>CloudTop
	/// - 청산: CCI가 +150 이상에서 꺾이거나, 가격이 구름대 재진입
	///
	/// </summary>
	public class Ci01(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
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

			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			bool cciCrossUp = c2.Cci < EntryLevel && c1.Cci >= EntryLevel;
			bool tenkanAboveKijun = c1.IcConversion > c1.IcBase;
			bool priceAboveCloud = c1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Above;

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

			if (cciTurnUp || priceReenterCloud)
			{
				DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
			}
		}
	}

}
