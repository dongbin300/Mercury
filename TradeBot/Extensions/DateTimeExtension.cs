using System;

namespace TradeBot.Extensions
{
    public static class DateTimeExtension
    {
        public static DateTime ToDateTime(this long timestamp)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
        }
    }
}
