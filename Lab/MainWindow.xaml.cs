using Binance.Net.Enums;

using Mercury.Charts;
using Mercury.Extensions;
using Mercury.Maths;
using Mercury.StatisticalAnalyses;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Lab
{
	/// <summary>
	/// TODO
	/// CubeAlgorithmCreator
	/// SC Mini
	/// </summary>
	public partial class MainWindow : Window
	{
		string[] symbols = [
			"BTCUSDT",
"ETHUSDT",
"BCHUSDT",
"XRPUSDT",
"EOSUSDT",
"LTCUSDT",
"TRXUSDT",
"ETCUSDT",
"XLMUSDT",
"ADAUSDT",
"XMRUSDT",
"DASHUSDT",
"ZECUSDT",
"XTZUSDT",
"BNBUSDT",
"ATOMUSDT",
"ONTUSDT",
"IOTAUSDT",
"BATUSDT",
"VETUSDT",
"NEOUSDT",
"QTUMUSDT",
"IOSTUSDT",
"THETAUSDT",
"ALGOUSDT",
"ZILUSDT",
"KNCUSDT",
"ZRXUSDT",
"COMPUSDT",
"OMGUSDT"
		];


		public MainWindow()
		{
			InitializeComponent();

			var startTime = new DateTime(2010, 1, 1);
			var endTime = new DateTime(2025, 3, 1);
			var symbol = "BTCUSDT";
			var interval = KlineInterval.OneMonth;

			//LocalApi.Init();
			//var quotes = LocalApi.GetOneDayQuotes(symbol);
			ChartLoader.InitCharts(symbol, interval, startTime, endTime);
			var quotes = ChartLoader.GetChartPack(symbol, interval).Charts;
			var open = quotes.Select(x => (double)x.Quote.Open).ToArray();
			var close = quotes.Select(x => (double)x.Quote.Close).ToArray();
			var high = quotes.Select(x => (double)x.Quote.High).ToArray();
			var low = quotes.Select(x => (double)x.Quote.Low).ToArray();
			var volume = quotes.Select(x => (double)x.Quote.Volume).ToArray();

			//var vwap = ArrayCalculator.Vwap(high.ToNullable(), low.ToNullable(), close.ToNullable(), volume.ToNullable());
			//var rvwap = ArrayCalculator.RollingVwap(high.ToNullable(), low.ToNullable(), close.ToNullable(), volume.ToNullable(), 20);
			//var stoch = ArrayCalculator.StochasticRsi(close, 3, 3, 14, 14);

			var dema = ArrayCalculator.Dema(close.ToNullable(), 20);
			var ema = ArrayCalculator.Ema(close.ToNullable(), 20);
			var cci = ArrayCalculator.Cci(high, low, close, 20);
			var atr = ArrayCalculator.Atr(high, low, close, 14);
			var atrv = ArrayCalculator.AtrVolume(high, low, close, volume, 14);
			var res = ArrayCalculator.Stochastic(high, low, close, 10, 6, 6);

			ChartLoader.Charts = [];
			List<ChartPack> chartPacks = [];
			for (int i = 0; i < symbols.Length; i++)
			{
				ChartLoader.InitCharts(symbols[i], interval, startTime, endTime);
			}
			for (int i = 0; i < symbols.Length; i++)
			{
				chartPacks.Add(ChartLoader.GetChartPack(symbols[i], interval));
			}
			for (int i = 0; i < chartPacks.Count; i++)
			{
				chartPacks[i].UseIchimokuCloud();
				chartPacks[i].UseRsi(14);
				chartPacks[i].UseSma(20, 50);
				chartPacks[i].UseBollingerBands(20, 2, QuoteType.Close);
				chartPacks[i].UseStochasticRsi(14, 14, 3, 3);
				chartPacks[i].UseMacd(12, 26, 9);
				chartPacks[i].UseAtr(14);
				chartPacks[i].UseVwap();
				chartPacks[i].UseRollingVwap(20);
				//chartPacks[i].UseEvwap(13);
				chartPacks[i].UseElderRayPower(13);
			}
			//var model = new Correlation();
			//model.Init(chartPacks);^
			//var result = model.Run();

			var model2 = new CandleVolume1();
			model2.Init(chartPacks);

			model2.Run_MarketAdaptiveStrategy();

			model2.Run_MarketStructureOrderBlockImproved();
			model2.Run_BollingerUpperRsiVolumeStat();
			model2.Run_MarketContextStrategy(); // Sma20, 50

			//model2.Run_ThreeWhiteSoldiersStat();
			//model2.Run_EngulfingStat();
			//model2.Run_RSIDivergenceStat();
			//model2.Run_WedgeBreakoutStat();
			//model2.Run_BollingerBandBreakStat();
			//model2.Run_RsiHammerVolumeStat();
			//model2.Run_BollingerLowerRsiVolumeStat();
			//model2.Run_StochHammerVolumeStat();
			//model2.Run_MacdCrossVolumeStat();
			model2.Run_BollingerUpperRsiVolumeStat();
			//model2.Run_VolumeDivergence();
			//model2.Run_SupportResistanceBreak();
			model2.Run_MacdRsiConvergence();
			//model2.Run_BollingerSqueeze();
			//model2.Run_DivergenceCandleVolume();
			model2.Run_VolatilityBreakout();
			//model2.Run_SupportResistanceCluster();
			//model2.Run_TrendFilterEngulfing();
			//model2.Run_TrendPullbackStrategy(); // Sma50, 200 사용
			//model2.Run_MacdVolumeWeighted();
			//model2.Run_BreakoutRetest();
			//model2.Run_RsiDivergencePriceAction();
			//model2.Run_BollingerSqueezeBreakout();
			//model2.Run_VolumeSpikeWithTrendDivergence();
			//model2.Run_MomentumReversalCluster();
			//model2.Run_CompositeATRBreakout();
			//model2.Run_SessionOpenBreakout();
			model2.Run_HighConfidenceBreakout();
			//model2.Run_VolumeMomentumDivergence();
			//model2.Run_SupportResistanceConfluence();
			//model2.Run_AdvancedATRVolume();
			//model2.Run_SessionVolumeTiming();
			//model2.Run_ThreeTouchBreakoutStrategy();
			//model2.Run_DoubleBottomVolumeStrategy();
			//model2.Run_MeanReversionFibonacciStrategy();
			model2.Run_IchimokuBreakoutMomentumStrategy();
			model2.Run_CompositeSignalStrategy();
			model2.Run_MarketContextStrategy(); // Sma 20,50 사용
												//model2.Run_VolumeProfileMarketStructure();
												//model2.Run_MultiTimeframeMomentumStrategy();
												//model2.Run_SmartMoneyConceptStrategy();
												//model2.Run_MarketStructureOrderBlockStrategy();		
			model2.Run_IntegratedEliteStrategy(); // Sma1, 2 사용
			model2.Run_VolumeProfileMarketStructureImproved(); // Sma1, 2 사용
			model2.Run_MultiTimeframeMomentumImproved(); // Sma1 사용
			model2.Run_MarketStructureOrderBlockImproved();
			model2.Run_HybridEliteStrategy(); // Sma1 사용
			model2.Run_AdaptiveMarketRegimeStrategy();  // Sma1 사용

			//model2.Run_VwapStochRsiPullbackImproved();
			//model2.Run_ElderRayEmaRibbonDivergenceImproved(); // Ema1, 2, 3 사용
			//model2.Run_EvwapStochRsiPullback();
			//model2.Run_RevisedElderRayEmaRibbon();
			//model2.Run_CompositeAtrVwapRsi();



			//var a1 = ArrayCalculator.Atr(high, low, close, 14);
			//var a2 = ArrayCalculator.Etr(high, low, close, 14);
			//var a3 = ArrayCalculator.Eatr(high, low, close, 14, 14);

			//var fr = Calculator.FibonacciRetracementLevels(1000, 5000);

			//var fibLevels = new decimal[]
			//{
			//		fr.Level0,
			//		fr.Level236,
			//		fr.Level382,
			//		fr.Level500,
			//		fr.Level618,
			//		fr.Level786,
			//		fr.Level1000
			//};
			//var zone = GetFibonacciZone(2100, fibLevels);
			//if (zone != null)
			//{
			//	int lower = zone.Value.LowerIdx;
			//	int upper = zone.Value.UpperIdx;

			//	decimal stopLoss = fibLevels[lower]; // 하단 레벨
			//	decimal takeProfit = fibLevels[upper]; // 상단 레벨
			//}
		}

		//private (int LowerIdx, int UpperIdx)? GetFibonacciZone(decimal price, decimal[] fibLevels)
		//{
		//	// 피보나치 레벨은 보통 고가~저가 방향이기 때문에 내림차순일 수 있음
		//	for (int j = 0; j < fibLevels.Length - 1; j++)
		//	{
		//		// 구간: fibLevels[j+1] < price <= fibLevels[j] (내림차순)
		//		if (price <= fibLevels[j] && price > fibLevels[j + 1])
		//			return (j + 1, j); // 하단, 상단 인덱스 반환
		//							   // 오름차순이면 부등호 반대로
		//		if (price >= fibLevels[j] && price < fibLevels[j + 1])
		//			return (j, j + 1);
		//	}
		//	return null; // 구간 밖
		//}
	}
}
