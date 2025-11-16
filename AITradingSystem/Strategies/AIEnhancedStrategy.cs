using System;
using System.Collections.Generic;
using System.Linq;

using AITradingSystem.Models;

namespace AITradingSystem.Strategies
{
    /// <summary>
    /// AI가 모든 필터와 최적화 기능을 통합한 최고급 전략
    /// </summary>
    public class AIEnhancedStrategy : TradingStrategy
    {
        private bool _isInPosition = false;
        private Queue<Trade> _recentTrades = new Queue<Trade>();
        private int _consecutiveLosses = 0;
        private DateTime _lastTradeTime = DateTime.MinValue;
        private DateTime _cooldownUntil = DateTime.MinValue;
        private double _lastEntryPrice = 0;
        private List<double> _recentVolatility = new List<double>();

        public AIEnhancedStrategy()
        {
            Name = "🤖 AI Enhanced Strategy";

            // 기본 MA 파라미터
            Parameters["Period"] = 20;
            Parameters["FastMA"] = 10;
            Parameters["SlowMA"] = 30;

            // === 필터 시스템 ===
            // 변동성 필터
            Parameters["EnableVolatilityFilter"] = false;
            Parameters["MaxVolatility"] = 0.015;
            Parameters["MinVolatility"] = 0.005;
            Parameters["VolatilityPeriod"] = 20;

            // 추세 필터
            Parameters["EnableTrendFilter"] = false;
            Parameters["TrendPeriod"] = 50;
            Parameters["MinTrendStrength"] = 0.6;
            Parameters["TrendConfirmationPeriod"] = 5;

            // 연속 손실 방지
            Parameters["EnableConsecutiveLossFilter"] = false;
            Parameters["MaxConsecutiveLosses"] = 3;
            Parameters["CooldownMinutes"] = 15;
            Parameters["ProgressiveCooldown"] = true; // 연속 손실이 늘수록 쿨다운 증가

            // 시간 필터
            Parameters["EnableTimeFilter"] = false;
            Parameters["AvoidHours"] = new List<int> { 14, 15 }; // 오후 2-3시
            Parameters["ActiveHours"] = new List<int> { 9, 10, 11, 13, 16 }; // 활성 시간대

            // === 고급 AI 기능 ===
            // 동적 포지션 사이징
            Parameters["EnableDynamicSizing"] = false;
            Parameters["BasePositionSize"] = 1.0;
            Parameters["VolatilityAdjustment"] = true;
            Parameters["PerformanceAdjustment"] = true;

            // 스마트 스탑로스/테이크프로핏
            Parameters["EnableSmartStops"] = false;
            Parameters["InitialStopLoss"] = 0.02; // 2%
            Parameters["InitialTakeProfit"] = 0.04; // 4%
            Parameters["TrailingStop"] = true;
            Parameters["BreakevenStop"] = true;

            // 시장 상태 인식
            Parameters["EnableMarketRegimeFilter"] = false;
            Parameters["BullMarketThreshold"] = 0.7;
            Parameters["BearMarketThreshold"] = -0.3;
            Parameters["SidewaysThreshold"] = 0.2;

            // RSI 오버레이
            Parameters["EnableRSIFilter"] = false;
            Parameters["RSIPeriod"] = 14;
            Parameters["RSIOverBought"] = 70;
            Parameters["RSIOverSold"] = 30;

            // 볼린저 밴드 필터
            Parameters["EnableBollingerFilter"] = false;
            Parameters["BollingerPeriod"] = 20;
            Parameters["BollingerStdDev"] = 2.0;
        }

        public override TradeSignal GenerateSignal(List<MarketData> historicalData, MarketData currentData)
        {
            if (historicalData.Count < Math.Max((int)Parameters["TrendPeriod"], 50))
                return null;

            // === 1단계: 기본 필터 체크 ===
            if (!PassesBasicFilters(historicalData, currentData))
                return null;

            // === 2단계: 고급 AI 필터 체크 ===
            if (!PassesAdvancedFilters(historicalData, currentData))
                return null;

            // === 3단계: 신호 생성 ===
            var signal = GenerateTradingSignal(historicalData, currentData);

            if (signal != null)
            {
                // 신호에 컨텍스트 정보 추가
                EnrichSignalContext(signal, historicalData, currentData);

                // 포지션 상태 업데이트
                UpdatePositionState(signal);
            }

            return signal;
        }

        #region 필터 시스템

        private bool PassesBasicFilters(List<MarketData> historicalData, MarketData currentData)
        {
            // 쿨다운 체크
            if (DateTime.Now < _cooldownUntil)
                return false;

            // 연속 손실 필터
            if ((bool)Parameters["EnableConsecutiveLossFilter"] &&
                _consecutiveLosses >= (int)Parameters["MaxConsecutiveLosses"])
            {
                var cooldownMinutes = (int)Parameters["CooldownMinutes"];

                // 프로그레시브 쿨다운 (연속 손실이 많을수록 더 긴 쿨다운)
                if ((bool)Parameters["ProgressiveCooldown"])
                {
                    cooldownMinutes *= Math.Min(_consecutiveLosses, 5);
                }

                if ((DateTime.Now - _lastTradeTime).TotalMinutes < cooldownMinutes)
                    return false;
            }

            // 시간 필터
            if ((bool)Parameters["EnableTimeFilter"])
            {
                var currentHour = currentData.Timestamp.Hour;
                var avoidHours = (List<int>)Parameters["AvoidHours"];
                var activeHours = (List<int>)Parameters["ActiveHours"];

                if (avoidHours.Contains(currentHour) || !activeHours.Contains(currentHour))
                    return false;
            }

            // 변동성 필터
            if ((bool)Parameters["EnableVolatilityFilter"])
            {
                var volatility = CalculateVolatility(historicalData, (int)Parameters["VolatilityPeriod"]);
                var maxVol = (double)Parameters["MaxVolatility"];
                var minVol = (double)Parameters["MinVolatility"];

                if (volatility > maxVol || volatility < minVol)
                    return false;
            }

            return true;
        }

        private bool PassesAdvancedFilters(List<MarketData> historicalData, MarketData currentData)
        {
            // 추세 필터
            if ((bool)Parameters["EnableTrendFilter"])
            {
                var trendStrength = CalculateTrendStrength(historicalData, (int)Parameters["TrendPeriod"]);
                if (trendStrength < (double)Parameters["MinTrendStrength"])
                    return false;

                // 추세 확인 (최근 N개 캔들이 같은 방향)
                var confirmationPeriod = (int)Parameters["TrendConfirmationPeriod"];
                if (!ConfirmTrendDirection(historicalData, confirmationPeriod))
                    return false;
            }

            // 시장 상태 필터
            if ((bool)Parameters["EnableMarketRegimeFilter"])
            {
                var marketRegime = DetermineMarketRegime(historicalData);
                if (!IsSuitableMarketCondition(marketRegime))
                    return false;
            }

            // RSI 필터
            if ((bool)Parameters["EnableRSIFilter"])
            {
                var rsi = CalculateRSI(historicalData, (int)Parameters["RSIPeriod"]);
                var overBought = (double)Parameters["RSIOverBought"];
                var overSold = (double)Parameters["RSIOverSold"];

                // 매수시 RSI가 너무 높으면 제외, 매도시 RSI가 너무 낮으면 제외
                if (!_isInPosition && rsi > overBought) return false;
                if (_isInPosition && rsi < overSold) return false;
            }

            // 볼린저 밴드 필터
            if ((bool)Parameters["EnableBollingerFilter"])
            {
                var (upper, middle, lower) = CalculateBollingerBands(historicalData,
                    (int)Parameters["BollingerPeriod"], (double)Parameters["BollingerStdDev"]);

                // 밴드 상단/하단 근처에서는 역방향 신호만 허용
                if (currentData.Close > upper && !_isInPosition) return false;
                if (currentData.Close < lower && _isInPosition) return false;
            }

            return true;
        }

        #endregion

        #region 신호 생성

        private TradeSignal GenerateTradingSignal(List<MarketData> historicalData, MarketData currentData)
        {
            // 듀얼 MA 크로스오버 (기본) + 단일 MA 돌파 (보조)
            var fastMA = CalculateMA(historicalData, (int)Parameters["FastMA"]);
            var slowMA = CalculateMA(historicalData, (int)Parameters["SlowMA"]);
            var mainMA = CalculateMA(historicalData, (int)Parameters["Period"]);

            var prevFastMA = CalculateMA(historicalData.Take(historicalData.Count - 1).ToList(), (int)Parameters["FastMA"]);
            var prevSlowMA = CalculateMA(historicalData.Take(historicalData.Count - 1).ToList(), (int)Parameters["SlowMA"]);

            var signal = new TradeSignal
            {
                Timestamp = currentData.Timestamp,
                Price = currentData.Close
            };

            // 매수 신호: FastMA가 SlowMA를 상향 돌파 + 가격이 MainMA 위에 있음
            if (!_isInPosition &&
                fastMA > slowMA && prevFastMA <= prevSlowMA && // MA 크로스오버
                currentData.Close > mainMA) // 가격이 주 이동평균 위에 있음
            {
                signal.Type = "BUY";
                signal.Reason = $"듀얼 MA 크로스오버 + 가격 > MA{Parameters["Period"]}";
                return signal;
            }

            // 매도 신호: FastMA가 SlowMA를 하향 돌파 OR 가격이 MainMA 아래로
            if (_isInPosition &&
                ((fastMA < slowMA && prevFastMA >= prevSlowMA) || // MA 크로스 다운
                 currentData.Close < mainMA)) // 또는 가격이 주 이동평균 아래로
            {
                signal.Type = "SELL";
                signal.Reason = fastMA < slowMA ? "듀얼 MA 크로스 다운" : $"가격 < MA{Parameters["Period"]}";
                return signal;
            }

            // 스마트 스탑로스/테이크프로핏 체크
            if (_isInPosition && (bool)Parameters["EnableSmartStops"])
            {
                var stopSignal = CheckSmartStops(currentData);
                if (stopSignal != null)
                    return stopSignal;
            }

            return null;
        }

        private TradeSignal CheckSmartStops(MarketData currentData)
        {
            if (_lastEntryPrice <= 0) return null;

            var currentPrice = currentData.Close;
            var pnlPercent = (currentPrice - _lastEntryPrice) / _lastEntryPrice;

            var stopLoss = -(double)Parameters["InitialStopLoss"];
            var takeProfit = (double)Parameters["InitialTakeProfit"];

            // 기본 스탑로스/테이크프로핏
            if (pnlPercent <= stopLoss)
            {
                return new TradeSignal
                {
                    Timestamp = currentData.Timestamp,
                    Price = currentPrice,
                    Type = "SELL",
                    Reason = $"스탑로스 ({pnlPercent:P2})"
                };
            }

            if (pnlPercent >= takeProfit)
            {
                return new TradeSignal
                {
                    Timestamp = currentData.Timestamp,
                    Price = currentPrice,
                    Type = "SELL",
                    Reason = $"테이크프로핏 ({pnlPercent:P2})"
                };
            }

            // 브레이크이븐 스탑 (수익이 1% 이상이면 손실 방지)
            if ((bool)Parameters["BreakevenStop"] && pnlPercent > 0.01 && pnlPercent < -0.005)
            {
                return new TradeSignal
                {
                    Timestamp = currentData.Timestamp,
                    Price = currentPrice,
                    Type = "SELL",
                    Reason = "브레이크이븐 스탑"
                };
            }

            return null;
        }

        #endregion

        #region 계산 메서드들

        private double CalculateMA(List<MarketData> data, int period)
        {
            return data.TakeLast(period).Average(x => x.Close);
        }

        private double CalculateVolatility(List<MarketData> data, int period)
        {
            if (data.Count < period + 1) return 0;

            var returns = data.TakeLast(period + 1)
                .Select((x, i) => i == 0 ? 0 : Math.Log(x.Close / data[data.Count - period - 1 + i - 1].Close))
                .Skip(1);

            var mean = returns.Average();
            var variance = returns.Sum(x => Math.Pow(x - mean, 2)) / period;
            return Math.Sqrt(variance) * Math.Sqrt(252); // 연환산
        }

        private double CalculateTrendStrength(List<MarketData> data, int period)
        {
            var prices = data.TakeLast(period).Select(x => x.Close).ToList();
            var correlation = CalculateCorrelation(prices);
            var slope = CalculateSlope(prices);

            // 상관계수의 절댓값 * tanh(기울기)로 추세 강도 계산
            return Math.Abs(correlation) * Math.Tanh(Math.Abs(slope) * 10000);
        }

        private bool ConfirmTrendDirection(List<MarketData> data, int confirmationPeriod)
        {
            var recentPrices = data.TakeLast(confirmationPeriod).Select(x => x.Close).ToList();

            // 단조증가 또는 단조감소 확인
            bool increasing = true, decreasing = true;

            for (int i = 1; i < recentPrices.Count; i++)
            {
                if (recentPrices[i] <= recentPrices[i - 1]) increasing = false;
                if (recentPrices[i] >= recentPrices[i - 1]) decreasing = false;
            }

            return increasing || decreasing;
        }

        private string DetermineMarketRegime(List<MarketData> data)
        {
            var returns = CalculateReturns(data, 50);
            var avgReturn = returns.Average();

            var bullThreshold = (double)Parameters["BullMarketThreshold"];
            var bearThreshold = (double)Parameters["BearMarketThreshold"];
            var sidewaysThreshold = (double)Parameters["SidewaysThreshold"];

            if (avgReturn > bullThreshold) return "BULL";
            if (avgReturn < bearThreshold) return "BEAR";
            if (Math.Abs(avgReturn) < sidewaysThreshold) return "SIDEWAYS";

            return "NEUTRAL";
        }

        private bool IsSuitableMarketCondition(string marketRegime)
        {
            // 강세장에서는 매수 위주, 약세장에서는 거래 제한, 횡보장에서는 신중하게
            return marketRegime != "BEAR"; // 약세장이 아닐 때만 거래
        }

        private double CalculateRSI(List<MarketData> data, int period)
        {
            if (data.Count < period + 1) return 50; // 중립값 반환

            var changes = data.TakeLast(period + 1)
                .Select((x, i) => i == 0 ? 0.0 : x.Close - data[data.Count - period - 1 + i - 1].Close)
                .Skip(1)
                .ToList();

            var gains = changes.Where(x => x > 0).DefaultIfEmpty(0).Average();
            var losses = Math.Abs(changes.Where(x => x < 0).DefaultIfEmpty(0).Average());

            if (losses == 0) return 100;
            var rs = gains / losses;
            return 100 - (100 / (1 + rs));
        }

        private (double upper, double middle, double lower) CalculateBollingerBands(List<MarketData> data, int period, double stdDev)
        {
            var prices = data.TakeLast(period).Select(x => x.Close).ToList();
            var middle = prices.Average();
            var std = Math.Sqrt(prices.Sum(x => Math.Pow(x - middle, 2)) / period);

            return (middle + stdDev * std, middle, middle - stdDev * std);
        }

        private double CalculateSlope(List<double> prices)
        {
            int n = prices.Count;
            double sumX = n * (n - 1) / 2.0;
            double sumY = prices.Sum();
            double sumXY = prices.Select((price, i) => i * price).Sum();
            double sumX2 = n * (n - 1) * (2 * n - 1) / 6.0;

            return (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        }

        private double CalculateCorrelation(List<double> prices)
        {
            var indices = Enumerable.Range(0, prices.Count).Select(i => (double)i).ToList();

            var meanX = indices.Average();
            var meanY = prices.Average();

            var numerator = indices.Zip(prices, (x, y) => (x - meanX) * (y - meanY)).Sum();
            var denomX = Math.Sqrt(indices.Sum(x => Math.Pow(x - meanX, 2)));
            var denomY = Math.Sqrt(prices.Sum(y => Math.Pow(y - meanY, 2)));

            return denomX > 0 && denomY > 0 ? numerator / (denomX * denomY) : 0;
        }

        private List<double> CalculateReturns(List<MarketData> data, int period)
        {
            return data.TakeLast(period + 1)
                .Select((x, i) => i == 0 ? 0.0 : (x.Close - data[data.Count - period - 1 + i - 1].Close) / data[data.Count - period - 1 + i - 1].Close)
                .Skip(1)
                .ToList();
        }

        #endregion

        #region 유틸리티 메서드

        private void EnrichSignalContext(TradeSignal signal, List<MarketData> historicalData, MarketData currentData)
        {
            signal.Context["Volatility"] = CalculateVolatility(historicalData, 20);
            signal.Context["TrendStrength"] = CalculateTrendStrength(historicalData, (int)Parameters["TrendPeriod"]);
            signal.Context["RSI"] = CalculateRSI(historicalData, 14);
            signal.Context["ConsecutiveLosses"] = _consecutiveLosses;
            signal.Context["MarketRegime"] = DetermineMarketRegime(historicalData);

            var (upper, middle, lower) = CalculateBollingerBands(historicalData, 20, 2.0);
            signal.Context["BollingerPosition"] = (currentData.Close - lower) / (upper - lower); // 0~1 범위
        }

        private void UpdatePositionState(TradeSignal signal)
        {
            if (signal.Type == "BUY")
            {
                _isInPosition = true;
                _lastEntryPrice = signal.Price;
            }
            else if (signal.Type == "SELL")
            {
                _isInPosition = false;
                _lastTradeTime = signal.Timestamp;
                _lastEntryPrice = 0;
            }
        }

        public void UpdateTradeResult(Trade trade)
        {
            // 연속 손실 카운터 업데이트
            if (trade.ProfitLoss < 0)
            {
                _consecutiveLosses++;

                // 연속 손실이 많을수록 더 긴 쿨다운 설정
                if (_consecutiveLosses >= 5)
                {
                    _cooldownUntil = DateTime.Now.AddMinutes(30);
                }
                else if (_consecutiveLosses >= 3)
                {
                    _cooldownUntil = DateTime.Now.AddMinutes(15);
                }
            }
            else
            {
                _consecutiveLosses = 0; // 승리시 리셋
                _cooldownUntil = DateTime.MinValue; // 쿨다운 해제
            }

            // 최근 거래 기록 관리
            _recentTrades.Enqueue(trade);
            while (_recentTrades.Count > 50)
            {
                _recentTrades.Dequeue();
            }

            // 성과 기반 파라미터 자동 조정
            AutoAdjustParameters();
        }

        private void AutoAdjustParameters()
        {
            if (_recentTrades.Count < 10) return;

            var recentTrades = _recentTrades.ToList();
            var winRate = (double)recentTrades.Count(t => t.ProfitLoss > 0) / recentTrades.Count;

            // 승률이 낮으면 더 보수적으로
            if (winRate < 0.4)
            {
                Parameters["MinTrendStrength"] = Math.Min(0.8, (double)Parameters["MinTrendStrength"] + 0.1);
                Parameters["MaxVolatility"] = Math.Max(0.01, (double)Parameters["MaxVolatility"] - 0.002);
            }
            // 승률이 높으면 좀 더 적극적으로
            else if (winRate > 0.7)
            {
                Parameters["MinTrendStrength"] = Math.Max(0.5, (double)Parameters["MinTrendStrength"] - 0.05);
                Parameters["MaxVolatility"] = Math.Min(0.02, (double)Parameters["MaxVolatility"] + 0.001);
            }
        }

        #endregion

        public override void UpdateParameters(Dictionary<string, object> newParameters)
        {
            foreach (var param in newParameters)
            {
                Parameters[param.Key] = param.Value;
            }

            // 파라미터 업데이트 후 상태 리셋
            if (newParameters.ContainsKey("EnableConsecutiveLossFilter"))
            {
                _consecutiveLosses = 0;
                _cooldownUntil = DateTime.MinValue;
            }
        }
    }
}