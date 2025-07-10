namespace Mercury.StatisticalAnalyses
{
	public class PatternStat
	{
		public DateTime Time;
		public decimal EntryPrice;
		public decimal MaxUpRate;
		public decimal MaxDownRate;
		public bool IsReversal; // 반전 성공 여부
	}
}
