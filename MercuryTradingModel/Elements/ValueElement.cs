using MercuryTradingModel.Interfaces;

namespace MercuryTradingModel.Elements
{
    public class ValueElement : IElement
    {
        public decimal Value { get; set; }

        public ValueElement(decimal value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
