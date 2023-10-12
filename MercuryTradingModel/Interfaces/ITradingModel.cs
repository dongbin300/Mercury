namespace MercuryTradingModel.Interfaces
{
    public interface ITradingModel
    {
        IList<IScenario> Scenarios { get; set; }
        string ScenarioNameInProgress { get; set; }
    }
}
