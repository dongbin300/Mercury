using Binance.Net.Enums;

using System;

namespace TradeBot.Models
{
	public enum CoolTimeType
	{
		/// <summary>
		/// 주문 중복 방지
		/// 300초 쿨타임
		/// </summary>
		BlockDuplicate,

		/// <summary>
		/// 반복되는 주문실패 방지
		/// 60초 쿨타임
		/// </summary>
		BlockRepeatFailed
	}

    public class PositionCoolTime(string symbol, PositionSide side, DateTime latestEntryTime)
	{
		public CoolTimeType Type { get; set; } = CoolTimeType.BlockDuplicate;
		public string Symbol { get; set; } = symbol;
		public PositionSide Side { get; set; } = side;
		public DateTime LatestEntryTime { get; set; } = latestEntryTime;

		public bool IsCoolTime()
        {
            return (DateTime.Now - LatestEntryTime).TotalSeconds < 300;
        }
	}
}
