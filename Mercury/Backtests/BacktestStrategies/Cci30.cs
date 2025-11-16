using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci30 - Emergency Fix: Prevent Total Loss
	///
	/// Cci29에서 청산 문제 긴급 수정
	///
	/// === 긴급 수정사항 ===
	/// - 손절 < 스캘핑으로 역전 방지
	/// - 진입 조건 재강화 (AND 로직 복귀)
	/// - 연속 손실 관리 극강화
	/// - 승률 70%+ 목표로 안정성 우선
	///
	/// === 핵심 목표 ===
	/// - 청산 방지: 절대 자금 보호
	/// - 승률: 70%+ 달성
	/// - 거래 횟수: 1000+ (적당한 수준)
	/// - RPR: 50+ (안정적 수준)
	/// - MDD: 5% 이하 (극보수)
	///
	/// </summary>
	public class Cci30(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		// === CCI 기본 파라미터 (보수적) ===
		public int CciPeriod = 14;
		public decimal ExtremeLevelHigh = 150m;      // 100 → 150 (더 보수적)
		public decimal ExtremeLevelLow = -150m;      // -100 → -150
		public decimal ExitBuffer = 0m;

		// === RSI 필터 파라미터 (보수적) ===
		public int RsiPeriod = 14;
		public decimal RsiOverbought = 70m;          // 65 → 70
		public decimal RsiOversold = 30m;            // 35 → 30

		// === DCA 설정 (원샷 유지) ===
		public int DcaMaxEntries = 1;
		public decimal DcaStepPercent = 0m;
		public decimal DcaMultiplier = 1.0m;

		// === 수정된 청산 전략 (손절 < 스캘핑) ===
		public decimal ScalpExitPercent = 0.8m;      // 80% 스캘핑
		public decimal ScalpTarget = 2.0m;           // 1.2 → 2.0% (손절보다 크게)
		public decimal PartialExitPercent = 0.2m;    // 0.4 → 0.2 (나머지 20%만)
		public decimal ProfitTarget = 3.0m;          // 2.0 → 3.0%
		public decimal FullExitTarget = 5.0m;        // 3.0 → 5.0%

		// === 극강 리스크 관리 ===
		public decimal StopLossPercent = 1.0m;       // 1.5 → 1.0% (스캘핑보다 작게!)
		public decimal TrailingPercent = 0.5m;       // 0.8 → 0.5%

		// === 극강 연속 손실 관리 ===
		private int consecutiveLosses = 0;
		private DateTime lastLossTime = DateTime.MinValue;
		private const int maxConsecutiveLosses = 2;  // 5 → 2 (극엄격)
		private const int cooldownMinutes = 60;      // 15 → 60 (긴 휴식)

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = false;
			chartPack.UseCci(CciPeriod);
			chartPack.UseRsi(RsiPeriod);
		}

		private bool IsHighVolatility(List<ChartInfo> charts, int i)
		{
			if (i < 20) return false;
			var recent = charts.Skip(i - 19).Take(20).ToList();
			var avgRange = recent.Average(c => (c.Quote.High - c.Quote.Low) / c.Quote.Close * 100);
			return avgRange > 2.0m; // 1.0 → 2.0 (더 엄격)
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
			return charts[i].Quote.Volume > avgVolume * 1.5m; // 120% → 150% (더 엄격)
		}

		private bool IsStrongTrend(List<ChartInfo> charts, int i)
		{
			var trendStrength = GetTrendStrength(charts, i);
			return Math.Abs(trendStrength) < 3.0m; // 강한 트렌드에서는 진입 금지
		}

		private bool IsInCooldown(DateTime currentTime)
		{
			if (consecutiveLosses >= maxConsecutiveLosses)
			{
				var timeSinceLastLoss = currentTime - lastLossTime;
				return timeSinceLastLoss.TotalMinutes < cooldownMinutes;
			}
			return false;
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 20) return;

			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			// 연속 손실 체크
			if (IsInCooldown(c0.DateTime)) return;

			var existingPosition = GetActivePosition(symbol, PositionSide.Long);
			if (existingPosition != null) return;

			var isHighVol = IsHighVolatility(charts, i);
			var volumeConfirmed = IsVolumeConfirmed(charts, i);
			var strongTrend = IsStrongTrend(charts, i);

			// === 극도로 엄격한 진입 조건 (AND 로직 복귀) ===
			bool cciCondition = c3.Cci <= ExtremeLevelLow &&
							   c2.Cci > c3.Cci &&
							   c1.Cci > c2.Cci &&
							   c0.Cci > c1.Cci; // 4단계 상승 확인

			bool rsiCondition = c1.Rsi1 <= RsiOversold && c0.Rsi1 > c1.Rsi1; // RSI 반등 확인

			bool volumeCondition = volumeConfirmed; // 볼륨 필수

			bool trendCondition = strongTrend; // 안정적 트렌드 필수

			bool priceConfirmation = c0.Quote.Close > c1.Quote.Close; // 가격 상승 확인

			// 모든 조건 만족 필수
			if (cciCondition && rsiCondition && volumeCondition && trendCondition && priceConfirmation)
			{
				var entry = c0.Quote.Open;
				var stopLoss = entry * (1 - StopLossPercent / 100);
				DcaEntryPosition(PositionSide.Long, c0, entry, 0m, 1.0m, stopLoss);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var profitPercent = GetPositionProfitPercent(longPosition, c0.Quote.Close);

			// 1. 최우선 손절 로직 (스캘핑보다 작음)
			if (longPosition.StopLossPrice > 0 && c0.Quote.Low <= longPosition.StopLossPrice)
			{
				DcaExitPosition(longPosition, c0, longPosition.StopLossPrice, 1.0m);
				consecutiveLosses++;
				lastLossTime = c0.DateTime;
				return;
			}

			// 2. 스캘핑 모드 (80% 청산으로 수익 확정)
			var scalpPrice = longPosition.EntryPrice * (1 + ScalpTarget / 100);
			if (longPosition.Stage == 0 && c0.Quote.High >= scalpPrice)
			{
				DcaExitPosition(longPosition, c0, scalpPrice, ScalpExitPercent);
				longPosition.Stage = 1;
				longPosition.StopLossPrice = longPosition.EntryPrice * 1.01m; // 1% 수익 보장
				consecutiveLosses = 0; // 수익 거래시 초기화
			}

			// 3. 나머지 20% 부분 익절
			var partialPrice = longPosition.EntryPrice * (1 + ProfitTarget / 100);
			if (longPosition.Stage == 1 && c0.Quote.High >= partialPrice)
			{
				DcaExitPosition(longPosition, c0, partialPrice, PartialExitPercent / (1 - ScalpExitPercent)); // 실제 비율 조정
				longPosition.Stage = 2;
				longPosition.StopLossPrice = longPosition.EntryPrice * 1.02m; // 2% 수익 보장
			}

			// 4. 최종 전량 익절 (남은 물량)
			var fullExitPrice = longPosition.EntryPrice * (1 + FullExitTarget / 100);
			if (c0.Quote.High >= fullExitPrice)
			{
				DcaExitPosition(longPosition, c0, fullExitPrice, 1.0m);
				consecutiveLosses = 0;
				return;
			}

			// 5. 극민감 트레일링 스톱
			if (profitPercent > TrailingPercent)
			{
				var newStopLoss = c0.Quote.High * (1 - TrailingPercent / 100);
				if (newStopLoss > longPosition.StopLossPrice)
				{
					longPosition.StopLossPrice = newStopLoss;
				}
			}

			// 6. 조기 청산 (손실 방지)
			bool earlyExit = c1.Cci >= 50m || c1.Rsi1 >= 60m;
			bool lossExit = profitPercent < -0.5m; // 0.5% 손실시 조기 청산

			if (earlyExit || lossExit)
			{
				DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
				if (lossExit)
				{
					consecutiveLosses++;
					lastLossTime = c0.DateTime;
				}
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

			// 연속 손실 체크
			if (IsInCooldown(c0.DateTime)) return;

			var existingPosition = GetActivePosition(symbol, PositionSide.Short);
			if (existingPosition != null) return;

			var isHighVol = IsHighVolatility(charts, i);
			var volumeConfirmed = IsVolumeConfirmed(charts, i);
			var strongTrend = IsStrongTrend(charts, i);

			// === 극도로 엄격한 진입 조건 (AND 로직 복귀) ===
			bool cciCondition = c3.Cci >= ExtremeLevelHigh &&
							   c2.Cci < c3.Cci &&
							   c1.Cci < c2.Cci &&
							   c0.Cci < c1.Cci; // 4단계 하락 확인

			bool rsiCondition = c1.Rsi1 >= RsiOverbought && c0.Rsi1 < c1.Rsi1; // RSI 반락 확인

			bool volumeCondition = volumeConfirmed; // 볼륨 필수

			bool trendCondition = strongTrend; // 안정적 트렌드 필수

			bool priceConfirmation = c0.Quote.Close < c1.Quote.Close; // 가격 하락 확인

			// 모든 조건 만족 필수
			if (cciCondition && rsiCondition && volumeCondition && trendCondition && priceConfirmation)
			{
				var entry = c0.Quote.Open;
				var stopLoss = entry * (1 + StopLossPercent / 100);
				DcaEntryPosition(PositionSide.Short, c0, entry, 0m, 1.0m, stopLoss);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var profitPercent = GetPositionProfitPercent(shortPosition, c0.Quote.Close);

			// 1. 최우선 손절 로직
			if (shortPosition.StopLossPrice > 0 && c0.Quote.High >= shortPosition.StopLossPrice)
			{
				DcaExitPosition(shortPosition, c0, shortPosition.StopLossPrice, 1.0m);
				consecutiveLosses++;
				lastLossTime = c0.DateTime;
				return;
			}

			// 2. 스캘핑 모드 (80% 청산으로 수익 확정)
			var scalpPrice = shortPosition.EntryPrice * (1 - ScalpTarget / 100);
			if (shortPosition.Stage == 0 && c0.Quote.Low <= scalpPrice)
			{
				DcaExitPosition(shortPosition, c0, scalpPrice, ScalpExitPercent);
				shortPosition.Stage = 1;
				shortPosition.StopLossPrice = shortPosition.EntryPrice * 0.99m; // 1% 수익 보장
				consecutiveLosses = 0;
			}

			// 3. 나머지 20% 부분 익절
			var partialPrice = shortPosition.EntryPrice * (1 - ProfitTarget / 100);
			if (shortPosition.Stage == 1 && c0.Quote.Low <= partialPrice)
			{
				DcaExitPosition(shortPosition, c0, partialPrice, PartialExitPercent / (1 - ScalpExitPercent));
				shortPosition.Stage = 2;
				shortPosition.StopLossPrice = shortPosition.EntryPrice * 0.98m; // 2% 수익 보장
			}

			// 4. 최종 전량 익절
			var fullExitPrice = shortPosition.EntryPrice * (1 - FullExitTarget / 100);
			if (c0.Quote.Low <= fullExitPrice)
			{
				DcaExitPosition(shortPosition, c0, fullExitPrice, 1.0m);
				consecutiveLosses = 0;
				return;
			}

			// 5. 극민감 트레일링 스톱
			if (profitPercent > TrailingPercent)
			{
				var newStopLoss = c0.Quote.Low * (1 + TrailingPercent / 100);
				if (newStopLoss < shortPosition.StopLossPrice || shortPosition.StopLossPrice == 0)
				{
					shortPosition.StopLossPrice = newStopLoss;
				}
			}

			// 6. 조기 청산 (손실 방지)
			bool earlyExit = c1.Cci <= -50m || c1.Rsi1 <= 40m;
			bool lossExit = profitPercent < -0.5m; // 0.5% 손실시 조기 청산

			if (earlyExit || lossExit)
			{
				DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
				if (lossExit)
				{
					consecutiveLosses++;
					lastLossTime = c0.DateTime;
				}
				return;
			}
		}
	}
}