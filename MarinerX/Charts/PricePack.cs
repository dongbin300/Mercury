using System;
using System.Collections.Generic;

namespace MarinerX.Charts
{
    internal class PricePack
    {
        public string Symbol { get; set; }
        public Dictionary<DateTime, List<decimal>> Prices { get; set; }

        public PricePack(string symbol)
        {
            Symbol = symbol;
            Prices = new();
        }
    }
}
