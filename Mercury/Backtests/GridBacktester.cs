using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests
{
	public class GridBacktester()
	{
		public decimal Seed = 1_000_000;
		public decimal Money = 1_000_000;
		public decimal Margin = 0;
		public decimal InitialMargin = 1_000_000;
		public decimal UpperPrice { get; set; }
		public decimal LowerPrice { get; set; }
		public decimal GridInterval { get; set; }
		public GridType GridType { get; set; }
		public string ReportFileName { get; set; } = string.Empty;
		public int GridCount { get; set; }
		public decimal UpperStopLossPrice { get; set; }
		public decimal LowerStopLossPrice { get; set; }

		public decimal FeeRate = 0.0002M; // 0.02%
		public decimal CoinQuantity { get; set; } = 0;
		public int LongFillCount = 0;
		public int ShortFillCount = 0;
		public List<string> PositionHistories { get; set; } = ["Time,Price,Side,OrderQuantity,Money,Margin,CoinQuantity,Pnl,NearestLong,NearestShort,LongOrder,ShortOrder"];
		public decimal StandardBaseOrderSize { get; set; }
		public string Symbol { get; set; } = string.Empty;
		public List<Order> LongOrders { get; set; } = [];
		public List<Order> ShortOrders { get; set; } = [];
		public Order NearestLongOrder = default!;
		public Order NearestShortOrder = default!;
		public List<Price> Prices { get; set; } = [];
		public List<ChartInfo> Charts { get; set; } = [];
		public ChartInfo CurrentChart(DateTime dateTime) => Charts.Where(d => d.DateTime <= dateTime).OrderByDescending(d => d.DateTime).ElementAt(0);
		public ChartInfo LastChart(DateTime dateTime) => Charts.Where(d => d.DateTime <= dateTime).OrderByDescending(d => d.DateTime).ElementAt(1);
		public ChartInfo LastChart2(DateTime dateTime) => Charts.Where(d => d.DateTime <= dateTime).OrderByDescending(d => d.DateTime).ElementAt(2);
		public int Leverage { get; set; } = 1;

		protected decimal GetPnl(decimal currentPrice)
		{
			if (CoinQuantity == 0)
			{
				return 0;
			}
			else if (CoinQuantity > 0)
			{
				return ((currentPrice - (Margin / CoinQuantity) / Leverage) * CoinQuantity).Round(2);
			}
			else
			{
				return ((Margin / Math.Abs(CoinQuantity) / Leverage - currentPrice) * Math.Abs(CoinQuantity)).Round(2);
			}
		}

		protected decimal GetEstimatedAsset(decimal currentPrice)
		{
			if (CoinQuantity == 0)
			{
				return Money;
			}
			else if (CoinQuantity > 0)
			{
				return Money + Margin + GetPnl(currentPrice);
			}
			else
			{
				return Money - Margin + GetPnl(currentPrice);
			}
		}

		protected void MakeOrder(PositionSide side, decimal price, decimal? quantity = null)
		{
			if (side == PositionSide.Long)
			{
				LongOrders.Add(new Order(Symbol, PositionSide.Long, price, quantity == null ? StandardBaseOrderSize / price : quantity.Value));
			}
			else
			{
				ShortOrders.Add(new Order(Symbol, PositionSide.Short, price, quantity == null ? StandardBaseOrderSize / price : quantity.Value));
			}

			// Refresh nearest long/short order
			NearestLongOrder = LongOrders.Find(x => x.Price.Equals(LongOrders.Max(x => x.Price))) ?? default!;
			NearestShortOrder = ShortOrders.Find(x => x.Price.Equals(ShortOrders.Min(x => x.Price))) ?? default!;
		}

		protected void Fill(Order order, int chartIndex)
		{
			if (order.Side == PositionSide.Long)
			{
				var amount = order.Price * order.Quantity;
				var effectiveAmount = amount / Leverage;
				Money -= effectiveAmount;
				Money -= effectiveAmount * FeeRate;

				if (CoinQuantity >= 0) // Additional	LONG
				{
					Margin += effectiveAmount;
				}
				else if (CoinQuantity + order.Quantity <= 0) // Close SHORT
				{
					var closeRatio = order.Quantity / -CoinQuantity;
					Margin -= Margin * closeRatio;
				}
				else // Close all SHORT and position LONG
				{
					var positionQuantity = order.Quantity + CoinQuantity;
					var positionAmount = order.Price * positionQuantity / Leverage;
					Margin = positionAmount;
				}

				CoinQuantity += order.Quantity;

				MakeOrder(PositionSide.Short, order.Price + GridInterval, order.Quantity);

				LongOrders.Remove(order);
				LongFillCount++;
			}
			else
			{
				var amount = order.Price * order.Quantity;
				var effectiveAmount = amount / Leverage;
				Money += effectiveAmount;
				Money -= effectiveAmount * FeeRate;

				if (CoinQuantity <= 0) // Additional SHORT
				{
					Margin += effectiveAmount;
				}
				else if (CoinQuantity - order.Quantity >= 0) // Close LONG
				{
					var closeRatio = order.Quantity / CoinQuantity;
					Margin -= Margin * closeRatio;
				}
				else // Close all LONG and position SHORT
				{
					var positionQuantity = order.Quantity - CoinQuantity;
					var positionAmount = order.Price * positionQuantity / Leverage;
					Margin = positionAmount;
				}

				CoinQuantity -= order.Quantity;

				MakeOrder(PositionSide.Long, order.Price - GridInterval, order.Quantity);

				ShortOrders.Remove(order);
				ShortFillCount++;
			}

			// Refresh nearest long/short order
			NearestLongOrder = LongOrders.Find(x => x.Price.Equals(LongOrders.Max(x => x.Price))) ?? default!;
			NearestShortOrder = ShortOrders.Find(x => x.Price.Equals(ShortOrders.Min(x => x.Price))) ?? default!;

			// Position History
			var nearestLongOrderPrice = 0m;
			if (NearestLongOrder != null)
			{
				nearestLongOrderPrice = NearestLongOrder.Price;
			}
			var nearestShortOrderPrice = 0m;
			if (NearestShortOrder != null)
			{
				nearestShortOrderPrice = NearestShortOrder.Price;
			}

			PositionHistories.Add($"{Prices[chartIndex].Date:yyyy-MM-dd HH:mm:ss.fff},{Prices[chartIndex].Value:#.##},{order.Side},{order.Quantity:#.##},{Money:#.##},{Margin:#.##},{CoinQuantity:#.##},{GetPnl(Prices[chartIndex].Value):#.##},{nearestLongOrderPrice:#.##},{nearestShortOrderPrice:#.##},{LongOrders.Count},{ShortOrders.Count}");
		}

		protected void CloseAllPositions(int chartIndex)
		{
			Money += CoinQuantity * Prices[chartIndex].Value;
			CoinQuantity = 0;
			Margin = 0;

			LongOrders = [];
			ShortOrders = [];

			WriteStatus(chartIndex, "CLOSE");
		}

		protected void SetOrder(int chartIndex)
		{
			var currentPrice = Prices[chartIndex].Value;
			LongOrders = [];
			ShortOrders = [];

			// 1. Set grid
			List<decimal> grids = [];
			for (int i = 0; i < GridCount; i++)
			{
				var price = LowerPrice + GridInterval * i;
				grids.Add(price);
			}

			// 2. Find nearest grid
			var nearestGrid = grids.OrderBy(x => Math.Abs(x - currentPrice)).First();

			// 3. Set order after remove nearest grid
			grids.Remove(nearestGrid);

			foreach (var grid in grids)
			{
				if (grid < currentPrice)
				{
					MakeOrder(PositionSide.Long, grid);
				}
				else
				{
					MakeOrder(PositionSide.Short, grid);
				}
			}

			// 4. Initial order (long/short)
			switch (GridType)
			{
				case GridType.Long:
					{
						var shortOrdersQuantity = ShortOrders.Sum(x => x.Quantity);
						var amount = currentPrice * shortOrdersQuantity;
						var effectiveAmount = amount / Leverage;

						Money -= effectiveAmount;
						Money -= effectiveAmount * FeeRate * 2; // Market Fee = 2 * Limit Fee
						Margin += effectiveAmount;
						CoinQuantity += shortOrdersQuantity;

						WriteStatus(chartIndex, "LONG_INIT_BUY");
					}
					break;

				case GridType.Short:
					{
						var longOrdersQuantity = LongOrders.Sum(x => x.Quantity);
						var amount = currentPrice * longOrdersQuantity;
						var effectiveAmount = amount / Leverage;

						Money += effectiveAmount;
						Money -= effectiveAmount * FeeRate * 2;
						Margin += effectiveAmount;
						CoinQuantity -= longOrdersQuantity;

						WriteStatus(chartIndex, "SHORT_INIT_SELL");
					}
					break;

				default:
					break;
			}
		}

		protected void WriteStatus(int currentIndex, string action)
		{
			var price = Prices[currentIndex].Value;
			var time = Prices[currentIndex].Date;
			var _estimatedMoney = GetEstimatedAsset(price);
			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
			$"{time:yyyy-MM-dd HH:mm:ss},{action},{CoinQuantity.Round(2)},{GridType},{LongFillCount},{ShortFillCount},EST:{_estimatedMoney.Round(0)},MAR:{Margin.Round(0)},MON:{Money.Round(0)},P:{price},PNL:{GetPnl(price)}" + Environment.NewLine);
		}
	}
}