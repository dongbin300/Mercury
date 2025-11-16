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
        public HashSet<string> Symbols { get; set; } = [];
		/// <summary>
		/// 멀티타임프레임
		/// Charts[0] = Interval1 (메인), Charts[1] = Interval2 (서브1), Charts[2] = Interval3 (서브2)
		/// </summary>
		public Dictionary<string, List<ChartInfo>>[] Charts { get; set; } = new Dictionary<string, List<ChartInfo>>[3];

        /* Leverage System */
        public int[] Leverages { get; set; } = [];
        public Dictionary<DateTime, decimal> BorrowSize = [];
        public decimal Borrowed = 0m;

        /* Position */
        public List<Position> Positions { get; set; } = [];
        public bool IsGeneratePositionHistory { get; set; } = false;
        public bool IsGenerateTradeHistory { get; set; } = false; public bool IsGenerateDailyHistory { get; set; } = true;
        public List<PositionHistory> PositionHistories { get; set; } = [];
        public bool IsEnableLongPosition { get; set; } = true;
        public bool IsEnableShortPosition { get; set; } = true;
        public int LongPositionCount => Positions.Count(x => x.Side.Equals(PositionSide.Long));
        public int ShortPositionCount => Positions.Count(x => x.Side.Equals(PositionSide.Short));

        /* DCA Settings */
        public bool UseDca { get; set; } = false;  // DCA 사용 여부 플래그
        public int DcaMaxSteps { get; set; } = 3;
        public decimal DcaStepPercent { get; set; } = 2.0m;
        public decimal DcaMultiplier { get; set; } = 1.5m;

        /* Calculation */
        public string CurrentSymbol { get; set; } = string.Empty;
        public int CurrentChartIndex { get; set; } = 0;

        protected abstract void LongEntry(string symbol, List<ChartInfo> charts, int i);
        protected abstract void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition);
        protected abstract void ShortEntry(string symbol, List<ChartInfo> charts, int i);
        protected abstract void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition);
        protected abstract void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p);

		public ChartInfo GetSubChart(string symbol, int i, int intervalIndex = 1)
		{
			if (!Charts[0].TryGetValue(symbol, out List<ChartInfo>? value) || value.Count <= i) return default!;
			if (!Charts[intervalIndex].TryGetValue(symbol, out var subCharts) || subCharts.Count == 0) return default!;

			var targetDateTime = value[i].DateTime;
			var index = GetSubChartIndex(symbol, i, intervalIndex);
			return index >= 0 ? subCharts[index] : default!;
		}
		public List<ChartInfo> GetSubCharts(string symbol, int intervalIndex = 1) => Charts[intervalIndex].TryGetValue(symbol, out var value) ? value : [];
		public int GetSubChartIndex(string symbol, int i, int intervalIndex = 1)
		{
			if (!Charts[0].TryGetValue(symbol, out List<ChartInfo>? value) || value.Count <= i) return -1;
			if (!Charts[intervalIndex].TryGetValue(symbol, out var subCharts) || subCharts.Count == 0) return -1;

			var targetDateTime = value[i].DateTime;

			// Binary search for O(log n) performance
			int left = 0;
			int right = subCharts.Count - 1;
			int result = -1;

			while (left <= right)
			{
				int mid = left + (right - left) / 2;
				if (subCharts[mid].DateTime <= targetDateTime)
				{
					result = mid;
					left = mid + 1;
				}
				else
				{
					right = mid - 1;
				}
			}

			return result;
		}

		private decimal GetBorrowSize(DateTime time)
		{
			var dateKey = new DateTime(time.Year, time.Month, time.Day);

			// 키가 없으면 기본값 계산 후 추가
			if (!BorrowSize.TryGetValue(dateKey, out decimal borrowSize))
			{
				borrowSize = MarginSize * (Leverage - 1);
				BorrowSize[dateKey] = borrowSize;
			}

			return borrowSize;
		}

		public void Init(List<List<ChartPack>> chartPacks, List<decimal[]> p)
		{
			Symbols = [];
			Charts = new Dictionary<string, List<ChartInfo>>[3];

			if (chartPacks.Count > 3)
			{
				throw new Exception("Timeframe supports up to 3 only.");
			}

			for (int i = 0; i < chartPacks.Count; i++)
			{
				Charts[i] = [];
				var timeframeParams = i < p.Count ? p[i] : [];

				foreach (var chartPack in chartPacks[i])
				{
					InitIndicator(chartPack, i, timeframeParams);

					if (!Symbols.Contains(chartPack.Symbol))
						Symbols.Add(chartPack.Symbol);

					if (!Charts[i].ContainsKey(chartPack.Symbol))
						Charts[i][chartPack.Symbol] = [];

					Charts[i][chartPack.Symbol] = [.. chartPack.Charts];
				}
			}
		}

		public (string, decimal) Run(DateTime startTime, DateTime? endTime = null)
        {
            InitializeCsvFiles();

            // 첫번째 타임프레임을 메인차트로 사용
            var MainCharts = Charts[0];

			var maxChartCount = MainCharts.Max(c => c.Value.Count);
            var startIndex = MainCharts.First().Value
                .Select((x, index) => new { x.DateTime, Index = index, Difference = Math.Abs((x.DateTime - startTime).Ticks) })
                .OrderBy(x => x.Difference).First().Index;

            var startTime00 = new DateTime(startTime.Year, startTime.Month, startTime.Day);
            ResetOrderSize(startTime00);

            for (int i = startIndex; i < maxChartCount; i++)
            {
                CurrentChartIndex = i;

                // 안전한 DateTime 가져오기: 유효한 차트에서만 시간 추출
                DateTime? time = null;
                foreach (var chart in MainCharts.Values)
                {
                    if (i < chart.Count)
                    {
                        time = chart[i].DateTime;
                        break;
                    }
                }

                if (time == null)
                {
                    // 모든 차트에서 해당 인덱스가 유효하지 않음 - 루프 종료
                    break;
                }

                if (endTime != null && endTime < time.Value)
                {
                    break;
                }

                var currentTime = time.Value;
                if (currentTime.Hour == 0 && currentTime.Minute == 0)
                {
                    if (IsLiquidation())
                    {
                        if (IsGenerateDailyHistory)
                        {
                            File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}_daily.csv"), $"LIQ" + Environment.NewLine + Environment.NewLine);
                        }
                        WritePositionHistory();
                        return (string.Empty, 0m);
                    }

                    // Daily risk process & print report to file
                    ProcessDailyRisk(currentTime);
                    ResetOrderSize(currentTime);
                }

                foreach (var symbol in Symbols)
                {
                    CurrentSymbol = symbol;
                    var charts = MainCharts[symbol];

                    if (charts.Count <= i)
                    {
                        continue;
                    }

                    /* LONG POSITION */
                    if (IsEnableLongPosition)
                    {
                        var longPosition = GetActivePosition(symbol, PositionSide.Long);
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
                            if (UseDca)
                            {
                                // DCA를 위해 진입과 청산 모두 체크
                                LongEntry(symbol, charts, i);
                            }
                            LongExit(symbol, charts, i, longPosition);
                        }
                    }

                    /* SHORT POSITION */
                    if (IsEnableShortPosition)
                    {
                        var shortPosition = GetActivePosition(symbol, PositionSide.Short);
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
                            if (UseDca)
                            {
                                // DCA를 위해 진입과 청산 모두 체크
                                ShortEntry(symbol, charts, i);
                            }
                            ShortExit(symbol, charts, i, shortPosition);
                        }
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

        void InitializeCsvFiles()
        {
            if (IsGenerateDailyHistory)
            {
                var dailyPath = MercuryPath.Desktop.Down($"{ReportFileName}_daily.csv");
                // Always add header before each backtest run
                File.AppendAllText(dailyPath, Environment.NewLine + "DateTime,Win,Lose,WinRate,Long,Short,EstimatedMoney,Change,ChangePer,MaxPer" + Environment.NewLine);
            }

            if (IsGeneratePositionHistory)
            {
                var positionPath = MercuryPath.Desktop.Down($"{ReportFileName}_position.csv");
                // Always add header before each backtest run
                File.AppendAllText(positionPath, Environment.NewLine + "EntryTime,Symbol,Side,ExitTime,Result,Income,Fee" + Environment.NewLine);
            }

            if (IsGenerateTradeHistory)
            {
                var tradePath = MercuryPath.Desktop.Down($"{ReportFileName}_trade.csv");
                File.AppendAllText(tradePath, Environment.NewLine + "Time,Symbol,Side,Type,Price,Size,Profit,Comment" + Environment.NewLine);
            }
        }

        /// <summary>
        /// trade.csv에 거래 내역 기록
        /// </summary>
        /// <param name="time">거래 시간</param>
        /// <param name="symbol">심볼</param>
        /// <param name="side">포지션 방향 (Long/Short)</param>
        /// <param name="type">거래 타입 (Entry/Exit/PartialEntry/PartialExit)</param>
        /// <param name="price">거래 가격</param>
        /// <param name="size">포지션 사이즈 (금액)</param>
        /// <param name="profit">수익 (진입시 0, 청산시 계산)</param>
        /// <param name="fee">수수료</param>
        /// <param name="comment">추가 설명</param>
        protected void LogTrade(DateTime time, string symbol, PositionSide side, string type, decimal price, decimal size, decimal profit = 0m, decimal fee = 0m, string comment = "")
        {
            if (IsGenerateTradeHistory)
            {
                var tradePath = MercuryPath.Desktop.Down($"{ReportFileName}_trade.csv");

                // 파일이 없으면 헤더 추가
                if (!File.Exists(tradePath))
                {
                    var header = "Time,Symbol,Side,Type,Price,Size,Profit,Fee,Comment";
                    File.WriteAllText(tradePath, header + Environment.NewLine);
                }

                var line = $"{time:yyyy-MM-dd HH:mm:ss},{symbol},{side},{type},{price:F4},{size:F2},{profit:F2},{fee:F2},{comment}";
                File.AppendAllText(tradePath, line + Environment.NewLine);
            }
        }

        void ProcessDailyRisk(DateTime time)
        {
            Ests.Add((time, EstimatedMoney));
            var change = Ests.Count <= 1 ? 0 : Ests[^1].Item2 - Ests[^2].Item2;
            var changePer = Ests.Count <= 1 ? 0 : change / Ests[^2].Item2;
            var maxPer = Ests.Count <= 1 ? 1 : Ests[^1].Item2 / Ests.Max(x => x.Item2);
            ChangePers.Add((time, changePer));
            MaxPers.Add((time, maxPer));

            if (IsGenerateDailyHistory)
            {
                File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}_daily.csv"), $"{time:yyyy-MM-dd HH:mm:ss},{Win},{Lose},{WinRate.Round(2)},{LongPositionCount},{ShortPositionCount},{EstimatedMoney.Round(0)},{change.Round(0)},{changePer.Round(4):P},{maxPer.Round(4):P}" + Environment.NewLine);
            }
        }

        /// <summary>
        /// DCA 최대 총 투입 금액 계산 (DcaMultiplier 고려)
        /// </summary>
        decimal CalculateDcaTotalAmount()
        {
            if (!UseDca)
                return 1m;

            decimal total = 1m; // 첫 번째 진입 (1x)
            for (int step = 1; step < DcaMaxSteps; step++)
            {
                total += (decimal)Math.Pow((double)DcaMultiplier, step);
            }
            return total;
        }

        void ResetOrderSize(DateTime time)
        {
            //Leverage = ((IUseLeverageByDayOfTheWeek)this).GetLeverage(time);

            var time0 = new DateTime(time.Year, time.Month, time.Day);

            if (UseDca)
            {
                // DCA 전략: 총 투입 금액이 할당된 자금을 초과하지 않도록 조정
                var dcaTotal = CalculateDcaTotalAmount();
                var availableFunds = MaxActiveDealsType == MaxActiveDealsType.Total ?
                    EstimatedMoney * 0.99m * Leverage / MaxActiveDeals :
                    EstimatedMoney * 0.99m * Leverage / MaxActiveDeals / 2;

                BaseOrderSize = availableFunds / dcaTotal;
            }
            else
            {
                // 일반 전략: 기존 계산 방식 유지
                BaseOrderSize =
                    MaxActiveDealsType == MaxActiveDealsType.Total ?
                    EstimatedMoney * 0.99m * Leverage / MaxActiveDeals :
                    EstimatedMoney * 0.99m * Leverage / MaxActiveDeals / 2;
            }


            MarginSize = BaseOrderSize / Leverage;

            if (BorrowSize.ContainsKey(time0))
            {
                return;
            }

            BorrowSize.Add(time0, MarginSize * (Leverage - 1));
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

                if (IsGenerateDailyHistory)
                {
                    File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}_daily.csv"), builder.ToString());
                }
            }
        }

        void WritePositionHistory()
        {
            if (IsGeneratePositionHistory)
            {
                foreach (var h in PositionHistories)
                {
                    File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}_position.csv"),
                        $"{h.EntryTime:yyyy-MM-dd HH:mm:ss},{h.Symbol},{h.Side},{h.Time:yyyy-MM-dd HH:mm:ss},{h.Result},{Math.Round(h.Income, 4)},{Math.Round(h.Fee, 4)}" + Environment.NewLine
                        );
                }
                File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}_position.csv"), Environment.NewLine + Environment.NewLine);
            }
        }

        #region Position
        /// <summary>
        /// 포지션 진입
        /// </summary>
        /// <param name="side"></param>
        /// <param name="currentChart"></param>
        /// <param name="entryPrice"></param>
        /// <param name="stopLossPrice"></param>
        /// <param name="takeProfitPrice"></param>
        protected void EntryPosition(PositionSide side, ChartInfo currentChart, decimal entryPrice, decimal? stopLossPrice = null, decimal? takeProfitPrice = null, decimal positionSizeMultiplier = 1.0m)
        {
            var quantity = BaseOrderSize * positionSizeMultiplier / entryPrice;
            var amount = entryPrice * quantity;

            Money += GetBorrowSize(currentChart.DateTime);
            Borrowed += GetBorrowSize(currentChart.DateTime);
            Money += side == PositionSide.Long ? -amount : amount;
            Money -= amount * FeeRate; // 수수료 차감

            var newPosition = new Position(currentChart.DateTime, currentChart.Symbol, side, entryPrice)
            {
                Quantity = quantity,
                EntryAmount = entryPrice * quantity,
                TotalEntryAmount = entryPrice * quantity
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

            // Trade history 로깅
            LogTrade(currentChart.DateTime, currentChart.Symbol, side, "Entry", entryPrice, amount, 0m, amount * FeeRate, "Initial Entry");
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
            Money -= amount * FeeRate; // 수수료 차감

            var newPosition = new Position(currentChart.DateTime, currentChart.Symbol, side, entryPrice)
            {
                Quantity = quantity,
                EntryAmount = entryPrice * quantity,
                TotalEntryAmount = entryPrice * quantity
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

            // Trade history 로깅
            LogTrade(currentChart.DateTime, currentChart.Symbol, side, "Entry", entryPrice, amount, 0m, amount * FeeRate, "Entry with Size");
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

                // Trade history 로깅 - 분할진입
                LogTrade(currentChart.DateTime, currentChart.Symbol, side, "PartialEntry", entryPrice, amount, 0m, amount * FeeRate, $"Avg Price: {avgEntryPrice:F4}");
            }
            else
            {
                // 4. 새 포지션 생성
                Money += GetBorrowSize(currentChart.DateTime);
                Borrowed += GetBorrowSize(currentChart.DateTime);
                Money += side == PositionSide.Long ? -amount : amount;
                Money -= amount * FeeRate; // 수수료 차감

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

                // Trade history 로깅 - 새 포지션 진입
                LogTrade(currentChart.DateTime, currentChart.Symbol, side, "Entry", entryPrice, amount, 0m, amount * FeeRate, "New Position");
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
            // 오버플로우 방지
            var borrowSize = GetBorrowSize(position.Time);

            Money -= borrowSize;
            Borrowed -= borrowSize;

            position.ExitAmount = price * quantity;
            position.ExitQuantity = quantity;

            // 전량 청산이므로 전체 진입금액 기준으로 수익 계산
            var profit = position.Side == PositionSide.Long ?
                amount - position.EntryAmount :
                position.EntryAmount - amount;

            Positions.Remove(position);
            PositionHistories.Add(new PositionHistory(currentChart.DateTime, position.Time, position.Symbol, position.Side, PositionResult.Lose)
            {
                EntryAmount = position.EntryAmount,
                ExitAmount = position.ExitAmount,
                Fee = (position.EntryAmount + position.ExitAmount) * FeeRate
            });
            Lose++;
            Money -= (position.EntryAmount + position.ExitAmount) * FeeRate;

            // Trade history 로깅
            LogTrade(currentChart.DateTime, currentChart.Symbol, position.Side, "StopLoss", price, amount, profit, amount * FeeRate, "Stop Loss Exit");
        }

        /// <summary>
        /// 반 익절
        /// </summary>
        /// <param name="position"></param>
        protected void TakeProfitHalf(Position position, decimal? price = null)
        {
            price ??= position.TakeProfitPrice;
            var quantity = position.Quantity / 2;

            // 반익절하는 수량에 해당하는 실제 진입 비용 계산
            var exitRatio = quantity / position.Quantity;
            var actualEntryAmount = position.EntryAmount * exitRatio;
            var amount = price.Value * quantity;

            // 실제 진입 비용 기준으로 수익 계산
            var partialProfit = position.Side == PositionSide.Long ?
                amount - actualEntryAmount :
                actualEntryAmount - amount;

            Money += position.Side == PositionSide.Long ? price.Value * quantity : -price.Value * quantity;
            position.Quantity -= quantity;
            position.ExitAmount = price.Value * quantity;
            position.Stage = 1;

            // Trade history 로깅 - 반익절
            LogTrade(position.Time, position.Symbol, position.Side, "HalfProfit", price.Value, price.Value * quantity, partialProfit, amount * FeeRate, $"Half Exit, Remaining: {position.Quantity:F6}");
        }

        /// <summary>
        /// 나머지 반 익절
        /// </summary>
        /// <param name="position"></param>
        /// <param name="currentChart"></param>
        protected void TakeProfitHalf2(Position position, ChartInfo currentChart)
        {
            var price = currentChart.Quote.Open;
            var quantity = position.Quantity;
            var amount = price * quantity;

            Money += position.Side == PositionSide.Long ? amount : -amount;
            // 오버플로우 방지
            var borrowSize = GetBorrowSize(position.Time);

            Money -= borrowSize;
            Borrowed -= borrowSize;

            position.ExitAmount += amount;
            position.ExitQuantity += amount / price;

            // 나머지 수량에 대한 수익 계산
            var remainingEntryAmount = position.EntryAmount;
            var profit = position.Side == PositionSide.Long ?
                amount - remainingEntryAmount :
                remainingEntryAmount - amount;

            var result = profit > 0 ? PositionResult.Win : PositionResult.Lose;

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

            // Trade history 로깅 - 나머지 반익절 완료
            LogTrade(currentChart.DateTime, currentChart.Symbol, position.Side, "HalfProfit2", price, amount, profit, amount * FeeRate, $"Second Half Exit - {result}");
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
            // 오버플로우 방지
            var borrowSize = GetBorrowSize(position.Time);

            Money -= borrowSize;
            Borrowed -= borrowSize;

            position.ExitAmount = amount;
            position.ExitQuantity = amount / price;

            // 전량 청산이므로 전체 진입금액 기준으로 수익 계산
            var profit = position.Side == PositionSide.Long ?
                amount - position.EntryAmount :
                position.EntryAmount - amount;

            Positions.Remove(position);
            PositionHistories.Add(new PositionHistory(currentChart.DateTime, position.Time, position.Symbol, position.Side, PositionResult.Win)
            {
                EntryAmount = position.EntryAmount,
                ExitAmount = position.ExitAmount,
                Fee = (position.EntryAmount + position.ExitAmount) * FeeRate
            });
            Win++;
            Money -= (position.EntryAmount + position.ExitAmount) * FeeRate;

            // Trade history 로깅
            LogTrade(currentChart.DateTime, currentChart.Symbol, position.Side, "TakeProfit", price, amount, profit, amount * FeeRate, "Take Profit Exit");
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

            // 전량 청산이므로 전체 진입금액 기준으로 수익 계산
            var profit = position.Side == PositionSide.Long ?
                amount - position.EntryAmount :
                position.EntryAmount - amount;

            Money += position.Side == PositionSide.Long ? amount : -amount;
            // 오버플로우 방지
            var borrowSize = GetBorrowSize(position.Time);

            Money -= borrowSize;
            Borrowed -= borrowSize;

            position.ExitAmount = amount;
            position.ExitQuantity = amount / exitPrice;
            var result = profit > 0 ? PositionResult.Win : PositionResult.Lose;

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

            // Trade history 로깅
            LogTrade(currentChart.DateTime, currentChart.Symbol, position.Side, "Exit", exitPrice, amount, profit, amount * FeeRate, $"Full Exit - {result}");
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

            // 청산하는 수량에 해당하는 실제 진입 비용 계산
            var exitRatio = quantity / position.Quantity;
            var actualEntryAmount = position.EntryAmount * exitRatio;

            // 실제 진입 비용 기준으로 수익 계산
            var partialProfit = position.Side == PositionSide.Long ?
                amount - actualEntryAmount :
                actualEntryAmount - amount;

            Money += position.Side == PositionSide.Long ? amount : -amount;
            // 오버플로우 방지
            var ratio = quantity / position.Quantity;
            if (ratio < 0 || ratio > 1)
            {
                ratio = Math.Max(0, Math.Min(1, ratio));
            }

            var borrowSize = GetBorrowSize(position.Time);
            var borrowAdjustment = borrowSize * ratio;


            Money -= borrowAdjustment;
            Borrowed -= borrowAdjustment;

            decimal fee = amount * FeeRate;
            Money -= fee;

            position.ExitAmount += amount;
            position.ExitQuantity += quantity;
            position.EntryAmount -= position.EntryAmount * (quantity / position.Quantity);
            position.Quantity -= quantity;

            if (position.Quantity <= 0)
            {
                var result = position.Income > 0 ? PositionResult.Win : PositionResult.Lose;

                Positions.Remove(position);
                PositionHistories.Add(new PositionHistory(currentChart.DateTime, position.Time, position.Symbol, position.Side, result)
                {
                    EntryAmount = position.TotalEntryAmount,
                    ExitAmount = position.ExitAmount,
                    Fee = fee
                });
                switch (result)
                {
                    case PositionResult.Win: Win++; break;
                    case PositionResult.Lose: Lose++; break;
                }

                // Trade history 로깅 - 마지막 분할청산 (이 청산분만의 수익)
                LogTrade(currentChart.DateTime, currentChart.Symbol, position.Side, "PartialExit", exitPrice, amount, partialProfit, fee, $"Final Exit - {result}");
            }
            else
            {
                // Trade history 로깅 - 부분청산
                LogTrade(currentChart.DateTime, currentChart.Symbol, position.Side, "PartialExit", exitPrice, amount, partialProfit, fee, $"Remaining: {position.Quantity:F6}");
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
            // 오버플로우 방지
            var borrowSize = GetBorrowSize(position.Time);

            Money -= borrowSize;
            Borrowed -= borrowSize;

            position.ExitAmount += amount;
            position.ExitQuantity += amount / exitPrice;
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
            // 오버플로우 방지
            var borrowSize = GetBorrowSize(position.Time);

            Money -= borrowSize;
            Borrowed -= borrowSize;

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
            // 오버플로우 방지
            var borrowSize = GetBorrowSize(position.Time);

            Money -= borrowSize;
            Borrowed -= borrowSize;

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

        #region DCA Entry & Exit Methods

        /// <summary>
        /// 분할 진입 (DCA) - 기본 포지션이 있는지 확인하고 추가 진입
        /// </summary>
        /// <param name="side">포지션 방향</param>
        /// <param name="currentChart">현재 차트</param>
        /// <param name="entryPrice">진입 가격</param>
        /// <param name="stepPercent">하락/상승 퍼센트 (기본값 사용시 null)</param>
        /// <param name="multiplier">수량 배수 (기본값 사용시 null)</param>
        /// <param name="stopLossPrice">손절가</param>
        /// <param name="takeProfitPrice">익절가</param>
        protected void DcaEntryPosition(PositionSide side, ChartInfo currentChart, decimal entryPrice,
            decimal? stepPercent = null, decimal? multiplier = null, decimal? stopLossPrice = null, decimal? takeProfitPrice = null)
        {
            stepPercent ??= DcaStepPercent;
            multiplier ??= DcaMultiplier;

            var existingPosition = Positions.FirstOrDefault(p =>
                p.Symbol == currentChart.Symbol &&
                p.Side == side &&
                p.ExitDateTime == null);

            decimal orderSize;

            if (existingPosition == null)
            {
                orderSize = BaseOrderSize;
            }
            else
            {
                if (existingPosition.DcaStep >= DcaMaxSteps)
                {
                    return;
                }

                bool shouldDca = false;
                if (side == PositionSide.Long)
                {
                    shouldDca = entryPrice <= existingPosition.EntryPrice * (1 - stepPercent.Value / 100);
                }
                else
                {
                    shouldDca = entryPrice >= existingPosition.EntryPrice * (1 + stepPercent.Value / 100);
                }

                if (!shouldDca)
                {
                    return;
                }

                orderSize = BaseOrderSize * (decimal)Math.Pow((double)multiplier.Value, existingPosition.DcaStep);
            }

            var quantity = orderSize / entryPrice;
            var amount = entryPrice * quantity;

            if (existingPosition != null)
            {
                decimal totalQuantity = existingPosition.Quantity + quantity;
                decimal totalEntryAmount = existingPosition.EntryAmount + amount;
                decimal avgEntryPrice = totalEntryAmount / totalQuantity;

                existingPosition.Quantity = totalQuantity;
                existingPosition.EntryAmount = totalEntryAmount;
                existingPosition.TotalEntryAmount = totalEntryAmount;
                existingPosition.EntryPrice = avgEntryPrice;
                existingPosition.DcaStep++;

                existingPosition.StopLossPrice = stopLossPrice ?? existingPosition.StopLossPrice;
                existingPosition.TakeProfitPrice = takeProfitPrice ?? existingPosition.TakeProfitPrice;

                Money += side == PositionSide.Long ? -amount : amount;
                Money -= amount * FeeRate; // 수수료 차감

                // Trade history 로깅 - DCA 분할진입
                LogTrade(currentChart.DateTime, currentChart.Symbol, side, "DcaEntry", entryPrice, amount, 0m, amount * FeeRate, $"DCA Step {existingPosition.DcaStep}, Avg: {avgEntryPrice:F4}");
            }
            else
            {
                Money += GetBorrowSize(currentChart.DateTime);
                Borrowed += GetBorrowSize(currentChart.DateTime);
                Money += side == PositionSide.Long ? -amount : amount;
                Money -= amount * FeeRate; // 수수료 차감

                var newPosition = new Position(currentChart.DateTime, currentChart.Symbol, side, entryPrice)
                {
                    Quantity = quantity,
                    EntryAmount = amount,
                    TotalEntryAmount = amount,
                    DcaStep = 1
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

                // Trade history 로깅 - DCA 첫 진입
                LogTrade(currentChart.DateTime, currentChart.Symbol, side, "DcaEntry", entryPrice, amount, 0m, amount * FeeRate, "DCA Initial Entry");
            }
        }

        /// <summary>
        /// 분할 청산 - 포지션의 일정 비율 청산
        /// </summary>
        /// <param name="position">청산할 포지션</param>
        /// <param name="currentChart">현재 차트</param>
        /// <param name="exitPrice">청산 가격</param>
        /// <param name="exitPercent">청산 비율 (0.0 ~ 1.0)</param>
        protected void DcaExitPosition(Position position, ChartInfo currentChart, decimal exitPrice, decimal exitPercent)
        {
            if (exitPercent <= 0 || exitPercent > 1 || position.Quantity <= 0)
            {
                return;
            }

            var exitQuantity = position.Quantity * exitPercent;
            var amount = exitPrice * exitQuantity;

            // 청산하는 수량에 해당하는 실제 진입 비용 계산
            var exitRatio = exitQuantity / position.Quantity;
            var actualEntryAmount = position.EntryAmount * exitRatio;

            // 실제 진입 비용 기준으로 수익 계산
            var partialProfit = position.Side == PositionSide.Long ?
                amount - actualEntryAmount :
                actualEntryAmount - amount;

            Money += position.Side == PositionSide.Long ? amount : -amount;

            var borrowRatio = exitQuantity / position.Quantity;

            // 오버플로우 방지
            var borrowSize = GetBorrowSize(position.Time);


            var borrowAdjustment = borrowSize * borrowRatio;


            Money -= borrowAdjustment;
            Borrowed -= borrowAdjustment;

            decimal fee = amount * FeeRate;
            Money -= fee;

            position.ExitAmount += amount;
            position.ExitQuantity += exitQuantity;
            position.EntryAmount -= position.EntryAmount * borrowRatio;
            position.Quantity -= exitQuantity;

            if (position.Quantity <= 0.0001m)
            {
                var result = position.Income > 0 ? PositionResult.Win : PositionResult.Lose;

                Positions.Remove(position);
                PositionHistories.Add(new PositionHistory(currentChart.DateTime, position.Time, position.Symbol, position.Side, result)
                {
                    EntryAmount = position.TotalEntryAmount,
                    ExitAmount = position.ExitAmount,
                    Fee = fee
                });
                switch (result)
                {
                    case PositionResult.Win: Win++; break;
                    case PositionResult.Lose: Lose++; break;
                }

                // Trade history 로깅 - DCA 마지막 분할청산 (이 청산분만의 수익)
                LogTrade(currentChart.DateTime, currentChart.Symbol, position.Side, "DcaExit", exitPrice, amount, partialProfit, fee, $"DCA Final Exit {exitPercent:P0} - {result}");
            }
            else
            {
                // Trade history 로깅 - DCA 부분청산
                LogTrade(currentChart.DateTime, currentChart.Symbol, position.Side, "DcaExit", exitPrice, amount, partialProfit, fee, $"DCA Exit {exitPercent:P0}, Remaining: {position.Quantity:F6}");
            }
        }

        /// <summary>
        /// 분할 청산 - 고정 금액으로 청산
        /// </summary>
        /// <param name="position">청산할 포지션</param>
        /// <param name="currentChart">현재 차트</param>
        /// <param name="exitPrice">청산 가격</param>
        /// <param name="exitAmount">청산 금액</param>
        protected void DcaExitPositionByAmount(Position position, ChartInfo currentChart, decimal exitPrice, decimal exitAmount)
        {
            if (position.Quantity <= 0 || exitAmount <= 0 || exitPrice <= 0)
            {
                return;
            }

            var exitQuantity = exitAmount / exitPrice;

            if (exitQuantity > position.Quantity)
            {
                exitQuantity = position.Quantity;
            }

            var actualAmount = exitPrice * exitQuantity;

            // 청산하는 수량에 해당하는 실제 진입 비용 계산
            var exitRatio = exitQuantity / position.Quantity;
            var actualEntryAmount = position.EntryAmount * exitRatio;

            // 실제 진입 비용 기준으로 수익 계산
            var partialProfit = position.Side == PositionSide.Long ?
                actualAmount - actualEntryAmount :
                actualEntryAmount - actualAmount;

            Money += position.Side == PositionSide.Long ? actualAmount : -actualAmount;

            var borrowRatio = exitQuantity / position.Quantity;

            // 오버플로우 방지
            var borrowSize = GetBorrowSize(position.Time);


            var borrowAdjustment = borrowSize * borrowRatio;


            Money -= borrowAdjustment;
            Borrowed -= borrowAdjustment;

            decimal fee = actualAmount * FeeRate;
            Money -= fee;

            position.ExitAmount += actualAmount;
            position.ExitQuantity += exitQuantity;
            position.EntryAmount -= position.EntryAmount * borrowRatio;
            position.Quantity -= exitQuantity;

            if (position.Quantity <= 0.0001m)
            {
                var result = position.Income > 0 ? PositionResult.Win : PositionResult.Lose;

                Positions.Remove(position);
                PositionHistories.Add(new PositionHistory(currentChart.DateTime, position.Time, position.Symbol, position.Side, result)
                {
                    EntryAmount = position.TotalEntryAmount,
                    ExitAmount = position.ExitAmount,
                    Fee = fee
                });
                switch (result)
                {
                    case PositionResult.Win: Win++; break;
                    case PositionResult.Lose: Lose++; break;
                }

                // Trade history 로깅 - DCA 금액별 마지막 분할청산 (이 청산분만의 수익)
                LogTrade(currentChart.DateTime, currentChart.Symbol, position.Side, "DcaExitByAmount", exitPrice, actualAmount, partialProfit, fee, $"DCA Final Exit ${exitAmount} - {result}");
            }
            else
            {
                // Trade history 로깅 - DCA 금액별 부분청산
                LogTrade(currentChart.DateTime, currentChart.Symbol, position.Side, "DcaExitByAmount", exitPrice, actualAmount, partialProfit, fee, $"DCA Exit ${exitAmount}, Remaining: {position.Quantity:F6}");
            }
        }

        /// <summary>
        /// 포지션이 존재하는지 확인 (심볼별, 방향별)
        /// </summary>
        /// <param name="symbol">심볼</param>
        /// <param name="side">포지션 방향</param>
        /// <returns>포지션 객체 (없으면 null)</returns>
        protected Position? GetActivePosition(string symbol, PositionSide side)
        {
            return Positions.FirstOrDefault(p =>
                p.Symbol == symbol &&
                p.Side == side &&
                p.ExitDateTime == null);
        }

        /// <summary>
        /// 특정 심볼에 대한 모든 활성 포지션 수 확인
        /// </summary>
        /// <param name="symbol">심볼</param>
        /// <returns>활성 포지션 수</returns>
        protected int GetActivePositionCount(string symbol)
        {
            return Positions.Count(p => p.Symbol == symbol && p.ExitDateTime == null);
        }

        /// <summary>
        /// 포지션의 현재 수익률 계산
        /// </summary>
        /// <param name="position">포지션</param>
        /// <param name="currentPrice">현재 가격</param>
        /// <returns>수익률 (퍼센트)</returns>
        protected decimal GetPositionProfitPercent(Position position, decimal currentPrice)
        {
            if (position.Side == PositionSide.Long)
            {
                return (currentPrice - position.EntryPrice) / position.EntryPrice * 100;
            }
            else
            {
                return (position.EntryPrice - currentPrice) / position.EntryPrice * 100;
            }
        }

        #endregion


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
        protected decimal? GetMinCci(IList<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Min(x => x.Cci);
        protected decimal? GetMaxCci(IList<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Max(x => x.Cci);

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

        protected bool IsPowerGoldenCross(List<ChartInfo> charts, int lookback, int index, int adxth, decimal? currentMacd = null)
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

        protected bool IsPowerGoldenCross2(List<ChartInfo> charts, int lookback, int index, int adxth, decimal? currentMacd = null)
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

        protected bool IsPowerDeadCross(List<ChartInfo> charts, int lookback, int index, int adxth, decimal? currentMacd = null)
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

        protected bool IsPowerDeadCross2(List<ChartInfo> charts, int lookback, int index, int adxth, decimal? currentMacd = null)
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
        /// 맨 마지막 인덱스가 가장 최근 봉(charts[i-1], 1봉전)
        /// </summary>
        /// <param name="charts"></param>
        /// <param name="i"></param>
        /// <param name="condition"></param>
        /// <returns></returns>
        protected bool IsTrueCandle(List<ChartInfo> charts, int i, string condition, decimal minBodyLength = 0)
        {
            // p = 0, charts[i-1], 1봉전, 가장 최근 봉
            // p = 1, charts[i-2], 2봉전
            // p = 2, charts[i-3], 3봉전
            for (int p = condition.Length - 1; p >= 0; p--)
            {
                // Length = 6, p = 5 일경우 i - 1, 1봉전
                var chartIndex = i + p - condition.Length;
                var chart = charts[chartIndex];
                var candleType = chart.CandlestickType;
                var body = chart.BodyLength;

                // 최소 변동 길이 이하일 경우, Doji로 간주
                bool isDoji = body < minBodyLength;

                switch (condition[p])
                {
                    case 'U':
                        if (candleType != CandlestickType.Bullish || isDoji)
                        {
                            return false;
                        }
                        break;

                    case 'D':
                        if (candleType != CandlestickType.Bearish || isDoji)
                        {
                            return false;
                        }
                        break;

                    case 'N':
                        if (!isDoji)
                        {
                            return false;
                        }
                        break;
                }
            }

            return true;
        }

        public static decimal? GetCrossPrice(decimal prevA, decimal prevB, decimal nextA, decimal nextB)
        {
            var diffPrev = prevA - prevB;
            var diffNext = nextA - nextB;

            if ((diffPrev < 0 && diffNext > 0) || (diffPrev > 0 && diffNext < 0))
            {
                var denominator = diffNext - diffPrev;
                if (denominator == 0)
                {
                    return null; // 분모 0 방지
                }

                var t = (prevB - prevA) / denominator;
                // 교차점 y값
                return prevA + (nextA - prevA) * t;
            }
            else
            {
                // 교차 없음
                return null;
            }
        }
    }
}
#endregion