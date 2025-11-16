using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
    /// <summary>
    /// HybridStrategy4 - 변동성 기반 동적 전략
    /// 
    /// 변동성 지표에 따른 전략 조정
    /// - 저변동성: 좁은 그리드 + 높은 빈도
    /// - 고변동성: 넓은 그리드 + DCA 보조
    /// - 극고변동성: 그리드 중단, 전면 DCA
    /// 
    /// === 파라미터 테스트 범위 ===
    /// LowVolThreshold: 0.5 ~ 1.5 (0.25 step)
    /// HighVolThreshold: 2.0 ~ 4.0 (0.5 step)
    /// LowVolGridSpacing: 0.3 ~ 0.8 (0.1 step)
    /// MedVolGridSpacing: 1.0 ~ 2.0 (0.25 step)
    /// HighVolDcaSpacing: 2.5 ~ 4.5 (0.5 step)
    /// </summary>
    public class HybridStrategy4(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals)
        : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
    {
        // === 변동성 임계값 ===
        public decimal LowVolThreshold = 1.0m;       // 저변동성 기준 (%)
        public decimal HighVolThreshold = 3.0m;      // 고변동성 기준 (%)
        public int VolatilityPeriod = 20;            // 변동성 계산 기간

        // === 저변동성 모드 (타이트 그리드) ===
        public decimal LowVolGridSpacing = 0.5m;     // 좁은 간격
        public decimal LowVolTarget = 1.0m;          // 빠른 익절
        public int LowVolMaxCount = 4;

        // === 중변동성 모드 (일반 그리드) ===
        public decimal MedVolGridSpacing = 1.5m;     // 중간 간격
        public decimal MedVolTarget = 2.5m;          // 중간 익절
        public int MedVolMaxCount = 5;

        // === 고변동성 모드 (DCA 우선) ===
        public decimal HighVolDcaSpacing = 3.5m;     // 넓은 간격
        public decimal HighVolTarget = 4.0m;         // 큰 익절
        public decimal HighVolMultiplier = 1.5m;     // DCA 배수
        public int HighVolMaxCount = 6;

        // === 진입 조건 ===
        public int CciPeriod = 14;
        public int RsiPeriod = 14;

        // === 리스크 관리 ===
        public decimal BaseStopLoss = 3.0m;          // 기본 손절 (%)
        public decimal VolatilityStopMultiplier = 1.5m; // 변동성별 손절 배수

        protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
        {
            UseDca = true;
            chartPack.UseCci(CciPeriod);
            chartPack.UseRsi(RsiPeriod);
            chartPack.UseAtr(VolatilityPeriod);
        }

        private decimal GetVolatility(List<ChartInfo> charts, int i)
        {
            if (i < VolatilityPeriod) return 0;

            // ATR 기반 변동성 계산
            var recent = charts.Skip(i - VolatilityPeriod + 1).Take(VolatilityPeriod).ToList();
            var avgPrice = recent.Average(c => c.Quote.Close);
            var atr = recent.Average(c => c.Quote.High - c.Quote.Low);

            return (atr / avgPrice) * 100; // 백분율로 변환
        }

        private int GetVolatilityRegime(decimal volatility)
        {
            if (volatility < LowVolThreshold) return 1;      // 저변동성
            if (volatility < HighVolThreshold) return 2;     // 중변동성
            return 3;                                        // 고변동성
        }

        protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < VolatilityPeriod) return;

            var c0 = charts[i];
            var position = GetActivePosition(symbol, PositionSide.Long);
            var volatility = GetVolatility(charts, i);
            var regime = GetVolatilityRegime(volatility);

            if (position == null)
            {
                // 신규 진입
                bool shouldEnter = false;

                if (regime == 1) // 저변동성: 적극적 진입
                {
                    shouldEnter = c0.Cci <= -80m && c0.Rsi1 <= 40m;
                }
                else if (regime == 2) // 중변동성: 표준 진입
                {
                    shouldEnter = c0.Cci <= -100m && c0.Rsi1 <= 35m;
                }
                else // 고변동성: 보수적 진입
                {
                    shouldEnter = c0.Cci <= -120m && c0.Rsi1 <= 30m;
                }

                if (shouldEnter)
                {
                    var entry = c0.Quote.Open;
                    var stopLossPercent = BaseStopLoss * (regime == 3 ? VolatilityStopMultiplier : 1.0m);
                    var stopLoss = entry * (1 - stopLossPercent / 100);
                    DcaEntryPosition(PositionSide.Long, c0, entry, 0m, 1.0m, stopLoss);
                }
            }
            else
            {
                var avgPrice = position.EntryPrice;

                if (regime == 1) // 저변동성: 좁은 그리드
                {
                    if (position.DcaStep < LowVolMaxCount)
                    {
                        var targetPrice = avgPrice * (1 - LowVolGridSpacing / 100);
                        if (c0.Quote.Low <= targetPrice)
                        {
                            DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, 1.0m, position.StopLossPrice);
                        }
                    }
                }
                else if (regime == 2) // 중변동성: 중간 그리드
                {
                    if (position.DcaStep < MedVolMaxCount)
                    {
                        var targetPrice = avgPrice * (1 - MedVolGridSpacing / 100);
                        if (c0.Quote.Low <= targetPrice)
                        {
                            DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, 1.0m, position.StopLossPrice);
                        }
                    }
                }
                else // 고변동성: DCA
                {
                    if (position.DcaStep < HighVolMaxCount)
                    {
                        var targetPrice = avgPrice * (1 - HighVolDcaSpacing / 100);
                        if (c0.Quote.Low <= targetPrice)
                        {
                            DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, HighVolMultiplier, position.StopLossPrice);
                        }
                    }
                }
            }
        }

        protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
        {
            var c0 = charts[i];
            var profitPercent = GetPositionProfitPercent(longPosition, c0.Quote.Close);
            var volatility = GetVolatility(charts, i);
            var regime = GetVolatilityRegime(volatility);

            // 손절
            if (longPosition.StopLossPrice > 0 && c0.Quote.Low <= longPosition.StopLossPrice)
            {
                var exitPrice = c0.Quote.Open <= longPosition.StopLossPrice ? c0.Quote.Open : longPosition.StopLossPrice;
                DcaExitPosition(longPosition, c0, exitPrice, 1.0m);
                return;
            }

            // 변동성별 익절 목표
            decimal targetProfit = 0m;
            if (regime == 1) targetProfit = LowVolTarget;
            else if (regime == 2) targetProfit = MedVolTarget;
            else targetProfit = HighVolTarget;

            var profitPrice = longPosition.EntryPrice * (1 + targetProfit / 100);

            if (c0.Quote.High >= profitPrice)
            {
                var exitPrice = c0.Quote.Open >= profitPrice ? c0.Quote.Open : profitPrice;
                DcaExitPosition(longPosition, c0, exitPrice, 1.0m);
                return;
            }

            // 변동성별 트레일링 스톱
            if (profitPercent > 1.0m)
            {
                decimal trailingPercent = regime == 1 ? 0.3m : regime == 2 ? 0.5m : 1.0m;
                var newStopLoss = c0.Quote.High * (1 - trailingPercent / 100);
                if (newStopLoss > longPosition.StopLossPrice)
                {
                    longPosition.StopLossPrice = newStopLoss;
                }
            }

            // 고변동성 구간에서 조기 청산
            if (regime == 3 && profitPercent < -1.0m && c0.Rsi1 < 25m)
            {
                DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
                return;
            }
        }

        protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < VolatilityPeriod) return;

            var c0 = charts[i];
            var position = GetActivePosition(symbol, PositionSide.Short);
            var volatility = GetVolatility(charts, i);
            var regime = GetVolatilityRegime(volatility);

            if (position == null)
            {
                bool shouldEnter = false;

                if (regime == 1)
                {
                    shouldEnter = c0.Cci >= 80m && c0.Rsi1 >= 60m;
                }
                else if (regime == 2)
                {
                    shouldEnter = c0.Cci >= 100m && c0.Rsi1 >= 65m;
                }
                else
                {
                    shouldEnter = c0.Cci >= 120m && c0.Rsi1 >= 70m;
                }

                if (shouldEnter)
                {
                    var entry = c0.Quote.Open;
                    var stopLossPercent = BaseStopLoss * (regime == 3 ? VolatilityStopMultiplier : 1.0m);
                    var stopLoss = entry * (1 + stopLossPercent / 100);
                    DcaEntryPosition(PositionSide.Short, c0, entry, 0m, 1.0m, stopLoss);
                }
            }
            else
            {
                var avgPrice = position.EntryPrice;

                if (regime == 1)
                {
                    if (position.DcaStep < LowVolMaxCount)
                    {
                        var targetPrice = avgPrice * (1 + LowVolGridSpacing / 100);
                        if (c0.Quote.High >= targetPrice)
                        {
                            DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, 1.0m, position.StopLossPrice);
                        }
                    }
                }
                else if (regime == 2)
                {
                    if (position.DcaStep < MedVolMaxCount)
                    {
                        var targetPrice = avgPrice * (1 + MedVolGridSpacing / 100);
                        if (c0.Quote.High >= targetPrice)
                        {
                            DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, 1.0m, position.StopLossPrice);
                        }
                    }
                }
                else
                {
                    if (position.DcaStep < HighVolMaxCount)
                    {
                        var targetPrice = avgPrice * (1 + HighVolDcaSpacing / 100);
                        if (c0.Quote.High >= targetPrice)
                        {
                            DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, HighVolMultiplier, position.StopLossPrice);
                        }
                    }
                }
            }
        }

        protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
        {
            var c0 = charts[i];
            var profitPercent = GetPositionProfitPercent(shortPosition, c0.Quote.Close);
            var volatility = GetVolatility(charts, i);
            var regime = GetVolatilityRegime(volatility);

            if (shortPosition.StopLossPrice > 0 && c0.Quote.High >= shortPosition.StopLossPrice)
            {
                var exitPrice = c0.Quote.Open >= shortPosition.StopLossPrice ? c0.Quote.Open : shortPosition.StopLossPrice;
                DcaExitPosition(shortPosition, c0, exitPrice, 1.0m);
                return;
            }

            decimal targetProfit = 0m;
            if (regime == 1) targetProfit = LowVolTarget;
            else if (regime == 2) targetProfit = MedVolTarget;
            else targetProfit = HighVolTarget;

            var profitPrice = shortPosition.EntryPrice * (1 - targetProfit / 100);

            if (c0.Quote.Low <= profitPrice)
            {
                var exitPrice = c0.Quote.Open <= profitPrice ? c0.Quote.Open : profitPrice;
                DcaExitPosition(shortPosition, c0, exitPrice, 1.0m);
                return;
            }

            if (profitPercent > 1.0m)
            {
                decimal trailingPercent = regime == 1 ? 0.3m : regime == 2 ? 0.5m : 1.0m;
                var newStopLoss = c0.Quote.Low * (1 + trailingPercent / 100);
                if (newStopLoss < shortPosition.StopLossPrice || shortPosition.StopLossPrice == 0)
                {
                    shortPosition.StopLossPrice = newStopLoss;
                }
            }

            if (regime == 3 && profitPercent < -1.0m && c0.Rsi1 > 75m)
            {
                DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
                return;
            }
        }
    }
}
