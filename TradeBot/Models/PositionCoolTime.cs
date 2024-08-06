using Binance.Net.Enums;

using System;

namespace TradeBot.Models
{
    public class PositionCoolTime(string symbol, PositionSide side, DateTime latestEntryTime)
	{
		public string Symbol { get; set; } = symbol;
		public PositionSide Side { get; set; } = side;
		public DateTime LatestEntryTime { get; set; } = latestEntryTime;

		public bool IsCoolTime()
        {
            return (DateTime.Now - LatestEntryTime).TotalSeconds < 120;
        }
    }
}
