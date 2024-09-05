using Binance.Net.Enums;

namespace Mercury.Cryptos.Binance
{
	public class BinanceFuturesTrade(DateTime time, string symbol, PositionSide positionSide, OrderSide side, decimal price, decimal quantity, decimal quoteQuantity, decimal fee, string feeAsset, decimal realizedPnl, bool isMaker)
	{
		public DateTime Time { get; set; } = time;
		public string Symbol { get; set; } = symbol;
		public PositionSide PositionSide { get; set; } = positionSide;
		public OrderSide Side { get; set; } = side;
		public decimal Price { get; set; } = price;
		public decimal Quantity { get; set; } = quantity;
		public decimal QuoteQuantity { get; set; } = quoteQuantity;
		public decimal Fee { get; set; } = fee;
		public string FeeAsset { get; set; } = feeAsset;
		public decimal RealizedPnl { get; set; } = realizedPnl;
		public bool IsMaker { get; set; } = isMaker;
	}

	public class BinanceFuturesTradeComparer : IComparer<BinanceFuturesTrade>
	{
		public int Compare(BinanceFuturesTrade? x, BinanceFuturesTrade? y)
		{
			if (x == null || y == null)
			{
				throw new ArgumentException("Arguments cannot be null");
			}

			return x.Time.CompareTo(y.Time);
		}
	}
}
