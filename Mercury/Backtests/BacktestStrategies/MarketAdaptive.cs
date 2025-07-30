using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
	public class MarketAdaptive : Backtester
	{
		public MarketAdaptive(string reportFileName, decimal startMoney, int leverage,
			MaxActiveDealsType maxActiveDealsType, int maxActiveDeals)
			: base(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
		{
		}

		public enum MarketRegime { Trend, Range }


		/// <summary>
		/// Market Regime Detection
		/// </summary>
		/// <param name="charts"></param>
		/// <param name="idx"></param>
		/// <param name="maLen"></param>
		/// <param name="baseSlopeSens"></param>
		/// <param name="baseAtrRangeThresh"></param>
		/// <returns></returns>
		public MarketRegime DetectMarketRegime(IList<ChartInfo> charts, int idx,
			int maLen = 50, decimal baseSlopeSens = 0.0005m, decimal baseAtrRangeThresh = 0.18m)
		{
			if (idx < maLen)
			{
				return MarketRegime.Range;
			}

			//decimal maNow = charts.Skip(idx - maLen + 1).Take(maLen).Average(x => x.Quote.Close);
			//decimal maPrev = charts.Skip(idx - maLen).Take(maLen).Average(x => x.Quote.Close);
			var maNow = charts[idx].Sma1 ?? 0;
			var maPrev = charts[idx - 1].Sma1 ?? 0;

			// 이동평균선의 변화율(기울기)
			var slope = Math.Abs((maNow - maPrev) / maPrev);

			//decimal hi = charts.Skip(idx - maLen + 1).Take(maLen).Max(x => x.Quote.High);
			//decimal lo = charts.Skip(idx - maLen + 1).Take(maLen).Min(x => x.Quote.Low);
			var hi = GetMaxPrice(charts, maLen, idx);
			var lo = GetMinPrice(charts, maLen, idx);

			var priceRange = hi - lo;
			var atr = charts[idx].Atr ?? 0;

			// 현재 변동성의 크기
			var atrRangeRatio = priceRange > 0 ? atr / priceRange : 0;

			// 이동평균선 기울기 임계값을 변동성에 따라 자동으로 조정
			// → 변동성이 작을수록(atrRangeRatio↓) 임계값이 커짐(덜 민감해짐)
			// → 변동성이 클수록(atrRangeRatio↑) 임계값이 작아짐(더 민감해짐)
			var dynamicSlopeSens = baseSlopeSens * (1 + (1 - atrRangeRatio));

			// ATR/Range 기준값을 변동성에 따라 조정
			// → 변동성이 클수록 임계값이 커짐
			var dynamicAtrRangeThresh = baseAtrRangeThresh * (1 + atrRangeRatio);

			// 이동평균선의 기울기(slope)가 동적으로 조정된 임계값(dynamicSlopeSens)보다 크고,
			// 현재 변동성의 크기도 동적 임계값(dynamicAtrRangeThresh)보다 크면
			if (slope > dynamicSlopeSens && atrRangeRatio > dynamicAtrRangeThresh)
			{
				return MarketRegime.Trend;
			}
			else
			{
				return MarketRegime.Range;
			}
		}

		protected override void InitIndicator(ChartPack chartPack, params decimal[] p)
		{
			chartPack.UseSma(50);
			chartPack.UseEma(50, 200);
			chartPack.UseAtr(14);
			chartPack.UseRsi(14);
			chartPack.UseBollingerBands(20, 2, Extensions.QuoteType.Close);
		}

		protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
		{
			// 1. 강한 하락장 필터링
			//if (IsStrongDowntrend(charts, i)) return;

			// 2. 거래 시간 필터 (뉴욕 세션: UTC 13-22시)
			//int hour = charts[i].Quote.Date.Hour;
			//if (hour < 13 || hour >= 22) return;

			// 3. 변동성 필터 (ATR이 50봉 평균보다 30% 이상 낮으면 스킵)
			//double avgAtr50 = charts.Skip(i - 50).Take(50).Average(c => c.Atr.Value);
			//if (charts[i].Atr < avgAtr50 * 0.7) return;

			//var regime = DetectMarketRegime(charts, i);
			//if (regime == MarketRegime.Trend)
			//{
			//	EnterTrendMarket(charts, i);
			//}
			//else
			{
				EnterRangeMarket(charts, i);
			}
		}

		private bool IsStrongDowntrend(List<ChartInfo> charts, int i)
		{
			if (charts[i].Ema2 == null || charts[i].Rsi1 == null) return false;
			return charts[i].Quote.Close < charts[i].Ema2 * 0.97m &&
				   charts[i].Rsi1 < 35;
		}

		private void EnterTrendMarket(List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c1.Rsi1 == null || c1.Atr == null)
			{
				return;
			}

			// 조건 단순화: 핵심 3가지 조건만 사용
			bool structureBreak = DetectStructureBreak(charts, i);
			(int orderBlockIndex, bool orderBlockRetest) = DetectOrderBlock(charts, i);
			bool rsiCondition = DetectRSICondition(charts, i);

			// 거래량 확인 (기존 대비 20% 완화)
			var avgAtr = charts.Skip(i - 20).Take(20).Average(c => c.Atr);
			bool volumeConfirmation = c1.Quote.Volume > charts.Skip(i - 20)
				.Take(20).Average(x => x.Quote.Volume) * (1 + avgAtr * 0.08m);

			if (structureBreak && orderBlockRetest && rsiCondition && volumeConfirmation)
			{
				var atr = c1.Atr.Value;
				decimal entryPrice = c0.Quote.Open;

				decimal stopLoss = entryPrice - (atr * 1.5m);
				decimal takeProfit = entryPrice + (atr * 3m);

				// 포지션 사이즈는 고정으로할지 복리로할지 테스트 더 필요함
				EntryPositionSize(PositionSide.Long, charts[i], entryPrice, Seed / MaxActiveDeals, stopLoss, takeProfit);
				//EntryPosition(PositionSide.Long, charts[i], entryPrice, stopLoss, takeProfit);
			}
		}

		private void EnterRangeMarket(List<ChartInfo> charts, int i)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];
			var c2 = charts[i - 2];

			if (c1.Bb1Lower == null || c1.Rsi1 == null)
			{
				return;
			}

			bool lowerTouch = c1.Quote.Close < (decimal)c1.Bb1Lower;
			bool rsiLow = c1.Rsi1 < 38; // 35 -> 38 완화
			var avgVol = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
			bool volSpike = c1.Quote.Volume > avgVol * 1.1m; // 1.2 -> 1.1 완화

			// 트렌드 조건은 아직 넣는게 좋을지 좀 더 테스트가 필요함
			// 볼밴하단 터치 + RSI 저점 + 거래량 스파이크가 모두 충족되면 진입
			// 
			if (lowerTouch && rsiLow && volSpike && !IsStrongDowntrend(charts, i))
			{
				var atr = c1.Atr ?? 0;

				decimal entryPrice = c0.Quote.Open;
				decimal stopLoss = entryPrice - (atr * 0.8m); // 1.0 -> 0.8 완화
				var takeProfit = c1.Bb1Sma;

				// 포지션 사이즈는 고정으로할지 복리로할지 테스트 더 필요함
				EntryPositionSize(PositionSide.Long, charts[i], entryPrice, Seed / MaxActiveDeals, stopLoss, takeProfit);
				//EntryPosition(PositionSide.Long, charts[i], entryPrice, stopLoss, takeProfit);
			}
		}

		private bool DetectStructureBreak(List<ChartInfo> charts, int i)
		{
			bool structureBreak = false;
			int lastLowIndex = -1;

			for (int j = i - 20; j < i - 3; j++)
			{
				if (j < 0) continue;

				bool isLocalLow = true;
				for (int k = j - 2; k <= j + 2; k++)
				{
					if (k == j || k < 0 || k >= charts.Count) continue;
					if (charts[k].Quote.Low < charts[j].Quote.Low)
					{
						isLocalLow = false;
						break;
					}
				}

				if (isLocalLow)
				{
					if (lastLowIndex != -1)
					{
						var atr = charts[j].Atr ?? 0;
						if (charts[j].Quote.Low < charts[lastLowIndex].Quote.Low - (atr * 0.5m))
							structureBreak = true;
					}
					lastLowIndex = j;
				}
			}
			return structureBreak;
		}

		private (int index, bool retest) DetectOrderBlock(List<ChartInfo> charts, int i)
		{
			int orderBlockIndex = -1;
			for (int j = i - 15; j < i - 2; j++)
			{
				if (j < 0) continue;

				var atr = charts[j].Atr ?? 0;
				decimal body = Math.Abs(charts[j].Quote.Close - charts[j].Quote.Open);
				bool strongMomentum = body > (atr * 0.25m); // 0.3 -> 0.25로 완화
				bool largeBody = body > charts.Skip(i - 20).Take(20)
					.Average(c => Math.Abs(c.Quote.Close - c.Quote.Open)) * 1.3m; // 1.5 -> 1.3 완화

				if (strongMomentum || largeBody)
				{
					orderBlockIndex = j;
					break;
				}
			}

			bool orderBlockRetest = false;
			if (orderBlockIndex != -1)
			{
				decimal obLow = charts[orderBlockIndex].Quote.Low;
				decimal obHigh = charts[orderBlockIndex].Quote.High;
				var atr = charts[orderBlockIndex].Atr ?? 0;

				for (int j = orderBlockIndex + 1; j < i; j++)
				{
					if (j >= charts.Count) break;

					decimal buffer = atr * 0.6m; // 0.5 -> 0.6 완화
					if (charts[j].Quote.Low < obHigh + buffer &&
						charts[j].Quote.High > obLow - buffer)
					{
						orderBlockRetest = true;
						break;
					}
				}
			}
			return (orderBlockIndex, orderBlockRetest);
		}

		private bool DetectRSICondition(List<ChartInfo> charts, int i)
		{
			bool rsiDivergence = false;
			bool rsiOversold = charts[i].Rsi1 < 42; // 40 -> 42 완화

			// 구조적 저점 찾기
			int lastLowIndex = -1;
			for (int j = i - 15; j < i; j++)
			{
				if (j < 0) continue;

				bool isLocalLow = true;
				for (int k = j - 2; k <= j + 2; k++)
				{
					if (k < 0 || k >= charts.Count) continue;
					if (k != j && charts[k].Quote.Low < charts[j].Quote.Low)
					{
						isLocalLow = false;
						break;
					}
				}
				if (isLocalLow) lastLowIndex = j;
			}

			if (lastLowIndex != -1 && i - lastLowIndex <= 15)
			{
				if (charts[i - 1].Quote.Low < charts[lastLowIndex].Quote.Low &&
					charts[i - 1].Rsi1 > charts[lastLowIndex].Rsi1)
					rsiDivergence = true;
			}

			return rsiDivergence || rsiOversold;
		}

		protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
		{
			var c0 = charts[i];
			var c1 = charts[i - 1];

			// 손절 조건
			if (c1.Quote.Low <= longPosition.StopLossPrice)
			{
				ExitPosition(longPosition, c0, longPosition.StopLossPrice);
				return;
			}

			// 익절 조건
			if (c1.Quote.High >= longPosition.TakeProfitPrice)
			{
				ExitPosition(longPosition, c0, longPosition.TakeProfitPrice);
				return;
			}

			// 트레일링 스톱 (ATR 기반)
			decimal currentAtr = c1.Atr ?? 0;
			decimal newStop = Math.Max(longPosition.StopLossPrice, c1.Quote.Close - currentAtr * 0.5m);
			if (newStop > longPosition.StopLossPrice)
				longPosition.StopLossPrice = newStop;

			// 최대 보유 기간 (10봉으로 단축)
			int maxHoldBars = 10;
			DateTime entryTime = longPosition.Time;
			int entryIdx = charts.FindIndex(x => x.Quote.Date == entryTime);
			if (entryIdx >= 0 && i - entryIdx >= maxHoldBars)
			{
				ExitPosition(longPosition, c0, c1.Quote.Close);
				return;
			}
		}

		protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i) { }
		protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition) { }
	}
}
