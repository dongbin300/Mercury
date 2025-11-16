using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
    /// <summary>
    /// Cci33 - Pure CCI Crossover Strategy
    ///
    /// CCI 지표만을 사용하여 진입 및 청산하는 매우 단순한 전략
    ///
    /// === 핵심 목표 ===
    /// - CCI 교차를 통한 진입 및 청산
    /// - 최소한의 파라미터와 로직으로 단순성 극대화
    ///
    /// === 주요 파라미터 ===
    /// - CciPeriod: CCI 계산 기간
    /// - EntryLevelLong: 롱 진입을 위한 CCI 수준
    /// - EntryLevelShort: 숏 진입을 위한 CCI 수준
    /// - ExitLevelLong: 롱 청산을 위한 CCI 수준
    /// - ExitLevelShort: 숏 청산을 위한 CCI 수준
    ///
    /// </summary>
    public class Cci33(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
    {
        // === CCI 파라미터 ===
        public int CciPeriod = 14;
        public decimal EntryLevelLong = -100m;
        public decimal EntryLevelShort = 100m;
        public decimal ExitLevelLong = 0m;
        public decimal ExitLevelShort = 0m;

        protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
        {
            UseDca = false;
            chartPack.UseCci(CciPeriod);
        }

        protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < 2) return; // 최소 c1, c2 필요

            var c0 = charts[i];
            var c1 = charts[i - 1];
            var c2 = charts[i - 2];

            // CCI가 EntryLevelLong을 아래에서 위로 교차할 때 롱 진입
            if (c2.Cci < EntryLevelLong && c1.Cci >= EntryLevelLong)
            {
                var entry = c0.Quote.Open;
                DcaEntryPosition(PositionSide.Long, c0, entry, 0m, 1.0m, 0m); // 손절매는 청산 로직에서 처리
            }
        }

        protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
        {
            var c0 = charts[i];
            var c1 = charts[i - 1];
            var c2 = charts[i - 2];

            // CCI가 ExitLevelLong을 위에서 아래로 교차할 때 롱 청산
            if (c2.Cci > ExitLevelLong && c1.Cci <= ExitLevelLong)
            {
                DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
            }
        }

        protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < 2) return; // 최소 c1, c2 필요

            var c0 = charts[i];
            var c1 = charts[i - 1];
            var c2 = charts[i - 2];

            // CCI가 EntryLevelShort을 위에서 아래로 교차할 때 숏 진입
            if (c2.Cci > EntryLevelShort && c1.Cci <= EntryLevelShort)
            {
                var entry = c0.Quote.Open;
                DcaEntryPosition(PositionSide.Short, c0, entry, 0m, 1.0m, 0m); // 손절매는 청산 로직에서 처리
            }
        }

        protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
        {
            var c0 = charts[i];
            var c1 = charts[i - 1];
            var c2 = charts[i - 2];

            // CCI가 ExitLevelShort을 아래에서 위로 교차할 때 숏 청산
            if (c2.Cci < ExitLevelShort && c1.Cci >= ExitLevelShort)
            {
                DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
            }
        }
    }
}