using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

using System.Diagnostics;

namespace Mercury.Backtests
{
	/// <summary>
	/// Grid Bot EMA Backtester
	/// </summary>
	/// <param name="symbol"></param>
	/// <param name="prices"></param>
	/// <param name="longTermCharts"></param>
	/// <param name="midTermCharts"></param>
	/// <param name="shortTermCharts"></param>
	/// <param name="gridIntervalRatio"></param>
	/// <param name="gridType"></param>
	public class GridEmaBacktester(string symbol, List<Price> prices, List<ChartInfo> longTermCharts, List<ChartInfo> shortTermCharts, GridType gridType, GridTypeChange gridTypeChange, string reportFileName)
	{
		public decimal Seed = 1_000_000;
		public decimal Money = 1_000_000;
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

		/// <summary>
		/// 표준 주문 사이즈
		/// </summary>
		public decimal StandardBaseOrderSize { get; set; }


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

		void Fill(Order order)
		{
			if (order.Side == PositionSide.Long)
			{
				var amount = order.Price * order.Quantity;
				Money -= amount;
				Money -= amount * FeeRate;
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
				Money += amount;
				Money -= amount * FeeRate;
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

				var price = Prices[i];

				if (SetGridType(i))
				{ // Grid type changed
					CloseAllPositions(i);
					SetGrid(i);
					SetStandardBaseOrderSize(i);
					SetOrder(i);

					var _estimatedMoney = Money + CoinQuantity * Prices[i].Value;
					var risk = Math.Abs(_estimatedMoney - Money) / _estimatedMoney * 100;
					if (risk > maxRisk)
					{
						maxRisk = risk;
					}
					File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
					$"{Prices[i].Date:yyyy-MM-dd HH:mm:ss},{CoinQuantity.Round(2)},{GridType},{LongFillCount},{ShortFillCount},{risk.Round(2)}%,{_estimatedMoney.Round(2)}" + Environment.NewLine);
				}

				if (NearestLongOrder != null && NearestLongOrder.Price >= price.Value)
				{
					Fill(NearestLongOrder);
				}
				if (NearestShortOrder != null && NearestShortOrder.Price <= price.Value)
				{
					Fill(NearestShortOrder);
				}

				TrailingOrder(price.Value);

				if (price.Date >= displayDate)
				{
					displayDate = displayDate.AddDays(1);

					var _estimatedMoney = Money + CoinQuantity * Prices[i].Value;
					var risk = Math.Abs(_estimatedMoney - Money) / _estimatedMoney * 100;
					if (risk > maxRisk)
					{
						maxRisk = risk;
					}
					File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
					$"{Prices[i].Date:yyyy-MM-dd HH:mm:ss},{CoinQuantity.Round(2)},{GridType},{LongFillCount},{ShortFillCount},{risk.Round(2)}%,{_estimatedMoney.Round(2)}" + Environment.NewLine);
				}
			}

			var estimatedMoney = Money + CoinQuantity * Prices[^1].Value;
			var period = (Prices[^1].Date - Prices[0].Date).Days;
			var tradePerDay = (double)(LongFillCount + ShortFillCount) / period;
			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
					$"{Symbol},{period}Days,{tradePerDay.Round(1)}/d,{maxRisk.Round(2)}%,{estimatedMoney.Round(2)}" + Environment.NewLine + Environment.NewLine);
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
		}

		public void SetGrid(int chartIndex)
		{
			var price = Prices[chartIndex];
			var stopLossMargin = 0.2m; // 20%

			var longTermChartsOrderByDescending = LongTermCharts.Where(d => d.DateTime <= price.Date).OrderByDescending(d => d.DateTime);
			var longTermEma = (decimal)longTermChartsOrderByDescending.ElementAt(1).Ema1;
			var longTermHighPrice = longTermChartsOrderByDescending.Skip(1).Take(40).Max(x => x.Quote.High);
			var longTermLowPrice = longTermChartsOrderByDescending.Skip(1).Take(40).Min(x => x.Quote.Low);

			var shortTermAverageAtr = (decimal)ShortTermCharts.Where(d => d.DateTime <= price.Date).OrderByDescending(d => d.DateTime).Skip(1).Take(48).Average(x => x.Atr);

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
				UpperStopLossPrice = UpperPrice + diffFromEma * stopLossMargin;
				LowerStopLossPrice = LowerPrice - diffFromEma * stopLossMargin;
				GridInterval = shortTermAverageAtr;
			}
			else if (GridTypeChange == GridTypeChange.ShortToNeutral)
			{
				var diffFromEma = longTermEma - longTermLowPrice;
				UpperPrice = longTermEma + diffFromEma;
				LowerPrice = longTermLowPrice;
				UpperStopLossPrice = UpperPrice + diffFromEma * stopLossMargin;
				LowerStopLossPrice = LowerPrice - diffFromEma * stopLossMargin;
				GridInterval = shortTermAverageAtr;
			}

			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
			$"{price.Date:yyyy-MM-dd HH:mm:ss},{GridTypeChange},Upper:{UpperPrice.Round(2)},Lower:{LowerPrice.Round(2)},UpperStopLoss:{UpperStopLossPrice.Round(2)},LowerStopLoss:{LowerStopLossPrice.Round(2)},GridInterval:{GridInterval.Round(2)}" + Environment.NewLine);
		}

		public void SetStandardBaseOrderSize(int chartIndex)
		{
			var currentPrice = Prices[chartIndex].Value;
			var upperPrice = GridType switch {
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

			StandardBaseOrderSize = Seed / gridCount; // Max risk 100%

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