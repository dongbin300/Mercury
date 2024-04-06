using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests
{
	/// <summary>
	/// Grid Bot EMA Backtester
	/// </summary>
	/// <param name="symbol"></param>
	/// <param name="prices"></param>
	/// <param name="longTermCharts"></param>
	/// <param name="shortTermCharts"></param>
	/// <param name="gridType"></param>
	/// <param name="gridTypeChange"></param>
	/// <param name="reportFileName"></param>
	public class GridTrendRiderBacktester(string symbol, List<Price> prices, List<ChartInfo> longTermCharts, List<ChartInfo> shortTermCharts, GridType gridType, GridTypeChange gridTypeChange, string reportFileName)
	{
		public decimal Seed = 1_000_000;
		public decimal Money = 1_000_000;
		public decimal Leverage = 1;
		public decimal Margin = 0;
		public decimal UpperPrice { get; set; }
		public decimal LowerPrice { get; set; }
		public decimal GridInterval { get; set; }
		public GridType GridType { get; set; } = gridType;
		public GridTypeChange GridTypeChange { get; set; } = gridTypeChange;
		public string ReportFileName { get; set; } = reportFileName;
		public decimal UpperStopLossPrice { get; set; }
		public decimal LowerStopLossPrice { get; set; }
		public decimal FeeRate = 0.0002M; // 0.02%

		public string Symbol { get; set; } = symbol;
		public List<Price> Prices { get; set; } = prices;
		public List<ChartInfo> LongTermCharts { get; set; } = longTermCharts;
		public List<ChartInfo> ShortTermCharts { get; set; } = shortTermCharts;
		public List<Order> LongOrders { get; set; } = [];
		public List<Order> ShortOrders { get; set; } = [];
		public Order NearestLongOrder = default!;
		public Order NearestShortOrder = default!;
		public decimal CoinQuantity { get; set; } = 0;
		public int LongFillCount = 0;
		public int ShortFillCount = 0;
		public List<string> PositionHistories { get; set; } = [];

		/// <summary>
		/// 표준 주문 사이즈
		/// </summary>
		public decimal StandardBaseOrderSize { get; set; }

		public readonly int ATR_COUNT = 24;
		public readonly decimal STOP_LOSS_MARGIN = 0.2m;
		public readonly int BASE_ORDER_SIZE_DIVISION = 1; // T: 2, F: 1


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

			var risk = 100 * (Margin * Leverage - currentPrice * Math.Abs(CoinQuantity)) / (Margin + Money); // 지정가 주문이 청산가에 영향을 주면 Margin, 안주면 Margin + Money

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
				return ((currentPrice - Margin * Leverage / CoinQuantity) * CoinQuantity).Round(2);
			}
			else
			{
				return ((Margin * Leverage / Math.Abs(CoinQuantity) - currentPrice) * Math.Abs(CoinQuantity)).Round(2);
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
				Money -= amount / Leverage;
				Money -= amount * FeeRate;

				if (CoinQuantity >= 0) // Additional LONG
				{
					Margin += amount / Leverage;
				}
				else if (CoinQuantity + order.Quantity <= 0) // Close SHORT
				{
					var closeRatio = order.Quantity / -CoinQuantity;
					Margin -= Margin * closeRatio;
				}
				else // Close all SHORT and position LONG
				{
					var positionQuantity = order.Quantity + CoinQuantity;
					var positionAmount = order.Price * positionQuantity * Leverage;
					Margin = positionAmount / Leverage;
				}

				CoinQuantity += order.Quantity;

				if (!ShortOrders.Any(x => x.Price.Equals(order.Price + GridInterval)))
				{
					MakeOrder(PositionSide.Short, order.Price + GridInterval);
				}

				LongOrders.Remove(order);
				LongFillCount++;
			}
			else
			{
				var amount = order.Price * order.Quantity;
				Money += amount / Leverage;
				Money -= amount * FeeRate;

				if (CoinQuantity <= 0) // Additional SHORT
				{
					Margin += amount / Leverage;
				}
				else if (CoinQuantity - order.Quantity >= 0) // Close LONG
				{
					var closeRatio = order.Quantity / CoinQuantity;
					Margin -= Margin * closeRatio;
				}
				else // Close all LONG and position SHORT
				{
					var positionQuantity = order.Quantity - CoinQuantity;
					var positionAmount = order.Price * positionQuantity * Leverage;
					Margin = positionAmount / Leverage;
				}

				CoinQuantity -= order.Quantity;

				if (!LongOrders.Any(x => x.Price.Equals(order.Price - GridInterval)))
				{
					MakeOrder(PositionSide.Long, order.Price - GridInterval);
				}

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

		public void RunManual(Action<int> reportProgress, Action<int, int> reportProgressCount, int startIndex, decimal upper, decimal lower, decimal upperStopLoss, decimal lowerStopLoss, decimal gridInterval)
		{
			UpperPrice = upper;
			LowerPrice = lower;
			UpperStopLossPrice = upperStopLoss;
			LowerStopLossPrice = lowerStopLoss;
			GridInterval = gridInterval;

			SetStandardBaseOrderSize(startIndex);
			SetOrder(startIndex);

			decimal maxRisk = 0;
			DateTime displayDate = Prices[startIndex].Date;
			for (int i = startIndex; i < Prices.Count; i++)
			{
				reportProgress((int)((double)i / Prices.Count * 100));
				reportProgressCount(i, Prices.Count);

				var time = Prices[i].Date;
				var price = Prices[i].Value;

				if (SetGridType(i))
				{ // Grid type changed
					CloseAllPositions(i);
					SetGrid(i);
					SetStandardBaseOrderSize(i);
					SetOrder(i);

					var _estimatedMoney = GetEstimatedAsset(price);
					var risk = GetRisk(price);
					if (risk > maxRisk)
					{
						maxRisk = risk;
					}
					File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
					$"{Prices[i].Date:yyyy-MM-dd HH:mm:ss},{CoinQuantity.Round(2)},{GridType},{LongFillCount},{ShortFillCount},{risk.Round(2)}%,{_estimatedMoney.Round(2)},{Margin.Round(2)},M:{Money.Round(2)},P:{price},PNL:{GetPnl(price)}" + Environment.NewLine);
				}

				if (NearestLongOrder != null && NearestLongOrder.Price >= price)
				{
					Fill(NearestLongOrder, i);
				}
				if (NearestShortOrder != null && NearestShortOrder.Price <= price)
				{
					Fill(NearestShortOrder, i);
				}

				TrailingOrder(price);

				if (time >= displayDate)
				{
					displayDate = displayDate.AddMinutes(1);

					var _estimatedMoney = GetEstimatedAsset(price);
					var risk = GetRisk(price);
					if (risk > maxRisk)
					{
						maxRisk = risk;
					}
					File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
					$"{time:yyyy-MM-dd HH:mm:ss},{CoinQuantity.Round(2)},{GridType},{LongFillCount},{ShortFillCount},{risk.Round(2)}%,{_estimatedMoney.Round(2)},{Margin.Round(2)},M:{Money.Round(2)},P:{price},PNL:{GetPnl(price)}" + Environment.NewLine);
				}
			}

			var estimatedMoney = GetEstimatedAsset(Prices[^1].Value);
			var period = (Prices[^1].Date - Prices[0].Date).Days + 1;
			var tradePerDay = (double)(LongFillCount + ShortFillCount) / period;
			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
					$"{Symbol},{period}Days,{tradePerDay.Round(1)}/d,{maxRisk.Round(2)}%,{estimatedMoney.Round(2)},{Margin.Round(2)}" + Environment.NewLine + Environment.NewLine);

			// Position History
			//File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}_position.csv"),
			//	string.Join(Environment.NewLine, PositionHistories) + Environment.NewLine + Environment.NewLine + Environment.NewLine
			//	);
		}

		public void Run(Action<int> reportProgress, Action<int, int> reportProgressCount, int startIndex)
		{
			/* Init */
			SetGrid(startIndex);
			SetStandardBaseOrderSize(startIndex);
			SetOrder(startIndex);

			decimal maxRisk = 0;
			DateTime displayDate = Prices[startIndex].Date;
			for (int i = startIndex; i < Prices.Count; i++)
			{
				reportProgress((int)((double)i / Prices.Count * 100));
				reportProgressCount(i, Prices.Count);

				var time = Prices[i].Date;
				var price = Prices[i].Value;

				if (SetGridType(i))
				{ // Grid type changed
					CloseAllPositions(i);
					SetGrid(i);
					SetStandardBaseOrderSize(i);
					SetOrder(i);

					var _estimatedMoney = GetEstimatedAsset(price);
					var risk = GetRisk(price);
					if (risk > maxRisk)
					{
						maxRisk = risk;
					}
					File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
					$"{time:yyyy-MM-dd HH:mm:ss},{CoinQuantity.Round(2)},{GridType},{LongFillCount},{ShortFillCount},{risk.Round(2)}%,{_estimatedMoney.Round(2)},{Margin.Round(2)},M:{Money.Round(2)},P:{price},PNL:{GetPnl(price)}" + Environment.NewLine);
				}

				if (NearestLongOrder != null && NearestLongOrder.Price >= price)
				{
					Fill(NearestLongOrder, i);
				}
				if (NearestShortOrder != null && NearestShortOrder.Price <= price)
				{
					Fill(NearestShortOrder, i);
				}

				TrailingOrder(price);

				if (time >= displayDate)
				{
					displayDate = displayDate.AddDays(1);

					var _estimatedMoney = GetEstimatedAsset(price);
					var risk = GetRisk(price);
					if (risk > maxRisk)
					{
						maxRisk = risk;
					}
					File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
					$"{time:yyyy-MM-dd HH:mm:ss},{CoinQuantity.Round(2)},{GridType},{LongFillCount},{ShortFillCount},{risk.Round(2)}%,{_estimatedMoney.Round(2)},{Margin.Round(2)},M:{Money.Round(2)},P:{price},PNL:{GetPnl(price)}" + Environment.NewLine);
				}
			}

			var estimatedMoney = GetEstimatedAsset(Prices[^1].Value);
			var period = (Prices[^1].Date - Prices[0].Date).Days + 1;
			var tradePerDay = (double)(LongFillCount + ShortFillCount) / period;
			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
					$"{Symbol},{period}Days,{tradePerDay.Round(1)}/d,{maxRisk.Round(2)}%,{estimatedMoney.Round(2)},{Margin.Round(2)}" + Environment.NewLine + Environment.NewLine);

			// Position History
			//File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}_position.csv"),
			//	string.Join(Environment.NewLine, PositionHistories) + Environment.NewLine + Environment.NewLine + Environment.NewLine
			//	);
		}

		public bool SetGridType(int chartIndex)
		{
			var price = Prices[chartIndex];
			var longTermEma = (decimal)LongTermCharts.Where(d => d.DateTime <= price.Date).OrderByDescending(d => d.DateTime).ElementAt(1).Ema1;

			if (GridType == GridType.Long)
			{
				if (price.Value <= longTermEma)
				{
					GridType = GridType.Neutral;
					GridTypeChange = GridTypeChange.LongToNeutral;
					return true;
				}
			}
			else if (GridType == GridType.Short)
			{
				if (price.Value >= longTermEma)
				{
					GridType = GridType.Neutral;
					GridTypeChange = GridTypeChange.ShortToNeutral;
					return true;
				}
			}
			else if (GridType == GridType.Neutral)
			{
				if (price.Value >= UpperStopLossPrice)
				{
					GridType = GridType.Long;
					GridTypeChange = GridTypeChange.NeutralToLong;
					return true;
				}
				else if (price.Value <= LowerStopLossPrice)
				{
					GridType = GridType.Short;
					GridTypeChange = GridTypeChange.NeutralToShort;
					return true;
				}
			}
			return false;
		}

		public void CloseAllPositions(int chartIndex)
		{
			Money += CoinQuantity * Prices[chartIndex].Value;
			CoinQuantity = 0;
			Margin = 0;
		}

		public void SetGrid(int chartIndex)
		{
			var price = Prices[chartIndex];

			var longTermChartsOrderByDescending = LongTermCharts.Where(d => d.DateTime <= price.Date).OrderByDescending(d => d.DateTime);
			var longTermEma = (decimal)longTermChartsOrderByDescending.ElementAt(1).Ema1;
			var longTermHighPrice = longTermChartsOrderByDescending.Skip(1).Take(40).Max(x => x.Quote.High);
			var longTermLowPrice = longTermChartsOrderByDescending.Skip(1).Take(40).Min(x => x.Quote.Low);

			var shortTermAverageAtr = (decimal)ShortTermCharts.Where(d => d.DateTime <= price.Date).OrderByDescending(d => d.DateTime).Skip(1).Take(ATR_COUNT).Average(x => x.Atr);

			if (GridType == GridType.Long)
			{
				UpperPrice = decimal.MaxValue;
				LowerPrice = longTermEma;
				UpperStopLossPrice = decimal.MaxValue;
				LowerStopLossPrice = decimal.MinValue;
				GridInterval = shortTermAverageAtr;
			}
			else if (GridType == GridType.Short)
			{
				UpperPrice = longTermEma;
				LowerPrice = decimal.MinValue;
				UpperStopLossPrice = decimal.MaxValue;
				LowerStopLossPrice = decimal.MinValue;
				GridInterval = shortTermAverageAtr;
			}
			else if (GridTypeChange == GridTypeChange.LongToNeutral)
			{
				var diffFromEma = longTermHighPrice - longTermEma;
				UpperPrice = longTermHighPrice;
				LowerPrice = longTermEma - diffFromEma;
				UpperStopLossPrice = UpperPrice + diffFromEma * STOP_LOSS_MARGIN;
				LowerStopLossPrice = LowerPrice - diffFromEma * STOP_LOSS_MARGIN;
				GridInterval = shortTermAverageAtr;
			}
			else if (GridTypeChange == GridTypeChange.ShortToNeutral)
			{
				var diffFromEma = longTermEma - longTermLowPrice;
				UpperPrice = longTermEma + diffFromEma;
				LowerPrice = longTermLowPrice;
				UpperStopLossPrice = UpperPrice + diffFromEma * STOP_LOSS_MARGIN;
				LowerStopLossPrice = LowerPrice - diffFromEma * STOP_LOSS_MARGIN;
				GridInterval = shortTermAverageAtr;
			}

			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
			$"{price.Date:yyyy-MM-dd HH:mm:ss},{GridTypeChange},Upper:{UpperPrice.Round(2)},Lower:{LowerPrice.Round(2)},UpperStopLoss:{UpperStopLossPrice.Round(2)},LowerStopLoss:{LowerStopLossPrice.Round(2)},GridInterval:{GridInterval.Round(2)}" + Environment.NewLine);
		}

		public void SetStandardBaseOrderSize(int chartIndex)
		{
			var currentPrice = Prices[chartIndex].Value;
			var upperPrice = GridType switch
			{
				GridType.Long => currentPrice,
				GridType.Short => UpperPrice,
				GridType.Neutral => UpperPrice,
				_ => UpperPrice
			};
			var lowerPrice = GridType switch
			{
				GridType.Long => LowerPrice,
				GridType.Short => currentPrice,
				GridType.Neutral => LowerPrice,
				_ => LowerPrice
			};
			var gridCount = (int)((upperPrice - lowerPrice) / GridInterval) + 1;

			StandardBaseOrderSize = GridType switch
			{
				GridType.Long => Seed / gridCount * Leverage / BASE_ORDER_SIZE_DIVISION,
				GridType.Short => Seed / gridCount * Leverage / BASE_ORDER_SIZE_DIVISION,
				GridType.Neutral => Seed / gridCount * Leverage,
				_ => Seed / gridCount
			}; // Max risk 100%

			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
			$"{Prices[chartIndex].Date:yyyy-MM-dd HH:mm:ss},CurrentPrice:{currentPrice.Round(2)},GridCount:{gridCount},StandardBaseOrderSize:{StandardBaseOrderSize.Round(2)}" + Environment.NewLine);
		}

		public void SetOrder(int chartIndex)
		{
			var currentPrice = Prices[chartIndex].Value;
			LongOrders.Clear();
			ShortOrders.Clear();
			if (GridType == GridType.Neutral)
			{
				for (decimal i = LowerPrice; i <= UpperPrice; i += GridInterval)
				{
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
			else if (GridType == GridType.Long)
			{
				for (decimal i = LowerPrice; i <= currentPrice; i += GridInterval)
				{
					MakeOrder(PositionSide.Long, i);
				}
			}
			else if (GridType == GridType.Short)
			{
				for (decimal i = currentPrice; i <= UpperPrice; i += GridInterval)
				{
					MakeOrder(PositionSide.Short, i);
				}
			}
		}

		public void TrailingOrder(decimal price)
		{
			if (GridType == GridType.Neutral)
			{
				return;
			}

			if (GridType == GridType.Long)
			{
				if (NearestLongOrder == null)
				{
					return;
				}

				// 가장 가까운 롱 주문으로부터 2인터벌 이상 멀어지면 롱 주문 추가
				if (price > NearestLongOrder.Price + 2 * GridInterval)
				{
					MakeOrder(PositionSide.Long, NearestLongOrder.Price + GridInterval);
				}
			}
			else if (GridType == GridType.Short)
			{
				if (NearestShortOrder == null)
				{
					return;
				}

				// 가장 가까운 숏 주문으로부터 2인터벌 이상 멀어지면 숏 주문 추가
				if (price < NearestShortOrder.Price - 2 * GridInterval)
				{
					MakeOrder(PositionSide.Short, NearestShortOrder.Price - GridInterval);
				}
			}
		}
	}
}