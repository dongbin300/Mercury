using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Ci24
	/// CCI와 일목균형표 조합 전략 (생존版)
	/// Cci2.cs 기반으로 안정성 최우선 설계
	/// MDD 30% 이하, 수익 안정화 목표
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class Ci24(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 32; // 최고 성능 기준
		public decimal Deviation = 2.8m;
		public int IchimokuTenkanPeriod = 9;
		public int IchimokuKijunPeriod = 26;
		public int IchimokuSenkouBPeriod = 52;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(CciPeriod);
			chartPack.UseBollingerBands(CciPeriod, (double)Deviation, Extensions.IndicatorType.Cci);
			chartPack.UseIchimokuCloud(IchimokuTenkanPeriod, IchimokuKijunPeriod, IchimokuSenkouBPeriod);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// Cci2.cs 스타일: CCI BB 하단 돌파 (기본 진입)
			if (c2.Cci < c2.Bb1Lower && c1.Cci > c1.Bb1Lower)
			{
				var entry = c1.Quote.Close;

				// 클라우드 위치 필터 (너무 아래에 있지 않을 때만)
				if (!(c1.Quote.Close < c1.IcLeadingSpan1 * 0.92m && c1.Quote.Close < c1.IcLeadingSpan2 * 0.92m))
				{
					EntryPosition(PositionSide.Long, c1, entry);
				}
			}

			// 추가 진입: CCI 과매도 반전 + 클라우드 위
			else if (c2.Cci < -100 && c1.Cci > -100 && c1.Quote.Close > c1.IcLeadingSpan1 && c1.Quote.Close > c1.IcLeadingSpan2)
			{
				var entry = c1.Quote.Close;
				EntryPosition(PositionSide.Long, c1, entry);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c1 = charts[i - 1];

			// 첫 번째 익실: CCI BB 상단 도달 시 50% 익실
			if (longPosition.Stage == 0 && c1.Cci > c1.Bb1Upper)
			{
				TakeProfitHalf(longPosition, c1.Quote.Close);
				return;
			}

			// 두 번째 익실: CCI 하락 반전 시 나머지 익실
			else if (longPosition.Stage == 1 && c1.Cci < c1.Bb1Upper)
			{
				TakeProfitHalf2(longPosition, c1);
				return;
			}

			// 강력한 손절 조건 (MDD 제어):
			// 1. 클라우드 하단 크게 이탈
			// 2. 5% 고정 손절
			// 3. CCI가 극단적 과매도
			if (c1.Quote.Close < c1.IcLeadingSpan1 * 0.90m && c1.Quote.Close < c1.IcLeadingSpan2 * 0.90m ||
				c1.Quote.Close <= longPosition.EntryPrice * 0.95m ||
				c1.Cci < -200)
			{
				ExitPosition(longPosition, c1, c1.Quote.Close);
				return;
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// Cci2.cs 스타일: CCI BB 상단 돌파 (기본 진입)
			if (c2.Cci > c2.Bb1Upper && c1.Cci < c1.Bb1Upper)
			{
				var entry = c1.Quote.Close;

				// 클라우드 위치 필터 (너무 위에 있지 않을 때만)
				if (!(c1.Quote.Close > c1.IcLeadingSpan1 * 1.08m && c1.Quote.Close > c1.IcLeadingSpan2 * 1.08m))
				{
					EntryPosition(PositionSide.Short, c1, entry);
				}
			}

			// 추가 진입: CCI 과매수 반전 + 클라우드 아래
			else if (c2.Cci > 100 && c1.Cci < 100 && c1.Quote.Close < c1.IcLeadingSpan1 && c1.Quote.Close < c1.IcLeadingSpan2)
			{
				var entry = c1.Quote.Close;
				EntryPosition(PositionSide.Short, c1, entry);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c1 = charts[i - 1];

			// 첫 번째 익실: CCI BB 하단 도달 시 50% 익실
			if (shortPosition.Stage == 0 && c1.Cci < c1.Bb1Lower)
			{
				TakeProfitHalf(shortPosition, c1.Quote.Close);
				return;
			}

			// 두 번째 익실: CCI 상승 반전 시 나머지 익실
			else if (shortPosition.Stage == 1 && c1.Cci > c1.Bb1Lower)
			{
				TakeProfitHalf2(shortPosition, c1);
				return;
			}

			// 강력한 손절 조건 (MDD 제어):
			// 1. 클라우드 상단 크게 이탈
			// 2. 5% 고정 손절
			// 3. CCI가 극단적 과매수
			if (c1.Quote.Close > c1.IcLeadingSpan1 * 1.10m && c1.Quote.Close > c1.IcLeadingSpan2 * 1.10m ||
				c1.Quote.Close >= shortPosition.EntryPrice * 1.05m ||
				c1.Cci > 200)
			{
				ExitPosition(shortPosition, c1, c1.Quote.Close);
				return;
			}
		}
	}
}