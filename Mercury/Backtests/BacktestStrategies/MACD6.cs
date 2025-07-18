using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// 9-12-26 EMA-MACD scalping strategy
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class MACD6(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public decimal slrate = 0.003m;      // 손절: 진입가 기준 0.3%
		public decimal tprate = 0.0045m;     // 익절: 진입가 기준 0.45% (RR 1:1.5)
		public decimal divergenceThreshold = 0.0015m; // MACD 다이버전스 감지 임계값

		protected override void InitIndicator(ChartPack chartPack, params decimal[] p)
		{
			chartPack.UseEma(9, 12, 26);
			chartPack.UseMacd(12, 26, 9);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// MACD 0선 돌파(골든크로스) + 가격이 EMA12, EMA26 위
			if (c2.Macd < 0 && c1.Macd > 0
				&& c1.Quote.Close > c1.Ema2 && c1.Quote.Close > c1.Ema3)
			{
				var entryPrice = c0.Quote.Open;
				var stopLossPrice = entryPrice * (1 - slrate);
				var takeProfitPrice = entryPrice * (1 + tprate);

				EntryPosition(PositionSide.Long, c0, entryPrice, stopLossPrice, takeProfitPrice);
			}
			// 추가: 강세 다이버전스 신호 (옵션)
			// if (IsBullishDivergence(charts, i, divergenceThreshold)) { ... }
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (c1.Quote.Low <= longPosition.StopLossPrice)
			{
				ExitPosition(longPosition, c0, longPosition.StopLossPrice);
				return;
			}
			if (c1.Quote.High >= longPosition.TakeProfitPrice)
			{
				ExitPosition(longPosition, c0, longPosition.TakeProfitPrice);
				return;
			}
			// MACD 0선 아래로 데드크로스 시 강제 청산
			if (c1.Macd < 0 && charts[i - 2].Macd > 0)
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

			// MACD 0선 하락 돌파(데드크로스) + 가격이 EMA12, EMA26 아래
			if (c2.Macd > 0 && c1.Macd < 0
				&& c1.Quote.Close < c1.Ema2 && c1.Quote.Close < c1.Ema3)
			{
				var entryPrice = c0.Quote.Open;
				var stopLossPrice = entryPrice * (1 + slrate);
				var takeProfitPrice = entryPrice * (1 - tprate);

				EntryPosition(PositionSide.Short, c0, entryPrice, stopLossPrice, takeProfitPrice);
			}
			// 추가: 약세 다이버전스 신호 (옵션)
			// if (IsBearishDivergence(charts, i, divergenceThreshold)) { ... }
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (c1.Quote.High >= shortPosition.StopLossPrice)
			{
				ExitPosition(shortPosition, c0, shortPosition.StopLossPrice);
				return;
			}
			if (c1.Quote.Low <= shortPosition.TakeProfitPrice)
			{
				ExitPosition(shortPosition, c0, shortPosition.TakeProfitPrice);
				return;
			}
			// MACD 0선 위로 골든크로스 시 강제 청산
			if (c1.Macd > 0 && charts[i - 2].Macd < 0)
			{
				ExitPosition(shortPosition, c0, c0.Quote.Open);
				return;
			}
		}
	}
}
