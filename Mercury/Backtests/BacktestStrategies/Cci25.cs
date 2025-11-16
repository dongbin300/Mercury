using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci25 - Realistic Backtest Engine
	/// 
	/// Cci24 베이스 + 현실적인 손절/익절 로직 수정
	/// 
	/// === 핵심 철학 ===
	/// - Cci24의 기본 전략 유지
	/// - 백테스트의 신뢰도 향상
	/// - 장중 가격 변동(Intra-bar price movement)을 반영하여 손절/익절 로직을 현실적으로 수정
	/// 
	/// === 주요 변경점 ===
	/// - Exit 로직에서 Open 가격 대신 High/Low 가격을 사용하여 손절/익절을 판단
	/// - 트레일링 스톱 업데이트 기준을 High/Low로 변경하여 더 정확하게 수익을 보존
	/// 
	/// === 기대 성과 ===
	/// - Cci24 대비 MDD는 증가하고 수익률은 감소할 수 있으나, 실제 거래 환경과 유사한 결과를 제공
	/// 
	/// </summary>
	public class Cci25(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		// === CCI 기본 파라미터 (Cci24 베이스) ===
		public int CciPeriod = 15;                   // CCI 계산 기간
		public decimal ExtremeLevelHigh = 200m;      // 과매수 극값
		public decimal ExtremeLevelLow = -200m;      // 과매도 극값
		public decimal ExitBuffer = 0m;              // 청산 버퍼

		// === DCA 설정 (Cci24 베이스) ===
		public int DcaMaxEntries = 4;                // 최대 분할 진입 횟수
		public decimal DcaStepPercent = 4.5m;        // DCA 진입 간격 (%)
		public decimal DcaMultiplier = 1.6m;         // DCA 포지션 사이징 배수

		// === 청산 전략 설정 (Cci24 베이스) ===
		public decimal PartialExitPercent = 0.8m;    // 부분 청산 비율
		public decimal ProfitTarget = 1.3m;          // 부분 청산 수익 목표
		public decimal FullExitTarget = 4.2m;        // 전량 청산 수익 목표

		// === 리스크 관리 설정 (Cci24 베이스) ===
		public decimal StopLossPercent = 4.2m;       // 기본 손절 비율
		public decimal TrailingPercent = 1.1m;       // 트레일링 스톱 비율

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = true;
			chartPack.UseCci(CciPeriod);
		}

		private bool IsHighVolatility(List<ChartInfo> charts, int i)
		{
			if (i < 11) return false;
			var recent = charts.Skip(i - 10).Take(10).ToList();
			var avgRange = recent.Average(c => (c.Quote.High - c.Quote.Low) / c.Quote.Close * 100);
			return avgRange > 3.0m;
		}

		private decimal GetTrendStrength(List<ChartInfo> charts, int i)
		{
			if (i < 11) return 0;
			var recent = charts.Skip(i - 10).Take(10).ToList();
			var firstPrice = recent.First().Quote.Close;
			var lastPrice = recent.Last().Quote.Close;
			return (lastPrice - firstPrice) / firstPrice * 100;
		}

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
				bool basicCondition = c3.Cci <= ExtremeLevelLow && c2.Cci > c3.Cci && c1.Cci > c2.Cci;
				bool enhancedCondition = basicCondition && (isHighVol || trendStrength > -2.0m);

				if (enhancedCondition)
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

					if (dropPercent >= dynamicStep * (existingPosition.DcaStep + 1))
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

			// 1. 현실적인 손절 로직: 캔들의 저가(Low)가 손절 라인을 건드렸는지 확인
			if (longPosition.StopLossPrice > 0 && c0.Quote.Low <= longPosition.StopLossPrice)
			{
				DcaExitPosition(longPosition, c0, longPosition.StopLossPrice, 1.0m);
				return;
			}

			// 2. 현실적인 전량 익절 로직: 캔들의 고가(High)가 익절 라인을 건드렸는지 확인
			var fullExitPrice = longPosition.EntryPrice * (1 + FullExitTarget / 100);
			if (c0.Quote.High >= fullExitPrice)
			{
				DcaExitPosition(longPosition, c0, fullExitPrice, 1.0m);
				return;
			}

			// 3. 현실적인 부분 익절 로직: 캔들의 고가(High)가 부분 익절 라인을 건드렸는지 확인
			var partialExitPrice = longPosition.EntryPrice * (1 + ProfitTarget / 100);
			if (longPosition.Stage == 0 && c0.Quote.High >= partialExitPrice)
			{
				var trendStrength = GetTrendStrength(charts, i);
				var dynamicExitPercent = trendStrength > 3.0m ? PartialExitPercent * 0.7m : PartialExitPercent;
				DcaExitPosition(longPosition, c0, partialExitPrice, dynamicExitPercent);
				longPosition.Stage = 1;
				longPosition.StopLossPrice = longPosition.EntryPrice; // 손절을 본절로 설정
			}

			// 4. 트레일링 스톱 업데이트 (수익 보존)
			var profitPercent = GetPositionProfitPercent(longPosition, c0.Quote.Close); // 현재가 대신 종가 기준으로 수익률 계산
			if (profitPercent > TrailingPercent)
			{
				var trendStrength = GetTrendStrength(charts, i);
				var dynamicTrailing = trendStrength > 5.0m ? TrailingPercent * 0.7m : TrailingPercent;
				var newStopLoss = c0.Quote.High * (1 - dynamicTrailing / 100); // 고점 기준으로 스톱 갱신
				if (newStopLoss > longPosition.StopLossPrice)
				{
					longPosition.StopLossPrice = newStopLoss;
				}
			}

			// 5. CCI 지표 기반 청산 (다음 캔들 시가에 청산)
			if (c1.Cci >= 20m || c1.Cci >= ExtremeLevelHigh)
			{
				DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
				return;
			}
		}

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
				bool basicCondition = c3.Cci >= ExtremeLevelHigh && c2.Cci < c3.Cci && c1.Cci < c2.Cci;
				bool enhancedCondition = basicCondition && (isHighVol || trendStrength < 2.0m);

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
					var dynamicStep = isHighVol ? DcaStepPercent * 0.8m : DcaStepPercent;

					if (risePercent >= dynamicStep * (existingPosition.DcaStep + 1))
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

			// 1. 현실적인 손절 로직: 캔들의 고가(High)가 손절 라인을 건드렸는지 확인
			if (shortPosition.StopLossPrice > 0 && c0.Quote.High >= shortPosition.StopLossPrice)
			{
				DcaExitPosition(shortPosition, c0, shortPosition.StopLossPrice, 1.0m);
				return;
			}

			// 2. 현실적인 전량 익절 로직: 캔들의 저가(Low)가 익절 라인을 건드렸는지 확인
			var fullExitPrice = shortPosition.EntryPrice * (1 - FullExitTarget / 100);
			if (c0.Quote.Low <= fullExitPrice)
			{
				DcaExitPosition(shortPosition, c0, fullExitPrice, 1.0m);
				return;
			}

			// 3. 현실적인 부분 익절 로직: 캔들의 저가(Low)가 부분 익절 라인을 건드렸는지 확인
			var partialExitPrice = shortPosition.EntryPrice * (1 - ProfitTarget / 100);
			if (shortPosition.Stage == 0 && c0.Quote.Low <= partialExitPrice)
			{
				var trendStrength = GetTrendStrength(charts, i);
				var dynamicExitPercent = trendStrength < -3.0m ? PartialExitPercent * 0.7m : PartialExitPercent;
				DcaExitPosition(shortPosition, c0, partialExitPrice, dynamicExitPercent);
				shortPosition.Stage = 1;
				shortPosition.StopLossPrice = shortPosition.EntryPrice; // 손절을 본절로 설정
			}

			// 4. 트레일링 스톱 업데이트 (수익 보존)
			var profitPercent = GetPositionProfitPercent(shortPosition, c0.Quote.Close); // 현재가 대신 종가 기준으로 수익률 계산
			if (profitPercent > TrailingPercent)
			{
				var trendStrength = GetTrendStrength(charts, i);
				var dynamicTrailing = trendStrength < -5.0m ? TrailingPercent * 0.7m : TrailingPercent;
				var newStopLoss = c0.Quote.Low * (1 + dynamicTrailing / 100); // 저점 기준으로 스톱 갱신
				if (newStopLoss < shortPosition.StopLossPrice || shortPosition.StopLossPrice == 0)
				{
					shortPosition.StopLossPrice = newStopLoss;
				}
			}

			// 5. CCI 지표 기반 청산 (다음 캔들 시가에 청산)
			if (c1.Cci <= -20m || c1.Cci <= ExtremeLevelLow)
			{
				DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
				return;
			}
		}
	}
}