using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;
using Mercury.Extensions;

namespace Mercury.Backtests
{
    /// <summary>
    /// Grid Bot Backtester
    /// </summary>
    /// <param name="prices"></param>
    /// <param name="longTermCharts"></param>
    /// <param name="midTermCharts"></param>
    /// <param name="shortTermCharts"></param>
    /// <param name="interval"></param>
    /// <param name="gridIntervalRatio"></param>
    /// <param name="gridType"></param>
    public class GridDetailBacktester(string symbol, List<Price> prices, List<ChartInfo> longTermCharts, List<ChartInfo> midTermCharts, List<ChartInfo> shortTermCharts, decimal gridIntervalRatio, GridType gridType)
	{
		public decimal Money = 1_000_000;
		public decimal UpperPrice { get; set; }
		public decimal LowerPrice { get; set; }
		public decimal GridInterval { get; set; }
		/// <summary>
		/// 수량 공비
		/// </summary>
		public decimal GridIntervalRatio { get; set; } = gridIntervalRatio;
		public GridType GridType { get; set; } = gridType;
		public decimal UpperStopLossPrice { get; set; }
		public decimal LowerStopLossPrice { get; set; }
		public decimal FeeRate = 0.0002M; // 0.02%

		public string Symbol { get; set; } = symbol;
		public List<Price> Prices { get; set; } = prices;
		/// <summary>
		/// For Grid Range
		/// </summary>
		public List<ChartInfo> LongTermCharts { get; set; } = longTermCharts;
		/// <summary>
		/// For MACD Cross
		/// </summary>
		public List<ChartInfo> MidTermCharts { get; set; } = midTermCharts;
		/// <summary>
		/// For Grid Interval
		/// </summary>
		public List<ChartInfo> ShortTermCharts { get; set; } = shortTermCharts;
		public List<Order> LongOrders { get; set; } = [];
		public List<Order> ShortOrders { get; set; } = [];
		public Order NearestLongOrder = default!;
		public Order NearestShortOrder = default!;
		public decimal CoinQuantity { get; set; } = 0;
		public int LongFillCount = 0;
		public int ShortFillCount = 0;

		/// <summary>
		/// Long 포지션 표준 가격
		/// </summary>
		public decimal StandardLongPrice { get; set; }
		/// <summary>
		/// Long 포지션 표준 수량
		/// </summary>
		public decimal StandardLongQuantity { get; set; } = 1.0M;
		/// <summary>
		/// Short 포지션 표준 가격
		/// </summary>
		public decimal StandardShortPrice { get; set; }
		/// <summary>
		/// Short 포지션 표준 수량
		/// </summary>
		public decimal StandardShortQuantity { get; set; } = 1.0M;


		void MakeOrder(PositionSide side, decimal price)
		{
			if (side == PositionSide.Long)
			{
				var quantity = StandardLongQuantity * (decimal)Math.Pow((double)GridIntervalRatio, (int)((StandardLongPrice - price) / GridInterval));
				LongOrders.Add(new Order(Symbol, PositionSide.Long, price, quantity));
			}
			else
			{
				var quantity = StandardShortQuantity * (decimal)Math.Pow((double)GridIntervalRatio, (int)((price - StandardShortPrice) / GridInterval));
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

		public void Run(Action<int> reportProgress, Action<int, int> reportProgressCount, string reportFileName, int startIndex)
		{
			/* Init */
			SetGrid(startIndex);
			SetStandardPrice(startIndex);
			SetOrder(startIndex);

			decimal maxRisk = 0;
			DateTime displayDate = Prices[startIndex].Date;
			for (int i = startIndex; i < Prices.Count; i++)
			{
				reportProgress((int)(50 + (double)i / Prices.Count * 50));
				reportProgressCount(i, Prices.Count);

				var price = Prices[i];

				if (SetGridType(i))
				{ // Grid type changed
					CloseAllPositions(i);
					SetGrid(i);
					SetStandardPrice(i);
					SetOrder(i);
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
					File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}.csv"),
					$"{Prices[i].Date:yyyy-MM-dd HH:mm:ss},{CoinQuantity.Round(2)},{GridType},{LongFillCount},{ShortFillCount},{risk.Round(2)}%,{_estimatedMoney.Round(2)}" + Environment.NewLine);
				}
			}

			var estimatedMoney = Money + CoinQuantity * Prices[^1].Value;
			var period = (Prices[^1].Date - Prices[0].Date).Days;
			var tradePerDay = (double)(LongFillCount + ShortFillCount) / period;
			File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}.csv"),
					$"{Symbol},{period}Days,{tradePerDay.Round(1)}/d,{maxRisk.Round(2)}%,{estimatedMoney.Round(2)}" + Environment.NewLine + Environment.NewLine);
		}

		public bool SetGridType(int chartIndex)
		{
			var price = Prices[chartIndex];
			var midTermOrderByDescendingCharts = MidTermCharts.Where(d => d.DateTime <= price.Date).OrderByDescending(d => d.DateTime);
			var midTermCharts1 = midTermOrderByDescendingCharts.ElementAt(1);
			var midTermCharts2 = midTermOrderByDescendingCharts.ElementAt(2);

			if (GridType == GridType.Long)
			{
				if (chartIndex > 0 && midTermCharts2.Macd > midTermCharts2.MacdSignal && midTermCharts1.Macd < midTermCharts1.MacdSignal)
				{
					GridType = GridType.Neutral;
					return true;
				}
			}
			else if (GridType == GridType.Short)
			{
				if (chartIndex > 0 && midTermCharts2.Macd < midTermCharts2.MacdSignal && midTermCharts1.Macd > midTermCharts1.MacdSignal)
				{
					GridType = GridType.Neutral;
					return true;
				}
			}
			else if (GridType == GridType.Neutral)
			{
				if (price.Value > UpperStopLossPrice)
				{
					GridType = GridType.Long;
					return true;
				}
				else if (price.Value < LowerStopLossPrice)
				{
					GridType = GridType.Short;
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
			var longTermLastAtr = (decimal)LongTermCharts.Where(d => d.DateTime <= price.Date).OrderByDescending(d => d.DateTime).ElementAt(1).Atr.Round(1);
			var shortTermLastAtr = (decimal)ShortTermCharts.Where(d => d.DateTime <= price.Date).OrderByDescending(d => d.DateTime).ElementAt(1).Atr.Round(1);

			UpperPrice = price.Value + longTermLastAtr;
			LowerPrice = price.Value - longTermLastAtr;
			UpperStopLossPrice = price.Value + longTermLastAtr * 1.1M;
			LowerStopLossPrice = price.Value - longTermLastAtr * 1.1M;
			GridInterval = shortTermLastAtr;
		}

		public void SetStandardPrice(int chartIndex)
		{
			var currentPrice = Prices[chartIndex].Value;
			var lower = LowerPrice;
			var upper = LowerPrice + GridInterval;

			while (upper <= UpperPrice)
			{
				if (currentPrice >= lower && currentPrice <= upper)
				{
					StandardLongPrice = Math.Abs(currentPrice - lower) <= Math.Abs(currentPrice - upper) ? lower : upper - GridInterval;
					StandardShortPrice = StandardLongPrice + GridInterval;
					break;
				}
				lower = upper;
				upper += GridInterval;
			}
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
				for (decimal i = LowerPrice; i <= StandardLongPrice; i += GridInterval)
				{
					MakeOrder(PositionSide.Long, i);
				}
			}
			else if (GridType == GridType.Short)
			{
				for (decimal i = StandardShortPrice; i <= UpperPrice; i += GridInterval)
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