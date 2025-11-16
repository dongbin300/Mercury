using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci11 - Double Extreme Confirmation Strategy
	/// 
	/// 100점 돌파를 위한 진입 정확도 극대화
	/// 진입: 연속 극값 확인 + 반전 패턴 (더블 바텀/탑)
	/// 청산: 검증된 제로라인 청산 (단순함 유지)
	/// 
	/// </summary>
	public class Cci11(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 15;
		public decimal ExtremeLevelHigh = 150m;
		public decimal ExtremeLevelLow = -150m;
		public int LookbackPeriod = 10;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(CciPeriod);
		}

		private bool HasDoubleExtreme(List<ChartInfo> charts, int index, decimal extremeLevel, bool isLow)
		{
			if (index < LookbackPeriod + 2) return false;

			int extremeCount = 0;
			for (int i = index - LookbackPeriod; i < index; i++)
			{
				if (isLow && charts[i].Cci <= extremeLevel)
					extremeCount++;
				else if (!isLow && charts[i].Cci >= extremeLevel)
					extremeCount++;
			}

			return extremeCount >= 2;
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < LookbackPeriod + 3) return;
			
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			if (c3.Cci <= ExtremeLevelLow && 
				c2.Cci > c3.Cci && 
				c1.Cci > c2.Cci)
			{
				if (HasDoubleExtreme(charts, i, ExtremeLevelLow, true))
				{
					var entry = c0.Quote.Open;
					EntryPosition(PositionSide.Long, c0, entry);
				}
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c1 = charts[i - 1];

			if (c1.Cci >= 0)
			{
				var c0 = charts[i];
				ExitPosition(longPosition, c0, c0.Quote.Open);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < LookbackPeriod + 3) return;
			
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			if (c3.Cci >= ExtremeLevelHigh && 
				c2.Cci < c3.Cci && 
				c1.Cci < c2.Cci)
			{
				if (HasDoubleExtreme(charts, i, ExtremeLevelHigh, false))
				{
					var entry = c0.Quote.Open;
					EntryPosition(PositionSide.Short, c0, entry);
				}
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c1 = charts[i - 1];

			if (c1.Cci <= 0)
			{
				var c0 = charts[i];
				ExitPosition(shortPosition, c0, c0.Quote.Open);
			}
		}
	}
}