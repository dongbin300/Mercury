namespace Mercury.Indicators
{
	public class SqueezeMomentumResult(DateTime date, double? value, int? direction, int? signal)
	{
		public DateTime Date { get; set; } = date;
		public double? Value { get; set; } = value;
		public int? Direction { get; set; } = direction;
		public int? Signal { get; set; } = signal;
	}
}
