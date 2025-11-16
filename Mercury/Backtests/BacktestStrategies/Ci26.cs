using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	public class Ci26(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals)
	: Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 26;
		public int IchimokuTenkanPeriod = 9;
		public int IchimokuKijunPeriod = 26;
		public int IchimokuSenkouBPeriod = 52;
		public decimal AtrMultiplierStop = 3m; // ATR 기반 손절 배수

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkanPeriod, IchimokuKijunPeriod, IchimokuSenkouBPeriod);
			chartPack.UseAtr(14); // 보조: ATR 14
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// 기본 진입: CCI 과매도 반전 + 가격이 구름 위
			if (c2.Cci < -100 && c1.Cci > -100
				&& c1.Quote.Close > c1.IcLeadingSpan1 && c1.Quote.Close > c1.IcLeadingSpan2
				&& c1.Quote.Close > c1.IcConversion) // 전환선 위 조건 추가
			{
				var entry = c0.Quote.Open;
				// ATR 기반 스탑로스 계산
				var sl = c1.Quote.Close - c1.Atr * AtrMultiplierStop;
				EntryPosition(PositionSide.Long, c0, entry, sl);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// 1) 절반 익절: CCI > 100
			if (longPosition.Stage == 0 && c1.Cci > 100)
			{
				TakeProfitHalf(longPosition, c1.Quote.Close);
				return;
			}
			// 2) 나머지 익절: CCI 하락 반전
			else if (longPosition.Stage == 1 && c1.Cci < c2.Cci)
			{
				TakeProfitHalf2(longPosition, c1);
				return;
			}
			// 3) 손절: 가격이 구름 아래로 이탈
			if (c1.Quote.Close < c1.IcLeadingSpan1 && c1.Quote.Close < c1.IcLeadingSpan2)
			{
				ExitPosition(longPosition, c1, c1.Quote.Close);
				return;
			}
		}

		// Short 대칭
		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c2.Cci > 100 && c1.Cci < 100
				&& c1.Quote.Close < c1.IcLeadingSpan1 && c1.Quote.Close < c1.IcLeadingSpan2
				&& c1.Quote.Close < c1.IcConversion)
			{
				var entry = c0.Quote.Open;
				var sl = c1.Quote.Close + c1.Atr * AtrMultiplierStop;
				EntryPosition(PositionSide.Short, c0, entry, sl);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (shortPosition.Stage == 0 && c1.Cci < -100)
			{
				TakeProfitHalf(shortPosition, c1.Quote.Close);
				return;
			}
			else if (shortPosition.Stage == 1 && c1.Cci > c2.Cci)
			{
				TakeProfitHalf2(shortPosition, c1);
				return;
			}
			if (c1.Quote.Close > c1.IcLeadingSpan1 && c1.Quote.Close > c1.IcLeadingSpan2)
			{
				ExitPosition(shortPosition, c1, c1.Quote.Close);
				return;
			}
		}
	}

}
