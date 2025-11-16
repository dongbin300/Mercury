namespace Backtester2.Models
{
	public class StrategyParameterValue(string key, string value)
	{
		public string Key { get; set; } = key;
		public string Value { get; set; } = value;
	}
}
