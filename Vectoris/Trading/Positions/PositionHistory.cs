using Binance.Net.Enums;

using Vectoris.Enums;

namespace Vectoris.Trading.Positions;

public class PositionHistory(Position position)
{
	/// <summary>
	/// 관련 Position ID
	/// </summary>
	public string PositionId { get; init; } = position.Id;
	/// <summary>
	/// 심볼/종목
	/// </summary>
	public string Symbol { get; init; } = position.Symbol;
	/// <summary>
	/// Long / Short
	/// </summary>
	public PositionSide Side { get; init; } = position.Side;
	/// <summary>
	/// 포지션 평균 진입 가격
	/// </summary>
	public decimal OpenPrice { get; init; } = position.OpenPrice;
	/// <summary>
	/// 포지션 수량
	/// </summary>
	public decimal OpenQuantity { get; init; } = position.OpenQuantity;
	/// <summary>
	/// 총 진입 금액
	/// </summary>
	public decimal OpenAmount { get; init; } = position.OpenAmount;
	/// <summary>
	/// 총 청산 수량
	/// </summary>
	public decimal CloseQuantity { get; init; } = position.CloseQuantity;
	/// <summary>
	/// 총 청산 금액
	/// </summary>
	public decimal CloseAmount { get; init; } = position.CloseAmount;
	/// <summary>
	/// 평균 청산 가격
	/// </summary>
	public decimal ClosePrice { get; init; } = position.ClosePrice;
	/// <summary>
	/// 실현 손익
	/// </summary>
	public decimal RealizedPnl { get; init; } = position.RealizedPnl;
	/// <summary>
	/// 최고/최저 가격
	/// </summary>
	public decimal? MaxPrice { get; init; } = position.MaxPrice;
	public decimal? MinPrice { get; init; } = position.MinPrice;
	/// <summary>
	/// 결과
	/// </summary>
	public PositionResult Result => 
		RealizedPnl > 0 ? PositionResult.Win : PositionResult.Lose;
}
