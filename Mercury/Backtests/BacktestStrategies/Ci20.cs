using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Ci20
	/// CCI와 일목균형표 조합 전략
	/// CCI를 주 지표로, IchimokuCloud를 부 지표로 활용
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class Ci20(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 32;
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
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// CCI가 과매도 영역에서 상승 반전하고, 일목균형표 클라우드 위에 있을 때 매수
			if (c2.Cci < -100 && c1.Cci > -100 && c1.Quote.Close > c1.IcLeadingSpan1 && c1.Quote.Close > c1.IcLeadingSpan2)
			{
				var entry = c0.Quote.Open;
				EntryPosition(PositionSide.Long, c0, entry);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// CCI가 과매수 영역에 도달하면 절반 익실
			if (longPosition.Stage == 0 && c1.Cci > 100)
			{
				TakeProfitHalf(longPosition, c1.Quote.Close);
				return;
			}
			// CCI가 하락 반전하면 나머지 익실
			else if (longPosition.Stage == 1 && c1.Cci < c2.Cci)
			{
				TakeProfitHalf2(longPosition, c1);
				return;
			}

			// 일목균형표 클라우드 아래로 떨어지면 손절
			if (c1.Quote.Close < c1.IcLeadingSpan1 && c1.Quote.Close < c1.IcLeadingSpan2)
			{
				ExitPosition(longPosition, c1, c1.Quote.Close);
				return;
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// CCI가 과매수 영역에서 하락 반전하고, 일목균형표 클라우드 아래에 있을 때 매도
			if (c2.Cci > 100 && c1.Cci < 100 && c1.Quote.Close < c1.IcLeadingSpan1 && c1.Quote.Close < c1.IcLeadingSpan2)
			{
				var entry = c0.Quote.Open;
				EntryPosition(PositionSide.Short, c0, entry);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// CCI가 과매도 영역에 도달하면 절반 익실
			if (shortPosition.Stage == 0 && c1.Cci < -100)
			{
				TakeProfitHalf(shortPosition, c0.Quote.Open);
				return;
			}
			// CCI가 상승 반전하면 나머지 익실
			else if (shortPosition.Stage == 1 && c1.Cci > c2.Cci)
			{
				TakeProfitHalf2(shortPosition, c0);
				return;
			}

			// 일목균형표 클라우드 위로 올라가면 손절
			if (c1.Quote.Close > c1.IcLeadingSpan1 && c1.Quote.Close > c1.IcLeadingSpan2)
			{
				ExitPosition(shortPosition, c1, c1.Quote.Close);
				return;
			}
		}
	}
}