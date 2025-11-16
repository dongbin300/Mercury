using KiwoomRestApi.Net.Clients;
using KiwoomRestApi.Net.Enums.StockInfo;
using KiwoomRestApi.Net.Objects.Commons;

using System.Diagnostics;
using System.IO;
using System.Windows;

namespace KiwoomManager2
{
	public class Results
	{
		public KiwoomDecimal? Price { get; set; }
		public KiwoomDecimal? ChangeRate { get; set; }
	}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		KiwoomRestApiClient client = default!;
		DateTime today = DateTime.Today;
		KiwoomStockInfoMarginLoanType loan = KiwoomStockInfoMarginLoanType.Loan;

		List<string> stockCodes = ["069920", "452400", "088800"];
		List<Results> results = [];

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			var appKey = File.ReadAllText("D:\\Assets\\kiwoom_appkey_mock.txt");
			var secretKey = File.ReadAllText("D:\\Assets\\kiwoom_secretkey_mock.txt");
			client = KiwoomRestApiClient.Create(appKey, secretKey, true);

			//var favorites = client.StockInfo.GetFavoriteStocksAsync("005930").Result.Data;
			//var favorites2 = client.StockInfo.GetListsAsync(KiwoomStockInfoMarketType3.Kosdaq).Result.Data;

			foreach (var code in stockCodes)
			{
				var result = client.StockInfo.GetStockInfoAsync(code, today, loan).Result.Data ?? default!;
				results.Add(new Results
				{
					Price = result.CurrentPrice,
					ChangeRate = result.ChangeRate
				});
				Thread.Sleep(1000);
			}

			//var result1 = client.StockInfo.GetStockInfoAsync(stockCode1, today, loan).Result.Data;
			//         Thread.Sleep(1000);
			//         var result2 = client.StockInfo.GetStockInfoAsync(stockCode2, today, loan).Result.Data;
			//         var result3 = client.MarketCondition.GetOrderBookListAsync(stockCode1).Result.Data;

			//         for (int i = 0; i < 10; i++)
			//         {
			//             Debug.WriteLine(Environment.NewLine);
			//         }
			//         Debug.WriteLine($"{result1.CurrentPrice} {result1.ChangeRate}");
			//         Debug.WriteLine($"{result2.CurrentPrice} {result2.ChangeRate}");
			//Debug.WriteLine($"{result3.SellQuotes[0].Quantity}");
		}
	}
}