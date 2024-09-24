using Binance.Net.Enums;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using TradeBot.Bots;
using TradeBot.Models;
using TradeBot.Systems;

namespace TradeBot.Views
{
	/// <summary>
	/// ReportWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ReportWindow : Window
	{
		public ReportBot Bot;
		private List<BinanceIncomeHistory> income = default!;
		private List<BinanceOrderHistory> order = default!;
		private List<BinanceTradeHistory> trade = default!;
		private List<BinancePositionHistory> position = default!;
		private List<BotReportHistory> botReport = default!;
		private List<BinanceDailyHistory> daily = default!;

		public ReportWindow()
		{
			InitializeComponent();
			Bot = new ReportBot();
		}

		private void ReportDataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
		{
			// Browsable(false) annotationed property 숨기기
			if (e.PropertyDescriptor is PropertyDescriptor propertyDescriptor)
			{
				if (propertyDescriptor.Attributes[typeof(BrowsableAttribute)] is BrowsableAttribute browsableAttribute && !browsableAttribute.Browsable)
				{
					e.Cancel = true;
				}
			}

			// 날짜 표시 형식 변경
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

			// 컬럼별 스타일 설정
			switch (e.PropertyName)
			{
				case "Income":
					{
						var column = (DataGridTextColumn)e.Column;
						var cellStyle = new Style(typeof(DataGridCell));
						var foregroundSetter = new Setter(ForegroundProperty, new Binding("IncomeColor"));
						cellStyle.Setters.Add(foregroundSetter);
						column.ElementStyle = new Style(typeof(TextBlock));
						column.ElementStyle.Setters.Add(foregroundSetter);
					}
					break;

				case "PositionSide":
					// 콤보박스 컬럼은 색깔이 안바뀜;;
					//{
					//	var comboBoxColumn = (DataGridComboBoxColumn)e.Column;
					//	var elementStyle = new Style(typeof(ComboBox));
					//	var foregroundSetter = new Setter(ForegroundProperty, new Binding("PositionSideColor"));
					//	elementStyle.Setters.Add(foregroundSetter);
					//	comboBoxColumn.ElementStyle = elementStyle;
					//	var editingElementStyle = new Style(typeof(ComboBox));
					//	var editingForegroundSetter = new Setter(ForegroundProperty, new Binding("PositionSideColor"));
					//	editingElementStyle.Setters.Add(editingForegroundSetter);
					//	comboBoxColumn.EditingElementStyle = editingElementStyle;
					//}
					break;	

				case "RealizedPnl":
					{
						var column = (DataGridTextColumn)e.Column;
						var cellStyle = new Style(typeof(DataGridCell));
						var foregroundSetter = new Setter(ForegroundProperty, new Binding("RealizedPnlColor"));
						cellStyle.Setters.Add(foregroundSetter);
						column.ElementStyle = new Style(typeof(TextBlock));
						column.ElementStyle.Setters.Add(foregroundSetter);
					}
					break;

				case "Change":
					{
						var column = (DataGridTextColumn)e.Column;
						var cellStyle = new Style(typeof(DataGridCell));
						var foregroundSetter = new Setter(ForegroundProperty, new Binding("ChangeColor"));
						cellStyle.Setters.Add(foregroundSetter);
						column.ElementStyle = new Style(typeof(TextBlock));
						column.ElementStyle.Setters.Add(foregroundSetter);
					}
					break;

				case "ChangePer":
					{
						var column = (DataGridTextColumn)e.Column;
						var cellStyle = new Style(typeof(DataGridCell));
						var foregroundSetter = new Setter(ForegroundProperty, new Binding("ChangeColor"));
						cellStyle.Setters.Add(foregroundSetter);
						column.Binding = new Binding("ChangePerString");
						column.ElementStyle = new Style(typeof(TextBlock));
						column.ElementStyle.Setters.Add(foregroundSetter);
					}
					break;

				case "MaxPer":
					{
						var column = (DataGridTextColumn)e.Column;
						var cellStyle = new Style(typeof(DataGridCell));
						var foregroundSetter = new Setter(ForegroundProperty, new Binding("MaxPerColor"));
						cellStyle.Setters.Add(foregroundSetter);
						column.Binding = new Binding("MaxPerString");
						column.ElementStyle = new Style(typeof(TextBlock));
						column.ElementStyle.Setters.Add(foregroundSetter);
					}
					break;
			}
		}

		IEnumerable<BinanceHistory> AddHistorySum(IEnumerable<BinanceHistory> histories)
		{
			switch (histories)
			{
				case List<BinanceIncomeHistory> income:
					{
						var sumRow = new BinanceIncomeHistory(default!, "", null, null, income.Sum(x => x.Income), null, null);
						income.Insert(0, sumRow);
						return income;
					}

				case List<BinanceTradeHistory> trade:
					{
						var sumRow = new BinanceTradeHistory(default!, "", PositionSide.Both, OrderSide.Buy, 0, 0, 0, trade.Sum(x => x.Fee), "", trade.Sum(x => x.RealizedPnl), false);
						trade.Insert(0, sumRow);
						return trade;
					}

				case List<BinancePositionHistory> position:
					{
						var sumRow = new BinancePositionHistory(default!, null, "", PositionSide.Both, 0, 0, 0);
						sumRow.RealizedPnl = position.Sum(x => x.RealizedPnl);
						position.Insert(0, sumRow);
						return position;
					}
			}

			return default!;
		}

		private void IncomeButton_Click(object sender, RoutedEventArgs e)
		{
			PlotChart.Visibility = Visibility.Collapsed;
			CollectButton.Visibility = Visibility.Visible;
			income = Bot.ReadIncomeReport().ToList();
			income.Reverse();
			income = (List<BinanceIncomeHistory>)AddHistorySum(income);

			ReportDataGrid.ItemsSource = null;
			ReportDataGrid.ItemsSource = income;
			Title = "Income";
		}

		private void OrderButton_Click(object sender, RoutedEventArgs e)
		{
			PlotChart.Visibility = Visibility.Collapsed;
			CollectButton.Visibility = Visibility.Visible;
			order = Bot.ReadOrderReport().ToList();
			order.Reverse();

			ReportDataGrid.ItemsSource = null;
			ReportDataGrid.ItemsSource = order;
			Title = "Order";
		}

		private void TradeButton_Click(object sender, RoutedEventArgs e)
		{
			PlotChart.Visibility = Visibility.Collapsed;
			CollectButton.Visibility = Visibility.Visible;
			trade = Bot.ReadTradeReport().ToList();
			trade.Reverse();
			trade = (List<BinanceTradeHistory>)AddHistorySum(trade);

			ReportDataGrid.ItemsSource = null;
			ReportDataGrid.ItemsSource = trade;
			Title = "Trade";
		}

		private void PositionButton_Click(object sender, RoutedEventArgs e)
		{
			PlotChart.Visibility = Visibility.Collapsed;
			CollectButton.Visibility = Visibility.Collapsed;
			trade ??= Bot.ReadTradeReport().ToList();
			position = Bot.GetPositionHistory(trade).ToList();
			position.Reverse();
			position = (List<BinancePositionHistory>)AddHistorySum(position);

			ReportDataGrid.ItemsSource = null;
			ReportDataGrid.ItemsSource = position;
			Title = "Position";
		}

		private void BotReportButton_Click(object sender, RoutedEventArgs e)
		{
			PlotChart.Visibility = Visibility.Collapsed;
			CollectButton.Visibility = Visibility.Collapsed;
			botReport = Bot.ReadBotReportReport().ToList();
			SetPlotChart(botReport.Select(x => x.Estimated).ToList());
			botReport.Reverse();

			ReportDataGrid.ItemsSource = null;
			ReportDataGrid.ItemsSource = botReport;
			Title = "Bot Report";
		}

		private void DailyButton_Click(object sender, RoutedEventArgs e)
		{
			PlotChart.Visibility = Visibility.Collapsed;
			CollectButton.Visibility = Visibility.Visible;
			daily = Bot.ReadDailyReport().ToList();
			SetPlotChart(daily.Select(x => x.Estimated).ToList());
			daily.Reverse();

			ReportDataGrid.ItemsSource = null;
			ReportDataGrid.ItemsSource = daily;
			Title = "Daily";

			if (daily.Count > 0 && daily[0].Time != DateTime.Today) // 최종날짜가 오늘날짜가 아니면 자동 수집
			{
				CollectButton_Click(sender, e);
			}
		}

		private void TodayPnlButton_Click(object sender, RoutedEventArgs e)
		{
			PlotChart.Visibility = Visibility.Collapsed;
			CollectButton.Visibility = Visibility.Collapsed;
			income = Bot.GetPnlReport(DateTime.Today, DateTime.Today.AddDays(1)).ToList();
			income.Reverse();
			income = (List<BinanceIncomeHistory>)AddHistorySum(income);

			ReportDataGrid.ItemsSource = null;
			ReportDataGrid.ItemsSource = income;
			//SetStatus($"PNL: {income.Sum(x => x.Income)}");
			Title = "Today PNL";
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

				switch (Title)
				{
					case "Income":
						{
							var incomef = income.Where(x => x.ToString().Contains(keyword, StringComparison.CurrentCultureIgnoreCase)).ToList();
							incomef = (List<BinanceIncomeHistory>)AddHistorySum(incomef);
							ReportDataGrid.ItemsSource = null;
							ReportDataGrid.ItemsSource = incomef;
						}
						break;

					case "Order":
						{
							var orderf = order.Where(x => x.ToString().Contains(keyword, StringComparison.CurrentCultureIgnoreCase)).ToList();
							ReportDataGrid.ItemsSource = null;
							ReportDataGrid.ItemsSource = orderf;
						}
						break;

					case "Trade":
						{
							var tradef = trade.Where(x => x.ToString().Contains(keyword, StringComparison.CurrentCultureIgnoreCase)).ToList();
							tradef = (List<BinanceTradeHistory>)AddHistorySum(tradef);
							ReportDataGrid.ItemsSource = null;
							ReportDataGrid.ItemsSource = tradef;
						}
						break;

					case "Position":
						{
							var positionf = position.Where(x => x.ToString().Contains(keyword, StringComparison.CurrentCultureIgnoreCase)).ToList();
							positionf = (List<BinancePositionHistory>)AddHistorySum(positionf);
							ReportDataGrid.ItemsSource = null;
							ReportDataGrid.ItemsSource = positionf;
						}
						break;

					case "Bot Report":
						{
							var botReportf = botReport.Where(x => x.ToString().Contains(keyword, StringComparison.CurrentCultureIgnoreCase)).ToList();
							ReportDataGrid.ItemsSource = null;
							ReportDataGrid.ItemsSource = botReportf;
						}
						break;

					case "Daily":
						{
							var dailyf = daily.Where(x => x.ToString().Contains(keyword, StringComparison.CurrentCultureIgnoreCase)).ToList();
							ReportDataGrid.ItemsSource = null;
							ReportDataGrid.ItemsSource = dailyf;
						}
						break;

					case "Today PNL":
						{
							var todayPnlf = income.Where(x => x.ToString().Contains(keyword, StringComparison.CurrentCultureIgnoreCase)).ToList();
							todayPnlf = (List<BinanceIncomeHistory>)AddHistorySum(todayPnlf);
							ReportDataGrid.ItemsSource = null;
							ReportDataGrid.ItemsSource = todayPnlf;
						}
						break;
				}
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
						await Bot.WriteIncomeReport(Progress).ConfigureAwait(false);
						SetStatus("Collect Income Complete");
						Common.AddHistory("Report Bot", "Collect Income Complete");

						income = Bot.ReadIncomeReport().ToList();
						income.Reverse();
						DispatcherService.Invoke(() =>
						{
							ReportDataGrid.ItemsSource = null;
							ReportDataGrid.ItemsSource = income;
						});
						break;

					case "Order":
						await Bot.WriteOrderReport(Progress).ConfigureAwait(false);
						SetStatus("Collect Order Complete");
						Common.AddHistory("Report Bot", "Collect Order Complete");

						order = Bot.ReadOrderReport().ToList();
						order.Reverse();
						DispatcherService.Invoke(() =>
						{
							ReportDataGrid.ItemsSource = null;
							ReportDataGrid.ItemsSource = order;
						});
						break;

					case "Trade":
						await Bot.WriteTradeReport(Progress).ConfigureAwait(false);
						SetStatus("Collect Trade Complete");
						Common.AddHistory("Report Bot", "Collect Trade Complete");

						trade = Bot.ReadTradeReport().ToList();
						trade.Reverse();
						DispatcherService.Invoke(() =>
						{
							ReportDataGrid.ItemsSource = null;
							ReportDataGrid.ItemsSource = trade;
						});
						break;

					case "Daily":
						Bot.WriteDailyReport();
						SetStatus("Collect Daily Complete");
						Common.AddHistory("Report Bot", "Collect Daily Complete");

						daily = Bot.ReadDailyReport().ToList();
						daily.Reverse();
						DispatcherService.Invoke(() =>
						{
							ReportDataGrid.ItemsSource = null;
							ReportDataGrid.ItemsSource = daily;
						});
						break;

					default:
						break;
				}
			}
			catch (Exception ex)
			{
				SetStatus(ex.Message);
				Common.AddHistory("Report Bot", ex.Message);
				Logger.Log(nameof(ReportWindow), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

		private void SetStatus(string status)
		{
			DispatcherService.Invoke(() =>
			{
				StatusText.Text = status;
			});
		}

		private void SetPlotChart(List<decimal> data)
		{
			var plotModel = new PlotModel
			{
				TextColor = Common.ForegroundColor.ToOxyColor()
			};
			var xAxis = new LinearAxis
			{
				Position = AxisPosition.Bottom,
				MajorGridlineColor = OxyColor.FromRgb(64, 64, 64),
				MajorGridlineStyle = LineStyle.Solid,
				MajorGridlineThickness = 1
			};
			var yAxis = new LinearAxis
			{
				Position = AxisPosition.Left,
				MajorGridlineColor = OxyColor.FromRgb(64, 64, 64),
				MajorGridlineStyle = LineStyle.Solid,
				MajorGridlineThickness = 1
			};
			plotModel.Axes.Add(xAxis);
			plotModel.Axes.Add(yAxis);

			var dataPoints = new List<DataPoint>();
			for (int i = 0; i < data.Count; i++)
			{
				dataPoints.Add(new DataPoint(i, (double)data[i]));
			}
			var series = new LineSeries
			{
				Color = Common.LongColor.ToOxyColor(),
				ItemsSource = dataPoints
			};
			plotModel.Series.Add(series);
			PlotChart.Model = plotModel;
			PlotChart.Visibility = Visibility.Visible;
		}

		private void ReportDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
		{
			if (e.Row.GetIndex() == 0)
			{
				e.Row.Background = new SolidColorBrush(Color.FromRgb(56, 56, 56));
			}
		}
	}
}
