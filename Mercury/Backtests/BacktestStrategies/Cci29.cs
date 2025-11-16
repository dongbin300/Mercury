using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci29 - Balanced High Frequency Trading
	///
	/// Cci28 베이스 + 진입 빈도 대폭 증가 (43건 → 3000+ 목표)
	///
	/// === 핵심 목표 ===
	/// - 거래 횟수: 4년에 3000+ 거래 (현재 43건 → 3000+)
	/// - RPR 점수: 80+ 달성 (현재 120점 유지)
	/// - MDD: 15% 이하 (현재 1.07% 좋음)
	/// - 승률: 75%+ 유지
	///
	/// === 주요 개선사항 ===
	/// 1. 진입 조건 OR 로직: AND → OR (하나만 만족해도 진입)
	/// 2. 볼륨 필터 완화: 150% → 120%
	/// 3. 강한신호 필터 제거: 너무 까다로움
	/// 4. CCI 범위 확대: 120/-120 → 100/-100
	/// 5. RSI 범위 확대: 60/40 → 65/35
	/// 6. 빠른 진입 모드 추가: 간단한 조건으로 추가 기회
	/// 7. 트렌드 필터 완화: 더 넓은 범위
	///
	/// </summary>
	public class Cci29(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		// === CCI 기본 파라미터 (완화) ===
		public int CciPeriod = 14;
		public decimal ExtremeLevelHigh = 100m;      // 120 → 100 (더 빠른 진입)
		public decimal ExtremeLevelLow = -100m;      // -120 → -100
		public decimal ExitBuffer = 0m;

		// === RSI 필터 파라미터 (완화) ===
		public int RsiPeriod = 14;
		public decimal RsiOverbought = 65m;          // 60 → 65
		public decimal RsiOversold = 35m;            // 40 → 35

		// === DCA 설정 (원샷 유지) ===
		public int DcaMaxEntries = 1;
		public decimal DcaStepPercent = 0m;
		public decimal DcaMultiplier = 1.0m;

		// === 스캘핑 청산 전략 ===
		public decimal ScalpExitPercent = 0.6m;      // 0.7 → 0.6 (더 많이 홀드)
		public decimal ScalpTarget = 1.2m;           // 1.5 → 1.2% (더 빠른 스캘핑)
		public decimal PartialExitPercent = 0.4m;
		public decimal ProfitTarget = 2.0m;          // 2.2 → 2.0%
		public decimal FullExitTarget = 3.0m;        // 3.5 → 3.0%

		// === 리스크 관리 설정 (유지) ===
		public decimal StopLossPercent = 1.5m;       // 1.2 → 1.5% (약간 완화)
		public decimal TrailingPercent = 0.8m;

		// === 연속 손실 관리 (완화) ===
		private int consecutiveLosses = 0;
		private DateTime lastLossTime = DateTime.MinValue;
		private const int maxConsecutiveLosses = 5;  // 3 → 5 (더 관대)
		private const int cooldownMinutes = 15;      // 30 → 15 (더 짧은 휴식)

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = false;
			chartPack.UseCci(CciPeriod);
			chartPack.UseRsi(RsiPeriod);
		}

		private bool IsHighVolatility(List<ChartInfo> charts, int i)
		{
			if (i < 10) return false;
			var recent = charts.Skip(i - 9).Take(10).ToList();
			var avgRange = recent.Average(c => (c.Quote.High - c.Quote.Low) / c.Quote.Close * 100);
			return avgRange > 1.0m; // 1.5 → 1.0 (더 자주 감지)
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
			return charts[i].Quote.Volume > avgVolume * 1.2m; // 150% → 120%
		}

		private bool IsQuickEntry(List<ChartInfo> charts, int i)
		{
			if (i < 2) return false;
			var c0 = charts[i];
			var c1 = charts[i - 1];

			// 간단한 빠른 진입 조건
			bool cciMoving = Math.Abs((decimal)(c0.Cci - c1.Cci)) > 10m;
			bool priceMoving = Math.Abs(c0.Quote.Close - c1.Quote.Close) / c1.Quote.Close * 100 > 0.3m;

			return cciMoving && priceMoving;
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

			// === 3가지 진입 경로 (OR 로직) ===

			// 1. 강한 CCI 신호 (기본)
			bool strongCci = c1.Cci <= ExtremeLevelLow && c0.Cci > c1.Cci;
			bool cciEntry = strongCci && c1.Rsi1 <= RsiOversold + 10m;

			// 2. 볼륨 + 트렌드 진입
			bool volumeTrend = volumeConfirmed && trendStrength > -5.0m && trendStrength < 8.0m;
			bool volumeEntry = volumeTrend && c1.Cci <= -50m && c0.Cci > c1.Cci;

			// 3. 빠른 진입 (추가 기회)
			bool quickCondition = quickEntry && c1.Cci <= -30m && c1.Rsi1 <= 50m;

			// 하나라도 만족하면 진입
			if (cciEntry || volumeEntry || quickCondition)
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

			// 1. 손절 로직
			if (longPosition.StopLossPrice > 0 && c0.Quote.Low <= longPosition.StopLossPrice)
			{
				DcaExitPosition(longPosition, c0, longPosition.StopLossPrice, 1.0m);
				consecutiveLosses++;
				lastLossTime = c0.DateTime;
				return;
			}

			// 2. 스캘핑 모드
			var scalpPrice = longPosition.EntryPrice * (1 + ScalpTarget / 100);
			if (longPosition.Stage == 0 && c0.Quote.High >= scalpPrice)
			{
				DcaExitPosition(longPosition, c0, scalpPrice, ScalpExitPercent);
				longPosition.Stage = 1;
				longPosition.StopLossPrice = longPosition.EntryPrice * 1.003m;
				consecutiveLosses = 0;
			}

			// 3. 부분 익절
			var partialPrice = longPosition.EntryPrice * (1 + ProfitTarget / 100);
			if (longPosition.Stage == 1 && c0.Quote.High >= partialPrice)
			{
				DcaExitPosition(longPosition, c0, partialPrice, PartialExitPercent);
				longPosition.Stage = 2;
				longPosition.StopLossPrice = longPosition.EntryPrice * 1.008m;
			}

			// 4. 전량 익절
			var fullExitPrice = longPosition.EntryPrice * (1 + FullExitTarget / 100);
			if (c0.Quote.High >= fullExitPrice)
			{
				DcaExitPosition(longPosition, c0, fullExitPrice, 1.0m);
				consecutiveLosses = 0;
				return;
			}

			// 5. 트레일링 스톱
			if (profitPercent > TrailingPercent)
			{
				var newStopLoss = c0.Quote.High * (1 - TrailingPercent / 100);
				if (newStopLoss > longPosition.StopLossPrice)
				{
					longPosition.StopLossPrice = newStopLoss;
				}
			}

			// 6. 빠른 청산 (완화)
			bool quickExit = c1.Cci >= 20m && profitPercent > 0.5m; // 0 → 20 (덜 민감)
			bool rsiExit = c1.Rsi1 >= 70m && profitPercent > 0.3m;  // 65 → 70

			if (quickExit || rsiExit)
			{
				DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
				consecutiveLosses = 0;
				return;
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 10) return;

			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// 연속 손실 체크
			if (IsInCooldown(c0.DateTime)) return;

			var existingPosition = GetActivePosition(symbol, PositionSide.Short);
			if (existingPosition != null) return;

			var isHighVol = IsHighVolatility(charts, i);
			var trendStrength = GetTrendStrength(charts, i);
			var volumeConfirmed = IsVolumeConfirmed(charts, i);
			var quickEntry = IsQuickEntry(charts, i);

			// === 3가지 진입 경로 (OR 로직) ===

			// 1. 강한 CCI 신호 (기본)
			bool strongCci = c1.Cci >= ExtremeLevelHigh && c0.Cci < c1.Cci;
			bool cciEntry = strongCci && c1.Rsi1 >= RsiOverbought - 10m;

			// 2. 볼륨 + 트렌드 진입
			bool volumeTrend = volumeConfirmed && trendStrength < 5.0m && trendStrength > -8.0m;
			bool volumeEntry = volumeTrend && c1.Cci >= 50m && c0.Cci < c1.Cci;

			// 3. 빠른 진입 (추가 기회)
			bool quickCondition = quickEntry && c1.Cci >= 30m && c1.Rsi1 >= 50m;

			// 하나라도 만족하면 진입
			if (cciEntry || volumeEntry || quickCondition)
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

			// 1. 손절 로직
			if (shortPosition.StopLossPrice > 0 && c0.Quote.High >= shortPosition.StopLossPrice)
			{
				DcaExitPosition(shortPosition, c0, shortPosition.StopLossPrice, 1.0m);
				consecutiveLosses++;
				lastLossTime = c0.DateTime;
				return;
			}

			// 2. 스캘핑 모드
			var scalpPrice = shortPosition.EntryPrice * (1 - ScalpTarget / 100);
			if (shortPosition.Stage == 0 && c0.Quote.Low <= scalpPrice)
			{
				DcaExitPosition(shortPosition, c0, scalpPrice, ScalpExitPercent);
				shortPosition.Stage = 1;
				shortPosition.StopLossPrice = shortPosition.EntryPrice * 0.997m;
				consecutiveLosses = 0;
			}

			// 3. 부분 익절
			var partialPrice = shortPosition.EntryPrice * (1 - ProfitTarget / 100);
			if (shortPosition.Stage == 1 && c0.Quote.Low <= partialPrice)
			{
				DcaExitPosition(shortPosition, c0, partialPrice, PartialExitPercent);
				shortPosition.Stage = 2;
				shortPosition.StopLossPrice = shortPosition.EntryPrice * 0.992m;
			}

			// 4. 전량 익절
			var fullExitPrice = shortPosition.EntryPrice * (1 - FullExitTarget / 100);
			if (c0.Quote.Low <= fullExitPrice)
			{
				DcaExitPosition(shortPosition, c0, fullExitPrice, 1.0m);
				consecutiveLosses = 0;
				return;
			}

			// 5. 트레일링 스톱
			if (profitPercent > TrailingPercent)
			{
				var newStopLoss = c0.Quote.Low * (1 + TrailingPercent / 100);
				if (newStopLoss < shortPosition.StopLossPrice || shortPosition.StopLossPrice == 0)
				{
					shortPosition.StopLossPrice = newStopLoss;
				}
			}

			// 6. 빠른 청산 (완화)
			bool quickExit = c1.Cci <= -20m && profitPercent > 0.5m; // 0 → -20
			bool rsiExit = c1.Rsi1 <= 30m && profitPercent > 0.3m;   // 35 → 30

			if (quickExit || rsiExit)
			{
				DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
				consecutiveLosses = 0;
				return;
			}
		}
	}
}