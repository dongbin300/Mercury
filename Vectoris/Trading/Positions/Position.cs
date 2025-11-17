using Binance.Net.Enums;

using Vectoris.Trading.Orders;

namespace Vectoris.Trading.Positions;

/// <summary>
/// 계좌 내 포지션 상태 (자동 갱신)
/// </summary>
public class Position(string symbol, PositionSide side, DateTime openTime, IEnumerable<Transaction> allTransactions)
{
	public string Id { get; } = Guid.NewGuid().ToString();
	public string Symbol { get; init; } = symbol;
	/// <summary>
	/// Long / Short
	/// </summary>
	public PositionSide Side { get; init; } = side;

	/// <summary>
	/// Position 관련 Transaction 필터링
	/// </summary>
	private IEnumerable<Transaction> RelatedTransactions =>
		allTransactions.Where(t => t.Symbol == Symbol).Where(t => t.Time >= OpenTime).OrderBy(t => t.Time);

	#region Open
	public DateTime OpenTime { get; init; } = openTime;

	/// <summary>
	/// 평균 진입 가격 (자동 갱신)
	/// </summary>
	public decimal OpenPrice
	{
		get
		{
			decimal totalQty = 0;
			decimal weightedPrice = 0;

			foreach (var t in RelatedTransactions)
			{
				if ((Side == PositionSide.Long && t.Side == OrderSide.Buy) ||
					(Side == PositionSide.Short && t.Side == OrderSide.Sell))
				{
					weightedPrice = (weightedPrice * totalQty + t.Price * t.Quantity) / (totalQty + t.Quantity);
					totalQty += t.Quantity;
				}
				else
				{
					totalQty -= t.Quantity;
					if (totalQty < 0) totalQty = 0;
				}
			}

			return totalQty > 0 ? weightedPrice : 0;
		}
	}

	/// <summary>
	/// 포지션 수량 (자동 갱신)
	/// </summary>
	public decimal OpenQuantity
	{
		get
		{
			decimal qty = 0;
			foreach (var t in RelatedTransactions)
			{
				if ((Side == PositionSide.Long && t.Side == OrderSide.Buy) ||
					(Side == PositionSide.Short && t.Side == OrderSide.Sell))
					qty += t.Quantity;
				else
					qty -= t.Quantity;
			}
			return Math.Max(qty, 0);
		}
	}

	/// <summary>
	/// 총 진입 금액 (자동 갱신)
	/// </summary>
	public decimal OpenAmount => OpenPrice * OpenQuantity;
	#endregion

	#region Close
	/// <summary>
	/// 청산 시각 (마지막 청산 Transaction)
	/// </summary>
	public DateTime? CloseTime => RelatedTransactions
		.Where(t => (Side == PositionSide.Long && t.Side == OrderSide.Sell) ||
					(Side == PositionSide.Short && t.Side == OrderSide.Buy))
		.OrderByDescending(t => t.Time)
		.FirstOrDefault()?.Time;

	/// <summary>
	/// 평균 청산 가격 (자동 계산, ClosePrice)
	/// </summary>
	public decimal ClosePrice => CloseQuantity > 0 ? CloseAmount / CloseQuantity : 0;

	/// <summary>
	/// 총 청산 수량 (자동 갱신)
	/// </summary>
	public decimal CloseQuantity =>
		RelatedTransactions
			.Where(t => (Side == PositionSide.Long && t.Side == OrderSide.Sell) ||
						(Side == PositionSide.Short && t.Side == OrderSide.Buy))
			.Sum(t => t.Quantity);

	/// <summary>
	/// 총 청산 금액 (자동 갱신)
	/// </summary>
	public decimal CloseAmount =>
		RelatedTransactions
			.Where(t => (Side == PositionSide.Long && t.Side == OrderSide.Sell) ||
						(Side == PositionSide.Short && t.Side == OrderSide.Buy))
			.Sum(t => t.Price * t.Quantity);
	#endregion

	/// <summary>
	/// 실현 손익
	/// </summary>
	public decimal RealizedPnl =>
		Side == PositionSide.Long ? CloseAmount - OpenAmount : OpenAmount - CloseAmount;

	/// <summary>
	/// 최고 가격
	/// </summary>
	public decimal? MaxPrice => RelatedTransactions.Any() ? RelatedTransactions.Max(t => t.Price) : null;

	/// <summary>
	/// 최저 가격
	/// </summary>
	public decimal? MinPrice => RelatedTransactions.Any() ? RelatedTransactions.Min(t => t.Price) : null;
}
