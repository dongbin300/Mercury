using Binance.Net.Enums;

using Vectoris.Trading.Orders;
using Vectoris.Trading.Positions;

namespace Vectoris.Trading.Accounts;

/// <summary>
/// 계좌 상태 관리 (실거래 / 백테스트 공용)
/// </summary>
/// <remarks>
/// 계좌 생성자
/// </remarks>
public class Account(string name, decimal initialBalance)
{
	public string Id { get; } = Guid.NewGuid().ToString();
	public string Name { get; init; } = name;
	public decimal InitialBalance { get; init; } = initialBalance;

	/// <summary>
	/// 거래 기록 컬렉션
	/// </summary>
	private readonly List<Transaction> _transactions = [];
	public IEnumerable<Transaction> Transactions => _transactions;

	/// <summary>
	/// Position 컬렉션
	/// </summary>
	private readonly List<Position> _positions = [];
	public IEnumerable<Position> Positions => _positions;

	#region 계산 속성

	/// <summary>
	/// 현재 가용 자산 (잔액 + 실현 손익)
	/// </summary>
	public decimal Balance => InitialBalance + RealizedPnL;

	/// <summary>
	/// 총 실현 손익 (자동 계산)
	/// </summary>
	public decimal RealizedPnL => _positions.Sum(p => p.RealizedPnl);

	/// <summary>
	/// 총 미실현 손익 (자동 계산)
	/// </summary>
	public decimal UnrealizedPnL => _positions.Sum(p =>
	{
		var lastPrice = p.MaxPrice ?? p.OpenPrice; // 간단 예시: 최고가 기준
		return p.Side == PositionSide.Long
			? (lastPrice - p.OpenPrice) * p.OpenQuantity
			: (p.OpenPrice - lastPrice) * p.OpenQuantity;
	});

	#endregion

	#region 거래/포지션 관리

	/// <summary>
	/// 거래 추가 → Position 자동 갱신 (Entry/Exit 모두 처리)
	/// </summary>
	public void AddTransaction(Transaction t)
	{
		ArgumentNullException.ThrowIfNull(t);
		_transactions.Add(t);

		var side = t.Side == OrderSide.Buy ? PositionSide.Long : PositionSide.Short;

		var position = _positions.FirstOrDefault(p => p.Symbol == t.Symbol && p.Side == side && p.OpenQuantity > 0);
		if (position == null)
		{
			_positions.Add(new Position(t.Symbol, side, t.Time, _transactions));
		}
	}

	#endregion
}
