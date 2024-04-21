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
		public decimal BaseOrderSize = 10000; //1000;
		public decimal FeeSize = 0; // 1.0m;
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

					case "macd4.1.14.2":
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

					default:
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
					var c2 = charts[i - 2];
					var c3 = charts[i - 3];
					currentTime = c0.DateTime;
					var c1LongBodyLength = c1.BodyLength(PositionSide.Long);
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
								ExitPosition(longPosition, c1, c1.Quote.Close);
								break;

							case "downcandle3":
								ExitPosition(longPosition, c1, c1.Quote.Close);
								break;

							case "candlesma":
								ExitPosition(longPosition, c1, c1.Quote.Close);
								break;

							default:
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

									if(IsPowerDeadCross(charts, 14, i, c1.Macd) && IsPowerDeadCross2(charts, 14, i, c1.Macd2) && tpPer > 1.0m)
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
								ExitPosition(shortPosition, c1, c1.Quote.Close);
								break;

							case "downcandle2":
								ExitPosition(shortPosition, c1, c1.Quote.Close);
								break;

							case "candlesma":
								ExitPosition(shortPosition, c1, c1.Quote.Close);
								break;

							default:
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
					File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_positionhistory.csv"),
						$"{h.EntryTime:yyyy-MM-dd HH:mm:ss},{h.Symbol},{h.Side},{h.Time:yyyy-MM-dd HH:mm:ss},{h.Result},{Math.Round(h.Income, 4)}" + Environment.NewLine
						);
				}
				File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}_positionhistory.csv"), Environment.NewLine + Environment.NewLine);
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
			Money += side == PositionSide.Long ? -entryPrice * quantity : entryPrice * quantity;
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
			Money += position.Side == PositionSide.Long ? price * quantity : -price * quantity;
			position.ExitAmount = price * quantity;
			Positions.Remove(position);
			PositionHistories.Add(new PositionHistory(currentChart.DateTime, position.Time, position.Symbol, position.Side, PositionResult.Lose)
			{
				EntryAmount = position.EntryAmount,
				ExitAmount = position.ExitAmount
			});
			Lose++;
			Money -= FeeSize;
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

		/// <summary>
		/// 전량 익절
		/// </summary>
		/// <param name="position"></param>
		/// <param name="currentChart"></param>
		void TakeProfit(Position position, ChartInfo currentChart)
		{
			var price = position.TakeProfitPrice;
			var quantity = position.Quantity;
			Money += position.Side == PositionSide.Long ? price * quantity : -price * quantity;
			position.ExitAmount = price * quantity;
			Positions.Remove(position);
			PositionHistories.Add(new PositionHistory(currentChart.DateTime, position.Time, position.Symbol, position.Side, PositionResult.Win)
			{
				EntryAmount = position.EntryAmount,
				ExitAmount = position.ExitAmount
			});
			Win++;
			Money -= FeeSize;
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
			Money += position.Side == PositionSide.Long ? exitPrice * quantity : -exitPrice * quantity;
			position.ExitAmount = exitPrice * quantity;
			var result = position.Income > 0 ? PositionResult.Win : PositionResult.Lose;
			Positions.Remove(position);
			PositionHistories.Add(new PositionHistory(currentChart.DateTime, position.Time, position.Symbol, position.Side, result)
			{
				EntryAmount = position.EntryAmount,
				ExitAmount = position.ExitAmount
			});
			switch (result)
			{
				case PositionResult.Win: Win++; break;
				case PositionResult.Lose: Lose++; break;
			}
			Money -= FeeSize;
		}

		decimal GetMinPrice(List<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Min(x => x.Quote.Low);
		decimal GetMaxPrice(List<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Max(x => x.Quote.High);
		decimal GetMinClosePrice(List<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Min(x => x.Quote.Close);
		decimal GetMaxClosePrice(List<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Max(x => x.Quote.Close);
	}
}
