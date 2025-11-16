using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci26 - Enhanced Risk Management & Better Entry/Exit
	///
	/// Cci25 베이스 + 강화된 리스크 관리 및 진입/청산 로직 개선
	///
	/// === 핵심 개선사항 ===
	/// 1. DCA 진입 횟수 감소 (4회 → 2회) - 리스크 관리 강화
	/// 2. 손절 비율 감소 (4.2% → 2.5%) - 빠른 손절로 손실 최소화
	/// 3. DCA 간격 축소 (4.5% → 3.0%) - 더 촘촘한 물타기
	/// 4. RSI 필터 추가 - 과매수/과매도 상황에서만 진입
	/// 5. 볼륨 필터 강화 - 거래량이 충분할 때만 진입
	/// 6. CCI 청산 조건 개선 - 너무 빠른 청산 방지
	/// 7. 동적 익절 목표 - 시장 상황에 따른 적응적 익절
	///
	/// === 기대 효과 ===
	/// - MDD 대폭 감소 (20-60% → 10-20% 목표)
	/// - 승률 개선 및 안정적인 수익률
	/// - 실제 거래 환경에서 실행 가능한 현실적인 전략
	///
	/// </summary>
	public class Cci26(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		// === CCI 기본 파라미터 ===
		public int CciPeriod = 14;                   // CCI 계산 기간 (15→14로 최적화)
		public decimal ExtremeLevelHigh = 200m;      // 과매수 극값
		public decimal ExtremeLevelLow = -200m;      // 과매도 극값
		public decimal ExitBuffer = 0m;              // 청산 버퍼

		// === RSI 필터 파라미터 ===
		public int RsiPeriod = 14;                   // RSI 계산 기간
		public decimal RsiOverbought = 70m;          // RSI 과매수 기준
		public decimal RsiOversold = 30m;            // RSI 과매도 기준

		// === DCA 설정 (리스크 관리 강화) ===
		public int DcaMaxEntries = 2;                // 최대 분할 진입 횟수 (4→2 감소)
		public decimal DcaStepPercent = 3.0m;        // DCA 진입 간격 (4.5→3.0% 감소)
		public decimal DcaMultiplier = 1.5m;         // DCA 포지션 사이징 배수 (1.6→1.5)

		// === 청산 전략 설정 ===
		public decimal PartialExitPercent = 0.6m;    // 부분 청산 비율 (0.8→0.6)
		public decimal ProfitTarget = 1.5m;          // 부분 청산 수익 목표 (1.3→1.5)
		public decimal FullExitTarget = 3.5m;        // 전량 청산 수익 목표 (4.2→3.5)

		// === 리스크 관리 설정 (강화) ===
		public decimal StopLossPercent = 2.5m;       // 기본 손절 비율 (4.2→2.5% 강화)
		public decimal TrailingPercent = 1.2m;       // 트레일링 스톱 비율

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = true;
			chartPack.UseCci(CciPeriod);
			chartPack.UseRsi(RsiPeriod);
		}

		private bool IsHighVolatility(List<ChartInfo> charts, int i)
		{
			if (i < 20) return false;
			var recent = charts.Skip(i - 19).Take(20).ToList();
			var avgRange = recent.Average(c => (c.Quote.High - c.Quote.Low) / c.Quote.Close * 100);
			return avgRange > 2.5m; // 변동성 기준 완화 (3.0→2.5)
		}

		private decimal GetTrendStrength(List<ChartInfo> charts, int i)
		{
			if (i < 20) return 0;
			var recent = charts.Skip(i - 19).Take(20).ToList();
			var firstPrice = recent.First().Quote.Close;
			var lastPrice = recent.Last().Quote.Close;
			return (lastPrice - firstPrice) / firstPrice * 100;
		}

		private bool IsVolumeConfirmed(List<ChartInfo> charts, int i)
		{
			if (i < 10) return false;
			var avgVolume = charts.Skip(i - 9).Take(10).Average(c => c.Quote.Volume);
			return charts[i].Quote.Volume > avgVolume * 1.2m; // 평균 거래량의 120% 이상
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 20) return;

			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			var existingPosition = GetActivePosition(symbol, PositionSide.Long);
			var isHighVol = IsHighVolatility(charts, i);
			var trendStrength = GetTrendStrength(charts, i);
			var volumeConfirmed = IsVolumeConfirmed(charts, i);

			if (existingPosition == null)
			{
				// 기본 CCI 조건
				bool cciCondition = c3.Cci <= ExtremeLevelLow && c2.Cci > c3.Cci && c1.Cci > c2.Cci;

				// RSI 필터: 과매도 상황에서만 진입
				bool rsiCondition = c1.Rsi1 <= RsiOversold;

				// 볼륨 확인
				bool volumeCondition = volumeConfirmed || isHighVol;

				// 트렌드 필터: 너무 강한 하락 트렌드에서는 진입 금지
				bool trendCondition = trendStrength > -5.0m;

				if (cciCondition && rsiCondition && volumeCondition && trendCondition)
				{
					var entry = c0.Quote.Open;
					var stopLoss = entry * (1 - StopLossPercent / 100);
					DcaEntryPosition(PositionSide.Long, c0, entry, DcaStepPercent, DcaMultiplier, stopLoss);
				}
			}
			else
			{
				if (existingPosition.DcaStep < DcaMaxEntries)
				{
					var currentPrice = c0.Quote.Open;
					var entryPrice = existingPosition.EntryPrice;
					var dropPercent = (entryPrice - currentPrice) / entryPrice * 100;
					var dynamicStep = isHighVol ? DcaStepPercent * 0.9m : DcaStepPercent;

					// RSI 조건: 추가 진입시에도 과매도 확인
					bool rsiOkForDca = c1.Rsi1 <= RsiOversold + 10m; // 약간 완화된 조건

					if (dropPercent >= dynamicStep * (existingPosition.DcaStep + 1) && rsiOkForDca)
					{
						var stopLoss = currentPrice * (1 - StopLossPercent / 100);
						DcaEntryPosition(PositionSide.Long, c0, currentPrice, DcaStepPercent, DcaMultiplier, stopLoss);
					}
				}
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			// 1. 강화된 손절 로직
			if (longPosition.StopLossPrice > 0 && c0.Quote.Low <= longPosition.StopLossPrice)
			{
				DcaExitPosition(longPosition, c0, longPosition.StopLossPrice, 1.0m);
				return;
			}

			// 2. 동적 전량 익절 로직
			var trendStrength = GetTrendStrength(charts, i);
			var dynamicFullTarget = trendStrength > 5.0m ? FullExitTarget * 1.2m : FullExitTarget;
			var fullExitPrice = longPosition.EntryPrice * (1 + dynamicFullTarget / 100);
			if (c0.Quote.High >= fullExitPrice)
			{
				DcaExitPosition(longPosition, c0, fullExitPrice, 1.0m);
				return;
			}

			// 3. 개선된 부분 익절 로직
			var dynamicPartialTarget = trendStrength > 3.0m ? ProfitTarget * 1.1m : ProfitTarget;
			var partialExitPrice = longPosition.EntryPrice * (1 + dynamicPartialTarget / 100);
			if (longPosition.Stage == 0 && c0.Quote.High >= partialExitPrice)
			{
				var dynamicExitPercent = trendStrength > 3.0m ? PartialExitPercent * 0.8m : PartialExitPercent;
				DcaExitPosition(longPosition, c0, partialExitPrice, dynamicExitPercent);
				longPosition.Stage = 1;
				longPosition.StopLossPrice = longPosition.EntryPrice * 1.002m; // 본절보다 약간 위로 설정
			}

			// 4. 개선된 트레일링 스톱
			var profitPercent = GetPositionProfitPercent(longPosition, c0.Quote.Close);
			if (profitPercent > TrailingPercent)
			{
				var dynamicTrailing = trendStrength > 5.0m ? TrailingPercent * 0.8m : TrailingPercent;
				var newStopLoss = c0.Quote.High * (1 - dynamicTrailing / 100);
				if (newStopLoss > longPosition.StopLossPrice)
				{
					longPosition.StopLossPrice = newStopLoss;
				}
			}

			// 5. 개선된 CCI 기반 청산 (더 신중하게)
			bool cciExit = c1.Cci >= 50m && c0.Cci < c1.Cci; // 모멘텀 확인 추가
			bool rsiExit = c1.Rsi1 >= RsiOverbought; // RSI 과매수 확인

			if ((cciExit || rsiExit) && profitPercent > 0.5m) // 최소 수익 조건 추가
			{
				DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
				return;
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 20) return;

			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			var existingPosition = GetActivePosition(symbol, PositionSide.Short);
			var isHighVol = IsHighVolatility(charts, i);
			var trendStrength = GetTrendStrength(charts, i);
			var volumeConfirmed = IsVolumeConfirmed(charts, i);

			if (existingPosition == null)
			{
				// 기본 CCI 조건
				bool cciCondition = c3.Cci >= ExtremeLevelHigh && c2.Cci < c3.Cci && c1.Cci < c2.Cci;

				// RSI 필터: 과매수 상황에서만 진입
				bool rsiCondition = c1.Rsi1 >= RsiOverbought;

				// 볼륨 확인
				bool volumeCondition = volumeConfirmed || isHighVol;

				// 트렌드 필터: 너무 강한 상승 트렌드에서는 진입 금지
				bool trendCondition = trendStrength < 5.0m;

				if (cciCondition && rsiCondition && volumeCondition && trendCondition)
				{
					var entry = c0.Quote.Open;
					var stopLoss = entry * (1 + StopLossPercent / 100);
					DcaEntryPosition(PositionSide.Short, c0, entry, DcaStepPercent, DcaMultiplier, stopLoss);
				}
			}
			else
			{
				if (existingPosition.DcaStep < DcaMaxEntries)
				{
					var currentPrice = c0.Quote.Open;
					var entryPrice = existingPosition.EntryPrice;
					var risePercent = (currentPrice - entryPrice) / entryPrice * 100;
					var dynamicStep = isHighVol ? DcaStepPercent * 0.9m : DcaStepPercent;

					// RSI 조건: 추가 진입시에도 과매수 확인
					bool rsiOkForDca = c1.Rsi1 >= RsiOverbought - 10m; // 약간 완화된 조건

					if (risePercent >= dynamicStep * (existingPosition.DcaStep + 1) && rsiOkForDca)
					{
						var stopLoss = currentPrice * (1 + StopLossPercent / 100);
						DcaEntryPosition(PositionSide.Short, c0, currentPrice, DcaStepPercent, DcaMultiplier, stopLoss);
					}
				}
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			// 1. 강화된 손절 로직
			if (shortPosition.StopLossPrice > 0 && c0.Quote.High >= shortPosition.StopLossPrice)
			{
				DcaExitPosition(shortPosition, c0, shortPosition.StopLossPrice, 1.0m);
				return;
			}

			// 2. 동적 전량 익절 로직
			var trendStrength = GetTrendStrength(charts, i);
			var dynamicFullTarget = trendStrength < -5.0m ? FullExitTarget * 1.2m : FullExitTarget;
			var fullExitPrice = shortPosition.EntryPrice * (1 - dynamicFullTarget / 100);
			if (c0.Quote.Low <= fullExitPrice)
			{
				DcaExitPosition(shortPosition, c0, fullExitPrice, 1.0m);
				return;
			}

			// 3. 개선된 부분 익절 로직
			var dynamicPartialTarget = trendStrength < -3.0m ? ProfitTarget * 1.1m : ProfitTarget;
			var partialExitPrice = shortPosition.EntryPrice * (1 - dynamicPartialTarget / 100);
			if (shortPosition.Stage == 0 && c0.Quote.Low <= partialExitPrice)
			{
				var dynamicExitPercent = trendStrength < -3.0m ? PartialExitPercent * 0.8m : PartialExitPercent;
				DcaExitPosition(shortPosition, c0, partialExitPrice, dynamicExitPercent);
				shortPosition.Stage = 1;
				shortPosition.StopLossPrice = shortPosition.EntryPrice * 0.998m; // 본절보다 약간 아래로 설정
			}

			// 4. 개선된 트레일링 스톱
			var profitPercent = GetPositionProfitPercent(shortPosition, c0.Quote.Close);
			if (profitPercent > TrailingPercent)
			{
				var dynamicTrailing = trendStrength < -5.0m ? TrailingPercent * 0.8m : TrailingPercent;
				var newStopLoss = c0.Quote.Low * (1 + dynamicTrailing / 100);
				if (newStopLoss < shortPosition.StopLossPrice || shortPosition.StopLossPrice == 0)
				{
					shortPosition.StopLossPrice = newStopLoss;
				}
			}

			// 5. 개선된 CCI 기반 청산 (더 신중하게)
			bool cciExit = c1.Cci <= -50m && c0.Cci > c1.Cci; // 모멘텀 확인 추가
			bool rsiExit = c1.Rsi1 <= RsiOversold; // RSI 과매도 확인

			if ((cciExit || rsiExit) && profitPercent > 0.5m) // 최소 수익 조건 추가
			{
				DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
				return;
			}
		}
	}
}