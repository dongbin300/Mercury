using Binance.Net.Enums;
using Mercury.Backtests;
using Mercury.Enums;

namespace Mercury.Data
{
    public class GridParameters
    {
		/// <summary>
		/// 그리드 타입
		/// Long Grid, Short Grid, Neutral Grid
		/// </summary>
		public GridType GridType { get; set; }

		/// <summary>
		/// 그리드 상한선
		/// </summary>
		public decimal UpperPrice { get; set; }

		/// <summary>
		/// 그리드 하한선
		/// </summary>
		public decimal LowerPrice { get; set; }

		/// <summary>
		/// 그리드 개수
		/// </summary>
		public int GridCount { get; set; }

		/// <summary>
		/// 그리드 간격
		/// </summary>
		public decimal GridInterval { get; set; }

		public GridParameters(GridType gridType, decimal upperPrice, decimal lowerPrice, int gridCount)
		{
			GridType = gridType;
			UpperPrice = upperPrice;
			LowerPrice = lowerPrice;
			GridCount = gridCount;
			GridInterval = (UpperPrice - LowerPrice) / GridCount;
		}

		public GridParameters(GridType gridType, decimal upperPrice, decimal lowerPrice, decimal gridInterval)
		{
			GridType = gridType;
			UpperPrice = upperPrice;
			LowerPrice = lowerPrice;
			GridInterval = gridInterval;
			GridCount = (int)((UpperPrice - LowerPrice) / GridInterval);
		}

		public void SetGridCount(int gridCount)
		{
			GridCount = gridCount;
			GridInterval = (UpperPrice - LowerPrice) / GridCount;
		}

		public void SetGridInterval(decimal gridInterval)
		{
			GridInterval = gridInterval;
			GridCount = (int)((UpperPrice - LowerPrice) / GridInterval);
		}

		public List<decimal> GetGridPrices()
		{
			var grids = new List<decimal>();

			for (int i = 0; i < GridCount; i++)
			{
				grids.Add(LowerPrice + GridInterval * i);
			}

			return grids;
		}

		public bool Validate()
		{
			if (UpperPrice <= LowerPrice)
			{
				return false;
			}

			if (GridCount <= 0)
			{
				return false;
			}

			if (GridInterval <= 0)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// 그리드 주문 목록을 생성합니다.
		/// </summary>
		/// <param name="symbol">거래 심볼</param>
		/// <param name="quantity">주문 수량</param>
		/// <returns>그리드 주문 목록</returns>
		public List<Order> GetGridOrders(string symbol, decimal quantity)
		{
			var orders = new List<Order>();
			var prices = GetGridPrices();

			for (int i = 0; i < prices.Count; i++)
			{
				var side = GridType switch
				{
					GridType.Long => PositionSide.Long,
					GridType.Short => PositionSide.Short,
					_ => PositionSide.Long
				};

				var order = new Order(symbol, side, prices[i], quantity);
				orders.Add(order);
			}

			return orders;
		}
	}
}
