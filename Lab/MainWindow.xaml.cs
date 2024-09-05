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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Lab
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
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

		public MainWindow()
		{
			InitializeComponent();

			BinanceRestApi.Init();
			var result1 = BinanceRestApi.BinanceClient.UsdFuturesApi.Account.GetIncomeHistoryAsync(null, null, new DateTime(2024, 8, 20), new DateTime(2024, 8, 27), 1000).Result;
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
