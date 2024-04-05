namespace Mercury.Charts.Technicals
{
	public class Checkpoint(DateTime time, CheckpointPosition position, decimal price)
	{
		public DateTime Time { get; set; } = time;
		public CheckpointPosition Position { get; set; } = position;
		public decimal Price { get; set; } = price;

		public override string ToString()
		{
			return (Position == CheckpointPosition.High ? "+" : "-") + Price;
		}
	}
}
