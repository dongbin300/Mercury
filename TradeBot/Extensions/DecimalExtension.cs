﻿using System;

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

		public static decimal ToUpTickPrice(this decimal price, string symbol, int count = 1)
		{
			var tickSize = Common.SymbolDetails.Find(x => x.Symbol.Equals(symbol))?.TickSize ?? 0;
			return price + tickSize * count;
		}

		public static decimal ToDownTickPrice(this decimal price, string symbol, int count = 1)
        {
			var tickSize = Common.SymbolDetails.Find(x => x.Symbol.Equals(symbol))?.TickSize ?? 0;
            return price - tickSize * count;
		}

        public static decimal ToUpTickPricePercent(this decimal price, string symbol, decimal percent)
        {
            var calPrice = price * (1 + percent / 100);
			calPrice = calPrice.ToValidPrice(symbol);
			return price == calPrice ? price.ToUpTickPrice(symbol) : calPrice;
		}

		public static decimal ToDownTickPricePercent(this decimal price, string symbol, decimal percent)
		{
            var calPrice = price * (1 - percent / 100);
			calPrice = calPrice.ToValidPrice(symbol);
			return price == calPrice ? price.ToDownTickPrice(symbol) : calPrice;
		}
	}
}
