using Mercury.Enums;

namespace Mercury.Trades
{
	public class BackTestTradeInfo(string symbol, string tradeTime, string side, string price, string quantity, string fee, string balance, string position, string baseAsset, string estimatedAsset, string tag)
	{
		public string Symbol { get; set; } = symbol;
		public string TradeTime { get; set; } = tradeTime;
		public string Side { get; set; } = side;
		public MtmPositionSide PositionSide => Side == "Buy" ? MtmPositionSide.Long : Side == "Sell" ? MtmPositionSide.Short : MtmPositionSide.None;
		public string Price { get; set; } = price;
		public string Quantity { get; set; } = quantity;
		public string Fee { get; set; } = fee;
		public string Balance { get; set; } = balance;
		public string Position { get; set; } = position;
		public string BaseAsset { get; set; } = baseAsset;
		public string EstimatedAsset { get; set; } = estimatedAsset;
		public string Tag { get; set; } = tag;
	}
}
