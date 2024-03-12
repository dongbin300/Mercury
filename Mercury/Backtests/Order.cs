using Binance.Net.Enums;

namespace Mercury.Backtests
{
	public class Order(string symbol, PositionSide side, decimal price, decimal quantity)
	{
		public string Symbol { get; set; } = symbol;
		public PositionSide Side { get; set; } = side;
		public decimal Price { get; set; } = price;

		/// <summary>
		/// Always (+)
		/// </summary>
		public decimal Quantity { get; set; } = quantity;
	}
}
