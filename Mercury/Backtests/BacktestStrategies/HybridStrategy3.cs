using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
    /// <summary>
    /// HybridStrategy3 - 임계점 기반 하이브리드 전략
    /// 
    /// 손실 임계점을 설정하여 전략 전환
    /// - 정상 운영: 그리드 전략으로 수익 실현
    /// - 손실 5% 도달: 그리드 중단, DCA 모드 진입
    /// - 손실 15% 도달: 부분 손절 + 장기 DCA 전환
    /// - 수익 전환점: 다시 그리드 모드 복귀
    /// 
    /// === 파라미터 테스트 범위 ===
    /// Threshold1: 3.0 ~ 7.0 (1.0 step)
    /// Threshold2: 10.0 ~ 20.0 (2.5 step)
    /// GridSpacing: 0.5 ~ 2.0 (0.25 step)
    /// DcaSpacing: 2.0 ~ 4.0 (0.5 step)
    /// PartialCutPercent: 0.3 ~ 0.7 (0.1 step)
    /// </summary>
    public class HybridStrategy3(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals)
        : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
    {
        // === 임계점 설정 ===
        public decimal Threshold1 = 5.0m;            // 1차 임계점: DCA 모드 전환 (%)
        public decimal Threshold2 = 15.0m;           // 2차 임계점: 부분 손절 (%)
        public decimal RecoveryThreshold = 0.0m;     // 그리드 복귀 임계점 (%)

        // === 그리드 모드 파라미터 ===
        public decimal GridSpacing = 1.0m;           // 그리드 간격 (%)
        public decimal GridTarget = 2.0m;            // 그리드 익절 목표 (%)
        public int MaxGridCount = 5;

        // === DCA 모드 파라미터 ===
        public decimal DcaSpacing = 3.0m;            // DCA 간격 (%)
        public decimal DcaMultiplier = 1.5m;         // DCA 배수
        public decimal DcaTarget = 4.0m;             // DCA 익절 목표 (%)
        public int MaxDcaStep = 5;

        // === 긴급 DCA 파라미터 ===
        public decimal EmergencyDcaSpacing = 5.0m;   // 긴급 DCA 간격 (%)
        public decimal EmergencyMultiplier = 2.0m;   // 긴급 DCA 배수
        public decimal PartialCutPercent = 0.5m;     // 2차 임계점 손절 비율

        // === 진입 조건 ===
        public int CciPeriod = 14;
        public int RsiPeriod = 14;

        // === 상태 변수 ===
        private enum TradingMode { Grid, DCA, Emergency }

        protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
        {
            UseDca = true;
            chartPack.UseCci(CciPeriod);
            chartPack.UseRsi(RsiPeriod);
        }

        private TradingMode GetCurrentMode(Position position, decimal currentPrice)
        {
            if (position == null) return TradingMode.Grid;

            var profitPercent = GetPositionProfitPercent(position, currentPrice);

            // 손실이 2차 임계점 넘으면 긴급 모드
            if (profitPercent < -Threshold2)
            {
                return TradingMode.Emergency;
            }

            // 손실이 1차 임계점 넘으면 DCA 모드
            if (profitPercent < -Threshold1)
            {
                return TradingMode.DCA;
            }

            // 수익 전환점 이상이면 그리드 모드 복귀
            if (profitPercent >= RecoveryThreshold)
            {
                return TradingMode.Grid;
            }

            // 그 외는 현재 상태 유지
            return position.Stage == 0 ? TradingMode.Grid :
                   position.Stage == 1 ? TradingMode.DCA : TradingMode.Emergency;
        }

        protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < 10) return;

            var c0 = charts[i];
            var position = GetActivePosition(symbol, PositionSide.Long);

            if (position == null)
            {
                // 신규 진입 - 그리드 모드로 시작
                if (c0.Cci <= -100m && c0.Rsi1 <= 35m)
                {
                    var entry = c0.Quote.Open;
                    var stopLoss = entry * (1 - Threshold2 / 100); // 2차 임계점을 손절로 설정
                    DcaEntryPosition(PositionSide.Long, c0, entry, 0m, 1.0m, stopLoss);
                }
            }
            else
            {
                var mode = GetCurrentMode(position, c0.Quote.Close);
                var avgPrice = position.EntryPrice;

                if (mode == TradingMode.Grid)
                {
                    // 그리드 추가 매수
                    if (position.DcaStep < MaxGridCount)
                    {
                        var targetPrice = avgPrice * (1 - GridSpacing / 100);
                        if (c0.Quote.Low <= targetPrice)
                        {
                            DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, 1.0m, position.StopLossPrice);
                        }
                    }
                }
                else if (mode == TradingMode.DCA)
                {
                    // 일반 DCA
                    if (position.DcaStep < MaxDcaStep)
                    {
                        var targetPrice = avgPrice * (1 - DcaSpacing / 100);
                        if (c0.Quote.Low <= targetPrice)
                        {
                            position.Stage = 1; // DCA 모드 표시
                            DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, DcaMultiplier, position.StopLossPrice);
                        }
                    }
                }
                else // Emergency
                {
                    // 긴급 DCA
                    var targetPrice = avgPrice * (1 - EmergencyDcaSpacing / 100);
                    if (c0.Quote.Low <= targetPrice)
                    {
                        position.Stage = 2; // 긴급 모드 표시
                        DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, EmergencyMultiplier, position.StopLossPrice);
                    }
                }
            }
        }

        protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
        {
            var c0 = charts[i];
            var profitPercent = GetPositionProfitPercent(longPosition, c0.Quote.Close);
            var mode = GetCurrentMode(longPosition, c0.Quote.Close);

            // 2차 임계점 도달: 부분 손절
            if (mode == TradingMode.Emergency && longPosition.Stage != 2)
            {
                // 처음 긴급 모드 진입시 부분 손절
                DcaExitPosition(longPosition, c0, c0.Quote.Open, PartialCutPercent);
                longPosition.Stage = 2;
                return;
            }

            // 손절
            if (longPosition.StopLossPrice > 0 && c0.Quote.Low <= longPosition.StopLossPrice)
            {
                var exitPrice = c0.Quote.Open <= longPosition.StopLossPrice ? c0.Quote.Open : longPosition.StopLossPrice;
                DcaExitPosition(longPosition, c0, exitPrice, 1.0m);
                return;
            }

            // 모드별 익절 목표
            decimal targetProfit = mode == TradingMode.Grid ? GridTarget : DcaTarget;
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

            // DCA 모드에서 손익분기점 도달시 그리드 모드 복귀
            if (mode == TradingMode.DCA && profitPercent >= RecoveryThreshold)
            {
                longPosition.Stage = 0; // 그리드 모드 복귀
            }
        }

        protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < 10) return;

            var c0 = charts[i];
            var position = GetActivePosition(symbol, PositionSide.Short);

            if (position == null)
            {
                if (c0.Cci >= 100m && c0.Rsi1 >= 65m)
                {
                    var entry = c0.Quote.Open;
                    var stopLoss = entry * (1 + Threshold2 / 100);
                    DcaEntryPosition(PositionSide.Short, c0, entry, 0m, 1.0m, stopLoss);
                }
            }
            else
            {
                var mode = GetCurrentMode(position, c0.Quote.Close);
                var avgPrice = position.EntryPrice;

                if (mode == TradingMode.Grid)
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
                else if (mode == TradingMode.DCA)
                {
                    if (position.DcaStep < MaxDcaStep)
                    {
                        var targetPrice = avgPrice * (1 + DcaSpacing / 100);
                        if (c0.Quote.High >= targetPrice)
                        {
                            position.Stage = 1;
                            DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, DcaMultiplier, position.StopLossPrice);
                        }
                    }
                }
                else
                {
                    var targetPrice = avgPrice * (1 + EmergencyDcaSpacing / 100);
                    if (c0.Quote.High >= targetPrice)
                    {
                        position.Stage = 2;
                        DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, EmergencyMultiplier, position.StopLossPrice);
                    }
                }
            }
        }

        protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
        {
            var c0 = charts[i];
            var profitPercent = GetPositionProfitPercent(shortPosition, c0.Quote.Close);
            var mode = GetCurrentMode(shortPosition, c0.Quote.Close);

            if (mode == TradingMode.Emergency && shortPosition.Stage != 2)
            {
                DcaExitPosition(shortPosition, c0, c0.Quote.Open, PartialCutPercent);
                shortPosition.Stage = 2;
                return;
            }

            if (shortPosition.StopLossPrice > 0 && c0.Quote.High >= shortPosition.StopLossPrice)
            {
                var exitPrice = c0.Quote.Open >= shortPosition.StopLossPrice ? c0.Quote.Open : shortPosition.StopLossPrice;
                DcaExitPosition(shortPosition, c0, exitPrice, 1.0m);
                return;
            }

            decimal targetProfit = mode == TradingMode.Grid ? GridTarget : DcaTarget;
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

            if (mode == TradingMode.DCA && profitPercent >= RecoveryThreshold)
            {
                shortPosition.Stage = 0;
            }
        }
    }
}
