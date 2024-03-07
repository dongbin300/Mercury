using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;
using Mercury.Maths;

namespace Mercury.Backtests
{
	public class EasyBacktester(string strategyId, List<string> symbols, KlineInterval interval)
	{
		public int Win { get; set; } = 0;
		public int Lose { get; set; } = 0;
		public decimal WinRate => Win + Lose == 0 ? 0 : (decimal)Win / (Win + Lose) * 100;
		public decimal Money = 10000;
		public decimal BaseOrderSize = 1000;
		public decimal FeeSize = 1.0m;
		public decimal EstimatedMoney => Money
			+ Positions.Where(x => x.Side.Equals(PositionSide.Long)).Sum(x => x.EntryPrice * x.Quantity)
			- Positions.Where(x => x.Side.Equals(PositionSide.Short)).Sum(x => x.EntryPrice * x.Quantity);

		public string StrategyId { get; set; } = strategyId;
		public List<string> Symbols { get; set; } = symbols;
		public KlineInterval Interval { get; set; } = interval;
		public Dictionary<string, List<ChartInfo>> Charts { get; set; } = [];
		public List<Position> Positions { get; set; } = [];
		public List<PositionHistory> PositionHistories { get; set; } = [];
		public MaxActiveDealsType MaxActiveDealsType { get; set; }
		public int MaxActiveDeals { get; set; }
		public int LongPositionCount => Positions.Count(x => x.Side.Equals(PositionSide.Long));
		public int ShortPositionCount => Positions.Count(x => x.Side.Equals(PositionSide.Short));

		public void SetMaxActiveDeals(MaxActiveDealsType maxActiveDealsType, int maxActiveDeals)
		{
			MaxActiveDealsType = maxActiveDealsType;
			MaxActiveDeals = maxActiveDeals;
		}

		public void InitIndicators()
		{
			foreach (var symbol in Symbols)
			{
				var chartPack = ChartLoader.GetChartPack(symbol, Interval);
				switch (StrategyId.ToLower())
				{
					case "macd2":
						chartPack.UseMacd();
						chartPack.UseAdx();
						chartPack.UseSupertrend(10, 1.5);
						break;
				}
				Charts.Add(symbol, [.. chartPack.Charts]);
			}
		}

		public void RunSymbol(Action<int> reportProgress, string reportFileName, string symbol, int startIndex)
		{
			var maxChartCount = Charts.Max(c => c.Value.Count);
			for (int i = startIndex; i < maxChartCount; i++)
			{
				reportProgress((int)(50 + (double)i / maxChartCount * 50));

			}
		}

		public void Run(BacktestType backtestType, Action<int> reportProgress, string reportFileName, int reportInterval, int startIndex)
		{
			var currentTime = DateTime.Now;
			var maxChartCount = Charts.Max(c => c.Value.Count);
			for (int i = startIndex; i < maxChartCount; i++)
			{
				if (backtestType == BacktestType.All)
				{
					reportProgress((int)(50 + (double)i / maxChartCount * 50));
				}
				foreach (var symbol in Symbols)
				{
					/* LONG POSITION */
					var longPosition = Positions.Find(x => x.Symbol.Equals(symbol) && x.Side.Equals(PositionSide.Long));
					var charts = Charts[symbol];

					if (charts.Count <= i)
					{
						continue;
					}

					var c0 = charts[i];
					var c1 = charts[i - 1];
					currentTime = c0.DateTime;
					var c1LongBodyLength = c1.BodyLength(PositionSide.Long);
					var minPrice = charts.Skip(i - 14).Take(14).Min(x => x.Quote.Low);
					var maxPrice = charts.Skip(i - 14).Take(14).Max(x => x.Quote.High);
					var longStopLossPercent = Calculator.Roe(PositionSide.Long, c0.Quote.Open, minPrice) * 1.1m;
					var longTakeProfitPercent = Calculator.Roe(PositionSide.Long, c0.Quote.Open, maxPrice) * 0.9m;

					if (longPosition == null)
					{
						if (backtestType == BacktestType.All && MaxActiveDealsType == MaxActiveDealsType.Each && LongPositionCount >= MaxActiveDeals)
						{

						}
						else if (backtestType == BacktestType.All && MaxActiveDealsType == MaxActiveDealsType.Total && LongPositionCount + ShortPositionCount >= MaxActiveDeals)
						{

						}
						else
						{
							/* LONG POSITION - ENTRY */
							switch (StrategyId.ToLower())
							{
								case "macd2":
									if (IsMacd2GoldenCross(charts, 5, i) &&
										c1.Supertrend1 > 0 &&
										c1LongBodyLength < 0.5m &&
										longStopLossPercent < -0.8m &&
										longTakeProfitPercent > 0.8m)
									{
										var price = c0.Quote.Open;
										var stopLossPrice = Calculator.TargetPrice(PositionSide.Long, c0.Quote.Open, longStopLossPercent);
										var takeProfitPrice = Calculator.TargetPrice(PositionSide.Long, c0.Quote.Open, longTakeProfitPercent);
										var quantity = BaseOrderSize / price;
										Money -= price * quantity;
										var newPosition = new Position(c0.DateTime, symbol, PositionSide.Long, price)
										{
											TakeProfitPrice = takeProfitPrice,
											StopLossPrice = stopLossPrice,
											Quantity = quantity,
											EntryAmount = price * quantity
										};
										Positions.Add(newPosition);
									}
									break;
							}
						}
					}
					else
					{
						/* LONG POSITION - EXIT */
						switch (StrategyId.ToLower())
						{
							case "macd2":
								if (longPosition.Stage == 0 && c0.Quote.Low <= longPosition.StopLossPrice)
								{
									StopLoss(longPosition, c0);
								}
								else if (longPosition.Stage == 0 && c0.Quote.High >= longPosition.TakeProfitPrice)
								{
									TakeProfitHalf(longPosition);
								}
								if (longPosition.Stage == 1 && c0.Supertrend1 < 0)
								{
									TakeProfit(longPosition, c0);
								}
								break;
						}
					}

					/* SHORT POSITION */
					var shortPosition = Positions.Find(x => x.Symbol.Equals(symbol) && x.Side.Equals(PositionSide.Short));
					var c1ShortBodyLength = c1.BodyLength(PositionSide.Short);
					var shortStopLossPercent = Calculator.Roe(PositionSide.Short, c0.Quote.Open, maxPrice) * 1.1m;
					var shortTakeProfitPercent = Calculator.Roe(PositionSide.Short, c0.Quote.Open, minPrice) * 0.9m;

					if (shortPosition == null)
					{
						if (backtestType == BacktestType.All && MaxActiveDealsType == MaxActiveDealsType.Each && ShortPositionCount >= MaxActiveDeals)
						{

						}
						else if (backtestType == BacktestType.All && MaxActiveDealsType == MaxActiveDealsType.Total && LongPositionCount + ShortPositionCount >= MaxActiveDeals)
						{

						}
						else
						{
							/* SHORT POSITION - ENTRY */
							switch (StrategyId.ToLower())
							{
								case "macd2":
									if (IsMacd2DeadCross(charts, 5, i) &&
										c1.Supertrend1 < 0 &&
										c1ShortBodyLength < 0.5m &&
										shortStopLossPercent < -0.8m &&
										shortTakeProfitPercent > 0.8m)
									{
										var price = c0.Quote.Open;
										var stopLossPrice = Calculator.TargetPrice(PositionSide.Short, c0.Quote.Open, shortStopLossPercent);
										var takeProfitPrice = Calculator.TargetPrice(PositionSide.Short, c0.Quote.Open, shortTakeProfitPercent);
										var quantity = BaseOrderSize / price;
										Money += price * quantity;
										var newPosition = new Position(c0.DateTime, symbol, PositionSide.Short, price)
										{
											StopLossPrice = stopLossPrice,
											TakeProfitPrice = takeProfitPrice,
											Quantity = quantity,
											EntryAmount = price * quantity
										};
										Positions.Add(newPosition);
									}
									break;
							}
						}
					}
					else
					{
						/* SHORT POSITION - EXIT */
						switch (StrategyId.ToLower())
						{
							case "macd2":
								if (shortPosition.Stage == 0 && c0.Quote.High >= shortPosition.StopLossPrice)
								{
									StopLoss(shortPosition, c0);
								}
								else if (shortPosition.Stage == 0 && c0.Quote.Low <= shortPosition.TakeProfitPrice)
								{
									TakeProfitHalf(shortPosition);
								}
								if (shortPosition.Stage == 1 && c0.Supertrend1 > 0)
								{
									TakeProfit(shortPosition, c0);
								}
								break;
						}
					}
				}

				if (backtestType == BacktestType.All && i % reportInterval == 0)
				{
					File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}.csv"), $"{currentTime:yyyy-MM-dd HH:mm:ss},{Win},{Lose},{WinRate.Round(2)},{LongPositionCount},{ShortPositionCount},{EstimatedMoney.Round(2)}" + Environment.NewLine);
				}
			}

			File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}.csv"),
				backtestType == BacktestType.All ? Environment.NewLine + Environment.NewLine :
				$"{Symbols[0]},{Win},{Lose},{WinRate.Round(2)},{EstimatedMoney.Round(2)}" + Environment.NewLine);

			if (backtestType == BacktestType.All)
			{
				foreach (var h in PositionHistories)
				{
					File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"),
						$"{h.EntryTime:yyyy-MM-dd HH:mm:ss},{h.Symbol},{h.Side},{h.Time:yyyy-MM-dd HH:mm:ss},{h.Result},{Math.Round(h.Income, 4)}" + Environment.NewLine
						);
				}
				File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"), Environment.NewLine + Environment.NewLine);
			}
		}

		bool IsMacd2GoldenCross(List<ChartInfo> charts, int lookback, int index)
		{
			// Starts at charts[index - 1]
			for (int i = 0; i < lookback; i++)
			{
				var c0 = charts[index - 1 - i];
				var c1 = charts[index - 2 - i];

				if (c0.Macd < 0 && c0.Macd > c0.MacdSignal && c1.Macd < c1.MacdSignal && c0.Adx > 30)
				{
					return true;
				}
			}
			return false;
		}

		bool IsMacd2DeadCross(List<ChartInfo> charts, int lookback, int index)
		{
			// Starts at charts[index - 1]
			for (int i = 0; i < lookback; i++)
			{
				var c0 = charts[index - 1 - i];
				var c1 = charts[index - 2 - i];

				if (c0.Macd > 0 && c0.Macd < c0.MacdSignal && c1.Macd > c1.MacdSignal && c0.Adx > 30)
				{
					return true;
				}
			}
			return false;
		}

		void StopLoss(Position position, ChartInfo currentChart)
		{
			var price = position.StopLossPrice;
			var quantity = position.Quantity;
			Money += position.Side == PositionSide.Long ? price * quantity : -price * quantity;
			Positions.Remove(position);
			PositionHistories.Add(new PositionHistory(currentChart.DateTime, position.Time, position.Symbol, position.Side, PositionResult.Lose)
			{
				EntryAmount = position.EntryAmount,
				ExitAmount = price * quantity
			});
			Lose++;
			Money -= FeeSize;
		}

		void TakeProfitHalf(Position position)
		{
			var price = position.TakeProfitPrice;
			var quantity = position.Quantity / 2;
			Money += position.Side == PositionSide.Long ? price * quantity : -price * quantity;
			position.Quantity -= quantity;
			position.ExitAmount = price * quantity;
			position.Stage = 1;
		}

		void TakeProfit(Position position, ChartInfo currentChart)
		{
			var price = currentChart.Quote.Close;
			var quantity = position.Quantity;
			Money += position.Side == PositionSide.Long ? price * quantity : -price * quantity;
			Positions.Remove(position);
			PositionHistories.Add(new PositionHistory(currentChart.DateTime, position.Time, position.Symbol, position.Side, PositionResult.Win)
			{
				EntryAmount = position.EntryAmount,
				ExitAmount = position.ExitAmount + price * quantity
			});
			Win++;
			Money -= FeeSize;
		}
	}
}
