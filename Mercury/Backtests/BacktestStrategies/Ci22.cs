using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/// Ci22
	/// CCI와 일목균형표 조합 전략 (밸런스版)
	/// CCI를 주 지표로, IchimokuCloud를 부 지표로 활용
	/// 거래 빈도와 성과의 균형 최적화
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class Ci22(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		public int CciPeriod = 28; // 좀 더 민감하게
		public int IchimokuTenkanPeriod = 9; // 표준값 복귀
		public int IchimokuKijunPeriod = 26; // 표준값 복귀
		public int IchimokuSenkouBPeriod = 52; // 표준값 복귀
		public decimal CciOversoldLevel = -100; // 표준 과매도
		public decimal CciOverboughtLevel = 100; // 표준 과매수
		public decimal VolumeMultiplier = 0.8m; // 거래량 조건 완화

		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseCci(CciPeriod);
			chartPack.UseIchimokuCloud(IchimokuTenkanPeriod, IchimokuKijunPeriod, IchimokuSenkouBPeriod);
			chartPack.UseEma(9); // 빠른 추세 확인용
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// 단순화된 매수 조건:
			// 1. CCI가 과매도에서 상승 반전 (2봉 기반)
			// 2. 클라우드 위 또는 클라우드 돌파 중
			// 3. EMA(9)로 추세 확인
			if (c2.Cci < CciOversoldLevel && c1.Cci > CciOversoldLevel &&
				(c1.Quote.Close > c1.IcLeadingSpan1 || c1.Quote.Close > c1.IcLeadingSpan2) &&
				c1.Quote.Close > c1.Ema9)
			{
				var entry = c1.Quote.Close;
				EntryPosition(PositionSide.Long, c1, entry);
			}

			// 추가 매수 기회: CCI 반전 + 클라우드 근접
			else if (c2.Cci < -80 && c1.Cci > -80 && c1.Cci > c2.Cci &&
					 c1.Quote.Close > c1.IcLeadingSpan1 * 0.98m && // 클라우드 근접 허용
					 c1.Quote.Close > c1.Ema9)
			{
				var entry = c1.Quote.Close;
				EntryPosition(PositionSide.Long, c1, entry);
			}
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// 빠른 익실 전략:
			if (longPosition.Stage == 0)
			{
				// CCI 과매수 도달 시 절반 익실
				if (c1.Cci > CciOverboughtLevel)
				{
					TakeProfitHalf(longPosition, c1.Quote.Close);
					return;
				}
				// 8% 빠른 이익 실현
				else if (c1.Quote.Close >= longPosition.EntryPrice * 1.08m)
				{
					TakeProfitHalf(longPosition, c1.Quote.Close);
					return;
				}
			}
			else if (longPosition.Stage == 1)
			{
				// CCI 하락 반전 시 나머지 익실
				if (c1.Cci < c2.Cci && c1.Cci < 50) // 50 미만에서 반전 시
				{
					TakeProfitHalf2(longPosition, c1);
					return;
				}
				// EMA 아래로 떨어지면 청산
				if (c1.Quote.Close < c1.Ema9)
				{
					TakeProfitHalf2(longPosition, c1);
					return;
				}
			}

			// 손절 조건:
			// 1. 클라우드 아래로 떨어짐
			// 2. Ema 아래로 떨어짐
			// 3. 6% 고정 손절
			if (c1.Quote.Close < c1.IcLeadingSpan1 && c1.Quote.Close < c1.IcLeadingSpan2 ||
				c1.Quote.Close < c1.Ema9 ||
				c1.Quote.Close <= longPosition.EntryPrice * 0.94m)
			{
				ExitPosition(longPosition, c1, c1.Quote.Close);
				return;
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// 단순화된 매도 조건:
			// 1. CCI가 과매수에서 하락 반전 (2봉 기반)
			// 2. 클라우드 아래 또는 클라우드 돌파 중
			// 3. EMA(9)로 추세 확인
			if (c2.Cci > CciOverboughtLevel && c1.Cci < CciOverboughtLevel &&
				(c1.Quote.Close < c1.IcLeadingSpan1 || c1.Quote.Close < c1.IcLeadingSpan2) &&
				c1.Quote.Close < c1.Ema9)
			{
				var entry = c1.Quote.Close;
				EntryPosition(PositionSide.Short, c1, entry);
			}

			// 추가 매도 기회: CCI 반전 + 클라우드 근접
			else if (c2.Cci > 80 && c1.Cci < 80 && c1.Cci < c2.Cci &&
					 c1.Quote.Close < c1.IcLeadingSpan1 * 1.02m && // 클라우드 근접 허용
					 c1.Quote.Close < c1.Ema9)
			{
				var entry = c1.Quote.Close;
				EntryPosition(PositionSide.Short, c1, entry);
			}
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			// 빠른 익실 전략:
			if (shortPosition.Stage == 0)
			{
				// CCI 과매도 도달 시 절반 익실
				if (c1.Cci < CciOversoldLevel)
				{
					TakeProfitHalf(shortPosition, c1.Quote.Close);
					return;
				}
				// 8% 빠른 이익 실현
				else if (c1.Quote.Close <= shortPosition.EntryPrice * 0.92m)
				{
					TakeProfitHalf(shortPosition, c1.Quote.Close);
					return;
				}
			}
			else if (shortPosition.Stage == 1)
			{
				// CCI 상승 반전 시 나머지 익실
				if (c1.Cci > c2.Cci && c1.Cci > -50) // -50 초과에서 반전 시
				{
					TakeProfitHalf2(shortPosition, c1);
					return;
				}
				// EMA 위로 올라가면 청산
				if (c1.Quote.Close > c1.Ema9)
				{
					TakeProfitHalf2(shortPosition, c1);
					return;
				}
			}

			// 손절 조건:
			// 1. 클라우드 위로 올라감
			// 2. EMA 위로 올라감
			// 3. 6% 고정 손절
			if (c1.Quote.Close > c1.IcLeadingSpan1 && c1.Quote.Close > c1.IcLeadingSpan2 ||
				c1.Quote.Close > c1.Ema9 ||
				c1.Quote.Close >= shortPosition.EntryPrice * 1.06m)
			{
				ExitPosition(shortPosition, c1, c1.Quote.Close);
				return;
			}
		}
	}
}