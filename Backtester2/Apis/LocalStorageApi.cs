using Mercury;
using Mercury.Cryptos;

using System.IO;

namespace Backtester2.Apis
{
	internal class LocalStorageApi
	{
		public static List<string> SymbolNames = [];
		public static List<(string, DateTime, DateTime)> Symbols = [];

		public static void Init()
		{
			SymbolNames = GetSymbolNames();
			Symbols = GetSymbols();
		}

		public static List<string> GetSymbolNames()
		{
			var symbolFile = new DirectoryInfo(MercuryPath.BinanceFuturesData).GetFiles("symbol_*.txt").OrderByDescending(x => x.LastAccessTime).FirstOrDefault() ?? default!;
			return [.. File.ReadAllLines(symbolFile.FullName)];
		}

		public static List<(string, DateTime, DateTime)> GetSymbols()
		{
			var result = new List<(string, DateTime, DateTime)>();
			foreach (var symbolName in SymbolNames)
			{
				var startTime = CryptoSymbol.GetStartDate(symbolName);
				var endTime = CryptoSymbol.GetEndDate(symbolName);

				result.Add((symbolName, startTime, endTime));
			}
			return result;
		}
	}
}
