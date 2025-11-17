using Binance.Net.Enums;

namespace Vectoris.Trading.Orders;

/// <summary>
/// 개별 체결 이벤트 (Fill)
/// </summary>
public class Transaction(string orderId, string symbol, OrderSide side, DateTime time, decimal price, decimal quantity)
{
	/// <summary>
	/// 어떤 주문으로 인해 발생한 체결인지
	/// </summary>
	public string OrderId { get; init; } = orderId;

	/// <summary>
	/// 심볼
	/// </summary>
	public string Symbol { get; init; } = symbol;

	/// <summary>
	/// Buy / Sell 방향
	/// </summary>
	public OrderSide Side { get; init; } = side;

	/// <summary>
	/// 체결 시각
	/// </summary>
	public DateTime Time { get; init; } = time;

	/// <summary>
	/// 체결 가격
	/// </summary>
	public decimal Price { get; init; } = price;

	/// <summary>
	/// 체결 수량 (항상 양수)
	/// </summary>
	public decimal Quantity { get; init; } = quantity;
}
