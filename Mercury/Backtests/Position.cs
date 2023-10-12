using Binance.Net.Enums;

namespace Mercury.Backtests
{
    public class Position
    {
        public DateTime Time { get; set; }
        public string Symbol { get; set; }
        public PositionSide Side { get; set; }
        public decimal EntryPrice { get; set; }

        public decimal Quantity { get; set; }
        public decimal StopLossPrice { get; set; }
        public decimal TakeProfitPrice { get; set; }
        public int Stage { get; set; } = 0;

        public decimal EntryAmount { get; set; }
        public decimal ExitAmount { get; set; }

        public int EntryCount { get; set; } = 0;


        public Position(DateTime time, string symbol, PositionSide side, decimal entryPrice)
        {
            Time = time;
            Symbol = symbol;
            Side = side;
            EntryPrice = entryPrice;
        }
    }
}
