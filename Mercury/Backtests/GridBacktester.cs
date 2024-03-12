using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests
{
	/// <summary>
	/// Grid Bot Backtester
	/// </summary>
	/// <param name="charts"></param>
	/// <param name="interval"></param>
	/// <param name="upperPrice"></param>
	/// <param name="lowerPrice"></param>
	/// <param name="gridInterval"></param>
	/// <param name="gridIntervalRatio"></param>
	/// <param name="gridType"></param>
	/// <param name="upperStopLossPrice"></param>
	/// <param name="lowerStopLossPrice"></param>
	public class GridBacktester(List<ChartInfo> charts, KlineInterval interval, decimal upperPrice, decimal lowerPrice, decimal gridInterval, decimal gridIntervalRatio, GridType gridType, decimal upperStopLossPrice, decimal lowerStopLossPrice)
	{
		public decimal Money = 1_000_000;
		public decimal UpperPrice { get; set; } = upperPrice;
		public decimal LowerPrice { get; set; } = lowerPrice;
		public decimal GridInterval { get; set; } = gridInterval;
		public decimal GridIntervalRatio { get; set; } = gridIntervalRatio;
		public GridType GridType { get; set; } = gridType;
		public decimal UpperStopLossPrice { get; set; } = upperStopLossPrice;
		public decimal LowerStopLossPrice { get; set; } = lowerStopLossPrice;

		public decimal BaseQuantity = 1;
		public decimal FeeRate = 0.0002M; // 0.02%

		public string Symbol => Charts[0].Symbol;
		public KlineInterval Interval { get; set; } = interval;
		public List<ChartInfo> Charts { get; set; } = charts;
		public List<Order> Orders { get; set; } = [];
		public decimal CoinQuantity { get; set; } = 0;

		public List<Order> AddOrders = [];
		public List<Order> RemoveOrders = [];

		void Fill(Order order)
		{
			if (order.Side == PositionSide.Long)
			{
				var amount = order.Price * order.Quantity;
				Money -= amount;
				Money -= amount * FeeRate;
				CoinQuantity += order.Quantity;

				if (!Orders.Any(x => x.Price.Equals(order.Price + GridInterval)))
				{
					AddOrders.Add(new Order(Symbol, PositionSide.Short, order.Price + GridInterval, BaseQuantity));
				}
			}
			else
			{
				var amount = order.Price * order.Quantity;
				Money += amount;
				Money -= amount * FeeRate;
				CoinQuantity -= order.Quantity;

				if (!Orders.Any(x => x.Price.Equals(order.Price - GridInterval)))
				{
					AddOrders.Add(new Order(Symbol, PositionSide.Long, order.Price - GridInterval, BaseQuantity));
				}
			}

			RemoveOrders.Add(order);
		}

		public void Run(Action<int> reportProgress, string reportFileName, int reportInterval, int startIndex)
		{
			var currentPrice = Charts[startIndex].Quote.Open;
			for (decimal i = LowerPrice; i <= UpperPrice; i += GridInterval)
			{
				if (i < currentPrice)
				{
					Orders.Add(new Order(Symbol, PositionSide.Long, i, BaseQuantity));
				}
				else
				{
					Orders.Add(new Order(Symbol, PositionSide.Short, i, BaseQuantity));
				}
			}

			var currentTime = DateTime.Now;
			var maxChartCount = Charts.Count;
			for (int i = startIndex; i < maxChartCount; i++)
			{
				reportProgress((int)(50 + (double)i / maxChartCount * 50));

				AddOrders = [];
				RemoveOrders = [];
				foreach (var order in Orders)
				{
					if (order.Side == PositionSide.Long && order.Price > Charts[i].Quote.Low)
					{
						Fill(order);
					}
					if (order.Side == PositionSide.Short && order.Price < Charts[i].Quote.High)
					{
						Fill(order);
					}
				}

				Orders.RemoveAll(x => RemoveOrders.Contains(x));
				Orders.AddRange(AddOrders);
			}

			var estimatedMoney = Money + CoinQuantity * Charts[^1].Quote.Close;
			File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}.csv"),
					$"{Symbol},{estimatedMoney.Round(2)}" + Environment.NewLine);
		}
	}
}