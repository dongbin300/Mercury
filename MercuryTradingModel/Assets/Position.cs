using MercuryTradingModel.Enums;

namespace MercuryTradingModel.Assets
{
    public class Position
    {
        /// <summary>
        /// Long or Short, default is None
        /// </summary>
        public PositionSide Side { get; set; } = PositionSide.None;

        /// <summary>
        /// Quantity * Price, Always +
        /// </summary>
        public decimal TransactionAmount { get; set; } = 0m;

        /// <summary>
        /// Position Quantity, Always +
        /// </summary>
        public decimal Quantity { get; set; } = 0m;

        /// <summary>
        /// Average Price, Always +
        /// </summary>
        public decimal AveragePrice => Quantity == 0 ? 0 : TransactionAmount / Quantity;

        /// <summary>
        /// Signed Quantity, -(short position) or +(long position)
        /// </summary>
        public decimal Value => Side == PositionSide.Short ? -Quantity : Quantity;

        public void Long(decimal quantity, decimal price)
        {
            if (Quantity == 0)
            {
                TransactionAmount += quantity * price;
                Quantity += quantity;
                Side = PositionSide.Long;
            }
            else if (Side == PositionSide.Long)
            {
                TransactionAmount += quantity * price;
                Quantity += quantity;
            }
            else if (Side == PositionSide.Short)
            {
                TransactionAmount -= TransactionAmount * (quantity / Quantity);
                Quantity -= quantity;
                if (Quantity < 0)
                {
                    Side = PositionSide.Long;
                    Quantity = -Quantity;
                    TransactionAmount = -TransactionAmount;
                }
            }
        }

        public void Short(decimal quantity, decimal price)
        {
            if (Quantity == 0)
            {
                TransactionAmount += quantity * price;
                Quantity += quantity;
                Side = PositionSide.Short;
            }
            else if (Side == PositionSide.Short)
            {
                TransactionAmount += quantity * price;
                Quantity += quantity;
            }
            else if (Side == PositionSide.Long)
            {
                TransactionAmount -= TransactionAmount * (quantity / Quantity);
                Quantity -= quantity;
                if (Quantity < 0)
                {
                    Side = PositionSide.Short;
                    Quantity = -Quantity;
                    TransactionAmount = -TransactionAmount;
                }
            }
        }

        public override string ToString()
        {
            return (Side == PositionSide.Long ? "+" : "-") + Quantity;
        }
    }
}
