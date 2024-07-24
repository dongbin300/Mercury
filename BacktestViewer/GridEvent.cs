using Mercury.Enums;

namespace BacktestViewer
{
	public class GridEvent
	{
		public DateTime Time { get; set; }
		public string _Time => Time.ToString("yyyy-MM-dd");
		public string EventType { get; set; }
		public int Leverage { get; set; }
		public decimal CurrentPrice { get; set; }
		public decimal Upper { get; set; }
		public decimal Lower { get; set; }
		public decimal UpperStoploss { get; set; }
		public decimal LowerStoploss { get; set; }
		public decimal Interval { get; set; }
		public int Count { get; set; }
		public decimal BaseOrderSize { get; set; }
		public decimal CoinQuantity { get; set; }
		public GridType GridType { get; set; }
		public string _GridType => GridType == GridType.Long ? "L" : GridType == GridType.Short ? "S" : "N";
		public int LongFilledNum { get; set; }
		public int ShortFilledNum { get; set; }
		public decimal Estimated { get; set; }
		public decimal Margin { get; set; }
		public decimal Money { get; set; }
		public decimal Pnl { get; set; }

        public GridEvent(DateTime time, GridType gridType, decimal currentPrice, decimal upper, decimal lower, decimal interval)
        {
            Time = time;
			GridType = gridType;
			CurrentPrice = currentPrice;
			Upper = upper;
			Lower = lower;
			Interval = interval;
        }
    }
}
