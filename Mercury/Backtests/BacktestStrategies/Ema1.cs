using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// 추세 유지(25candle연속 유지)중에 ema(50) cross(cross candle은 거래량이 평균 이하여야함) 후 10 candle 이내 recross 시 entry
	/// sl은 20 candle min price 혹은 recross candle low price
	/// tp는 40 candle max price 절반 st(10,1.5) < 0
	/// 
	/// Stage 0:
	/// 25 candle 연속 유지 확인
	/// 
	/// Stage 1:
	/// ema(50) cross + cross candle 거래량 평균이하 확인
	/// 
	/// Stage 2:
	/// 10 candle 이내 recross 확인 후 entry
	/// 
	/// 
	///		
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class Ema1(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int LStage { get; set; }
		public int SStage { get; set; }
		public int LInnerCount { get; set; }
		public int SInnerCount { get; set; }

		public int EmaPeriod { get; set; } = 50;
		public int Stage0Count { get; set; } = 25;
		public int Stage2Count { get; set; } = 10;
		public int SlCount { get; set; } = 20;
		public int TpCount { get; set; } = 40;

		protected override void InitIndicator(ChartPack chartPack, params decimal[] p)
		{
			chartPack.UseEma(EmaPeriod);
			chartPack.UseSupertrend(10, 1.5);
			LStage = 0;
			SStage = 0;
			LInnerCount = 0;
			SInnerCount = 0;
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			switch (LStage)
			{
				case 0:
					if (LInnerCount >= Stage0Count)
					{
						LStage = 1;
						LInnerCount = 0;
					}
					else if (c1.Quote.Close > (decimal)c1.Ema1)
					{
						LInnerCount++;
					}
					else
					{
						LInnerCount = 0;
					}
					break;

				case 1:
					if (c2.Quote.Close > (decimal)c2.Ema1 && c1.Quote.Close < (decimal)c1.Ema1 &&
						c1.Quote.Volume < GetAverageVolume(charts, i - 2, Stage0Count)) // ema cross + low volume
					{
						LStage = 2;
					}
					break;

				case 2:
					if (LInnerCount > Stage2Count) // recross fail
					{
						LStage = 0;
						LInnerCount = 0;
					}
					else if (c2.Quote.Close < (decimal)c2.Ema1 && c1.Quote.Close > (decimal)c1.Ema1) // recross
					{
						var slPrice = GetMinPrice(charts, SlCount, i);
						//var slPrice = c1.Quote.Low;
						var tpPrice = GetMaxPrice(charts, TpCount, i);

						EntryPosition(PositionSide.Long, c0, c1.Quote.Close, slPrice, tpPrice);
					}
					else
					{
						LInnerCount++;
					}
					break;
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (longPosition.Stage == 0 && c1.Quote.Low <= longPosition.StopLossPrice)
			{
				StopLoss(longPosition, c1);
			}
			else if (longPosition.Stage == 0 && c1.Quote.High >= longPosition.TakeProfitPrice)
			{
				TakeProfitHalf(longPosition);
			}
			if (longPosition.Stage == 1 && c1.Supertrend1 < 0)
			{
				TakeProfitHalf2(longPosition, c0);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			switch (LStage)
			{
				case 0:
					if (SInnerCount >= Stage0Count)
					{
						SStage = 1;
						SInnerCount = 0;
					}
					else if (c1.Quote.Close < (decimal)c1.Ema1)
					{
						SInnerCount++;
					}
					else
					{
						SInnerCount = 0;
					}
					break;

				case 1:
					if (c2.Quote.Close < (decimal)c2.Ema1 && c1.Quote.Close > (decimal)c1.Ema1 &&
						c1.Quote.Volume < GetAverageVolume(charts, i - 2, Stage0Count)) // ema cross + low volume
					{
						SStage = 2;
					}
					break;

				case 2:
					if (SInnerCount > Stage2Count) // recross fail
					{
						SStage = 0;
						SInnerCount = 0;
					}
					else if (c2.Quote.Close > (decimal)c2.Ema1 && c1.Quote.Close < (decimal)c1.Ema1) // recross
					{
						var slPrice = GetMaxPrice(charts, SlCount, i);
						//var slPrice = c1.Quote.High;
						var tpPrice = GetMinPrice(charts, TpCount, i);

						EntryPosition(PositionSide.Short, c0, c1.Quote.Close, slPrice, tpPrice);
					}
					else
					{
						SInnerCount++;
					}
					break;
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (shortPosition.Stage == 0 && c1.Quote.High >= shortPosition.StopLossPrice)
			{
				StopLoss(shortPosition, c1);
			}
			else if (shortPosition.Stage == 0 && c1.Quote.Low <= shortPosition.TakeProfitPrice)
			{
				TakeProfitHalf(shortPosition);
			}
			if (shortPosition.Stage == 1 && c1.Supertrend1 > 0)
			{
				TakeProfitHalf2(shortPosition, c0);
			}
		}

		/// <summary>
		/// i 부터 이전 period candle volume들의 평균
		/// </summary>
		/// <param name="charts"></param>
		/// <param name="i"></param>
		/// <param name="period"></param>
		/// <returns></returns>
		decimal GetAverageVolume(List<ChartInfo> charts, int i, int period)
		{
			decimal sum = 0;
			for (int j = i - period + 1; j <= i; j++)
			{
				sum += charts[j].Quote.Volume;
			}
			return sum / period;
		}
	}
}
