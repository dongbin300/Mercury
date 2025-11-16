using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci24 - Back to Basics Plus Strategy
	/// 
	/// Cci23 베이스 + 최소한의 개선
	/// 
	/// === 핵심 철학 ===
	/// - Cci23의 단순함 유지 (검증된 성능)
	/// - 최소한의 개선만 추가
	/// - KISS 원칙 (Keep It Simple, Stupid)
	/// 
	/// === 주요 특징 ===
	/// - CCI 기반 극값 반전 전략 (핵심 유지)
	/// - 4단계 DCA + 적응형 청산 (Cci23 베이스)
	/// - 약간의 진입 조건 개선
	/// - 트레일링 스톱 미세 조정
	/// 
	/// === 기대 성과 ===
	/// - 연평균 300%+ 목표 (Cci23 수준)
	/// - MDD 30% 이하 유지
	/// - 단순하지만 효과적인 전략
	/// 
	/// </summary>
	public class Cci24(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		// === CCI 기본 파라미터 (Cci23 베이스) ===
		public int CciPeriod = 15;                   // CCI 계산 기간
		public decimal ExtremeLevelHigh = 200m;      // 과매수 극값 (Cci23과 동일)
		public decimal ExtremeLevelLow = -200m;      // 과매도 극값 (Cci23과 동일)
		public decimal ExitBuffer = 0m;              // 청산 버퍼

		// === DCA 설정 (Cci23 베이스) ===
		public int DcaMaxEntries = 4;                // 최대 분할 진입 횟수
		public decimal DcaStepPercent = 4.5m;        // DCA 진입 간격 (% 기반으로 복귀)
		public decimal DcaMultiplier = 1.6m;         // DCA 포지션 사이징 배수

		// === 청산 전략 설정 (Cci23 베이스) ===
		public decimal PartialExitPercent = 0.8m;    // 부분 청산 비율 (Cci23: 0.9 → 0.8로 약간 보수적)
		public decimal ProfitTarget = 1.3m;          // 부분 청산 수익 목표 (Cci23: 1.2 → 1.3으로 약간 상향)
		public decimal FullExitTarget = 4.2m;        // 전량 청산 수익 목표 (Cci23: 4.5 → 4.2로 약간 하향)

		// === 리스크 관리 설정 (Cci23 베이스) ===
		public decimal StopLossPercent = 4.2m;       // 기본 손절 비율 (Cci23: 4.5 → 4.2로 약간 타이트)
		public decimal TrailingPercent = 1.1m;       // 트레일링 스톱 비율 (Cci23: 1.2 → 1.1로 약간 타이트)

		/// <summary>
		/// 지표 초기화 - CCI만 사용 (단순함 유지)
		/// </summary>
		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = true;  // DCA 기능 활성화
			chartPack.UseCci(CciPeriod);  // CCI 지표만 사용 (Cci23과 동일)
		}

		/// <summary>
		/// 변동성 기반 동적 진입 조건 판단 (Cci23 스타일 단순화)
		/// 최근 10캔들의 평균 변동성이 3% 이상인지 확인
		/// </summary>
		private bool IsHighVolatility(List<ChartInfo> charts, int i)
		{
			if (i < 11) return false;
			
			var recent = charts.Skip(i - 10).Take(10).ToList();
			var avgRange = recent.Average(c => (c.Quote.High - c.Quote.Low) / c.Quote.Close * 100);
			
			return avgRange > 3.0m;
		}

		/// <summary>
		/// 트렌드 강도 측정 (Cci23 스타일)
		/// </summary>
		private decimal GetTrendStrength(List<ChartInfo> charts, int i)
		{
			if (i < 11) return 0;
			
			var recent = charts.Skip(i - 10).Take(10).ToList();
			var firstPrice = recent.First().Quote.Close;
			var lastPrice = recent.Last().Quote.Close;
			
			return (lastPrice - firstPrice) / firstPrice * 100;
		}

		/// <summary>
		/// 롱 포지션 진입 로직 (Cci23 베이스 + 미세 개선)
		/// </summary>
		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 11) return;
			
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			var existingPosition = GetActivePosition(symbol, PositionSide.Long);
			var isHighVol = IsHighVolatility(charts, i);
			var trendStrength = GetTrendStrength(charts, i);
			
			if (existingPosition == null)
			{
				// CCI 기본 조건 (Cci23과 동일)
				bool basicCondition = c3.Cci <= ExtremeLevelLow && 
									  c2.Cci > c3.Cci && 
									  c1.Cci > c2.Cci;

				// 약간의 추가 필터 (Cci23 스타일)
				bool enhancedCondition = basicCondition && 
										  (isHighVol || trendStrength > -2.0m);

				if (enhancedCondition)
				{
					var entry = c0.Quote.Open;
					var stopLoss = entry * (1 - StopLossPercent / 100);
					
					DcaEntryPosition(PositionSide.Long, c0, entry, DcaStepPercent, DcaMultiplier, stopLoss);
				}
			}
			else
			{
				// DCA 추가 진입 (% 기반으로 복귀)
				if (existingPosition.DcaStep < DcaMaxEntries)
				{
					var currentPrice = c0.Quote.Open;
					var entryPrice = existingPosition.EntryPrice;
					var dropPercent = (entryPrice - currentPrice) / entryPrice * 100;
					
					// 동적 DCA 스텝 (Cci23 스타일)
					var dynamicStep = isHighVol ? 
						DcaStepPercent * 0.8m : 
						DcaStepPercent;
					
					if (dropPercent >= dynamicStep * (existingPosition.DcaStep + 1))
					{
						var stopLoss = currentPrice * (1 - StopLossPercent / 100);
						DcaEntryPosition(PositionSide.Long, c0, currentPrice, DcaStepPercent, DcaMultiplier, stopLoss);
					}
				}
			}
		}

		/// <summary>
		/// 롱 포지션 청산 로직 (Cci23 베이스 + 미세 개선)
		/// </summary>
		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var currentPrice = c0.Quote.Open;
			var profitPercent = GetPositionProfitPercent(longPosition, currentPrice);
			var trendStrength = GetTrendStrength(charts, i);

			// 손절 조건
			if (longPosition.StopLossPrice > 0 && currentPrice <= longPosition.StopLossPrice)
			{
				DcaExitPosition(longPosition, c0, currentPrice, 1.0m);
				return;
			}

			// 적응형 트레일링 스톱
			if (profitPercent > TrailingPercent)
			{
				var dynamicTrailing = trendStrength > 5.0m ? 
					TrailingPercent * 0.7m : 
					TrailingPercent;
				
				var newStopLoss = currentPrice * (1 - dynamicTrailing / 100);
				if (newStopLoss > longPosition.StopLossPrice)
				{
					longPosition.StopLossPrice = newStopLoss;
				}
			}

			// 부분 청산
			if (profitPercent >= ProfitTarget && longPosition.Stage == 0)
			{
				var dynamicExitPercent = trendStrength > 3.0m ? 
					PartialExitPercent * 0.7m : 
					PartialExitPercent;
				
				DcaExitPosition(longPosition, c0, currentPrice, dynamicExitPercent);
				longPosition.Stage = 1;
				longPosition.StopLossPrice = longPosition.EntryPrice;
				return;
			}

			// 전량 청산
			if (profitPercent >= FullExitTarget)
			{
				DcaExitPosition(longPosition, c0, currentPrice, 1.0m);
				return;
			}

			// CCI 기반 청산
			if (c1.Cci >= 20m)
			{
				DcaExitPosition(longPosition, c0, currentPrice, 1.0m);
				return;
			}

			// 극값 청산
			if (c1.Cci >= ExtremeLevelHigh)
			{
				DcaExitPosition(longPosition, c0, currentPrice, 1.0m);
			}
		}

		/// <summary>
		/// 숏 포지션 진입 로직 (Cci23 베이스 + 미세 개선)
		/// </summary>
		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 11) return;
			
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			var existingPosition = GetActivePosition(symbol, PositionSide.Short);
			var isHighVol = IsHighVolatility(charts, i);
			var trendStrength = GetTrendStrength(charts, i);
			
			if (existingPosition == null)
			{
				bool basicCondition = c3.Cci >= ExtremeLevelHigh && 
									  c2.Cci < c3.Cci && 
									  c1.Cci < c2.Cci;

				bool enhancedCondition = basicCondition && 
										  (isHighVol || trendStrength < 2.0m);

				if (enhancedCondition)
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
					
					var dynamicStep = isHighVol ? 
						DcaStepPercent * 0.8m : 
						DcaStepPercent;
					
					if (risePercent >= dynamicStep * (existingPosition.DcaStep + 1))
					{
						var stopLoss = currentPrice * (1 + StopLossPercent / 100);
						DcaEntryPosition(PositionSide.Short, c0, currentPrice, DcaStepPercent, DcaMultiplier, stopLoss);
					}
				}
			}
		}

		/// <summary>
		/// 숏 포지션 청산 로직 (Cci23 베이스 + 미세 개선)
		/// </summary>
		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var currentPrice = c0.Quote.Open;
			var profitPercent = GetPositionProfitPercent(shortPosition, currentPrice);
			var trendStrength = GetTrendStrength(charts, i);

			// 손절 조건
			if (shortPosition.StopLossPrice > 0 && currentPrice >= shortPosition.StopLossPrice)
			{
				DcaExitPosition(shortPosition, c0, currentPrice, 1.0m);
				return;
			}

			// 적응형 트레일링 스톱
			if (profitPercent > TrailingPercent)
			{
				var dynamicTrailing = trendStrength < -5.0m ? 
					TrailingPercent * 0.7m : 
					TrailingPercent;
				
				var newStopLoss = currentPrice * (1 + dynamicTrailing / 100);
				if (newStopLoss < shortPosition.StopLossPrice || shortPosition.StopLossPrice == 0)
				{
					shortPosition.StopLossPrice = newStopLoss;
				}
			}

			// 부분 청산
			if (profitPercent >= ProfitTarget && shortPosition.Stage == 0)
			{
				var dynamicExitPercent = trendStrength < -3.0m ? 
					PartialExitPercent * 0.7m : 
					PartialExitPercent;
				
				DcaExitPosition(shortPosition, c0, currentPrice, dynamicExitPercent);
				shortPosition.Stage = 1;
				shortPosition.StopLossPrice = shortPosition.EntryPrice;
				return;
			}

			// 전량 청산
			if (profitPercent >= FullExitTarget)
			{
				DcaExitPosition(shortPosition, c0, currentPrice, 1.0m);
				return;
			}

			// CCI 기반 청산
			if (c1.Cci <= -20m)
			{
				DcaExitPosition(shortPosition, c0, currentPrice, 1.0m);
				return;
			}

			// 극값 청산
			if (c1.Cci <= ExtremeLevelLow)
			{
				DcaExitPosition(shortPosition, c0, currentPrice, 1.0m);
			}
		}
	}
}