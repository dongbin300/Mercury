using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;
using Mercury.Maths;

using System;

namespace Mercury.Backtests
{
	public class EasyBacktester(string strategyId, List<string> symbols, KlineInterval interval, KlineInterval subInterval, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals, decimal money, int leverage)
	{
		public int Win { get; set; } = 0;
		public int Lose { get; set; } = 0;
		public decimal WinRate => Win + Lose == 0 ? 0 : (decimal)Win / (Win + Lose) * 100;
		public decimal Money = money;
		public int Leverage = leverage;
		public decimal BaseOrderSize = money * leverage / maxActiveDeals;
		public decimal EstimatedMoney => Money
			+ Positions.Where(x => x.Side.Equals(PositionSide.Long)).Sum(x => x.EntryPrice * x.Quantity)
			- Positions.Where(x => x.Side.Equals(PositionSide.Short)).Sum(x => x.EntryPrice * x.Quantity)
			- Borrowed;

		public readonly decimal FeeRate = 0.0002m;
		public decimal MarginSize = money / maxActiveDeals;
		public Dictionary<DateTime, decimal> BorrowSize = [];// money * (leverage - 1) / maxActiveDeals;
		public decimal Borrowed = 0m;

		public string StrategyId { get; set; } = strategyId;
		public List<string> Symbols { get; set; } = symbols;
		public KlineInterval Interval { get; set; } = interval;
		public KlineInterval SubInterval { get; set; } = subInterval;
		public Dictionary<string, List<ChartInfo>> Charts { get; set; } = [];
		public Dictionary<string, List<ChartInfo>> SubCharts { get; set; } = [];
		public List<Position> Positions { get; set; } = [];
		public List<PositionHistory> PositionHistories { get; set; } = [];
		public bool IsGeneratePositionHistory = false;
		public MaxActiveDealsType MaxActiveDealsType { get; set; } = maxActiveDealsType;
		public int MaxActiveDeals { get; set; } = maxActiveDeals;
		public int LongPositionCount => Positions.Count(x => x.Side.Equals(PositionSide.Long));
		public int ShortPositionCount => Positions.Count(x => x.Side.Equals(PositionSide.Short));
		public int LongFillCount = 0;
		public int LongExitCount = 0;
		public int ShortFillCount = 0;
		public int ShortExitCount = 0;

		public void InitIndicators(params decimal[] p)
		{
			foreach (var symbol in Symbols)
			{
				var chartPack = ChartLoader.GetChartPack(symbol, Interval);
				var subChartPack = new ChartPack(SubInterval);

				if (Interval != SubInterval)
				{
					subChartPack = ChartLoader.GetChartPack(symbol, SubInterval);
				}

				switch (StrategyId.ToLower())
				{
					case "macd2":
						chartPack.UseMacd();
						chartPack.UseAdx();
						chartPack.UseSupertrend(10, 1.5);
						break;

					case "macd4.1.14.2":
						//chartPack.UseMacd((int)p[0], (int)p[1], (int)p[2], (int)p[3], (int)p[4], (int)p[5]);
						//chartPack.UseAdx();
						//chartPack.UseSupertrend((int)p[6], (double)p[7]);

						chartPack.UseMacd(12, 26, 9, 9, 20, 7);
						chartPack.UseAdx();
						chartPack.UseSupertrend(10, 1.5);
						break;

					case "triple_rsi":
						chartPack.UseRsi(7, 14, 21);
						chartPack.UseEma(50);
						chartPack.UseAdx(14, 14);
						chartPack.UseSupertrend(10, 1.5);
						break;

					case "goldbb":
						chartPack.UseBollingerBands();
						chartPack.UseSma(10);
						chartPack.UseEma(10);
						break;

					case "upcandle2":
					case "upcandle3":
					case "downcandle2":
					case "downcandle3":
						break;

					case "candlesma":
						chartPack.UseSma(20, 60);
						break;

					case "custom":
						//subChartPack.UseSupertrend(10, 1.2);
						//chartPack.UseStoch();
						//chartPack.UseRsi();
						//chartPack.UseSupertrend(10, 2.0);
						break;

					default:
						break;
				}
				Charts.Add(symbol, [.. chartPack.Charts]);
				if (Interval != SubInterval)
				{
					SubCharts.Add(symbol, [.. subChartPack.Charts]);
				}
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

		private decimal GetBorrowSize(DateTime time)
		{
			return BorrowSize[new DateTime(time.Year, time.Month, time.Day)];
		}

		public (string, decimal) Run(BacktestType backtestType, Action<int> reportProgress, string reportFileName, int reportInterval, int startIndex)
		{
			//var seedPercent = 0.51m;
			//var prevEstimatedMoney = 0m;
			var currentTime = DateTime.Now;
			var maxChartCount = Charts.Max(c => c.Value.Count);
			for (int i = startIndex; i < maxChartCount; i++)
			{
				// Reset Order Size
				var time = Charts.ElementAt(0).Value[i].DateTime;
				if (time.Hour == 0 && time.Minute == 0)
				{
					//if(EstimatedMoney > prevEstimatedMoney)
					//{
					//	seedPercent -= 0.01m;
					//}
					//else
					//{
					//	seedPercent += 0.01m;
					//}
					BaseOrderSize = EstimatedMoney * 0.99m * Leverage / MaxActiveDeals;
					//prevEstimatedMoney = EstimatedMoney;
					MarginSize = BaseOrderSize / Leverage;
					BorrowSize.Add(time, MarginSize * (Leverage - 1));
				}
				//var t = new DateTime(2022, 1, 1);
				//for (int j = 0; j < 1000; j++)
				//{
				//	var time = t.AddDays(j);
				//	if (BorrowSize.ContainsKey(time))
				//	{
				//		continue;
				//	}
				//	BorrowSize.Add(time, MarginSize * (Leverage - 1));
				//}

				#region ...
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
					var c2 = charts[i - 2];
					var c3 = charts[i - 3];
					var c4 = charts[i - 4];
					var c5 = charts[i - 5];
					var c6 = charts[i - 6];
					var c7 = charts[i - 7];
					var c8 = charts[i - 8];
					currentTime = c0.DateTime;
					var c1LongBodyLength = c1.BodyLength;
					var minPrice = GetMinPrice(charts, 14, i);
					var maxPrice = GetMaxPrice(charts, 14, i);
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
									if (IsMacd2GoldenCross(charts, 14, i) &&
										c1.Supertrend1 > 0 &&
										c1LongBodyLength < 0.5m &&
										longStopLossPercent < -0.8m &&
										longTakeProfitPercent > 0.8m)
									{
										EntryPosition(PositionSide.Long, c0,
											c0.Quote.Open,
											Calculator.TargetPrice(PositionSide.Long, c0.Quote.Open, longStopLossPercent),
											Calculator.TargetPrice(PositionSide.Long, c0.Quote.Open, longTakeProfitPercent));
									}
									break;

								case "macd4.1.14.2":
									var slPrice = minPrice - (maxPrice - minPrice) * 0.1m;
									var tpPrice = maxPrice - (maxPrice - minPrice) * 0.1m;
									var slPer = Calculator.Roe(PositionSide.Long, c0.Quote.Open, slPrice);
									var tpPer = Calculator.Roe(PositionSide.Long, c0.Quote.Open, tpPrice);
									if (IsPowerGoldenCross(charts, 14, i, c1.Macd) && IsPowerGoldenCross2(charts, 14, i, c1.Macd2) && tpPer > 1.0m)
									{
										EntryPosition(PositionSide.Long, c0, c0.Quote.Open, slPrice, tpPrice);
									}
									break;

								case "triple_rsi":
									if (c1.Rsi3 > 50 && c1.Rsi1 > c1.Rsi2 && c1.Rsi2 > c1.Rsi3 && c1.Quote.Close > (decimal)c1.Ema1 && c1.Adx > 20)
									{
										EntryPosition(PositionSide.Long, c0,
											c0.Quote.Open,
											minPrice - (maxPrice - minPrice) * 0.1m,
											maxPrice - (maxPrice - minPrice) * 0.1m);
									}
									break;

								case "goldbb":
									if (c1.Quote.Close > (decimal)c1.Bb1Upper)
									{
										EntryPosition(PositionSide.Long, c0, c0.Quote.Open);
									}
									break;

								case "upcandle2":
									if (c1.CandlestickType == CandlestickType.Bullish && c2.CandlestickType == CandlestickType.Bullish)
									{
										EntryPosition(PositionSide.Long, c0, c0.Quote.Open);
									}
									break;

								case "downcandle3":
									if (c1.CandlestickType == CandlestickType.Bearish && c2.CandlestickType == CandlestickType.Bearish && c3.CandlestickType == CandlestickType.Bearish)
									{
										EntryPosition(PositionSide.Long, c0, c0.Quote.Open);
									}
									break;

								case "candlesma":
									if (!(c1.Quote.Close < (decimal)c1.Ema1 && c1.Ema1 < c1.Ema2)) // 위에서부터 60이평, 20이평, 가격 순이면 매수하지 않음
									{
										if (c1.CandlestickType == CandlestickType.Bearish && c2.CandlestickType == CandlestickType.Bearish && c3.CandlestickType == CandlestickType.Bearish)
										{
											EntryPosition(PositionSide.Long, c0, c0.Quote.Open);
										}
									}
									break;

								#endregion
								case "custom":
									if (
										c1.CandlestickType == CandlestickType.Bearish
										&& c2.CandlestickType == CandlestickType.Bearish
										&& c3.CandlestickType == CandlestickType.Bearish
										&& c1.BodyLength > 0.05m
										&& c2.BodyLength > 0.05m
										&& c3.BodyLength > 0.05m
										)
									{
										//if (c0.Quote.Low < c1.Quote.Close)
										//{
										//	LongFillCount++;
										EntryPosition(PositionSide.Long, c0, c1.Quote.Close);
										//}
									}
									break;
								#region ...
								default:
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
									TakeProfitHalf2(longPosition, c0);
								}
								break;

							case "macd4.1.14.2":
								if (longPosition.Stage == 0 && c1.Quote.Low <= longPosition.StopLossPrice)
								{
									StopLoss(longPosition, c1);
								}
								else if (longPosition.Stage == 0 && c1.Quote.High >= longPosition.TakeProfitPrice)
								{
									TakeProfitHalf(longPosition);
								}
								if (longPosition.Stage == 1 && c1.Supertrend1 < 0)
								{
									TakeProfitHalf2(longPosition, c0);
								}
								break;

							case "triple_rsi":
								if (c1.Quote.High >= longPosition.TakeProfitPrice)
								{
									TakeProfit(longPosition, c0);
								}
								else if (c1.Quote.Low <= longPosition.StopLossPrice)
								{
									StopLoss(longPosition, c0);
								}
								break;

							case "goldbb":
								if (c1.Quote.Close < (decimal)c1.Ema1)
								{
									ExitPosition(longPosition, c0, c0.Quote.Open);
								}
								break;

							case "upcandle2":
							case "downcandle3":
							case "candlesma":
								ExitPosition(longPosition, c1, c1.Quote.Close);
								break;
							#endregion
							case "custom":
								//if(c0.Quote.Low <= longPosition.StopLossPrice)
								//{
								//	StopLoss(longPosition, c0);
								//} else if
								if (
									//longPosition.Stage == 0 &&
									c1.CandlestickType == CandlestickType.Bullish
									&& c2.CandlestickType == CandlestickType.Bullish
									)
								{
									//if (c0.Quote.High > c1.Quote.Close)
									//{
									//	LongExitCount++;
									ExitPosition(longPosition, c1, c1.Quote.Close);
									//ExitPositionHalf(longPosition, c1.Quote.Close);
									//}
								}
								//else if (longPosition.Stage == 1)
								//{
								//	var subCharts = GetSubChart(symbol, time, Interval);
								//	for (int j = 0; j < subCharts.Count(); j++)
								//	{
								//		var chart = subCharts.ElementAt(j);
								//		if (chart.Supertrend1 < 0)
								//		{
								//			ExitPositionHalf2(longPosition, chart, chart.Quote.Close);
								//			break;
								//		}
								//	}
								//}
								break;
							#region ...
							default:
								break;
						}
					}

					/* SHORT POSITION */
					var shortPosition = Positions.Find(x => x.Symbol.Equals(symbol) && x.Side.Equals(PositionSide.Short));
					var c1ShortBodyLength = c1.BodyLength;
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
									if (IsMacd2DeadCross(charts, 14, i) &&
										c1.Supertrend1 < 0 &&
										c1ShortBodyLength < 0.5m &&
										shortStopLossPercent < -0.8m &&
										shortTakeProfitPercent > 0.8m)
									{
										EntryPosition(PositionSide.Short, c0,
											c0.Quote.Open,
											Calculator.TargetPrice(PositionSide.Short, c0.Quote.Open, shortStopLossPercent),
											Calculator.TargetPrice(PositionSide.Short, c0.Quote.Open, shortTakeProfitPercent));
									}
									break;

								case "macd4.1.14.2":
									var slPrice = maxPrice + (maxPrice - minPrice) * 0.1m;
									var tpPrice = minPrice + (maxPrice - minPrice) * 0.1m;
									var slPer = Calculator.Roe(PositionSide.Short, c0.Quote.Open, slPrice);
									var tpPer = Calculator.Roe(PositionSide.Short, c0.Quote.Open, tpPrice);

									if (IsPowerDeadCross(charts, 14, i, c1.Macd) && IsPowerDeadCross2(charts, 14, i, c1.Macd2) && tpPer > 1.0m)
									{
										EntryPosition(PositionSide.Short, c0, c0.Quote.Open, slPrice, tpPrice);
									}
									break;

								case "triple_rsi":
									if (c1.Rsi3 < 50 && c1.Rsi1 < c1.Rsi2 && c1.Rsi2 < c1.Rsi3 && c1.Quote.Close < (decimal)c1.Ema1 && c1.Adx > 20)
									{
										EntryPosition(PositionSide.Short, c0,
											c0.Quote.Open,
											maxPrice + (maxPrice - minPrice) * 0.1m,
											minPrice + (maxPrice - minPrice) * 0.1m);
									}
									break;

								case "goldbb":
									if (c1.Quote.Close < (decimal)c1.Bb1Lower)
									{
										EntryPosition(PositionSide.Short, c0, c0.Quote.Open);
									}
									break;

								case "upcandle3":
									if (c1.CandlestickType == CandlestickType.Bullish && c2.CandlestickType == CandlestickType.Bullish && c3.CandlestickType == CandlestickType.Bullish)
									{
										EntryPosition(PositionSide.Short, c0, c0.Quote.Open);
									}
									break;

								case "downcandle2":
									if (c1.CandlestickType == CandlestickType.Bearish && c2.CandlestickType == CandlestickType.Bearish)
									{
										EntryPosition(PositionSide.Short, c0, c0.Quote.Open);
									}
									break;

								case "candlesma":
									if (!(c1.Quote.Close > (decimal)c1.Ema1 && c1.Ema1 > c1.Ema2)) // 위에서부터 가격, 20이평, 60이평 순이면 매도하지 않음
									{
										if (c1.CandlestickType == CandlestickType.Bullish && c2.CandlestickType == CandlestickType.Bullish && c3.CandlestickType == CandlestickType.Bullish)
										{
											EntryPosition(PositionSide.Short, c0, c0.Quote.Open);
										}
									}
									break;
								#endregion
								case "custom":
									if (
										c1.CandlestickType == CandlestickType.Bullish
										&& c2.CandlestickType == CandlestickType.Bullish
										&& c3.CandlestickType == CandlestickType.Bullish
										&& c1.BodyLength > 0.05m
										&& c2.BodyLength > 0.05m
										&& c3.BodyLength > 0.05m
										)
									{
										//if (c0.Quote.High > c1.Quote.Close)
										//{
										//	ShortFillCount++;
										EntryPosition(PositionSide.Short, c0, c1.Quote.Close);
										//}
									}
									break;
								#region ...
								default:
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
									TakeProfitHalf2(shortPosition, c0);
								}
								break;

							case "macd4.1.14.2":
								if (shortPosition.Stage == 0 && c1.Quote.High >= shortPosition.StopLossPrice)
								{
									StopLoss(shortPosition, c1);
								}
								else if (shortPosition.Stage == 0 && c1.Quote.Low <= shortPosition.TakeProfitPrice)
								{
									TakeProfitHalf(shortPosition);
								}
								if (shortPosition.Stage == 1 && c1.Supertrend1 > 0)
								{
									TakeProfitHalf2(shortPosition, c0);
								}
								break;

							case "triple_rsi":
								if (c1.Quote.Low <= shortPosition.TakeProfitPrice)
								{
									TakeProfit(shortPosition, c0);
								}

								if (c1.Quote.High >= shortPosition.StopLossPrice)
								{
									StopLoss(shortPosition, c0);
								}
								break;

							case "goldbb":
								if (c1.Quote.Close > (decimal)c1.Ema1)
								{
									ExitPosition(shortPosition, c0, c0.Quote.Open);
								}
								break;

							case "upcandle3":
							case "downcandle2":
							case "candlesma":
								ExitPosition(shortPosition, c1, c1.Quote.Close);
								break;
							#endregion
							case "custom":
								//if (c0.Quote.High >= shortPosition.StopLossPrice)
								//{
								//	StopLoss(shortPosition, c0);
								//} else if
								if (
									//shortPosition.Stage == 0 &&
									c1.CandlestickType == CandlestickType.Bearish
									&& c2.CandlestickType == CandlestickType.Bearish
									)
								{
									//if (c0.Quote.Low < c1.Quote.Close)
									//{
									//	ShortExitCount++;
									ExitPosition(shortPosition, c1, c1.Quote.Close);
									//ExitPositionHalf(shortPosition, c1.Quote.Close);
									//}
								}
								//else if (shortPosition.Stage == 1)
								//{
								//	var subCharts = GetSubChart(symbol, time, Interval);
								//	for (int j = 0; j < subCharts.Count(); j++)
								//	{
								//		var chart = subCharts.ElementAt(j);
								//		if (chart.Supertrend1 > 0)
								//		{
								//			ExitPositionHalf2(shortPosition, chart, chart.Quote.Close);
								//			break;
								//		}
								//	}
								//}
								break;

							default:
								break;
						}
					}
				}

				if (backtestType == BacktestType.All && i % reportInterval == 0)
				{
					if (EstimatedMoney < 0) // LIQUIDATION
					{
						File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}.csv"), $"LIQ" + Environment.NewLine + Environment.NewLine);

						if (IsGeneratePositionHistory)
						{
							foreach (var h in PositionHistories)
							{
								File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_positionhistory.csv"),
									$"{h.EntryTime:yyyy-MM-dd HH:mm:ss},{h.Symbol},{h.Side},{h.Time:yyyy-MM-dd HH:mm:ss},{h.Result},{Math.Round(h.Income, 4)},{Math.Round(h.Fee, 4)}" + Environment.NewLine
									);
							}
							File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_positionhistory.csv"), Environment.NewLine + Environment.NewLine);
						}

						return (string.Empty, 0m);
					}

					File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}.csv"), $"{currentTime:yyyy-MM-dd HH:mm:ss},{Win},{Lose},{WinRate.Round(2)},{LongPositionCount},{ShortPositionCount},{EstimatedMoney.Round(2)}" + Environment.NewLine);
					//,{LongFillCount},{LongExitCount},{ShortFillCount},{ShortExitCount}
				}
			}

			File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}.csv"),
				backtestType == BacktestType.All ? Environment.NewLine + Environment.NewLine :
				$"{Symbols[0]},{Win},{Lose},{WinRate.Round(2)},{EstimatedMoney.Round(2)}" + Environment.NewLine);

			if (IsGeneratePositionHistory && backtestType == BacktestType.All)
			{
				foreach (var h in PositionHistories)
				{
					File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_positionhistory.csv"),
						$"{h.EntryTime:yyyy-MM-dd HH:mm:ss},{h.Symbol},{h.Side},{h.Time:yyyy-MM-dd HH:mm:ss},{h.Result},{Math.Round(h.Income, 4)},{Math.Round(h.Fee, 4)}" + Environment.NewLine
						);
				}
				File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_positionhistory.csv"), Environment.NewLine + Environment.NewLine);
			}

			if (backtestType == BacktestType.All)
			{
				return (string.Empty, 0m);
			}
			else
			{
				return (Symbols[0], EstimatedMoney.Round(2));
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

		bool IsPowerGoldenCross(List<ChartInfo> charts, int lookback, int index, double? currentMacd = null)
		{
			// Starts at charts[index - 1]
			for (int i = 0; i < lookback; i++)
			{
				var c0 = charts[index - 1 - i];
				var c1 = charts[index - 2 - i];

				if (currentMacd == null)
				{
					if (c0.Macd < 0 && c0.Macd > c0.MacdSignal && c1.Macd < c1.MacdSignal && c0.Adx > 30 && c0.Supertrend1 > 0)
					{
						return true;
					}
				}
				else
				{
					if (c0.Macd < 0 && c0.Macd > c0.MacdSignal && c1.Macd < c1.MacdSignal && c0.Adx > 30 && c0.Supertrend1 > 0 && c0.Macd < currentMacd)
					{
						return true;
					}
				}
			}
			return false;
		}

		bool IsPowerGoldenCross2(List<ChartInfo> charts, int lookback, int index, double? currentMacd = null)
		{
			// Starts at charts[index - 1]
			for (int i = 0; i < lookback; i++)
			{
				var c0 = charts[index - 1 - i];
				var c1 = charts[index - 2 - i];

				if (currentMacd == null)
				{
					if (c0.Macd2 < 0 && c0.Macd2 > c0.MacdSignal2 && c1.Macd2 < c1.MacdSignal2 && c0.Adx > 30 && c0.Supertrend1 > 0)
					{
						return true;
					}
				}
				else
				{
					if (c0.Macd2 < 0 && c0.Macd2 > c0.MacdSignal2 && c1.Macd2 < c1.MacdSignal && c0.Adx > 30 && c0.Supertrend1 > 0 && c0.Macd2 < currentMacd)
					{
						return true;
					}
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

		bool IsPowerDeadCross(List<ChartInfo> charts, int lookback, int index, double? currentMacd = null)
		{
			// Starts at charts[index - 1]
			for (int i = 0; i < lookback; i++)
			{
				var c0 = charts[index - 1 - i];
				var c1 = charts[index - 2 - i];

				if (currentMacd == null)
				{
					if (c0.Macd > 0 && c0.Macd < c0.MacdSignal && c1.Macd > c1.MacdSignal && c0.Adx > 30 && c0.Supertrend1 < 0)
					{
						return true;
					}
				}
				else
				{
					if (c0.Macd > 0 && c0.Macd < c0.MacdSignal && c1.Macd > c1.MacdSignal && c0.Adx > 30 && c0.Supertrend1 < 0 && c0.Macd > currentMacd)
					{
						return true;
					}
				}
			}
			return false;
		}

		bool IsPowerDeadCross2(List<ChartInfo> charts, int lookback, int index, double? currentMacd = null)
		{
			// Starts at charts[index - 1]
			for (int i = 0; i < lookback; i++)
			{
				var c0 = charts[index - 1 - i];
				var c1 = charts[index - 2 - i];

				if (currentMacd == null)
				{
					if (c0.Macd2 > 0 && c0.Macd2 < c0.MacdSignal2 && c1.Macd2 > c1.MacdSignal2 && c0.Adx > 30 && c0.Supertrend1 < 0)
					{
						return true;
					}
				}
				else
				{
					if (c0.Macd2 > 0 && c0.Macd2 < c0.MacdSignal2 && c1.Macd2 > c1.MacdSignal2 && c0.Adx > 30 && c0.Supertrend1 < 0 && c0.Macd > currentMacd)
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// 포지션 진입
		/// </summary>
		/// <param name="side"></param>
		/// <param name="currentChart"></param>
		/// <param name="entryPrice"></param>
		/// <param name="stopLossPrice"></param>
		/// <param name="takeProfitPrice"></param>
		void EntryPosition(PositionSide side, ChartInfo currentChart, decimal entryPrice, decimal? stopLossPrice = null, decimal? takeProfitPrice = null)
		{
			var quantity = BaseOrderSize / entryPrice;
			var amount = entryPrice * quantity;

			Money += GetBorrowSize(currentChart.DateTime);
			Borrowed += GetBorrowSize(currentChart.DateTime);
			Money += side == PositionSide.Long ? -amount : amount;

			var newPosition = new Position(currentChart.DateTime, currentChart.Symbol, side, entryPrice)
			{
				Quantity = quantity,
				EntryAmount = entryPrice * quantity
			};

			if (takeProfitPrice != null)
			{
				newPosition.TakeProfitPrice = takeProfitPrice.Value;
			}

			if (stopLossPrice != null)
			{
				newPosition.StopLossPrice = stopLossPrice.Value;
			}

			Positions.Add(newPosition);
		}

		/// <summary>
		/// 전량 손절
		/// </summary>
		/// <param name="position"></param>
		/// <param name="currentChart"></param>
		void StopLoss(Position position, ChartInfo currentChart)
		{
			var price = position.StopLossPrice;
			var quantity = position.Quantity;
			var amount = price * quantity;

			Money += position.Side == PositionSide.Long ? amount : -amount;
			Money -= GetBorrowSize(position.Time);
			Borrowed -= GetBorrowSize(position.Time);

			position.ExitAmount = price * quantity;
			Positions.Remove(position);
			PositionHistories.Add(new PositionHistory(currentChart.DateTime, position.Time, position.Symbol, position.Side, PositionResult.Lose)
			{
				EntryAmount = position.EntryAmount,
				ExitAmount = position.ExitAmount,
				Fee = (position.EntryAmount + position.ExitAmount) * FeeRate
			});
			Lose++;
			Money -= (position.EntryAmount + position.ExitAmount) * FeeRate;
		}

		/// <summary>
		/// 반 익절
		/// </summary>
		/// <param name="position"></param>
		void TakeProfitHalf(Position position)
		{
			var price = position.TakeProfitPrice;
			var quantity = position.Quantity / 2;

			Money += position.Side == PositionSide.Long ? price * quantity : -price * quantity;
			position.Quantity -= quantity;
			position.ExitAmount = price * quantity;
			position.Stage = 1;
		}

		/// <summary>
		/// 나머지 반 익절
		/// </summary>
		/// <param name="position"></param>
		/// <param name="currentChart"></param>
		void TakeProfitHalf2(Position position, ChartInfo currentChart)
		{
			var price = currentChart.Quote.Close;
			var quantity = position.Quantity;
			var amount = price * quantity;

			Money += position.Side == PositionSide.Long ? amount : -amount;
			Money -= GetBorrowSize(position.Time);
			Borrowed -= GetBorrowSize(position.Time);

			position.ExitAmount += amount;
			Positions.Remove(position);
			PositionHistories.Add(new PositionHistory(currentChart.DateTime, position.Time, position.Symbol, position.Side, PositionResult.Win)
			{
				EntryAmount = position.EntryAmount,
				ExitAmount = position.ExitAmount,
				Fee = (position.EntryAmount + position.ExitAmount) * FeeRate
			});
			Win++;
			Money -= (position.EntryAmount + position.ExitAmount) * FeeRate;
		}

		/// <summary>
		/// 전량 익절
		/// </summary>
		/// <param name="position"></param>
		/// <param name="currentChart"></param>
		void TakeProfit(Position position, ChartInfo currentChart)
		{
			var price = position.TakeProfitPrice;
			var quantity = position.Quantity;
			var amount = price * quantity;

			Money += position.Side == PositionSide.Long ? amount : -amount;
			Money -= GetBorrowSize(position.Time);
			Borrowed -= GetBorrowSize(position.Time);

			position.ExitAmount = amount;
			Positions.Remove(position);
			PositionHistories.Add(new PositionHistory(currentChart.DateTime, position.Time, position.Symbol, position.Side, PositionResult.Win)
			{
				EntryAmount = position.EntryAmount,
				ExitAmount = position.ExitAmount,
				Fee = (position.EntryAmount + position.ExitAmount) * FeeRate
			});
			Win++;
			Money -= (position.EntryAmount + position.ExitAmount) * FeeRate;
		}

		/// <summary>
		/// 포지션 탈출
		/// </summary>
		/// <param name="position"></param>
		/// <param name="currentChart"></param>
		/// <param name="exitPrice"></param>
		void ExitPosition(Position position, ChartInfo currentChart, decimal exitPrice)
		{
			var quantity = position.Quantity;
			var amount = exitPrice * quantity;

			Money += position.Side == PositionSide.Long ? amount : -amount;
			Money -= GetBorrowSize(position.Time);
			Borrowed -= GetBorrowSize(position.Time);

			position.ExitAmount = amount;
			var result = position.Income > 0 ? PositionResult.Win : PositionResult.Lose;
			Positions.Remove(position);
			PositionHistories.Add(new PositionHistory(currentChart.DateTime, position.Time, position.Symbol, position.Side, result)
			{
				EntryAmount = position.EntryAmount,
				ExitAmount = position.ExitAmount,
				Fee = (position.EntryAmount + position.ExitAmount) * FeeRate
			});
			switch (result)
			{
				case PositionResult.Win: Win++; break;
				case PositionResult.Lose: Lose++; break;
			}
			Money -= (position.EntryAmount + position.ExitAmount) * FeeRate;
		}

		/// <summary>
		/// 포지션 반 탈출
		/// </summary>
		/// <param name="position"></param>
		void ExitPositionHalf(Position position, decimal exitPrice)
		{
			var quantity = position.Quantity / 2;

			Money += position.Side == PositionSide.Long ? exitPrice * quantity : -exitPrice * quantity;
			position.Quantity -= quantity;
			position.ExitAmount = exitPrice * quantity;
			position.Stage = 1;
		}

		/// <summary>
		/// 나머지 포지션 반 탈출
		/// </summary>
		/// <param name="position"></param>
		/// <param name="currentChart"></param>
		void ExitPositionHalf2(Position position, ChartInfo currentChart, decimal exitPrice)
		{
			var quantity = position.Quantity;
			var amount = exitPrice * quantity;

			Money += position.Side == PositionSide.Long ? amount : -amount;
			Money -= GetBorrowSize(position.Time);
			Borrowed -= GetBorrowSize(position.Time);

			position.ExitAmount += amount;
			var result = position.Income > 0 ? PositionResult.Win : PositionResult.Lose;
			Positions.Remove(position);
			PositionHistories.Add(new PositionHistory(currentChart.DateTime, position.Time, position.Symbol, position.Side, result)
			{
				EntryAmount = position.EntryAmount,
				ExitAmount = position.ExitAmount,
				Fee = (position.EntryAmount + position.ExitAmount) * FeeRate
			});
			switch (result)
			{
				case PositionResult.Win: Win++; break;
				case PositionResult.Lose: Lose++; break;
			}
			Money -= (position.EntryAmount + position.ExitAmount) * FeeRate;
		}

		decimal GetMinPrice(List<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Min(x => x.Quote.Low);
		decimal GetMaxPrice(List<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Max(x => x.Quote.High);
		decimal GetMinClosePrice(List<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Min(x => x.Quote.Close);
		decimal GetMaxClosePrice(List<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Max(x => x.Quote.Close);

		IEnumerable<ChartInfo> GetSubChart(string symbol, DateTime startTime, KlineInterval mainChartInterval)
		{
			if (Interval == SubInterval)
			{
				throw new Exception("No subchart due to interval equal subinterval");
			}

			var endTime = startTime + mainChartInterval.ToTimeSpan() - TimeSpan.FromSeconds(1);
			return SubCharts[symbol].Where(d => d.DateTime >= startTime && d.DateTime <= endTime);
		}
	}
}
