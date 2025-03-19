using Binance.Net.Enums;

using Mercury.Maths;

namespace Mercury.Backtests.BacktestInterfaces
{
	public interface IUseBlacklist
	{
		public List<BlacklistPosition> BlacklistPositions { get; set; }
		/// <summary>
		/// 블랙리스트 등록 임계%
		/// 기본적으로 이 값은 -여야함.
		/// </summary>
		public decimal BlacklistLossThresholdPercent { get; set; }
		/// <summary>
		/// 블랙리스트 등록 시 포지션 진입 금지 시간
		/// </summary>
		public int BlacklistBanHour { get; set; }

		public bool IsPostBlacklist(PositionSide side, decimal entryPrice, decimal exitPrice)
		{
			return Calculator.Roe(side, entryPrice, exitPrice) <= BlacklistLossThresholdPercent;
		}

		public bool IsBannedPosition(string symbol, PositionSide side, DateTime time)
		{
			var blacklist = BlacklistPositions.Where(x => x.Symbol.Equals(symbol) && x.Side.Equals(side));

			if (blacklist.Any())
			{
				return blacklist.ElementAt(0).IsBanned(time);
			}

			return false;
		}

		public void AddBlacklist(BlacklistPosition position)
		{
			var blacklist = BlacklistPositions.Where(x => x.Symbol.Equals(position.Symbol) && x.Side.Equals(position.Side));

			if (blacklist.Any())
			{
				BlacklistPositions.Remove(blacklist.ElementAt(0));
			}

			BlacklistPositions.Add(position);
		}
	}
}
