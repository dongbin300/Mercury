using MercuryTradingModel.Interfaces;

namespace MercuryTradingModel.Scenarios
{
    public class Scenario : IScenario
    {
        public string Name { get; set; } = string.Empty;
        public IList<IStrategy> Strategies { get; set; } = new List<IStrategy>();

        public Scenario()
        {

        }

        public Scenario(string name)
        {
            Name = name;
        }

        public IScenario AddStrategy(IStrategy strategy)
        {
            Strategies.Add(strategy);
            return this;
        }
    }
}
