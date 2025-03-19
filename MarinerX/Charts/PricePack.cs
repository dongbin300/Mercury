using System;
using System.Collections.Generic;

namespace MarinerX.Charts
{
    internal class PricePack(string symbol)
	{
		public string Symbol { get; set; } = symbol;
		public Dictionary<DateTime, List<decimal>> Prices { get; set; } = new();
	}
}
