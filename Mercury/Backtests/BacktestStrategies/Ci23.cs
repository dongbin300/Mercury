using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Ci23
	/// CCI와 일목균형표 조합 전략 (적극版)
	/// CCI 메인 신호 기반, IchimokuCloud는 보조 필터로만 활용
	/// 실제 거래 발생을 최우선으로 설계
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class Ci23(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 20; // 더 빠른 반응
		public int IchimokuTenkanPeriod = 9;
		public int IchimokuKijunPeriod = 26;
		public int IchimokuSenkouBPeriod = 52;

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkanPeriod, IchimokuKijunPeriod, IchimokuSenkouBPeriod);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// 기본 매수 조건: CCI 과매도 반전 (Cci2.cs 스타일)
			if (c2.Cci < -100 && c1.Cci > -100)
			{
				var entry = c1.Quote.Close;

				// 클라우드가 강력한 저항이 아닐 때만 진입
				if (!(c1.Quote.Close < c1.IcLeadingSpan1 * 0.95m && c1.Quote.Close < c1.IcLeadingSpan2 * 0.95m))
				{
					EntryPosition(PositionSide.Long, c1, entry);
				}
			}

			// 추가 매수: CCI 0선 돌파
			else if (c2.Cci < 0 && c1.Cci > 0 && c1.Quote.Close > c1.IcBase)
			{
				var entry = c1.Quote.Close;
				EntryPosition(PositionSide.Long, c1, entry);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// CCI 과매수 시 절반 익실
			if (longPosition.Stage == 0 && c1.Cci > 100)
			{
				TakeProfitHalf(longPosition, c1.Quote.Close);
				return;
			}

			// CCI 하락 반전 시 나머지 익실
			else if (longPosition.Stage == 1 && c1.Cci < c2.Cci)
			{
				TakeProfitHalf2(longPosition, c1);
				return;
			}

			// 손절: 강력한 클라우드 저항 or Base 아래
			if (c1.Quote.Close < c1.IcLeadingSpan1 && c1.Quote.Close < c1.IcLeadingSpan2 &&
				c1.Quote.Close < c1.IcBase)
			{
				ExitPosition(longPosition, c1, c1.Quote.Close);
				return;
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// 기본 매도 조건: CCI 과매수 반전 (Cci2.cs 스타일)
			if (c2.Cci > 100 && c1.Cci < 100)
			{
				var entry = c1.Quote.Close;

				// 클라우드가 강력한 지지가 아닐 때만 진입
				if (!(c1.Quote.Close > c1.IcLeadingSpan1 * 1.05m && c1.Quote.Close > c1.IcLeadingSpan2 * 1.05m))
				{
					EntryPosition(PositionSide.Short, c1, entry);
				}
			}

			// 추가 매도: CCI 0선 하락 돌파
			else if (c2.Cci > 0 && c1.Cci < 0 && c1.Quote.Close < c1.IcBase)
			{
				var entry = c1.Quote.Close;
				EntryPosition(PositionSide.Short, c1, entry);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// CCI 과매도 시 절반 익실
			if (shortPosition.Stage == 0 && c1.Cci < -100)
			{
				TakeProfitHalf(shortPosition, c1.Quote.Close);
				return;
			}

			// CCI 상승 반전 시 나머지 익실
			else if (shortPosition.Stage == 1 && c1.Cci > c2.Cci)
			{
				TakeProfitHalf2(shortPosition, c1);
				return;
			}

			// 손절: 강력한 클라우드 지지 or Base 위
			if (c1.Quote.Close > c1.IcLeadingSpan1 && c1.Quote.Close > c1.IcLeadingSpan2 &&
				c1.Quote.Close > c1.IcBase)
			{
				ExitPosition(shortPosition, c1, c1.Quote.Close);
				return;
			}
		}
	}
}