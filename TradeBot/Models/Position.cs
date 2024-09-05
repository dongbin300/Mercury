using Binance.Net.Enums;

namespace TradeBot.Models
{
	public class Position(string symbol, string side)
    {
        public string Symbol { get; set; } = symbol;
        public string Side { get; set; } = side;
    }
}
