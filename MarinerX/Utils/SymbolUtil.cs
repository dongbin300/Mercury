using Mercury;

using System;
using System.IO;
using System.Linq;

namespace MarinerX.Utils
{
    internal class SymbolUtil
    {
        public static DateTime GetStartDate(string symbol)
        {
            return GetDate(GetStartDateFileName(symbol));
        }

        public static DateTime GetEndDate(string symbol)
        {
            return GetDate(GetEndDateFileName(symbol));
        }

        public static DateTime GetEndDateOf1D(string symbol)
        {
            var data = File.ReadAllLines(PathUtil.BinanceFuturesData.Down("1D", $"{symbol}.csv"));
            return DateTime.Parse(data[^1].Split(',')[0].Split(' ')[0]);
        }

        public static DateTime GetDate(string fileName)
        {
            return DateTime.Parse(fileName.Split('_', '.')[1]);
        }

        public static string GetStartDateFileName(string symbol)
        {
            return new DirectoryInfo(PathUtil.BinanceFuturesData.Down("1m", symbol))
                .GetFiles("*.csv")
                .OrderBy(x => x.Name)
                .First().Name;
        }

        public static string GetEndDateFileName(string symbol)
        {
            return new DirectoryInfo(PathUtil.BinanceFuturesData.Down("1m", symbol))
                .GetFiles("*.csv")
                .OrderByDescending(x => x.Name)
                .First().Name;
        }
    }
}
