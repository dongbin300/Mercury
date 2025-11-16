using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci7 - Back to Simplicity with Volume Filter
	/// 
	/// Cci4의 검증된 로직 + 볼륨 필터링으로 신호 품질 향상
	/// 진입: CCI 극값 반전 + 거래량 급증 확인
	/// 청산: 제로라인 복귀 (단순화)
	/// 
	/// </summary>
	public class Cci7(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 16;
		public decimal ExtremeLevelHigh = 150m;
		public decimal ExtremeLevelLow = -150m;
		public decimal VolumeMultiplier = 1.5m;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(CciPeriod);
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

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 2) return;
			
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c2.Cci <= ExtremeLevelLow && 
				c1.Cci > c2.Cci && 
				c1.Cci > ExtremeLevelLow)
			{
				if (HasVolumeSpike(charts, i))
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
			if (i < 2) return;
			
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c2.Cci >= ExtremeLevelHigh && 
				c1.Cci < c2.Cci && 
				c1.Cci < ExtremeLevelHigh)
			{
				if (HasVolumeSpike(charts, i))
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