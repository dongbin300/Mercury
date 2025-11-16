using KiwoomRestApi.Net.Clients;
using KiwoomRestApi.Net.Enums.Order;

using System.IO;
using System.Windows;

namespace KiwoomTrade
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		KiwoomRestApiClient client = default!;

		public MainWindow()
		{
			InitializeComponent();

			var appKey = File.ReadAllText("D:\\Assets\\kiwoom_appkey_mock.txt");
			var secretKey = File.ReadAllText("D:\\Assets\\kiwoom_secretkey_mock.txt");
			client = KiwoomRestApiClient.Create(appKey, secretKey, true);

		}

		private void BuyButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var transactionType = PriceTextBox.Text == "-" ? KiwoomOrderTransactionType.Market : KiwoomOrderTransactionType.Normal;
				var price = transactionType == KiwoomOrderTransactionType.Normal ? decimal.Parse(PriceTextBox.Text) : 0;
				var result = client.Order.PlaceOrderAsync(KiwoomOrderType.Buy, KiwoomOrderDomesticStockExchangeType.Krx, StockCodeTextBox.Text, decimal.Parse(QuantityTextBox.Text), transactionType, price).Result;

				ResultTextBox.Text = result?.ToString();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void SellButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var transactionType = PriceTextBox.Text == "-" ? KiwoomOrderTransactionType.Market : KiwoomOrderTransactionType.Normal;
				var price = transactionType == KiwoomOrderTransactionType.Normal ? decimal.Parse(PriceTextBox.Text) : 0;
				var result = client.Order.PlaceOrderAsync(KiwoomOrderType.Sell, KiwoomOrderDomesticStockExchangeType.Krx, StockCodeTextBox.Text, decimal.Parse(QuantityTextBox.Text), transactionType, price).Result;

				ResultTextBox.Text = result?.ToString();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}