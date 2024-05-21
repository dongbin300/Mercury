using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

using System.Diagnostics;

namespace Mercury.Backtests
{
	/// <summary>
	/// Grid Bot Flex EMA Backtester
	/// </summary>
	/// <param name="symbol"></param>
	/// <param name="prices"></param>
	/// <param name="longTermCharts"></param>
	/// <param name="gridType"></param>
	/// <param name="gridTypeChange"></param>
	/// <param name="reportFileName"></param>
	public class GridFlexEmaBacktester(string symbol, List<Price> prices, List<ChartInfo> longTermCharts, List<ChartInfo> shortTermCharts, GridType gridType, string reportFileName, int gridCount, decimal slMargin)
	{
		public decimal Seed = 1_000_000;
		public decimal Money = 1_000_000;
		public decimal Leverage = 1;
		public decimal Margin = 0;
		public decimal UpperPrice { get; set; }
		public decimal LowerPrice { get; set; }
		public decimal GridInterval { get; set; }
		public GridType GridType { get; set; } = gridType;
		public string ReportFileName { get; set; } = reportFileName;
		public decimal UpperStopLossPrice { get; set; }
		public decimal LowerStopLossPrice { get; set; }
		public decimal FeeRate = 0.0002M; // 0.02%
		public decimal GridStartPrice { get; set; }

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

		public decimal LastEma; // 이전 EMA
		public decimal LastAtr; // 이전 ATR
		public decimal MaxRisk; // 리스크 최대값

		public readonly int GRID_COUNT = gridCount;
		public readonly decimal SL_MARGIN = slMargin;

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

			var risk = 100 * (Margin * Leverage - currentPrice * Math.Abs(CoinQuantity)) / (Margin); // 지정가 주문이 청산가에 영향을 주면 Margin, 안주면 Margin + Money

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

		void InitMarketBuy(int chartIndex)
		{
			var currentPrice = Prices[chartIndex].Value;
			var shortOrdersQuantity = ShortOrders.Sum(x => x.Quantity);
			var amount = currentPrice * shortOrdersQuantity;
			Money -= amount / Leverage;
			Money -= amount * FeeRate * 2; // Market Fee = 2 * Limit Fee
			Margin += amount / Leverage;
			CoinQuantity += shortOrdersQuantity;

			// 그리드 시작 시 생성한 Long Position과 함께 처리
			//foreach (var shortOrder in ShortOrders)
			//{
			//	shortOrder.Quantity *= 2;
			//}
		}

		void InitMarketSell(int chartIndex)
		{
			var currentPrice = Prices[chartIndex].Value;
			var longOrdersQuantity = LongOrders.Sum(x => x.Quantity);
			var amount = currentPrice * longOrdersQuantity;
			Money += amount / Leverage;
			Money -= amount * FeeRate * 2;
			Margin += amount / Leverage;
			CoinQuantity -= longOrdersQuantity;

			//foreach (var longOrder in LongOrders)
			//{
			//	longOrder.Quantity *= 2;
			//}
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

				MakeOrder(PositionSide.Short, order.Price + GridInterval);

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

		public string Run(Action<int> reportProgress, Action<int, int> reportProgressCount, int startIndex)
		{
			var startTime = Prices[startIndex].Date;
			DateTime displayDate = startTime;
			DateTime gridResetDate = startTime;
			LastEma = (decimal)ShortTermCharts.Where(d => d.DateTime <= startTime).OrderByDescending(d => d.DateTime).ElementAt(1).Ema1;
			LastAtr = (decimal)LongTermCharts.Where(d => d.DateTime <= startTime).OrderByDescending(d => d.DateTime).ElementAt(1).Atr;

			SetGrid(startIndex);
			SetOrder(startIndex);

			for (int i = startIndex; i < Prices.Count; i++)
			{
				//reportProgress((int)((double)i / Prices.Count * 100));
				//reportProgressCount(i, Prices.Count);

				var time = Prices[i].Date;
				var price = Prices[i].Value;

				// 하루 이상이 지나고 EMA 크로스하면 정리 후 그리드 설정 초기화
				// 추후에 그리드 시작점오면 정리 후 그리드 설정 초기화 하는 것도 구현
				if (!(gridResetDate.Month == time.Month && gridResetDate.Day == time.Day))
				{
					switch (GridType)
					{
						case GridType.Neutral:
							if (i > 0)
							{
								if ((Prices[i - 1].Value < GridStartPrice && price > GridStartPrice) || (Prices[i - 1].Value > GridStartPrice && price < GridStartPrice))
								{
									gridResetDate = time;

									WriteStatus(i);
									CloseAllPositions(i);
									SetGrid(i);
									SetOrder(i);
									WriteStatus(i);
								}
							}
							break;

						case GridType.Long:
							if (i > 0)
							{
								if (Prices[i - 1].Value > LastEma && price < LastEma)
								{
									gridResetDate = time;

									WriteStatus(i);
									CloseAllPositions(i);
									GridType = GridType.Neutral;
									SetGrid(i);
									SetOrder(i);
									WriteStatus(i);
								}
							}
							break;

						case GridType.Short:
							if (i > 0)
							{
								if (Prices[i - 1].Value < LastEma && price > LastEma)
								{
									gridResetDate = time;

									WriteStatus(i);
									CloseAllPositions(i);
									GridType = GridType.Neutral;
									SetGrid(i);
									SetOrder(i);
									WriteStatus(i);
								}
							}
							break;
					}
				}

				//if (GridType == GridType.Long && price < LastEma)
				//{
				//	WriteStatus(i);
				//	CloseAllPositions(i);
				//	GridType = GridType.Neutral;
				//	SetGrid(i);
				//	SetOrder(i);
				//	WriteStatus(i);
				//}
				//else if (GridType == GridType.Short && price > LastEma)
				//{
				//	WriteStatus(i);
				//	CloseAllPositions(i);
				//	GridType = GridType.Neutral;
				//	SetGrid(i);
				//	SetOrder(i);
				//	WriteStatus(i);
				//}

				// 스탑로스
				if (price >= UpperStopLossPrice)
				{
					WriteStatus(i);
					CloseAllPositions(i);
					GridType = GridType.Long;
					SetGrid(i);
					SetOrder(i);
					WriteStatus(i);
				}
				else if (price <= LowerStopLossPrice)
				{
					WriteStatus(i);
					CloseAllPositions(i);
					GridType = GridType.Short;
					SetGrid(i);
					SetOrder(i);
					WriteStatus(i);
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

				if (time >= displayDate)
				{
					displayDate = displayDate.AddDays(1);

					// 하루가 지나면 1일봉 EMA, 1주봉 ATR 재계산
					LastEma = (decimal)ShortTermCharts.Where(d => d.DateTime <= time).OrderByDescending(d => d.DateTime).ElementAt(1).Ema1;
					LastAtr = (decimal)LongTermCharts.Where(d => d.DateTime <= time).OrderByDescending(d => d.DateTime).ElementAt(1).Atr;

					WriteStatus(i);
				}
			}

			var estimatedMoney = GetEstimatedAsset(Prices[^1].Value);
			var period = (Prices[^1].Date - Prices[0].Date).Days + 1;
			var tradePerDay = (double)(LongFillCount + ShortFillCount) / period;
			var resultString = $"{Symbol},{period}Days,{tradePerDay.Round(1)}/d,{MaxRisk.Round(2)}%,{estimatedMoney.Round(2)},{Margin.Round(2)}";
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
			$"{time:yyyy-MM-dd HH:mm:ss},{CoinQuantity.Round(2)},{GridType},{LongFillCount},{ShortFillCount},{risk.Round(2)}%,{_estimatedMoney.Round(2)},{Margin.Round(2)},M:{Money.Round(2)},P:{price},PNL:{GetPnl(price)}" + Environment.NewLine);
		}

		public void CloseAllPositions(int chartIndex)
		{
			Money += CoinQuantity * Prices[chartIndex].Value;
			CoinQuantity = 0;
			Margin = 0;
		}

		public void SetGrid(int chartIndex)
		{
			var currentPrice = Prices[chartIndex].Value;
			var currentTime = Prices[chartIndex].Date;

			if (GridType == GridType.Long)
			{
				UpperPrice = currentPrice + LastAtr;
				LowerPrice = currentPrice - LastAtr; // currentPrice - LastAtr or LastEma
				UpperStopLossPrice = currentPrice + LastAtr * SL_MARGIN;
				LowerStopLossPrice = currentPrice - LastAtr * SL_MARGIN;
			}
			else if (GridType == GridType.Short)
			{
				UpperPrice = currentPrice + LastAtr;
				LowerPrice = currentPrice - LastAtr;
				UpperStopLossPrice = currentPrice + LastAtr * SL_MARGIN;
				LowerStopLossPrice = currentPrice - LastAtr * SL_MARGIN;
			}
			else if (GridType == GridType.Neutral)
			{
				UpperPrice = currentPrice + LastAtr;
				LowerPrice = currentPrice - LastAtr;
				// 스탑로스 구간은 좀 더 고민해봐야 할듯
				UpperStopLossPrice = currentPrice + LastAtr * SL_MARGIN;
				LowerStopLossPrice = currentPrice - LastAtr * SL_MARGIN;
			}
			GridInterval = (UpperPrice - LowerPrice) / GRID_COUNT;
			StandardBaseOrderSize = Seed / GRID_COUNT * Leverage;
			GridStartPrice = currentPrice;

			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
			$"{currentTime:yyyy-MM-dd HH:mm:ss},{GridType},CurrentPrice:{currentPrice.Round(2)},Upper:{UpperPrice.Round(2)},Lower:{LowerPrice.Round(2)},UpperStopLoss:{UpperStopLossPrice.Round(2)},LowerStopLoss:{LowerStopLossPrice.Round(2)},GridInterval:{GridInterval.Round(2)},GridCount:{GRID_COUNT},StandardBaseOrderSize:{StandardBaseOrderSize.Round(2)}" + Environment.NewLine);
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

			if (GridType == GridType.Long)
			{
				InitMarketBuy(chartIndex);
			}
			else if (GridType == GridType.Short)
			{
				InitMarketSell(chartIndex);
			}
		}
	}
}