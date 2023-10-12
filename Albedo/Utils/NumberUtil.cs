using System;

namespace Albedo.Utils
{
    public class NumberUtil
    {
        public static int GetSignificantDigitCount(decimal value)
        {
            string valueString = value.ToString().TrimEnd('0');
            int decimalIndex = valueString.IndexOf('.');
            int significantDigits = valueString.Replace(".", "").Length;

            if (decimalIndex >= 0)
            {
                significantDigits -= decimalIndex;
            }

            return significantDigits;
        }

        public static string ToRoundedValueString(decimal value)
        {
            return value.ToString("#,0.############################");
        }

        public static double Max(double value1, double value2, double value3)
        {
            return Math.Max(value1, Math.Max(value2, value3));
        }

        public static double Min(double value1, double value2, double value3)
        {
            return Math.Min(value1, Math.Min(value2, value3));
        }
    }
}
