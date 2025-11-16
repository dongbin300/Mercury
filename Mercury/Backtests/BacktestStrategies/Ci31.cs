using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Ci31 - Multi-Timeframe Strategy (4H Trend + 1H Entry)
	/// 주 지표: CCI (하위타임프레임)
	/// 부 지표: Ichimoku Cloud (상위타임프레임)
	/// 보조: ATR 기반 스탑로스
	/// </summary>
	public class Ci31 : Backtester
	{
		public Ci31(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals)
			: base(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
		{
		}

		// --- 파라미터 ---
		public int LowerCciPeriod = 26;              // 하위 TF용 CCI
		public int UpperIchimokuTenkan = 9;          // 상위 TF Ichimoku
		public int UpperIchimokuKijun = 26;
		public int UpperIchimokuSenkouB = 52;
		public int AtrPeriod = 14;                   // ATR for stoploss
		public decimal AtrMultiplierStop = 2.8m;     // ATR x 배수 손절
		public decimal FirstTakeProfitAtr = 1.2m;    // 1차 익절
		public decimal SecondTakeProfitAtr = 2.5m;   // 2차 익절
		public decimal RsiFilter = 45m;              // RSI 보조필터
		public int UpperTrendLookback = 2;           // 상위TF 확인 캔들 수

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			switch (intervalIndex)
			{
				case 0:
					chartPack.UseCci(LowerCciPeriod);
					chartPack.UseAtr(AtrPeriod);
					chartPack.UseRsi(14);
					break;

				case 1:
					chartPack.UseIchimokuCloud(UpperIchimokuTenkan, UpperIchimokuKijun, UpperIchimokuSenkouB);
					break;
			}
		}

		// ---- Long Entry ----
		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var charts2 = GetSubCharts(symbol, 1);
			var j = GetSubChartIndex(symbol, i, 1);
			var d0 = charts2[j];
			var d1 = charts2[j - 1];

			// 상위 추세 필터: 가격이 Ichimoku 구름 위 -> 상승 추세
			bool upperTrendUp = d1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Above;

			// 하위 타임프레임 조건:
			bool cciReversal = (c2.Cci < -100m && c1.Cci > -100m);
			bool rsiConfirm = (c1.Rsi1 >= RsiFilter);

			if (upperTrendUp && cciReversal && rsiConfirm)
			{
				decimal entry = c0.Quote.Open;
				decimal atr = (decimal)c1.Atr;
				decimal stopLoss = entry - atr * AtrMultiplierStop;
				EntryPosition(PositionSide.Long, c0, entry, stopLoss);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			if (i < 2) return;
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var charts2 = GetSubCharts(symbol, 1);
			var j = GetSubChartIndex(symbol, i, 1);
			var d0 = charts2[j];
			var d1 = charts2[j - 1];

			var entryPrice = longPosition.EntryPrice;
			decimal atr = (decimal)c1.Atr;

			decimal firstTarget = entryPrice + atr * FirstTakeProfitAtr;
			decimal secondTarget = entryPrice + atr * SecondTakeProfitAtr;

			if (longPosition.Stage == 0 && c1.Quote.Close >= firstTarget)
			{
				TakeProfitHalf(longPosition, c1.Quote.Close);
				return;
			}
			else if (longPosition.Stage == 1 && (c1.Cci < c2.Cci || c1.Quote.Close >= secondTarget))
			{
				TakeProfitHalf2(longPosition, c1);
				return;
			}

			bool upperTrendDown = d1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Below;
			if (upperTrendDown)
			{
				ExitPosition(longPosition, c1, c1.Quote.Close);
				return;
			}
		}

		// ---- Short Entry ----
		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var charts2 = GetSubCharts(symbol, 1);
			var j = GetSubChartIndex(symbol, i, 1);
			var d0 = charts2[j];
			var d1 = charts2[j - 1];

			// 상위 추세: 가격이 구름 아래 -> 하락 추세
			bool upperTrendDown = d1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Below;

			// 하위 조건: CCI 과매수 -> 하락 반전
			bool cciReversal = (c2.Cci > 100m && c1.Cci < 100m);
			bool rsiConfirm = (c1.Rsi1 <= 55m); // 숏 필터 (완화)

			if (upperTrendDown && cciReversal && rsiConfirm)
			{
				decimal entry = c0.Quote.Open;
				decimal atr = (decimal)c1.Atr;
				decimal stopLoss = entry + atr * AtrMultiplierStop;
				EntryPosition(PositionSide.Short, c0, entry, stopLoss);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			if (i < 2) return;
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var charts2 = GetSubCharts(symbol, 1);
			var j = GetSubChartIndex(symbol, i, 1);
			var d0 = charts2[j];
			var d1 = charts2[j - 1];

			var entryPrice = shortPosition.EntryPrice;
			decimal atr = (decimal)c1.Atr;

			decimal firstTarget = entryPrice - atr * FirstTakeProfitAtr;
			decimal secondTarget = entryPrice - atr * SecondTakeProfitAtr;

			if (shortPosition.Stage == 0 && c1.Quote.Close <= firstTarget)
			{
				TakeProfitHalf(shortPosition, c1.Quote.Close);
				return;
			}
			else if (shortPosition.Stage == 1 && (c1.Cci > c2.Cci || c1.Quote.Close <= secondTarget))
			{
				TakeProfitHalf2(shortPosition, c1);
				return;
			}

			bool upperTrendUp = d1.GetIchimokuCloudPosition() == IchimokuCloudPosition.Above;
			if (upperTrendUp)
			{
				ExitPosition(shortPosition, c1, c1.Quote.Close);
				return;
			}
		}
	}

}
