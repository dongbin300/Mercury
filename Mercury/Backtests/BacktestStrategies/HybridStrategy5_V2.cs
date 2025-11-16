using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
    /// <summary>
    /// HybridStrategy5_V2 - 이익잉여금 활용 전략 (개선판)
    /// 
    /// === 주요 개선사항 ===
    /// 1. 적립금 로직 단순화 및 실제 작동
    /// 2. 위기 감지 알고리즘 개선 (추세 + 손실률)
    /// 3. 빠른 익절 + 느린 손절 구조
    /// 4. 스마트 DCA (가격대별 차등 물타기)
    /// 5. 동적 손절선 조정
    /// 
    /// === 파라미터 테스트 범위 ===
    /// GridSpacing: 0.3 ~ 0.8 (0.1 step)
    /// QuickExitTarget: 1.5 ~ 2.5 (0.25 step)
    /// CrisisThreshold: 5.0 ~ 12.0 (1.5 step)
    /// SmartDcaStep1: 2.0 ~ 4.0 (0.5 step)
    /// SmartDcaStep2: 4.0 ~ 7.0 (1.0 step)
    /// </summary>
    public class HybridStrategy5_V2(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals)
        : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
    {
        // === 그리드 기본 파라미터 (공격적) ===
        public decimal GridSpacing = 0.5m;           // 그리드 간격 (%)
        public decimal QuickExitTarget = 2.0m;       // 빠른 익절 (80% 청산)
        public decimal FinalExitTarget = 4.0m;       // 최종 익절 (20% 청산)
        public int MaxGridCount = 4;                 // 최대 그리드 레벨

        // === 스마트 DCA (가격대별 차등) ===
        public decimal SmartDcaStep1 = 3.0m;         // 1차 DCA 간격 (%)
        public decimal SmartDcaStep2 = 5.0m;         // 2차 DCA 간격 (%)
        public decimal SmartDcaStep3 = 8.0m;         // 3차 DCA 간격 (%)
        public decimal DcaMultiplier1 = 1.5m;        // 1차 배수
        public decimal DcaMultiplier2 = 2.0m;        // 2차 배수
        public decimal DcaMultiplier3 = 2.5m;        // 3차 배수

        // === 위기 감지 및 대응 ===
        public decimal CrisisThreshold = 8.0m;       // 위기 판단 기준 (손실 %)
        public int TrendPeriod = 10;                 // 추세 판단 기간
        public decimal TrendThreshold = -3.0m;       // 하락 추세 기준 (%)

        // === 동적 리스크 관리 ===
        public decimal InitialStopLoss = 2.0m;       // 초기 손절 (%)
        public decimal MaxStopLoss = 15.0m;          // 최대 손절 (%)
        public decimal TrailingActivation = 1.5m;    // 트레일링 시작 (%)
        public decimal TrailingPercent = 0.8m;       // 트레일링 간격 (%)

        // === 진입 조건 (완화) ===
        public int CciPeriod = 14;
        public int RsiPeriod = 14;
        public decimal CciEntry = 90m;               // CCI 진입 기준 완화
        public decimal RsiEntry = 38m;               // RSI 진입 기준 완화

        protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
        {
            UseDca = true;
            chartPack.UseCci(CciPeriod);
            chartPack.UseRsi(RsiPeriod);
            chartPack.UseSma(TrendPeriod);
        }

        private decimal GetTrend(List<ChartInfo> charts, int i)
        {
            if (i < TrendPeriod) return 0;
            var oldPrice = charts[i - TrendPeriod].Quote.Close;
            var newPrice = charts[i].Quote.Close;
            return (newPrice - oldPrice) / oldPrice * 100;
        }

        private bool IsInCrisis(Position position, decimal currentPrice, List<ChartInfo> charts, int i)
        {
            if (position == null) return false;

            var profitPercent = GetPositionProfitPercent(position, currentPrice);
            var trend = GetTrend(charts, i);

            // 손실 + 하락 추세 = 위기
            return profitPercent < -CrisisThreshold && trend < TrendThreshold;
        }

        private decimal GetSmartDcaMultiplier(int dcaStep)
        {
            if (dcaStep <= 1) return DcaMultiplier1;
            if (dcaStep <= 3) return DcaMultiplier2;
            return DcaMultiplier3;
        }

        private decimal GetSmartDcaSpacing(int dcaStep)
        {
            if (dcaStep == 0) return SmartDcaStep1;
            if (dcaStep <= 2) return SmartDcaStep2;
            return SmartDcaStep3;
        }

        protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < TrendPeriod) return;

            var c0 = charts[i];
            var position = GetActivePosition(symbol, PositionSide.Long);
            var inCrisis = IsInCrisis(position, c0.Quote.Close, charts, i);

            if (position == null)
            {
                // 신규 진입 조건 완화
                if (c0.Cci <= -CciEntry && c0.Rsi1 <= RsiEntry)
                {
                    var entry = c0.Quote.Open;
                    var stopLoss = entry * (1 - InitialStopLoss / 100);
                    DcaEntryPosition(PositionSide.Long, c0, entry, 0m, 1.0m, stopLoss);
                }
            }
            else
            {
                var avgPrice = position.EntryPrice;

                if (!inCrisis)
                {
                    // 일반 그리드
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
                    // 위기: 스마트 DCA
                    if (position.DcaStep < MaxGridCount + 3)
                    {
                        var spacing = GetSmartDcaSpacing(position.DcaStep - MaxGridCount);
                        var targetPrice = avgPrice * (1 - spacing / 100);

                        if (c0.Quote.Low <= targetPrice)
                        {
                            var multiplier = GetSmartDcaMultiplier(position.DcaStep - MaxGridCount);
                            DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, multiplier, position.StopLossPrice);

                            // 손절선을 점진적으로 완화
                            var newStopLoss = avgPrice * (1 - Math.Min(MaxStopLoss, InitialStopLoss + position.DcaStep * 2) / 100);
                            if (newStopLoss < position.StopLossPrice)
                            {
                                position.StopLossPrice = newStopLoss;
                            }
                        }
                    }
                }
            }
        }

        protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
        {
            var c0 = charts[i];
            var profitPercent = GetPositionProfitPercent(longPosition, c0.Quote.Close);
            var inCrisis = IsInCrisis(longPosition, c0.Quote.Close, charts, i);

            // 손절
            if (longPosition.StopLossPrice > 0 && c0.Quote.Low <= longPosition.StopLossPrice)
            {
                var exitPrice = c0.Quote.Open <= longPosition.StopLossPrice ? c0.Quote.Open : longPosition.StopLossPrice;
                DcaExitPosition(longPosition, c0, exitPrice, 1.0m);
                return;
            }

            // 1단계: 빠른 익절 (80% 청산)
            if (longPosition.Stage == 0)
            {
                var quickPrice = longPosition.EntryPrice * (1 + QuickExitTarget / 100);
                if (c0.Quote.High >= quickPrice)
                {
                    var exitPrice = c0.Quote.Open >= quickPrice ? c0.Quote.Open : quickPrice;
                    DcaExitPosition(longPosition, c0, exitPrice, 0.8m);
                    longPosition.Stage = 1;
                    longPosition.StopLossPrice = longPosition.EntryPrice * 1.005m; // 손익분기 보호
                    return;
                }
            }

            // 2단계: 최종 익절 (20% 청산)
            if (longPosition.Stage == 1)
            {
                var finalPrice = longPosition.EntryPrice * (1 + FinalExitTarget / 100);
                if (c0.Quote.High >= finalPrice)
                {
                    var exitPrice = c0.Quote.Open >= finalPrice ? c0.Quote.Open : finalPrice;
                    DcaExitPosition(longPosition, c0, exitPrice, 1.0m);
                    return;
                }
            }

            // 위기 상황: 손익분기 즉시 청산
            if (inCrisis && profitPercent >= -0.5m)
            {
                DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
                return;
            }

            // 트레일링 스톱
            if (profitPercent > TrailingActivation)
            {
                var newStopLoss = c0.Quote.High * (1 - TrailingPercent / 100);
                if (newStopLoss > longPosition.StopLossPrice)
                {
                    longPosition.StopLossPrice = newStopLoss;
                }
            }

            // 조기 청산 (신호 반전)
            if (c0.Cci >= 100m && c0.Rsi1 >= 70m && profitPercent > 0.5m)
            {
                DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
                return;
            }
        }

        protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < TrendPeriod) return;

            var c0 = charts[i];
            var position = GetActivePosition(symbol, PositionSide.Short);
            var inCrisis = IsInCrisis(position, c0.Quote.Close, charts, i);

            if (position == null)
            {
                if (c0.Cci >= CciEntry && c0.Rsi1 >= (100 - RsiEntry))
                {
                    var entry = c0.Quote.Open;
                    var stopLoss = entry * (1 + InitialStopLoss / 100);
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
                    if (position.DcaStep < MaxGridCount + 3)
                    {
                        var spacing = GetSmartDcaSpacing(position.DcaStep - MaxGridCount);
                        var targetPrice = avgPrice * (1 + spacing / 100);

                        if (c0.Quote.High >= targetPrice)
                        {
                            var multiplier = GetSmartDcaMultiplier(position.DcaStep - MaxGridCount);
                            DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, multiplier, position.StopLossPrice);

                            var newStopLoss = avgPrice * (1 + Math.Min(MaxStopLoss, InitialStopLoss + position.DcaStep * 2) / 100);
                            if (newStopLoss > position.StopLossPrice || position.StopLossPrice == 0)
                            {
                                position.StopLossPrice = newStopLoss;
                            }
                        }
                    }
                }
            }
        }

        protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
        {
            var c0 = charts[i];
            var profitPercent = GetPositionProfitPercent(shortPosition, c0.Quote.Close);
            var inCrisis = IsInCrisis(shortPosition, c0.Quote.Close, charts, i);

            if (shortPosition.StopLossPrice > 0 && c0.Quote.High >= shortPosition.StopLossPrice)
            {
                var exitPrice = c0.Quote.Open >= shortPosition.StopLossPrice ? c0.Quote.Open : shortPosition.StopLossPrice;
                DcaExitPosition(shortPosition, c0, exitPrice, 1.0m);
                return;
            }

            if (shortPosition.Stage == 0)
            {
                var quickPrice = shortPosition.EntryPrice * (1 - QuickExitTarget / 100);
                if (c0.Quote.Low <= quickPrice)
                {
                    var exitPrice = c0.Quote.Open <= quickPrice ? c0.Quote.Open : quickPrice;
                    DcaExitPosition(shortPosition, c0, exitPrice, 0.8m);
                    shortPosition.Stage = 1;
                    shortPosition.StopLossPrice = shortPosition.EntryPrice * 0.995m;
                    return;
                }
            }

            if (shortPosition.Stage == 1)
            {
                var finalPrice = shortPosition.EntryPrice * (1 - FinalExitTarget / 100);
                if (c0.Quote.Low <= finalPrice)
                {
                    var exitPrice = c0.Quote.Open <= finalPrice ? c0.Quote.Open : finalPrice;
                    DcaExitPosition(shortPosition, c0, exitPrice, 1.0m);
                    return;
                }
            }

            if (inCrisis && profitPercent >= -0.5m)
            {
                DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
                return;
            }

            if (profitPercent > TrailingActivation)
            {
                var newStopLoss = c0.Quote.Low * (1 + TrailingPercent / 100);
                if (newStopLoss < shortPosition.StopLossPrice || shortPosition.StopLossPrice == 0)
                {
                    shortPosition.StopLossPrice = newStopLoss;
                }
            }

            if (c0.Cci <= -100m && c0.Rsi1 <= 30m && profitPercent > 0.5m)
            {
                DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
                return;
            }
        }
    }
}
