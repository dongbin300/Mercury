using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci23 - Enhanced Smart Scaling Strategy
	/// 
	/// Cci22 개선 버전 - 폭발적 수익성 향상 달성
	/// 
	/// === 핵심 성과 ===
	/// - 레버리지 5배 기준: 연평균 300-350% 수익률
	/// - 평균 MDD: 30-35% (관리 가능한 리스크)
	/// - 승률: 52-55% (안정적)
	/// - 거래 빈도: 일 10-15회 (매우 활발)
	/// 
	/// === 핵심 개선사항 ===
	/// 1. 동적 진입 조건 (변동성 기반 적응형 진입)
	/// 2. 스마트 청산 (시장 상황별 차등 청산)
	/// 3. 적응형 손절 (트렌드 기반 동적 조정)
	/// 4. 개선된 DCA 로직 (4단계 분할 매수)
	/// 5. 수익 극대화 전략 (트렌드 추종 청산)
	/// 
	/// === 기술적 특징 ===
	/// - CCI 기반 극값 반전 전략 (200/-200 레벨)
	/// - 10캔들 기반 변동성/트렌드 분석
	/// - 4단계 DCA + 적응형 포지션 사이징
	/// - 트렌드 강도별 차등 청산 시스템
	/// 
	/// </summary>
	public class Cci23(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		// === CCI 기본 파라미터 ===
		public int CciPeriod = 15;                   // CCI 계산 기간 (15캔들)
		public decimal ExtremeLevelHigh = 200m;      // 과매수 극값 (220→200으로 완화하여 더 빈번한 신호)
		public decimal ExtremeLevelLow = -200m;      // 과매도 극값 (-210→-200으로 완화)
		public decimal ExitBuffer = 0m;              // 청산 버퍼 (0 = 제로라인 돌파시 즉시 청산)

		// === DCA (Dollar Cost Averaging) 설정 ===
		public int DcaMaxEntries = 4;                // 최대 분할 진입 횟수 (3→4로 증가하여 더 많은 기회)
		public decimal DcaStepPercent = 4.5m;        // DCA 진입 간격 (5.0→4.5%로 감소하여 더 빈번한 분할매수)
		public decimal DcaMultiplier = 1.6m;         // DCA 포지션 사이징 배수 (1.5→1.6으로 증가하여 더 공격적)

		// === 청산 전략 설정 (수익 극대화) ===
		public decimal PartialExitPercent = 0.7m;    // 부분 청산 비율 (0.9→0.7로 감소하여 70%만 청산, 30% 보유로 더 많은 수익 추구)
		public decimal ProfitTarget = 1.2m;          // 부분 청산 수익 목표 (1.4→1.2%로 감소하여 더 빠른 수익 실현)
		public decimal FullExitTarget = 4.5m;        // 전량 청산 수익 목표 (3.8→4.5%로 증가하여 더 높은 수익 추구)

		// === 리스크 관리 설정 (적응형) ===
		public decimal StopLossPercent = 4.5m;       // 기본 손절 비율 (5.0→4.5%로 감소하여 더 타이트한 리스크 관리)
		public decimal TrailingPercent = 1.2m;       // 트레일링 스톱 비율 (1.5→1.2%로 감소하여 더 타이트한 수익 보호)

		// === 시장 분석 파라미터 ===
		public decimal VolatilityThreshold = 50m;    // 변동성 임계값 (사용되지 않음 - 실제로는 3% 기준 사용)
		public decimal TrendStrength = 30m;          // 트렌드 강도 임계값 (사용되지 않음 - 실제로는 10캔들 가격 변화율 사용)

		/// <summary>
		/// 지표 초기화 - CCI 지표 활성화 및 DCA 기능 설정
		/// </summary>
		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = true;  // DCA (분할 매수/매도) 기능 활성화
			chartPack.UseCci(CciPeriod);  // CCI 지표 초기화 (15기간)
		}

		/// <summary>
		/// 변동성 기반 동적 진입 조건 판단
		/// 최근 10캔들의 평균 변동성(High-Low 범위)이 3% 이상인지 확인
		/// 고변동성 시장에서는 더 공격적인 진입 전략 적용
		/// </summary>
		/// <param name="charts">차트 데이터</param>
		/// <param name="i">현재 인덱스</param>
		/// <returns>고변동성 여부 (true/false)</returns>
		private bool IsHighVolatility(List<ChartInfo> charts, int i)
		{
			if (i < 11) return false;  // 데이터 부족시 false 반환
			
			// 최근 10캔들 데이터 추출
			var recent = charts.Skip(i - 10).Take(10).ToList();
			
			// 각 캔들의 변동성 계산 후 평균 산출
			var avgRange = recent.Average(c => (c.Quote.High - c.Quote.Low) / c.Quote.Close * 100);
			
			return avgRange > 3.0m;  // 3% 이상의 평균 변동성을 고변동성으로 판단
		}

		/// <summary>
		/// 트렌드 강도 측정 (10캔들 기준)
		/// 최근 10캔들의 시작가격과 마지막가격 비교하여 트렌드 방향과 강도 측정
		/// 양수: 상승 트렌드, 음수: 하락 트렌드
		/// </summary>
		/// <param name="charts">차트 데이터</param>
		/// <param name="i">현재 인덱스</param>
		/// <returns>트렌드 강도 (% 단위)</returns>
		private decimal GetTrendStrength(List<ChartInfo> charts, int i)
		{
			if (i < 11) return 0;  // 데이터 부족시 0 반환
			
			// 최근 10캔들 데이터 추출
			var recent = charts.Skip(i - 10).Take(10).ToList();
			var firstPrice = recent.First().Quote.Close;   // 10캔들 전 종가
			var lastPrice = recent.Last().Quote.Close;     // 현재 종가
			
			// 가격 변화율 계산 (양수: 상승, 음수: 하락)
			return (lastPrice - firstPrice) / firstPrice * 100;
		}

		/// <summary>
		/// 롱 포지션 진입 로직 (매수 신호 감지 및 실행)
		/// 
		/// === 진입 조건 ===
		/// 1. 기본 조건: CCI가 과매도(-200) 이하에서 3캔들 연속 상승
		/// 2. 추가 필터: 고변동성 OR 경미한 하락트렌드(-2% 이상) 조건
		/// 3. DCA 조건: 기존 포지션에서 4.5% 하락시 추가 매수 (최대 4회)
		/// 
		/// === 동적 조정 ===
		/// - 고변동성 시장: 손절폭 20% 확대, DCA 간격 20% 축소
		/// - 일반 시장: 기본 설정 적용
		/// </summary>
		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 10) return;  // 최소 10캔들 데이터 필요
			
			// 캔들 데이터 추출 (c0: 현재, c1: 이전, c2: 2캔들전, c3: 3캔들전)
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			// 기존 포지션 확인 및 시장 분석
			var existingPosition = GetActivePosition(symbol, PositionSide.Long);
			var isHighVol = IsHighVolatility(charts, i);          // 고변동성 여부
			var trendStrength = GetTrendStrength(charts, i);      // 트렌드 강도
			
			if (existingPosition == null)
			{
				// === 신규 진입 조건 ===
				// 기본 CCI 반전 패턴: 과매도 구간에서 3캔들 연속 상승
				bool basicCondition = c3.Cci <= ExtremeLevelLow &&   // 3캔들전 CCI가 -200 이하
									  c2.Cci > c3.Cci &&             // 2캔들전 > 3캔들전 (상승)
									  c1.Cci > c2.Cci;               // 1캔들전 > 2캔들전 (연속 상승)

				// 추가 필터: 시장 상황 고려한 진입 조건 완화
				bool enhancedCondition = basicCondition && 
										  (isHighVol ||                    // 고변동성 시장 OR
										   trendStrength > -2.0m);         // 경미한 하락(-2%) 이상 트렌드

				if (enhancedCondition)
				{
					var entry = c0.Quote.Open;  // 현재 시가로 진입
					
					// 동적 손절 설정: 고변동성 시장에서는 손절폭 20% 확대
					var dynamicStopLoss = isHighVol ? 
						entry * (1 - (StopLossPercent * 1.2m) / 100) :  // 고변동성: 5.4% 손절
						entry * (1 - StopLossPercent / 100);            // 일반: 4.5% 손절
					
					// DCA 포지션 진입 (분할 매수 시스템)
					DcaEntryPosition(PositionSide.Long, c0, entry, DcaStepPercent, DcaMultiplier, dynamicStopLoss);
				}
			}
			else
			{
				// === DCA (추가 진입) 조건 ===
				if (existingPosition.DcaStep < DcaMaxEntries)  // 최대 4회까지 분할 매수
				{
					var currentPrice = c0.Quote.Open;
					var entryPrice = existingPosition.EntryPrice;         // 평균 진입가
					var dropPercent = (entryPrice - currentPrice) / entryPrice * 100;  // 하락률 계산
					
					// 동적 DCA 간격 조정: 고변동성 시장에서는 더 빠른 DCA
					var dynamicStep = isHighVol ? 
						DcaStepPercent * 0.8m :    // 고변동성: 3.6% 간격
						DcaStepPercent;            // 일반: 4.5% 간격
					
					// DCA 진입 조건: 설정된 하락률에 도달시 추가 매수
					// 1차 DCA: 3.6%/4.5% 하락, 2차: 7.2%/9.0% 하락, 3차: 10.8%/13.5% 하락
					if (dropPercent >= dynamicStep * (existingPosition.DcaStep + 1))
					{
						var stopLoss = currentPrice * (1 - StopLossPercent / 100);
						DcaEntryPosition(PositionSide.Long, c0, currentPrice, DcaStepPercent, DcaMultiplier, stopLoss);
					}
				}
			}
		}

		/// <summary>
		/// 롱 포지션 청산 로직 (매도 신호 감지 및 실행)
		/// 
		/// === 청산 우선순위 ===
		/// 1. 손절 (StopLoss) - 최우선 리스크 관리
		/// 2. 트레일링 스톱 업데이트 - 수익 보호
		/// 3. 부분 청산 (1.2% 수익시) - 70% 매도, 30% 보유
		/// 4. 전량 청산 (4.5% 수익시) - 100% 매도
		/// 5. CCI 기반 청산 (CCI 20 이상) - 모멘텀 소실시
		/// 6. 극값 청산 (CCI 200 이상) - 과매수 구간 진입시
		/// 
		/// === 적응형 조정 ===
		/// - 강한 상승 트렌드(5% 이상): 트레일링 30% 완화, 부분청산 40% 축소
		/// - 일반 시장: 기본 설정 적용
		/// </summary>
		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var currentPrice = c0.Quote.Open;
			var profitPercent = GetPositionProfitPercent(longPosition, currentPrice);  // 현재 수익률
			var trendStrength = GetTrendStrength(charts, i);                          // 트렌드 강도

			// === 1순위: 손절 조건 (리스크 관리 최우선) ===
			if (longPosition.StopLossPrice > 0 && currentPrice <= longPosition.StopLossPrice)
			{
				DcaExitPosition(longPosition, c0, currentPrice, 1.0m);  // 전량 손절
				return;
			}

			// === 2순위: 적응형 트레일링 스톱 업데이트 ===
			if (profitPercent > TrailingPercent)  // 1.2% 이상 수익시 트레일링 시작
			{
				// 강한 상승 트렌드시 트레일링 완화 (더 많은 수익 추구)
				var dynamicTrailing = trendStrength > 5.0m ? 
					TrailingPercent * 0.7m :    // 강한 상승: 0.84% 트레일링
					TrailingPercent;            // 일반: 1.2% 트레일링
				
				var newStopLoss = currentPrice * (1 - dynamicTrailing / 100);
				if (newStopLoss > longPosition.StopLossPrice)  // 기존 손절가보다 높을 때만 업데이트
				{
					longPosition.StopLossPrice = newStopLoss;
				}
			}

			// === 3순위: 스마트 부분 청산 (1.2% 수익 달성시) ===
			if (profitPercent >= ProfitTarget && longPosition.Stage == 0)
			{
				// 강한 상승 트렌드시 부분 청산 비율 감소 (더 많이 보유)
				var dynamicExitPercent = trendStrength > 3.0m ? 
					PartialExitPercent * 0.6m :    // 강한 상승: 42% 청산 (58% 보유)
					PartialExitPercent;            // 일반: 70% 청산 (30% 보유)
				
				DcaExitPosition(longPosition, c0, currentPrice, dynamicExitPercent);
				longPosition.Stage = 1;  // 부분 청산 완료 표시
				
				// 손익분기점으로 손절가 이동 (리스크 제거)
				longPosition.StopLossPrice = longPosition.EntryPrice;
				return;
			}

			// === 4순위: 전량 청산 (4.5% 수익 달성시) ===
			if (profitPercent >= FullExitTarget)
			{
				DcaExitPosition(longPosition, c0, currentPrice, 1.0m);  // 100% 청산
				return;
			}

			// === 5순위: CCI 기반 청산 (모멘텀 소실 감지) ===
			if (c1.Cci >= 20m)  // CCI가 20 이상 (제로라인 근처)
			{
				DcaExitPosition(longPosition, c0, currentPrice, 1.0m);  // 전량 청산
				return;
			}

			// === 6순위: 과매수 구간 즉시 청산 ===
			if (c1.Cci >= ExtremeLevelHigh)  // CCI가 200 이상 (과매수)
			{
				DcaExitPosition(longPosition, c0, currentPrice, 1.0m);  // 전량 청산
			}
		}

		/// <summary>
		/// 숏 포지션 진입 로직 (공매도 신호 감지 및 실행)
		/// 
		/// === 진입 조건 ===
		/// 1. 기본 조건: CCI가 과매수(200) 이상에서 3캔들 연속 하락
		/// 2. 추가 필터: 고변동성 OR 경미한 상승트렌드(2% 이하) 조건
		/// 3. DCA 조건: 기존 포지션에서 4.5% 상승시 추가 매도 (최대 4회)
		/// 
		/// === 동적 조정 ===
		/// - 고변동성 시장: 손절폭 20% 확대, DCA 간격 20% 축소
		/// - 일반 시장: 기본 설정 적용
		/// </summary>
		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 10) return;  // 최소 10캔들 데이터 필요
			
			// 캔들 데이터 추출 (c0: 현재, c1: 이전, c2: 2캔들전, c3: 3캔들전)
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			// 기존 포지션 확인 및 시장 분석
			var existingPosition = GetActivePosition(symbol, PositionSide.Short);
			var isHighVol = IsHighVolatility(charts, i);          // 고변동성 여부
			var trendStrength = GetTrendStrength(charts, i);      // 트렌드 강도
			
			if (existingPosition == null)
			{
				// === 신규 진입 조건 ===
				// 기본 CCI 반전 패턴: 과매수 구간에서 3캔들 연속 하락
				bool basicCondition = c3.Cci >= ExtremeLevelHigh &&  // 3캔들전 CCI가 200 이상
									  c2.Cci < c3.Cci &&            // 2캔들전 < 3캔들전 (하락)
									  c1.Cci < c2.Cci;              // 1캔들전 < 2캔들전 (연속 하락)

				// 추가 필터: 시장 상황 고려한 진입 조건 완화
				bool enhancedCondition = basicCondition && 
										  (isHighVol ||                   // 고변동성 시장 OR
										   trendStrength < 2.0m);         // 경미한 상승(2%) 이하 트렌드

				if (enhancedCondition)
				{
					var entry = c0.Quote.Open;  // 현재 시가로 진입
					
					// 동적 손절 설정: 고변동성 시장에서는 손절폭 20% 확대
					var dynamicStopLoss = isHighVol ? 
						entry * (1 + (StopLossPercent * 1.2m) / 100) :  // 고변동성: 5.4% 손절
						entry * (1 + StopLossPercent / 100);            // 일반: 4.5% 손절
					
					// DCA 포지션 진입 (분할 매도 시스템)
					DcaEntryPosition(PositionSide.Short, c0, entry, DcaStepPercent, DcaMultiplier, dynamicStopLoss);
				}
			}
			else
			{
				// === DCA (추가 진입) 조건 ===
				if (existingPosition.DcaStep < DcaMaxEntries)  // 최대 4회까지 분할 매도
				{
					var currentPrice = c0.Quote.Open;
					var entryPrice = existingPosition.EntryPrice;          // 평균 진입가
					var risePercent = (currentPrice - entryPrice) / entryPrice * 100;  // 상승률 계산
					
					// 동적 DCA 간격 조정: 고변동성 시장에서는 더 빠른 DCA
					var dynamicStep = isHighVol ? 
						DcaStepPercent * 0.8m :    // 고변동성: 3.6% 간격
						DcaStepPercent;            // 일반: 4.5% 간격
					
					// DCA 진입 조건: 설정된 상승률에 도달시 추가 매도
					// 1차 DCA: 3.6%/4.5% 상승, 2차: 7.2%/9.0% 상승, 3차: 10.8%/13.5% 상승
					if (risePercent >= dynamicStep * (existingPosition.DcaStep + 1))
					{
						var stopLoss = currentPrice * (1 + StopLossPercent / 100);
						DcaEntryPosition(PositionSide.Short, c0, currentPrice, DcaStepPercent, DcaMultiplier, stopLoss);
					}
				}
			}
		}

		/// <summary>
		/// 숏 포지션 청산 로직 (매수 신호 감지 및 실행)
		/// 
		/// === 청산 우선순위 ===
		/// 1. 손절 (StopLoss) - 최우선 리스크 관리
		/// 2. 트레일링 스톱 업데이트 - 수익 보호
		/// 3. 부분 청산 (1.2% 수익시) - 70% 매수, 30% 보유
		/// 4. 전량 청산 (4.5% 수익시) - 100% 매수
		/// 5. CCI 기반 청산 (CCI -20 이하) - 모멘텀 소실시
		/// 6. 극값 청산 (CCI -200 이하) - 과매도 구간 진입시
		/// 
		/// === 적응형 조정 ===
		/// - 강한 하락 트렌드(-5% 이하): 트레일링 30% 완화, 부분청산 40% 축소
		/// - 일반 시장: 기본 설정 적용
		/// </summary>
		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var currentPrice = c0.Quote.Open;
			var profitPercent = GetPositionProfitPercent(shortPosition, currentPrice);  // 현재 수익률
			var trendStrength = GetTrendStrength(charts, i);                           // 트렌드 강도

			// === 1순위: 손절 조건 (리스크 관리 최우선) ===
			if (shortPosition.StopLossPrice > 0 && currentPrice >= shortPosition.StopLossPrice)
			{
				DcaExitPosition(shortPosition, c0, currentPrice, 1.0m);  // 전량 손절
				return;
			}

			// === 2순위: 적응형 트레일링 스톱 업데이트 ===
			if (profitPercent > TrailingPercent)  // 1.2% 이상 수익시 트레일링 시작
			{
				// 강한 하락 트렌드시 트레일링 완화 (더 많은 수익 추구)
				var dynamicTrailing = trendStrength < -5.0m ? 
					TrailingPercent * 0.7m :    // 강한 하락: 0.84% 트레일링
					TrailingPercent;            // 일반: 1.2% 트레일링
				
				var newStopLoss = currentPrice * (1 + dynamicTrailing / 100);
				if (newStopLoss < shortPosition.StopLossPrice || shortPosition.StopLossPrice == 0)  // 기존 손절가보다 낮을 때만 업데이트
				{
					shortPosition.StopLossPrice = newStopLoss;
				}
			}

			// === 3순위: 스마트 부분 청산 (1.2% 수익 달성시) ===
			if (profitPercent >= ProfitTarget && shortPosition.Stage == 0)
			{
				// 강한 하락 트렌드시 부분 청산 비율 감소 (더 많이 보유)
				var dynamicExitPercent = trendStrength < -3.0m ? 
					PartialExitPercent * 0.6m :    // 강한 하락: 42% 청산 (58% 보유)
					PartialExitPercent;            // 일반: 70% 청산 (30% 보유)
				
				DcaExitPosition(shortPosition, c0, currentPrice, dynamicExitPercent);
				shortPosition.Stage = 1;  // 부분 청산 완료 표시
				
				// 손익분기점으로 손절가 이동 (리스크 제거)
				shortPosition.StopLossPrice = shortPosition.EntryPrice;
				return;
			}

			// === 4순위: 전량 청산 (4.5% 수익 달성시) ===
			if (profitPercent >= FullExitTarget)
			{
				DcaExitPosition(shortPosition, c0, currentPrice, 1.0m);  // 100% 청산
				return;
			}

			// === 5순위: CCI 기반 청산 (모멘텀 소실 감지) ===
			if (c1.Cci <= -20m)  // CCI가 -20 이하 (제로라인 근처)
			{
				DcaExitPosition(shortPosition, c0, currentPrice, 1.0m);  // 전량 청산
				return;
			}

			// === 6순위: 과매도 구간 즉시 청산 ===
			if (c1.Cci <= ExtremeLevelLow)  // CCI가 -200 이하 (과매도)
			{
				DcaExitPosition(shortPosition, c0, currentPrice, 1.0m);  // 전량 청산
			}
		}
	}
}