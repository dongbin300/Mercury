using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

using TradeBot.Bots;
using TradeBot.Models;

namespace TradeBot.Views
{
	/// <summary>
	/// ReportWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ReportWindow : Window
	{
		public ReportBot Bot;
		private List<BinanceIncomeHistory> income = default!, incomef = default!;
		private List<BinanceTradeHistory> trade = default!, tradef = default!;
		private List<BotReportHistory> botReport = default!, botReportf = default!;
		private List<BinanceDailyHistory> daily = default!, dailyf = default!;

		public ReportWindow()
		{
			InitializeComponent();
			Bot = new ReportBot();
		}

		private void ReportDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
		{
			if (e.PropertyType == typeof(DateTime))
			{
				if (e.Column is DataGridTextColumn textColumn)
				{
					textColumn.Binding = new Binding(e.PropertyName)
					{
						StringFormat = "yyyy-MM-dd HH:mm:ss"
					};
				}
			}
		}
		
		private void IncomeButton_Click(object sender, RoutedEventArgs e)
		{
			income = Bot.ReadIncomeReport().ToList();
			ReportDataGrid.ItemsSource = null;
			ReportDataGrid.ItemsSource = income;
			Title = "Income";
		}

		private void TradeButton_Click(object sender, RoutedEventArgs e)
		{
			trade = Bot.ReadTradeReport().ToList();
			ReportDataGrid.ItemsSource = null;
			ReportDataGrid.ItemsSource = trade;
			Title = "Trade";
		}

		private void BotReportButton_Click(object sender, RoutedEventArgs e)
		{
			botReport = Bot.ReadBotReportReport().ToList();
			ReportDataGrid.ItemsSource = null;
			ReportDataGrid.ItemsSource = botReport;
			Title = "Bot Report";
		}

		private void DailyButton_Click(object sender, RoutedEventArgs e)
		{
			daily = Bot.ReadDailyReport().ToList();
			ReportDataGrid.ItemsSource = null;
			ReportDataGrid.ItemsSource = daily;
			Title = "Daily";
		}

		private void KeywordTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Enter:
					SearchButton_Click(sender, e);
					break;
			}
		}

		private void SearchButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var keyword = KeywordTextBox.Text;

				ReportDataGrid.ItemsSource = null;
				ReportDataGrid.ItemsSource = Title switch
				{
					"Income" => income.Where(x => x.ToString().Contains(keyword, StringComparison.CurrentCultureIgnoreCase)).ToList(),
					"Trade" => trade.Where(x => x.ToString().Contains(keyword, StringComparison.CurrentCultureIgnoreCase)).ToList(),
					"Bot Report" => botReport.Where(x => x.ToString().Contains(keyword, StringComparison.CurrentCultureIgnoreCase)).ToList(),
					"Daily" => daily.Where(x => x.ToString().Contains(keyword, StringComparison.CurrentCultureIgnoreCase)).ToList(),
					_ => null
				};
			}
			catch
			{
			}
		}

		private async void CollectButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				switch (Title)
				{
					case "Income":
						await Bot.WriteIncomeReport().ConfigureAwait(false);
						Common.AddHistory("Report Bot", "Collect Income Complete");
						break;

					case "Trade":
						await Bot.WriteTradeReport().ConfigureAwait(false);
						Common.AddHistory("Report Bot", "Collect Trade Complete");
						break;

					case "Daily":
						Bot.WriteDailyReport();
						Common.AddHistory("Report Bot", "Collect Daily Complete");
						break;

					default:
						break;
				}
			}
			catch (Exception ex)
			{
				Common.AddHistory("Report Bot", ex.Message);
				Logger.Log(nameof(ReportWindow), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}
	}
}
