using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;
using Mercury.Maths;

namespace Mercury.Backtests.BacktestStrategies
{
	/// <summary>
	/* 
	 *		1. 거래량 기반 추세 전환 전략
			핵심 아이디어:
			거래량이 증가하는데 가격이 하락하지 않으면 매수,
			거래량이 증가하는데 가격이 상승하지 않으면 매도
			
			구현 예시:
			최근 20봉 평균 거래량보다 30% 이상 거래량이 급증
			가격이 3일 연속 하락하지 않으면 매수(하락 멈춤 시그널)
			반대로, 거래량 급증 + 3일 연속 상승 멈추면 매도
			
			백테스트 포인트:
			진입/청산 시점, Win rate, MDD, 평균 수익률 등
			
			2. 이동평균선 돌파 분할매수/매도 전략
			핵심 아이디어:
			5일/15일/30일 이동평균선 돌파 시 분할매수, 이탈 시 분할매도
			
			구현 예시:
			5일선 돌파 시 1/3 매수
			15일선 돌파 시 1/3 추가 매수
			30일선 돌파 시 나머지 매수
			반대로, 각 이평선 이탈 시 분할매도
			
			백테스트 포인트:
			누적 수익률, 최대 낙폭, 거래 빈도 등
			
			3. 지지/저항 + 거래량 결합 전략
			핵심 아이디어:
			강한 지지/저항선에서 거래량이 동반된 돌파 또는 반전 시 진입
			
			구현 예시:
			볼륨 프로파일, OBV, VWAP 등으로 강한 지지/저항 구간 탐색
			해당 구간에서 거래량 급증 + 돌파 시 매수(지지선) 또는 매도(저항선)
			돌파 실패 시 반대 포지션

			백테스트 포인트:
			돌파 성공률, 평균 손익, 리스크-리워드 비율 등
			
			4. 멀티타임프레임 추세 확인 전략
			핵심 아이디어:
			1분/3분/30분/1시간봉을 동시에 활용해 진입 타이밍 최적화
			
			구현 예시:
			1시간봉이 상승 추세일 때, 3분봉/1분봉에서 단기 눌림목 매수
			1시간봉이 하락 추세일 때, 3분봉/1분봉에서 단기 반등 매도
			
			백테스트 포인트:
			진입 타이밍별 승률, 평균 보유 기간 등
			
			5. 손절(Stop Loss) 철저 전략
			핵심 아이디어:
			진입 후 손절 기준(이평선, 지지선 등) 명확히 설정, 손실 최소화
			
			구현 예시:
			진입가 대비 2~3% 손절
			5일선 이탈 시 무조건 청산
			
			백테스트 포인트:
			손절 빈도, 손실폭, 전체 전략 수익률에 미치는 영향 등
			
			6. 익절(분할매도) 전략
			핵심 아이디어:
			일정 구간마다 분할매도, 이익 실현
			
			구현 예시:
			5일선 이탈 시 1/3 매도, 15일선 이탈 시 추가 매도, 30일선 이탈 시 전량 청산
			
			백테스트 포인트:
			분할매도 vs. 일괄매도 전략 비교, 총 수익률, 리스크 등
	*/
	/// </summary>
	/// <param name="reportFileName"></param>
	/// <param name="startMoney"></param>
	/// <param name="leverage"></param>
	/// <param name="maxActiveDealsType"></param>
	/// <param name="maxActiveDeals"></param>
	public class SixRules1(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
	{
		// 2. 거래량 급증 기준
		public decimal VolumeSpikeRatio = 1.5m; // 1.5배 이상 급증
		// 3. 지지/저항선 lookback
		public int SupportResistanceLookback = 60;
		// 4. 손절/익절 비율
		public decimal StopLossRatio = 0.97m; // -3%
		public decimal TakeProfitRatio = 1.06m; // +6%


		protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
		{
			chartPack.UseSma(5, 15, 30);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];

			// 1. 거래량 급증 + 지지선 근처 조건
			if (IsVolumeSpike(charts, i, 5, VolumeSpikeRatio) && IsNearSupport(charts, i, SupportResistanceLookback, 0.05m))
			{
				// 2. 5일선 돌파 시 1차 매수 (stage 0)
				if (IsMaCross(charts, i, 5))
				{
					var entryPrice = c0.Quote.Open;
					var orderSize = BaseOrderSize * 0.33m;
					var stopLoss = entryPrice * StopLossRatio;
					var takeProfit = entryPrice * TakeProfitRatio;
					EntryPositionSize(PositionSide.Long, c0, entryPrice, orderSize, stopLoss, takeProfit);
				}
				// 3. 15일선 돌파 시 2차 매수 (stage 1)
				else if (IsMaCross(charts, i, 15))
				{
					var entryPrice = c0.Quote.Open;
					var orderSize = BaseOrderSize * 0.33m;
					EntryPositionSize(PositionSide.Long, c0, entryPrice, orderSize);
				}
				// 4. 30일선 돌파 시 3차 매수 (stage 2)
				else if (IsMaCross(charts, i, 30))
				{
					var entryPrice = c0.Quote.Open;
					var orderSize = BaseOrderSize * 0.34m;
					EntryPositionSize(PositionSide.Long, c0, entryPrice, orderSize);
				}
			}
		}


		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			// 1. 손절: 종가가 손절가 이하
			if (longPosition.Stage == 0 && c1.Quote.Close <= longPosition.StopLossPrice)
			{
				ExitPositionSize(longPosition, c0, c1.Quote.Close, longPosition.Quantity); // 전량 청산
			}
			// 2. 익절: 고가가 익절가 이상
			else if (longPosition.Stage == 0 && c1.Quote.High >= longPosition.TakeProfitPrice)
			{
				// 절반 청산 (분할 익절)
				decimal halfQty = Math.Round(longPosition.Quantity * 0.5m, 8); // 소수점 방지
				ExitPositionSize(longPosition, c0, longPosition.TakeProfitPrice, halfQty);
				longPosition.Stage = 1; // 다음 익절 단계로
			}
			// 3. 15일선 이탈 시 추가 익절
			else if (longPosition.Stage == 1 && c1.Sma2 != null && c1.Quote.Close < (decimal)c1.Sma2)
			{
				// 남은 절반 중 절반 청산 (4분의 1 남김)
				decimal halfQty = Math.Round(longPosition.Quantity * 0.5m, 8);
				ExitPositionSize(longPosition, c0, c1.Quote.Close, halfQty);
				longPosition.Stage = 2;
			}
			// 4. 30일선 이탈 시 전량 청산
			else if (longPosition.Stage == 2 && c1.Sma3 != null && c1.Quote.Close < (decimal)c1.Sma3)
			{
				ExitPositionSize(longPosition, c0, c1.Quote.Close, longPosition.Quantity); // 남은 전량 청산
			}
		}


		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
		{
		}

		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
		{
		}

		// 거래량 급증 체크
		private bool IsVolumeSpike(List<ChartInfo> charts, int currentIndex, int period, decimal spikeRatio)
		{
			int from = Math.Max(0, currentIndex - period + 1);
			decimal avg = 0;
			int cnt = 0;
			for (int i = from; i < currentIndex; i++)
			{
				avg += charts[i].Quote.Volume;
				cnt++;
			}
			if (cnt == 0) return false;
			avg /= cnt;
			return charts[currentIndex].Quote.Volume >= avg * spikeRatio;
		}

		// 이동평균선 돌파 체크
		private bool IsMaCross(List<ChartInfo> charts, int i, int period)
		{
			if (i < 1)
			{
				return false;
			}
			var prev = charts[i - 1];
			var curr = charts[i];
			var prevMa = period == 5 ? prev.Sma1 : period == 15 ? prev.Sma2 : prev.Sma3;
			var currMa = period == 5 ? curr.Sma1 : period == 15 ? curr.Sma2 : curr.Sma3;
			if (prevMa == null || currMa == null) return false;
			return prev.Quote.Close < (decimal)prevMa && curr.Quote.Close > (decimal)currMa;
		}

		// 지지선 근처 여부(최저점 대비 5% 이내)
		private bool IsNearSupport(List<ChartInfo> charts, int i, int lookback, decimal threshold)
		{
			int from = Math.Max(0, i - lookback + 1);
			decimal min = decimal.MaxValue;
			for (int j = from; j <= i; j++)
			{
				if (charts[j].Quote.Low < min)
					min = charts[j].Quote.Low;
			}
			decimal price = charts[i].Quote.Close;
			return (price - min) / min <= threshold;
		}
	}
}
