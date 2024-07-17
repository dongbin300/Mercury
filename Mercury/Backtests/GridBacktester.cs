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
		public List<string> PositionHistories { get; set; } = [];
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


		protected decimal GetPnl(decimal currentPrice)
		{
			if (CoinQuantity == 0)
			{
				return 0;
			}
			else if (CoinQuantity > 0)
			{
				return ((currentPrice - Margin / CoinQuantity) * CoinQuantity).Round(2);
			}
			else
			{
				return ((Margin / Math.Abs(CoinQuantity) - currentPrice) * Math.Abs(CoinQuantity)).Round(2);
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

		protected void MakeOrder(PositionSide side, decimal price)
		{
			if (side == PositionSide.Long)
			{
				var quantity = StandardBaseOrderSize / price;
				LongOrders.Add(new Order(Symbol, PositionSide.Long, price, quantity));
			}
			else
			{
				var quantity = StandardBaseOrderSize / price;
				ShortOrders.Add(new Order(Symbol, PositionSide.Short, price, quantity));
			}

			// Refresh nearest long/short order
			NearestLongOrder = LongOrders.Find(x => x.Price.Equals(LongOrders.Max(x => x.Price))) ?? default!;
			NearestShortOrder = ShortOrders.Find(x => x.Price.Equals(ShortOrders.Min(x => x.Price))) ?? default!;
		}

		protected void InitMarketBuy(int chartIndex)
		{
			var currentPrice = Prices[chartIndex].Value;
			var shortOrdersQuantity = ShortOrders.Sum(x => x.Quantity);
			var amount = currentPrice * shortOrdersQuantity;
			Money -= amount;
			Money -= amount * FeeRate * 2; // Market Fee = 2 * Limit Fee
			Margin += amount;
			CoinQuantity += shortOrdersQuantity;

			// 그리드 시작 시 생성한 Long Position과 함께 처리
			//foreach (var shortOrder in ShortOrders)
			//{
			//	shortOrder.Quantity *= 2;
			//}
		}

		protected void InitMarketSell(int chartIndex)
		{
			var currentPrice = Prices[chartIndex].Value;
			var longOrdersQuantity = LongOrders.Sum(x => x.Quantity);
			var amount = currentPrice * longOrdersQuantity;
			Money += amount;
			Money -= amount * FeeRate * 2;
			Margin += amount;
			CoinQuantity -= longOrdersQuantity;

			//foreach (var longOrder in LongOrders)
			//{
			//	longOrder.Quantity *= 2;
			//}
		}

		protected void Fill(Order order, int chartIndex)
		{
			if (order.Side == PositionSide.Long)
			{
				var amount = order.Price * order.Quantity;
				Money -= amount;
				Money -= amount * FeeRate;

				if (CoinQuantity >= 0) // Additional LONG
				{
					Margin += amount;
				}
				else if (CoinQuantity + order.Quantity <= 0) // Close SHORT
				{
					var closeRatio = order.Quantity / -CoinQuantity;
					Margin -= Margin * closeRatio;
				}
				else // Close all SHORT and position LONG
				{
					var positionQuantity = order.Quantity + CoinQuantity;
					var positionAmount = order.Price * positionQuantity;
					Margin = positionAmount;
				}

				CoinQuantity += order.Quantity;

				MakeOrder(PositionSide.Short, order.Price + GridInterval);

				LongOrders.Remove(order);
				LongFillCount++;
			}
			else
			{
				var amount = order.Price * order.Quantity;
				Money += amount;
				Money -= amount * FeeRate;

				if (CoinQuantity <= 0) // Additional SHORT
				{
					Margin += amount;
				}
				else if (CoinQuantity - order.Quantity >= 0) // Close LONG
				{
					var closeRatio = order.Quantity / CoinQuantity;
					Margin -= Margin * closeRatio;
				}
				else // Close all LONG and position SHORT
				{
					var positionQuantity = order.Quantity - CoinQuantity;
					var positionAmount = order.Price * positionQuantity;
					Margin = positionAmount;
				}

				CoinQuantity -= order.Quantity;

				MakeOrder(PositionSide.Long, order.Price - GridInterval);

				ShortOrders.Remove(order);
				ShortFillCount++;
			}

			// Refresh nearest long/short order
			NearestLongOrder = LongOrders.Find(x => x.Price.Equals(LongOrders.Max(x => x.Price))) ?? default!;
			NearestShortOrder = ShortOrders.Find(x => x.Price.Equals(ShortOrders.Min(x => x.Price))) ?? default!;

			// Position History
			//var nearestLongOrderPrice = 0m;
			//if (NearestLongOrder != null)
			//{
			//	nearestLongOrderPrice = NearestLongOrder.Price;
			//}
			//var nearestShortOrderPrice = 0m;
			//if (NearestShortOrder != null)
			//{
			//	nearestShortOrderPrice = NearestShortOrder.Price;
			//}

			//PositionHistories.Add($"{Prices[chartIndex].Date:yyyy-MM-dd HH:mm:ss.fff},{Prices[chartIndex].Value:#.##},{order.Side},{order.Quantity:#.##},{Money:#.##},{Margin:#.##},{CoinQuantity:#.##},{GetPnl(Prices[chartIndex].Value):#.##},{nearestLongOrderPrice:#.##},{nearestShortOrderPrice:#.##}");
		}

		protected void CloseAllPositions(int chartIndex)
		{
			Money += CoinQuantity * Prices[chartIndex].Value;
			CoinQuantity = 0;
			Margin = 0;

			LongOrders = [];
			ShortOrders = [];
		}

		protected void ApplyLeverage(int chartIndex)
		{
			var time = Prices[chartIndex].Date;
			var leverage = (decimal)LastChart(time).PredictiveRangesMaxLeverage;

			Money = InitialMargin + ((Money - InitialMargin) * leverage);
		}

		protected void SetOrder(int chartIndex)
		{
			var currentPrice = Prices[chartIndex].Value;
			LongOrders = [];
			ShortOrders = [];

			for (decimal i = LowerPrice; i <= UpperPrice; i += GridInterval)
			{
				if (Math.Abs(currentPrice - i) < 1m)
				{
					continue;
				}

				if (i < currentPrice)
				{
					MakeOrder(PositionSide.Long, i);
				}
				else
				{
					MakeOrder(PositionSide.Short, i);
				}
			}

			if (GridType == GridType.Long)
			{
				InitMarketBuy(chartIndex);
			}
			else if (GridType == GridType.Short)
			{
				InitMarketSell(chartIndex);
			}
		}

		protected void WriteStatus(int currentIndex)
		{
			var price = Prices[currentIndex].Value;
			var time = Prices[currentIndex].Date;
			var _estimatedMoney = (int)GetEstimatedAsset(price);
			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
			$"{time:yyyy-MM-dd HH:mm:ss},{CoinQuantity.Round(2)},{GridType},{LongFillCount},{ShortFillCount},EST:{_estimatedMoney},MAR:{(int)Margin},MON:{(int)Money},P:{price},PNL:{GetPnl(price)}" + Environment.NewLine);
		}
	}
}