using Binance.Net.Enums;

namespace Mercury.Backtests
{
    public enum PositionResult
    {
        Win,
        Lose,
        LittleWin
    }

    public class PositionHistory
    {
        public DateTime Time { get; set; }
        public DateTime EntryTime { get; set; }
        public string Symbol { get; set; }
        public PositionSide Side { get; set; }
        public PositionResult Result { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        public decimal EntryAmount { get; set; }
        public decimal ExitAmount { get; set; }
        public decimal Income => Side == PositionSide.Long ? ExitAmount - EntryAmount : EntryAmount - ExitAmount;
        public int EntryCount { get; set; }
        public decimal Fee { get; set; }

        public PositionHistory(DateTime time, DateTime entryTime, string symbol, PositionSide side, PositionResult result)
        {
            Time = time;
            EntryTime = entryTime;
            Symbol = symbol;
            Side = side;
            Result = result;
        }
    }
}
