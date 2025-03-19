namespace Mercury.Trades
{
	public class BinanceAggregateTrade
	{
		public long AggregateTradeId { get; set; }
		public decimal Price { get; set; }
		public decimal Quantity { get; set; }
		public long FirstTradeId { get; set; }
		public long LastTradeId { get; set; }
		public long TradeTime { get; set; }
		public bool IsBuyerMarketMaker { get; set; }
	}
}
