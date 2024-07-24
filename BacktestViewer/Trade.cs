using Binance.Net.Enums;

namespace BacktestViewer
{
	public class Trade
	{
		/// <summary>
		/// 거래일시
		/// </summary>
		public DateTime Time { get; set; }
		public string _Time => Time.ToString("yyyy-MM-dd HH:mm:ss");

		/// <summary>
		/// 거래단가
		/// </summary>
		public decimal Price { get; set; }

		/// <summary>
		/// 포지션 방향
		/// </summary>
		public PositionSide Side { get; set; }
		public string _Side => Side == PositionSide.Long ? "L" : "S";

		/// <summary>
		/// 거래량
		/// </summary>
		public decimal OrderQuantity { get; set; }

		/// <summary>
		/// 거래 후 잔고
		/// </summary>
		public decimal Money { get; set; }
		public string _Money => Money.ToString("#,###");

		/// <summary>
		/// 거래 후 모든 포지션의 마진
		/// </summary>
		public decimal Margin { get; set; }
		public string _Margin => Margin.ToString("#,###");

		/// <summary>
		/// 거래 후 코인 보유량
		/// </summary>
		public decimal CoinQuantity { get; set; }

		/// <summary>
		/// 손익금액
		/// </summary>
		public decimal Pnl { get; set; }
		public string _Pnl => Pnl.ToString("#,###");

		/// <summary>
		/// 추정 자산
		/// </summary>
		public decimal Estimated => CoinQuantity > 0 ? Money + Margin + Pnl : CoinQuantity < 0 ? Money - Margin + Pnl : Money;
		public string _Estimated => Estimated.ToString("#,###");

		/// <summary>
		/// (그리드) 가장 근접한 롱 주문 가격
		/// </summary>
		public decimal NearestLongOrderPrice { get; set;}

		/// <summary>
		/// (그리드) 가장 근접한 숏 주문 가격
		/// </summary>
		public decimal NearestShortOrderPrice { get; set; }

		/// <summary>
		/// (그리드) 현재 롱 주문 개수
		/// </summary>
		public int LongOrderNum { get; set; }

		/// <summary>
		/// (그리드) 현재 숏 주문 개수
		/// </summary>
		public int ShortOrderNum { get; set; }

		public string Custom1 => $"{OrderQuantity}/{CoinQuantity}";


		public Trade(DateTime time, decimal price, PositionSide side, decimal orderQuantity, decimal money, decimal margin, decimal coinQuantity, decimal pnl, decimal nearestLongOrderPrice, decimal nearestShortOrderPrice, int longOrderNum, int shortOrderNum)
        {
			Time = time;
			Price = price;
			Side = side;
			OrderQuantity = orderQuantity;
			Money = money;
			Margin = margin;
			CoinQuantity = coinQuantity;
			Pnl = pnl;
			NearestLongOrderPrice = nearestLongOrderPrice;
			NearestShortOrderPrice = nearestShortOrderPrice;
			LongOrderNum = longOrderNum;
			ShortOrderNum = shortOrderNum;
        }
    }
}
