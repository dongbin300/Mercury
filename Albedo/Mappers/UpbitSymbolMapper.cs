using Albedo.Enums;

using System.Collections.Generic;
using System.Linq;

namespace Albedo.Mappers
{
    public class UpbitSymbolMapper
    {
        static Dictionary<string, string> values = new()
        {

        };

        public static List<string> Symbols => values.Keys.ToList();
        public static List<string> KrwSymbols => Symbols.FindAll(i => i.StartsWith("KRW"));
        public static List<string> BtcSymbols => Symbols.FindAll(i => i.StartsWith("BTC"));
        public static List<string> UsdtSymbols => Symbols.FindAll(i => i.StartsWith("USDT"));

        public static void Add(string key, string value)
        {
            if (values.ContainsKey(key))
            {
                return;
            }

            values.Add(key, value);
        }

        public static string GetKoreanName(string symbol)
        {
            return values.TryGetValue(symbol, out var name) ? name : symbol.Split('-')[1];
        }

        public static PairQuoteAsset GetPairQuoteAsset(string symbol)
        {
            if (symbol.StartsWith("KRW"))
            {
                return PairQuoteAsset.KRW;
            }
            else if (symbol.StartsWith("BTC"))
            {
                return PairQuoteAsset.BTC;
            }
            else if (symbol.StartsWith("USDT"))
            {
                return PairQuoteAsset.USDT;
            }
            else
            {
                return PairQuoteAsset.None;
            }
        }
    }
}
