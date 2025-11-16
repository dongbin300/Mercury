
using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
    /// <summary>
    /// A simple strategy based on the Commodity Channel Index (CCI).
    /// </summary>
    public class Cci101(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
    {
        // === Strategy Parameters ===
        // === Strategy Parameters ===
        public int CciPeriod = 20;
        public decimal EntryLevel = 0m;
        public decimal TakeProfitLevel = 100m;
        public int EmaPeriod = 200;
        public int AtrPeriod = 14;
        public decimal AtrMultiplier = 1.5m;
        public int VolumeEmaPeriod = 20;
        public decimal EmaDistancePercent = 1.5m;

        // Multi-Timeframe Trend Confirmation Parameters
        public KlineInterval HigherTimeframeInterval = KlineInterval.FourHour;
        public int HigherTimeframeEmaPeriod = 50;

        // Dynamic Position Sizing Parameters
        public decimal MaxPositionSizeMultiplier = 2.0m;
        public decimal MinAtrRatioForMaxPosition = 0.005m; // 0.5%
        public decimal MaxAtrRatioForMinPosition = 0.02m;  // 2.0%

        /// <summary>
        /// This method is called once to initialize the indicators for a chart.
        /// </summary>
        protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
        {
            // Use the CCI indicator with the specified period.
            chartPack.UseCci(CciPeriod);
            chartPack.UseEma(EmaPeriod);
            chartPack.UseAtr(AtrPeriod);
            chartPack.UseVolumeEma(VolumeEmaPeriod);

            // Initialize Higher Timeframe EMA if this chartPack matches the HigherTimeframeInterval
            if (chartPack.Interval == HigherTimeframeInterval)
            {
                chartPack.UseEma(HigherTimeframeEmaPeriod);
            }
        }

        /// <summary>
        /// Defines the logic for entering a long position.
        /// </summary>
        protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < 2) return;

            var c0 = charts[i];
            var c1 = charts[i - 1];
            var c2 = charts[i - 2];

            // Condition: CCI crossed above the lower entry level on the previous candle.
            if (c2.Cci <= -EntryLevel && c1.Cci > -EntryLevel && c1.Quote.Close > c1.Ema1 && c1.Quote.Volume > c1.VolumeEma && c1.Quote.Close < c1.Ema1 * (1 + EmaDistancePercent / 100))
            {
                var entryPrice = c0.Quote.Open;
                var stopLossPrice = entryPrice - (c1.Atr * AtrMultiplier);

                // Calculate dynamic position size multiplier
                decimal positionSizeMultiplier = 1.0m;
                if (c1.Atr.HasValue && c1.Quote.Close > 0)
                {
                    decimal atrRatio = c1.Atr.Value / c1.Quote.Close;
                    if (atrRatio <= MinAtrRatioForMaxPosition)
                    {
                        positionSizeMultiplier = MaxPositionSizeMultiplier;
                    }
                    else if (atrRatio >= MaxAtrRatioForMinPosition)
                    {
                        positionSizeMultiplier = 1.0m; // Default or minimum size
                    }
                    else
                    {
                        // Linear interpolation between min and max ATR ratio
                        decimal range = MaxAtrRatioForMinPosition - MinAtrRatioForMaxPosition;
                        decimal positionRange = MaxPositionSizeMultiplier - 1.0m;
                        positionSizeMultiplier = MaxPositionSizeMultiplier - (atrRatio - MinAtrRatioForMaxPosition) / range * positionRange;
                    }
                }
                
                EntryPosition(PositionSide.Long, c0, entryPrice, stopLossPrice, null, positionSizeMultiplier);
            }
        }

        /// <summary>
        /// Defines the logic for exiting a long position.
        /// </summary>
        protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
        {
            if (i < 2) return;

            var c0 = charts[i];
            var c1 = charts[i - 1];
            var c2 = charts[i - 2];

            // Stop-Loss Check
            if (c0.Quote.Low <= longPosition.StopLossPrice)
            {
                ExitPosition(longPosition, c0, longPosition.StopLossPrice);
                return;
            }

            // Condition: CCI crossed above the take profit level on the previous candle.
            if (c2.Cci <= TakeProfitLevel && c1.Cci > TakeProfitLevel)
            {
                ExitPosition(longPosition, c0, c0.Quote.Open);
            }
        }

        /// <summary>
        /// Defines the logic for entering a short position.
        /// </summary>
        protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < 2) return;

            var c0 = charts[i];
            var c1 = charts[i - 1];
            var c2 = charts[i - 2];

            // Condition: CCI crossed below the upper entry level on the previous candle.
            if (c2.Cci >= EntryLevel && c1.Cci < EntryLevel && c1.Quote.Close < c1.Ema1 && c1.Quote.Volume > c1.VolumeEma && c1.Quote.Close > c1.Ema1 * (1 - EmaDistancePercent / 100))
            {
                var entryPrice = c0.Quote.Open;
                var stopLossPrice = entryPrice + (c1.Atr * AtrMultiplier);

                // Calculate dynamic position size multiplier
                decimal positionSizeMultiplier = 1.0m;
                if (c1.Atr.HasValue && c1.Quote.Close > 0)
                {
                    decimal atrRatio = c1.Atr.Value / c1.Quote.Close;
                    if (atrRatio <= MinAtrRatioForMaxPosition)
                    {
                        positionSizeMultiplier = MaxPositionSizeMultiplier;
                    }
                    else if (atrRatio >= MaxAtrRatioForMinPosition)
                    {
                        positionSizeMultiplier = 1.0m; // Default or minimum size
                    }
                    else
                    {
                        // Linear interpolation between min and max ATR ratio
                        decimal range = MaxAtrRatioForMinPosition - MinAtrRatioForMaxPosition;
                        decimal positionRange = MaxPositionSizeMultiplier - 1.0m;
                        positionSizeMultiplier = MaxPositionSizeMultiplier - (atrRatio - MinAtrRatioForMaxPosition) / range * positionRange;
                    }
                }

                EntryPosition(PositionSide.Short, c0, entryPrice, stopLossPrice, null, positionSizeMultiplier);
            }
        }

        /// <summary>
        /// Defines the logic for exiting a short position.
        /// </summary>
        protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
        {
            if (i < 2) return;

            var c0 = charts[i];
            var c1 = charts[i - 1];
            var c2 = charts[i - 2];

            // Stop-Loss Check
            if (c0.Quote.High >= shortPosition.StopLossPrice)
            {
                ExitPosition(shortPosition, c0, shortPosition.StopLossPrice);
                return;
            }

            // Condition: CCI crossed below the take profit level on the previous candle.
            if (c2.Cci >= -TakeProfitLevel && c1.Cci < -TakeProfitLevel)
            {
                ExitPosition(shortPosition, c0, c0.Quote.Open);
            }
        }
    }
}
