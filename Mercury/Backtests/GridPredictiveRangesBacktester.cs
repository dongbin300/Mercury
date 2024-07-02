using Binance.Net.Enums;

using Mercury.Charts;

namespace Mercury.Backtests
{
	/// <summary>
	/// Grid Bot Predictive Ranges Backtester
	/// 
	/// 1. Predictive Ranges Average(PRA) 값이 바뀌면 전량 정리 후 그 봉은 매매하지 않음
	/// 2. 다음 봉 시가를 기준가로 그리드 재설정
	/// 3. 그리드 타입은 항상 Neutral
	/// </summary>
	public class GridPredictiveRangesBacktester(string symbol, List<Price> prices, List<ChartInfo> charts, string reportFileName, int gridCount)
	{
		public decimal Seed = 1_000_000;
		public decimal Money = 1_000_000;
		public decimal Margin = 0;
		public decimal UpperPrice { get; set; }
		public decimal LowerPrice { get; set; }
		public decimal GridInterval { get; set; }
		public string ReportFileName { get; set; } = reportFileName;
		public decimal UpperStopLossPrice { get; set; }
		public decimal LowerStopLossPrice { get; set; }
		public decimal FeeRate = 0.0002M; // 0.02%
		public decimal GridStartPrice { get; set; }

		public string Symbol { get; set; } = symbol;
		public List<Price> Prices { get; set; } = prices;
		public List<ChartInfo> Charts { get; set; } = charts;
		public ChartInfo CurrentChart(DateTime dateTime) => Charts.Where(d => d.DateTime <= dateTime).OrderByDescending(d => d.DateTime).ElementAt(0);
		public ChartInfo LastChart(DateTime dateTime) => Charts.Where(d => d.DateTime <= dateTime).OrderByDescending(d => d.DateTime).ElementAt(1);
		public List<Order> LongOrders { get; set; } = [];
		public List<Order> ShortOrders { get; set; } = [];
		public Order NearestLongOrder = default!;
		public Order NearestShortOrder = default!;
		public decimal CoinQuantity { get; set; } = 0;
		public int LongFillCount = 0;
		public int ShortFillCount = 0;
		public List<string> PositionHistories { get; set; } = [];
		public decimal StandardBaseOrderSize { get; set; }
		public decimal MaxRisk; // 리스크 최대값

		public readonly int GRID_COUNT = gridCount;

		/// <summary>
		/// 리스크가 -면 수익 중이라고 판단.
		/// 리스크가 +면 손실 중이라고 판단.
		/// 리스크가 100% 초과하면 강제 청산이라고 판단.
		/// </summary>
		/// <param name="currentPrice"></param>
		/// <returns></returns>
		decimal GetRisk(decimal currentPrice)
		{
			if (Margin == 0)
			{
				return 0;
			}

			var risk = 100 * (Margin - currentPrice * Math.Abs(CoinQuantity)) / Margin;

			if (CoinQuantity < 0)
			{
				risk *= -1;
			}

			return risk.Round(2);
		}

		decimal GetPnl(decimal currentPrice)
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

		decimal GetEstimatedAsset(decimal currentPrice)
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

		void MakeOrder(PositionSide side, decimal price)
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

		void Fill(Order order, int chartIndex)
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

		public string Run(int startIndex)
		{
			var isRest = false;
			var startTime = Prices[startIndex].Date;
			DateTime displayDate = startTime;

			SetGrid(startIndex);
			SetOrder(startIndex);

			for (int i = startIndex; i < Prices.Count; i++)
			{
				var time = Prices[i].Date;
				var price = Prices[i].Value;

				// PRA 값이 바뀌면 정리 후 그 봉에서는 매매하지 않음.
				// 다음 봉부터 다시 그리드 재설정
				if (time >= displayDate)
				{
					// 결과 출력
					displayDate = displayDate.AddDays(1);
					WriteStatus(i);

					// 그리드 재설정
					if (isRest)
					{
						isRest = false;
						SetGrid(i);
						SetOrder(i);
						WriteStatus(i);
					}

					// PRA 값이 바뀜
					if (LastChart(time).PredictiveRangesAverage != CurrentChart(time).PredictiveRangesAverage)
					{
						WriteStatus(i);
						CloseAllPositions(i);
						isRest = true;
					}
				}

				// 매매 Filled
				if (NearestLongOrder != null && NearestLongOrder.Price >= price)
				{
					Fill(NearestLongOrder, i);
				}
				if (NearestShortOrder != null && NearestShortOrder.Price <= price)
				{
					Fill(NearestShortOrder, i);
				}

				// Trailing
				//TrailingOrder(price);
			}

			var estimatedMoney = GetEstimatedAsset(Prices[^1].Value);
			var period = (Prices[^1].Date - Prices[0].Date).Days + 1;
			var tradePerDay = (double)(LongFillCount + ShortFillCount) / period;
			var resultString = $"{tradePerDay.Round(1)},{MaxRisk.Round(2)}%,{(int)estimatedMoney}";
			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
					 resultString + Environment.NewLine + Environment.NewLine);

			// Position History
			//File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}_position.csv"),
			//	string.Join(Environment.NewLine, PositionHistories) + Environment.NewLine + Environment.NewLine + Environment.NewLine
			//	);

			return resultString;
		}

		public void WriteStatus(int currentIndex)
		{
			var price = Prices[currentIndex].Value;
			var time = Prices[currentIndex].Date;
			var _estimatedMoney = GetEstimatedAsset(price);
			var risk = GetRisk(price);
			if (risk > MaxRisk)
			{
				MaxRisk = risk;
			}
			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
			$"{time:yyyy-MM-dd HH:mm:ss},{CoinQuantity.Round(2)},{LongFillCount},{ShortFillCount},{risk.Round(2)}%,{_estimatedMoney.Round(2)},{Margin.Round(2)},M:{Money.Round(2)},P:{price},PNL:{GetPnl(price)}" + Environment.NewLine);
		}

		public void CloseAllPositions(int chartIndex)
		{
			Money += CoinQuantity * Prices[chartIndex].Value;
			CoinQuantity = 0;
			Margin = 0;

			LongOrders = [];
			ShortOrders = [];
		}

		public void SetGrid(int chartIndex)
		{
			var currentPrice = Prices[chartIndex].Value;
			var currentTime = Prices[chartIndex].Date;

			UpperPrice = (decimal)CurrentChart(currentTime).PredictiveRangesUpper2;
			LowerPrice = (decimal)CurrentChart(currentTime).PredictiveRangesLower2;
			UpperStopLossPrice = (decimal)CurrentChart(currentTime).PredictiveRangesUpper2;
			LowerStopLossPrice = (decimal)CurrentChart(currentTime).PredictiveRangesLower2;

			GridInterval = (UpperPrice - LowerPrice) / GRID_COUNT;
			StandardBaseOrderSize = Money / GRID_COUNT;
			GridStartPrice = currentPrice;

			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
			$"{currentTime:yyyy-MM-dd HH:mm:ss},CurrentPrice:{currentPrice.Round(2)},Upper:{UpperPrice.Round(2)},Lower:{LowerPrice.Round(2)},UpperStopLoss:{UpperStopLossPrice.Round(2)},LowerStopLoss:{LowerStopLossPrice.Round(2)},GridInterval:{GridInterval.Round(2)},GridCount:{GRID_COUNT},StandardBaseOrderSize:{StandardBaseOrderSize.Round(2)}" + Environment.NewLine);
		}

		public void SetOrder(int chartIndex)
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
		}
	}
}