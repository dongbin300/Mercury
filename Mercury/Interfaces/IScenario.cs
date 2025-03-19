namespace Mercury.Interfaces
{
	public interface IScenario
	{
		string Name { get; set; }
		IList<IStrategy> Strategies { get; set; }

		IScenario AddStrategy(IStrategy strategy);
	}
}
