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
		/// 최대 리스크 시 낙폭률 (Max Draw Down)
		/// </summary>
		public decimal Mdd { get; set; }
		/// <summary>
		/// 추후 개발 예정
		/// </summary>
		public decimal SharpeRatio { get; set; }
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
				Mdd = 1 - mMPer;
				ChangePerAveragesByDayOfTheWeek = ChangePers.GroupBy(cp => cp.Item1.AddDays(-1).DayOfWeek).Select(g => new KeyValuePair<DayOfWeek, decimal>
				(
					g.Key,
					g.Average(x => x.Item2)
				)).OrderBy(g => g.Key).ToDictionary(x => x.Key, x => x.Value);

				var builder = new StringBuilder();
				builder.AppendLine($"MDD: {Mdd.Round(4):P}");
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
		/// 포지션 진입 with 사이즈 (분할매수는 아님)
		/// </summary>
		/// <param name="side"></param>
		/// <param name="currentChart"></param>
		/// <param name="entryPrice"></param>
		/// <param name="stopLossPrice"></param>
		/// <param name="takeProfitPrice"></param>
		protected void EntryPositionOnlySize(PositionSide side, ChartInfo currentChart, decimal entryPrice, decimal orderSize, decimal? stopLossPrice = null, decimal? takeProfitPrice = null)
		{
			var quantity = orderSize / entryPrice;
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
		/// 분할매수용 포지션 진입 (주문량이 아닌 금액으로 계산)
		/// </summary>
		/// <param name="side"></param>
		/// <param name="currentChart"></param>
		/// <param name="entryPrice"></param>
		/// <param name="orderSize"></param>
		/// <param name="stopLossPrice"></param>
		/// <param name="takeProfitPrice"></param>
		protected void EntryPositionSize(PositionSide side, ChartInfo currentChart, decimal entryPrice, decimal orderSize, decimal? stopLossPrice = null, decimal? takeProfitPrice = null)
		{
			var quantity = orderSize / entryPrice;
			var amount = entryPrice * quantity;

			// 1. 기존 포지션 확인 (동일 심볼 & 방향)
			var existingPosition = Positions.FirstOrDefault(p =>
				p.Symbol == currentChart.Symbol &&
				p.Side == side &&
				p.ExitDateTime == null);

			if (existingPosition != null)
			{
				// 2. 기존 포지션에 수량 추가 (평균단가 계산)
				decimal totalQuantity = existingPosition.Quantity + quantity;
				decimal totalEntryAmount = existingPosition.EntryAmount + amount;
				decimal avgEntryPrice = totalEntryAmount / totalQuantity;

				existingPosition.Quantity = totalQuantity;
				existingPosition.EntryAmount = totalEntryAmount;
				existingPosition.EntryPrice = avgEntryPrice;

				// 3. 손절/익절 가격 업데이트 (원래 설정 유지 or 재계산)
				existingPosition.StopLossPrice = stopLossPrice ?? existingPosition.StopLossPrice;
				existingPosition.TakeProfitPrice = takeProfitPrice ?? existingPosition.TakeProfitPrice;
			}
			else
			{
				// 4. 새 포지션 생성
				Money += GetBorrowSize(currentChart.DateTime);
				Borrowed += GetBorrowSize(currentChart.DateTime);
				Money += side == PositionSide.Long ? -amount : amount;

				var newPosition = new Position(currentChart.DateTime, currentChart.Symbol, side, entryPrice)
				{
					Quantity = quantity,
					EntryAmount = amount
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
		/// 분할매도/청산 지원 포지션 탈출
		/// </summary>
		/// <param name="position">청산할 포지션</param>
		/// <param name="currentChart">현재 차트 정보</param>
		/// <param name="exitPrice">청산 가격</param>
		/// <param name="exitQuantity">청산 수량 (null이면 전량)</param>
		protected void ExitPositionSize(Position position, ChartInfo currentChart, decimal exitPrice, decimal exitQuantity)
		{
			var quantity = exitQuantity;
			var amount = exitPrice * quantity;

			Money += position.Side == PositionSide.Long ? amount : -amount;
			Money -= GetBorrowSize(position.Time) * (quantity / position.Quantity);
			Borrowed -= GetBorrowSize(position.Time) * (quantity / position.Quantity);

			decimal fee = (position.EntryAmount * (quantity / position.Quantity) + amount) * FeeRate;
			Money -= fee;

			position.ExitAmount += amount;
			position.EntryAmount -= position.EntryAmount * (quantity / position.Quantity);
			position.Quantity -= quantity;

			if (position.Quantity <= 0)
			{
				var result = position.Income > 0 ? PositionResult.Win : PositionResult.Lose;
				Positions.Remove(position);
				PositionHistories.Add(new PositionHistory(currentChart.DateTime, position.Time, position.Symbol, position.Side, result)
				{
					EntryAmount = position.EntryAmount + position.ExitAmount,
					ExitAmount = position.ExitAmount,
					Fee = fee
				});
				switch (result)
				{
					case PositionResult.Win: Win++; break;
					case PositionResult.Lose: Lose++; break;
				}
			}
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

		/// <summary>
		/// 포지션 절반 청산 (Stage 0 → 1)
		/// </summary>
		protected void GptExitPositionHalf(Position position, decimal exitPrice)
		{
			var quantity = position.Quantity / 2;
			var amount = exitPrice * quantity;

			Money += position.Side == PositionSide.Long ? amount : -amount;
			position.Quantity -= quantity;
			position.ExitAmount += amount;
			position.Stage = 1;
		}

		/// <summary>
		/// 남은 절반 포지션 청산 (Stage 1에서 호출)
		/// </summary>
		protected void GptExitPositionHalf2(Position position, ChartInfo currentChart, decimal exitPrice)
		{
			var quantity = position.Quantity;
			var amount = exitPrice * quantity;

			Money += position.Side == PositionSide.Long ? amount : -amount;
			Money -= GetBorrowSize(position.Time);
			Borrowed -= GetBorrowSize(position.Time);

			position.ExitAmount += amount;
			GptFinalizePosition(position, currentChart);
		}

		/// <summary>
		/// 전체 포지션 전량 청산 (손절 or MACD 반전 등)
		/// </summary>
		protected void GptExitPositionAll(Position position, decimal exitPrice, ChartInfo currentChart)
		{
			var quantity = position.Quantity;
			var amount = exitPrice * quantity;

			Money += position.Side == PositionSide.Long ? amount : -amount;
			Money -= GetBorrowSize(position.Time);
			Borrowed -= GetBorrowSize(position.Time);

			position.ExitAmount += amount;
			position.Quantity = 0;
			GptFinalizePosition(position, currentChart);
		}

		/// <summary>
		/// 포지션 최종 정산 및 히스토리 저장
		/// </summary>
		private void GptFinalizePosition(Position position, ChartInfo currentChart)
		{
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
		/// 지정가 주문 체결 여부
		/// </summary>
		/// <param name="side"></param>
		/// <param name="currentChart"></param>
		/// <param name="orderPrice"></param>
		/// <returns></returns>
		protected bool IsOrderFilled(PositionSide side, ChartInfo currentChart, decimal orderPrice, bool isEntry)
		{
			if (side == PositionSide.Long)
			{
				return isEntry
					? currentChart.Quote.Low <= orderPrice
					: currentChart.Quote.High >= orderPrice;
			}
			else
			{
				return isEntry
					? currentChart.Quote.High >= orderPrice
					: currentChart.Quote.Low <= orderPrice;
			}
		}

		/// <summary>
		/// 지정가 주문 시 예상 주문가격
		/// </summary>
		/// <param name="side"></param>
		/// <param name="currentChart"></param>
		/// <param name="isEntry"></param>
		/// <returns></returns>
		protected decimal GetAdjustedPrice(PositionSide side, ChartInfo currentChart, bool isEntry)
		{
			var open = currentChart.Quote.Open;
			var gapRate = 0.0003m; // 0.03%

			if (side == PositionSide.Long)
			{
				return isEntry
					? open * (1 - gapRate)
					: open * (1 + gapRate);
			}
			else
			{
				return isEntry
					? open * (1 + gapRate)
					: open * (1 - gapRate);
			}
		}

		protected decimal GetMinPrice(IList<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Min(x => x.Quote.Low);
		protected decimal GetMaxPrice(IList<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Max(x => x.Quote.High);
		protected decimal GetMinClosePrice(IList<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Min(x => x.Quote.Close);
		protected decimal GetMaxClosePrice(IList<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Max(x => x.Quote.Close);

		protected int GetSupertrendSwitchCount(IList<ChartInfo> charts, int period, int i)
		{
			if (i < period || charts == null || charts.Count == 0) return 0;

			int count = 0;
			decimal? prevSignal = null;
			for (int idx = i - period + 1; idx <= i; idx++)
			{
				var signal = charts[idx].Supertrend1;
				if (prevSignal != null && signal != null && prevSignal * signal < 0)
					count++;
				prevSignal = signal;
			}
			return count;
		}
	}
}
