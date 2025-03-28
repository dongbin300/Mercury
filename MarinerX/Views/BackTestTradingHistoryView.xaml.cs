using MarinerX.Utils;

using Mercury.Extensions;
using Mercury.Trades;

using System.Collections.Generic;
using System.Windows;

namespace MarinerX.Views
{
	/// <summary>
	/// BackTestTradingHistoryView.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class BackTestTradingHistoryView : Window
    {
        public BackTestTradingHistoryView()
        {
            InitializeComponent();
        }

        public void Init(List<BackTestTradeInfo> trades)
        {
            HistoryDataGrid.ItemsSource = null;
            HistoryDataGrid.ItemsSource = trades;
        }

        public void Init(string fileName)
        {
            Title = fileName.GetOnlyFileName();

            var csv = FileUtil.ReadCsv(fileName);
            HistoryDataGrid.ItemsSource = null;
            HistoryDataGrid.ItemsSource = csv.DefaultView;
        }
    }
}
