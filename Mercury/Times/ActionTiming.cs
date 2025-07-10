using Mercury.Enums;

namespace Mercury.Times
{
	public class ActionTiming(TimingType type, double value = 0)
	{
		public TimingType TimingType { get; set; } = type;
		public double Value { get; set; } = value;
	}
}
