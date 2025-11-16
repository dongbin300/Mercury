using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

using System;
using System.Collections.Generic;
using System.Text;

namespace Mercury.Backtests.BacktestStrategies
{
    /// <summary>
    /// HybridStrategy5 - 이익잉여금 활용 전략
    /// 
    /// 그리드 수익을 DCA 손절 보험으로 활용
    /// - 그리드 전략으로 발생한 수익의 일정 비율을 별도 적립
    /// - 시장 급락 시 적립금으로 DCA 추가 매수력 확보
    /// - 손절 임계점에서 적립금 활용하여 평단가 개선
    /// 
    /// === 파라미터 테스트 범위 ===
    /// GridSpacing: 0.5 ~ 1.5 (0.25 step)
    /// ReserveRate: 0.2 ~ 0.5 (0.1 step)
    /// CrisisThreshold: 5.0 ~ 15.0 (2.5 step)
    /// ReserveMultiplier: 1.5 ~ 3.0 (0.5 step)
    /// GridTarget: 1.0 ~ 2.5 (0.5 step)
    /// </summary>
    public class HybridStrategy5(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals)
        : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
    {
        // === 그리드 기본 파라미터 ===
        public decimal GridSpacing = 1.0m;           // 그리드 간격 (%)
        public decimal GridTarget = 1.5m;            // 그리드 익절 목표 (%)
        public int MaxGridCount = 5;

        // === 적립금 관리 ===
        public decimal ReserveRate = 0.3m;           // 수익의 30%를 적립
        public decimal MinReserveToUse = 0.05m;      // 최소 적립금 (시작자금 대비 5%)
        private decimal reserveFund = 0m;            // 적립금 누적액
        private decimal totalGridProfit = 0m;        // 그리드 총 수익

        // === 위기 대응 파라미터 ===
        public decimal CrisisThreshold = 10.0m;      // 위기 판단 기준 (손실 %)
        public decimal ReserveMultiplier = 2.0m;     // 적립금 사용시 배수
        public decimal EmergencyDcaSpacing = 5.0m;   // 긴급 DCA 간격 (%)
        public int MaxEmergencyDca = 3;              // 최대 긴급 DCA 횟수

        // === 진입 조건 ===
        public int CciPeriod = 14;
        public int RsiPeriod = 14;

        // === 리스크 관리 ===
        public decimal BaseStopLoss = 3.0m;          // 기본 손절 (%)
        public decimal FinalStopLoss = 20.0m;        // 최종 손절선 (%)

        protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
        {
            UseDca = true;
            chartPack.UseCci(CciPeriod);
            chartPack.UseRsi(RsiPeriod);
        }

        private bool IsInCrisis(Position position, decimal currentPrice)
        {
            if (position == null) return false;
            var profitPercent = GetPositionProfitPercent(position, currentPrice);
            return profitPercent < -CrisisThreshold;
        }

        private bool CanUseReserve()
        {
            var minReserve = Seed * MinReserveToUse;
            return reserveFund >= minReserve;
        }

        private void AddToReserve(decimal profit)
        {
            if (profit > 0)
            {
                var reserve = profit * ReserveRate;
                reserveFund += reserve;
                totalGridProfit += profit;
            }
        }

        private decimal UseReserve(decimal amount)
        {
            var useAmount = Math.Min(amount, reserveFund);
            reserveFund -= useAmount;
            return useAmount;
        }

        protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < 10) return;

            var c0 = charts[i];
            var position = GetActivePosition(symbol, PositionSide.Long);
            var inCrisis = IsInCrisis(position, c0.Quote.Close);

            if (position == null)
            {
                // 신규 진입 - 그리드 모드
                if (c0.Cci <= -100m && c0.Rsi1 <= 35m)
                {
                    var entry = c0.Quote.Open;
                    var stopLoss = entry * (1 - FinalStopLoss / 100);
                    DcaEntryPosition(PositionSide.Long, c0, entry, 0m, 1.0m, stopLoss);
                }
            }
            else
            {
                var avgPrice = position.EntryPrice;

                if (!inCrisis)
                {
                    // 일반 그리드 운영
                    if (position.DcaStep < MaxGridCount)
                    {
                        var targetPrice = avgPrice * (1 - GridSpacing / 100);
                        if (c0.Quote.Low <= targetPrice)
                        {
                            DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, 1.0m, position.StopLossPrice);
                        }
                    }
                }
                else
                {
                    // 위기 상황: 적립금 활용 긴급 DCA
                    if (CanUseReserve() && position.Stage < MaxEmergencyDca)
                    {
                        var targetPrice = avgPrice * (1 - EmergencyDcaSpacing / 100);
                        if (c0.Quote.Low <= targetPrice)
                        {
                            // 적립금을 활용한 큰 물타기
                            var multiplier = ReserveMultiplier;
                            DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, multiplier, position.StopLossPrice);

                            // 적립금에서 차감 (시뮬레이션)
                            var usedAmount = c0.Quote.Open * multiplier * 0.1m; // 개념적 차감
                            UseReserve(usedAmount);

                            position.Stage++; // 긴급 DCA 횟수 카운트
                        }
                    }
                }
            }
        }

        protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
        {
            var c0 = charts[i];
            var profitPercent = GetPositionProfitPercent(longPosition, c0.Quote.Close);
            var inCrisis = IsInCrisis(longPosition, c0.Quote.Close);

            // 최종 손절선
            if (longPosition.StopLossPrice > 0 && c0.Quote.Low <= longPosition.StopLossPrice)
            {
                var exitPrice = c0.Quote.Open <= longPosition.StopLossPrice ? c0.Quote.Open : longPosition.StopLossPrice;
                DcaExitPosition(longPosition, c0, exitPrice, 1.0m);
                return;
            }

            // 그리드 익절
            var profitPrice = longPosition.EntryPrice * (1 + GridTarget / 100);

            if (c0.Quote.High >= profitPrice)
            {
                var exitPrice = c0.Quote.Open >= profitPrice ? c0.Quote.Open : profitPrice;

                // 수익 계산 및 적립
                var exitValue = exitPrice * longPosition.Quantity;
                var entryValue = longPosition.EntryPrice * longPosition.Quantity;
                var profit = exitValue - entryValue;

                if (!inCrisis) // 정상 운영시에만 적립
                {
                    AddToReserve(profit);
                }

                DcaExitPosition(longPosition, c0, exitPrice, 1.0m);
                return;
            }

            // 위기 상황: 손익분기점 도달시 즉시 청산
            if (inCrisis && profitPercent >= 0m)
            {
                DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
                return;
            }

            // 트레일링 스톱 (수익 구간)
            if (profitPercent > 1.0m)
            {
                var newStopLoss = c0.Quote.High * 0.995m;
                if (newStopLoss > longPosition.StopLossPrice)
                {
                    longPosition.StopLossPrice = newStopLoss;
                }
            }

            // 일반 손절 (위기 전 단계)
            if (!inCrisis && profitPercent < -BaseStopLoss)
            {
                DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
                return;
            }
        }

        protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < 10) return;

            var c0 = charts[i];
            var position = GetActivePosition(symbol, PositionSide.Short);
            var inCrisis = IsInCrisis(position, c0.Quote.Close);

            if (position == null)
            {
                if (c0.Cci >= 100m && c0.Rsi1 >= 65m)
                {
                    var entry = c0.Quote.Open;
                    var stopLoss = entry * (1 + FinalStopLoss / 100);
                    DcaEntryPosition(PositionSide.Short, c0, entry, 0m, 1.0m, stopLoss);
                }
            }
            else
            {
                var avgPrice = position.EntryPrice;

                if (!inCrisis)
                {
                    if (position.DcaStep < MaxGridCount)
                    {
                        var targetPrice = avgPrice * (1 + GridSpacing / 100);
                        if (c0.Quote.High >= targetPrice)
                        {
                            DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, 1.0m, position.StopLossPrice);
                        }
                    }
                }
                else
                {
                    if (CanUseReserve() && position.Stage < MaxEmergencyDca)
                    {
                        var targetPrice = avgPrice * (1 + EmergencyDcaSpacing / 100);
                        if (c0.Quote.High >= targetPrice)
                        {
                            var multiplier = ReserveMultiplier;
                            DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, multiplier, position.StopLossPrice);

                            var usedAmount = c0.Quote.Open * multiplier * 0.1m;
                            UseReserve(usedAmount);

                            position.Stage++;
                        }
                    }
                }
            }
        }

        protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
        {
            var c0 = charts[i];
            var profitPercent = GetPositionProfitPercent(shortPosition, c0.Quote.Close);
            var inCrisis = IsInCrisis(shortPosition, c0.Quote.Close);

            if (shortPosition.StopLossPrice > 0 && c0.Quote.High >= shortPosition.StopLossPrice)
            {
                var exitPrice = c0.Quote.Open >= shortPosition.StopLossPrice ? c0.Quote.Open : shortPosition.StopLossPrice;
                DcaExitPosition(shortPosition, c0, exitPrice, 1.0m);
                return;
            }

            var profitPrice = shortPosition.EntryPrice * (1 - GridTarget / 100);

            if (c0.Quote.Low <= profitPrice)
            {
                var exitPrice = c0.Quote.Open <= profitPrice ? c0.Quote.Open : profitPrice;

                var exitValue = exitPrice * shortPosition.Quantity;
                var entryValue = shortPosition.EntryPrice * shortPosition.Quantity;
                var profit = entryValue - exitValue; // 숏은 반대

                if (!inCrisis)
                {
                    AddToReserve(profit);
                }

                DcaExitPosition(shortPosition, c0, exitPrice, 1.0m);
                return;
            }

            if (inCrisis && profitPercent >= 0m)
            {
                DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
                return;
            }

            if (profitPercent > 1.0m)
            {
                var newStopLoss = c0.Quote.Low * 1.005m;
                if (newStopLoss < shortPosition.StopLossPrice || shortPosition.StopLossPrice == 0)
                {
                    shortPosition.StopLossPrice = newStopLoss;
                }
            }

            if (!inCrisis && profitPercent < -BaseStopLoss)
            {
                DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
                return;
            }
        }
    }
}
