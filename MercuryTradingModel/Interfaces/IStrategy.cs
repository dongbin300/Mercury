namespace MercuryTradingModel.Interfaces
{
    public interface IStrategy
    {
        string Name { get; set; }
        ICue? Cue { get; set; }
        ISignal Signal { get; set; }
        IOrder Order { get; set; }
        string Tag { get; set; }
    }
}
