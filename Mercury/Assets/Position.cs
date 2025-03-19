using Mercury.Enums;

namespace Mercury.Assets
{
	public class Position
	{
		/// <summary>
		/// Long or Short, default is None
		/// </summary>
		public MtmPositionSide Side { get; set; } = MtmPositionSide.None;

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
		public decimal Value => Side == MtmPositionSide.Short ? -Quantity : Quantity;

		public void Long(decimal quantity, decimal price)
		{
			if (Quantity == 0)
			{
				TransactionAmount += quantity * price;
				Quantity += quantity;
				Side = MtmPositionSide.Long;
			}
			else if (Side == MtmPositionSide.Long)
			{
				TransactionAmount += quantity * price;
				Quantity += quantity;
			}
			else if (Side == MtmPositionSide.Short)
			{
				TransactionAmount -= TransactionAmount * (quantity / Quantity);
				Quantity -= quantity;
				if (Quantity < 0)
				{
					Side = MtmPositionSide.Long;
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
				Side = MtmPositionSide.Short;
			}
			else if (Side == MtmPositionSide.Short)
			{
				TransactionAmount += quantity * price;
				Quantity += quantity;
			}
			else if (Side == MtmPositionSide.Long)
			{
				TransactionAmount -= TransactionAmount * (quantity / Quantity);
				Quantity -= quantity;
				if (Quantity < 0)
				{
					Side = MtmPositionSide.Short;
					Quantity = -Quantity;
					TransactionAmount = -TransactionAmount;
				}
			}
		}

		public override string ToString()
		{
			return (Side == MtmPositionSide.Long ? "+" : "-") + Quantity;
		}
	}
}
