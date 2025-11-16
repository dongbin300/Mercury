using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	public class MarketAdaptive2 : Backtester
	{
		//public int SmaPeriod = 50;
		public int RsiLongThreshold = 38;
		public double StopLossAtrMultiplier = 0.8;
		public double NewStopLossAtrMultiplier = 0.5;
		public int MaxHoldBars = 10;


		public MarketAdaptive2(string reportFileName, decimal startMoney, int leverage,
			MaxActiveDealsType maxActiveDealsType, int maxActiveDeals)
			: base(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
		{
		}

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			//chartPack.UseSma(SmaPeriod);
			chartPack.UseAtr(14);
			chartPack.UseRsi(14);
			chartPack.UseBollingerBands(20, 2, Extensions.QuoteType.Close);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (c1.Bb1Sma == null || c1.Rsi1 == null || c1.Atr == null)
			{
				return;
			}

			if (c1.Rsi1 < RsiLongThreshold) // 35 -> 38 완화
			{
				var atr = (decimal)(c1.Atr ?? 0);

				decimal entryPrice = c0.Quote.Open;
				decimal stopLoss = entryPrice - atr * (decimal)StopLossAtrMultiplier; // 1.0 -> 0.8 완화
				decimal takeProfit = (decimal)c1.Bb1Sma;

				if (entryPrice >= takeProfit)
				{
					return;
				}

				// 포지션 사이즈는 고정으로할지 복리로할지 테스트 더 필요함
				EntryPositionOnlySize(PositionSide.Long, charts[i], entryPrice, Seed / MaxActiveDeals, stopLoss, takeProfit);
				//EntryPosition(PositionSide.Long, charts[i], entryPrice, stopLoss, takeProfit);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			// 손절 조건
			if (c1.Quote.Low <= longPosition.StopLossPrice)
			{
				ExitPosition(longPosition, c0, longPosition.StopLossPrice);
				return;
			}

			// 익절 조건
			if (c1.Quote.High >= longPosition.TakeProfitPrice)
			{
				ExitPosition(longPosition, c0, longPosition.TakeProfitPrice);
				return;
			}

			// 트레일링 스톱 (ATR 기반)
			var currentAtr = (decimal)(c1.Atr ?? 0);
			// 진입가보다 충분히 올랐을 때만 손절가를 올림
			if (c1.Quote.Close > longPosition.EntryPrice + currentAtr * (decimal)NewStopLossAtrMultiplier)
			{
				decimal newStop = c1.Quote.Close - currentAtr * (decimal)NewStopLossAtrMultiplier;
				if (newStop > longPosition.StopLossPrice)
				{
					longPosition.StopLossPrice = newStop;
				}
			}

			//decimal newStop = longPosition.EntryPrice + currentAtr * (decimal)NewStopLossAtrMultiplier;
			//if (newStop > longPosition.StopLossPrice)
			//{
			//	longPosition.StopLossPrice = newStop;
			//}

			// 최대 보유 기간 (10봉으로 단축)
			DateTime entryTime = longPosition.Time;
			int entryIdx = charts.FindIndex(x => x.Quote.Date == entryTime);
			if (entryIdx >= 0 && i - entryIdx >= MaxHoldBars)
			{
				ExitPosition(longPosition, c0, c1.Quote.Close);
				return;
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i) { }
		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition) { }
	}
}
