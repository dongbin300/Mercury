using Binance.Net.Enums;

namespace Vectoris.Trading.Orders
{
	/// <summary>
	/// 개별 거래 이벤트
	/// </summary>
	public class Transaction(string positionId, string symbol, OrderSide side, DateTime time, decimal price, decimal quantity)
	{
		/// <summary>
		/// 관련 Position ID
		/// </summary>
		public string PositionId { get; init; } = positionId;
		/// <summary>
		/// 심볼
		/// </summary>
		public string Symbol { get; init; } = symbol;
		/// <summary>
		/// 거래 타입: Buy / Sell
		/// </summary>
		public OrderSide Side { get; init; } = side;
		/// <summary>
		/// 거래 시각
		/// </summary>
		public DateTime Time { get; init; } = time;
		/// <summary>
		/// 거래 가격
		/// </summary>
		public decimal Price { get; init; } = price;
		/// <summary>
		/// 거래 수량 (항상 양수)
		/// </summary>
		public decimal Quantity { get; init; } = quantity;
	}
}
