using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

using System;

namespace Mercury.Backtests.BacktestStrategies
{
	public class Ci27 : Backtester
	{
		// 생성자: Backtester 기본 생성자 호출
		public Ci27(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals)
			: base(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
		{
		}

		// --- 전략 파라미터 (외부에서 수정 가능) ---
		public int CciPeriod = 20;
		public int IchimokuTenkanPeriod = 7;
		public int IchimokuKijunPeriod = 22;
		public int IchimokuSenkouBPeriod = 44;

		public int AtrPeriod = 14;
		public decimal AtrMultiplierStop = 2.5m;   // 초기 손절 배수 (ATR * X)
		public decimal FirstTakeProfitAtr = 1.0m;  // 1차 익절 목표: 엔트리 기준 ATR * 1
		public decimal SecondTakeProfitAtr = 2.0m; // 2차 익절 목표: 엔트리 기준 ATR * 2

		public int RsiPeriod = 14;
		public decimal RsiFilter = 40m; // Long시 RSI > 40 요구 (추세 보조)
		public decimal MaxPositionRiskPercent = 1.0m; // 각 포지션 위험(계좌 대비 %) - 프레임워크에서 적용될 수 있음

		// 재진입 쿨다운(캔들 수) - 손절 후 즉시 재진입 방지 (옵션)
		public int ReentryCooldownCandles = 6;

		// 내부 상태: 최근 청산 인덱스 추적 (symbol 당 관리가 필요하면 Position에 메타데이터 사용)
		private Dictionary<string, int> lastExitIndexBySymbol = new Dictionary<string, int>();

		// --- 인디케이터 초기화 ---
		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkanPeriod, IchimokuKijunPeriod, IchimokuSenkouBPeriod);
			chartPack.UseAtr(AtrPeriod);
			chartPack.UseRsi(RsiPeriod);
		}

		// --- Long 진입 로직 ---
		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return; // 안전 체크: 최소 3캔들 필요
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// 재진입 쿨다운 체크 (symbol 기반)
			if (lastExitIndexBySymbol.ContainsKey(symbol))
			{
				int lastExitIndex = lastExitIndexBySymbol[symbol];
				if (i - lastExitIndex < ReentryCooldownCandles) return;
			}

			// 기본 진입 조건:
			// 1) CCI가 과매도(-100)에서 반전(하방->상방)
			// 2) 가격이 Ichimoku 구름 위에 있고(추세 필터)
			// 3) RSI가 추세 보조(> RsiFilter)
			// 4) 전환선(Conversion) 위이면 추가 필터(추세 확인)
			bool cciReversal = (c2.Cci < -100m && c1.Cci > -100m);
			bool priceAboveCloud = (c1.Quote.Close > c1.IcLeadingSpan1 && c1.Quote.Close > c1.IcLeadingSpan2);
			bool rsiOk = (c1.Rsi1 >= RsiFilter);
			bool aboveConversion = (c1.Quote.Close > c1.IcConversion);

			if (cciReversal && priceAboveCloud && rsiOk && aboveConversion)
			{
				// 엔트리 가격: 다음 캔들의 시가(c0.Quote.Open)
				var entry = c0.Quote.Open;

				// ATR 기반 손절: 현재 종가 - ATR * AtrMultiplierStop
				decimal atr = Math.Max(0.00000001m, (decimal)c1.Atr); // 안전 방어
				decimal stopLoss = entry - atr * AtrMultiplierStop;
				if (stopLoss <= 0) stopLoss = entry * 0.95m; // 안전한 하한 적용

				// 포지션 사이즈는 프레임워크 EntryPosition에서 내부 관리될 수 있음.
				// EntryPosition(PositionSide.Long, ChartInfo, entryPrice, stopLoss: decimal?, takeProfit: decimal?...)
				EntryPosition(PositionSide.Long, c0, entry, stopLoss);
			}
		}

		// --- Long 청산 로직 ---
		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			if (i < 2 || longPosition == null) return;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// 1) 절반 익절: 첫 목표 (엔트리 기준 ATR * FirstTakeProfitAtr) 도달 시
			// 프레임워크에서 엔트리 가격을 longPosition.EntryPrice로 제공한다고 가정
			decimal atr = Math.Max(0.00000001m, (decimal)c1.Atr);
			decimal entryPrice = longPosition.EntryPrice;
			decimal firstTarget = entryPrice + atr * FirstTakeProfitAtr;
			decimal secondTarget = entryPrice + atr * SecondTakeProfitAtr;

			// Stage 0: 초기 (아직 첫 익절 전)
			if (longPosition.Stage == 0)
			{
				if (c1.Quote.Close >= firstTarget)
				{
					// 절반 익절
					TakeProfitHalf(longPosition, firstTarget);
					return;
				}
			}
			// Stage 1: 첫 익절 후 (나머지 포지션)
			else if (longPosition.Stage == 1)
			{
				// CCI의 하락 반전 신호(수익 보호)
				if (c1.Cci < c2.Cci)
				{
					TakeProfitHalf2(longPosition, c1);
					return;
				}

				// 또는 2차 목표 도달 시 전체 청산
				if (c1.Quote.Close >= secondTarget)
				{
					TakeProfitHalf2(longPosition, c1);
					return;
				}
			}

			// 손절: 가격이 Ichimoku 구름 아래로 완전 이탈하면 손절
			bool priceBelowCloud = (c1.Quote.Close < c1.IcLeadingSpan1 && c1.Quote.Close < c1.IcLeadingSpan2);
			if (priceBelowCloud)
			{
				// 청산 시점 기록(쿨다운용)
				lastExitIndexBySymbol[symbol] = i;
				ExitPosition(longPosition, c1, c1.Quote.Close);
				return;
			}

			// 추가 안전장치: 만약 포지션이 큰 손실 영역에 빠지면 강제 청산
			// (예: 엔트리 대비 - (ATR * 6) 이상 손실)
			decimal worstStop = entryPrice - atr * (AtrMultiplierStop * 3m);
			if (c1.Quote.Close <= worstStop)
			{
				lastExitIndexBySymbol[symbol] = i;
				ExitPosition(longPosition, c1, c1.Quote.Close);
				return;
			}
		}

		// --- Short 진입 로직 ---
		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// 재진입 쿨다운 체크
			if (lastExitIndexBySymbol.ContainsKey(symbol))
			{
				int lastExitIndex = lastExitIndexBySymbol[symbol];
				if (i - lastExitIndex < ReentryCooldownCandles) return;
			}

			// 기본 진입 조건 (숏):
			// 1) CCI가 과매수(>100)에서 하락 반전
			// 2) 가격이 Ichimoku 구름 아래에 있음 (하락 추세)
			// 3) RSI가 과열(추세 보조): RSI < (100 - RsiFilter) 를 활용할 수 있으나 여기서는 RSI < 60 정도로 완화
			bool cciReversal = (c2.Cci > 100m && c1.Cci < 100m);
			bool priceBelowCloud = (c1.Quote.Close < c1.IcLeadingSpan1 && c1.Quote.Close < c1.IcLeadingSpan2);
			bool rsiOk = (c1.Rsi1 <= 60m); // 완화된 필터
			bool belowConversion = (c1.Quote.Close < c1.IcConversion);

			if (cciReversal && priceBelowCloud && rsiOk && belowConversion)
			{
				var entry = c0.Quote.Open;
				decimal atr = Math.Max(0.00000001m, (decimal)c1.Atr);
				decimal stopLoss = entry + atr * AtrMultiplierStop;
				EntryPosition(PositionSide.Short, c0, entry, stopLoss);
			}
		}

		// --- Short 청산 로직 ---
		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			if (i < 2 || shortPosition == null) return;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			decimal atr = Math.Max(0.00000001m, (decimal)c1.Atr);
			decimal entryPrice = shortPosition.EntryPrice;
			decimal firstTarget = entryPrice - atr * FirstTakeProfitAtr;
			decimal secondTarget = entryPrice - atr * SecondTakeProfitAtr;

			if (shortPosition.Stage == 0)
			{
				if (c1.Quote.Close <= firstTarget)
				{
					TakeProfitHalf(shortPosition, firstTarget);
					return;
				}
			}
			else if (shortPosition.Stage == 1)
			{
				if (c1.Cci > c2.Cci)
				{
					TakeProfitHalf2(shortPosition, c1);
					return;
				}
				if (c1.Quote.Close <= secondTarget)
				{
					TakeProfitHalf2(shortPosition, c1);
					return;
				}
			}

			// 손절: 가격이 구름 위로 완전히 돌파하면 손절
			bool priceAboveCloud = (c1.Quote.Close > c1.IcLeadingSpan1 && c1.Quote.Close > c1.IcLeadingSpan2);
			if (priceAboveCloud)
			{
				lastExitIndexBySymbol[symbol] = i;
				ExitPosition(shortPosition, c1, c1.Quote.Close);
				return;
			}

			// 안전 강제 청산: 손실이 과도하면 강제 종료
			decimal worstStop = entryPrice + atr * (AtrMultiplierStop * 3m);
			if (c1.Quote.Close >= worstStop)
			{
				lastExitIndexBySymbol[symbol] = i;
				ExitPosition(shortPosition, c1, c1.Quote.Close);
				return;
			}
		}
	}
}
