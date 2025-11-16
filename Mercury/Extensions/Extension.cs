using Binance.Net.Enums;
using Mercury.Enums;

namespace Mercury.Extensions
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
        public static int ToInt(this string value) => string.IsNullOrEmpty(value) ? 0 : int.Parse(value);
        public static int ToInt(this object? value)
        {
            if (value == null)
            {
                return 0;
            }
            try
            {
                if (value is int i)
                {
                    return i;
                }
                return Convert.ToInt32(value);
            }
            catch
            {
                return 0;
			}
		}
        public static double ToDouble(this string value) => string.IsNullOrEmpty(value) ? 0 : double.Parse(value);
        public static decimal ToDecimal(this string value) => string.IsNullOrEmpty(value) ? 0 : decimal.Parse(value);
        public static decimal ToDecimal(this object? value)
        {
			if (value == null)
            {
				return 0m;
			}
			try
			{
				if (value is decimal d)
                {
					return d;
				}
				return Convert.ToDecimal(value);
			}
			catch
			{
				return 0m;
			}
		}
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
            string data = path.GetFileName();
            return data.Replace(path.GetExtension(), "");
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
            return DateTimeOffset.FromUnixTimeSeconds(value).UtcDateTime;
        }

        public static DateTime TimeStampMillisecondsToDateTime(this long value)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(value).UtcDateTime;
        }

        public static PositionSide ToPositionSide(this string side)
        {
            return Enum.Parse<PositionSide>(side, true);
        }

        public static GridType ToGridType(this string type)
        {
            return Enum.Parse<GridType>(type, true);
        }

		public static double?[] ToNullable(this double[] source)
		{
			return [.. source.Select(item => (double?)item)];
		}
	}
}
