using Binance.Net.Enums;

using Mercury.Backtests.BacktestInterfaces;
using Mercury.Charts;
using Mercury.Enums;
using Mercury.Extensions;

using System.Text;

namespace Mercury.Backtests
{
	public abstract class Backtester(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : IUseLeverageByDayOfTheWeek
	{
		/* Inputs */
		public string ReportFileName { get; set; } = reportFileName;
		public decimal Seed { get; set; } = startMoney;
		public int Leverage { get; set; } = leverage;
		public decimal BaseOrderSize { get; set; }
		public decimal MarginSize { get; set; }

		public MaxActiveDealsType MaxActiveDealsType { get; set; } = maxActiveDealsType;
		public int MaxActiveDeals { get; set; } = maxActiveDeals;

		/* Outputs */
		/// <summary>
		/// 자산
		/// </summary>
		public decimal Money { get; set; } = startMoney;
		/// <summary>
		/// 추정자산
		/// </summary>
		public decimal EstimatedMoney => Money
			+ Positions.Where(x => x.Side.Equals(PositionSide.Long)).Sum(x => x.EntryPrice * x.Quantity)
			- Positions.Where(x => x.Side.Equals(PositionSide.Short)).Sum(x => x.EntryPrice * x.Quantity)
			- Borrowed;
		/// <summary>
		/// 일별 추정자산
		/// </summary>
		public List<(DateTime, decimal)> Ests { get; set; } = [];
		/// <summary>
		/// 일별 손익%
		/// </summary>
		public List<(DateTime, decimal)> ChangePers { get; set; } = [];
		/// <summary>
		/// 일별 고점대비%
		/// </summary>
		public List<(DateTime, decimal)> MaxPers { get; set; } = [];
		/// <summary>
		/// 최대 리스크 시 고점 대비 최저%
		/// </summary>
		public decimal mMPer { get; set; }
		/// <summary>
		/// 요일별 자산 변동률 평균
		/// </summary>
		public Dictionary<DayOfWeek, decimal> ChangePerAveragesByDayOfTheWeek { get; set; } = [];
		/// <summary>
		/// 시드 대비 수익률, 2.5배면 값이 2.5
		/// </summary>
		public decimal ProfitRoe => Ests.Count > 0 ? Ests[^1].Item2 / Seed : 0;
		/// <summary>
		/// 리스크 대비 수익률에 대한 점수, 높을수록 좋은 전략
		/// mMPer가 1(100%)이면 잘못된 값이므로 점수 0
		/// </summary>
		public decimal ResultPerRisk => mMPer == 1 ? 0 : ProfitRoe / (1 - mMPer);
		/// <summary>
		/// 수익회수
		/// </summary>
		public int Win { get; set; } = 0;
		/// <summary>
		/// 손실회수
		/// </summary>
		public int Lose { get; set; } = 0;
		/// <summary>
		/// 승률
		/// </summary>
		public decimal WinRate => Win + Lose == 0 ? 0 : (decimal)Win / (Win + Lose) * 100;

		/* Info */
		/// <summary>
		/// 거래 수수료
		/// </summary>
		public decimal FeeRate { get; set; } = 0.0002m;

		/* Chart */
		public List<string> Symbols { get; set; } = [];
		public Dictionary<string, List<ChartInfo>> Charts { get; set; } = [];

		/* Leverage System */
		public int[] Leverages { get; set; } = [];
		public Dictionary<DateTime, decimal> BorrowSize = [];
		public decimal Borrowed = 0m;

		/* Position */
		public List<Position> Positions { get; set; } = [];
		public bool IsGeneratePositionHistory { get; set; } = false;
		public List<PositionHistory> PositionHistories { get; set; } = [];
		public int LongPositionCount => Positions.Count(x => x.Side.Equals(PositionSide.Long));
		public int ShortPositionCount => Positions.Count(x => x.Side.Equals(PositionSide.Short));

		protected abstract void LongEntry(string symbol, List<ChartInfo> charts, int i);
		protected abstract void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition);
		protected abstract void ShortEntry(string symbol, List<ChartInfo> charts, int i);
		protected abstract void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition);
		protected abstract void InitIndicator(ChartPack chartPack, params decimal[] p);

		private decimal GetBorrowSize(DateTime time) => BorrowSize[new DateTime(time.Year, time.Month, time.Day)];

		public void Init(List<ChartPack> chartPacks, params decimal[] p)
		{
			Symbols = [];
			Charts = [];
			foreach (var chartPack in chartPacks)
			{
				InitIndicator(chartPack, p);
				Symbols.Add(chartPack.Symbol);
				Charts.Add(chartPack.Symbol, [.. chartPack.Charts]);
			}
		}

		public (string, decimal) Run(DateTime startTime, DateTime? endTime = null)
		{
			//var currentTime = DateTime.Now;
			var maxChartCount = Charts.Max(c => c.Value.Count);
			var startIndex = Charts.First().Value.Select((x, index) => new { x.DateTime, Index = index, Difference = Math.Abs((x.DateTime - startTime).Ticks) }).OrderBy(x => x.Difference).First().Index;

			var startTime00 = new DateTime(startTime.Year, startTime.Month, startTime.Day);
			ResetOrderSize(startTime00);

			for (int i = startIndex; i < maxChartCount; i++)
			{
				var time = Charts.ElementAt(0).Value[i].DateTime;

				if (endTime != null && endTime < time)
				{
					break;
				}

				if (time.Hour == 0 && time.Minute == 0)
				{
					if (IsLiquidation())
					{
						File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"), $"LIQ" + Environment.NewLine + Environment.NewLine);
						WritePositionHistory();
						return (string.Empty, 0m);
					}

					// Daily risk process & print report to file
					ProcessDailyRisk(time);
					ResetOrderSize(time);
				}

				foreach (var symbol in Symbols)
				{
					var charts = Charts[symbol];

					if (charts.Count <= i)
					{
						continue;
					}

					/* LONG POSITION */
					var longPosition = Positions.Find(x => x.Symbol.Equals(symbol) && x.Side.Equals(PositionSide.Long));
					if (longPosition == null)
					{
						if (MaxActiveDealsType == MaxActiveDealsType.Each && LongPositionCount >= MaxActiveDeals)
						{

						}
						else if (MaxActiveDealsType == MaxActiveDealsType.Total && LongPositionCount + ShortPositionCount >= MaxActiveDeals)
						{

						}
						else
						{
							LongEntry(symbol, charts, i);
						}
					}
					else
					{
						LongExit(symbol, charts, i, longPosition);
					}

					/* SHORT POSITION */
					var shortPosition = Positions.Find(x => x.Symbol.Equals(symbol) && x.Side.Equals(PositionSide.Short));
					if (shortPosition == null)
					{
						if (MaxActiveDealsType == MaxActiveDealsType.Each && ShortPositionCount >= MaxActiveDeals)
						{

						}
						else if (MaxActiveDealsType == MaxActiveDealsType.Total && LongPositionCount + ShortPositionCount >= MaxActiveDeals)
						{

						}
						else
						{
							ShortEntry(symbol, charts, i);
						}
					}
					else
					{
						ShortExit(symbol, charts, i, shortPosition);
					}
				}


			}

			WriteDailyRisk();
			WritePositionHistory();
			return (string.Empty, 0m);
		}

		bool IsLiquidation()
		{
			return EstimatedMoney < 0;
		}

		void ProcessDailyRisk(DateTime time)
		{
			Ests.Add((time, EstimatedMoney));
			var change = Ests.Count <= 1 ? 0 : Ests[^1].Item2 - Ests[^2].Item2;
			var changePer = Ests.Count <= 1 ? 0 : change / Ests[^2].Item2;
			var maxPer = Ests.Count <= 1 ? 1 : Ests[^1].Item2 / Ests.Max(x => x.Item2);
			ChangePers.Add((time, changePer));
			MaxPers.Add((time, maxPer));
			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"), $"{time:yyyy-MM-dd HH:mm:ss},{Win},{Lose},{WinRate.Round(2)},{LongPositionCount},{ShortPositionCount},{EstimatedMoney.Round(0)},{change.Round(0)},{changePer.Round(4):P},{maxPer.Round(4):P}" + Environment.NewLine);
		}

		void ResetOrderSize(DateTime time)
		{
			//Leverage = ((IUseLeverageByDayOfTheWeek)this).GetLeverage(time);

			BaseOrderSize =
				MaxActiveDealsType == MaxActiveDealsType.Total ?
				EstimatedMoney * 0.99m * Leverage / MaxActiveDeals :
				EstimatedMoney * 0.99m * Leverage / MaxActiveDeals / 2;

			MarginSize = BaseOrderSize / Leverage;

			if (BorrowSize.ContainsKey(time))
			{
				return;
			}

			BorrowSize.Add(time, MarginSize * (Leverage - 1));
		}

		void WriteDailyRisk()
		{
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
				builder.AppendLine();
				builder.AppendLine();

				File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"), builder.ToString());
			}
		}

		void WritePositionHistory()
		{
			if (IsGeneratePositionHistory)
			{
				foreach (var h in PositionHistories)
				{
					File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}_positionhistory.csv"),
						$"{h.EntryTime:yyyy-MM-dd HH:mm:ss},{h.Symbol},{h.Side},{h.Time:yyyy-MM-dd HH:mm:ss},{h.Result},{Math.Round(h.Income, 4)},{Math.Round(h.Fee, 4)}" + Environment.NewLine
						);
				}
				File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}_positionhistory.csv"), Environment.NewLine + Environment.NewLine);
			}
		}

		/// <summary>
		/// 포지션 진입
		/// </summary>
		/// <param name="side"></param>
		/// <param name="currentChart"></param>
		/// <param name="entryPrice"></param>
		/// <param name="stopLossPrice"></param>
		/// <param name="takeProfitPrice"></param>
		protected void EntryPosition(PositionSide side, ChartInfo currentChart, decimal entryPrice, decimal? stopLossPrice = null, decimal? takeProfitPrice = null)
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
		protected void StopLoss(Position position, ChartInfo currentChart)
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
		protected void TakeProfitHalf(Position position)
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
		protected void TakeProfitHalf2(Position position, ChartInfo currentChart)
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
		protected void TakeProfit(Position position, ChartInfo currentChart)
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
		protected void ExitPosition(Position position, ChartInfo currentChart, decimal exitPrice)
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
		protected void ExitPositionHalf(Position position, decimal exitPrice)
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
		protected void ExitPositionHalf2(Position position, ChartInfo currentChart, decimal exitPrice)
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

		protected decimal GetMinPrice(List<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Min(x => x.Quote.Low);
		protected decimal GetMaxPrice(List<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Max(x => x.Quote.High);
		protected decimal GetMinClosePrice(List<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Min(x => x.Quote.Close);
		protected decimal GetMaxClosePrice(List<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Max(x => x.Quote.Close);
	}
}
