using Binance.Net.Enums;

namespace Mercury
{
    public static class IntervalExtension
    {
        public static KlineInterval ToKlineInterval(this string intervalString) => intervalString switch
        {
            "1m" => KlineInterval.OneMinute,
            "3m" => KlineInterval.ThreeMinutes,
            "5m" => KlineInterval.FiveMinutes,
            "15m" => KlineInterval.FifteenMinutes,
            "30m" => KlineInterval.ThirtyMinutes,
            "1h" => KlineInterval.OneHour,
            "2h" => KlineInterval.TwoHour,
            "4h" => KlineInterval.FourHour,
            "6h" => KlineInterval.SixHour,
            "8h" => KlineInterval.EightHour,
            "12h" => KlineInterval.TwelveHour,
            "1D" => KlineInterval.OneDay,
            "3D" => KlineInterval.ThreeDay,
            "1W" => KlineInterval.OneWeek,
            "1M" => KlineInterval.OneMonth,
            _ => KlineInterval.OneMinute
        };

        public static string ToIntervalString(this KlineInterval interval) => interval switch
        { 
            KlineInterval.OneMinute => "1m",
            KlineInterval.ThreeMinutes => "3m",
            KlineInterval.FiveMinutes => "5m",
            KlineInterval.FifteenMinutes => "15m",
            KlineInterval.ThirtyMinutes => "30m",
            KlineInterval.OneHour => "1h",
            KlineInterval.TwoHour => "2h",
            KlineInterval.FourHour => "4h",
            KlineInterval.SixHour => "6h",
            KlineInterval.EightHour => "8h",
            KlineInterval.TwelveHour => "12h",
            KlineInterval.OneDay => "1D",
            KlineInterval.ThreeDay => "3D",
            KlineInterval.OneWeek => "1W",
            KlineInterval.OneMonth => "1M",
            _ => "1m"
        };
    }
}
