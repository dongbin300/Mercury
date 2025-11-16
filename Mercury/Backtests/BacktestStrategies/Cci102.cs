using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
    /// <summary>
    /// High-profit targeting Donchian Channel + CCI strategy.
    /// Focus on larger moves to overcome fees.
    /// </summary>
    public class Cci102(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
    {
        // === Strategy Parameters ===
        public int DonchianPeriod = 20;
        public int CciPeriod = 14;
        public decimal CciOverboughtLevel = 100m;
        public decimal CciOversoldLevel = -100m;
        public int AtrPeriod = 14;
        public decimal AtrMultiplier = 2.0m;
        public int VolumeEmaPeriod = 20;

        // Dynamic Position Sizing Parameters
        public decimal MaxPositionSizeMultiplier = 2.0m;
        public decimal MinAtrRatioForMaxPosition = 0.005m; // 0.5%
        public decimal MaxAtrRatioForMinPosition = 0.02m;  // 2.0%

        /// <summary>
        /// Initialize indicators
        /// </summary>
        protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
        {
            chartPack.UseDonchianChannel(DonchianPeriod);
            chartPack.UseCci(CciPeriod);
            chartPack.UseAtr(AtrPeriod);
            chartPack.UseVolumeEma(VolumeEmaPeriod);
        }

        /// <summary>
        /// Long Entry: More selective for bigger moves
        /// </summary>
        protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < 2) return;

            var c0 = charts[i];     // Current candle (for entry only)
            var c1 = charts[i - 1]; // Previous candle
            var c2 = charts[i - 2]; // Two candles ago

            // Stronger volume requirement - only high conviction moves
            if (c1.Quote.Volume <= c1.VolumeEma * 1.5m) return;

            // More selective Donchian condition - actual touch required
            bool touchedLower = c1.Quote.Low <= c1.DcLower * 1.02m || c2.Quote.Low <= c2.DcLower * 1.02m;

            // Strong bullish momentum - not just green candle
            bool strongBullish = c1.Quote.Close > c1.Quote.Open && // Green candle
                                c1.Quote.Close > c1.DcLower * 1.05m && // Well above lower band
                                c1.Quote.Close > c2.Quote.Close; // Higher than previous

            // CCI strong recovery from oversold
            bool strongCciRecovery = c2.Cci <= CciOversoldLevel && c1.Cci > CciOversoldLevel * 0.7m; // Strong bounce

            if (touchedLower && strongBullish && strongCciRecovery)
            {
                var entryPrice = c0.Quote.Open;
                var stopLossPrice = c1.Quote.Low - (c1.Atr * 0.3m); // Tighter stop

                // MUCH larger profit target to overcome fees
                var riskAmount = entryPrice - stopLossPrice;
                var takeProfitPrice = entryPrice + (riskAmount * 4.0m); // 4:1 risk/reward!

                decimal positionSizeMultiplier = CalculatePositionSize(c1);

                EntryPosition(PositionSide.Long, c0, entryPrice, stopLossPrice, takeProfitPrice, positionSizeMultiplier);
            }
        }

        /// <summary>
        /// Long Exit: Hold for bigger profits
        /// </summary>
        protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
        {
            if (i < 1) return;

            var c0 = charts[i];
            var c1 = charts[i - 1];

            // Stop-Loss Check
            if (c0.Quote.Low <= longPosition.StopLossPrice)
            {
                ExitPosition(longPosition, c0, longPosition.StopLossPrice);
                return;
            }

            // Take Profit Check - wait for the big target
            if (longPosition.TakeProfitPrice != 0 && c0.Quote.High >= longPosition.TakeProfitPrice)
            {
                ExitPosition(longPosition, c0, longPosition.TakeProfitPrice);
                return;
            }

            // Only exit on very strong signals - let profits run
            if (c1.Cci >= CciOverboughtLevel * 1.5m) // Extremely overbought (150)
            {
                ExitPosition(longPosition, c0, c0.Quote.Open);
                return;
            }

            // Exit only on strong bearish reversal with high volume
            if (c1.Quote.Close < c1.Quote.Open &&
                c1.Quote.Low < c1.DcLower &&
                c1.Quote.Volume > c1.VolumeEma * 2.0m) // Very strong bearish signal
            {
                ExitPosition(longPosition, c0, c0.Quote.Open);
            }
        }

        /// <summary>
        /// Short Entry: More selective for bigger moves
        /// </summary>
        protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < 2) return;

            var c0 = charts[i];     // Current candle (for entry only)
            var c1 = charts[i - 1]; // Previous candle
            var c2 = charts[i - 2]; // Two candles ago

            // Stronger volume requirement - only high conviction moves
            if (c1.Quote.Volume <= c1.VolumeEma * 1.5m) return;

            // More selective Donchian condition - actual touch required
            bool touchedUpper = c1.Quote.High >= c1.DcUpper * 0.98m || c2.Quote.High >= c2.DcUpper * 0.98m;

            // Strong bearish momentum - not just red candle
            bool strongBearish = c1.Quote.Close < c1.Quote.Open && // Red candle
                                c1.Quote.Close < c1.DcUpper * 0.95m && // Well below upper band
                                c1.Quote.Close < c2.Quote.Close; // Lower than previous

            // CCI strong decline from overbought
            bool strongCciDecline = c2.Cci >= CciOverboughtLevel && c1.Cci < CciOverboughtLevel * 0.7m; // Strong drop

            if (touchedUpper && strongBearish && strongCciDecline)
            {
                var entryPrice = c0.Quote.Open;
                var stopLossPrice = c1.Quote.High + (c1.Atr * 0.3m); // Tighter stop

                // MUCH larger profit target to overcome fees
                var riskAmount = stopLossPrice - entryPrice;
                var takeProfitPrice = entryPrice - (riskAmount * 4.0m); // 4:1 risk/reward!

                decimal positionSizeMultiplier = CalculatePositionSize(c1);

                EntryPosition(PositionSide.Short, c0, entryPrice, stopLossPrice, takeProfitPrice, positionSizeMultiplier);
            }
        }

        /// <summary>
        /// Short Exit: Hold for bigger profits
        /// </summary>
        protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
        {
            if (i < 1) return;

            var c0 = charts[i];
            var c1 = charts[i - 1];

            // Stop-Loss Check
            if (c0.Quote.High >= shortPosition.StopLossPrice)
            {
                ExitPosition(shortPosition, c0, shortPosition.StopLossPrice);
                return;
            }

            // Take Profit Check - wait for the big target
            if (shortPosition.TakeProfitPrice != 0 && c0.Quote.Low <= shortPosition.TakeProfitPrice)
            {
                ExitPosition(shortPosition, c0, shortPosition.TakeProfitPrice);
                return;
            }

            // Only exit on very strong signals - let profits run
            if (c1.Cci <= CciOversoldLevel * 1.5m) // Extremely oversold (-150)
            {
                ExitPosition(shortPosition, c0, c0.Quote.Open);
                return;
            }

            // Exit only on strong bullish reversal with high volume
            if (c1.Quote.Close > c1.Quote.Open &&
                c1.Quote.High > c1.DcUpper &&
                c1.Quote.Volume > c1.VolumeEma * 2.0m) // Very strong bullish signal
            {
                ExitPosition(shortPosition, c0, c0.Quote.Open);
            }
        }

        /// <summary>
        /// Calculate dynamic position size based on ATR ratio
        /// </summary>
        private decimal CalculatePositionSize(ChartInfo chart)
        {
            decimal positionSizeMultiplier = 1.0m;

            if (chart.Atr.HasValue && chart.Quote.Close > 0)
            {
                decimal atrRatio = chart.Atr.Value / chart.Quote.Close;

                if (atrRatio <= MinAtrRatioForMaxPosition)
                {
                    positionSizeMultiplier = MaxPositionSizeMultiplier;
                }
                else if (atrRatio >= MaxAtrRatioForMinPosition)
                {
                    positionSizeMultiplier = 1.0m;
                }
                else
                {
                    // Linear interpolation
                    decimal range = MaxAtrRatioForMinPosition - MinAtrRatioForMaxPosition;
                    decimal positionRange = MaxPositionSizeMultiplier - 1.0m;
                    positionSizeMultiplier = MaxPositionSizeMultiplier - (atrRatio - MinAtrRatioForMaxPosition) / range * positionRange;
                }
            }

            return positionSizeMultiplier;
        }
    }
}