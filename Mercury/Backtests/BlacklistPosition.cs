using Binance.Net.Enums;

namespace Mercury.Backtests
{
	public class BlacklistPosition(string symbol, PositionSide side, DateTime triggerTime, DateTime releaseTime)
	{
		public string Symbol { get; set; } = symbol;
		public PositionSide Side { get; set; } = side;

		/// <summary>
		/// 발동 시각
		/// </summary>
		public DateTime TriggerTime { get; set; } = triggerTime;

		/// <summary>
		/// 해제 시각
		/// </summary>
		public DateTime ReleaseTime { get; set; } = releaseTime;

		public bool IsBanned(DateTime time) => time >= TriggerTime && time < ReleaseTime;
	}
}
