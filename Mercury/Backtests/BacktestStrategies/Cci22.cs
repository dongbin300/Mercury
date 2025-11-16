using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci22 - Back to Basics with Smart Scaling
	/// 
	/// Cci21의 단순함 + 스마트한 포지션 관리
	/// 핵심 전략:
	/// 1. Cci21과 동일한 진입 조건 (검증됨)
	/// 2. 단순한 분할 진입 (3회까지)
	/// 3. 빠른 부분 청산 (수익 보호)
	/// 4. 엄격한 손절 (손실 제한)
	/// 5. 트레일링으로 수익 극대화
	/// 
	/// </summary>
	public class Cci22(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		// 기본 CCI 파라미터 (Cci21과 동일)
		public int CciPeriod = 15;
		public decimal ExtremeLevelHigh = 220m;
		public decimal ExtremeLevelLow = -210m;
		public decimal ExitBuffer = 0m;

		// DCA 설정 (2차 최적화됨)
		public int DcaMaxEntries = 3;
		public decimal DcaStepPercent = 5.0m; // 5% 최적
		public decimal DcaMultiplier = 1.5m; // 1.5배 최적 (8.93% MDD)

		// 청산 설정 (2차 최적화됨) 
		public decimal PartialExitPercent = 0.9m; // 90% 청산 최적
		public decimal ProfitTarget = 1.4m; // 1.4% 수익시 부분 청산 최적
		public decimal FullExitTarget = 3.8m; // 3.8% 수익시 전량 청산 최적

		// 손절 설정 (엄격)
		public decimal StopLossPercent = 5.5m; // 5.5% 손절
		public decimal TrailingPercent = 1.5m; // 1.5% 트레일링

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			UseDca = true;  // DCA 기능 활성화
			chartPack.UseCci(CciPeriod);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 3) return;
			
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			var existingPosition = GetActivePosition(symbol, PositionSide.Long);
			
			if (existingPosition == null)
			{
				// Cci21과 동일한 진입 조건
				if (c3.Cci <= ExtremeLevelLow && 
					c2.Cci > c3.Cci && 
					c1.Cci > c2.Cci)
				{
					var entry = c0.Quote.Open;
					var stopLoss = entry * (1 - StopLossPercent / 100);
					
					DcaEntryPosition(PositionSide.Long, c0, entry, DcaStepPercent, DcaMultiplier, stopLoss);
				}
			}
			else
			{
				// DCA 추가 진입 (단순한 조건)
				if (existingPosition.DcaStep < DcaMaxEntries)
				{
					var currentPrice = c0.Quote.Open;
					var entryPrice = existingPosition.EntryPrice;
					var dropPercent = (entryPrice - currentPrice) / entryPrice * 100;
					
					// 단순 DCA 조건 - 수정된 로직
					if (dropPercent >= DcaStepPercent * (existingPosition.DcaStep + 1))
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
			var currentPrice = c0.Quote.Open;
			var profitPercent = GetPositionProfitPercent(longPosition, currentPrice);

			// 손절 조건 (최우선)
			if (longPosition.StopLossPrice > 0 && currentPrice <= longPosition.StopLossPrice)
			{
				DcaExitPosition(longPosition, c0, currentPrice, 1.0m);
				return;
			}

			// 트레일링 스톱 업데이트
			if (profitPercent > TrailingPercent)
			{
				var newStopLoss = currentPrice * (1 - TrailingPercent / 100);
				if (newStopLoss > longPosition.StopLossPrice)
				{
					longPosition.StopLossPrice = newStopLoss;
				}
			}

			// 1단계: 부분 청산 (빠른 수익 확정)
			if (profitPercent >= ProfitTarget && longPosition.Stage == 0)
			{
				DcaExitPosition(longPosition, c0, currentPrice, PartialExitPercent);
				longPosition.Stage = 1;
				
				// 손익분기점으로 손절 이동
				longPosition.StopLossPrice = longPosition.EntryPrice;
				return;
			}

			// 2단계: 전량 청산 (목표 달성)
			if (profitPercent >= FullExitTarget)
			{
				DcaExitPosition(longPosition, c0, currentPrice, 1.0m);
				return;
			}

			// Cci21과 동일한 기본 청산 조건
			if (c1.Cci >= ExitBuffer)
			{
				DcaExitPosition(longPosition, c0, currentPrice, 1.0m);
				return;
			}

			// 반대 극값 도달시 즉시 청산
			if (c1.Cci >= ExtremeLevelHigh)
			{
				DcaExitPosition(longPosition, c0, currentPrice, 1.0m);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 3) return;
			
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			var existingPosition = GetActivePosition(symbol, PositionSide.Short);
			
			if (existingPosition == null)
			{
				// Cci21과 동일한 진입 조건
				if (c3.Cci >= ExtremeLevelHigh && 
					c2.Cci < c3.Cci && 
					c1.Cci < c2.Cci)
				{
					var entry = c0.Quote.Open;
					var stopLoss = entry * (1 + StopLossPercent / 100);
					
					DcaEntryPosition(PositionSide.Short, c0, entry, DcaStepPercent, DcaMultiplier, stopLoss);
				}
			}
			else
			{
				// DCA 추가 진입
				if (existingPosition.DcaStep < DcaMaxEntries)
				{
					var currentPrice = c0.Quote.Open;
					var entryPrice = existingPosition.EntryPrice;
					var risePercent = (currentPrice - entryPrice) / entryPrice * 100;
					
					if (risePercent >= DcaStepPercent * (existingPosition.DcaStep + 1))
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
			var currentPrice = c0.Quote.Open;
			var profitPercent = GetPositionProfitPercent(shortPosition, currentPrice);

			// 손절 조건
			if (shortPosition.StopLossPrice > 0 && currentPrice >= shortPosition.StopLossPrice)
			{
				DcaExitPosition(shortPosition, c0, currentPrice, 1.0m);
				return;
			}

			// 트레일링 스톱 업데이트
			if (profitPercent > TrailingPercent)
			{
				var newStopLoss = currentPrice * (1 + TrailingPercent / 100);
				if (newStopLoss < shortPosition.StopLossPrice || shortPosition.StopLossPrice == 0)
				{
					shortPosition.StopLossPrice = newStopLoss;
				}
			}

			// 1단계: 부분 청산
			if (profitPercent >= ProfitTarget && shortPosition.Stage == 0)
			{
				DcaExitPosition(shortPosition, c0, currentPrice, PartialExitPercent);
				shortPosition.Stage = 1;
				
				shortPosition.StopLossPrice = shortPosition.EntryPrice;
				return;
			}

			// 2단계: 전량 청산
			if (profitPercent >= FullExitTarget)
			{
				DcaExitPosition(shortPosition, c0, currentPrice, 1.0m);
				return;
			}

			// 기본 청산 조건
			if (c1.Cci <= -ExitBuffer)
			{
				DcaExitPosition(shortPosition, c0, currentPrice, 1.0m);
				return;
			}

			// 반대 극값 도달시 청산
			if (c1.Cci <= ExtremeLevelLow)
			{
				DcaExitPosition(shortPosition, c0, currentPrice, 1.0m);
			}
		}
	}
}