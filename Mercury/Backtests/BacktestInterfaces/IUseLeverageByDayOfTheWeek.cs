namespace Mercury.Backtests.BacktestInterfaces
{
	public interface IUseLeverageByDayOfTheWeek
	{
		public int[] Leverages { get; set; }

		public int GetLeverage(DateTime time)
		{
			return Leverages[(int)time.DayOfWeek];
		}
	}
}
