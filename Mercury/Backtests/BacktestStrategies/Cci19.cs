using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Cci19 - Gradient Exit Strategy
	/// 
	/// 진입은 단순하게, 청산을 혁신적으로
	/// 진입: 검증된 CCI 극값 반전
	/// 청산: CCI 기울기 기반 적응형 청산
	/// 
	/// </summary>
	public class Cci19(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 15;
		public decimal ExtremeLevelHigh = 150m;
		public decimal ExtremeLevelLow = -150m;
		public decimal GradientThreshold = -10m;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(CciPeriod);
		}

		private decimal GetCciGradient(List<ChartInfo> charts, int index)
		{
			if (index < 3) return 0;
			
			var c1 = charts[index - 1];
			var c2 = charts[index - 2];
			var c3 = charts[index - 3];
			
			// 3캔들 평균 기울기
			var grad1 = c1.Cci - c2.Cci;
			var grad2 = c2.Cci - c3.Cci;
			
			return (grad1.Value + grad2.Value) / 2;
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 3) return;
			
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			if (c3.Cci <= ExtremeLevelLow && 
				c2.Cci > c3.Cci && 
				c1.Cci > c2.Cci)
			{
				var entry = c0.Quote.Open;
				EntryPosition(PositionSide.Long, c0, entry);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c1 = charts[i - 1];
			var gradient = GetCciGradient(charts, i);

			// CCI 상승 둔화 시 청산
			if (c1.Cci >= 0 && gradient <= GradientThreshold)
			{
				var c0 = charts[i];
				ExitPosition(longPosition, c0, c0.Quote.Open);
			}
			// 극값 도달 시 무조건 청산
			else if (c1.Cci >= ExtremeLevelHigh)
			{
				var c0 = charts[i];
				ExitPosition(longPosition, c0, c0.Quote.Open);
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			if (i < 3) return;
			
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			if (c3.Cci >= ExtremeLevelHigh && 
				c2.Cci < c3.Cci && 
				c1.Cci < c2.Cci)
			{
				var entry = c0.Quote.Open;
				EntryPosition(PositionSide.Short, c0, entry);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c1 = charts[i - 1];
			var gradient = GetCciGradient(charts, i);

			// CCI 하락 둔화 시 청산
			if (c1.Cci <= 0 && gradient >= -GradientThreshold)
			{
				var c0 = charts[i];
				ExitPosition(shortPosition, c0, c0.Quote.Open);
			}
			// 극값 도달 시 무조건 청산
			else if (c1.Cci <= ExtremeLevelLow)
			{
				var c0 = charts[i];
				ExitPosition(shortPosition, c0, c0.Quote.Open);
			}
		}
	}
}