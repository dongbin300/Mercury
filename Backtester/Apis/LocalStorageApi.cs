using Mercury;

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MarinerXX.Apis
{
    public class LocalStorageApi
    {
        public static List<string> SymbolNames => GetSymbolNames();

        public static List<string> GetSymbolNames()
        {
            var symbolFile = new DirectoryInfo(MercuryPath.BinanceFuturesData).GetFiles("symbol_*.txt").OrderByDescending(x => x.LastAccessTime).FirstOrDefault() ?? default!;
            return File.ReadAllLines(symbolFile.FullName).ToList();
        }

        //public static List<Quote>? GetQuotes(string symbol, DateTime date)
        //{
        //    try
        //    {
        //        var path = CryptoPath.BinanceFutures1m.Down(symbol, $"{symbol}_{date:yyyy-MM-dd}.csv");
        //        return IoUtil.ReadQuote(path).ToList();
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}
    }
}
