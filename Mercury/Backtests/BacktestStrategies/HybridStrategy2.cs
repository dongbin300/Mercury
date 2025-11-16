using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
    /// <summary>
    /// HybridStrategy2 - 계층화 포지션 관리 전략
    /// 
    /// 포트폴리오를 3개 계층으로 분할하여 독립적 관리
    /// - 1계층 (30%): 좁은 간격 그리드로 단기 수익
    /// - 2계층 (50%): 넓은 간격 그리드 + 트레일링 스톱
    /// - 3계층 (20%): 장기 DCA 보험 포지션
    /// 
    /// === 파라미터 테스트 범위 ===
    /// Layer1GridSpacing: 0.3 ~ 1.0 (0.1 step)
    /// Layer2GridSpacing: 1.0 ~ 2.5 (0.25 step)
    /// Layer3DcaSpacing: 2.0 ~ 4.0 (0.5 step)
    /// Layer1Target: 0.5 ~ 1.5 (0.25 step)
    /// Layer2Target: 1.5 ~ 3.0 (0.5 step)
    /// </summary>
    public class HybridStrategy2(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals)
        : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
    {
        // === 계층별 자금 배분 ===
        public decimal Layer1Percent = 0.30m;        // 1계층: 30%
        public decimal Layer2Percent = 0.50m;        // 2계층: 50%
        public decimal Layer3Percent = 0.20m;        // 3계층: 20%

        // === 1계층: 빠른 그리드 ===
        public decimal Layer1GridSpacing = 0.5m;     // 좁은 간격
        public decimal Layer1Target = 1.0m;          // 빠른 익절
        public decimal Layer1StopLoss = 1.5m;        // 작은 손절
        public int Layer1MaxCount = 3;               // 최대 3회

        // === 2계층: 중간 그리드 ===
        public decimal Layer2GridSpacing = 1.5m;     // 중간 간격
        public decimal Layer2Target = 2.5m;          // 중간 익절
        public decimal Layer2TrailingPercent = 0.5m; // 트레일링
        public int Layer2MaxCount = 4;

        // === 3계층: 장기 DCA ===
        public decimal Layer3DcaSpacing = 3.0m;      // 넓은 간격
        public decimal Layer3Target = 5.0m;          // 큰 익절
        public decimal Layer3Multiplier = 1.5m;      // 물타기 배수
        public int Layer3MaxCount = 5;

        // === 진입 조건 ===
        public int CciPeriod = 14;
        public int RsiPeriod = 14;
        public decimal CciExtreme = 100m;
        public decimal RsiExtreme = 35m;

        protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
        {
            UseDca = true;
            chartPack.UseCci(CciPeriod);
            chartPack.UseRsi(RsiPeriod);
        }

        private int GetLayerFromStage(int stage)
        {
            // Stage 0-2: Layer 1
            // Stage 3-6: Layer 2
            // Stage 7-11: Layer 3
            if (stage <= 2) return 1;
            if (stage <= 6) return 2;
            return 3;
        }

        protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < 10) return;

            var c0 = charts[i];
            var c1 = charts[i - 1];
            var position = GetActivePosition(symbol, PositionSide.Long);

            if (position == null)
            {
                // 신규 진입: 3개 계층 동시 진입
                if (c0.Cci <= -CciExtreme && c0.Rsi1 <= RsiExtreme)
                {
                    var entry = c0.Quote.Open;

                    // Layer 1 진입
                    var layer1StopLoss = entry * (1 - Layer1StopLoss / 100);
                    DcaEntryPosition(PositionSide.Long, c0, entry, 0m, Layer1Percent, layer1StopLoss);
                }
            }
            else
            {
                var currentLayer = GetLayerFromStage(position.DcaStep);
                var avgPrice = position.EntryPrice;

                // Layer 1 추가 매수
                if (currentLayer == 1 && position.DcaStep < Layer1MaxCount)
                {
                    var targetPrice = avgPrice * (1 - Layer1GridSpacing / 100);
                    if (c0.Quote.Low <= targetPrice)
                    {
                        DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, Layer1Percent, position.StopLossPrice);
                    }
                }
                // Layer 2 추가 매수
                else if (currentLayer == 2 && position.DcaStep < Layer1MaxCount + Layer2MaxCount)
                {
                    var targetPrice = avgPrice * (1 - Layer2GridSpacing / 100);
                    if (c0.Quote.Low <= targetPrice)
                    {
                        DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, Layer2Percent, position.StopLossPrice);
                    }
                }
                // Layer 3 DCA
                else if (position.DcaStep < Layer1MaxCount + Layer2MaxCount + Layer3MaxCount)
                {
                    var targetPrice = avgPrice * (1 - Layer3DcaSpacing / 100);
                    if (c0.Quote.Low <= targetPrice)
                    {
                        var multiplier = Layer3Multiplier;
                        DcaEntryPosition(PositionSide.Long, c0, c0.Quote.Open, 0m, Layer3Percent * multiplier, position.StopLossPrice);
                    }
                }
            }
        }

        protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
        {
            var c0 = charts[i];
            var profitPercent = GetPositionProfitPercent(longPosition, c0.Quote.Close);
            var currentLayer = GetLayerFromStage(longPosition.DcaStep);

            // 손절
            if (longPosition.StopLossPrice > 0 && c0.Quote.Low <= longPosition.StopLossPrice)
            {
                var exitPrice = c0.Quote.Open <= longPosition.StopLossPrice ? c0.Quote.Open : longPosition.StopLossPrice;
                DcaExitPosition(longPosition, c0, exitPrice, 1.0m);
                return;
            }

            // 계층별 익절 목표
            decimal targetProfit = 0m;
            if (currentLayer == 1) targetProfit = Layer1Target;
            else if (currentLayer == 2) targetProfit = Layer2Target;
            else targetProfit = Layer3Target;

            var profitPrice = longPosition.EntryPrice * (1 + targetProfit / 100);

            if (c0.Quote.High >= profitPrice)
            {
                var exitPrice = c0.Quote.Open >= profitPrice ? c0.Quote.Open : profitPrice;

                // Layer 1, 2는 전량 청산, Layer 3는 50%만 청산
                decimal exitPercent = currentLayer == 3 ? 0.5m : 1.0m;
                DcaExitPosition(longPosition, c0, exitPrice, exitPercent);

                if (currentLayer == 3 && exitPercent < 1.0m)
                {
                    longPosition.StopLossPrice = longPosition.EntryPrice * 1.02m; // 2% 수익 보장
                }
                return;
            }

            // Layer 2 트레일링 스톱
            if (currentLayer == 2 && profitPercent > Layer2TrailingPercent)
            {
                var newStopLoss = c0.Quote.High * (1 - Layer2TrailingPercent / 100);
                if (newStopLoss > longPosition.StopLossPrice)
                {
                    longPosition.StopLossPrice = newStopLoss;
                }
            }

            // Layer 3 트레일링 스톱 (더 보수적)
            if (currentLayer == 3 && profitPercent > 2.0m)
            {
                var newStopLoss = c0.Quote.High * 0.98m;
                if (newStopLoss > longPosition.StopLossPrice)
                {
                    longPosition.StopLossPrice = newStopLoss;
                }
            }
        }

        protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < 10) return;

            var c0 = charts[i];
            var position = GetActivePosition(symbol, PositionSide.Short);

            if (position == null)
            {
                if (c0.Cci >= CciExtreme && c0.Rsi1 >= 100 - RsiExtreme)
                {
                    var entry = c0.Quote.Open;
                    var layer1StopLoss = entry * (1 + Layer1StopLoss / 100);
                    DcaEntryPosition(PositionSide.Short, c0, entry, 0m, Layer1Percent, layer1StopLoss);
                }
            }
            else
            {
                var currentLayer = GetLayerFromStage(position.DcaStep);
                var avgPrice = position.EntryPrice;

                if (currentLayer == 1 && position.DcaStep < Layer1MaxCount)
                {
                    var targetPrice = avgPrice * (1 + Layer1GridSpacing / 100);
                    if (c0.Quote.High >= targetPrice)
                    {
                        DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, Layer1Percent, position.StopLossPrice);
                    }
                }
                else if (currentLayer == 2 && position.DcaStep < Layer1MaxCount + Layer2MaxCount)
                {
                    var targetPrice = avgPrice * (1 + Layer2GridSpacing / 100);
                    if (c0.Quote.High >= targetPrice)
                    {
                        DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, Layer2Percent, position.StopLossPrice);
                    }
                }
                else if (position.DcaStep < Layer1MaxCount + Layer2MaxCount + Layer3MaxCount)
                {
                    var targetPrice = avgPrice * (1 + Layer3DcaSpacing / 100);
                    if (c0.Quote.High >= targetPrice)
                    {
                        var multiplier = Layer3Multiplier;
                        DcaEntryPosition(PositionSide.Short, c0, c0.Quote.Open, 0m, Layer3Percent * multiplier, position.StopLossPrice);
                    }
                }
            }
        }

        protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
        {
            var c0 = charts[i];
            var profitPercent = GetPositionProfitPercent(shortPosition, c0.Quote.Close);
            var currentLayer = GetLayerFromStage(shortPosition.DcaStep);

            if (shortPosition.StopLossPrice > 0 && c0.Quote.High >= shortPosition.StopLossPrice)
            {
                var exitPrice = c0.Quote.Open >= shortPosition.StopLossPrice ? c0.Quote.Open : shortPosition.StopLossPrice;
                DcaExitPosition(shortPosition, c0, exitPrice, 1.0m);
                return;
            }

            decimal targetProfit = 0m;
            if (currentLayer == 1) targetProfit = Layer1Target;
            else if (currentLayer == 2) targetProfit = Layer2Target;
            else targetProfit = Layer3Target;

            var profitPrice = shortPosition.EntryPrice * (1 - targetProfit / 100);

            if (c0.Quote.Low <= profitPrice)
            {
                var exitPrice = c0.Quote.Open <= profitPrice ? c0.Quote.Open : profitPrice;
                decimal exitPercent = currentLayer == 3 ? 0.5m : 1.0m;
                DcaExitPosition(shortPosition, c0, exitPrice, exitPercent);

                if (currentLayer == 3 && exitPercent < 1.0m)
                {
                    shortPosition.StopLossPrice = shortPosition.EntryPrice * 0.98m;
                }
                return;
            }

            if (currentLayer == 2 && profitPercent > Layer2TrailingPercent)
            {
                var newStopLoss = c0.Quote.Low * (1 + Layer2TrailingPercent / 100);
                if (newStopLoss < shortPosition.StopLossPrice || shortPosition.StopLossPrice == 0)
                {
                    shortPosition.StopLossPrice = newStopLoss;
                }
            }

            if (currentLayer == 3 && profitPercent > 2.0m)
            {
                var newStopLoss = c0.Quote.Low * 1.02m;
                if (newStopLoss < shortPosition.StopLossPrice || shortPosition.StopLossPrice == 0)
                {
                    shortPosition.StopLossPrice = newStopLoss;
                }
            }
        }
    }
}
