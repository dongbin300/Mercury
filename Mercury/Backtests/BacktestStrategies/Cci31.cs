using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci31 - High Frequency + Safety Balance
	///
	/// Cci30 베이스 + 거래 빈도 대폭 증가 (68건 → 3000+ 목표)
	///
	/// === 핵심 목표 ===
	/// - 거래 횟수: 4년에 3000+ 거래 (현재 68건 → 3000+)
	/// - 승률: 75%+ 유지 (현재 88% → 75%+ 목표)
	/// - RPR: 80+ 유지 (현재 111 → 80+ 목표)
	/// - MDD: 10% 이하 (현재 2% → 10% 이하 허용)
	///
	/// === 주요 개선사항 ===
	/// 1. 진입 조건 완화: 4단계 → 2단계 확인
	/// 2. OR 로직 추가: 3가지 진입 경로
	/// 3. 볼륨 필터 완화: 150% → 130%
	/// 4. 연속 손실 완화: 2회 → 4회
	/// 5. CCI 범위 확대: 150/-150 → 130/-130
	/// 6. 빠른 진입 모드 복활
	/// 7. 트렌드 필터 완화
	///
	/// </summary>
	public class Cci31(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		// === CCI 기본 파라미터 (완화) ===
		public int CciPeriod = 14;
		public decimal ExtremeLevelHigh = 130m;      // 150 → 130 (더 빠른 진입)
		public decimal ExtremeLevelLow = -130m;      // -150 → -130
		public decimal ExitBuffer = 0m;

		// === RSI 필터 파라미터 (완화) ===
		public int RsiPeriod = 14;
		public decimal RsiOverbought = 68m;          // 70 → 68
		public decimal RsiOversold = 32m;            // 30 → 32

		// === 성공한 청산 전략 유지 ===
		public decimal ScalpExitPercent = 0.8m;      // 80% 스캘핑 유지
		public decimal ScalpTarget = 2.0m;           // 2.0% 스캘핑 유지
		public decimal PartialExitPercent = 0.2m;    // 나머지 20%
		public decimal ProfitTarget = 3.0m;          // 3.0% 부분익절
		public decimal FullExitTarget = 5.0m;        // 5.0% 전량익절

		// === 검증된 리스크 관리 유지 ===
		public decimal StopLossPercent = 1.0m;       // 1.0% 손절 유지
		public decimal TrailingPercent = 0.5m;       // 0.5% 트레일링 유지

		// === 연속 손실 관리 완화 ===
		private int consecutiveLosses = 0;
		private DateTime lastLossTime = DateTime.MinValue;
		private const int maxConsecutiveLosses = 4;  // 2 → 4 (더 관대)
		private const int cooldownMinutes = 30;      // 60 → 30 (더 짧은 휴식)

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = false;
			chartPack.UseCci(CciPeriod);
			chartPack.UseRsi(RsiPeriod);
		}

		private bool IsHighVolatility(List<ChartInfo> charts, int i)
		{
			if (i < 11) return false;
			var recent = charts.Skip(i - 10).Take(10).ToList();
			var avgRange = recent.Average(c => (c.Quote.High - c.Quote.Low) / c.Quote.Close * 100);
			return avgRange > 1.5m; // 2.0 → 1.5 (더 자주 감지)
		}

		private decimal GetTrendStrength(List<ChartInfo> charts, int i)
		{
			if (i < 11) return 0;
			var recent = charts.Skip(i - 10).Take(10).ToList();
			var firstPrice = recent.First().Quote.Close;
			var lastPrice = recent.Last().Quote.Close;
			return (lastPrice - firstPrice) / firstPrice * 100;
		}

		private bool IsVolumeConfirmed(List<ChartInfo> charts, int i)
		{
			if (i < 6) return false;
			var avgVolume = charts.Skip(i - 5).Take(5).Average(c => c.Quote.Volume);
			return charts[i].Quote.Volume > avgVolume * 1.3m; // 150% → 130% (완화)
		}

		private bool IsQuickEntry(List<ChartInfo> charts, int i)
		{
			if (i < 3) return false;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// 빠른 진입 조건 (모멘텀 기반)
			bool cciMoving = Math.Abs((decimal)(c1.Cci - c2.Cci)) > 8m; // 10 → 8 (더 민감)
			bool priceMoving = Math.Abs(c0.Quote.Close - c2.Quote.Close) / c2.Quote.Close * 100 > 0.2m; // 0.3 → 0.2

			return cciMoving && priceMoving;
		}

		private bool IsModerateEntry(List<ChartInfo> charts, int i)
		{
			if (i < 3) return false;
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			// 중간 강도 진입 조건
			bool cciImproving = c2.Cci > c3.Cci && c1.Cci > c2.Cci; // 2단계 개선
			bool rsiOk = c2.Rsi1 >= 25m && c2.Rsi1 <= 75m; // 극단 아닌 구간

			return cciImproving && rsiOk;
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
			if (i < 10) return;

			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// 연속 손실 체크
			if (IsInCooldown(c0.DateTime)) return;

			var existingPosition = GetActivePosition(symbol, PositionSide.Long);
			if (existingPosition != null) return;

			var isHighVol = IsHighVolatility(charts, i);
			var trendStrength = GetTrendStrength(charts, i);
			var volumeConfirmed = IsVolumeConfirmed(charts, i);
			var quickEntry = IsQuickEntry(charts, i);
			var moderateEntry = IsModerateEntry(charts, i);

			// === 3가지 진입 경로 (OR 로직) ===

			// 1. 강한 CCI 신호 (기존 로직 완화)
			bool strongCci = c2.Cci <= ExtremeLevelLow && c1.Cci > c2.Cci; // 2단계로 완화
			bool strongEntry = strongCci && c2.Rsi1 <= RsiOversold + 5m && volumeConfirmed;

			// 2. 중간 강도 진입 (볼륨 + 모멘텀)
			bool mediumEntry = moderateEntry && volumeConfirmed &&
							  trendStrength > -5.0m && trendStrength < 8.0m;

			// 3. 빠른 진입 (모멘텀 + 기본 조건)
			bool fastEntry = quickEntry && c2.Cci <= -80m && c2.Rsi1 <= 45m;

			// 하나라도 만족하면 진입
			if (strongEntry || mediumEntry || fastEntry)
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

			// 1. 검증된 손절 로직 유지 (Look-ahead bias 수정)
			if (longPosition.StopLossPrice > 0 && c0.Quote.Low <= longPosition.StopLossPrice)
			{
				decimal exitPrice = longPosition.StopLossPrice;
				if (c0.Quote.Open <= longPosition.StopLossPrice) // 캔들이 손절가보다 낮게 시작했으면 시가로 손절
				{
					exitPrice = c0.Quote.Open;
				}
				DcaExitPosition(longPosition, c0, exitPrice, 1.0m);
				consecutiveLosses++;
				lastLossTime = c0.DateTime;
				return;
			}

			// 2. 검증된 스캘핑 모드 유지 (80% 청산) (Look-ahead bias 수정)
			var scalpPrice = longPosition.EntryPrice * (1 + ScalpTarget / 100);
			if (longPosition.Stage == 0 && c0.Quote.High >= scalpPrice)
			{
				decimal exitPrice = scalpPrice;
				if (c0.Quote.Open >= scalpPrice) // 캔들이 익절가보다 높게 시작했으면 시가로 익절
				{
					exitPrice = c0.Quote.Open;
				}
				DcaExitPosition(longPosition, c0, exitPrice, ScalpExitPercent);
				longPosition.Stage = 1;
				longPosition.StopLossPrice = longPosition.EntryPrice * 1.01m; // 1% 수익 보장
				consecutiveLosses = 0; // 수익 거래시 초기화
			}

			// 3. 나머지 20% 부분 익절 (Look-ahead bias 수정)
			var partialPrice = longPosition.EntryPrice * (1 + ProfitTarget / 100);
			if (longPosition.Stage == 1 && c0.Quote.High >= partialPrice)
			{
				decimal exitPrice = partialPrice;
				if (c0.Quote.Open >= partialPrice) // 캔들이 익절가보다 높게 시작했으면 시가로 익절
				{
					exitPrice = c0.Quote.Open;
				}
				DcaExitPosition(longPosition, c0, exitPrice, PartialExitPercent);
				longPosition.Stage = 2;
				longPosition.StopLossPrice = longPosition.EntryPrice * 1.02m; // 2% 수익 보장
			}

			// 4. 최종 전량 익절 (Look-ahead bias 수정)
			var fullExitPrice = longPosition.EntryPrice * (1 + FullExitTarget / 100);
			if (c0.Quote.High >= fullExitPrice)
			{
				decimal exitPrice = fullExitPrice;
				if (c0.Quote.Open >= fullExitPrice) // 캔들이 익절가보다 높게 시작했으면 시가로 익절
				{
					exitPrice = c0.Quote.Open;
				}
				DcaExitPosition(longPosition, c0, exitPrice, 1.0m);
				consecutiveLosses = 0;
				return;
			}

			// 5. 검증된 트레일링 스톱 유지
			if (profitPercent > TrailingPercent)
			{
				var newStopLoss = c0.Quote.High * (1 - TrailingPercent / 100);
				if (newStopLoss > longPosition.StopLossPrice)
				{
					longPosition.StopLossPrice = newStopLoss;
				}
			}

			// 6. 조기 청산 (완화)
			bool earlyExit = c1.Cci >= 60m || c1.Rsi1 >= 65m; // 50 → 60, 60 → 65 (완화)
			bool lossExit = profitPercent < -0.5m; // 0.5% 손실시 조기 청산 유지

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
			if (i < 10) return;

			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			// 연속 손실 체크
			if (IsInCooldown(c0.DateTime)) return;

			var existingPosition = GetActivePosition(symbol, PositionSide.Short);
			if (existingPosition != null) return;

			var isHighVol = IsHighVolatility(charts, i);
			var trendStrength = GetTrendStrength(charts, i);
			var volumeConfirmed = IsVolumeConfirmed(charts, i);
			var quickEntry = IsQuickEntry(charts, i);
			var moderateEntry = IsModerateEntry(charts, i);

			// === 3가지 진입 경로 (OR 로직) ===

			// 1. 강한 CCI 신호 (기존 로직 완화)
			bool strongCci = c2.Cci >= ExtremeLevelHigh && c1.Cci < c2.Cci; // 2단계로 완화
			bool strongEntry = strongCci && c2.Rsi1 >= RsiOverbought - 5m && volumeConfirmed;

			// 2. 중간 강도 진입 (볼륨 + 모멘텀)
			bool mediumEntry = moderateEntry && volumeConfirmed &&
							  trendStrength < 5.0m && trendStrength > -8.0m;

			// 3. 빠른 진입 (모멘텀 + 기본 조건)
			bool fastEntry = quickEntry && c2.Cci >= 80m && c2.Rsi1 >= 55m;

			// 하나라도 만족하면 진입
			if (strongEntry || mediumEntry || fastEntry)
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

			// 1. 검증된 손절 로직 유지 (Look-ahead bias 수정)
			if (shortPosition.StopLossPrice > 0 && c0.Quote.High >= shortPosition.StopLossPrice)
			{
				decimal exitPrice = shortPosition.StopLossPrice;
				if (c0.Quote.Open >= shortPosition.StopLossPrice) // 캔들이 손절가보다 높게 시작했으면 시가로 손절
				{
					exitPrice = c0.Quote.Open;
				}
				DcaExitPosition(shortPosition, c0, exitPrice, 1.0m);
				consecutiveLosses++;
				lastLossTime = c0.DateTime;
				return;
			}

			// 2. 검증된 스캘핑 모드 유지 (80% 청산) (Look-ahead bias 수정)
			var scalpPrice = shortPosition.EntryPrice * (1 - ScalpTarget / 100);
			if (shortPosition.Stage == 0 && c0.Quote.Low <= scalpPrice)
			{
				decimal exitPrice = scalpPrice;
				if (c0.Quote.Open <= scalpPrice) // 캔들이 익절가보다 낮게 시작했으면 시가로 익절
				{
					exitPrice = c0.Quote.Open;
				}
				DcaExitPosition(shortPosition, c0, exitPrice, ScalpExitPercent);
				shortPosition.Stage = 1;
				shortPosition.StopLossPrice = shortPosition.EntryPrice * 0.99m; // 1% 수익 보장
				consecutiveLosses = 0;
			}

			// 3. 나머지 20% 부분 익절 (Look-ahead bias 수정)
			var partialPrice = shortPosition.EntryPrice * (1 - ProfitTarget / 100);
			if (shortPosition.Stage == 1 && c0.Quote.Low <= partialPrice)
			{
				decimal exitPrice = partialPrice;
				if (c0.Quote.Open <= partialPrice) // 캔들이 익절가보다 낮게 시작했으면 시가로 익절
				{
					exitPrice = c0.Quote.Open;
				}
				DcaExitPosition(shortPosition, c0, exitPrice, PartialExitPercent);
				shortPosition.Stage = 2;
				shortPosition.StopLossPrice = shortPosition.EntryPrice * 0.98m; // 2% 수익 보장
			}

			// 4. 최종 전량 익절 (Look-ahead bias 수정)
			var fullExitPrice = shortPosition.EntryPrice * (1 - FullExitTarget / 100);
			if (c0.Quote.Low <= fullExitPrice)
			{
				decimal exitPrice = fullExitPrice;
				if (c0.Quote.Open <= fullExitPrice) // 캔들이 익절가보다 낮게 시작했으면 시가로 익절
				{
					exitPrice = c0.Quote.Open;
				}
				DcaExitPosition(shortPosition, c0, exitPrice, 1.0m);
				consecutiveLosses = 0;
				return;
			}

			// 5. 검증된 트레일링 스톱 유지
			if (profitPercent > TrailingPercent)
			{
				var newStopLoss = c0.Quote.Low * (1 + TrailingPercent / 100);
				if (newStopLoss < shortPosition.StopLossPrice || shortPosition.StopLossPrice == 0)
				{
					shortPosition.StopLossPrice = newStopLoss;
				}
			}

			// 6. 조기 청산 (완화)
			bool earlyExit = c1.Cci <= -60m || c1.Rsi1 <= 35m; // -50 → -60, 40 → 35 (완화)
			bool lossExit = profitPercent < -0.5m; // 0.5% 손실시 조기 청산 유지

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