using System.Text.RegularExpressions;

namespace MercuryTradingModel.Extensions
{
    public static class StringExtension
    {
        public static string[] SplitKeep(this string str, char[] separator)
        {
            string pattern = $@"([{new string(separator)}])";
            string[] parts = Regex.Split(str, pattern);
            return parts;
        }

        public static string[] SplitKeep(this string str, string[] separator)
        {
            string pattern = $@"({string.Join('|', separator)})";
            string[] parts = Regex.Split(str, pattern);
            return parts;
        }
    }
}
