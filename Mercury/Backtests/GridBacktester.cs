using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Data;
using Mercury.Enums;
using Mercury.Extensions;
using Mercury.Maths;

namespace Mercury.Backtests
{
	public class GridBacktester
	{
		#region Assets
		/// <summary>
		/// 초기 시드
		/// </summary>
		public decimal Seed { get; set; } = 1_000_000;

		/// <summary>
		/// 현재 주문 가능한 마진
		/// </summary>
		public decimal Money { get; set; } = 1_000_000;

		/// <summary>
		/// 보유 코인 개수
		/// +면 롱 포지션
		/// -면 숏 포지션
		/// </summary>
		public decimal CoinQuantity { get; set; } = 0;

		/// <summary>
		/// 추정 자산 (Money + 코인 가치)
		/// </summary>
		public decimal EstimatedMoney(int chartIndex) => Money + CoinQuantity * Prices[chartIndex].Value;

		/// <summary>
		/// 일별 평가자산
		/// </summary>
		public List<decimal> DailyEstimatedMoney { get; set; } = [];
		#endregion

		#region Estimated Indicator
		public decimal Mdd => Calculator.Mdd(DailyEstimatedMoney);
		public decimal SharpeRatio => Calculator.SharpeRatio(DailyEstimatedMoney);
		#endregion

		#region Chart Info
		/// <summary>
		/// 심볼
		/// </summary>
		public string Symbol { get; set; } = string.Empty;

		/// <summary>
		/// 가격 데이터 리스트
		/// </summary>
		public List<Price> Prices { get; set; } = [];

		/// <summary>
		/// 차트 데이터 리스트
		/// </summary>
		public List<ChartInfo> Charts { get; set; } = [];
		#endregion

		#region Market
		/// <summary>
		/// 수수료율
		/// 주문마다 발생되는 수수료율
		/// </summary>
		public decimal FeeRate = 0.0002M; // 0.02%

		/// <summary>
		/// 레버리지 배율
		/// </summary>
		public int Leverage { get; set; } = 1;

		/// <summary>
		/// 현재 세션 주문 크기
		/// </summary>
		public decimal SessionOrderSize { get; set; }
		#endregion

		#region Grid Info
		/// <summary>
		/// 그리드 파라미터 정보
		/// </summary>
		public GridParameters Grid { get; set; } = default!;

		/// <summary>
		/// 롱 오더 리스트
		/// </summary>
		public List<Order> LongOrders { get; set; } = [];

		/// <summary>
		/// 숏 오더 리스트
		/// </summary>
		public List<Order> ShortOrders { get; set; } = [];

		/// <summary>
		/// 가장 가까운 롱 오더
		/// </summary>
		public Order NearestLongOrder = default!;

		/// <summary>
		/// 가장 가까운 숏 오더
		/// </summary>
		public Order NearestShortOrder = default!;

		/// <summary>
		/// 상단 스탑로스
		/// </summary>
		public decimal UpperStopLossPrice { get; set; }

		/// <summary>
		/// 하단 스탑로스
		/// </summary>
		public decimal LowerStopLossPrice { get; set; }

		/// <summary>
		/// 체결 회수
		/// </summary>
		public int FillCount { get; set; } = 0;
		#endregion

		#region Log
		/// <summary>
		/// 보고서 파일 이름
		/// </summary>
		public string ReportFileName { get; set; } = string.Empty;

		/// <summary>
		/// 포지션 히스토리 생성 여부
		/// </summary>
		public bool IsGeneratePositionHistory = false;

		/// <summary>
		/// 포지션 히스토리
		/// </summary>
		public List<string> PositionHistories { get; set; } = [];		
		#endregion


		public GridBacktester(string symbol, List<Price> prices, List<ChartInfo> charts, string reportFileName)
		{
			Symbol = symbol;
			Prices = prices;
			Charts = charts;
			ReportFileName = reportFileName;
		}

		/// <summary>
		/// 롱 주문 생성
		/// </summary>
		/// <param name="price"></param>
		/// <param name="size"></param>
		protected void MakeLongOrder(decimal price, decimal size)
		{
			LongOrders.Add(Order.FromSize(Symbol, PositionSide.Long, price, size));
			NearestLongOrder = LongOrders.Find(x => x.Price.Equals(LongOrders.Max(x => x.Price))) ?? default!;
		}

		/// <summary>
		/// 숏 주문 생성
		/// </summary>
		/// <param name="price"></param>
		/// <param name="size"></param>
		protected void MakeShortOrder(decimal price, decimal size)
		{
			ShortOrders.Add(Order.FromSize(Symbol, PositionSide.Short, price, size));
			NearestShortOrder = ShortOrders.Find(x => x.Price.Equals(ShortOrders.Min(x => x.Price))) ?? default!;
		}

		/// <summary>
		/// 시장가 롱 주문 체결
		/// </summary>
		/// <param name="price"></param>
		/// <param name="size"></param>
		protected void FillMarketLong(decimal price, decimal size)
		{
			Money -= size / Leverage;
			Money -= size * FeeRate;
			CoinQuantity += size / price;
		}

		/// <summary>
		/// 시장가 숏 주문 체결
		/// </summary>
		/// <param name="price"></param>
		/// <param name="size"></param>
		protected void FillMarketShort(decimal price, decimal size)
		{
			Money += size / Leverage;
			Money -= size * FeeRate;
			CoinQuantity -= size / price;
		}

		/// <summary>
		/// 주문 체결 처리
		/// </summary>
		/// <param name="order"></param>
		/// <param name="chartIndex"></param>
		protected void Fill(Order order, int chartIndex)
		{
			if(order.Side == PositionSide.Long)
			{
				Money -= order.Size / Leverage;
				Money -= order.Size * FeeRate;
				CoinQuantity += order.Quantity;

				// 롱 주문 체결됐으므로 한 단계 위에 숏 주문 생성
				// Binance Futures Grid Bot은 기본적으로 각 그리드 주문에 같은 수량을 사용함(사이즈가 아니라)
				MakeShortOrder(order.Price + Grid.GridInterval, (order.Price + Grid.GridInterval) * order.Quantity);

				// 체결된 롱 주문은 삭제
				LongOrders.Remove(order);
			}
			else
			{
				Money += order.Size / Leverage;
				Money -= order.Size * FeeRate;
				CoinQuantity -= order.Quantity;

				MakeLongOrder(order.Price - Grid.GridInterval, (order.Price - Grid.GridInterval) * order.Quantity);

				ShortOrders.Remove(order);
			}

			// 가까운 주문 갱신
			NearestLongOrder = LongOrders.Find(x => x.Price.Equals(LongOrders.Max(x => x.Price))) ?? default!;
			NearestShortOrder = ShortOrders.Find(x => x.Price.Equals(ShortOrders.Min(x => x.Price))) ?? default!;

			// 포지션 히스토리 생성
			if (IsGeneratePositionHistory)
			{
				var nearestLongOrderPrice = 0m;
				if (NearestLongOrder != null)
				{
					nearestLongOrderPrice = NearestLongOrder.Price;
				}
				var nearestShortOrderPrice = 0m;
				if (NearestShortOrder != null)
				{
					nearestShortOrderPrice = NearestShortOrder.Price;
				}

				PositionHistories.Add(
					$"{Prices[chartIndex].Date:yyyy-MM-dd HH:mm:ss.fff}," +
					$"{Prices[chartIndex].Value:#.##}," +
					$"{order.Side}," +
					$"{order.Quantity:#.##}," +
					$"{Money:0.00}," +
					$"{CoinQuantity:0.00}," +
					$"{nearestLongOrderPrice:#.##}," +
					$"{nearestShortOrderPrice:#.##}"
					);
			}

			FillCount++;
		}

		/// <summary>
		/// 시장가로 모든 포지션 정리
		/// </summary>
		/// <param name="chartIndex"></param>
		protected void CloseAllPositions(int chartIndex)
		{
			var currentPrice = Prices[chartIndex].Value;

			Money += currentPrice * CoinQuantity;
			Money -= currentPrice * Math.Abs(CoinQuantity) * FeeRate;
			CoinQuantity = 0;

			WriteStatus(chartIndex, "CLOSE_ALL");
		}

		/// <summary>
		/// 그리드 초기화
		/// </summary>
		protected void InitGrid(GridType gridType, decimal upperPrice, decimal lowerPrice, int gridCount, int chartIndex)
		{
			SetGrid(gridType, upperPrice, lowerPrice, gridCount);
			SetOrder(chartIndex);

			WriteStatus(chartIndex, "INIT_GRID");
		}

		/// <summary>
		/// 그리드 초기화
		/// </summary>
		protected void InitGrid(GridType gridType, decimal upperPrice, decimal lowerPrice, decimal gridInterval, int chartIndex)
		{
			SetGrid(gridType, upperPrice, lowerPrice, gridInterval);
			SetOrder(chartIndex);

			WriteStatus(chartIndex, "INIT_GRID");
		}

		/// <summary>
		/// 그리드 파라미터 설정
		/// </summary>
		protected void SetGrid(GridType gridType, decimal upperPrice, decimal lowerPrice, int gridCount)
		{
			Grid = new GridParameters(gridType, upperPrice, lowerPrice, gridCount);

			// 현재 세션 주문 크기 설정
			SessionOrderSize = Money * 0.9m * Leverage / Grid.GridCount * 2;
		}

		/// <summary>
		/// 그리드 파라미터 설정
		/// </summary>
		protected void SetGrid(GridType gridType, decimal upperPrice, decimal lowerPrice, decimal gridInterval)
		{
			Grid = new GridParameters(gridType, upperPrice, lowerPrice, gridInterval);

			// 현재 세션 주문 크기 설정
			SessionOrderSize = Money * 0.9m * Leverage / Grid.GridCount * 2;
		}

		/// <summary>
		/// 주문 설정
		/// </summary>
		protected void SetOrder(int chartIndex)
		{
			var currentPrice = Prices[chartIndex].Value;
			LongOrders = [];
			ShortOrders = [];

			// 그리드 가격 리스트
			var gridPrices = Grid.GetGridPrices();

			// 가장 가까운 그리드 가격 제외
			var nearestGrid = gridPrices.OrderBy(x => Math.Abs(x - currentPrice)).First();
			gridPrices.Remove(nearestGrid);

			// 주문 생성
			foreach (var gridPrice in gridPrices)
			{
				if (gridPrice < currentPrice)
				{
					MakeLongOrder(gridPrice, SessionOrderSize);
				}
				else
				{
					MakeShortOrder(gridPrice, SessionOrderSize);
				}
			}

			// 롱/숏 그리드일 경우 시장가 주문으로 초기 보유
			if(Grid.GridType == GridType.Long)
			{
				// 숏 주문의 코인개수 합계만큼 선 롱진입
				FillMarketLong(currentPrice, currentPrice * ShortOrders.Sum(x => x.Quantity));
			}
			else if (Grid.GridType == GridType.Short)
			{
				// 롱 주문의 코인개수 합계만큼 선 숏진입
				FillMarketShort(currentPrice, currentPrice * LongOrders.Sum(x => x.Quantity));
			}
		}

		protected void AddDailyEstimatedMoney(int chartIndex)
		{
			DailyEstimatedMoney.Add(EstimatedMoney(chartIndex));
		}

		protected void WriteStatus(int chartIndex, string message)
		{
			var price = Prices[chartIndex].Value;
			var time = Prices[chartIndex].Date;

			File.AppendAllText(MercuryPath.Desktop.Down($"{ReportFileName}.csv"),
			$"{time:yyyy-MM-dd HH:mm:ss}," +
			$"{message}," +
			$"{Grid.GridType}," +
			$"{Grid.LowerPrice.Round(2)}," +
			$"{Grid.UpperPrice.Round(2)}," +
			$"{Grid.GridCount}," +
			$"{Grid.GridInterval.Round(2)}," +
			$"{LongOrders.Count}," +
			$"{ShortOrders.Count}," +
			$"{SessionOrderSize.Round(0)}," +
			$"{CoinQuantity.Round(2)}," +
			$"EST:{EstimatedMoney(chartIndex).Round(0)}," +
			Environment.NewLine);
		}
	}
}