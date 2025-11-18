using Binance.Net.Enums;

using Vectoris.Enums;

namespace Vectoris.Trading.Orders;

/// <summary>
/// 주문 객체 (실계좌 / 가상계좌 공통 사용)
/// 하나의 Order는 여러 Transaction(체결)로 이어질 수 있음.
/// </summary>
public class Order
{
	/// <summary>
	/// 내부 시스템에서 생성한 고유 주문 ID
	/// (백테스트 / 실거래 모두 사용)
	/// </summary>
	public string OrderId { get; } = Guid.NewGuid().ToString();

	/// <summary>
	/// 거래소에서 발급한 주문 ID (실계좌일 경우)
	/// </summary>
	public string? ExchangeOrderId { get; init; }

	/// <summary>
	/// 주문한 심볼
	/// </summary>
	public string Symbol { get; init; }

	/// <summary>
	/// Buy / Sell
	/// </summary>
	public OrderSide Side { get; init; }

	/// <summary>
	/// 주문 유형 (Market / Limit / Stop / TP 등)
	/// </summary>
	public OrderType Type { get; init; }

	/// <summary>
	/// 지정가 주문일 경우 지정 가격
	/// 시장가 주문이면 null
	/// </summary>
	public decimal? Price { get; init; }

	/// <summary>
	/// 주문량 (항상 양수)
	/// </summary>
	public decimal Quantity { get; init; }

	/// <summary>
	/// 주문 시각 (클라이언트 기준)
	/// </summary>
	public DateTime CreateTime { get; init; }

	/// <summary>
	/// 주문 상태
	/// </summary>
	public OrderStatus Status { get; private set; } = OrderStatus.New;

	/// <summary>
	/// 체결된 수량 합계 (자동 계산 가능)
	/// </summary>
	public decimal FilledQuantity =>
		Transactions.Sum(t => t.Quantity);

	/// <summary>
	/// 거래소로부터 체결된 Transaction 목록
	/// 실계좌: 실제 체결 기록
	/// 백테스트: 가상 체결 기록
	/// </summary>
	public List<Transaction> Transactions { get; } = [];

	public Order(
		string symbol,
		OrderSide side,
		OrderType type,
		decimal quantity,
		decimal? price,
		DateTime createTime,
		string? exchangeOrderId = null
	)
	{
		Symbol = symbol;
		Side = side;
		Type = type;
		Quantity = quantity;
		Price = price;
		CreateTime = createTime;
		ExchangeOrderId = exchangeOrderId;
	}

	/// <summary>
	/// 주문 체결 기록을 추가
	/// (실거래/백테스트 공통 사용)
	/// </summary>
	public void AddTransaction(Transaction t)
	{
		if (t.Quantity <= 0)
			throw new ArgumentException("Transaction quantity must be positive.");

		if (t.Symbol != Symbol)
			throw new InvalidOperationException("Transaction symbol mismatch.");

		// OrderId 매칭
		if (t.OrderId != OrderId)
			throw new InvalidOperationException("Transaction does not belong to this order.");

		Transactions.Add(t);

		UpdateStatus();
	}

	/// <summary>
	/// 체결 정도에 따라 주문 상태 자동 업데이트
	/// </summary>
	private void UpdateStatus()
	{
		if (FilledQuantity == 0)
			Status = OrderStatus.New;
		else if (FilledQuantity < Quantity)
			Status = OrderStatus.PartiallyFilled;
		else
			Status = OrderStatus.Filled;
	}
}
