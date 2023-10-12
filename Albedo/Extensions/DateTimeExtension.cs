using System;

namespace Albedo.Extensions
{
    public static class DateTimeExtension
    {
        public static long ToTimestamp(this DateTime value)
        {
            return ((DateTimeOffset)value).ToUnixTimeSeconds();
        }

        public static DateTime ToDateTime(this long value)
        {
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddSeconds(value).ToLocalTime();
            return dt;
        }

        public static DateTime KstToUtc(this DateTime kstDateTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(kstDateTime, TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time"));
        }
    }
}
