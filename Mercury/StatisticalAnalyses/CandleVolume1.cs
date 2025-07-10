using Mercury.Charts;

using System.Diagnostics;
using System.Text;

namespace Mercury.StatisticalAnalyses
{
	public class CandleVolume1 : BaseStatisticalAnalysis
	{
		public void Run()
		{
			foreach (var chartPack in ChartPacks)
			{
				var charts = chartPack.Charts;
				var hammerStats = new List<PatternStat>();
				var shootingStarStats = new List<PatternStat>();

				for (int i = 20; i < charts.Count - 10; i++) // 20봉 이동평균, 10봉 미래 데이터 확보
				{
					var c = charts[i];
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);

					// Hammer + 거래량 급증
					if (IsHammer(c) && c.Quote.Volume > avgVolume * 2)
					{
						var stat = AnalyzeFuture(charts, i, 10, isLong: true);
						hammerStats.Add(stat);
					}
					// Shooting Star + 거래량 급증
					if (IsShootingStar(c) && c.Quote.Volume > avgVolume * 2)
					{
						var stat = AnalyzeFuture(charts, i, 10, isLong: false);
						shootingStarStats.Add(stat);
					}
				}

				PrintStats("Hammer + Volume Spike (저점반전)", hammerStats);
				PrintStats("Shooting Star + Volume Spike (고점반전)", shootingStarStats);
			}

		}

		public void Run_ThreeWhiteSoldiersStat()
		{
			foreach (var chartPack in ChartPacks)
			{
				var charts = chartPack.Charts;
				var stats = new List<PatternStat>();

				for (int i = 22; i < charts.Count - 10; i++)
				{
					// 3연속 양봉 체크
					bool isPattern =
						charts[i - 2].Quote.Close > charts[i - 2].Quote.Open &&
						charts[i - 1].Quote.Close > charts[i - 1].Quote.Open &&
						charts[i].Quote.Close > charts[i].Quote.Open;

					// 거래량 증가 체크 (마지막 봉이 20봉 평균의 1.5배 이상)
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool isVolume = charts[i].Quote.Volume > avgVolume * 1.5m;

					if (isPattern && isVolume)
					{
						var stat = AnalyzeFuture(charts, i, 10, isLong: true);
						stats.Add(stat);
					}
				}
				PrintStats("Three White Soldiers + Volume Spike", stats);
			}
		}

		public void Run_EngulfingStat()
		{
			foreach (var chartPack in ChartPacks)
			{
				var charts = chartPack.Charts;
				var bullStats = new List<PatternStat>();
				var bearStats = new List<PatternStat>();

				for (int i = 21; i < charts.Count - 10; i++)
				{
					var prev = charts[i - 1];
					var curr = charts[i];
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);

					// 강세 장악형 (Bullish Engulfing)
					if (prev.Quote.Close < prev.Quote.Open && curr.Quote.Close > curr.Quote.Open &&
						curr.Quote.Open < prev.Quote.Close && curr.Quote.Close > prev.Quote.Open &&
						curr.Quote.Volume > avgVolume * 1.5m)
					{
						var stat = AnalyzeFuture(charts, i, 10, isLong: true);
						bullStats.Add(stat);
					}
					// 약세 장악형 (Bearish Engulfing)
					if (prev.Quote.Close > prev.Quote.Open && curr.Quote.Close < curr.Quote.Open &&
						curr.Quote.Open > prev.Quote.Close && curr.Quote.Close < prev.Quote.Open &&
						curr.Quote.Volume > avgVolume * 1.5m)
					{
						var stat = AnalyzeFuture(charts, i, 10, isLong: false);
						bearStats.Add(stat);
					}
				}
				PrintStats("Bullish Engulfing + Volume", bullStats);
				PrintStats("Bearish Engulfing + Volume", bearStats);
			}
		}

		public void Run_RSIDivergenceStat()
		{
			foreach (var chartPack in ChartPacks)
			{
				var charts = chartPack.Charts;
				var stats = new List<PatternStat>();

				for (int i = 20; i < charts.Count - 10; i++)
				{
					// RSI 다이버전스: 가격 저점 갱신, RSI 저점 미갱신
					if (i >= 15 &&
						charts[i].Quote.Low < charts[i - 5].Quote.Low && // 가격 신저점
						charts[i].Rsi1 > charts[i - 5].Rsi1 && // RSI 신저점 미달
						IsHammer(charts[i]))
					{
						var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
						if (charts[i].Quote.Volume > avgVolume * 1.5m)
						{
							var stat = AnalyzeFuture(charts, i, 10, isLong: true);
							stats.Add(stat);
						}
					}
				}
				PrintStats("RSI Divergence + Hammer + Volume", stats);
			}
		}

		public void Run_WedgeBreakoutStat()
		{
			foreach (var chartPack in ChartPacks)
			{
				var charts = chartPack.Charts;
				var stats = new List<PatternStat>();

				for (int i = 30; i < charts.Count - 10; i++)
				{
					// 최근 20봉 저점/고점 수렴 조건 (단순화)
					var lows = charts.Skip(i - 20).Take(20).Select(x => x.Quote.Low).ToList();
					var highs = charts.Skip(i - 20).Take(20).Select(x => x.Quote.High).ToList();
					bool isConverging = highs.Max() - highs.Min() < highs.Max() * 0.02m &&
										lows.Max() - lows.Min() < lows.Max() * 0.02m;

					// 돌파 + 거래량 급증
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool isBreakout = charts[i].Quote.Close > highs.Max() && charts[i].Quote.Volume > avgVolume * 2;

					if (isConverging && isBreakout)
					{
						var stat = AnalyzeFuture(charts, i, 10, isLong: true);
						stats.Add(stat);
					}
				}
				PrintStats("Wedge Breakout + Volume Spike", stats);
			}
		}

		public void Run_BollingerBandBreakStat()
		{
			foreach (var chartPack in ChartPacks)
			{
				var charts = chartPack.Charts;
				var stats = new List<PatternStat>();

				for (int i = 20; i < charts.Count - 10; i++)
				{
					var currClose = charts[i].Quote.Close;
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);

					if (currClose < (decimal)charts[i].Bb1Lower && charts[i].Quote.Volume > avgVolume * 1.5m)
					{
						var stat = AnalyzeFuture(charts, i, 10, isLong: true);
						stats.Add(stat);
					}
				}
				PrintStats("Bollinger Lower Break + Volume Spike", stats);
			}
		}

		public void Run_RsiHammerVolumeStat()
		{
			foreach (var chartPack in ChartPacks)
			{
				var charts = chartPack.Charts;
				var stats = new List<PatternStat>();

				for (int i = 20; i < charts.Count - 10; i++)
				{
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					if (charts[i].Rsi1 <= 30 && IsHammer(charts[i]) && charts[i].Quote.Volume > avgVolume * 1.5m)
					{
						var stat = AnalyzeFuture(charts, i, 10, isLong: true);
						stats.Add(stat);
					}
				}
				PrintStats("RSI<=30 + Hammer + Volume", stats);
			}
		}

		public void Run_BollingerLowerRsiVolumeStat()
		{
			foreach (var chartPack in ChartPacks)
			{
				var charts = chartPack.Charts;
				var stats = new List<PatternStat>();

				for (int i = 20; i < charts.Count - 10; i++)
				{
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					if (
						charts[i].Quote.Close < (decimal)charts[i].Bb1Lower
						&& charts[i].Rsi1 <= 35
						&& charts[i].Quote.Volume > avgVolume * 1.5m
					)
					{
						var stat = AnalyzeFuture(charts, i, 10, isLong: true);
						stats.Add(stat);
					}
				}
				PrintStats("Bollinger Lower Break + RSI<=35 + Volume", stats);
			}
		}

		public void Run_StochHammerVolumeStat()
		{
			foreach (var chartPack in ChartPacks)
			{
				var charts = chartPack.Charts;
				var stats = new List<PatternStat>();

				for (int i = 20; i < charts.Count - 10; i++)
				{
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					if (
						charts[i].StochK <= 20
						&& IsHammer(charts[i])
						&& charts[i].Quote.Volume > avgVolume * 1.5m
					)
					{
						var stat = AnalyzeFuture(charts, i, 10, isLong: true);
						stats.Add(stat);
					}
				}
				PrintStats("Stoch %K<=20 + Hammer + Volume", stats);
			}
		}

		public void Run_MacdCrossVolumeStat()
		{
			foreach (var chartPack in ChartPacks)
			{
				var charts = chartPack.Charts;
				var stats = new List<PatternStat>();

				for (int i = 1; i < charts.Count - 10; i++)
				{
					var avgVolume = charts.Skip(i - 20 < 0 ? 0 : i - 20).Take(20).Average(x => x.Quote.Volume);
					if (
						charts[i - 1].Macd < charts[i - 1].MacdSignal
						&& charts[i].Macd > charts[i].MacdSignal
						&& charts[i].Quote.Volume > avgVolume * 1.5m
					)
					{
						var stat = AnalyzeFuture(charts, i, 10, isLong: true);
						stats.Add(stat);
					}
				}
				PrintStats("MACD Cross Up + Volume", stats);
			}
		}

		public void Run_BollingerUpperRsiVolumeStat()
		{
			foreach (var chartPack in ChartPacks)
			{
				var charts = chartPack.Charts;
				var stats = new List<PatternStat>();

				for (int i = 20; i < charts.Count - 10; i++)
				{
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					if (
						charts[i].Quote.Close > (decimal)charts[i].Bb1Upper
						&& charts[i].Rsi1 >= 70
						&& charts[i].Quote.Volume > avgVolume * 1.5m
					)
					{
						var stat = AnalyzeFuture(charts, i, 10, isLong: false);
						stats.Add(stat);
					}
				}
				PrintStats("Bollinger Upper Break + RSI>=70 + Volume", stats);
			}
		}

		public void Run_VolumeDivergence()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 20; i < charts.Count - 10; i++)
				{
					bool priceHigher = charts[i].Quote.High > charts[i - 5].Quote.High;
					bool volumeLower = charts[i].Quote.Volume < charts[i - 5].Quote.Volume * 0.8m;
					bool rsiLower = charts[i].Rsi1 < charts[i - 5].Rsi1 - 5;

					if (priceHigher && volumeLower && rsiLower)
					{
						var stat = AnalyzeFuture(charts, i, 10, false);
						stats.Add(stat);
					}
				}
				PrintStats("Volume Divergence (Price↑ Volume↓ RSI↓)", stats);
			}
		}

		public void Run_SupportResistanceBreak()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 20; i < charts.Count - 10; i++)
				{
					decimal supportLevel = charts.Skip(i - 20).Take(10).Min(x => x.Quote.Low);
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);

					bool breakSupport = charts[i].Quote.Close < supportLevel * 0.995m;
					bool volumeSpike = charts[i].Quote.Volume > avgVolume * 3.0m;

					if (breakSupport && volumeSpike)
					{
						var stat = AnalyzeFuture(charts, i, 10, true);
						stats.Add(stat);
					}
				}
				PrintStats("Support Break + Volume Explosion", stats);
			}
		}

		public void Run_MacdRsiConvergence()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 20; i < charts.Count - 10; i++)
				{
					bool macdRise = charts[i].Macd > charts[i - 3].Macd;
					bool rsiRise = charts[i].Rsi1 > charts[i - 3].Rsi1;

					// 개선된 Bull Candle 조건
					var body = Math.Abs(charts[i].Quote.Close - charts[i].Quote.Open);
					var avgBody = charts.Skip(i - 20).Take(20).Average(x => Math.Abs(x.Quote.Close - x.Quote.Open));
					bool isBodyLarge = body > avgBody * 1.2m;
					bool isBull = charts[i].Quote.Close > charts[i].Quote.Open;
					var upperWick = charts[i].Quote.High - Math.Max(charts[i].Quote.Close, charts[i].Quote.Open);
					bool isUpperWickShort = upperWick < body * 0.3m;
					bool isBullCandle = isBodyLarge && isBull && isUpperWickShort;

					bool volumeOk = charts[i].Quote.Volume > charts[i - 1].Quote.Volume * 1.2m;

					if (macdRise && rsiRise && isBullCandle && volumeOk)
					{
						var stat = AnalyzeFuture(charts, i, 10, true);
						stats.Add(stat);
					}
				}
				PrintStats("MACD/RSI Convergence + Bull Candle", stats);
			}
		}

		public void Run_BollingerSqueeze()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 20; i < charts.Count - 10; i++)
				{
					// null 체크
					if (charts[i].Bb1Upper == null || charts[i].Bb1Lower == null ||
						charts[i - 5].Bb1Upper == null || charts[i - 5].Bb1Lower == null)
						continue;

					decimal bandwidthD = charts[i].Bb1Upper.Value - charts[i].Bb1Lower.Value;
					decimal prevBandwidthD = charts[i - 5].Bb1Upper.Value - charts[i - 5].Bb1Lower.Value;
					decimal bandwidth = (decimal)bandwidthD;
					decimal prevBandwidth = (decimal)prevBandwidthD;
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);

					bool squeeze = bandwidth < prevBandwidth * 0.7m;
					bool breakout = charts[i].Quote.Close > (decimal)charts[i].Bb1Upper.Value;
					bool volumeSpike = charts[i].Quote.Volume > avgVolume * 2.5m;

					if (squeeze && breakout && volumeSpike)
					{
						var stat = AnalyzeFuture(charts, i, 10, true);
						stats.Add(stat);
					}
				}
				PrintStats("Bollinger Squeeze Breakout", stats);
			}
		}

		public void Run_DivergenceCandleVolume()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;
				for (int i = 15; i < charts.Count - 10; i++)
				{
					// 가격 저점 갱신, RSI 저점 미갱신, Hammer, 거래량 급증
					if (charts[i].Quote.Low < charts[i - 5].Quote.Low
						&& charts[i].Rsi1 > charts[i - 5].Rsi1
						&& IsHammer(charts[i])
						&& charts[i].Quote.Volume > charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume) * 1.5m)
					{
						var stat = AnalyzeFuture(charts, i, 10, true);
						stats.Add(stat);
					}
				}
				PrintStats("Divergence+Hammer+Volume", stats);
			}
		}

		public void Run_VolatilityBreakout()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;
				for (int i = 20; i < charts.Count - 10; i++)
				{
					// ATR 돌파(당일 고가-저가 > 20봉 ATR*1.5), 거래량 급증
					decimal atr = charts[i].Atr.Value;
					decimal range = charts[i].Quote.High - charts[i].Quote.Low;
					if (atr > 0 && range > (atr * 1.5m)
						&& charts[i].Quote.Volume > charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume) * 2.0m)
					{
						var stat = AnalyzeFuture(charts, i, 10, true);
						stats.Add(stat);
					}
				}
				PrintStats("Volatility Breakout (ATR+Volume)", stats);
			}
		}

		public void Run_SupportResistanceCluster()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;
				for (int i = 30; i < charts.Count - 10; i++)
				{
					// 최근 30봉 최저가(지지선) 부근에서 Hammer + 거래량 급증
					decimal support = charts.Skip(i - 30).Take(30).Min(x => x.Quote.Low);
					if (Math.Abs(charts[i].Quote.Low - support) < support * 0.002m
						&& IsHammer(charts[i])
						&& charts[i].Quote.Volume > charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume) * 1.5m)
					{
						var stat = AnalyzeFuture(charts, i, 10, true);
						stats.Add(stat);
					}
				}
				PrintStats("Support Cluster+Hammer+Volume", stats);
			}
		}

		public void Run_TrendFilterEngulfing()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;
				for (int i = 21; i < charts.Count - 10; i++)
				{
					if (charts[i].Sma3 == null) continue; // null이면 건너뛰기

					if (charts[i].Quote.Close > (decimal)charts[i].Sma3
						&& IsBullishEngulfing(charts[i - 1], charts[i])
						&& charts[i].Quote.Volume > charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume) * 1.5m)
					{
						var stat = AnalyzeFuture(charts, i, 10, true);
						stats.Add(stat);
					}
				}

				PrintStats("MA120+Bullish Engulfing+Volume", stats);
			}
		}

		public void Run_TrendPullbackStrategy()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;
				for (int i = 50; i < charts.Count - 15; i++)
				{
					// null 체크
					if (charts[i].Sma2 == null || charts[i].Sma1 == null || charts[i].Rsi1 == null)
						continue;

					// 장기 추세 확인 (MA200 위)
					if (charts[i].Quote.Close < (decimal)charts[i].Sma2.Value) continue;

					// 풀백 확인 (MA50 접근)
					decimal ma50 = (decimal)charts[i].Sma1.Value;
					bool isPullback = Math.Abs(charts[i].Quote.Close - ma50) < ma50 * 0.01m;

					// 모멘텀 확인 (RSI 40-60)
					bool isMomentumOk = charts[i].Rsi1.Value > 40 && charts[i].Rsi1.Value < 60;

					// 거래량 스파이크 (2.5배 이상)
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool isVolumeSpike = charts[i].Quote.Volume > avgVolume * 2.5m;

					if (isPullback && isMomentumOk && isVolumeSpike)
					{
						var stat = AnalyzeFuture(charts, i, 15, true); // 15봉 후까지 분석
						stats.Add(stat);
					}
				}
				PrintStats("Trend Following with Pullback", stats);
			}
		}

		public void Run_MacdVolumeWeighted()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;
				for (int i = 2; i < charts.Count - 10; i++)
				{
					// MACD 골든크로스
					bool isMacdCross = charts[i - 1].Macd < charts[i - 1].MacdSignal &&
									  charts[i].Macd > charts[i].MacdSignal;

					// 거래량 가중치 (현재 거래량 > 이전 3봉 평균의 2배)
					double avgVol3 = charts.Skip(i - 3).Take(3).Average(x => (double)x.Quote.Volume);
					bool isVolumeWeighted = (double)charts[i].Quote.Volume > avgVol3 * 2.0;

					// RSI 과매수 회피
					bool isRsiOk = charts[i].Rsi1 < 65;

					if (isMacdCross && isVolumeWeighted && isRsiOk)
					{
						var stat = AnalyzeFuture(charts, i, 10, true);
						stats.Add(stat);
					}
				}
				PrintStats("Volume-Weighted MACD Crossover", stats);
			}
		}

		public void Run_BreakoutRetest()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;
				for (int i = 10; i < charts.Count - 15; i++)
				{
					// 저항선 돌파 확인 (3봉 전 고점 돌파)
					decimal resistance = charts.Skip(i - 10).Take(7).Max(x => x.Quote.High);
					bool isBreakout = charts[i].Quote.Close > resistance;

					// 리테스트 확인 (돌파 후 지지 확인)
					bool isRetest = false;
					for (int j = 1; j <= 3; j++)
					{
						if (i + j >= charts.Count) break;
						if (charts[i + j].Quote.Low > resistance * 0.995m)
						{
							isRetest = true;
							break;
						}
					}

					// 거래량 패턴 (돌파 시 거래량 > 리테스트 시 거래량)
					bool isVolumePattern = isBreakout && isRetest &&
										  charts[i].Quote.Volume > charts[i + 1].Quote.Volume * 1.2m;

					if (isBreakout && isRetest && isVolumePattern)
					{
						var stat = AnalyzeFuture(charts, i, 10, true);
						stats.Add(stat);
					}
				}
				PrintStats("Breakout with Retest", stats);
			}
		}

		public void Run_RsiDivergencePriceAction()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;
				for (int i = 20; i < charts.Count - 10; i++)
				{
					// RSI 다이버전스 확인 (가격 저점 하락, RSI 저점 상승)
					bool isDivergence = charts[i].Quote.Low < charts[i - 5].Quote.Low &&
										charts[i].Rsi1 > charts[i - 5].Rsi1 + 5;

					// 캔들 몸통/꼬리 기반 강세 캔들 조건
					var body = Math.Abs(charts[i].Quote.Close - charts[i].Quote.Open);
					var avgBody = charts.Skip(i - 20).Take(20).Average(x => Math.Abs(x.Quote.Close - x.Quote.Open));
					bool isBodyLarge = body > avgBody * 1.2m;
					bool isBullish = charts[i].Quote.Close > charts[i].Quote.Open;
					var upperWick = charts[i].Quote.High - Math.Max(charts[i].Quote.Close, charts[i].Quote.Open);
					bool isUpperWickShort = upperWick < body * 0.3m;
					bool isBullishCandle = isBodyLarge && isBullish && isUpperWickShort;

					// 거래량 증가 (평균 대비 1.8배 이상)
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool isVolumeOk = charts[i].Quote.Volume > avgVolume * 1.8m;

					if (isDivergence && isBullishCandle && isVolumeOk)
					{
						var stat = AnalyzeFuture(charts, i, 10, true);
						stats.Add(stat);
					}
				}
				PrintStats("RSI Divergence with Price Action", stats);
			}
		}

		public void Run_BollingerSqueezeBreakout()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;
				for (int i = 20; i < charts.Count - 10; i++)
				{
					// null 체크
					if (charts[i].Bb1Upper == null || charts[i].Bb1Lower == null ||
						charts[i - 5].Bb1Upper == null || charts[i - 5].Bb1Lower == null)
						continue;

					decimal currentBandwidth = charts[i].Bb1Upper.Value - charts[i].Bb1Lower.Value;
					decimal prevBandwidth = charts[i - 5].Bb1Upper.Value - charts[i - 5].Bb1Lower.Value;
					bool isSqueeze = currentBandwidth < prevBandwidth * 0.4m;

					// 상향 돌파 확인
					bool isBreakout = charts[i].Quote.Close > (decimal)charts[i].Bb1Upper.Value;

					// 거래량 스파이크 (3배 이상)
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool isVolumeSpike = charts[i].Quote.Volume > avgVolume * 3.0m;

					if (isSqueeze && isBreakout && isVolumeSpike)
					{
						var stat = AnalyzeFuture(charts, i, 10, true);
						stats.Add(stat);
					}
				}

				PrintStats("Bollinger Squeeze Breakout", stats);
			}
		}

		public void Run_VolumeSpikeWithTrendDivergence()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;
				for (int i = 25; i < charts.Count - 10; i++)
				{
					if (charts[i].Rsi1 == null) continue;
					// 가격은 저점 갱신, RSI는 저점 미갱신, 강세 Engulfing, 거래량 2배
					if (charts[i].Quote.Low < charts[i - 5].Quote.Low
						&& charts[i].Rsi1.Value > charts[i - 5].Rsi1.Value
						&& IsBullishEngulfing(charts[i - 1], charts[i])
						&& charts[i].Quote.Volume > charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume) * 2.0m)
					{
						var stat = AnalyzeFuture(charts, i, 10, true);
						stats.Add(stat);
					}
				}
				PrintStats("Volume Spike + Trend Divergence + Engulfing", stats);
			}
		}

		public void Run_MomentumReversalCluster()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;
				for (int i = 30; i < charts.Count - 10; i++)
				{
					if (charts[i].Rsi1 == null) continue;
					// 최근 30봉 저점/고점 근처, RSI 35 이하, Hammer, 거래량 2배
					decimal support = charts.Skip(i - 30).Take(30).Min(x => x.Quote.Low);
					if (Math.Abs(charts[i].Quote.Low - support) < support * 0.003m
						&& charts[i].Rsi1.Value <= 35
						&& IsHammer(charts[i])
						&& charts[i].Quote.Volume > charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume) * 2.0m)
					{
						var stat = AnalyzeFuture(charts, i, 10, true);
						stats.Add(stat);
					}
				}
				PrintStats("Momentum Reversal Cluster (Support+RSI+Hammer+Volume)", stats);
			}
		}

		public void Run_CompositeATRBreakout()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;
				for (int i = 20; i < charts.Count - 10; i++)
				{
					if (charts[i].Bb1Upper == null || charts[i].Atr == null) continue;
					decimal atr = charts[i].Atr.Value;
					decimal range = charts[i].Quote.High - charts[i].Quote.Low;
					bool atrBreak = atr > 0 && range > (atr * 1.2m);
					bool bbBreak = charts[i].Quote.Close > charts[i].Bb1Upper.Value;
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool volumeSpike = charts[i].Quote.Volume > avgVolume * 2.0m;
					if (atrBreak && bbBreak && volumeSpike)
					{
						var stat = AnalyzeFuture(charts, i, 10, true);
						stats.Add(stat);
					}
				}
				PrintStats("ATR Breakout + UpperBand + Volume", stats);
			}
		}

		public void Run_SessionOpenBreakout()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;
				for (int i = 20; i < charts.Count - 10; i++)
				{
					if (charts[i].Bb1Upper == null) continue;
					// 세션 오픈(UTC 8시, 13시, 20시) 근처
					var hour = charts[i].Quote.Date.Hour;
					bool isSessionOpen = hour == 8 || hour == 13 || hour == 20;
					bool bbBreak = charts[i].Quote.Close > (decimal)charts[i].Bb1Upper.Value;
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool volumeSpike = charts[i].Quote.Volume > avgVolume * 2.0m;
					if (isSessionOpen && bbBreak && volumeSpike)
					{
						var stat = AnalyzeFuture(charts, i, 10, true);
						stats.Add(stat);
					}
				}
				PrintStats("Session Open + UpperBand + Volume", stats);
			}
		}

		public void Run_HighConfidenceBreakout()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;
				for (int i = 30; i < charts.Count - 15; i++)
				{
					if (charts[i].Bb1Upper == null || charts[i].Atr == null || charts[i].Rsi1 == null) continue;
					decimal atr = charts[i].Atr.Value;
					decimal range = charts[i].Quote.High - charts[i].Quote.Low;
					bool atrBreak = atr > 0 && range > (atr * 1.3m);
					bool bbBreak = charts[i].Quote.Close > charts[i].Bb1Upper.Value;
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool volumeSpike = charts[i].Quote.Volume > avgVolume * 2.5m;
					bool rsiFilter = charts[i].Rsi1.Value > 55 && charts[i].Rsi1.Value < 75;
					if (atrBreak && bbBreak && volumeSpike && rsiFilter)
					{
						var stat = AnalyzeFuture(charts, i, 15, true);
						stats.Add(stat);
					}
				}
				PrintStats("High Confidence Breakout", stats);
			}
		}

		public void Run_VolumeMomentumDivergence()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;
				for (int i = 25; i < charts.Count - 10; i++)
				{
					if (charts[i].Rsi1 == null) continue;
					bool priceLower = charts[i].Quote.Low < charts[i - 5].Quote.Low;
					bool rsiHigher = charts[i].Rsi1.Value > charts[i - 5].Rsi1.Value + 7;
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool volumeSpike = charts[i].Quote.Volume > avgVolume * 2.2m;
					bool isBullishEngulfing = IsBullishEngulfing(charts[i - 1], charts[i]);
					if (priceLower && rsiHigher && volumeSpike && isBullishEngulfing)
					{
						var stat = AnalyzeFuture(charts, i, 15, true);
						stats.Add(stat);
					}
				}
				PrintStats("Volume+Momentum+Divergence+Engulfing", stats);
			}
		}

		public void Run_SupportResistanceConfluence()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;
				for (int i = 30; i < charts.Count - 10; i++)
				{
					if (charts[i].Rsi1 == null) continue;
					decimal support = charts.Skip(i - 30).Take(30).Min(x => x.Quote.Low);
					decimal resistance = charts.Skip(i - 30).Take(30).Max(x => x.Quote.High);
					bool nearSupport = Math.Abs(charts[i].Quote.Low - support) < support * 0.0025m;
					bool nearResistance = Math.Abs(charts[i].Quote.High - resistance) < resistance * 0.0025m;
					bool rsiLow = charts[i].Rsi1.Value <= 38;
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool volumeSpike = charts[i].Quote.Volume > avgVolume * 2.0m;
					bool isHammer = IsHammer(charts[i]);
					if ((nearSupport || nearResistance) && rsiLow && volumeSpike && isHammer)
					{
						var stat = AnalyzeFuture(charts, i, 15, true);
						stats.Add(stat);
					}
				}
				PrintStats("Support/Resistance Confluence + RSI + Hammer + Volume", stats);
			}
		}

		public void Run_AdvancedATRVolume()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;
				for (int i = 20; i < charts.Count - 10; i++)
				{
					if (charts[i].Atr == null || charts[i].Bb1Upper == null || charts[i].Rsi1 == null) continue;
					decimal atr = charts[i].Atr.Value;
					decimal range = charts[i].Quote.High - charts[i].Quote.Low;
					bool atrBreak = atr > 0 && range > (atr * 1.4m);
					bool bbBreak = charts[i].Quote.Close > charts[i].Bb1Upper.Value;
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool volumeSpike = charts[i].Quote.Volume > avgVolume * 3.0m;
					bool rsiFilter = charts[i].Rsi1.Value > 50 && charts[i].Rsi1.Value < 70;
					if (atrBreak && bbBreak && volumeSpike && rsiFilter)
					{
						var stat = AnalyzeFuture(charts, i, 15, true);
						stats.Add(stat);
					}
				}
				PrintStats("Advanced ATR + Bollinger + Volume + RSI", stats);
			}
		}

		public void Run_SessionVolumeTiming()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;
				for (int i = 20; i < charts.Count - 10; i++)
				{
					if (charts[i].Bb1Upper == null) continue;
					var hour = charts[i].Quote.Date.Hour;
					bool isSessionOpen = hour == 8 || hour == 13 || hour == 20;
					bool bbBreak = charts[i].Quote.Close > (decimal)charts[i].Bb1Upper.Value;
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool volumeSpike = charts[i].Quote.Volume > avgVolume * 2.5m;
					if (isSessionOpen && bbBreak && volumeSpike)
					{
						var stat = AnalyzeFuture(charts, i, 10, true);
						stats.Add(stat);
					}
				}
				PrintStats("Session Open + UpperBand + Volume Spike", stats);
			}
		}

		public void Run_ThreeTouchBreakoutStrategy()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 30; i < charts.Count - 15; i++)
				{
					// null 체크
					if (charts[i].Bb1Upper == null || charts[i].Rsi1 == null) continue;

					// 최근 20봉 내 고점을 저항선으로 설정
					decimal resistance = charts.Skip(i - 20).Take(20).Max(x => x.Quote.High);
					int touchCount = 0;

					// 저항선 터치 횟수 계산
					for (int j = i - 15; j < i; j++)
					{
						if (Math.Abs(charts[j].Quote.High - resistance) < resistance * 0.005m)
							touchCount++;
					}

					// 정확히 3번 터치 후 돌파 + 거래량 급증 + RSI 필터
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool isBreakout = charts[i].Quote.Close > resistance * 1.002m;
					bool volumeSpike = charts[i].Quote.Volume > avgVolume * 2.5m;
					bool rsiFilter = charts[i].Rsi1.Value > 50 && charts[i].Rsi1.Value < 75;

					if (touchCount == 3 && isBreakout && volumeSpike && rsiFilter)
					{
						var stat = AnalyzeFuture(charts, i, 15, true);
						stats.Add(stat);
					}
				}
				PrintStats("3-Touch Resistance Breakout Strategy", stats);
			}
		}

		public void Run_DoubleBottomVolumeStrategy()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 40; i < charts.Count - 15; i++)
				{
					// null 체크
					if (charts[i].Rsi1 == null) continue;

					// 더블바텀 패턴 감지
					var lows = charts.Skip(i - 30).Take(30).Select((c, idx) => new { Low = c.Quote.Low, Index = idx }).ToList();
					var sortedLows = lows.OrderBy(x => x.Low).Take(2).ToList();

					// 두 저점이 비슷한 수준이고 시간차가 있는지 확인
					bool isDoubleBottom = Math.Abs(sortedLows[0].Low - sortedLows[1].Low) < sortedLows[0].Low * 0.02m
										&& Math.Abs(sortedLows[0].Index - sortedLows[1].Index) > 5;

					// 넥라인 돌파 확인
					decimal neckline = charts.Skip(i - 20).Take(10).Max(x => x.Quote.High);
					bool necklineBreak = charts[i].Quote.Close > neckline;

					// 거래량 급증 및 RSI 과매도 탈출
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool volumeSpike = charts[i].Quote.Volume > avgVolume * 2.0m;
					bool rsiRecovery = charts[i].Rsi1.Value > 35 && charts[i].Rsi1.Value < 60;

					if (isDoubleBottom && necklineBreak && volumeSpike && rsiRecovery)
					{
						var stat = AnalyzeFuture(charts, i, 20, true);
						stats.Add(stat);
					}
				}
				PrintStats("Double Bottom + Volume Confirmation", stats);
			}
		}

		public void Run_MeanReversionFibonacciStrategy()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 50; i < charts.Count - 15; i++)
				{
					// null 체크
					if (charts[i].Bb1Lower == null || charts[i].Bb1Upper == null || charts[i].Rsi1 == null) continue;

					// 평균회귀 조건: 볼린저밴드 하단 돌파
					bool meanReversionSignal = charts[i].Quote.Close < (decimal)charts[i].Bb1Lower.Value;

					// 최근 20봉 고점/저점으로 피보나치 레벨 계산
					decimal high = charts.Skip(i - 20).Take(20).Max(x => x.Quote.High);
					decimal low = charts.Skip(i - 20).Take(20).Min(x => x.Quote.Low);
					decimal fib618 = high - (high - low) * 0.618m;
					decimal fib382 = high - (high - low) * 0.382m;

					// 피보나치 지지선 근처에서 반등
					bool nearFibSupport = charts[i].Quote.Low <= fib618 && charts[i].Quote.Close >= fib618 * 0.995m;

					// RSI 과매도 + 거래량 증가
					bool rsiOversold = charts[i].Rsi1.Value <= 30;
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool volumeIncrease = charts[i].Quote.Volume > avgVolume * 1.5m;

					// 강세 캔들 확인
					bool bullishCandle = charts[i].Quote.Close > charts[i].Quote.Open * 1.005m;

					if (meanReversionSignal && nearFibSupport && rsiOversold && volumeIncrease && bullishCandle)
					{
						var stat = AnalyzeFuture(charts, i, 15, true);
						stats.Add(stat);
					}
				}
				PrintStats("Mean Reversion + Fibonacci Support", stats);
			}
		}

		public void Run_IchimokuBreakoutMomentumStrategy()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 52; i < charts.Count - 15; i++)
				{
					// null 체크 (이치모쿠 계산을 위한 충분한 데이터 필요)
					if (charts[i].Rsi1 == null || charts[i].Macd == null) continue;

					// 간단한 이치모쿠 클라우드 계산
					decimal tenkan = (charts.Skip(i - 9).Take(9).Max(x => x.Quote.High) +
									 charts.Skip(i - 9).Take(9).Min(x => x.Quote.Low)) / 2;
					decimal kijun = (charts.Skip(i - 26).Take(26).Max(x => x.Quote.High) +
									charts.Skip(i - 26).Take(26).Min(x => x.Quote.Low)) / 2;
					decimal senkouA = (tenkan + kijun) / 2;
					decimal senkouB = (charts.Skip(i - 52).Take(52).Max(x => x.Quote.High) +
									  charts.Skip(i - 52).Take(52).Min(x => x.Quote.Low)) / 2;

					decimal cloudTop = Math.Max(senkouA, senkouB);
					decimal cloudBottom = Math.Min(senkouA, senkouB);

					// 클라우드 돌파 조건
					bool breakoutAboveCloud = charts[i].Quote.Close > cloudTop &&
											 charts[i - 1].Quote.Close <= cloudTop;

					// 텐칸/기준선 골든크로스
					bool tenkanKijunBullish = tenkan > kijun;

					// 모멘텀 확인: MACD > 0, RSI > 50
					bool momentumConfirm = charts[i].Macd > 0 && charts[i].Rsi1.Value > 50;

					// 거래량 급증
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool volumeBreakout = charts[i].Quote.Volume > avgVolume * 2.0m;

					// 클라우드 두께 확인 (얇은 클라우드일 때 더 신뢰도 높음)
					bool thinCloud = (cloudTop - cloudBottom) / cloudTop < 0.05m;

					if (breakoutAboveCloud && tenkanKijunBullish && momentumConfirm && volumeBreakout && thinCloud)
					{
						var stat = AnalyzeFuture(charts, i, 18, true);
						stats.Add(stat);
					}
				}
				PrintStats("Ichimoku Cloud Breakout + Momentum Confirmation", stats);
			}
		}

		public void Run_CompositeSignalStrategy()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 50; i < charts.Count - 15; i++)
				{
					// null 체크
					if (charts[i].Rsi1 == null || charts[i].Bb1Upper == null ||
						charts[i].Bb1Lower == null || charts[i].Macd == null ||
						charts[i].MacdSignal == null) continue;

					// 1. 3-Touch Resistance 확인
					decimal resistance = charts.Skip(i - 20).Take(20).Max(x => x.Quote.High);
					int touchCount = 0;
					for (int j = i - 15; j < i; j++)
					{
						if (Math.Abs(charts[j].Quote.High - resistance) < resistance * 0.005m)
							touchCount++;
					}
					bool resistanceTouch = touchCount >= 2; // 최소 2번 이상 터치

					// 2. 피보나치 지지선 확인
					decimal high = charts.Skip(i - 20).Take(20).Max(x => x.Quote.High);
					decimal low = charts.Skip(i - 20).Take(20).Min(x => x.Quote.Low);
					decimal fib618 = high - (high - low) * 0.618m;
					bool fibSupport = Math.Abs(charts[i].Quote.Low - fib618) < fib618 * 0.01m;

					// 3. 이치모쿠 클라우드 돌파
					decimal tenkan = (charts.Skip(i - 9).Take(9).Max(x => x.Quote.High) +
									 charts.Skip(i - 9).Take(9).Min(x => x.Quote.Low)) / 2;
					decimal kijun = (charts.Skip(i - 26).Take(26).Max(x => x.Quote.High) +
									charts.Skip(i - 26).Take(26).Min(x => x.Quote.Low)) / 2;
					bool tenkanKijunCross = tenkan > kijun && charts[i - 1].Quote.Close < (decimal)charts[i - 1].Sma1;

					// 4. RSI 과매도 회복
					bool rsiRecovery = charts[i].Rsi1.Value < 40 && charts[i].Rsi1.Value > charts[i - 1].Rsi1.Value;

					// 5. MACD 골든크로스
					bool macdCross = charts[i - 1].Macd < charts[i - 1].MacdSignal &&
									 charts[i].Macd > charts[i].MacdSignal;

					// 6. 거래량 급증
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool volumeSpike = charts[i].Quote.Volume > avgVolume * 2.0m;

					// 최소 3개 이상의 조건 충족 시 신호 생성
					int conditionsMet = 0;
					if (resistanceTouch) conditionsMet++;
					if (fibSupport) conditionsMet++;
					if (tenkanKijunCross) conditionsMet++;
					if (rsiRecovery) conditionsMet++;
					if (macdCross) conditionsMet++;
					if (volumeSpike) conditionsMet++;

					if (conditionsMet >= 3)
					{
						var stat = AnalyzeFuture(charts, i, 15, true);
						stats.Add(stat);
					}
				}
				PrintStats("Composite Signal Strategy (3+ Conditions)", stats);
			}
		}

		public void Run_MarketContextStrategy()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 50; i < charts.Count - 15; i++)
				{
					// null 체크
					if (charts[i].Sma1 == null || charts[i].Sma2 == null ||
						charts[i].Bb1Upper == null || charts[i].Bb1Lower == null ||
						charts[i].Rsi1 == null) continue;

					// 시장 상황 판단 (추세/횡보장)
					decimal ma20 = charts[i].Sma1.Value;
					decimal ma50 = charts[i].Sma2.Value;
					decimal bandWidth = charts[i].Bb1Upper.Value - charts[i].Bb1Lower.Value;
					decimal avgBandWidth = 0;

					for (int j = i - 10; j < i; j++)
					{
						if (charts[j].Bb1Upper != null && charts[j].Bb1Lower != null)
							avgBandWidth += (charts[j].Bb1Upper.Value - charts[j].Bb1Lower.Value);
					}
					avgBandWidth /= 10;

					bool isTrending = ma20 > ma50 * 1.01m || ma20 < ma50 * 0.99m;
					bool isRangebound = bandWidth < avgBandWidth * 0.8m;

					// 추세장 전략: 3-Touch Breakout + Ichimoku
					if (isTrending)
					{
						// 저항선 터치 확인
						decimal resistance = charts.Skip(i - 20).Take(20).Max(x => x.Quote.High);
						int touchCount = 0;
						for (int j = i - 15; j < i; j++)
						{
							if (Math.Abs(charts[j].Quote.High - resistance) < resistance * 0.005m)
								touchCount++;
						}

						// 이치모쿠 확인 (간소화)
						decimal tenkan = (charts.Skip(i - 9).Take(9).Max(x => x.Quote.High) +
										 charts.Skip(i - 9).Take(9).Min(x => x.Quote.Low)) / 2;
						decimal kijun = (charts.Skip(i - 26).Take(26).Max(x => x.Quote.High) +
										charts.Skip(i - 26).Take(26).Min(x => x.Quote.Low)) / 2;

						bool breakoutSignal = touchCount >= 2 &&
											 charts[i].Quote.Close > resistance &&
											 tenkan > kijun &&
											 charts[i].Quote.Volume > charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume) * 1.8m;

						if (breakoutSignal)
						{
							var stat = AnalyzeFuture(charts, i, 15, true);
							stats.Add(stat);
						}
					}
					// 횡보장 전략: Mean Reversion + Fibonacci
					else if (isRangebound)
					{
						// 볼린저밴드 하단 접근
						bool nearLowerBand = charts[i].Quote.Low < (decimal)charts[i].Bb1Lower.Value * 1.01m;

						// 피보나치 지지선
						decimal high = charts.Skip(i - 20).Take(20).Max(x => x.Quote.High);
						decimal low = charts.Skip(i - 20).Take(20).Min(x => x.Quote.Low);
						decimal fib618 = high - (high - low) * 0.618m;

						// RSI 과매도
						bool rsiOversold = charts[i].Rsi1.Value < 35;

						bool reversalSignal = nearLowerBand &&
											 Math.Abs(charts[i].Quote.Low - fib618) < fib618 * 0.01m &&
											 rsiOversold &&
											 charts[i].Quote.Volume > charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume) * 1.5m;

						if (reversalSignal)
						{
							var stat = AnalyzeFuture(charts, i, 15, true);
							stats.Add(stat);
						}
					}
				}
				PrintStats("Market Context Adaptive Strategy", stats);
			}
		}

		public void Run_VolumeProfileMarketStructure()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 50; i < charts.Count - 15; i++)
				{
					// null 체크
					if (charts[i].Sma1 == null || charts[i].Sma2 == null ||
						charts[i].Bb1Upper == null || charts[i].Bb1Lower == null ||
						charts[i].Rsi1 == null) continue;

					// 1. 시장 구조 분석 (고점/저점 패턴)
					var highs = new List<decimal>();
					var lows = new List<decimal>();
					for (int j = i - 30; j < i; j += 5)
					{
						highs.Add(charts.Skip(j).Take(5).Max(x => x.Quote.High));
						lows.Add(charts.Skip(j).Take(5).Min(x => x.Quote.Low));
					}

					bool higherHighs = highs.Count >= 3 && highs[highs.Count - 1] > highs[highs.Count - 2] &&
									  highs[highs.Count - 2] > highs[highs.Count - 3];
					bool higherLows = lows.Count >= 3 && lows[lows.Count - 1] > lows[lows.Count - 2] &&
									 lows[lows.Count - 2] > lows[lows.Count - 3];
					bool uptrend = higherHighs && higherLows;

					// 2. 볼륨 프로파일 (거래량 집중 구간)
					var volumeByPrice = new Dictionary<decimal, decimal>();
					for (int j = i - 20; j < i; j++)
					{
						decimal priceKey = Math.Round(charts[j].Quote.Close, 2);
						if (!volumeByPrice.ContainsKey(priceKey))
							volumeByPrice[priceKey] = 0;
						volumeByPrice[priceKey] += charts[j].Quote.Volume;
					}

					// 최대 거래량 가격대 찾기
					decimal maxVolumePrice = volumeByPrice.OrderByDescending(x => x.Value).First().Key;
					bool nearVolumeNode = Math.Abs(charts[i].Quote.Low - maxVolumePrice) < maxVolumePrice * 0.01m;

					// 3. 기술적 조건
					bool maAlignment = (decimal)charts[i].Sma1.Value > (decimal)charts[i].Sma2.Value;
					bool rsiCondition = charts[i].Rsi1.Value > 45 && charts[i].Rsi1.Value < 65;
					bool volumeSpike = charts[i].Quote.Volume > charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume) * 1.8m;

					// 4. 캔들 패턴
					bool bullishCandle = charts[i].Quote.Close > charts[i].Quote.Open * 1.005m;

					// 모든 조건 조합
					if (uptrend && nearVolumeNode && maAlignment && rsiCondition && volumeSpike && bullishCandle)
					{
						var stat = AnalyzeFuture(charts, i, 15, true);
						stats.Add(stat);
					}
				}
				PrintStats("Volume Profile + Market Structure Strategy", stats);
			}
		}

		public void Run_MultiTimeframeMomentumStrategy()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 50; i < charts.Count - 15; i++)
				{
					// null 체크
					if (charts[i].Rsi1 == null || charts[i].Macd == null ||
						charts[i].MacdSignal == null || charts[i].Sma1 == null) continue;

					// 1. 상위 타임프레임 추세 확인 (4시간 = 240분)
					bool higherTimeframeUptrend = true;
					if (i >= 240)
					{
						decimal h4Open = charts[i - 240].Quote.Open;
						decimal h4Close = charts[i].Quote.Close;
						higherTimeframeUptrend = h4Close > h4Open * 1.01m;
					}

					// 2. 모멘텀 확인
					bool rsiRising = i >= 3 && charts[i].Rsi1.Value > charts[i - 3].Rsi1.Value;
					bool macdCrossover = charts[i - 1].Macd < charts[i - 1].MacdSignal &&
										charts[i].Macd > charts[i].MacdSignal;
					bool priceAboveMA = charts[i].Quote.Close > (decimal)charts[i].Sma1.Value;

					// 3. 거래량 확인
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool volumeConfirmation = charts[i].Quote.Volume > avgVolume * 2.0m;

					// 4. 시간대 필터 (뉴욕 오픈 시간대)
					int hour = charts[i].Quote.Date.Hour;
					bool optimalTimeWindow = (hour >= 13 && hour <= 15);

					// 모든 조건 조합
					if (higherTimeframeUptrend && rsiRising && macdCrossover &&
						priceAboveMA && volumeConfirmation && optimalTimeWindow)
					{
						var stat = AnalyzeFuture(charts, i, 15, true);
						stats.Add(stat);
					}
				}
				PrintStats("Multi-Timeframe Momentum Strategy", stats);
			}
		}

		public void Run_SmartMoneyConceptStrategy()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 50; i < charts.Count - 15; i++)
				{
					// null 체크
					if (charts[i].Rsi1 == null || charts[i].Bb1Upper == null ||
						charts[i].Bb1Lower == null) continue;

					// 1. 유동성 영역 식별 (최근 고점/저점 클러스터)
					decimal recentHigh = charts.Skip(i - 30).Take(30).Max(x => x.Quote.High);
					decimal recentLow = charts.Skip(i - 30).Take(30).Min(x => x.Quote.Low);

					// 2. 유동성 스윕 확인 (고점/저점 돌파 후 반전)
					bool liquiditySwipHigh = false;
					bool liquiditySwipLow = false;

					for (int j = i - 5; j < i; j++)
					{
						if (charts[j].Quote.High > recentHigh && charts[j].Quote.Close < recentHigh)
							liquiditySwipHigh = true;

						if (charts[j].Quote.Low < recentLow && charts[j].Quote.Close > recentLow)
							liquiditySwipLow = true;
					}

					// 3. 불균형(Imbalance) 확인
					bool imbalanceUp = false;
					bool imbalanceDown = false;

					for (int j = i - 10; j < i - 1; j++)
					{
						if (charts[j].Quote.Low > charts[j + 1].Quote.High)
							imbalanceDown = true;

						if (charts[j].Quote.High < charts[j + 1].Quote.Low)
							imbalanceUp = true;
					}

					// 4. 거래량 감소 확인 (기관 축적 신호)
					bool volumeDecline = true;
					for (int j = i - 5; j < i; j++)
					{
						if (charts[j].Quote.Volume > charts[j - 1].Quote.Volume * 1.2m)
						{
							volumeDecline = false;
							break;
						}
					}

					// 5. 반전 캔들 확인
					bool reversalCandle = (liquiditySwipLow && charts[i].Quote.Close > charts[i].Quote.Open * 1.01m) ||
										 (liquiditySwipHigh && charts[i].Quote.Close < charts[i].Quote.Open * 0.99m);

					// 매수 조건
					bool buyCondition = liquiditySwipLow && imbalanceUp && volumeDecline &&
									   charts[i].Rsi1.Value < 40 && reversalCandle;

					// 매도 조건
					bool sellCondition = liquiditySwipHigh && imbalanceDown && volumeDecline &&
										charts[i].Rsi1.Value > 60 && reversalCandle;

					if (buyCondition)
					{
						var stat = AnalyzeFuture(charts, i, 15, true);
						stats.Add(stat);
					}
					else if (sellCondition)
					{
						var stat = AnalyzeFuture(charts, i, 15, false);
						stats.Add(stat);
					}
				}
				PrintStats("Smart Money Concept Strategy", stats);
			}
		}

		public void Run_MarketStructureOrderBlockStrategy()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 50; i < charts.Count - 15; i++)
				{
					// null 체크
					if (charts[i].Rsi1 == null) continue;

					// 1. 시장 구조 변화 감지 (저점 돌파)
					bool structureBreak = false;
					int lastLowIndex = -1;

					for (int j = i - 20; j < i - 5; j++)
					{
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
							if (lastLowIndex != -1 && charts[j].Quote.Low < charts[lastLowIndex].Quote.Low)
								structureBreak = true;

							lastLowIndex = j;
						}
					}

					// 2. 오더블록 식별 (강한 모멘텀 캔들)
					int orderBlockIndex = -1;
					for (int j = i - 15; j < i - 3; j++)
					{
						bool strongMomentum = charts[j].Quote.Close > charts[j].Quote.Open * 1.015m;
						bool largeBody = Math.Abs(charts[j].Quote.Close - charts[j].Quote.Open) >
										(charts[j].Quote.High - charts[j].Quote.Low) * 0.7m;

						if (strongMomentum && largeBody)
						{
							orderBlockIndex = j;
							break;
						}
					}

					// 3. 오더블록 리테스트
					bool orderBlockRetest = false;
					if (orderBlockIndex != -1)
					{
						decimal obLow = charts[orderBlockIndex].Quote.Low;
						decimal obHigh = charts[orderBlockIndex].Quote.High;

						for (int j = orderBlockIndex + 1; j <= i; j++)
						{
							if (charts[j].Quote.Low < obHigh && charts[j].Quote.High > obLow)
							{
								orderBlockRetest = true;
								break;
							}
						}
					}

					// 4. RSI 다이버전스
					bool rsiDivergence = false;
					if (lastLowIndex != -1 && i - lastLowIndex <= 10)
					{
						if (charts[i].Quote.Low < charts[lastLowIndex].Quote.Low &&
							charts[i].Rsi1.Value > charts[lastLowIndex].Rsi1.Value)
							rsiDivergence = true;
					}

					// 5. 거래량 확인
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool volumeConfirmation = charts[i].Quote.Volume > avgVolume * 1.5m;

					// 모든 조건 조합
					if (structureBreak && orderBlockRetest && rsiDivergence && volumeConfirmation)
					{
						var stat = AnalyzeFuture(charts, i, 15, true);
						stats.Add(stat);
					}
				}
				PrintStats("Market Structure + Order Block Strategy", stats);
			}
		}

		public void Run_IntegratedEliteStrategy()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 50; i < charts.Count - 15; i++)
				{
					// null 체크
					if (charts[i].Rsi1 == null || charts[i].Macd == null ||
						charts[i].MacdSignal == null || charts[i].Bb1Upper == null ||
						charts[i].Bb1Lower == null || charts[i].Sma1 == null ||
						charts[i].Sma2 == null || charts[i].Atr == null) continue;

					// 1. 시장 맥락 확인
					bool isUptrend = (decimal)charts[i].Sma1.Value > (decimal)charts[i].Sma2.Value;

					// 2. 시간대 필터 (최적 거래 시간)
					int hour = charts[i].Quote.Date.Hour;
					bool optimalTimeWindow = (hour >= 8 && hour <= 16);

					// 3. 볼륨 프로파일 (거래량 집중 구간)
					var volumeByPrice = new Dictionary<decimal, decimal>();
					for (int j = i - 20; j < i; j++)
					{
						decimal priceKey = Math.Round(charts[j].Quote.Close, 2);
						if (!volumeByPrice.ContainsKey(priceKey))
							volumeByPrice[priceKey] = 0;
						volumeByPrice[priceKey] += charts[j].Quote.Volume;
					}

					decimal maxVolumePrice = volumeByPrice.OrderByDescending(x => x.Value).First().Key;
					bool nearVolumeNode = Math.Abs(charts[i].Quote.Low - maxVolumePrice) < maxVolumePrice * 0.01m;

					// 4. 기술적 지표 조합
					bool rsiFilter = isUptrend ?
									(charts[i].Rsi1.Value > 40 && charts[i].Rsi1.Value < 70) :
									(charts[i].Rsi1.Value > 30 && charts[i].Rsi1.Value < 60);

					bool macdSignal = isUptrend ?
									 (charts[i].Macd > charts[i].MacdSignal) :
									 (charts[i - 1].Macd < charts[i - 1].MacdSignal && charts[i].Macd > charts[i].MacdSignal);

					// 5. 변동성 필터
					decimal atr = charts[i].Atr.Value;
					decimal range = charts[i].Quote.High - charts[i].Quote.Low;
					bool volatilityFilter = isUptrend ?
										   (range > (atr * 1.2m)) :
										   (range < (atr * 0.8m));

					// 6. 거래량 확인
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool volumeFilter = charts[i].Quote.Volume > avgVolume * 2.0m;

					// 7. 캔들 패턴
					bool candlePattern = isUptrend ?
									   (charts[i].Quote.Close > charts[i].Quote.Open * 1.008m) :
									   (IsHammer(charts[i]));

					// 모든 조건 조합 (시장 맥락에 따라 다른 조건 적용)
					int conditionsMet = 0;
					if (optimalTimeWindow) conditionsMet++;
					if (nearVolumeNode) conditionsMet++;
					if (rsiFilter) conditionsMet++;
					if (macdSignal) conditionsMet++;
					if (volatilityFilter) conditionsMet++;
					if (volumeFilter) conditionsMet++;
					if (candlePattern) conditionsMet++;

					// 최소 5개 이상 조건 충족 시 신호 생성
					if (conditionsMet >= 5)
					{
						var stat = AnalyzeFuture(charts, i, 15, isUptrend);
						stats.Add(stat);
					}
				}
				PrintStats("Integrated Elite Strategy", stats);
			}
		}

		public void Run_VolumeProfileMarketStructureImproved()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 50; i < charts.Count - 15; i++)
				{
					// null 체크
					if (charts[i].Sma1 == null || charts[i].Sma2 == null ||
						charts[i].Bb1Upper == null || charts[i].Bb1Lower == null ||
						charts[i].Rsi1 == null) continue;

					// 1. 시장 구조 분석 (고점/저점 패턴) - 조건 완화
					var highs = new List<decimal>();
					var lows = new List<decimal>();
					for (int j = i - 30; j < i; j += 5)
					{
						highs.Add(charts.Skip(j).Take(5).Max(x => x.Quote.High));
						lows.Add(charts.Skip(j).Take(5).Min(x => x.Quote.Low));
					}

					bool higherHighs = highs.Count >= 3 && highs[highs.Count - 1] > highs[highs.Count - 3];
					bool higherLows = lows.Count >= 3 && lows[lows.Count - 1] > lows[lows.Count - 3];
					bool uptrend = higherHighs || higherLows; // OR 조건으로 완화

					// 2. 볼륨 프로파일 (거래량 집중 구간) - 범위 확대
					var volumeByPrice = new Dictionary<decimal, decimal>();
					for (int j = i - 20; j < i; j++)
					{
						decimal priceKey = Math.Round(charts[j].Quote.Close, 1); // 소수점 한 자리로 범위 확대
						if (!volumeByPrice.ContainsKey(priceKey))
							volumeByPrice[priceKey] = 0;
						volumeByPrice[priceKey] += charts[j].Quote.Volume;
					}

					// 최대 거래량 가격대 찾기 - 범위 확대
					decimal maxVolumePrice = volumeByPrice.OrderByDescending(x => x.Value).First().Key;
					bool nearVolumeNode = Math.Abs(charts[i].Quote.Low - maxVolumePrice) < maxVolumePrice * 0.015m;

					// 3. 기술적 조건 - 필수 조건만 유지
					bool maAlignment = (decimal)charts[i].Sma1.Value > (decimal)charts[i].Sma2.Value;
					bool rsiCondition = charts[i].Rsi1.Value > 40 && charts[i].Rsi1.Value < 70;
					bool volumeSpike = charts[i].Quote.Volume > charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume) * 1.5m;

					// 필수 조건만 조합 (조건 수 감소)
					if (uptrend && nearVolumeNode && (maAlignment || rsiCondition) && volumeSpike)
					{
						var stat = AnalyzeFuture(charts, i, 15, true);
						stats.Add(stat);
					}
				}
				PrintStats("Improved Volume Profile + Market Structure", stats);
			}
		}

		public void Run_MultiTimeframeMomentumImproved()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 50; i < charts.Count - 15; i++)
				{
					// null 체크
					if (charts[i].Rsi1 == null || charts[i].Macd == null ||
						charts[i].MacdSignal == null || charts[i].Sma1 == null) continue;

					// 1. 상위 타임프레임 추세 확인 (4시간 = 240분) - 조건 완화
					bool higherTimeframeUptrend = true;
					if (i >= 120) // 240 -> 120으로 완화
					{
						decimal h4Open = charts[i - 120].Quote.Open;
						decimal h4Close = charts[i].Quote.Close;
						higherTimeframeUptrend = h4Close > h4Open * 1.005m; // 1.01 -> 1.005로 완화
					}

					// 2. 모멘텀 확인 - 필수 조건만 유지
					bool rsiRising = i >= 3 && charts[i].Rsi1.Value > charts[i - 3].Rsi1.Value;
					bool macdCrossover = charts[i - 1].Macd < charts[i - 1].MacdSignal &&
										charts[i].Macd > charts[i].MacdSignal;
					bool priceAboveMA = charts[i].Quote.Close > (decimal)charts[i].Sma1.Value;

					// 3. 거래량 확인
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool volumeConfirmation = charts[i].Quote.Volume > avgVolume * 1.5m; // 2.0 -> 1.5로 완화

					// 4. 시간대 필터 (뉴욕 오픈 시간대) - 제거하여 조건 완화

					// 필수 조건만 조합
					if (higherTimeframeUptrend && (rsiRising || macdCrossover) && priceAboveMA && volumeConfirmation)
					{
						var stat = AnalyzeFuture(charts, i, 15, true);
						stats.Add(stat);
					}
				}
				PrintStats("Improved Multi-Timeframe Momentum", stats);
			}
		}

		public void Run_MarketStructureOrderBlockImproved()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 50; i < charts.Count - 15; i++)
				{
					// null 체크 (ATR 필수)
					if (charts[i].Rsi1 == null || charts[i].Atr == null) continue;

					// 1. 시장 구조 변화 감지 (저점 돌파)
					bool structureBreak = false;
					int lastLowIndex = -1;

					for (int j = i - 20; j < i - 3; j++)
					{
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
								// ATR 기반 돌파 감지 (1% 대신 0.5배 ATR 사용)
								decimal atr = charts[j].Atr.Value;
								if (charts[j].Quote.Low < charts[lastLowIndex].Quote.Low - (atr * 0.5m))
									structureBreak = true;
							}
							lastLowIndex = j;
						}
					}

					// 2. 오더블록 식별 (강한 모멘텀 캔들)
					int orderBlockIndex = -1;
					for (int j = i - 15; j < i - 2; j++)
					{
						// 몸통 크기 (ATR 기반)
						decimal atr = charts[j].Atr.Value;
						decimal body = Math.Abs(charts[j].Quote.Close - charts[j].Quote.Open);
						bool strongMomentum = body > (atr * 0.3m);
						bool largeBody = body > charts.Skip(i - 20).Take(20)
							.Average(c => Math.Abs(c.Quote.Close - c.Quote.Open)) * 1.5m;

						if (strongMomentum || largeBody)
						{
							orderBlockIndex = j;
							break;
						}
					}

					// 3. 오더블록 리테스트 (ATR 기반 범위)
					bool orderBlockRetest = false;
					if (orderBlockIndex != -1)
					{
						decimal obLow = charts[orderBlockIndex].Quote.Low;
						decimal obHigh = charts[orderBlockIndex].Quote.High;
						decimal atr = charts[orderBlockIndex].Atr.Value;

						for (int j = orderBlockIndex + 1; j <= i; j++)
						{
							// ATR 기반 범위 (1% 대신 0.5배 ATR 사용)
							decimal buffer = (atr * 0.5m);
							if (charts[j].Quote.Low < obHigh + buffer &&
								charts[j].Quote.High > obLow - buffer)
							{
								orderBlockRetest = true;
								break;
							}
						}
					}

					// 4. RSI 조건
					bool rsiDivergence = false;
					bool rsiOversold = charts[i].Rsi1.Value < 40;

					if (lastLowIndex != -1 && i - lastLowIndex <= 15)
					{
						if (charts[i].Quote.Low < charts[lastLowIndex].Quote.Low &&
							charts[i].Rsi1.Value > charts[lastLowIndex].Rsi1.Value)
							rsiDivergence = true;
					}

					// 5. 거래량 확인 (ATR 상대적)
					decimal avgAtr = charts.Skip(i - 20).Take(20).Average(c => c.Atr.Value);
					bool volumeConfirmation = charts[i].Quote.Volume > charts.Skip(i - 20)
						.Take(20).Average(x => x.Quote.Volume) * (1 + avgAtr * 0.1m);

					// 조건 조합
					if (structureBreak && orderBlockRetest && (rsiDivergence || rsiOversold) && volumeConfirmation)
					{
						var stat = AnalyzeFuture(charts, i, 15, true);
						stats.Add(stat);
					}
				}
				PrintStats("Improved Market Structure + Order Block (ATR-based)", stats);
			}
		}

		public enum MarketRegime { Trend, Range }

		public MarketRegime DetectMarketRegime(IList<ChartInfo> charts, int idx, int maLen = 50, decimal baseSlopeSens = 0.0005m, decimal baseAtrRangeThresh = 0.18m)
		{
			if (idx < maLen) return MarketRegime.Range;

			// MA 계산
			decimal maNow = charts.Skip(idx - maLen + 1).Take(maLen).Average(x => x.Quote.Close);
			decimal maPrev = charts.Skip(idx - maLen).Take(maLen).Average(x => x.Quote.Close);
			decimal slope = Math.Abs((maNow - maPrev) / maPrev);

			// Range, ATR 계산
			decimal hi = charts.Skip(idx - maLen + 1).Take(maLen).Max(x => x.Quote.High);
			decimal lo = charts.Skip(idx - maLen + 1).Take(maLen).Min(x => x.Quote.Low);
			decimal priceRange = hi - lo;
			decimal atr = charts[idx].Atr ?? 0;
			decimal atrRangeRatio = priceRange > 0 ? atr / priceRange : 0;

			// 동적 임계값 계산
			// 변동성이 낮으면 slopeSens를 더 민감하게(낮게), atrRangeThresh를 더 관대하게(낮게)
			// 변동성이 높으면 slopeSens를 덜 민감하게(높게), atrRangeThresh를 더 엄격하게(높게)
			decimal dynamicSlopeSens = baseSlopeSens * (1 + (1 - atrRangeRatio));
			decimal dynamicAtrRangeThresh = baseAtrRangeThresh * (1 + atrRangeRatio);

			// 판정
			if (slope > dynamicSlopeSens && atrRangeRatio > dynamicAtrRangeThresh)
				return MarketRegime.Trend;
			else
				return MarketRegime.Range;
		}



		public void Run_MarketAdaptiveStrategy()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 60; i < charts.Count - 15; i++)
				{
					var regime = DetectMarketRegime(charts, i);

					if (regime == MarketRegime.Trend)
					{
						// null 체크 (ATR 필수)
						if (charts[i].Rsi1 == null || charts[i].Atr == null) continue;

						// 1. 시장 구조 변화 감지 (저점 돌파)
						bool structureBreak = false;
						int lastLowIndex = -1;

						for (int j = i - 20; j < i - 3; j++)
						{
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
									// ATR 기반 돌파 감지 (1% 대신 0.5배 ATR 사용)
									decimal atr = charts[j].Atr.Value;
									if (charts[j].Quote.Low < charts[lastLowIndex].Quote.Low - (atr * 0.5m))
										structureBreak = true;
								}
								lastLowIndex = j;
							}
						}

						// 2. 오더블록 식별 (강한 모멘텀 캔들)
						int orderBlockIndex = -1;
						for (int j = i - 15; j < i - 2; j++)
						{
							// 몸통 크기 (ATR 기반)
							decimal atr = charts[j].Atr.Value;
							decimal body = Math.Abs(charts[j].Quote.Close - charts[j].Quote.Open);
							bool strongMomentum = body > (atr * 0.3m);
							bool largeBody = body > charts.Skip(i - 20).Take(20)
								.Average(c => Math.Abs(c.Quote.Close - c.Quote.Open)) * 1.5m;

							if (strongMomentum || largeBody)
							{
								orderBlockIndex = j;
								break;
							}
						}

						// 3. 오더블록 리테스트 (ATR 기반 범위)
						bool orderBlockRetest = false;
						if (orderBlockIndex != -1)
						{
							decimal obLow = charts[orderBlockIndex].Quote.Low;
							decimal obHigh = charts[orderBlockIndex].Quote.High;
							decimal atr = charts[orderBlockIndex].Atr.Value;

							for (int j = orderBlockIndex + 1; j <= i; j++)
							{
								// ATR 기반 범위 (1% 대신 0.5배 ATR 사용)
								decimal buffer = (atr * 0.5m);
								if (charts[j].Quote.Low < obHigh + buffer &&
									charts[j].Quote.High > obLow - buffer)
								{
									orderBlockRetest = true;
									break;
								}
							}
						}

						// 4. RSI 조건
						bool rsiDivergence = false;
						bool rsiOversold = charts[i].Rsi1.Value < 40;

						if (lastLowIndex != -1 && i - lastLowIndex <= 15)
						{
							if (charts[i].Quote.Low < charts[lastLowIndex].Quote.Low &&
								charts[i].Rsi1.Value > charts[lastLowIndex].Rsi1.Value)
								rsiDivergence = true;
						}

						// 5. 거래량 확인 (ATR 상대적)
						decimal avgAtr = charts.Skip(i - 20).Take(20).Average(c => c.Atr.Value);
						bool volumeConfirmation = charts[i].Quote.Volume > charts.Skip(i - 20)
							.Take(20).Average(x => x.Quote.Volume) * (1 + avgAtr * 0.1m);

						// 조건 조합
						if (structureBreak && orderBlockRetest && (rsiDivergence || rsiOversold) && volumeConfirmation)
						{
							var stat = AnalyzeFuture(charts, i, 15, true);
							stats.Add(stat);
						}
					}
					else
					{
						// 횡보장 → 평균회귀(Mean Reversion) 전략 예시
						if (charts[i].Bb1Lower == null || charts[i].Bb1Upper == null || charts[i].Rsi1 == null)
						{
							continue;
						}
						bool lowerTouch = charts[i].Quote.Close < (decimal)charts[i].Bb1Lower;
						bool rsiLow = charts[i].Rsi1 < 35;
						var avgVol = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
						bool volSpike = charts[i].Quote.Volume > avgVol * 1.2m;
						if (lowerTouch && rsiLow && volSpike)
						{
							stats.Add(AnalyzeFuture(charts, i, 10, true));
						}
					}
				}
				PrintStats("Market Adaptive Strategy", stats);
			}
		}


		public void Run_HybridEliteStrategy()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 50; i < charts.Count - 15; i++)
				{
					// null 체크
					if (charts[i].Rsi1 == null || charts[i].Macd == null ||
						charts[i].MacdSignal == null || charts[i].Bb1Upper == null ||
						charts[i].Bb1Lower == null || charts[i].Sma1 == null) continue;

					// 1. 시장 맥락 확인
					bool isUptrend = (decimal)charts[i].Sma1.Value > charts[i - 20].Quote.Close;

					// 2. 볼륨 프로파일 (거래량 집중 구간)
					var volumeByPrice = new Dictionary<decimal, decimal>();
					for (int j = i - 20; j < i; j++)
					{
						decimal priceKey = Math.Round(charts[j].Quote.Close, 1);
						if (!volumeByPrice.ContainsKey(priceKey))
							volumeByPrice[priceKey] = 0;
						volumeByPrice[priceKey] += charts[j].Quote.Volume;
					}

					decimal maxVolumePrice = volumeByPrice.OrderByDescending(x => x.Value).First().Key;
					bool nearVolumeNode = Math.Abs(charts[i].Quote.Low - maxVolumePrice) < maxVolumePrice * 0.015m;

					// 3. 기술적 지표 조합
					bool rsiFilter = isUptrend ?
									(charts[i].Rsi1.Value > 40 && charts[i].Rsi1.Value < 70) :
									(charts[i].Rsi1.Value > 30 && charts[i].Rsi1.Value < 60);

					bool macdSignal = isUptrend ?
									 (charts[i].Macd > charts[i].MacdSignal) :
									 (charts[i - 1].Macd < charts[i - 1].MacdSignal && charts[i].Macd > charts[i].MacdSignal);

					// 4. 거래량 확인
					var avgVolume = charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool volumeFilter = charts[i].Quote.Volume > avgVolume * 1.5m;

					// 5. 캔들 패턴
					bool candlePattern = isUptrend ?
									   (charts[i].Quote.Close > charts[i].Quote.Open * 1.005m) :
									   (IsHammer(charts[i]));

					// 조건 조합 - 필수 조건 + 선택 조건
					bool essentialConditions = isUptrend && volumeFilter; // 필수 조건
					int optionalConditionsCount = 0;

					if (nearVolumeNode) optionalConditionsCount++;
					if (rsiFilter) optionalConditionsCount++;
					if (macdSignal) optionalConditionsCount++;
					if (candlePattern) optionalConditionsCount++;

					// 필수 조건 + 선택 조건 중 2개 이상 충족
					if (essentialConditions && optionalConditionsCount >= 2)
					{
						var stat = AnalyzeFuture(charts, i, 15, isUptrend);
						stats.Add(stat);
					}
				}
				PrintStats("Hybrid Elite Strategy", stats);
			}
		}

		public void Run_AdaptiveMarketRegimeStrategy()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 50; i < charts.Count - 15; i++)
				{
					// null 체크
					if (charts[i].Rsi1 == null || charts[i].Bb1Upper == null ||
						charts[i].Bb1Lower == null || charts[i].Sma1 == null) continue;

					// 1. 시장 레짐 판단 (추세/횡보/변동성)
					decimal bandwidthCurrent = 0;
					decimal bandwidthPast = 0;

					if (charts[i].Bb1Upper != null && charts[i].Bb1Lower != null &&
						charts[i - 20].Bb1Upper != null && charts[i - 20].Bb1Lower != null)
					{
						bandwidthCurrent = charts[i].Bb1Upper.Value - charts[i].Bb1Lower.Value;
						bandwidthPast = charts[i - 20].Bb1Upper.Value - charts[i - 20].Bb1Lower.Value;
					}

					bool isTrending = charts[i].Sma1.Value > charts[i - 20].Quote.Close * 1.02m ||
									 charts[i].Sma1.Value < charts[i - 20].Quote.Close * 0.98m;
					bool isRangebound = bandwidthCurrent < bandwidthPast * 0.8m;
					bool isVolatile = bandwidthCurrent > bandwidthPast * 1.2m;

					// 2. 레짐별 전략 적용
					bool signalGenerated = false;

					// 추세 레짐: 추세 추종 전략
					if (isTrending)
					{
						bool trendFollowSignal = charts[i].Quote.Close > charts[i].Sma1.Value &&
												charts[i].Rsi1.Value > 50 &&
												charts[i].Quote.Volume > charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume) * 1.5m;

						if (trendFollowSignal)
						{
							var stat = AnalyzeFuture(charts, i, 15, true);
							stats.Add(stat);
							signalGenerated = true;
						}
					}
					// 횡보 레짐: 평균 회귀 전략
					else if (isRangebound)
					{
						bool meanReversionSignal = charts[i].Quote.Close < charts[i].Bb1Lower.Value * 1.01m &&
												  charts[i].Rsi1.Value < 40 &&
												  charts[i].Quote.Volume > charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume) * 1.3m;

						if (meanReversionSignal)
						{
							var stat = AnalyzeFuture(charts, i, 15, true);
							stats.Add(stat);
							signalGenerated = true;
						}
					}
					// 변동성 레짐: 돌파 전략
					else if (isVolatile)
					{
						bool breakoutSignal = charts[i].Quote.Close > charts[i].Bb1Upper.Value * 0.99m &&
											 charts[i].Quote.Volume > charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume) * 1.8m;

						if (breakoutSignal)
						{
							var stat = AnalyzeFuture(charts, i, 15, true);
							stats.Add(stat);
							signalGenerated = true;
						}
					}

					// 레짐 판단이 불확실한 경우: 기본 전략
					if (!signalGenerated && !isTrending && !isRangebound && !isVolatile)
					{
						bool defaultSignal = charts[i].Rsi1.Value > 40 && charts[i].Rsi1.Value < 60 &&
											charts[i].Quote.Volume > charts.Skip(i - 20).Take(20).Average(x => x.Quote.Volume) * 1.5m;

						if (defaultSignal)
						{
							var stat = AnalyzeFuture(charts, i, 15, true);
							stats.Add(stat);
						}
					}
				}
				PrintStats("Adaptive Market Regime Strategy", stats);
			}
		}

		public void Run_VwapStochRsiPullback()
		{
			foreach (var cp in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var ch = cp.Charts;
				for (int i = 20; i < ch.Count - 10; i++)
				{
					if (ch[i].Vwap == null || ch[i].StochK == null) continue;
					// 1) VWAP 아래 돌파 후 리테스트
					bool vwapBreak = ch[i - 1].Quote.Close > (decimal)ch[i - 1].Vwap &&
									 ch[i].Quote.Close < (decimal)ch[i].Vwap;
					bool vwapRetest = ch[i + 1].Quote.Close > (decimal)ch[i].Vwap;
					// 2) Stochastic RSI 과매도 반등
					bool stochRsiOk = ch[i].StochK.Value <= 20 &&
									  ch[i + 1].StochK.Value > ch[i].StochK.Value;
					// 3) 거래량 스파이크
					var avgVol = ch.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool volSpike = ch[i].Quote.Volume > avgVol * 1.8m;

					if (vwapBreak && vwapRetest && stochRsiOk && volSpike)
					{
						stats.Add(AnalyzeFuture(ch, i, 12, true));
					}
				}
				PrintStats("VWAP Pullback + StochRSI Oversold", stats);
			}
		}

		public void Run_ElderRayEmaRibbonDivergence()
		{
			foreach (var cp in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var ch = cp.Charts;
				for (int i = 21; i < ch.Count - 10; i++)
				{
					if (ch[i].ElderRayBullPower == null || ch[i].ElderRayBearPower == null) continue;
					// 1) EMA 리본 상승 정렬
					bool emaTrend = ch[i].Ema1 > ch[i].Ema2 && ch[i].Ema2 > ch[i].Ema3;
					// 2) Bull Power 다이버전스 (가격 저점 갱신, Bull Power 미갱신)
					bool divergence = ch[i].Quote.Low < ch[i - 5].Quote.Low &&
									  ch[i].ElderRayBullPower.Value >= ch[i - 5].ElderRayBullPower.Value;
					// 3) 강세 캔들 & 거래량 스파이크
					bool bullCandle = ch[i].Quote.Close > ch[i].Quote.Open;
					var avgVol = ch.Skip(i - 20).Take(20).Average(x => x.Quote.Volume);
					bool volSpike = ch[i].Quote.Volume > avgVol * 2.0m;

					if (emaTrend && divergence && bullCandle && volSpike)
					{
						stats.Add(AnalyzeFuture(ch, i, 15, true));
					}
				}
				PrintStats("Elder-Ray Divergence + EMA Ribbon", stats);
			}
		}

		public void Run_VwapStochRsiPullbackImproved()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 14; i < charts.Count - 11; i++)  // i+1 사용 고려[2]
				{
					// 필수 지표 null 검사
					if (charts[i - 1].RollingVwap == null ||
						charts[i].RollingVwap == null ||
						charts[i + 1].RollingVwap == null ||
						charts[i].StochK == null ||
						charts[i + 1].StochK == null)
						continue;  // null시 건너뛰기[1]

					// 1) 이동식 VWAP 돌파 후 되돌림
					decimal prevVwap = charts[i - 1].RollingVwap.Value;
					decimal currVwap = charts[i].RollingVwap.Value;
					bool breakBelow = charts[i - 1].Quote.Close > prevVwap &&
									   charts[i].Quote.Close < currVwap;
					bool retestAbove = charts[i + 1].Quote.Close > currVwap;

					// 2) StochRSI 과매도(≤20) → 중립권(>50) 반등
					decimal prevStoch = charts[i].StochK.Value;
					decimal nextStoch = charts[i + 1].StochK.Value;
					bool stochSignal = prevStoch <= 20 && nextStoch > 50;

					// 3) 거래량 스파이크
					var avgVol = charts.Skip(i - 14).Take(14)
									   .Average(c => c.Quote.Volume);
					bool volSpike = charts[i].Quote.Volume > avgVol * 1.5m;

					if (breakBelow && retestAbove && stochSignal && volSpike)
						stats.Add(AnalyzeFuture(charts, i, 12, true));
				}

				PrintStats("Revised VWAP Pullback + StochRSI", stats);
			}
		}

		public void Run_ElderRayEmaRibbonDivergenceImproved()
		{
			foreach (var chartPack in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var charts = chartPack.Charts;

				for (int i = 21; i < charts.Count - 12; i++)
				{
					// 필수 지표 null 검사
					if (charts[i].Ema1 == null ||
						charts[i].Ema2 == null ||
						charts[i].Ema3 == null ||
						charts[i].ElderRayBullPower == null ||
						charts[i - 5].ElderRayBullPower == null)
						continue;  // null시 건너뛰기[1]

					// 1) EMA 리본 정렬
					decimal ema1 = charts[i].Ema1.Value;
					decimal ema2 = charts[i].Ema2.Value;
					decimal ema3 = charts[i].Ema3.Value;
					bool ribbonUp = (ema1 >= ema2) && (ema2 >= ema3);

					// 2) Bull Power 다이버전스
					decimal currLow = charts[i].Quote.Low;
					decimal prevLow = charts[i - 5].Quote.Low;
					decimal currBull = charts[i].ElderRayBullPower.Value;
					decimal prevBull = charts[i - 5].ElderRayBullPower.Value;
					bool divergence = currLow < prevLow && currBull >= prevBull;

					// 3) 강세 캔들 + 거래량 스파이크
					bool bullCandle = charts[i].Quote.Close > charts[i].Quote.Open;
					var avgVol = charts.Skip(i - 20).Take(20)
										   .Average(c => c.Quote.Volume);
					bool volSpike = charts[i].Quote.Volume > avgVol * 1.8m;

					if (ribbonUp && divergence && bullCandle && volSpike)
						stats.Add(AnalyzeFuture(charts, i, 15, true));
				}

				PrintStats("Revised Elder-Ray + EMA Ribbon", stats);
			}
		}

		public void Run_EvwapStochRsiPullback()
		{
			foreach (var cp in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var ch = cp.Charts;
				for (int i = 14; i < ch.Count - 10; i++)
				{
					// null 검사
					var prevVW = ch[i - 1].RollingVwap.GetValueOrDefault(0);
					var curVW = ch[i].RollingVwap.GetValueOrDefault(0);
					var nextVW = ch[i + 1].RollingVwap.GetValueOrDefault(0);
					var curSt = ch[i].StochK.GetValueOrDefault(0);
					var nxtSt = ch[i + 1].StochK.GetValueOrDefault(0);

					// EVWAP 풀백
					bool breakBelow = ch[i - 1].Quote.Close > (decimal)prevVW
									&& ch[i].Quote.Close < (decimal)curVW;
					bool retestAbove = ch[i + 1].Quote.Close > (decimal)curVW;

					// StochRSI 반등
					bool stochSignal = curSt <= 20 && nxtSt >= 30;

					// 거래량 스파이크 (double로 연산 후 decimal 캐스팅)
					double avgVolD = (double)ch.Skip(i - 14).Take(14)
									   .Average(c => c.Quote.Volume);
					decimal avgVolM = (decimal)avgVolD;
					bool volSpike = ch[i].Quote.Volume > avgVolM * 1.3m;

					if (breakBelow && retestAbove && stochSignal && volSpike)
						stats.Add(AnalyzeFuture(ch, i, 12, true));
				}
				PrintStats("EVWAP Pullback + StochRSI", stats);
			}
		}



		public void Run_RevisedElderRayEmaRibbon()
		{
			foreach (var cp in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var ch = cp.Charts;

				for (int i = 21; i < ch.Count - 12; i++)
				{
					if (ch[i].Ema1 == null || ch[i].Ema2 == null
						|| ch[i].Ema3 == null || ch[i].ElderRayBullPower == null)
						continue;

					decimal e1 = ch[i].Ema1.Value;
					decimal e2 = ch[i].Ema2.Value;
					decimal e3 = ch[i].Ema3.Value;
					bool ribbonUp = e1 >= e2 * 1.002m && e2 >= e3 * 1.002m;

					bool divergence = ch[i].Quote.Low < ch[i - 5].Quote.Low
								   && ch[i].ElderRayBullPower.Value >= ch[i - 5].ElderRayBullPower.Value * 0.95m;

					bool bullCandle = ch[i].Quote.Close > ch[i].Quote.Open;
					var avgVol = ch.Skip(i - 20).Take(20).Average(c => c.Quote.Volume);
					bool volSpike = ch[i].Quote.Volume > avgVol * 1.6m;

					if (ribbonUp && divergence && bullCandle && volSpike)
						stats.Add(AnalyzeFuture(ch, i, 15, true));
				}
				PrintStats("Revised Elder-Ray + EMA Ribbon", stats);
			}
		}

		public void Run_CompositeAtrVwapRsi()
		{
			foreach (var cp in ChartPacks)
			{
				var stats = new List<PatternStat>();
				var ch = cp.Charts;

				for (int i = 20; i < ch.Count - 10; i++)
				{
					if (ch[i].Atr == null || ch[i].RollingVwap == null || ch[i].Rsi1 == null)
						continue;

					decimal atr = ch[i].Atr.Value;
					decimal range = ch[i].Quote.High - ch[i].Quote.Low;
					bool atrBreak = atr > 0 && range > (atr * 1.3m);

					bool vwapBreak = ch[i].Quote.Close > ch[i].RollingVwap.Value;
					bool rsiOk = ch[i].Rsi1.Value >= 40 && ch[i].Rsi1.Value <= 60;

					var avgVol = ch.Skip(i - 20).Take(20).Average(c => c.Quote.Volume);
					bool volSpike = ch[i].Quote.Volume > avgVol * 2.0m;

					if (atrBreak && vwapBreak && rsiOk && volSpike)
						stats.Add(AnalyzeFuture(ch, i, 10, true));
				}
				PrintStats("ATR+VWAP+RSI+Volume", stats);
			}
		}


		#region SubMethod
		// Hammer 패턴 (단순화)
		public static bool IsHammer(ChartInfo c)
		{
			var body = Math.Abs(c.Quote.Open - c.Quote.Close);
			var lowerWick = Math.Min(c.Quote.Open, c.Quote.Close) - c.Quote.Low;
			var upperWick = c.Quote.High - Math.Max(c.Quote.Open, c.Quote.Close);
			return lowerWick > body * 2 && upperWick < body * 0.5m;
		}

		// Shooting Star 패턴 (단순화)
		public static bool IsShootingStar(ChartInfo c)
		{
			var body = Math.Abs(c.Quote.Open - c.Quote.Close);
			var upperWick = c.Quote.High - Math.Max(c.Quote.Open, c.Quote.Close);
			var lowerWick = Math.Min(c.Quote.Open, c.Quote.Close) - c.Quote.Low;
			return upperWick > body * 2 && lowerWick < body * 0.5m;
		}

		// Engulfing 패턴
		public bool IsBullishEngulfing(ChartInfo prev, ChartInfo curr)
		{
			return prev.Quote.Close < prev.Quote.Open &&
				   curr.Quote.Close > curr.Quote.Open &&
				   curr.Quote.Open < prev.Quote.Close &&
				   curr.Quote.Close > prev.Quote.Open;
		}

		// 패턴 출현 후 N봉 동안 최대 상승/하락률, 반전 성공률 계산
		public static PatternStat AnalyzeFuture(IList<ChartInfo> charts, int idx, int lookahead, bool isLong)
		{
			var entry = charts[idx].Quote.Close;
			var maxUp = decimal.MinValue;
			var maxDown = decimal.MaxValue;

			for (int i = 1; i <= lookahead && idx + i < charts.Count; i++)
			{
				var rate = (charts[idx + i].Quote.Close - entry) / entry * 100.0m;
				if (rate > maxUp) maxUp = rate;
				if (rate < maxDown) maxDown = rate;
			}
			// 반전 성공: Hammer는 1% 이상 상승, Shooting Star는 -1% 이하 하락이 있으면 true
			bool isReversal = isLong ? maxUp > 0.3m : maxDown < -0.3m;

			return new PatternStat
			{
				Time = charts[idx].Quote.Date,
				EntryPrice = entry,
				MaxUpRate = maxUp,
				MaxDownRate = maxDown,
				IsReversal = isReversal
			};
		}

		// 통계 출력
		public static void PrintStats(string title, List<PatternStat> stats)
		{
			string fileName = "C:\\Users\\Gaten\\Desktop\\pattern_stats.csv";
			bool fileExists = File.Exists(fileName);

			using var sw = new StreamWriter(fileName, append: true, Encoding.UTF8);
			if (!fileExists)
			{
				sw.WriteLine("Title,Count,AvgMaxUp,AvgMaxDown,ReversalRate");
			}

			if (stats.Count == 0)
			{
				sw.WriteLine($"{title},0");
			}
			else if (stats.Count < 100)
			{
				sw.WriteLine($"{title},{stats.Count},통계 수집이 충분하지 않습니다.");
			}
			else
			{
				var avgMaxUp = stats.Average(x => x.MaxUpRate);
				var avgMaxDown = stats.Average(x => x.MaxDownRate);
				var reversalRate = stats.Count(x => x.IsReversal) / (decimal)stats.Count * 100.0m;

				var sample = stats[0];
				sw.WriteLine($"{title},{stats.Count},{avgMaxUp:F2},{avgMaxDown:F2},{reversalRate:F2}");
			}
		}

		#endregion

	}

}
