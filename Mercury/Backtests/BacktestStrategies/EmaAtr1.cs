using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// EMA 12/26 크로스 + ATR 변동성 손절 + 동적 포지션 사이즈 전략
	/// </summary>
	public class EmaAtr1(
		string reportFileName,
		decimal startMoney,
		int leverage,
		MaxActiveDealsType maxActiveDealsType,
		int maxActiveDeals
	) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		// 파라미터
		protected decimal atrPeriod = 14m;         // ATR 기간
		protected decimal atrMultiplier = 2.0m;    // ATR 손절 배수
		protected decimal tprate = 2.0m;           // TP: 손절폭의 몇 배로 익절할지 (RR 1:2)
		protected decimal riskPerTrade = 0.02m;    // 한 트레이드당 자본의 몇 %만 리스크

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseEma(12, 26);
			chartPack.UseAtr((int)atrPeriod);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// EMA12가 EMA26 위로 돌파(골든크로스)
			if (c2.Ema1 < c2.Ema2 && c1.Ema1 > c1.Ema2)
			{
				var entryPrice = c0.Quote.Open;
				var atr = c0.Atr ?? 0;
				var stopLossPrice = entryPrice - atr * atrMultiplier;
				var takeProfitPrice = entryPrice + (entryPrice - stopLossPrice) * tprate;

				EntryPositionOnlySize(PositionSide.Long, c0, entryPrice, EstimatedMoney, stopLossPrice, takeProfitPrice);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			// 손절
			if (c1.Quote.Low <= longPosition.StopLossPrice)
			{
				ExitPosition(longPosition, c0, longPosition.StopLossPrice);
				return;
			}
			// 익절
			if (c1.Quote.High >= longPosition.TakeProfitPrice)
			{
				ExitPosition(longPosition, c0, longPosition.TakeProfitPrice);
				return;
			}
			// EMA12가 EMA26 아래로 데드크로스 시 강제 청산
			if (c1.Ema1 < c1.Ema2 && charts[i - 2].Ema1 >= charts[i - 2].Ema2)
			{
				ExitPosition(longPosition, c0, c0.Quote.Open);
				return;
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// EMA12가 EMA26 아래로 돌파(데드크로스)
			if (c2.Ema1 > c2.Ema2 && c1.Ema1 < c1.Ema2)
			{
				var entryPrice = c0.Quote.Open;
				var atr = c0.Atr ?? 0;
				var stopLossPrice = entryPrice + atr * atrMultiplier;
				var takeProfitPrice = entryPrice - (stopLossPrice - entryPrice) * tprate;

				EntryPositionOnlySize(PositionSide.Short, c0, entryPrice, EstimatedMoney, stopLossPrice, takeProfitPrice);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			// 손절
			if (c1.Quote.High >= shortPosition.StopLossPrice)
			{
				ExitPosition(shortPosition, c0, shortPosition.StopLossPrice);
				return;
			}
			// 익절
			if (c1.Quote.Low <= shortPosition.TakeProfitPrice)
			{
				ExitPosition(shortPosition, c0, shortPosition.TakeProfitPrice);
				return;
			}
			// EMA12가 EMA26 위로 골든크로스 시 강제 청산
			if (c1.Ema1 > c1.Ema2 && charts[i - 2].Ema1 <= charts[i - 2].Ema2)
			{
				ExitPosition(shortPosition, c0, c0.Quote.Open);
				return;
			}
		}
	}
}
