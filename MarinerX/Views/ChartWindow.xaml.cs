using MarinerX.Bots;
using MarinerX.Views.Controls;

using Mercury.Charts;

using System.Windows;

namespace MarinerX.Views
{
	/// <summary>
	/// ChartWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ChartWindow : Window
    {
        BackTestChartControl backTestChartControl = new();

        public ChartWindow()
        {
            InitializeComponent();

            Screen.Content = backTestChartControl;
        }

        public void AddChartInfo(ChartInfo info)
        {
            backTestChartControl.Quotes.Add(info.Quote);
            backTestChartControl.TotalCount = backTestChartControl.End = backTestChartControl.Quotes.Count;
            InvalidateVisual(); // Might have to change it later.
        }

        public void AddTradeInfo(BackTestTrade trade)
        {
            backTestChartControl.Trades.Add(trade);
            InvalidateVisual(); // Might have to change it later.
        }
    }
}
