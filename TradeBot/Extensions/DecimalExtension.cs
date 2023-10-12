using System;

namespace TradeBot.Extensions
{
    public static class DecimalExtension
    {
        public static decimal ToValidPrice(this decimal price, string symbol)
        {
            var tick = Common.SymbolDetails.Find(x => x.Symbol.Equals(symbol))?.PriceTick ?? 0;
            return Math.Round(price, tick);
        }

        public static decimal ToValidQuantity(this decimal quantity, string symbol)
        {
            var tick = Common.SymbolDetails.Find(x => x.Symbol.Equals(symbol))?.QuantityTick ?? 0;
            return Math.Round(quantity, tick);
        }
    }
}
