namespace Mercury.Charts.Technicals
{
    public class DealCheckpoint(DateTime time, CheckpointDirection direction, decimal price)
	{
		public DateTime Time { get; set; } = time;
		public CheckpointDirection Direction { get; set; } = direction;
		public decimal Price { get; set; } = price;

		public override string ToString()
        {
            return (Direction == CheckpointDirection.Profit ? "+" : "-") + Price;
        }
    }
}
