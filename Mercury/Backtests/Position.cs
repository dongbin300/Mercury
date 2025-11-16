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
        /// Always (+) - 총 진입 금액 (부분 청산 시에도 보존)
        /// </summary>
        public decimal TotalEntryAmount { get; set; }
        /// <summary>
        /// Always (+)
        /// </summary>
        public decimal ExitAmount { get; set; }

        public int EntryCount { get; set; } = 0;
        public int DcaStep { get; set; } = 0;

		public DateTime? ExitDateTime { get; set; } = null;

        public decimal? HighestPrice { get; set; }
        public decimal? LowestPrice { get; set; }

		/// <summary>
		/// 실현 수익/손실 = 청산금액 - 실제 진입금액
		/// </summary>
		public decimal Income
		{
			get
			{
				if (ExitAmount == 0) return 0;

				// 실제 진입금액 사용 (분할 진입 고려)
				var actualEntryAmount = TotalEntryAmount > 0 ? TotalEntryAmount : EntryAmount;

				return Side == PositionSide.Long ?
					ExitAmount - actualEntryAmount :
					actualEntryAmount - ExitAmount;
			}
		}

		/// <summary>
		/// 총 청산 수량
		/// </summary>
		public decimal ExitQuantity { get; set; }
	}
}
