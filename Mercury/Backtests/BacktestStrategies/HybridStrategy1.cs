using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

using System;
using System.Collections.Generic;
using System.Text;

namespace Mercury.Backtests.BacktestStrategies
{
    /// <summary>
    /// HybridStrategy1 - 적응형 그리드-DCA 전략
    /// 
    /// 시장 상황에 따라 그리드와 DCA를 동적으로 전환
    /// - 상승장/횡보장: 그리드 모드로 수익 실현 극대화
    /// - 하락장: DCA 모드로 평단가 낮추며 리스크 관리
    /// 
    /// === 파라미터 테스트 범위 ===
    /// TrendThreshold: 2.0 ~ 5.0 (0.5 step)
    /// GridSpacing: 0.5 ~ 2.0 (0.25 step)
    /// DcaMultiplier: 1.2 ~ 2.0 (0.2 step)
    /// MaxDcaStep: 3 ~ 7 (1 step)
    /// </summary>
    public class HybridStrategy1(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals)
        : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
    {
        // === 트렌드 감지 파라미터 ===
        public decimal TrendThreshold = 3.0m;        // 상승/하락 트렌드 판단 기준 (%)
        public int TrendPeriod = 20;                 // 트렌드 계산 기간

        // === 그리드 모드 파라미터 ===
        public decimal GridSpacing = 1.0m;           // 그리드 간격 (%)
        public decimal GridProfitTarget = 1.5m;      // 그리드 익절 목표 (%)
        public int MaxGridLevels = 5;                // 최대 그리드 레벨

        // === DCA 모드 파라미터 ===
        public decimal DcaSpacing = 2.0m;            // DCA 추가 매수 간격 (%)
        public decimal DcaMultiplier = 1.5m;         // DCA 물타기 배수
        public int MaxDcaStep = 5;                  // 최대 DCA 횟수
        public decimal DcaExitTarget = 3.0m;         // DCA 익절 목표 (%)

        // === 리스크 관리 ===
        public decimal StopLossPercent = 5.0m;       // 최대 손절 (%)
        public decimal MaxDrawdownTrigger = 3.0m;    // DCA 모드 전환 임계점 (%)

        // === 상태 변수 ===
        private enum TradingMode { Grid, DCA }
        private TradingMode currentMode = TradingMode.Grid;

        protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
        {
            UseDca = true;
            chartPack.UseCci(14);
            chartPack.UseRsi(14);
            chartPack.UseSma(20);
            chartPack.UseSma(50);
        }

        private decimal GetTrendStrength(List<ChartInfo> charts, int i)
        {
            if (i < TrendPeriod) return 0;
            var oldPrice = charts[i - TrendPeriod].Quote.Close;
            var newPrice = charts[i].Quote.Close;
            return (newPrice - oldPrice) / oldPrice * 100;
        }

        private TradingMode DetermineTradingMode(List<ChartInfo> charts, int i, Position position)
        {
            var trend = GetTrendStrength(charts, i);

            // 포지션이 있고 손실이 임계점을 넘으면 DCA 모드
            if (position != null)
            {
                var profitPercent = GetPositionProfitPercent(position, charts[i].Quote.Close);
                if (profitPercent < -MaxDrawdownTrigger)
                {
                    return TradingMode.DCA;
                }
            }

            // 하락 트렌드면 DCA 모드
            if (trend < -TrendThreshold)
            {
                return TradingMode.DCA;
            }

            // 그 외는 그리드 모드
            return TradingMode.Grid;
        }

        protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < TrendPeriod) return;

            var c0 = charts[i];
            var position = GetActivePosition(symbol, PositionSide.Long);

            // 모드 결정
            currentMode = DetermineTradingMode(charts, i, position);

            if (position == null)
            {
                // 신규 진입 - 그리드 모드만
                if (currentMode == TradingMode.Grid && c0.Cci <= -100m && c0.Rsi1 <= 35m)
                {
                    var entry = c0.Quote.Open;
                    var stopLoss = entry * (1 - StopLossPercent / 100);
                    DcaEntryPosition(PositionSide.Long, c0, entry, 0m, 1.0m, stopLoss);
                }
            }
            else
            {
                // 추가 진입
                if (currentMode == TradingMode.Grid)
                {
                    // 그리드 추가 매수
                    if (position.DcaStep < MaxGridLevels)
                    {
                        var avgPrice = position.EntryPrice;
                        var targetPrice = avgPrice * (1 - GridSpacing / 100);

                        if (c0.Quote.Low <= targetPrice)
                        {
                            DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, 1.0m, position.StopLossPrice);
                        }
                    }
                }
                else // DCA 모드
                {
                    // DCA 물타기
                    if (position.DcaStep < MaxDcaStep)
                    {
                        var avgPrice = position.EntryPrice;
                        var targetPrice = avgPrice * (1 - DcaSpacing / 100);

                        if (c0.Quote.Low <= targetPrice)
                        {
                            var multiplier = (decimal)Math.Pow((double)DcaMultiplier, position.DcaStep);
                            DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, multiplier, position.StopLossPrice);
                        }
                    }
                }
            }
        }

        protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
        {
            var c0 = charts[i];
            var profitPercent = GetPositionProfitPercent(longPosition, c0.Quote.Close);

            // 손절
            if (longPosition.StopLossPrice > 0 && c0.Quote.Low <= longPosition.StopLossPrice)
            {
                var exitPrice = c0.Quote.Open <= longPosition.StopLossPrice ? c0.Quote.Open : longPosition.StopLossPrice;
                DcaExitPosition(longPosition, c0, exitPrice, 1.0m);
                return;
            }

            // 익절
            decimal targetProfit = currentMode == TradingMode.Grid ? GridProfitTarget : DcaExitTarget;
            var profitPrice = longPosition.EntryPrice * (1 + targetProfit / 100);

            if (c0.Quote.High >= profitPrice)
            {
                var exitPrice = c0.Quote.Open >= profitPrice ? c0.Quote.Open : profitPrice;
                DcaExitPosition(longPosition, c0, exitPrice, 1.0m);
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
        }

        protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < TrendPeriod) return;

            var c0 = charts[i];
            var position = GetActivePosition(symbol, PositionSide.Short);

            currentMode = DetermineTradingMode(charts, i, position);

            if (position == null)
            {
                if (currentMode == TradingMode.Grid && c0.Cci >= 100m && c0.Rsi1 >= 65m)
                {
                    var entry = c0.Quote.Open;
                    var stopLoss = entry * (1 + StopLossPercent / 100);
                    DcaEntryPosition(PositionSide.Short, c0, entry, 0m, 1.0m, stopLoss);
                }
            }
            else
            {
                if (currentMode == TradingMode.Grid)
                {
                    if (position.DcaStep < MaxGridLevels)
                    {
                        var avgPrice = position.EntryPrice;
                        var targetPrice = avgPrice * (1 + GridSpacing / 100);

                        if (c0.Quote.High >= targetPrice)
                        {
                            DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, 1.0m, position.StopLossPrice);
                        }
                    }
                }
                else
                {
                    if (position.DcaStep < MaxDcaStep)
                    {
                        var avgPrice = position.EntryPrice;
                        var targetPrice = avgPrice * (1 + DcaSpacing / 100);

                        if (c0.Quote.High >= targetPrice)
                        {
                            var multiplier = (decimal)Math.Pow((double)DcaMultiplier, position.DcaStep);
                            DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, multiplier, position.StopLossPrice);
                        }
                    }
                }
            }
        }

        protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
        {
            var c0 = charts[i];
            var profitPercent = GetPositionProfitPercent(shortPosition, c0.Quote.Close);

            if (shortPosition.StopLossPrice > 0 && c0.Quote.High >= shortPosition.StopLossPrice)
            {
                var exitPrice = c0.Quote.Open >= shortPosition.StopLossPrice ? c0.Quote.Open : shortPosition.StopLossPrice;
                DcaExitPosition(shortPosition, c0, exitPrice, 1.0m);
                return;
            }

            decimal targetProfit = currentMode == TradingMode.Grid ? GridProfitTarget : DcaExitTarget;
            var profitPrice = shortPosition.EntryPrice * (1 - targetProfit / 100);

            if (c0.Quote.Low <= profitPrice)
            {
                var exitPrice = c0.Quote.Open <= profitPrice ? c0.Quote.Open : profitPrice;
                DcaExitPosition(shortPosition, c0, exitPrice, 1.0m);
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
        }
    }
}
