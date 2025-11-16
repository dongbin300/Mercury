using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci27 - High Frequency Trading with Scalping Mode
	///
	/// Cci26 베이스 + 거래 빈도 대폭 증가 및 스캘핑 모드 추가
	///
	/// === 핵심 목표 ===
	/// - 거래 횟수: 4년에 3000+ 거래 (기존 300 → 3000+)
	/// - RPR 점수: 100+ 달성
	/// - MDD: 15% 이하 유지
	/// - 작은 수익이라도 자주 거래하여 안정적 복리 효과
	///
	/// === 주요 개선사항 ===
	/// 1. CCI 레벨 완화: 200/-200 → 150/-150 (진입 기회 증가)
	/// 2. RSI 조건 완화: 70/30 → 65/35 (더 빠른 진입)
	/// 3. 볼륨 필터 완화: 120% → 110% (진입 장벽 낮춤)
	/// 4. 스캘핑 모드 추가: 0.8% 빠른 익절 옵션
	/// 5. 다단계 익절 시스템: 0.8% → 1.2% → 2.5%
	/// 6. 진입 조건 OR 로직 추가 (AND → OR)
	/// 7. 미니 DCA 시스템: 1.5% 간격으로 빠른 추가 진입
	///
	/// </summary>
	public class Cci27(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		// === CCI 기본 파라미터 (완화) ===
		public int CciPeriod = 14;
		public decimal ExtremeLevelHigh = 150m;      // 200 → 150 (진입 기회 증가)
		public decimal ExtremeLevelLow = -150m;      // -200 → -150
		public decimal ExitBuffer = 0m;

		// === RSI 필터 파라미터 (완화) ===
		public int RsiPeriod = 14;
		public decimal RsiOverbought = 65m;          // 70 → 65 (더 빠른 진입)
		public decimal RsiOversold = 35m;            // 30 → 35

		// === DCA 설정 (빠른 거래용) ===
		public int DcaMaxEntries = 3;                // 2 → 3 (더 많은 기회)
		public decimal DcaStepPercent = 1.5m;        // 3.0 → 1.5% (빠른 추가 진입)
		public decimal DcaMultiplier = 1.3m;         // 1.5 → 1.3 (리스크 관리)

		// === 스캘핑 청산 전략 ===
		public decimal ScalpExitPercent = 0.5m;      // 스캘핑 비율 (50%)
		public decimal ScalpTarget = 0.8m;           // 스캘핑 목표 (0.8%)
		public decimal PartialExitPercent = 0.4m;    // 부분 청산 비율 (0.6 → 0.4)
		public decimal ProfitTarget = 1.2m;          // 부분 청산 목표 (1.5 → 1.2%)
		public decimal FullExitTarget = 2.5m;        // 전량 청산 목표 (3.5 → 2.5%)

		// === 리스크 관리 설정 ===
		public decimal StopLossPercent = 2.0m;       // 2.5 → 2.0% (빠른 손절)
		public decimal TrailingPercent = 0.8m;       // 1.2 → 0.8% (촘촘한 트레일링)

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = true;
			chartPack.UseCci(CciPeriod);
			chartPack.UseRsi(RsiPeriod);
		}

		private bool IsHighVolatility(List<ChartInfo> charts, int i)
		{
			if (i < 10) return false;
			var recent = charts.Skip(i - 9).Take(10).ToList();
			var avgRange = recent.Average(c => (c.Quote.High - c.Quote.Low) / c.Quote.Close * 100);
			return avgRange > 2.0m; // 2.5 → 2.0 (더 자주 감지)
		}

		private decimal GetTrendStrength(List<ChartInfo> charts, int i)
		{
			if (i < 10) return 0;
			var recent = charts.Skip(i - 9).Take(10).ToList();
			var firstPrice = recent.First().Quote.Close;
			var lastPrice = recent.Last().Quote.Close;
			return (lastPrice - firstPrice) / firstPrice * 100;
		}

		private bool IsVolumeConfirmed(List<ChartInfo> charts, int i)
		{
			if (i < 5) return false;
			var avgVolume = charts.Skip(i - 4).Take(5).Average(c => c.Quote.Volume);
			return charts[i].Quote.Volume > avgVolume * 1.1m; // 120% → 110% (완화)
		}

		private bool IsQuickEntrySignal(List<ChartInfo> charts, int i)
		{
			if (i < 3) return false;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// 빠른 반전 신호 감지
			bool priceReversal = (c2.Quote.Close > c1.Quote.Close) && (c1.Quote.Close < c0.Quote.Close);
			bool cciImproving = c1.Cci > c2.Cci;

			return priceReversal && cciImproving;
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 10) return;

			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			var existingPosition = GetActivePosition(symbol, PositionSide.Long);
			var isHighVol = IsHighVolatility(charts, i);
			var trendStrength = GetTrendStrength(charts, i);
			var volumeConfirmed = IsVolumeConfirmed(charts, i);
			var quickSignal = IsQuickEntrySignal(charts, i);

			if (existingPosition == null)
			{
				// 기본 CCI 조건 (완화)
				bool basicCciCondition = c1.Cci <= ExtremeLevelLow && c0.Cci > c1.Cci;
				bool strongCciCondition = c3.Cci <= ExtremeLevelLow && c2.Cci > c3.Cci && c1.Cci > c2.Cci;

				// RSI 조건 (완화)
				bool rsiCondition = c1.Rsi1 <= RsiOversold + 5m; // 더 관대한 조건

				// 볼륨 조건 (완화)
				bool volumeCondition = volumeConfirmed || isHighVol || quickSignal;

				// 트렌드 필터 (완화)
				bool trendCondition = trendStrength > -8.0m; // -5.0 → -8.0

				// OR 로직: 하나라도 강한 신호면 진입
				bool strongEntry = (strongCciCondition && rsiCondition && volumeCondition) ||
								  (basicCciCondition && rsiCondition && volumeCondition && trendCondition) ||
								  (quickSignal && c1.Cci <= -100m); // 빠른 진입 옵션

				if (strongEntry)
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
					var dynamicStep = isHighVol ? DcaStepPercent * 0.8m : DcaStepPercent;

					// 더 관대한 DCA 조건
					bool rsiOkForDca = c1.Rsi1 <= RsiOversold + 15m; // +10 → +15
					bool quickDca = quickSignal && dropPercent >= DcaStepPercent * 0.5m; // 빠른 DCA

					if ((dropPercent >= dynamicStep * (existingPosition.DcaStep + 1) && rsiOkForDca) || quickDca)
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
			var trendStrength = GetTrendStrength(charts, i);
			var profitPercent = GetPositionProfitPercent(longPosition, c0.Quote.Close);

			// 1. 손절 로직
			if (longPosition.StopLossPrice > 0 && c0.Quote.Low <= longPosition.StopLossPrice)
			{
				DcaExitPosition(longPosition, c0, longPosition.StopLossPrice, 1.0m);
				return;
			}

			// 2. 스캘핑 모드 (Stage 0에서만)
			var scalpPrice = longPosition.EntryPrice * (1 + ScalpTarget / 100);
			if (longPosition.Stage == 0 && c0.Quote.High >= scalpPrice)
			{
				DcaExitPosition(longPosition, c0, scalpPrice, ScalpExitPercent);
				longPosition.Stage = 1;
				longPosition.StopLossPrice = longPosition.EntryPrice * 1.001m; // 거의 본절
			}

			// 3. 1차 부분 익절 (Stage 1에서만)
			var partialPrice = longPosition.EntryPrice * (1 + ProfitTarget / 100);
			if (longPosition.Stage == 1 && c0.Quote.High >= partialPrice)
			{
				DcaExitPosition(longPosition, c0, partialPrice, PartialExitPercent);
				longPosition.Stage = 2;
				longPosition.StopLossPrice = longPosition.EntryPrice * 1.005m; // 0.5% 수익 보장
			}

			// 4. 최종 전량 익절
			var fullExitPrice = longPosition.EntryPrice * (1 + FullExitTarget / 100);
			if (c0.Quote.High >= fullExitPrice)
			{
				DcaExitPosition(longPosition, c0, fullExitPrice, 1.0m);
				return;
			}

			// 5. 촘촘한 트레일링 스톱
			if (profitPercent > TrailingPercent)
			{
				var dynamicTrailing = trendStrength > 3.0m ? TrailingPercent * 0.7m : TrailingPercent;
				var newStopLoss = c0.Quote.High * (1 - dynamicTrailing / 100);
				if (newStopLoss > longPosition.StopLossPrice)
				{
					longPosition.StopLossPrice = newStopLoss;
				}
			}

			// 6. 빠른 CCI 청산 (완화된 조건)
			bool quickCciExit = c1.Cci >= 30m && c0.Cci < c1.Cci; // 50 → 30
			bool rsiExit = c1.Rsi1 >= RsiOverbought;

			if ((quickCciExit || rsiExit) && profitPercent > 0.3m) // 0.5 → 0.3%
			{
				DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
				return;
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 10) return;

			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			var existingPosition = GetActivePosition(symbol, PositionSide.Short);
			var isHighVol = IsHighVolatility(charts, i);
			var trendStrength = GetTrendStrength(charts, i);
			var volumeConfirmed = IsVolumeConfirmed(charts, i);
			var quickSignal = IsQuickEntrySignal(charts, i);

			if (existingPosition == null)
			{
				// 기본 CCI 조건 (완화)
				bool basicCciCondition = c1.Cci >= ExtremeLevelHigh && c0.Cci < c1.Cci;
				bool strongCciCondition = c3.Cci >= ExtremeLevelHigh && c2.Cci < c3.Cci && c1.Cci < c2.Cci;

				// RSI 조건 (완화)
				bool rsiCondition = c1.Rsi1 >= RsiOverbought - 5m; // 더 관대한 조건

				// 볼륨 조건 (완화)
				bool volumeCondition = volumeConfirmed || isHighVol || quickSignal;

				// 트렌드 필터 (완화)
				bool trendCondition = trendStrength < 8.0m; // 5.0 → 8.0

				// OR 로직: 하나라도 강한 신호면 진입
				bool strongEntry = (strongCciCondition && rsiCondition && volumeCondition) ||
								  (basicCciCondition && rsiCondition && volumeCondition && trendCondition) ||
								  (quickSignal && c1.Cci >= 100m); // 빠른 진입 옵션

				if (strongEntry)
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
					var dynamicStep = isHighVol ? DcaStepPercent * 0.8m : DcaStepPercent;

					// 더 관대한 DCA 조건
					bool rsiOkForDca = c1.Rsi1 >= RsiOverbought - 15m; // -10 → -15
					bool quickDca = quickSignal && risePercent >= DcaStepPercent * 0.5m; // 빠른 DCA

					if ((risePercent >= dynamicStep * (existingPosition.DcaStep + 1) && rsiOkForDca) || quickDca)
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
			var trendStrength = GetTrendStrength(charts, i);
			var profitPercent = GetPositionProfitPercent(shortPosition, c0.Quote.Close);

			// 1. 손절 로직
			if (shortPosition.StopLossPrice > 0 && c0.Quote.High >= shortPosition.StopLossPrice)
			{
				DcaExitPosition(shortPosition, c0, shortPosition.StopLossPrice, 1.0m);
				return;
			}

			// 2. 스캘핑 모드 (Stage 0에서만)
			var scalpPrice = shortPosition.EntryPrice * (1 - ScalpTarget / 100);
			if (shortPosition.Stage == 0 && c0.Quote.Low <= scalpPrice)
			{
				DcaExitPosition(shortPosition, c0, scalpPrice, ScalpExitPercent);
				shortPosition.Stage = 1;
				shortPosition.StopLossPrice = shortPosition.EntryPrice * 0.999m; // 거의 본절
			}

			// 3. 1차 부분 익절 (Stage 1에서만)
			var partialPrice = shortPosition.EntryPrice * (1 - ProfitTarget / 100);
			if (shortPosition.Stage == 1 && c0.Quote.Low <= partialPrice)
			{
				DcaExitPosition(shortPosition, c0, partialPrice, PartialExitPercent);
				shortPosition.Stage = 2;
				shortPosition.StopLossPrice = shortPosition.EntryPrice * 0.995m; // 0.5% 수익 보장
			}

			// 4. 최종 전량 익절
			var fullExitPrice = shortPosition.EntryPrice * (1 - FullExitTarget / 100);
			if (c0.Quote.Low <= fullExitPrice)
			{
				DcaExitPosition(shortPosition, c0, fullExitPrice, 1.0m);
				return;
			}

			// 5. 촘촘한 트레일링 스톱
			if (profitPercent > TrailingPercent)
			{
				var dynamicTrailing = trendStrength < -3.0m ? TrailingPercent * 0.7m : TrailingPercent;
				var newStopLoss = c0.Quote.Low * (1 + dynamicTrailing / 100);
				if (newStopLoss < shortPosition.StopLossPrice || shortPosition.StopLossPrice == 0)
				{
					shortPosition.StopLossPrice = newStopLoss;
				}
			}

			// 6. 빠른 CCI 청산 (완화된 조건)
			bool quickCciExit = c1.Cci <= -30m && c0.Cci > c1.Cci; // -50 → -30
			bool rsiExit = c1.Rsi1 <= RsiOversold;

			if ((quickCciExit || rsiExit) && profitPercent > 0.3m) // 0.5 → 0.3%
			{
				DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
				return;
			}
		}
	}
}