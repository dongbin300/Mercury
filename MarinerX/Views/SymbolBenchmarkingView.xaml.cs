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
        List<SymbolBenchmark> benchmarks = new();

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

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            var data = string.Join(Environment.NewLine, benchmarks.Select(x => x.ToCopyString()));
            Clipboard.SetText(data);
            MessageBox.Show("Copied.");
        }
    }
}
