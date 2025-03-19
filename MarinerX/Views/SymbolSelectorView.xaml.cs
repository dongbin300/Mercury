using Binance.Net.Enums;

using Mercury.Apis;
using Mercury.Extensions;

using System;
using System.Windows;

namespace MarinerX.Views
{
	/// <summary>
	/// SymbolSelectorView.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class SymbolSelectorView : Window
	{
		public string SelectedSymbol = string.Empty;
		public KlineInterval SelectedInterval = KlineInterval.OneMinute;
		public DateTime SelectedStartDate = default!;
		public DateTime SelectedEndDate = default!;

		public SymbolSelectorView()
		{
			InitializeComponent();

			SymbolComboBox.ItemsSource = LocalApi.SymbolNames;
			SymbolComboBox.SelectedItem = "BTCUSDT";
			IntervalComboBox.SelectedIndex = 0;
			StartDateTextBox.Text = DateTime.Today.AddMonths(-1).ToString("yyyy-MM-dd");
			EndDateTextBox.Text = DateTime.Today.ToString("yyyy-MM-dd");
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			SelectedSymbol = SymbolComboBox.SelectedItem.ToString() ?? string.Empty;
			SelectedInterval = (IntervalComboBox.SelectedItem.ToString() ?? "1m").ToKlineInterval();
			SelectedStartDate = DateTime.Parse(StartDateTextBox.Text);
			SelectedEndDate = DateTime.Parse(EndDateTextBox.Text);
			DialogResult = true;
			Close();
		}
	}
}
