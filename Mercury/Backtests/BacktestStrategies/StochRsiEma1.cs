using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// StochRSI + EMA
	///  롱
	///	 EMA 200 위
	///	 StochRSI K선이 20 상향돌파
	///	
	///	 숏
	///	
	///	 EMA 200 아래
	///	 StochRSI K선이 80 하향돌파
	///	
	///	 정리
	///	
	///	 손절 전저점
	///	 익절 손절비 1:2
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class StochRsiEma1(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public decimal sltprate = 2.0m;

		protected override void InitIndicator(ChartPack chartPack, params decimal[] p)
		{
			chartPack.UseEma(200);
			chartPack.UseStochasticRsi();
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c1.Quote.Close > c1.Ema1 && c2.StochasticRsiK < 20 && c1.StochasticRsiK > 20)
			{
				var entryPrice = c0.Quote.Open;
				var stopLossPrice = GetMinPrice(charts, 26, i);
				var takeProfitPrice = entryPrice + (entryPrice - stopLossPrice) * sltprate;

				EntryPosition(PositionSide.Long, c0, entryPrice, stopLossPrice, takeProfitPrice);
				//EntryPositionOnlySize(PositionSide.Long, c0, entryPrice, Seed, stopLossPrice, takeProfitPrice);
			}
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
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
		}
	}
}
