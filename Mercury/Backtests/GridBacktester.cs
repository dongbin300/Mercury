using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests
{
	/// <summary>
	/// Grid Bot Backtester
	/// </summary>
	/// <param name="charts"></param>
	/// <param name="longTermCharts"></param>
	/// <param name="midTermCharts"></param>
	/// <param name="shortTermCharts"></param>
	/// <param name="interval"></param>
	/// <param name="gridIntervalRatio"></param>
	/// <param name="gridType"></param>
	public class GridBacktester(List<ChartInfo> charts, List<ChartInfo> longTermCharts, List<ChartInfo> midTermCharts, List<ChartInfo> shortTermCharts, KlineInterval interval, decimal gridIntervalRatio, GridType gridType)
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

		//public decimal BaseQuantity = 1;
		public decimal FeeRate = 0.0002M; // 0.02%

		public string Symbol => Charts[0].Symbol;
		public KlineInterval Interval { get; set; } = interval;
		public List<ChartInfo> Charts { get; set; } = charts;
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
		}

		public void Run(Action<int> reportProgress, string reportFileName, int reportInterval, int startIndex)
		{
			/* Init */
			SetGrid(startIndex);
			SetStandardPrice(startIndex);
			SetOrder(startIndex);

			decimal maxRisk = 0;
			for (int i = startIndex; i < Charts.Count; i++)
			{
				reportProgress((int)(50 + (double)i / Charts.Count * 50));

				if (SetGridType(i))
				{ // Grid type changed
					CloseAllPositions(i);
					SetGrid(i);
					SetStandardPrice(i);
					SetOrder(i);
				}

				for (int j = 0; j < LongOrders.Count; j++)
				{
					if (LongOrders[j].Price > Charts[i].Quote.Low)
					{
						Fill(LongOrders[j]);
						j--;
					}
				}
				for (int j = 0; j < ShortOrders.Count; j++)
				{
					if (ShortOrders[j].Price < Charts[i].Quote.High)
					{
						Fill(ShortOrders[j]);
						j--;
					}
				}

				TrailingOrder(i);

				if (i % reportInterval == 0)
				{
					var _estimatedMoney = Money + CoinQuantity * Charts[i].Quote.Close;
					var risk = Math.Abs(_estimatedMoney - Money) / _estimatedMoney * 100;
					if (risk > maxRisk)
					{
						maxRisk = risk;
					}
					File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}.csv"),
					$"{Charts[i].DateTime:yyyy-MM-dd HH:mm:ss},{CoinQuantity.Round(2)},{GridType},{LongFillCount},{ShortFillCount},{risk.Round(2)}%,{_estimatedMoney.Round(2)}" + Environment.NewLine);
				}
			}

			var estimatedMoney = Money + CoinQuantity * Charts[^1].Quote.Close;
			var period = (Charts[^1].DateTime - Charts[0].DateTime).Days;
			var tradePerDay = (double)(LongFillCount + ShortFillCount) / period;
			File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}.csv"),
					$"{Symbol},{period}Days,{tradePerDay.Round(1)}/d,{maxRisk.Round(2)}%,{estimatedMoney.Round(2)}" + Environment.NewLine + Environment.NewLine);
		}

		public bool SetGridType(int chartIndex)
		{
			var chart = Charts[chartIndex];
			var midTermOrderByDescendingCharts = MidTermCharts.Where(d => d.DateTime <= chart.DateTime).OrderByDescending(d => d.DateTime);
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
				if (chart.Quote.High > UpperStopLossPrice)
				{
					GridType = GridType.Long;
					return true;
				}
				else if (chart.Quote.Low < LowerStopLossPrice)
				{
					GridType = GridType.Short;
					return true;
				}
			}
			return false;
		}

		public void CloseAllPositions(int chartIndex)
		{
			Money += CoinQuantity * Charts[chartIndex].Quote.Open;
			CoinQuantity = 0;
		}

		public void SetGrid(int chartIndex)
		{
			var chart = Charts[chartIndex];
			var longTermLastAtr = (decimal)LongTermCharts.Where(d => d.DateTime <= chart.DateTime).OrderByDescending(d => d.DateTime).ElementAt(1).Atr.Round(1);
			var shortTermLastAtr = (decimal)ShortTermCharts.Where(d => d.DateTime <= chart.DateTime).OrderByDescending(d => d.DateTime).ElementAt(1).Atr.Round(1);

			UpperPrice = chart.Quote.Open + longTermLastAtr;
			LowerPrice = chart.Quote.Open - longTermLastAtr;
			UpperStopLossPrice = chart.Quote.Open + longTermLastAtr * 1.1M;
			LowerStopLossPrice = chart.Quote.Open - longTermLastAtr * 1.1M;
			GridInterval = shortTermLastAtr;
		}

		public void SetStandardPrice(int chartIndex)
		{
			var currentPrice = Charts[chartIndex].Quote.Open;
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
			var currentPrice = Charts[chartIndex].Quote.Open;
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

		public void TrailingOrder(int chartIndex)
		{
			if (GridType == GridType.Neutral)
			{
				return;
			}

			if (GridType == GridType.Long)
			{
				var currentHigh = Charts[chartIndex].Quote.High;

				// 가장 가까운(가격이 높은) 롱 주문
				var nearestOrder = LongOrders.Find(x => x.Price.Equals(LongOrders.Max(x => x.Price))) ?? default!;

				if (nearestOrder == null)
				{
					return;
				}

				// 가장 가까운 롱 주문으로부터 2인터벌 이상 멀어지면 롱 주문 추가
				if (currentHigh > nearestOrder.Price + 2 * GridInterval)
				{
					MakeOrder(PositionSide.Long, nearestOrder.Price + GridInterval);
				}
			}
			else if (GridType == GridType.Short)
			{
				var currentLow = Charts[chartIndex].Quote.Low;

				// 가장 가까운(가격이 낮은) 숏 주문
				var nearestOrder = ShortOrders.Find(x => x.Price.Equals(ShortOrders.Min(x => x.Price))) ?? default!;

				if (nearestOrder == null)
				{
					return;
				}

				// 가장 가까운 숏 주문으로부터 2인터벌 이상 멀어지면 숏 주문 추가
				if (currentLow < nearestOrder.Price - 2 * GridInterval)
				{
					MakeOrder(PositionSide.Short, nearestOrder.Price - GridInterval);
				}
			}
		}
	}
}