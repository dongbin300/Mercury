namespace Mercury
{
    public static class Extension
    {
        public static string ToSignedString(this int value) => value >= 0 ? "+" + value : value.ToString();
        public static string ToSignedString(this double value) => value >= 0 ? "+" + value : value.ToString();
        public static string ToSignedString(this decimal value) => value >= 0 ? "+" + value : value.ToString();
        public static string ToSignedPercentString(this int value) => value >= 0 ? "+" + value + "%" : value + "%";
        public static string ToSignedPercentString(this double value) => value >= 0 ? "+" + value + "%" : value + "%";
        public static string ToSignedPercentString(this decimal value) => value >= 0 ? "+" + value + "%" : value + "%";
        public static double Round(this double value, int digit) => Math.Round(value, digit);
        public static decimal Round(this decimal value, int digit) => Math.Round(value, digit);
        public static int ToInt(this string value) => int.Parse(value);
        public static double ToDouble(this string value) => double.Parse(value);
        public static decimal ToDecimal(this string value) => decimal.Parse(value);
        public static long ToLong(this string value) => long.Parse(value);
        public static DateTime ToDateTime(this string value) => DateTime.Parse(value);
        public static DateTime ToDateTime(this long timestamp) => DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
		public static string Down(this string path, params string[] downPaths) => Path.Combine(path, Path.Combine(downPaths));
        public static void TryCreate(this string path)
        {
            if (!File.Exists(path))
            {
                File.WriteAllText(path, string.Empty);
            }
        }
        public static void TryCreateDirectory(this string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        public static string GetDirectory(this string path)
        {
            string[] data = path.Split('\\');
            return path.Replace(data[^1], "");
        }
        public static string GetFileName(this string path)
        {
            string[] data = path.Split('\\');
            return data[^1];
        }
        public static string GetExtension(this string path)
        {
            return path[path.LastIndexOf('.')..];
        }
        public static string GetOnlyFileName(this string path)
        {
            string data = GetFileName(path);
            return data.Replace(GetExtension(path), "");
        }
        public static long DateTimeToTimeStamp(this DateTime value)
        {
            return ((DateTimeOffset)value).ToUnixTimeSeconds();
        }

        public static long DateTimeToTimeStampMilliseconds(this DateTime value)
        {
            return ((DateTimeOffset)value).ToUnixTimeMilliseconds();
        }

        public static DateTime TimeStampToDateTime(this long value)
        {
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddSeconds(value).ToLocalTime();
            return dt;
        }

        public static DateTime TimeStampMillisecondsToDateTime(this long value)
        {
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddMilliseconds(value).ToLocalTime();
            return dt;
        }
    }
}
