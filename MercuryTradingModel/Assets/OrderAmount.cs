using MercuryTradingModel.Enums;

namespace MercuryTradingModel.Assets
{
    public class OrderAmount
    {
        public OrderAmountType OrderType { get; set; }
        public decimal Value { get; set; }

        public OrderAmount(OrderAmountType orderType, decimal value)
        {
            OrderType = orderType;
            Value = value;
        }
    }
}
