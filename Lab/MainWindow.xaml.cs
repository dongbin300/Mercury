using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models;
using Binance.Net.Objects.Models.Futures.Socket;
using Binance.Net.Objects.Models.Spot;

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects.Sockets;

using Mercury;
using Mercury.Apis;
using Mercury.Backtests.Calculators;
using Mercury.Charts;
using Mercury.Cryptos.Binance;
using Mercury.Extensions;
using Mercury.Maths;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Lab
{
	public class Position
	{
		public string Symbol { get; set; }
		public decimal Change { get; set; }
		public decimal BarPer { get; set; }
		public SolidColorBrush BarColor => Change > 0 ? new SolidColorBrush(Color.FromRgb(0, 0, 255)) : new SolidColorBrush(Color.FromRgb(255, 0, 0));
	}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// TODO
	/// CubeAlgorithmCreator
	/// SC Mini
	/// </summary>
	public partial class MainWindow : Window
	{
		string[] symbols = [
			"JASMYUSDT",
			"DOGEUSDT",
			"ZECUSDT",
			"UNIUSDT",
			"XMRUSDT",
			"MASKUSDT",
			"SUSHIUSDT",
			"ATOMUSDT",
			"GRTUSDT",
			"ENSUSDT",
			"CHZUSDT",
			"BNBUSDT",
			"NKNUSDT",
			"OMGUSDT",
			"NEOUSDT",
			"WAVESUSDT",
			"XRPUSDT",
			"DASHUSDT",
			"OCEANUSDT",
			"ROSEUSDT",
			"BALUSDT",
			"SANDUSDT",
			"BANDUSDT",
			"COTIUSDT",
			"EGLDUSDT",
			"IOSTUSDT",
			"LTCUSDT",
			"ADAUSDT",
			"KAVAUSDT",
			"RLCUSDT",
			"MATICUSDT",
			"AAVEUSDT",
			"BELUSDT",
			"FTMUSDT",
			"ALPHAUSDT",
			"XLMUSDT",
			"KNCUSDT",
			"ETCUSDT",
			"OPUSDT",
			"HBARUSDT"
			];

		List<string> histories = [];

		public enum Subject
		{
			None,
			Master,
			Long,
			Short
		}

		public enum Act
		{
			None,
			OpenOrder,
			CloseOrder,
			CancelOrder,
			BotOn,
			BotOff,
			Collect
		}

		public static void EvaluateCandleScore(
	double?[] scores, double[] close, int forward = 5, double scoreThreshold = 1.0)
		{
			int totalLong = 0, totalShort = 0;
			int correctLong = 0, correctShort = 0;

			for (int i = 0; i < close.Length - forward; i++)
			{
				if (scores[i] == null) continue;

				double score = scores[i].Value;
				double futureReturn = (close[i + forward] - close[i]) / close[i];

				if (score >= scoreThreshold)
				{
					totalLong++;
					if (futureReturn > 0) correctLong++;
				}
				else if (score <= -scoreThreshold)
				{
					totalShort++;
					if (futureReturn < 0) correctShort++;
				}
			}

			double accLong = totalLong > 0 ? (double)correctLong / totalLong * 100 : 0;
			double accShort = totalShort > 0 ? (double)correctShort / totalShort * 100 : 0;

			Debug.WriteLine($"📊 롱 조건 (Score ≥ {scoreThreshold})");
			Debug.WriteLine($"  → 총: {totalLong}개, 적중: {correctLong}개, 정확도: {accLong:0.00}%");

			Debug.WriteLine($"📉 숏 조건 (Score ≤ -{scoreThreshold})");
			Debug.WriteLine($"  → 총: {totalShort}개, 적중: {correctShort}개, 정확도: {accShort:0.00}%");
		}

		public MainWindow()
		{
			InitializeComponent();

			var startTime = new DateTime(2022, 1, 1);
			var endTime = new DateTime(2023, 12, 31);
			var symbol = "BTCUSDT";
			var interval = KlineInterval.FifteenMinutes;

			//LocalApi.Init();
			//var quotes = LocalApi.GetOneDayQuotes(symbol);
			ChartLoader.InitCharts(symbol, interval, startTime, endTime);
			var quotes = ChartLoader.GetChartPack(symbol, interval).Charts;
			var open = quotes.Select(x => (double)x.Quote.Open).ToArray();
			var close = quotes.Select(x => (double)x.Quote.Close).ToArray();
			var high = quotes.Select(x => (double)x.Quote.High).ToArray();
			var low = quotes.Select(x => (double)x.Quote.Low).ToArray();
			var volume = quotes.Select(x => (double)x.Quote.Volume).ToArray();

			//var cci = ArrayCalculator.Cci(high, low, close, 20);
			//var ewmac = ArrayCalculator.Ewmac(close.ToNullable(), 20, 60);
			//var tr = ArrayCalculator.TrendRider(high, low, close, 10, 3.0, 14, 12, 26, 9);
			//var trend = tr.Item1;
			//for (int i = 0; i < trend.Length; i++)
			//{
			//	quotes[i].TrendRiderTrend = trend[i];
			//}
			var cs = ArrayCalculator.CandleScore(open, high, low, close, volume);
			EvaluateCandleScore(cs, close, 5, 0.5);

			//quotes.Select(x=> x.DateTime + " | " + x.TrendRiderTrend)
			//(var value, var direction, var signal) = ArrayCalculator.SqueezeMomentum(high, low, close, 20, 2.0, 20, 1.5, true);


			//var score = ArrayCalculator.MarketScore(high, low, close);
			//for(int i = 0; i < score.Length; i++)
			//{
			//	quotes[i].MarketScore = score[i];
			//}

			//BinanceRestApi.Init();
			//var result = BinanceRestApi.BinanceClient.UsdFuturesApi.Account.GetAccountInfoV3Async().Result;

			//var result = BinanceRestApi.GetQuotes("BTCUSDT", KlineInterval.OneMinute, startTime, null, 1000);
			//var result1 = BinanceRestApi.BinanceClient.UsdFuturesApi.Trading.GetOrdersAsync("ZECUSDT", null, startTime, endTime, 1000).Result;
			//var result2 = BinanceRestApi.GetFuturesTradeHistory(new string[] { "JASMYUSDT" }, new DateTime(2024, 8, 30)).ToList();

			/* trade history */
			//var trades = BinanceRestApi.GetFuturesTradeHistory(symbols, new DateTime(2024, 8, 6)).ToList();
			//trades.Sort(new BinanceFuturesTradeComparer());

			//var builder = new StringBuilder();
			//builder.AppendLine("Time,Symbol,PositionSide,Side,Price,Quantity,QuoteQuantity,Fee,FeeAsset,RealizedPnl,IsMaker");
			//foreach (var trade in trades)
			//{
			//	builder.AppendLine($"{trade.Time:yyyy-MM-dd HH:mm:ss},{trade.Symbol},{trade.PositionSide},{trade.Side},{trade.Price},{trade.Quantity},{trade.QuoteQuantity.Round(3)},{trade.Fee},{trade.FeeAsset},{trade.RealizedPnl},{trade.IsMaker}");
			//}
			//File.WriteAllText(MercuryPath.Desktop.Down($"binance_trade_history_{DateTime.Today:yyyyMMdd}.csv"), builder.ToString());




			//ChartLoader.InitCharts("BTCUSDT", KlineInterval.OneDay);
			//var chartPack = ChartLoader.GetChartPack("BTCUSDT", KlineInterval.OneDay);
			//chartPack.UsePredictiveRanges();

			//var calculator = new PredictiveRangesRiskCalculator(chartPack.Symbol, [.. chartPack.Charts], 50);
			//var result = calculator.Run(1057);

			//var upper2 = chartPack.Charts.Select(x => x.PredictiveRangesUpper2);
			//var upper = chartPack.Charts.Select(x => x.PredictiveRangesUpper);
			//var average = chartPack.Charts.Select(x => x.PredictiveRangesAverage);
			//var lower = chartPack.Charts.Select(x => x.PredictiveRangesLower);
			//var lower2 = chartPack.Charts.Select(x => x.PredictiveRangesLower2);
		}

		//public async Task SubscribeToUserDataUpdatesAsync()
		//{
		//	BinanceSocketApi.BinanceClient.ClientOptions.OutputOriginalData = true;
		//	var listenKey = BinanceRestApi.StartUserStream();
		//	var result = await BinanceSocketApi.BinanceClient.UsdFuturesApi.SubscribeToUserDataUpdatesAsync(
		//		listenKey,
		//		null,
		//		null,
		//		AccountUpdateOnMessage,
		//		null,
		//		ListenKeyExpiredOnMessage
		//		).ConfigureAwait(false);

		//	var substate = BinanceSocketApi.BinanceClient.GetSubscriptionsState();
		//}

		//public void ListenKeyExpiredOnMessage(DataEvent<BinanceStreamEvent> obj)
		//{

		//}

		//public void AccountUpdateOnMessage(DataEvent<BinanceFuturesStreamAccountUpdate> obj)
		//{

		//}

		//private async void Test_Click(object sender, RoutedEventArgs e)
		//{
		//	BinanceRestApi.Init();
		//	BinanceSocketApi.Init();

		//	await SubscribeToUserDataUpdatesAsync().ConfigureAwait(false);
		//}
	}
}
