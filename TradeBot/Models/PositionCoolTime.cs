using Binance.Net.Enums;

using System;

namespace TradeBot.Models
{
    public class PositionCoolTime
    {
        public string Symbol { get; set; }
        public PositionSide Side { get; set; }
        public DateTime LatestEntryTime { get; set; }

        public PositionCoolTime(string symbol, PositionSide side, DateTime latestEntryTime)
        {
            Symbol = symbol;
            Side = side;
            LatestEntryTime = latestEntryTime;
        }

        public bool IsCoolTime()
        {
            return (DateTime.Now - LatestEntryTime).TotalSeconds < 120;
        }
    }
}
