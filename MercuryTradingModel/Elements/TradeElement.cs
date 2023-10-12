using MercuryTradingModel.Enums;
using MercuryTradingModel.Interfaces;

namespace MercuryTradingModel.Elements
{
    public class TradeElement : IElement
    {
        public TradeElementType ElementType { get; set; }
        public decimal Value { get; set; }

        public TradeElement(TradeElementType elementType, decimal value)
        {
            ElementType = elementType;
            Value = value;
        }

        public override string ToString()
        {
            return ElementType.ToString() + " " + Value.ToString();
        }
    }
}
