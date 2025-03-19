using MarinerX.Markets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MarinerX.Views
{
	/// <summary>
	/// SymbolBenchmarkingView.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class SymbolBenchmarkingView : Window
	{
		List<SymbolBenchmark> benchmarks = [];
		List<SymbolBenchmark2> benchmarks2 = [];

		public SymbolBenchmarkingView()
		{
			InitializeComponent();
		}

		public void Init(List<SymbolBenchmark> benchmarks)
		{
			HistoryDataGrid.ItemsSource = null;
			HistoryDataGrid.ItemsSource = benchmarks;
			this.benchmarks = benchmarks;
		}

		public void Init2(List<SymbolBenchmark2> benchmarks)
		{
			HistoryDataGrid.ItemsSource = null;
			HistoryDataGrid.ItemsSource = benchmarks;
			benchmarks2 = benchmarks;
		}

		private void CopyButton_Click(object sender, RoutedEventArgs e)
		{
			var data = string.Join(Environment.NewLine,
				benchmarks.Count > 0 ?
				benchmarks.Select(x => x.ToCopyString()) :
				benchmarks2.Select(x => x.ToCopyString())
				);
			Clipboard.SetText(data);
			MessageBox.Show("Copied.");
		}

		private void HistoryDataGrid_AutoGeneratingColumn(object sender, System.Windows.Controls.DataGridAutoGeneratingColumnEventArgs e)
		{
			if (e.PropertyName == "MaxLeverage")
			{
				e.Cancel = true;
			}
		}
	}
}
