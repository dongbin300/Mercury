using Binance.Net.Enums;
using Mercury.Charts;

namespace Mercury.Backtests
{
	/// <summary>
	/// 기존 Backtester와는 다른 개선된 Backtester 구현체입니다.
	/// Backtester2는 다음과 같은 기능을 지원해야 합니다.
	///
	/// 1. 향상된 성능: 대규모 데이터셋을 효율적으로 처리할 수 있어야 합니다.
	/// 2. 확장성: 다양한 전략과 지표를 쉽게 추가할 수 있어야 합니다.(abstract methods)
	/// 3. 차트 데이터(csv파일 데이터)를 읽어들여 백테스트를 수행할 수 있어야 합니다.
	/// 4. 멀티타임프레임 지원: 여러 타임프레임의 데이터를 동시에 분석할 수 있어야 합니다.
	/// 5. 레버리지, 분할매매 기능을 사용하여 백테스트를 수행할수 있어야 합니다.
	/// 6. 추후에 DcaBacktester, GridBacktester 등으로 확장할 수 있는 구조여야 합니다.
	///
	///
	/// 코딩을 할 때 Backtester의 코드를 곧이 곧대로 쓰지 않아도 됩니다.
	/// 개선이 필요한 부분은 개선해서 코딩해주시면 됩니다.
	/// </summary>
	public abstract class Backtester2 : IBacktesterStrategy
	{
		#region Properties

		/* 기본 설정 */
		public string ReportFileName { get; set; } = string.Empty;
		public decimal Seed { get; set; }
		public int Leverage { get; set; } = 1;
		public decimal BaseOrderSize { get; set; }
		public decimal MarginSize { get; set; }
		public decimal FeeRate { get; set; } = 0.0002m;

		/* 자산 관리 */
		public decimal Money { get; set; }
		public decimal EstimatedMoney => Money + CalculateUnrealizedPnL() - Borrowed;
		public List<(DateTime, decimal)> Ests { get; set; } = [];
		public List<(DateTime, decimal)> ChangePers { get; set; } = [];
		public List<(DateTime, decimal)> MaxPers { get; set; } = [];
		public decimal Mdd { get; set; }
		public decimal SharpeRatio { get; set; }
		public decimal ProfitRoe => Ests.Count > 0 ? Ests[^1].Item2 / Seed : 0;
		public decimal ResultPerRisk => Mdd == 1 ? 0 : ProfitRoe / (1 - Mdd);

		/* 거래 통계 */
		public int Win { get; set; } = 0;
		public int Lose { get; set; } = 0;
		public decimal WinRate => Win + Lose == 0 ? 0 : (decimal)Win / (Win + Lose) * 100;

		/* 포지션 관리 */
		public List<Position2> Positions { get; set; } = [];
		public bool IsEnableLongPosition { get; set; } = true;
		public bool IsEnableShortPosition { get; set; } = true;
		public bool IsGeneratePositionHistory { get; set; } = false;
		public bool IsGenerateTradeHistory { get; set; } = false;
		public bool IsGenerateDailyHistory { get; set; } = true;
		public List<PositionHistory2> PositionHistories { get; set; } = [];

		/* 레버리지 시스템 */
		public Dictionary<DateTime, decimal> BorrowSize { get; set; } = [];
		public decimal Borrowed { get; set; } = 0m;

		/* 차트 데이터 - 멀티타임프레임 지원 */
		public HashSet<string> Symbols { get; set; } = [];
		public Dictionary<string, List<ChartInfo>>[] Charts { get; set; } = new Dictionary<string, List<ChartInfo>>[3];

		/* DCA 설정 */
		public bool UseDca { get; set; } = false;
		public int DcaMaxSteps { get; set; } = 3;
		public decimal DcaStepPercent { get; set; } = 2.0m;
		public decimal DcaMultiplier { get; set; } = 1.5m;

		/* 현재 상태 */
		public string CurrentSymbol { get; set; } = string.Empty;
		public int CurrentChartIndex { get; set; } = 0;

		#endregion

		#region Abstract Methods - 전략 확장 포인트

		/// <summary>
		/// 롱 포지션 오픈 조건
		/// </summary>
		protected abstract void OnLongOpen(string symbol, Dictionary<string, List<ChartInfo>>[] charts, int i);

		/// <summary>
		/// 롱 포지션 클로즈 조건
		/// </summary>
		protected abstract void OnLongClose(string symbol, Dictionary<string, List<ChartInfo>>[] charts, int i, Position2 position);

		/// <summary>
		/// 숏 포지션 오픈 조건
		/// </summary>
		protected abstract void OnShortOpen(string symbol, Dictionary<string, List<ChartInfo>>[] charts, int i);

		/// <summary>
		/// 숏 포지션 클로즈 조건
		/// </summary>
		protected abstract void OnShortClose(string symbol, Dictionary<string, List<ChartInfo>>[] charts, int i, Position2 position);

		/// <summary>
		/// 지표 초기화
		/// </summary>
		protected abstract void InitializeIndicators(ChartPack chartPack, int timeframeIndex, params decimal[] parameters);

		/// <summary>
		/// 초기 자산 설정 (새로 추가)
		/// </summary>
		protected abstract void SetupInitialCapital();

		/// <summary>
		/// 리스크 관리 설정 (새로 추가)
		/// </summary>
		protected abstract void SetupRiskManagement();

		#endregion

		#region Core Methods - 핵심 기능

		/// <summary>
		/// CSV 파일로부터 차트 데이터 로드
		/// </summary>
		public virtual void LoadFromCsv(string filePath, string symbol, KlineInterval interval)
		{
			// TODO: CSV 파일 파싱 및 ChartInfo 로드 구현
			throw new NotImplementedException("CSV 로드 기능은 추후 구현 예정");
		}

		/// <summary>
		/// 백테스트 실행
		/// </summary>
		public virtual (string, decimal) Run(DateTime startTime, DateTime? endTime = null)
		{
			InitializeBacktest();
			ExecuteBacktest(startTime, endTime);
			return GenerateReport();
		}

		/// <summary>
		/// 백테스트 초기화
		/// </summary>
		protected virtual void InitializeBacktest()
		{
			// 초기 자산 설정
			SetupInitialCapital();

			// 리스크 관리 설정
			SetupRiskManagement();

			// 통계 초기화
			Win = 0;
			Lose = 0;
			Positions.Clear();
			PositionHistories.Clear();
			Ests.Clear();
			ChangePers.Clear();
			MaxPers.Clear();
			BorrowSize.Clear();
			Borrowed = 0m;
		}

		/// <summary>
		/// 백테스트 실행 루프 (성능 최적화)
		/// </summary>
		protected virtual void ExecuteBacktest(DateTime startTime, DateTime? endTime = null)
		{
			if (Charts[0] == null || Charts[0].Count == 0)
				throw new InvalidOperationException("차트 데이터가 로드되지 않았습니다.");

			var mainCharts = Charts[0];
			var endTimeActual = endTime ?? DateTime.MaxValue;

			// 각 심볼별로 병렬 처리 가능한 구조로 개선
			foreach (var symbol in Symbols)
			{
				if (!mainCharts.TryGetValue(symbol, out var symbolCharts) || symbolCharts.Count == 0)
					continue;

				// 시작 인덱스 찾기 (이진 검색으로 성능 향상)
				var startIndex = FindStartIndex(symbolCharts, startTime);
				if (startIndex == -1) continue;

				// 메인 루프
				for (int i = startIndex; i < symbolCharts.Count && symbolCharts[i].DateTime <= endTimeActual; i++)
				{
					ProcessCandle(symbol, symbolCharts[i], i);
				}
			}
		}

		/// <summary>
		/// 캔들 처리 (단일 책임 원칙 적용)
		/// </summary>
		protected virtual void ProcessCandle(string symbol, ChartInfo currentCandle, int index)
		{
			CurrentSymbol = symbol;
			CurrentChartIndex = index;

			// 현재 심볼의 모든 타임프레임 데이터 준비
			var timeframes = PrepareTimeframesData(symbol, index);

			// 기존 포지션 체크
			var existingPositions = Positions.Where(p => p.Symbol == symbol).ToList();

			// 포지션 클로즈 조건 확인
			foreach (var position in existingPositions)
			{
				if (position.Side == PositionSide.Long)
					OnLongClose(symbol, timeframes, index, position);
				else
					OnShortClose(symbol, timeframes, index, position);
			}

			// 포지션 오픈 조건 확인
			OnLongOpen(symbol, timeframes, index);
			OnShortOpen(symbol, timeframes, index);

			// 일별 자산 기록 (성능 최적화: 하루에 한 번만)
			RecordDailyEquity(currentCandle.DateTime);
		}

		/// <summary>
		/// 타임프레임 데이터 준비 (성능 최적화)
		/// </summary>
		protected virtual Dictionary<string, List<ChartInfo>>[] PrepareTimeframesData(string symbol, int mainIndex)
		{
			var timeframes = new Dictionary<string, List<ChartInfo>>[3];

			for (int i = 0; i < Charts.Length; i++)
			{
				timeframes[i] = Charts[i] ?? new Dictionary<string, List<ChartInfo>>();
			}

			return timeframes;
		}

		/// <summary>
		/// 시작 인덱스 이진 검색으로 찾기
		/// </summary>
		protected virtual int FindStartIndex(List<ChartInfo> charts, DateTime startTime)
		{
			var left = 0;
			var right = charts.Count - 1;

			while (left <= right)
			{
				var mid = left + (right - left) / 2;
				if (charts[mid].DateTime < startTime)
					left = mid + 1;
				else
					right = mid - 1;
			}

			return left < charts.Count ? left : -1;
		}

		/// <summary>
		/// 일별 자산 기록 (성능 최적화)
		/// </summary>
		protected virtual void RecordDailyEquity(DateTime currentTime)
		{
			// 마지막 기록이 오늘 아니면 새로 기록
			var lastRecord = Ests.LastOrDefault();
			var currentDay = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day);

			if (lastRecord.Item1.Date != currentDay)
			{
				var currentEquity = EstimatedMoney;
				Ests.Add((currentDay, currentEquity));

				// 일별 변동률 및 MDD 계산
				if (Ests.Count > 1)
				{
					var prevEquity = Ests[^2].Item2;
					var changePercent = (currentEquity - prevEquity) / prevEquity * 100;
					ChangePers.Add((currentDay, changePercent));

					// MDD 업데이트
					UpdateMaxDrawdown(currentEquity);
				}
			}
		}

		/// <summary>
		/// MDD 업데이트 (최적화된 알고리즘)
		/// </summary>
		protected virtual void UpdateMaxDrawdown(decimal currentEquity)
		{
			if (Ests.Count == 0) return;

			var peak = Ests.Select(e => e.Item2).Max();
			if (peak > 0)
			{
				var currentMdd = (peak - currentEquity) / peak;
				if (currentMdd > Mdd)
					Mdd = currentMdd;
			}
		}

		/// <summary>
		/// 보고서 생성 (향상된 형식)
		/// </summary>
		protected virtual (string, decimal) GenerateReport()
		{
			var fileName = string.IsNullOrEmpty(ReportFileName)
				? $"Backtest2_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
				: ReportFileName;

			// TODO: CSV 보고서 생성 로직 구현
			return (fileName, ProfitRoe);
		}

		#endregion

		#region Position Management - 포지션 관리

		/// <summary>
		/// 롱 포지션 오픈
		/// </summary>
		protected virtual void OpenLong(string symbol, decimal quantity, decimal price, DateTime time)
		{
			if (!IsEnableLongPosition) return;

			var position = new Position2(time, symbol, PositionSide.Long, price);
			position.Quantity = quantity;
			position.EntryAmount = quantity * price;
			position.TotalEntryAmount = position.EntryAmount;
			position.EntryCount = 1;
			position.MaxLeverageUsed = Leverage;

			Positions.Add(position);

			// 자산 차감
			var requiredMargin = position.EntryAmount / Leverage;
			if (Money >= requiredMargin)
			{
				Money -= requiredMargin;
				Borrowed += requiredMargin * (Leverage - 1);
			}
		}

		/// <summary>
		/// 숏 포지션 오픈
		/// </summary>
		protected virtual void OpenShort(string symbol, decimal quantity, decimal price, DateTime time)
		{
			if (!IsEnableShortPosition) return;

			var position = new Position2(time, symbol, PositionSide.Short, price);
			position.Quantity = quantity;
			position.EntryAmount = quantity * price;
			position.TotalEntryAmount = position.EntryAmount;
			position.EntryCount = 1;
			position.MaxLeverageUsed = Leverage;

			Positions.Add(position);

			// 자산 차감
			var requiredMargin = position.EntryAmount / Leverage;
			if (Money >= requiredMargin)
			{
				Money -= requiredMargin;
				Borrowed += requiredMargin * (Leverage - 1);
			}
		}

		/// <summary>
		/// 포지션 클로즈
		/// </summary>
		protected virtual void ClosePosition(Position2 position, decimal closePrice, DateTime time)
		{
			position.ExitAmount = position.Quantity * closePrice;
			position.ExitDateTime = time;

			// 실현 손익 계산
			var realizedPnL = position.Side == PositionSide.Long
				? (closePrice - position.EntryPrice) * position.Quantity
				: (position.EntryPrice - closePrice) * position.Quantity;

			// 수수료 차감
			var fee = (position.EntryAmount + position.ExitAmount) * FeeRate;
			var netProfit = realizedPnL - fee;

			// 자산 업데이트
			Money += position.ExitAmount / Leverage + netProfit;
			Borrowed -= position.EntryAmount / Leverage * (Leverage - 1);

			// 통계 업데이트
			if (netProfit > 0)
				Win++;
			else
				Lose++;

			// 포지션 기록 저장
			if (IsGeneratePositionHistory)
			{
				PositionHistories.Add(new PositionHistory2(position, closePrice, time));
			}

			// 활성 포지션에서 제거
			Positions.Remove(position);
		}

		/// <summary>
		/// DCA 추가 오픈
		/// </summary>
		protected virtual void AddDcaPosition(Position2 position, decimal quantity, decimal price, DateTime time)
		{
			if (!UseDca) return;

			// 평균 진입 가격 재계산
			var totalAmount = position.TotalEntryAmount + (quantity * price);
			var totalQuantity = position.Quantity + quantity;

			position.TotalEntryAmount = totalAmount;
			position.EntryPrice = totalAmount / totalQuantity;
			position.Quantity = totalQuantity;
			position.EntryCount++;
			position.DcaStep++;

			// 추가 자산 차감
			var additionalMargin = (quantity * price) / Leverage;
			if (Money >= additionalMargin)
			{
				Money -= additionalMargin;
				Borrowed += additionalMargin * (Leverage - 1);
			}
		}

		#endregion

		#region Utility Methods - 보조 기능

		/// <summary>
		/// 미실현 P&L 계산 (성능 최적화)
		/// </summary>
		protected virtual decimal CalculateUnrealizedPnL()
		{
			if (Positions.Count == 0) return 0m;

			var totalUnrealized = 0m;

			foreach (var position in Positions)
			{
				// 현재 가격 가져오기 (가장 최근 캔들의 종가)
				var currentPrice = GetCurrentPrice(position.Symbol);
				if (currentPrice == 0m) continue;

				// 미실현 손익 계산
				var unrealizedPnL = position.Side == PositionSide.Long
					? (currentPrice - position.EntryPrice) * position.Quantity
					: (position.EntryPrice - currentPrice) * position.Quantity;

				totalUnrealized += unrealizedPnL;
			}

			return totalUnrealized;
		}

		/// <summary>
		/// 현재 가격 가져오기 (캐싱 적용)
		/// </summary>
		protected virtual decimal GetCurrentPrice(string symbol)
		{
			if (Charts[0].TryGetValue(symbol, out var charts) && charts.Count > 0)
				return charts[^1].Quote.Close;

			return 0m;
		}

		/// <summary>
		/// 서브 차트 정보 가져오기 (멀티타임프레임 지원)
		/// </summary>
		public ChartInfo GetSubChart(string symbol, int mainIndex, int timeframeIndex = 1)
		{
			if (!Charts[0].TryGetValue(symbol, out var mainCharts) || mainCharts.Count <= mainIndex)
				return default!;

			var targetTime = mainCharts[mainIndex].DateTime;
			return Charts[timeframeIndex][symbol]
				.Where(c => c.DateTime <= targetTime)
				.OrderByDescending(c => c.DateTime)
				.FirstOrDefault() ?? default!;
		}

		/// <summary>
		/// 서브 차트 인덱스 가져오기
		/// </summary>
		public int GetSubChartIndex(string symbol, int mainIndex, int timeframeIndex = 1)
		{
			if (!Charts[0].TryGetValue(symbol, out var mainCharts) || mainCharts.Count <= mainIndex)
				return -1;

			var targetTime = mainCharts[mainIndex].DateTime;
			var charts = Charts[timeframeIndex][symbol];

			for (int i = charts.Count - 1; i >= 0; i--)
			{
				if (charts[i].DateTime <= targetTime)
					return i;
			}
			return -1;
		}

		/// <summary>
		/// 레버리지에 따른 차입 금액 계산
		/// </summary>
		protected virtual decimal GetBorrowSize(DateTime time)
		{
			var dateKey = new DateTime(time.Year, time.Month, time.Day);

			if (!BorrowSize.TryGetValue(dateKey, out decimal borrowSize))
			{
				borrowSize = MarginSize * (Leverage - 1);
				BorrowSize[dateKey] = borrowSize;
			}

			return borrowSize;
		}

		#endregion
	}

	/// <summary>
	/// Backtester2를 위한 향상된 포지션 클래스
	/// </summary>
	public class Position2(DateTime time, string symbol, PositionSide side, decimal entryPrice)
		: Position(time, symbol, side, entryPrice)
	{
		/// <summary>
		/// 추가된 속성: 포지션 전략 타입
		/// </summary>
		public string StrategyType { get; set; } = string.Empty;

		/// <summary>
		/// 추가된 속성: 최대 적재 레버리지
		/// </summary>
		public int MaxLeverageUsed { get; set; } = 1;

		/// <summary>
		/// 추가된 속성: 포지션 메모
		/// </summary>
		public string Notes { get; set; } = string.Empty;
	}

	/// <summary>
	/// Backtester2를 위한 향상된 포지션 기록 클래스
	/// </summary>
	public class PositionHistory2
	{
		/// <summary>
		/// 진입 시간
		/// </summary>
		public DateTime EntryTime { get; set; }

		/// <summary>
		/// 청산 시간
		/// </summary>
		public DateTime ExitTime { get; set; }

		/// <summary>
		/// 심볼
		/// </summary>
		public string Symbol { get; set; } = string.Empty;

		/// <summary>
		/// 포지션 방향
		/// </summary>
		public PositionSide Side { get; set; }

		/// <summary>
		/// 총 진입 가격 (평균)
		/// </summary>
		public decimal AverageEntryPrice { get; set; }

		/// <summary>
		/// 청산 가격
		/// </summary>
		public decimal ExitPrice { get; set; }

		/// <summary>
		/// 총 수량
		/// </summary>
		public decimal TotalQuantity { get; set; }

		/// <summary>
		/// 총 진입 금액
		/// </summary>
		public decimal TotalEntryAmount { get; set; }

		/// <summary>
		/// 총 청산 금액
		/// </summary>
		public decimal TotalExitAmount { get; set; }

		/// <summary>
		/// 실현 손익 (수수료 제외)
		/// </summary>
		public decimal RealizedPnL { get; set; }

		/// <summary>
		/// 총 수수료
		/// </summary>
		public decimal TotalFee { get; set; }

		/// <summary>
		/// 순수익 (실현 손익 - 수수료)
		/// </summary>
		public decimal NetProfit => RealizedPnL - TotalFee;

		/// <summary>
		/// 수익률 (%)
		/// </summary>
		public decimal ProfitPercentage => TotalEntryAmount == 0 ? 0 : (NetProfit / TotalEntryAmount) * 100;

		/// <summary>
		/// 보유 기간 (분)
		/// </summary>
		public double HoldingMinutes => (ExitTime - EntryTime).TotalMinutes;

		/// <summary>
		/// DCA 횟수
		/// </summary>
		public int DcaCount { get; set; }

		/// <summary>
		/// 사용한 레버리지
		/// </summary>
		public int LeverageUsed { get; set; }

		/// <summary>
		/// 전략 타입
		/// </summary>
		public string StrategyType { get; set; } = string.Empty;

		/// <summary>
		/// 메모
		/// </summary>
		public string Notes { get; set; } = string.Empty;

		public PositionHistory2()
		{
		}

		/// <summary>
		/// Position2로부터 PositionHistory2 생성
		/// </summary>
		public PositionHistory2(Position2 position, decimal exitPrice, DateTime exitTime)
		{
			EntryTime = position.Time;
			ExitTime = exitTime;
			Symbol = position.Symbol;
			Side = position.Side;
			AverageEntryPrice = position.EntryPrice;
			ExitPrice = exitPrice;
			TotalQuantity = position.Quantity;
			TotalEntryAmount = position.TotalEntryAmount;
			TotalExitAmount = position.ExitAmount;
			DcaCount = position.DcaStep;
			LeverageUsed = position.MaxLeverageUsed;
			StrategyType = position.StrategyType;
			Notes = position.Notes;

			// 실현 손익 계산
			RealizedPnL = Side == PositionSide.Long
				? (ExitPrice - AverageEntryPrice) * TotalQuantity
				: (AverageEntryPrice - ExitPrice) * TotalQuantity;

			// 수수료 계산 (진입 + 청산)
			TotalFee = (TotalEntryAmount + TotalExitAmount) * 0.0002m; // 0.02% 기준
		}
	}

	/// <summary>
	/// 백테스터 전략 인터페이스
	/// </summary>
	public interface IBacktesterStrategy
	{
		(string, decimal) Run(DateTime startTime, DateTime? endTime = null);
		void LoadFromCsv(string filePath, string symbol, KlineInterval interval);
	}

	/// <summary>
	/// DCA 백테스터를 위한 기반 클래스
	/// </summary>
	public abstract class DcaBacktester2 : Backtester2
	{
		/* DCA 고유 설정 */
		public decimal DcaPriceDropPercent { get; set; } = 2.0m;  // DCA 진입 가격 하락 %
		public int MaxDcaLevels { get; set; } = 5;               // 최대 DCA 단계
		public decimal DcaSizeMultiplier { get; set; } = 1.5m;    // DCA 주문 크기 배수
		public decimal TakeProfitPercent { get; set; } = 2.0m;    // 익절 %
		public bool IsDynamicSizing { get; set; } = false;        // 동적 사이징 사용 여부

		/// <summary>
		/// DCA 오픈 조건 오버라이드
		/// </summary>
		protected override void OnLongOpen(string symbol, Dictionary<string, List<ChartInfo>>[] charts, int i)
		{
			if (!charts[0].TryGetValue(symbol, out var chartList) || i >= chartList.Count)
				return;

			var currentPrice = chartList[i].Quote.Close;
			var existingLongPosition = Positions.FirstOrDefault(p => p.Symbol == symbol && p.Side == PositionSide.Long);

			if (existingLongPosition == null)
			{
				// 신규 오픈
				ExecuteDcaLongOpen(symbol, currentPrice, chartList[i].DateTime, 0);
			}
			else
			{
				// DCA 추가 오픈 확인
				if (ShouldAddDcaPosition(existingLongPosition, currentPrice))
				{
					var dcaStep = existingLongPosition.DcaStep + 1;
					if (dcaStep <= MaxDcaLevels)
					{
						ExecuteDcaLongOpen(symbol, currentPrice, chartList[i].DateTime, dcaStep);
					}
				}
			}
		}

		/// <summary>
		/// DCA 롱 오픈 실행
		/// </summary>
		protected virtual void ExecuteDcaLongOpen(string symbol, decimal price, DateTime time, int dcaStep)
		{
			decimal orderSize = CalculateDcaOrderSize(dcaStep);

			if (dcaStep == 0)
				OpenLong(symbol, orderSize, price, time);
			else
			{
				var existingPosition = Positions.FirstOrDefault(p => p.Symbol == symbol && p.Side == PositionSide.Long);
				if (existingPosition != null)
					AddDcaPosition(existingPosition, orderSize, price, time);
			}
		}

		/// <summary>
		/// DCA 추가 진입 조건 확인
		/// </summary>
		protected virtual bool ShouldAddDcaPosition(Position2 position, decimal currentPrice)
		{
			var priceDropPercent = (position.EntryPrice - currentPrice) / position.EntryPrice * 100;
			return priceDropPercent >= DcaPriceDropPercent;
		}

		/// <summary>
		/// DCA 주문 크기 계산
		/// </summary>
		protected virtual decimal CalculateDcaOrderSize(int dcaStep)
		{
			if (!IsDynamicSizing)
				return BaseOrderSize * (decimal)Math.Pow((double)DcaSizeMultiplier, dcaStep);

			// 동적 사이징 로직 (구현 필요)
			return BaseOrderSize;
		}

		/// <summary>
		/// DCA 익절 조건 확인
		/// </summary>
		protected virtual bool ShouldTakeProfit(Position2 position, decimal currentPrice)
		{
			var averageEntryPrice = CalculateAverageEntryPrice(position);
			var profitPercent = (currentPrice - averageEntryPrice) / averageEntryPrice * 100;
			return profitPercent >= TakeProfitPercent;
		}

		/// <summary>
		/// 평균 진입 가격 계산
		/// </summary>
		protected virtual decimal CalculateAverageEntryPrice(Position2 position)
		{
			return position.TotalEntryAmount > 0 ? position.TotalEntryAmount / position.Quantity : position.EntryPrice;
		}

		protected override void OnLongClose(string symbol, Dictionary<string, List<ChartInfo>>[] charts, int i, Position2 position)
		{
			if (!charts[0].TryGetValue(symbol, out var chartList) || i >= chartList.Count)
				return;

			var currentPrice = chartList[i].Quote.Close;

			// 익절 조건 확인
			if (ShouldTakeProfit(position, currentPrice))
				ClosePosition(position, currentPrice, chartList[i].DateTime);
		}

		protected override void OnShortOpen(string symbol, Dictionary<string, List<ChartInfo>>[] charts, int i)
		{
			// DCA는 기본적으로 롱만 지원
		}

		protected override void OnShortClose(string symbol, Dictionary<string, List<ChartInfo>>[] charts, int i, Position2 position)
		{
			// DCA는 기본적으로 롱만 지원
		}

		protected override void InitializeIndicators(ChartPack chartPack, int timeframeIndex, params decimal[] parameters)
		{
			// DCA 전략에서는 특별한 지표가 필요 없음
		}

		protected override void SetupInitialCapital()
		{
			Money = Seed;
		}

		protected override void SetupRiskManagement()
		{
			// DCA 전략 리스크 관리 설정
			UseDca = true;
			DcaMaxSteps = MaxDcaLevels;
		}
	}

	/// <summary>
	/// 그리드 백테스터를 위한 기반 클래스
	/// </summary>
	public abstract class GridBacktester2 : Backtester2
	{
		/* 그리드 고유 설정 */
		public decimal GridSizePercent { get; set; } = 1.0m;      // 그리드 간격 %
		public int GridLevels { get; set; } = 10;                 // 그리드 레벨 수
		public decimal GridOrderSize { get; set; } = 100m;        // 그리드 주문 크기
		public bool IsSymmetricGrid { get; set; } = true;         // 대칭 그리드 여부
		public decimal BasePrice { get; set; } = 0m;              // 기준 가격

		/* 그리드 상태 관리 */
		public Dictionary<string, List<GridLevel>> ActiveGrids { get; set; } = [];

		/// <summary>
		/// 그리드 레벨 정보
		/// </summary>
		public class GridLevel
		{
			public decimal Price { get; set; }
			public bool IsActive { get; set; }
			public PositionSide Side { get; set; }
			public DateTime PlacedTime { get; set; }
			public decimal OrderSize { get; set; }
		}

		/// <summary>
		/// 그리드 초기화
		/// </summary>
		protected virtual void InitializeGrid(string symbol, decimal basePrice)
		{
			BasePrice = basePrice;
			if (!ActiveGrids.ContainsKey(symbol))
			{
				ActiveGrids[symbol] = CreateGridLevels(basePrice);
			}
		}

		/// <summary>
		/// 그리드 레벨 생성
		/// </summary>
		protected virtual List<GridLevel> CreateGridLevels(decimal basePrice)
		{
			var gridLevels = new List<GridLevel>();
			var gridStep = basePrice * GridSizePercent / 100;

			for (int i = 1; i <= GridLevels; i++)
			{
				// 상단 그리드 (숏)
				var upperPrice = basePrice + (gridStep * i);
				gridLevels.Add(new GridLevel
				{
					Price = upperPrice,
					Side = PositionSide.Short,
					IsActive = false,
					OrderSize = GridOrderSize
				});

				// 하단 그리드 (롱)
				if (IsSymmetricGrid)
				{
					var lowerPrice = basePrice - (gridStep * i);
					gridLevels.Add(new GridLevel
					{
						Price = lowerPrice,
						Side = PositionSide.Long,
						IsActive = false,
						OrderSize = GridOrderSize
					});
				}
			}

			return gridLevels.OrderBy(g => g.Price).ToList();
		}

		/// <summary>
		/// 그리드 주문 실행
		/// </summary>
		protected override void OnLongOpen(string symbol, Dictionary<string, List<ChartInfo>>[] charts, int i)
		{
			if (!charts[0].TryGetValue(symbol, out var chartList) || i >= chartList.Count)
				return;

			var currentPrice = chartList[i].Quote.Close;

			if (!ActiveGrids.ContainsKey(symbol))
				InitializeGrid(symbol, currentPrice);

			ExecuteGridOrders(symbol, currentPrice, chartList[i].DateTime);
		}

		protected override void OnLongClose(string symbol, Dictionary<string, List<ChartInfo>>[] charts, int i, Position2 position)
		{
			// 그리드 전략에서는 자동 클로즈 로직이 필요 없음
		}

		protected override void OnShortOpen(string symbol, Dictionary<string, List<ChartInfo>>[] charts, int i)
		{
			// OnLongOpen에서 그리드 주문이 모두 처리됨
		}

		protected override void OnShortClose(string symbol, Dictionary<string, List<ChartInfo>>[] charts, int i, Position2 position)
		{
			// 그리드 전략에서는 자동 클로즈 로직이 필요 없음
		}

		protected override void InitializeIndicators(ChartPack chartPack, int timeframeIndex, params decimal[] parameters)
		{
			// 그리드 전략에서는 특별한 지표가 필요 없음
		}

		protected override void SetupInitialCapital()
		{
			Money = Seed;
		}

		protected override void SetupRiskManagement()
		{
			// 그리드 전략 리스크 관리 설정
		}

		/// <summary>
		/// 그리드 주문 실행 로직
		/// </summary>
		protected virtual void ExecuteGridOrders(string symbol, decimal currentPrice, DateTime time)
		{
			var gridLevels = ActiveGrids[symbol];

			foreach (var level in gridLevels.Where(g => !g.IsActive))
			{
				if ((level.Side == PositionSide.Long && currentPrice <= level.Price) ||
					(level.Side == PositionSide.Short && currentPrice >= level.Price))
				{
					// 그리드 주문 실행
					if (level.Side == PositionSide.Long)
						OpenLong(symbol, level.OrderSize, level.Price, time);
					else
						OpenShort(symbol, level.OrderSize, level.Price, time);

					level.IsActive = true;
					level.PlacedTime = time;
				}
			}
		}

		/// <summary>
		/// 그리드 재조정
		/// </summary>
		protected virtual void RebalanceGrid(string symbol, decimal newBasePrice)
		{
			// TODO: 그리드 재조정 로직 구현
		}
	}
}
