using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci13 - Pure Price Action Revolution
	/// 
	/// CCI 완전 포기! 순수 가격 액션으로 100점 돌파
	/// 진입: 강한 캔들 패턴 + 볼륨 확인
	/// 청산: 반대 패턴 출현 시
	/// 
	/// </summary>
	public class Cci13(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public decimal MinBodyPercent = 70m;
		public decimal VolumeMultiplier = 1.5m;
		public int LookbackPeriod = 3;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
		}

		private bool IsStrongBullCandle(ChartInfo chart)
		{
			var bodySize = chart.Quote.Close - chart.Quote.Open;
			var totalSize = chart.Quote.High - chart.Quote.Low;
			if (totalSize == 0) return false;
			
			var bodyPercent = (bodySize / totalSize) * 100;
			return chart.Quote.Close > chart.Quote.Open && bodyPercent >= MinBodyPercent;
		}

		private bool IsStrongBearCandle(ChartInfo chart)
		{
			var bodySize = chart.Quote.Open - chart.Quote.Close;
			var totalSize = chart.Quote.High - chart.Quote.Low;
			if (totalSize == 0) return false;
			
			var bodyPercent = (bodySize / totalSize) * 100;
			return chart.Quote.Close < chart.Quote.Open && bodyPercent >= MinBodyPercent;
		}

		private bool HasVolumeSpike(List<ChartInfo> charts, int index)
		{
			if (index < 5) return true;

			var currentVolume = charts[index - 1].Quote.Volume;
			var avgVolume = 0m;
			
			for (int i = 2; i <= 6; i++)
			{
				avgVolume += charts[index - i].Quote.Volume;
			}
			avgVolume /= 5;

			return currentVolume > avgVolume * VolumeMultiplier;
		}

		private bool IsNewHigh(List<ChartInfo> charts, int index)
		{
			if (index < LookbackPeriod + 1) return false;
			
			var currentHigh = charts[index - 1].Quote.High;
			for (int i = 2; i <= LookbackPeriod + 1; i++)
			{
				if (charts[index - i].Quote.High >= currentHigh)
					return false;
			}
			return true;
		}

		private bool IsNewLow(List<ChartInfo> charts, int index)
		{
			if (index < LookbackPeriod + 1) return false;
			
			var currentLow = charts[index - 1].Quote.Low;
			for (int i = 2; i <= LookbackPeriod + 1; i++)
			{
				if (charts[index - i].Quote.Low <= currentLow)
					return false;
			}
			return true;
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < LookbackPeriod + 2) return;
			
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (IsStrongBullCandle(c1) && 
				IsNewHigh(charts, i) &&
				HasVolumeSpike(charts, i))
			{
				var entry = c0.Quote.Open;
				EntryPosition(PositionSide.Long, c0, entry);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c1 = charts[i - 1];

			if (IsStrongBearCandle(c1))
			{
				var c0 = charts[i];
				ExitPosition(longPosition, c0, c0.Quote.Open);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < LookbackPeriod + 2) return;
			
			var c0 = charts[i];
			var c1 = charts[i - 1];

			if (IsStrongBearCandle(c1) && 
				IsNewLow(charts, i) &&
				HasVolumeSpike(charts, i))
			{
				var entry = c0.Quote.Open;
				EntryPosition(PositionSide.Short, c0, entry);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c1 = charts[i - 1];

			if (IsStrongBullCandle(c1))
			{
				var c0 = charts[i];
				ExitPosition(shortPosition, c0, c0.Quote.Open);
			}
		}
	}
}