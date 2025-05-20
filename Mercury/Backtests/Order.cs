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

		public decimal Size => Price * Quantity;

		public static Order FromQuantity(string symbol, PositionSide side, decimal price, decimal quantity)
			=> new(symbol, side, price, quantity);

		public static Order FromSize(string symbol, PositionSide side, decimal price, decimal size)
		{
			if (price == 0)
			{
				throw new ArgumentException("Price cannot be zero when creating order from size.");
			}
			return new Order(symbol, side, price, size / price);
		}
	}
}
