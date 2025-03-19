using Binance.Net.Enums;

using Mercury.Backtests.BacktestInterfaces;
using Mercury.Charts;
using Mercury.Data;
using Mercury.Enums;
using Mercury.Extensions;
using Mercury.Maths;

using System.Text;

namespace Mercury.Backtests
{
    public class EasyBacktester(string strategyId, List<string> symbols, KlineInterval interval, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals, decimal money, int[] leverages, decimal blacklistLossThresholdPercent = -10m, int blacklistBanHour = 24 * 7, decimal closeBodyLengthMin = 0.1m) : IUseBlacklist, IUseLeverageByDayOfTheWeek
	{
		public int Win { get; set; } = 0;
		public int Lose { get; set; } = 0;
		public decimal WinRate => Win + Lose == 0 ? 0 : (decimal)Win / (Win + Lose) * 100;
		public decimal Seed = money;
		public decimal Money = money;
		public int Leverage = leverages[0];
		public decimal BaseOrderSize = maxActiveDealsType == MaxActiveDealsType.Total ? money * leverages[0] / maxActiveDeals : money * leverages[0] / maxActiveDeals / 2;
		public decimal EstimatedMoney => Money
			+ Positions.Where(x => x.Side.Equals(PositionSide.Long)).Sum(x => x.EntryPrice * x.Quantity)
			- Positions.Where(x => x.Side.Equals(PositionSide.Short)).Sum(x => x.EntryPrice * x.Quantity)
			- Borrowed;
		public List<(DateTime, decimal)> Ests { get; set; } = [];
		public List<(DateTime, decimal)> ChangePers { get; set; } = [];
		public List<(DateTime, decimal)> MaxPers { get; set; } = [];

		public decimal FeeRate { get; set; } = 0.0002m;
		public decimal MarginSize = money / maxActiveDeals;
		public Dictionary<DateTime, decimal> BorrowSize = [];// money * (leverage - 1) / maxActiveDeals;
		public decimal Borrowed = 0m;

		public string StrategyId { get; set; } = strategyId;
		public string ExitStrategyId { get; set; } = string.Empty;
		public List<string> Symbols { get; set; } = symbols;
		public KlineInterval Interval { get; set; } = interval;
		public Dictionary<string, List<ChartInfo>> Charts { get; set; } = [];
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


		public List<BlacklistPosition> BlacklistPositions { get; set; } = [];
		public decimal BlacklistLossThresholdPercent { get; set; } = blacklistLossThresholdPercent;
		public int BlacklistBanHour { get; set; } = blacklistBanHour;


		public decimal CloseBodyLengthMin { get; set; } = closeBodyLengthMin;
		public int[] Leverages { get; set; } = leverages; //[6, 5, 5, 5, 4, 7, 4]; // 요일별 레버리지 리스트, 일요일부터 차례대로
		public decimal mMPer { get; set; } // 최대 리스크 시 고점 대비 최저%
		public Dictionary<DayOfWeek, decimal> ChangePerAveragesByDayOfTheWeek { get; set; } = []; // 요일별 자산 변동률 평균
		public decimal ProfitRoe => Ests.Count > 0 ? Ests[^1].Item2 / Seed : 0; // 시드 대비 수익률, 2.5배면 값이 2.5
		public decimal ResultPerRisk => mMPer == 1 ? 0 : ProfitRoe / (1 - mMPer); // 리스크 대비 수익률에 대한 점수, 높을수록 좋은 전략

		public int adxth { get; set; }

		public void InitIndicators(params decimal[] p)
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
						//chartPack.UseMacd((int)p[0], (int)p[1], (int)p[2], (int)p[3], (int)p[4], (int)p[5]);
						//chartPack.UseAdx();
						//chartPack.UseSupertrend((int)p[6], (double)p[7]);

						chartPack.UseMacd(12, 26, 9, 9, 20, 7);
						chartPack.UseAdx();
						chartPack.UseSupertrend(10, 1.5);
						break;

					case "macd5":
						var macd1 = MacdTable.GetValues((int)p[0]);
						var macd2 = MacdTable.GetValues((int)p[1]);
						chartPack.UseMacd(macd1.Item1, macd1.Item2, macd1.Item3, macd2.Item1, macd2.Item2, macd2.Item3);
						chartPack.UseAdx();
						chartPack.UseSupertrend(10, 1.5);
						chartPack.UseEma((int)p[2]);
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

				if (StrategyId.StartsWith("custom", StringComparison.CurrentCultureIgnoreCase))
				{
					chartPack.UseRsi();
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

		private decimal GetBorrowSize(DateTime time)
		{
			return BorrowSize[new DateTime(time.Year, time.Month, time.Day)];
		}

		public (string, decimal) Run(BacktestType backtestType, Action<int> reportProgress, string reportFileName, int startIndex)
		{
			var reportInterval = Interval switch
			{
				KlineInterval.FiveMinutes => 288,
				KlineInterval.FifteenMinutes => 96,
				KlineInterval.ThirtyMinutes => 48,
				KlineInterval.OneHour => 24,
				KlineInterval.TwoHour => 12,
				KlineInterval.FourHour => 6,
				KlineInterval.SixHour => 4,
				KlineInterval.EightHour => 3,
				KlineInterval.TwelveHour => 2,
				KlineInterval.OneDay => 1,
				_ => 1440
			};

			//var seedPercent = 0.51m;
			//var prevEstimatedMoney = 0m;
			var currentTime = DateTime.Now;
			var maxChartCount = Charts.Max(c => c.Value.Count);

			{
				var time = Charts.ElementAt(0).Value[startIndex].DateTime;
				var time00 = new DateTime(time.Year, time.Month, time.Day);
				Leverage = ((IUseLeverageByDayOfTheWeek)this).GetLeverage(time);

				BaseOrderSize =
					MaxActiveDealsType == MaxActiveDealsType.Total ?
					EstimatedMoney * 0.99m * Leverage / MaxActiveDeals :
					EstimatedMoney * 0.99m * Leverage / MaxActiveDeals / 2;

				MarginSize = BaseOrderSize / Leverage;
				BorrowSize.Add(time00, MarginSize * (Leverage - 1));
			}
			
			for (int i = startIndex; i < maxChartCount; i++)
			{
				// Reset Order Size
				var time = Charts.ElementAt(0).Value[i].DateTime;
				if (time.Hour == 0 && time.Minute == 0)
				{
					Leverage = ((IUseLeverageByDayOfTheWeek)this).GetLeverage(time);

					BaseOrderSize =
						MaxActiveDealsType == MaxActiveDealsType.Total ?
						EstimatedMoney * 0.99m * Leverage / MaxActiveDeals :
						EstimatedMoney * 0.99m * Leverage / MaxActiveDeals / 2;

					MarginSize = BaseOrderSize / Leverage;
					BorrowSize.Add(time, MarginSize * (Leverage - 1));
				}

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
									{
										var slPrice = minPrice - (maxPrice - minPrice) * 0.1m;
										var tpPrice = maxPrice - (maxPrice - minPrice) * 0.1m;
										var slPer = Calculator.Roe(PositionSide.Long, c0.Quote.Open, slPrice);
										var tpPer = Calculator.Roe(PositionSide.Long, c0.Quote.Open, tpPrice);
										if (IsPowerGoldenCross(charts, 14, i, c1.Macd) && IsPowerGoldenCross2(charts, 14, i, c1.Macd2) && tpPer > 1.0m)
										{
											EntryPosition(PositionSide.Long, c0, c0.Quote.Open, slPrice, tpPrice);
										}
									}
									break;

								case "macd5":
									{
										var tpPrice = (decimal)c1.Ema1;
										var slPrice = minPrice - (tpPrice - minPrice) * 0.1m;
										var tpPer = Calculator.Roe(PositionSide.Long, c0.Quote.Open, tpPrice);
										if (IsPowerGoldenCross(charts, 14, i, c1.Macd) && IsPowerGoldenCross2(charts, 14, i, c1.Macd2) && tpPer > 1.0m)
										{
											EntryPosition(PositionSide.Long, c0, c0.Quote.Open, slPrice, tpPrice);
										}
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
										//IsTrueCandle(charts, i, "DDUDUDDU")
										////|| IsTrueCandle(charts, i, "DDDUDUDD")
										//|| IsTrueCandle(charts, i, "DDDUDUD")
										//|| IsTrueCandle(charts, i, "DUDUDDUD")
										//|| IsTrueCandle(charts, i, "DUUDUUUD")
										IsTrueCandle(charts, i, "DUDUDDU")
										)
										EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								//if (
								//	c1.CandlestickType == CandlestickType.Bearish
								//	&& c2.CandlestickType == CandlestickType.Bearish
								//	&& c3.CandlestickType == CandlestickType.Bullish
								//	&& c4.CandlestickType == CandlestickType.Bearish
								//	&& c5.CandlestickType == CandlestickType.Bearish
								//	//&& c6.CandlestickType == CandlestickType.Bearish
								//	//&& c7.CandlestickType == CandlestickType.Bearish
								//	//&& c1.BodyLength > 0.05m
								//	//&& c2.BodyLength > 0.05m
								//	//&& c3.BodyLength > 0.05m
								//	//&& !((IUseBlacklist)this).IsBannedPosition(symbol, PositionSide.Long, time)
								//	)
								//{
								//	//if (c0.Quote.Low < c1.Quote.Close)
								//	//{
								//	//	LongFillCount++;
								//	EntryPosition(PositionSide.Long, c0, c1.Quote.Close);
								//	//}
								//}
								//break;
								case "custom-1-01": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-02": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-03": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-04": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-05": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-06": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-07": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-08": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-09": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-10": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-11": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-12": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-13": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-14": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-15": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-16": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-17": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-18": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-19": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-20": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-21": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-22": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-23": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDDDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-24": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDDDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-25": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDDDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-26": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDDUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-27": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDDUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-28": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDDUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-29": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDUDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-30": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDUDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-31": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDUDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-32": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDUUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-33": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDUUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-34": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DDUUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-35": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUDDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-36": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUDDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-37": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUDDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-38": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUDUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-39": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUDUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-40": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUDUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-41": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUUDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-42": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUUDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-43": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUUDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-44": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUUUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-45": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUUUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-46": if (charts[i - 1].Rsi1 < 30 && IsTrueCandle(charts, i, "DUUUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;

								case "custom-1-47": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDDDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-48": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDDDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-49": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDDDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-50": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDDUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-51": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDDUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-52": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDDUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-53": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDUDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-54": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDUDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-55": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDUDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-56": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDUUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-57": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDUUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-58": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDUUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-59": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUDDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-60": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUDDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-61": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUDDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-62": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUDUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-63": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUDUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-64": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUDUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-65": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUUDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-66": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUUDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-67": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUUDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-68": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUUUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-69": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUUUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-70": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUUUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-71": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDDDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-72": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDDDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-73": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDDDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-74": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDDUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-75": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDDUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-76": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDDUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-77": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDUDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-78": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDUDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-79": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDUDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-80": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDUUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-81": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDUUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-82": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDUUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-83": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUDDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-84": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUDDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-85": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUDDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-86": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUDUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-87": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUDUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-88": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUDUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-89": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUUDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-90": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUUDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-91": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUUDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-92": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUUUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-93": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUUUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-94": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUUUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-95": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDDDDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-96": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDDDDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-97": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDDDDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-98": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDDDUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-99": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDDDUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-100": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDDDUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-101": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDDUDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-102": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDDUDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-103": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDDUDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-104": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDDUUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-105": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDDUUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-106": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDDUUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-107": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDUDDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-108": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDUDDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-109": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDUDDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-110": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDUDUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-111": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDUDUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-112": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDUDUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-113": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDUUDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-114": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDUUDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-115": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDUUDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-116": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDUUUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-117": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDUUUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-118": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDDUUUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-119": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUDDDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-120": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUDDDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-121": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUDDDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-122": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUDDUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-123": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUDDUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-124": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUDDUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-125": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUDUDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-126": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUDUDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-127": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUDUDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-128": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUDUUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-129": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUDUUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-130": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUDUUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-131": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUUDDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-132": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUUDDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-133": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUUDDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-134": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUUDUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-135": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUUDUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-136": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUUDUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-137": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUUUDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-138": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUUUDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-139": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUUUDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-140": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUUUUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-141": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUUUUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-142": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DDUUUUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-143": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDDDDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-144": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDDDDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-145": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDDDDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-146": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDDDUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-147": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDDDUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-148": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDDDUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-149": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDDUDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-150": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDDUDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-151": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDDUDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-152": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDDUUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-153": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDDUUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-154": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDDUUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-155": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDUDDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-156": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDUDDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-157": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDUDDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-158": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDUDUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-159": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDUDUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-160": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDUDUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-161": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDUUDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-162": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDUUDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-163": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDUUDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-164": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDUUUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-165": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDUUUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-166": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUDUUUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-167": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUDDDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-168": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUDDDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-169": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUDDDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-170": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUDDUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-171": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUDDUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-172": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUDDUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-173": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUDUDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-174": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUDUDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-175": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUDUDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-176": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUDUUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-177": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUDUUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-178": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUDUUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-179": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUUDDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-180": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUUDDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-181": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUUDDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-182": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUUDUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-183": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUUDUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-184": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUUDUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-185": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUUUDDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-186": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUUUDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-187": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUUUDUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-188": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUUUUDD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-189": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUUUUDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;
								case "custom-1-190": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "DUUUUUUD")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;


								case "custom-3-1": if (IsTrueCandle(charts, i, "DDUDUDDU")) EntryPosition(PositionSide.Long, c0, c1.Quote.Close); break;

								#region ...
								default:
									break;
							}
						}
					}
					else
					{
						/* LONG POSITION - EXIT */
						switch (ExitStrategyId.ToLower())
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

							case "macd5":
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
								if (IsTrueCandle(charts, i, "UDUUU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							//if(c0.Quote.Low <= longPosition.StopLossPrice)
							//{
							//	StopLoss(longPosition, c0);
							//} else if
							//if (
							//	//longPosition.Stage == 0 &&
							//	c1.CandlestickType == CandlestickType.Bullish
							//	&& c2.CandlestickType == CandlestickType.Bullish
							//	&& c3.CandlestickType == CandlestickType.Bullish
							//	//&& c4.CandlestickType == CandlestickType.Bullish
							//	//&& c5.CandlestickType == CandlestickType.Bullish
							//	//&& c1.BodyLength + c2.BodyLength >= CloseBodyLengthMin
							//	)
							//{
							//	//if (c0.Quote.High > c1.Quote.Close)
							//	//{
							//	//	LongExitCount++;
							//	ExitPosition(longPosition, c1, c1.Quote.Close);

							//	//if (((IUseBlacklist)this).IsPostBlacklist(PositionSide.Long, longPosition.EntryPrice, c1.Quote.Close)) // 손실이 임계치를 벗어나면 블랙리스트 등록
							//	//{
							//	//	var blacklistPosition = new BlacklistPosition(symbol, PositionSide.Long, time, time.AddHours(BlacklistBanHour));
							//	//	((IUseBlacklist)this).AddBlacklist(blacklistPosition);
							//	//}

							//	//ExitPositionHalf(longPosition, c1.Quote.Close);
							//	//}
							//}
							////else if (longPosition.Stage == 1)
							////{
							////	var subCharts = GetSubChart(symbol, time, Interval);
							////	for (int j = 0; j < subCharts.Count(); j++)
							////	{
							////		var chart = subCharts.ElementAt(j);
							////		if (chart.Supertrend1 < 0)
							////		{
							////			ExitPositionHalf2(longPosition, chart, chart.Quote.Close);
							////			break;
							////		}
							////	}
							////}
							//break;
							case "custom-2-01": if (IsTrueCandle(charts, i, "UU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-02": if (IsTrueCandle(charts, i, "UUU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-03": if (IsTrueCandle(charts, i, "UDU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-04": if (IsTrueCandle(charts, i, "UUUU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-05": if (IsTrueCandle(charts, i, "UUDU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-06": if (IsTrueCandle(charts, i, "UDUU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-07": if (IsTrueCandle(charts, i, "UUUUU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-08": if (IsTrueCandle(charts, i, "UUUDU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-09": if (IsTrueCandle(charts, i, "UUDUU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-10": if (IsTrueCandle(charts, i, "UUDDU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-11": if (IsTrueCandle(charts, i, "UDUUU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-12": if (IsTrueCandle(charts, i, "UDUDU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-13": if (IsTrueCandle(charts, i, "UDDUU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-14": if (IsTrueCandle(charts, i, "UUUUUU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-15": if (IsTrueCandle(charts, i, "UUUUDU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-16": if (IsTrueCandle(charts, i, "UUUDUU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-17": if (IsTrueCandle(charts, i, "UUUDDU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-18": if (IsTrueCandle(charts, i, "UUDUUU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-19": if (IsTrueCandle(charts, i, "UDUUUU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-20": if (IsTrueCandle(charts, i, "UDUUDU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-21": if (IsTrueCandle(charts, i, "UDUDUU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;
							case "custom-2-22": if (IsTrueCandle(charts, i, "UDDUUU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;


							case "custom-4-1":
								if (IsTrueCandle(charts, i, "UDUUU")) ExitPosition(longPosition, c1, c1.Quote.Close);
								else if (IsTrueCandle(charts, i, "UDUDU")) ExitPosition(longPosition, c1, c1.Quote.Close); break;

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
									{
										var slPrice = maxPrice + (maxPrice - minPrice) * 0.1m;
										var tpPrice = minPrice + (maxPrice - minPrice) * 0.1m;
										var slPer = Calculator.Roe(PositionSide.Short, c0.Quote.Open, slPrice);
										var tpPer = Calculator.Roe(PositionSide.Short, c0.Quote.Open, tpPrice);

										if (IsPowerDeadCross(charts, 14, i, c1.Macd) && IsPowerDeadCross2(charts, 14, i, c1.Macd2) && tpPer > 1.0m)
										{
											EntryPosition(PositionSide.Short, c0, c0.Quote.Open, slPrice, tpPrice);
										}
									}
									break;

								case "macd5":
									{
										var tpPrice = (decimal)c1.Ema1;
										var slPrice = maxPrice + (maxPrice - tpPrice) * 0.1m;
										var tpPer = Calculator.Roe(PositionSide.Short, c0.Quote.Open, tpPrice);
										if (IsPowerDeadCross(charts, 14, i, c1.Macd) && IsPowerDeadCross2(charts, 14, i, c1.Macd2) && tpPer > 1.0m)
										{
											EntryPosition(PositionSide.Short, c0, c0.Quote.Open, slPrice, tpPrice);
										}
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
										//IsTrueCandle(charts, i, "UUDUDUUD")
										////|| IsTrueCandle(charts, i, "UUUDUDUU")
										//|| IsTrueCandle(charts, i, "UUUDUDU")
										//|| IsTrueCandle(charts, i, "UDUDUUDU")
										//|| IsTrueCandle(charts, i, "UDDUDDDU")
										IsTrueCandle(charts, i, "UDUDUUD")
										)
										EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								//if (
								//	c1.CandlestickType == CandlestickType.Bullish
								//	&& c2.CandlestickType == CandlestickType.Bullish
								//	&& c3.CandlestickType == CandlestickType.Bearish
								//	&& c4.CandlestickType == CandlestickType.Bullish
								//	&& c5.CandlestickType == CandlestickType.Bullish
								//	//&& c6.CandlestickType == CandlestickType.Bullish
								//	//&& c7.CandlestickType == CandlestickType.Bullish
								//	//&& c1.BodyLength > 0.05m
								//	//&& c2.BodyLength > 0.05m
								//	//&& c3.BodyLength > 0.05m
								//	//&& !((IUseBlacklist)this).IsBannedPosition(symbol, PositionSide.Short, time)
								//	)
								//{
								//	//if (c0.Quote.High > c1.Quote.Close)
								//	//{
								//	//	ShortFillCount++;
								//	EntryPosition(PositionSide.Short, c0, c1.Quote.Close);
								//	//}
								//}
								//break;
								case "custom-1-01": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-02": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-03": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-04": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-05": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-06": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-07": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-08": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-09": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-10": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-11": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-12": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-13": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-14": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-15": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-16": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-17": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-18": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-19": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-20": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-21": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-22": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-23": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUUUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-24": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUUUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-25": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUUUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-26": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUUDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-27": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUUDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-28": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUUDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-29": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUDUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-30": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUDUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-31": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUDUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-32": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUDDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-33": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUDDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-34": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UUDDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-35": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDUUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-36": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDUUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-37": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDUUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-38": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDUDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-39": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDUDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-40": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDUDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-41": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDDUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-42": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDDUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-43": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDDUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-44": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDDDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-45": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDDDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-46": if (charts[i - 1].Rsi1 > 70 && IsTrueCandle(charts, i, "UDDDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;

								case "custom-1-47": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUUUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-48": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUUUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-49": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUUUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-50": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUUDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-51": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUUDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-52": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUUDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-53": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUDUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-54": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUDUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-55": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUDUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-56": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUDDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-57": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUDDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-58": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUDDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-59": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDUUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-60": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDUUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-61": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDUUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-62": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDUDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-63": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDUDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-64": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDUDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-65": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDDUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-66": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDDUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-67": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDDUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-68": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDDDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-69": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDDDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-70": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDDDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-71": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUUUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-72": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUUUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-73": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUUUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-74": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUUDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-75": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUUDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-76": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUUDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-77": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUDUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-78": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUDUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-79": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUDUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-80": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUDDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-81": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUDDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-82": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUDDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-83": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDUUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-84": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDUUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-85": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDUUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-86": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDUDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-87": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDUDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-88": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDUDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-89": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDDUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-90": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDDUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-91": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDDUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-92": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDDDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-93": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDDDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-94": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDDDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-95": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUUUUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-96": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUUUUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-97": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUUUUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-98": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUUUDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-99": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUUUDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-100": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUUUDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-101": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUUDUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-102": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUUDUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-103": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUUDUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-104": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUUDDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-105": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUUDDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-106": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUUDDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-107": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUDUUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-108": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUDUUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-109": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUDUUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-110": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUDUDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-111": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUDUDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-112": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUDUDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-113": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUDDUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-114": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUDDUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-115": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUDDUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-116": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUDDDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-117": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUDDDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-118": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUUDDDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-119": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDUUUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-120": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDUUUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-121": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDUUUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-122": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDUUDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-123": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDUUDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-124": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDUUDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-125": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDUDUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-126": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDUDUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-127": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDUDUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-128": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDUDDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-129": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDUDDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-130": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDUDDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-131": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDDUUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-132": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDDUUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-133": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDDUUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-134": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDDUDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-135": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDDUDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-136": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDDUDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-137": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDDDUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-138": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDDDUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-139": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDDDUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-140": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDDDDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-141": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDDDDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-142": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UUDDDDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-143": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUUUUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-144": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUUUUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-145": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUUUUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-146": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUUUDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-147": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUUUDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-148": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUUUDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-149": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUUDUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-150": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUUDUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-151": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUUDUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-152": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUUDDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-153": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUUDDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-154": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUUDDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-155": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUDUUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-156": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUDUUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-157": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUDUUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-158": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUDUDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-159": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUDUDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-160": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUDUDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-161": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUDDUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-162": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUDDUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-163": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUDDUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-164": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUDDDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-165": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUDDDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-166": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDUDDDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-167": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDUUUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-168": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDUUUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-169": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDUUUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-170": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDUUDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-171": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDUUDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-172": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDUUDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-173": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDUDUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-174": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDUDUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-175": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDUDUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-176": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDUDDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-177": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDUDDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-178": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDUDDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-179": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDDUUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-180": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDDUUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-181": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDDUUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-182": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDDUDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-183": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDDUDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-184": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDDUDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-185": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDDDUUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-186": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDDDUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-187": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDDDUDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-188": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDDDDUU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-189": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDDDDUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;
								case "custom-1-190": if (charts[i - 1].Adx < 20 && IsTrueCandle(charts, i, "UDDDDDDU")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;


								case "custom-3-1": if (IsTrueCandle(charts, i, "UUDUDUUD")) EntryPosition(PositionSide.Short, c0, c1.Quote.Close); break;

								#region ...
								default:
									break;
							}
						}
					}
					else
					{
						/* SHORT POSITION - EXIT */
						switch (ExitStrategyId.ToLower())
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

							case "macd5":
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
								if (IsTrueCandle(charts, i, "DUDDD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							//if (c0.Quote.High >= shortPosition.StopLossPrice)
							//{
							//	StopLoss(shortPosition, c0);
							//} else if
							//if (
							//	//shortPosition.Stage == 0 &&
							//	c1.CandlestickType == CandlestickType.Bearish
							//	&& c2.CandlestickType == CandlestickType.Bearish
							//	&& c3.CandlestickType == CandlestickType.Bearish
							//	//&& c4.CandlestickType == CandlestickType.Bearish
							//	//&& c5.CandlestickType == CandlestickType.Bearish
							//	//&& c1.BodyLength + c2.BodyLength >= CloseBodyLengthMin
							//	)
							//{
							//	//if (c0.Quote.Low < c1.Quote.Close)
							//	//{
							//	//	ShortExitCount++;
							//	ExitPosition(shortPosition, c1, c1.Quote.Close);

							//	//if (((IUseBlacklist)this).IsPostBlacklist(PositionSide.Short, shortPosition.EntryPrice, c1.Quote.Close)) // 손실이 임계치를 벗어나면 블랙리스트 등록
							//	//{
							//	//	var blacklistPosition = new BlacklistPosition(symbol, PositionSide.Short, time, time.AddHours(BlacklistBanHour));
							//	//	((IUseBlacklist)this).AddBlacklist(blacklistPosition);
							//	//}

							//	//ExitPositionHalf(shortPosition, c1.Quote.Close);
							//	//}
							//}
							////else if (shortPosition.Stage == 1)
							////{
							////	var subCharts = GetSubChart(symbol, time, Interval);
							////	for (int j = 0; j < subCharts.Count(); j++)
							////	{
							////		var chart = subCharts.ElementAt(j);
							////		if (chart.Supertrend1 > 0)
							////		{
							////			ExitPositionHalf2(shortPosition, chart, chart.Quote.Close);
							////			break;
							////		}
							////	}
							////}
							//break;
							case "custom-2-01": if (IsTrueCandle(charts, i, "DD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-02": if (IsTrueCandle(charts, i, "DDD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-03": if (IsTrueCandle(charts, i, "DUD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-04": if (IsTrueCandle(charts, i, "DDDD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-05": if (IsTrueCandle(charts, i, "DDUD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-06": if (IsTrueCandle(charts, i, "DUDD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-07": if (IsTrueCandle(charts, i, "DDDDD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-08": if (IsTrueCandle(charts, i, "DDDUD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-09": if (IsTrueCandle(charts, i, "DDUDD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-10": if (IsTrueCandle(charts, i, "DDUUD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-11": if (IsTrueCandle(charts, i, "DUDDD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-12": if (IsTrueCandle(charts, i, "DUDUD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-13": if (IsTrueCandle(charts, i, "DUUDD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-14": if (IsTrueCandle(charts, i, "DDDDDD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-15": if (IsTrueCandle(charts, i, "DDDDUD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-16": if (IsTrueCandle(charts, i, "DDDUDD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-17": if (IsTrueCandle(charts, i, "DDDUUD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-18": if (IsTrueCandle(charts, i, "DDUDDD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-19": if (IsTrueCandle(charts, i, "DUDDDD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-20": if (IsTrueCandle(charts, i, "DUDDUD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-21": if (IsTrueCandle(charts, i, "DUDUDD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;
							case "custom-2-22": if (IsTrueCandle(charts, i, "DUUDDD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;


							case "custom-4-1":
								if (IsTrueCandle(charts, i, "DUDDD")) ExitPosition(shortPosition, c1, c1.Quote.Close);
								else if (IsTrueCandle(charts, i, "DUDUD")) ExitPosition(shortPosition, c1, c1.Quote.Close); break;

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

					Ests.Add((currentTime, EstimatedMoney));
					var change = Ests.Count <= 1 ? 0 : Ests[^1].Item2 - Ests[^2].Item2;
					var changePer = Ests.Count <= 1 ? 0 : change / Ests[^2].Item2;
					var maxPer = Ests.Count <= 1 ? 1 : Ests[^1].Item2 / Ests.Max(x => x.Item2);
					ChangePers.Add((currentTime, changePer));
					MaxPers.Add((currentTime, maxPer));
					File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}.csv"), $"{currentTime:yyyy-MM-dd HH:mm:ss},{Win},{Lose},{WinRate.Round(2)},{LongPositionCount},{ShortPositionCount},{EstimatedMoney.Round(0)},{change.Round(0)},{changePer.Round(4):P},{maxPer.Round(4):P}" + Environment.NewLine);
					//,{LongFillCount},{LongExitCount},{ShortFillCount},{ShortExitCount}
				}
			}

			if (MaxPers.Count > 0)
			{
				mMPer = MaxPers.Min(x => x.Item2);
				ChangePerAveragesByDayOfTheWeek = ChangePers.GroupBy(cp => cp.Item1.AddDays(-1).DayOfWeek).Select(g => new KeyValuePair<DayOfWeek, decimal>
				(
					g.Key,
					g.Average(x => x.Item2)
				)).OrderBy(g => g.Key).ToDictionary(x => x.Key, x => x.Value);

				var builder = new StringBuilder();
				builder.AppendLine($"mMPer: {mMPer.Round(4):P}");
				foreach (var ch in ChangePerAveragesByDayOfTheWeek)
				{
					builder.AppendLine($"{ch.Key.ToString()}: {ch.Value.Round(4):P}");
				}
				File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}.csv"), builder.ToString());
			}

			File.AppendAllText(MercuryPath.Desktop.Down($"{reportFileName}.csv"),
				backtestType == BacktestType.All ? Environment.NewLine + Environment.NewLine :
				$"{Symbols[0]},{Win},{Lose},{WinRate.Round(2)},{EstimatedMoney.Round(2)}" + Environment.NewLine);

			// Position History
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
					if (c0.Macd < 0 && c0.Macd > c0.MacdSignal && c1.Macd < c1.MacdSignal && c0.Adx > adxth && c0.Supertrend1 > 0)
					{
						return true;
					}
				}
				else
				{
					if (c0.Macd < 0 && c0.Macd > c0.MacdSignal && c1.Macd < c1.MacdSignal && c0.Adx > adxth && c0.Supertrend1 > 0 && c0.Macd < currentMacd)
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
					if (c0.Macd2 < 0 && c0.Macd2 > c0.MacdSignal2 && c1.Macd2 < c1.MacdSignal2 && c0.Adx > adxth && c0.Supertrend1 > 0)
					{
						return true;
					}
				}
				else
				{
					if (c0.Macd2 < 0 && c0.Macd2 > c0.MacdSignal2 && c1.Macd2 < c1.MacdSignal && c0.Adx > adxth && c0.Supertrend1 > 0 && c0.Macd2 < currentMacd)
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
					if (c0.Macd > 0 && c0.Macd < c0.MacdSignal && c1.Macd > c1.MacdSignal && c0.Adx > adxth && c0.Supertrend1 < 0)
					{
						return true;
					}
				}
				else
				{
					if (c0.Macd > 0 && c0.Macd < c0.MacdSignal && c1.Macd > c1.MacdSignal && c0.Adx > adxth && c0.Supertrend1 < 0 && c0.Macd > currentMacd)
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
					if (c0.Macd2 > 0 && c0.Macd2 < c0.MacdSignal2 && c1.Macd2 > c1.MacdSignal2 && c0.Adx > adxth && c0.Supertrend1 < 0)
					{
						return true;
					}
				}
				else
				{
					if (c0.Macd2 > 0 && c0.Macd2 < c0.MacdSignal2 && c1.Macd2 > c1.MacdSignal2 && c0.Adx > adxth && c0.Supertrend1 < 0 && c0.Macd > currentMacd)
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

		//IEnumerable<ChartInfo> GetSubChart(string symbol, DateTime startTime, KlineInterval mainChartInterval)
		//{
		//	if (Interval == SubInterval)
		//	{
		//		throw new Exception("No subchart due to interval equal subinterval");
		//	}

		//	var endTime = startTime + mainChartInterval.ToTimeSpan() - TimeSpan.FromSeconds(1);
		//	return SubCharts[symbol].Where(d => d.DateTime >= startTime && d.DateTime <= endTime);
		//}

		/// <summary>
		/// 맨 마지막 인덱스가 가장 최근 봉(charts[i-1], 1봉전)
		/// </summary>
		/// <param name="charts"></param>
		/// <param name="i"></param>
		/// <param name="condition"></param>
		/// <returns></returns>
		bool IsTrueCandle(List<ChartInfo> charts, int i, string condition)
		{
			// p = 0, charts[i-1], 1봉전, 가장 최근 봉
			// p = 1, charts[i-2], 2봉전
			// p = 2, charts[i-3], 3봉전
			for (int p = condition.Length - 1; p >= 0; p--)
			{
				// Length = 6, p = 5 일경우 i - 1, 1봉전
				var chartIndex = i + p - condition.Length;
				switch (condition[p])
				{
					case 'U':
						if (charts[chartIndex].CandlestickType == CandlestickType.Bearish)
						{
							return false;
						}
						break;

					case 'D':
						if (charts[chartIndex].CandlestickType == CandlestickType.Bullish)
						{
							return false;
						}
						break;
				}
			}

			return true;
		}
	}
}
