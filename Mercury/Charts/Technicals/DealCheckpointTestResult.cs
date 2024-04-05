namespace Mercury.Charts.Technicals
{
	public class DealCheckpointTestResult
	{
		public decimal TargetRoe { get; set; }
		public int Win { get; set; } = 0;
		public int Lose { get; set; } = 0;
		public int Draw { get; set; } = 0;
		public decimal WinRate => Math.Round((decimal)Win / (Win + Lose) * 100, 2);

		public override string ToString()
		{
			return $"{TargetRoe}%, {Win}승 {Lose}패 {Draw}무 ({WinRate}%)";
		}
	}
}
