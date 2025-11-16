using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Enums;

namespace Mercury.Backtests.BacktestStrategies
{
    /// <summary>
    /// Cci32 - High Frequency + Safety Balance (Improved)
    ///
    /// Cci31 베이스 + 백테스트 데이터 분석을 통한 개선
    ///
    /// === 핵심 목표 ===
    /// - 거래 횟수: 4년에 3000+ 거래 (현재 68건 → 3000+)
    /// - 승률: 75%+ 유지 (현재 88% → 75%+ 목표)
    /// - RPR: 80+ 유지 (현재 111 → 80+ 목표)
    /// - MDD: 10% 이하 (현재 2% → 10% 이하 허용)
    ///
    /// === 주요 개선사항 (Cci31 대비) ===
    /// 1. 진입 조건 강화: 불필요한 손실 거래 감소
    /// 2. 트렌드 필터 강화: 더 명확한 추세에서만 진입
    /// 3. CCI 범위 조정: CCI 신호의 민감도 조절
    ///
    /// </summary>
    public class Cci32(string reportFileName, decimal startMoney, int leverage, MaxActiveDealsType maxActiveDealsType, int maxActiveDeals) : Backtester(reportFileName, startMoney, leverage, maxActiveDealsType, maxActiveDeals)
    {
        // === CCI 기본 파라미터 (완화) ===
        public int CciPeriod = 14;
        public decimal ExtremeLevelHigh = 140m;      // 150 → 140 (더 엄격한 진입)
        public decimal ExtremeLevelLow = -140m;      // -150 → -140
        public decimal ExitBuffer = 0m;

        // === RSI 필터 파라미터 (강화) ===
        public int RsiPeriod = 14;
        public decimal RsiOverbought = 65m;          // 70 → 65
        public decimal RsiOversold = 35m;            // 30 → 35

        // === ADX 파라미터 추가 ===
        public int AdxPeriod = 14;
        public decimal AdxThreshold = 20m; // 추세 강도 임계값

        // === ATR 파라미터 추가 ===
        public int AtrPeriod = 14;
        public decimal AtrMultiplier = 1.0m; // 손절매/익절에 사용할 ATR 배수

        // === 성공한 청산 전략 유지 ===
        public decimal ScalpExitPercent = 0.8m;      // 80% 스캘핑 유지
        public decimal ScalpTarget = 1.0m;           // 2.0% → 1.0 (ATR 배수)
        public decimal PartialExitPercent = 0.2m;    // 나머지 20%
        public decimal ProfitTarget = 2.0m;          // 3.0% → 2.0 (ATR 배수)
        public decimal FullExitTarget = 3.0m;        // 5.0% → 3.0 (ATR 배수)

        // === 검증된 리스크 관리 유지 ===
        public decimal StopLossPercent = 1.0m;       // 1.0% 손절 유지
        public decimal TrailingPercent = 0.5m;       // 0.5% 트레일링 유지

        // === 연속 손실 관리 완화 ===
        private int consecutiveLosses = 0;
        private DateTime lastLossTime = DateTime.MinValue;
        private const int maxConsecutiveLosses = 4;  // 2 → 4 (더 관대)
        private const int cooldownMinutes = 30;      // 60 → 30 (더 짧은 휴식)

        protected override void InitIndicator(ChartPack chartPack, int intervalIndex, params decimal[] p)
        {
            UseDca = false;
            chartPack.UseCci(CciPeriod);
            chartPack.UseRsi(RsiPeriod);
            chartPack.UseAdx(AdxPeriod);
            chartPack.UseAtr(AtrPeriod);
        }

        private bool IsHighVolatility(List<ChartInfo> charts, int i)
        {
            if (i < 10) return false; // c1을 기준으로 최소 10개의 캔들이 필요
                                      // c1과 그 이전 9개의 캔들 (총 10개의 완성된 캔들)을 사용
            var recent = charts.Skip(i - 10).Take(10).ToList();
            var avgRange = recent.Average(c => (c.Quote.High - c.Quote.Low) / c.Quote.Close * 100);
            return avgRange > 1.5m; // 2.0 → 1.5 (더 자주 감지)
        }

        private decimal GetTrendStrength(List<ChartInfo> charts, int i)
        {
            if (i < 10) return 0; // c1을 기준으로 최소 10개의 캔들이 필요
                                  // c1과 그 이전 9개의 캔들 (총 10개의 완성된 캔들)을 사용
            var recent = charts.Skip(i - 10).Take(10).ToList();
            var firstPrice = recent.First().Quote.Close;
            var lastPrice = recent.Last().Quote.Close; // lastPrice will now be c1.Quote.Close
            return (lastPrice - firstPrice) / firstPrice * 100;
        }

        private bool IsVolumeConfirmed(List<ChartInfo> charts, int i)
        {
            if (i < 5) return false; // c1을 기준으로 최소 5개의 캔들이 필요
                                     // c1과 그 이전 4개의 캔들 (총 5개의 완성된 캔들)을 사용
            var recent = charts.Skip(i - 5).Take(5).ToList();
            var avgVolume = recent.Average(c => c.Quote.Volume);
            return charts[i - 1].Quote.Volume > avgVolume * 1.3m; // c1의 Volume 사용
        }

        private bool IsQuickEntry(List<ChartInfo> charts, int i)
        {
            if (i < 2) return false; // Need at least c1 and c2
            var c1 = charts[i - 1];
            var c2 = charts[i - 2];

            // 빠른 진입 조건 (모멘텀 기반)
            bool cciMoving = Math.Abs((decimal)(c1.Cci - c2.Cci)) > 10m; // c1과 c2의 CCI 변화
            bool priceMoving = Math.Abs(c1.Quote.Close - c2.Quote.Close) / c2.Quote.Close * 100 > 0.3m; // c1과 c2의 가격 변화

            return cciMoving && priceMoving;
        }

        		private bool IsModerateEntry(List<ChartInfo> charts, int i)
        		{
        			if (i < 3) return false; // Need at least c1, c2, c3
        			var c1 = charts[i - 1];
        			var c2 = charts[i - 2];
        			var c3 = charts[i - 3]; // New: c3
        
        			// 중간 강도 진입 조건
        			bool cciImproving = c2.Cci > c3.Cci && c1.Cci > c2.Cci; // c2와 c1의 CCI 개선
        			bool rsiOk = c1.Rsi1 >= 25m && c1.Rsi1 <= 75m; // 극단 아닌 구간
        
        			return cciImproving && rsiOk;
        		}
        
        		private bool IsBullishCandle(ChartInfo candle)
        		{
        			return candle.Quote.Close > candle.Quote.Open;
        		}
        
        		private bool IsBearishCandle(ChartInfo candle)
        		{
        			return candle.Quote.Close < candle.Quote.Open;
        		}
        
        		private bool IsInCooldown(DateTime currentTime)
        		{
        			if (consecutiveLosses >= maxConsecutiveLosses)
        			{
        				var timeSinceLastLoss = currentTime - lastLossTime;
        				return timeSinceLastLoss.TotalMinutes < cooldownMinutes;
        			}
        			return false;
        		}
        protected override void LongEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < 10) return;

            var c0 = charts[i];
            var c1 = charts[i - 1];
            var c2 = charts[i - 2];

            if (!c1.Atr.HasValue) return; // ATR 값이 없으면 진입하지 않음

            // 연속 손실 체크
            if (IsInCooldown(c0.DateTime)) return;

            var existingPosition = GetActivePosition(symbol, PositionSide.Long);
            if (existingPosition != null) return;

            var isHighVol = IsHighVolatility(charts, i);
            var trendStrength = GetTrendStrength(charts, i);
            var volumeConfirmed = IsVolumeConfirmed(charts, i);
            var quickEntry = IsQuickEntry(charts, i);
            var moderateEntry = IsModerateEntry(charts, i);

            // === 3가지 진입 경로 (OR 로직) ===

            // 1. 강한 CCI 신호 (기존 로직 완화)
            bool strongCci = c1.Cci <= ExtremeLevelLow && c1.Cci > c2.Cci; // 2단계로 완화
            			bool strongEntry = strongCci && c1.Rsi1 <= RsiOversold && volumeConfirmed && c1.Adx > AdxThreshold && trendStrength > 0 && IsBullishCandle(c1);
            
            			// 2. 중간 강도 진입 (볼륨 + 모멘텀)
            			bool mediumEntry = moderateEntry && volumeConfirmed &&
            							  trendStrength > 2.0m && trendStrength < 10.0m && c1.Adx > AdxThreshold && IsBullishCandle(c1);
            
            			// 3. 빠른 진입 (모멘텀 + 기본 조건)
            			bool fastEntry = quickEntry && c1.Cci <= -80m && c1.Rsi1 <= 45m && c1.Adx > AdxThreshold && trendStrength > 0 && IsBullishCandle(c1);
            // 하나라도 만족하면 진입
            if (strongEntry || mediumEntry || fastEntry)
            {
                var entry = c0.Quote.Open;
                var stopLoss = entry - (c1.Atr.Value * AtrMultiplier);
                DcaEntryPosition(PositionSide.Long, c0, entry, 0m, 1.0m, stopLoss);
            }
        }

        protected override void LongExit(string symbol, List<ChartInfo> charts, int i, Position longPosition)
        {
            var c0 = charts[i];
            var c1 = charts[i - 1];
            if (!c1.Atr.HasValue) return; // ATR 값이 없으면 손절/익절 계산 불가
            var profitPercent = GetPositionProfitPercent(longPosition, c0.Quote.Close);

            // 1. 검증된 손절 로직 유지 (Look-ahead bias 수정)
            if (longPosition.StopLossPrice > 0 && c0.Quote.Low <= longPosition.StopLossPrice)
            {
                decimal exitPrice = longPosition.StopLossPrice;
                if (c0.Quote.Open <= longPosition.StopLossPrice) // 캔들이 손절가보다 낮게 시작했으면 시가로 손절
                {
                    exitPrice = c0.Quote.Open;
                }
                DcaExitPosition(longPosition, c0, exitPrice, 1.0m);
                consecutiveLosses++;
                lastLossTime = c0.DateTime;
                return;
            }

            // 2. 검증된 스캘핑 모드 유지 (80% 청산) (Look-ahead bias 수정)
            var scalpPrice = longPosition.EntryPrice + (c1.Atr.Value * AtrMultiplier * ScalpTarget);
            if (longPosition.Stage == 0 && c0.Quote.High >= scalpPrice)
            {
                decimal exitPrice = scalpPrice;
                if (c0.Quote.Open >= scalpPrice) // 캔들이 익절가보다 높게 시작했으면 시가로 익절
                {
                    exitPrice = c0.Quote.Open;
                }
                DcaExitPosition(longPosition, c0, exitPrice, ScalpExitPercent);
                longPosition.Stage = 1;
                longPosition.StopLossPrice = longPosition.EntryPrice + (c1.Atr.Value * AtrMultiplier * 0.5m); // 0.5 ATR 수익 보장
                consecutiveLosses = 0; // 수익 거래시 초기화
            }

            // 3. 나머지 20% 부분 익절 (Look-ahead bias 수정)
            var partialPrice = longPosition.EntryPrice + (c1.Atr.Value * AtrMultiplier * ProfitTarget);
            if (longPosition.Stage == 1 && c0.Quote.High >= partialPrice)
            {
                decimal exitPrice = partialPrice;
                if (c0.Quote.Open >= partialPrice) // 캔들이 익절가보다 높게 시작했으면 시가로 익절
                {
                    exitPrice = c0.Quote.Open;
                }
                DcaExitPosition(longPosition, c0, exitPrice, PartialExitPercent);
                longPosition.Stage = 2;
                longPosition.StopLossPrice = longPosition.EntryPrice + (c1.Atr.Value * AtrMultiplier * 1.0m); // 1.0 ATR 수익 보장
            }

            // 4. 최종 전량 익절 (Look-ahead bias 수정)
            var fullExitPrice = longPosition.EntryPrice + (c1.Atr.Value * AtrMultiplier * FullExitTarget);
            if (c0.Quote.High >= fullExitPrice)
            {
                decimal exitPrice = fullExitPrice;
                if (c0.Quote.Open >= fullExitPrice) // 캔들이 익절가보다 높게 시작했으면 시가로 익절
                {
                    exitPrice = c0.Quote.Open;
                }
                DcaExitPosition(longPosition, c0, exitPrice, 1.0m);
                consecutiveLosses = 0;
                return;
            }

            // 5. 검증된 트레일링 스톱 유지
            if (profitPercent > TrailingPercent)
            {
                var newStopLoss = c0.Quote.High * (1 - TrailingPercent / 100);
                if (newStopLoss > longPosition.StopLossPrice)
                {
                    longPosition.StopLossPrice = newStopLoss;
                }
            }

            // 6. 조기 청산 (완화)
            bool earlyExit = c1.Cci >= 60m || c1.Rsi1 >= 65m; // 50 → 60, 60 → 65 (완화)
            bool lossExit = profitPercent < -0.5m; // 0.5% 손실시 조기 청산 유지

            if (earlyExit || lossExit)
            {
                DcaExitPosition(longPosition, c0, c0.Quote.Open, 1.0m);
                if (lossExit)
                {
                    consecutiveLosses++;
                    lastLossTime = c0.DateTime;
                }
                return;
            }
        }

        protected override void ShortEntry(string symbol, List<ChartInfo> charts, int i)
        {
            if (i < 10) return;

            var c0 = charts[i];
            var c1 = charts[i - 1];
            var c2 = charts[i - 2];

            if (!c1.Atr.HasValue) return; // ATR 값이 없으면 손절/익절 계산 불가

            // 연속 손실 체크
            if (IsInCooldown(c0.DateTime)) return;

            var existingPosition = GetActivePosition(symbol, PositionSide.Short);
            if (existingPosition != null) return;

            var isHighVol = IsHighVolatility(charts, i);
            var trendStrength = GetTrendStrength(charts, i);
            var volumeConfirmed = IsVolumeConfirmed(charts, i);
            var quickEntry = IsQuickEntry(charts, i);
            var moderateEntry = IsModerateEntry(charts, i);

            // === 3가지 진입 경로 (OR 로직) ===

            // 1. 강한 CCI 신호 (기존 로직 완화)
            bool strongCci = c1.Cci >= ExtremeLevelHigh && c1.Cci < c2.Cci; // 2단계로 완화
            			bool strongEntry = strongCci && c1.Rsi1 >= RsiOverbought && volumeConfirmed && c1.Adx > AdxThreshold && trendStrength < 0 && IsBearishCandle(c1);
            
            			// 2. 중간 강도 진입 (볼륨 + 모멘텀)
            			bool mediumEntry = moderateEntry && volumeConfirmed &&
            							  trendStrength < -2.0m && trendStrength > -10.0m && c1.Adx > AdxThreshold && IsBearishCandle(c1);
            
            			// 3. 빠른 진입 (모멘텀 + 기본 조건)
            			bool fastEntry = quickEntry && c1.Cci >= 80m && c1.Rsi1 >= 55m && c1.Adx > AdxThreshold && trendStrength < 0 && IsBearishCandle(c1);
            // 하나라도 만족하면 진입
            if (strongEntry || mediumEntry || fastEntry)
            {
                var entry = c0.Quote.Open;
                var stopLoss = entry + (c1.Atr.Value * AtrMultiplier);
                DcaEntryPosition(PositionSide.Short, c0, entry, 0m, 1.0m, stopLoss);
            }
        }

        protected override void ShortExit(string symbol, List<ChartInfo> charts, int i, Position shortPosition)
        {
            var c0 = charts[i];
            var c1 = charts[i - 1];
            if (!c1.Atr.HasValue) return; // ATR 값이 없으면 손절/익절 계산 불가
            var profitPercent = GetPositionProfitPercent(shortPosition, c0.Quote.Close);

            // 1. 검증된 손절 로직 유지 (Look-ahead bias 수정)
            if (shortPosition.StopLossPrice > 0 && c0.Quote.High >= shortPosition.StopLossPrice)
            {
                decimal exitPrice = shortPosition.StopLossPrice;
                if (c0.Quote.Open >= shortPosition.StopLossPrice) // 캔들이 손절가보다 높게 시작했으면 시가로 손절
                {
                    exitPrice = c0.Quote.Open;
                }
                DcaExitPosition(shortPosition, c0, exitPrice, 1.0m);
                consecutiveLosses++;
                lastLossTime = c0.DateTime;
                return;
            }

            // 2. 검증된 스캘핑 모드 유지 (80% 청산) (Look-ahead bias 수정)
            var scalpPrice = shortPosition.EntryPrice - (c1.Atr.Value * AtrMultiplier * ScalpTarget);
            if (shortPosition.Stage == 0 && c0.Quote.Low <= scalpPrice)
            {
                decimal exitPrice = scalpPrice;
                if (c0.Quote.Open <= scalpPrice) // 캔들이 익절가보다 낮게 시작했으면 시가로 익절
                {
                    exitPrice = c0.Quote.Open;
                }
                DcaExitPosition(shortPosition, c0, exitPrice, ScalpExitPercent);
                shortPosition.Stage = 1;
                shortPosition.StopLossPrice = shortPosition.EntryPrice - (c1.Atr.Value * AtrMultiplier * 0.5m); // 0.5 ATR 수익 보장
                consecutiveLosses = 0;
            }

            // 3. 나머지 20% 부분 익절 (Look-ahead bias 수정)
            var partialPrice = shortPosition.EntryPrice - (c1.Atr.Value * AtrMultiplier * ProfitTarget);
            if (shortPosition.Stage == 1 && c0.Quote.Low <= partialPrice)
            {
                decimal exitPrice = partialPrice;
                if (c0.Quote.Open <= partialPrice) // 캔들이 익절가보다 낮게 시작했으면 시가로 익절
                {
                    exitPrice = c0.Quote.Open;
                }
                DcaExitPosition(shortPosition, c0, exitPrice, PartialExitPercent);
                shortPosition.Stage = 2;
                shortPosition.StopLossPrice = shortPosition.EntryPrice - (c1.Atr.Value * AtrMultiplier * 1.0m); // 1.0 ATR 수익 보장			
            }

            // 4. 최종 전량 익절 (Look-ahead bias 수정)
            var fullExitPrice = shortPosition.EntryPrice - (c1.Atr.Value * AtrMultiplier * FullExitTarget);
            if (c0.Quote.Low <= fullExitPrice)
            {
                decimal exitPrice = fullExitPrice;
                if (c0.Quote.Open <= fullExitPrice) // 캔들이 익절가보다 낮게 시작했으면 시가로 익절
                {
                    exitPrice = c0.Quote.Open;
                }
                DcaExitPosition(shortPosition, c0, exitPrice, 1.0m);
                consecutiveLosses = 0;
                return;
            }

            // 5. 검증된 트레일링 스톱 유지
            if (profitPercent > TrailingPercent)
            {
                var newStopLoss = c0.Quote.Low * (1 + TrailingPercent / 100);
                if (newStopLoss < shortPosition.StopLossPrice || shortPosition.StopLossPrice == 0)
                {
                    shortPosition.StopLossPrice = newStopLoss;
                }
            }

            // 6. 조기 청산 (완화)
            bool earlyExit = c1.Cci <= -60m || c1.Rsi1 <= 35m; // -50 → -60, 40 → 35 (완화)
            bool lossExit = profitPercent < -0.5m; // 0.5% 손실시 조기 청산 유지

            if (earlyExit || lossExit)
            {
                DcaExitPosition(shortPosition, c0, c0.Quote.Open, 1.0m);
                if (lossExit)
                {
                    consecutiveLosses++;
                    lastLossTime = c0.DateTime;
                }
                return;
            }
        }
    }
}