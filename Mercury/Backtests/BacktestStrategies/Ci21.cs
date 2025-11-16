using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Ci21
	/// CCI와 일목균형표 조합 전략 (개선版)
	/// CCI를 주 지표로, IchimokuCloud를 부 지표로 활용
	/// 백테스팅 결과 기반 개선: 추가 필터링 및 리스크 관리 강화
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class Ci21(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 32; // 최적값 기반
		public int IchimokuTenkanPeriod = 12; // 최적화: 9->12
		public int IchimokuKijunPeriod = 30; // 최적화: 26->30
		public int IchimokuSenkouBPeriod = 60; // 최적화: 52->60
		public decimal CciOversoldLevel = -120; // 더 보수적인 과매도
		public decimal CciOverboughtLevel = 120; // 더 보수적인 과매수
		public decimal VolumeMultiplier = 1.2m; // 거래량 필터

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkanPeriod, IchimokuKijunPeriod, IchimokuSenkouBPeriod);
			chartPack.UseSma(20); // 추가: 추방 필터용 SMA
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3]; // 추가 과거 데이터

			// 강화된 매수 조건:
			// 1. CCI가 심한 과매도(-120)에서 상승 반전
			// 2. 일목균형표 클라우드 위에 있음
			// 3. 가격이 SMA(20) 위 (추세 확인)
			// 4. 거래량이 평균보다 높음
			// 5. Tenkan이 Kijun 위 (모멘텀 확인)
			if (c3.Cci < CciOversoldLevel && c2.Cci < CciOversoldLevel && c1.Cci > CciOversoldLevel &&
				c1.Quote.Close > c1.IcLeadingSpan1 && c1.Quote.Close > c1.IcLeadingSpan2 &&
				c1.Quote.Close > c1.Sma1 &&
				c1.Quote.Volume > charts.Take(20).Average(x => x.Quote.Volume) * VolumeMultiplier &&
				c1.IcConversion > c1.IcBase)
			{
				var entry = c1.Quote.Close;
				EntryPosition(PositionSide.Long, c1, entry);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			// 동적 익실 전략:
			if (longPosition.Stage == 0)
			{
				// CCI가 과매수 도달 시 절반 익실
				if (c1.Cci > CciOverboughtLevel)
				{
					TakeProfitHalf(longPosition, c1.Quote.Close);
					return;
				}
				// 빠른 이익 실현 (10% 이익 시)
				else if (c1.Quote.Close >= longPosition.EntryPrice * 1.10m)
				{
					TakeProfitHalf(longPosition, c1.Quote.Close);
					return;
				}
			}
			else if (longPosition.Stage == 1)
			{
				// CCI 하락 반전 시 나머지 익실
				if (c1.Cci < c2.Cci && c2.Cci < c3.Cci)
				{
					TakeProfitHalf2(longPosition, c1);
					return;
				}
				// Tenkan이 Kijun 아래로 떨어지면 청산
				if (c1.IcConversion < c1.IcBase)
				{
					TakeProfitHalf2(longPosition, c1);
					return;
				}
			}

			// 강화된 손절 조건:
			// 1. 클라우드 아래로 떨어짐
			// 2. 가격이 SMA(20) 아래로 떨어짐
			// 3. 고정 손절 (5%)
			if ((c1.Quote.Close < c1.IcLeadingSpan1 && c1.Quote.Close < c1.IcLeadingSpan2) ||
				c1.Quote.Close < c1.Sma1 ||
				c1.Quote.Close <= longPosition.EntryPrice * 0.95m)
			{
				ExitPosition(longPosition, c1, c1.Quote.Close);
				return;
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			// 강화된 매도 조건:
			// 1. CCI가 심한 과매수(120)에서 하락 반전
			// 2. 일목균형표 클라우드 아래에 있음
			// 3. 가격이 SMA(20) 아래 (추세 확인)
			// 4. 거래량이 평균보다 높음
			// 5. Tenkan이 Kijun 아래 (모멘텀 확인)
			if (c3.Cci > CciOverboughtLevel && c2.Cci > CciOverboughtLevel && c1.Cci < CciOverboughtLevel &&
				c1.Quote.Close < c1.IcLeadingSpan1 && c1.Quote.Close < c1.IcLeadingSpan2 &&
				c1.Quote.Close < c1.Sma1 &&
				c1.Quote.Volume > charts.Take(20).Average(x => x.Quote.Volume) * VolumeMultiplier &&
				c1.IcConversion < c1.IcBase)
			{
				var entry = c1.Quote.Close;
				EntryPosition(PositionSide.Short, c1, entry);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];
			var c3 = charts[i - 3];

			// 동적 익실 전략:
			if (shortPosition.Stage == 0)
			{
				// CCI가 과매도 도달 시 절반 익실
				if (c1.Cci < CciOversoldLevel)
				{
					TakeProfitHalf(shortPosition, c1.Quote.Close);
					return;
				}
				// 빠른 이익 실현 (10% 이익 시)
				else if (c1.Quote.Close <= shortPosition.EntryPrice * 0.90m)
				{
					TakeProfitHalf(shortPosition, c1.Quote.Close);
					return;
				}
			}
			else if (shortPosition.Stage == 1)
			{
				// CCI 상승 반전 시 나머지 익실
				if (c1.Cci > c2.Cci && c2.Cci > c3.Cci)
				{
					TakeProfitHalf2(shortPosition, c1);
					return;
				}
				// Tenkan이 Kijun 위로 올라가면 청산
				if (c1.IcConversion > c1.IcBase)
				{
					TakeProfitHalf2(shortPosition, c1);
					return;
				}
			}

			// 강화된 손절 조건:
			// 1. 클라우드 위로 올라감
			// 2. 가격이 SMA(20) 위로 올라감
			// 3. 고정 손절 (5%)
			if ((c1.Quote.Close > c1.IcLeadingSpan1 && c1.Quote.Close > c1.IcLeadingSpan2) ||
				c1.Quote.Close > c1.Sma1 ||
				c1.Quote.Close >= shortPosition.EntryPrice * 1.05m)
			{
				ExitPosition(shortPosition, c1, c1.Quote.Close);
				return;
			}
		}
	}
}