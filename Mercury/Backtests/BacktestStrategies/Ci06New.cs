using Binance.Net.Enums;
using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
    public class Ci06New : Backtester
    {
        // CCI Parameters
        public int CciPeriod = 14;
        public decimal EntryCciLong = -150;
        public decimal EntryCciShort = 150;
        public decimal ExitCciLong = 100;
        public decimal ExitCciShort = -100;

        // Ichimoku Cloud Parameters
        public int IchimokuConversionPeriod = 9;
        public int IchimokuBasePeriod = 26;
        public int IchimokuLeadingSpanPeriod = 52;

        // Additional Filters
        public bool UseTrendConfirmation { get; set; } = true;
        public decimal VolumeThreshold { get; set; } = 1.2m;
        public int ConfirmationCandles { get; set; } = 1;

        public Ci06New(string reportFileName = "Ci06New", decimal money = 10000m, int maxActiveDeals = 10, MaxActiveDealsType maxActiveDealsType = MaxActiveDealsType.Total, int leverage = 1)
            : base(reportFileName, money, leverage, maxActiveDealsType, maxActiveDeals)
        {
        }

        protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
        {
            chartPack.UseCci(CciPeriod);
            chartPack.UseIchimokuCloud(IchimokuConversionPeriod, IchimokuBasePeriod, IchimokuLeadingSpanPeriod);
        }

        protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
        {
            // 진입 결정은 i-1 캔들까지의 정보로 i 캔들에서 진입
            if (i < 1 || charts.Count <= i || charts[i-1].Cci == null ||
                charts[i-1].IcConversion == null || charts[i-1].IcBase == null ||
                charts[i-1].IcLeadingSpan1 == null || charts[i-1].IcLeadingSpan2 == null)
                return;

            var previousChart = charts[i-1];
            var currentChart = charts[i];

            // i-1 캔들의 정보로 진입 결정
            var cci = previousChart.Cci.Value;
            var conversion = previousChart.IcConversion.Value;
            var baseLine = previousChart.IcBase.Value;
            var leadingSpan1 = previousChart.IcLeadingSpan1.Value;
            var leadingSpan2 = previousChart.IcLeadingSpan2.Value;
            var close = previousChart.Quote.Close;

            // Check CCI oversold condition (이전 캔들의 CCI 값으로 판단)
            if (cci > EntryCciLong) return;

            // Volume filter
            var volumeOk = !UseTrendConfirmation || previousChart.Quote.Volume >= (i >= 2 ? charts[i-2].Quote.Volume : previousChart.Quote.Volume) * VolumeThreshold;

            // Determine market trend using Ichimoku Cloud (이전 캔들 정보로 판단)
            var isBullishTrend = close > leadingSpan1 && close > leadingSpan2 && conversion > baseLine;
            var priceAboveCloud = close > Math.Max(leadingSpan1, leadingSpan2);

            var confirmEntry = true;
            if (UseTrendConfirmation)
            {
                confirmEntry = isBullishTrend && priceAboveCloud && volumeOk;

                // Additional confirmation: check previous candles
                if (ConfirmationCandles > 0 && i >= ConfirmationCandles + 1)
                {
                    var prevChart = charts[i - ConfirmationCandles - 1];
                    if (prevChart.Cci.HasValue)
                    {
                        var prevCci = prevChart.Cci.Value;
                        var prevClose = prevChart.Quote.Close;
                        var prevAboveCloud = prevClose > Math.Max(
                            prevChart.IcLeadingSpan1 ?? 0,
                            prevChart.IcLeadingSpan2 ?? 0);

                        confirmEntry = confirmEntry && (prevCci <= EntryCciLong || prevAboveCloud);
                    }
                }
            }

            if (confirmEntry)
            {
                // i 캔들의 시가로 진입 (또는 지정가 진입)
                EntryPosition(PositionSide.Long, currentChart, currentChart.Quote.Open);
            }
        }

        protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
        {
            // 청산 결정은 i-1 캔들까지의 정보로 i 캔들에서 청산
            if (i < 1 || charts.Count <= i || charts[i-1].Cci == null ||
                charts[i-1].IcConversion == null || charts[i-1].IcBase == null ||
                charts[i-1].IcLeadingSpan1 == null || charts[i-1].IcLeadingSpan2 == null)
                return;

            var previousChart = charts[i-1];
            var currentChart = charts[i];

            // i-1 캔들의 정보로 청산 결정
            var cci = previousChart.Cci.Value;
            var conversion = previousChart.IcConversion.Value;
            var baseLine = previousChart.IcBase.Value;
            var leadingSpan1 = previousChart.IcLeadingSpan1.Value;
            var leadingSpan2 = previousChart.IcLeadingSpan2.Value;
            var close = previousChart.Quote.Close;

            // Exit long if CCI becomes overbought or bearish Ichimoku signal
            var shouldExit = cci >= ExitCciLong;

            if (!shouldExit && !UseTrendConfirmation)
            {
                var isBullishTrend = close > leadingSpan1 && close > leadingSpan2 && conversion > baseLine;
                shouldExit = !isBullishTrend;
            }

            if (!shouldExit && UseTrendConfirmation)
            {
                var isBearishTrend = close < leadingSpan1 && close < leadingSpan2 && conversion < baseLine;
                var priceAboveCloud = close > Math.Max(leadingSpan1, leadingSpan2);
                shouldExit = isBearishTrend || !priceAboveCloud;
            }

            if (shouldExit)
            {
                ExitPosition(longPosition, currentChart, currentChart.Quote.Open);
            }
        }

        protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
        {
            // 진입 결정은 i-1 캔들까지의 정보로 i 캔들에서 진입
            if (i < 1 || charts.Count <= i || charts[i-1].Cci == null ||
                charts[i-1].IcConversion == null || charts[i-1].IcBase == null ||
                charts[i-1].IcLeadingSpan1 == null || charts[i-1].IcLeadingSpan2 == null)
                return;

            var previousChart = charts[i-1];
            var currentChart = charts[i];

            // i-1 캔들의 정보로 진입 결정
            var cci = previousChart.Cci.Value;
            var conversion = previousChart.IcConversion.Value;
            var baseLine = previousChart.IcBase.Value;
            var leadingSpan1 = previousChart.IcLeadingSpan1.Value;
            var leadingSpan2 = previousChart.IcLeadingSpan2.Value;
            var close = previousChart.Quote.Close;

            // Check CCI overbought condition (이전 캔들의 CCI 값으로 판단)
            if (cci < EntryCciShort) return;

            // Volume filter
            var volumeOk = !UseTrendConfirmation || previousChart.Quote.Volume >= (i >= 2 ? charts[i-2].Quote.Volume : previousChart.Quote.Volume) * VolumeThreshold;

            // Determine market trend using Ichimoku Cloud (이전 캔들 정보로 판단)
            var isBearishTrend = close < leadingSpan1 && close < leadingSpan2 && conversion < baseLine;
            var priceBelowCloud = close < Math.Min(leadingSpan1, leadingSpan2);

            var confirmEntry = true;
            if (UseTrendConfirmation)
            {
                confirmEntry = isBearishTrend && priceBelowCloud && volumeOk;

                // Additional confirmation: check previous candles
                if (ConfirmationCandles > 0 && i >= ConfirmationCandles + 1)
                {
                    var prevChart = charts[i - ConfirmationCandles - 1];
                    if (prevChart.Cci.HasValue)
                    {
                        var prevCci = prevChart.Cci.Value;
                        var prevClose = prevChart.Quote.Close;
                        var prevBelowCloud = prevClose < Math.Min(
                            prevChart.IcLeadingSpan1 ?? 0,
                            prevChart.IcLeadingSpan2 ?? 0);

                        confirmEntry = confirmEntry && (prevCci >= EntryCciShort || prevBelowCloud);
                    }
                }
            }

            if (confirmEntry)
            {
                // i 캔들의 시가로 진입 (또는 지정가 진입)
                EntryPosition(PositionSide.Short, currentChart, currentChart.Quote.Open);
            }
        }

        protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
        {
            // 청산 결정은 i-1 캔들까지의 정보로 i 캔들에서 청산
            if (i < 1 || charts.Count <= i || charts[i-1].Cci == null ||
                charts[i-1].IcConversion == null || charts[i-1].IcBase == null ||
                charts[i-1].IcLeadingSpan1 == null || charts[i-1].IcLeadingSpan2 == null)
                return;

            var previousChart = charts[i-1];
            var currentChart = charts[i];

            // i-1 캔들의 정보로 청산 결정
            var cci = previousChart.Cci.Value;
            var conversion = previousChart.IcConversion.Value;
            var baseLine = previousChart.IcBase.Value;
            var leadingSpan1 = previousChart.IcLeadingSpan1.Value;
            var leadingSpan2 = previousChart.IcLeadingSpan2.Value;
            var close = previousChart.Quote.Close;

            // Exit short if CCI becomes oversold or bullish Ichimoku signal
            var shouldExit = cci <= ExitCciShort;

            if (!shouldExit && !UseTrendConfirmation)
            {
                var isBearishTrend = close < leadingSpan1 && close < leadingSpan2 && conversion < baseLine;
                shouldExit = !isBearishTrend;
            }

            if (!shouldExit && UseTrendConfirmation)
            {
                var isBullishTrend = close > leadingSpan1 && close > leadingSpan2 && conversion > baseLine;
                var priceBelowCloud = close < Math.Min(leadingSpan1, leadingSpan2);
                shouldExit = isBullishTrend || priceBelowCloud;
            }

            if (shouldExit)
            {
                ExitPosition(shortPosition, currentChart, currentChart.Quote.Open);
            }
        }

        public override string ToString()
        {
            return $"Ci06New[CCI:{CciPeriod}, EntryLong:{EntryCciLong}, EntryShort:{EntryCciShort}, ExitLong:{ExitCciLong}, ExitShort:{ExitCciShort}, Ichimoku:({IchimokuConversionPeriod},{IchimokuBasePeriod},{IchimokuLeadingSpanPeriod}), TrendConfirm:{UseTrendConfirmation}, VolThresh:{VolumeThreshold:F2}]";
        }
    }
}