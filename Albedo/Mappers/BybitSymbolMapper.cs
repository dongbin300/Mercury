using Albedo.Enums;

using System;

namespace Albedo.Mappers
{
    public class BybitSymbolMapper
    {
        public static PairQuoteAsset GetPairQuoteAsset(string symbol)
        {
            if (Enum.TryParse(typeof(PairQuoteAsset), symbol[^3..], out object? _quoteAsset))
            {
                return (PairQuoteAsset)_quoteAsset;
            }
            else if (Enum.TryParse(typeof(PairQuoteAsset), symbol[^4..], out object? __quoteAsset))
            {
                return (PairQuoteAsset)__quoteAsset;
            }
            return PairQuoteAsset.None;
        }
    }
}
