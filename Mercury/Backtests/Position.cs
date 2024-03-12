using Binance.Net.Enums;

namespace Mercury.Backtests
{
    public class Position(DateTime time, string symbol, PositionSide side, decimal entryPrice)
	{
		public DateTime Time { get; set; } = time;
		public string Symbol { get; set; } = symbol;
		public PositionSide Side { get; set; } = side;
		public decimal EntryPrice { get; set; } = entryPrice;

		/// <summary>
		/// Always (+)
		/// </summary>
		public decimal Quantity { get; set; }
        public decimal StopLossPrice { get; set; }
        public decimal TakeProfitPrice { get; set; }
        public int Stage { get; set; } = 0;

        /// <summary>
        /// Always (+)
        /// </summary>
        public decimal EntryAmount { get; set; }
        /// <summary>
        /// Always (+)
        /// </summary>
        public decimal ExitAmount { get; set; }

        public int EntryCount { get; set; } = 0;

        public decimal Income => Side == PositionSide.Long ? ExitAmount - EntryAmount : EntryAmount - ExitAmount;
	}
}
