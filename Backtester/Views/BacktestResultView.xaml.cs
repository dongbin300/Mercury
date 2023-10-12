using Mercury.Backtests;

using System.Windows;

namespace Backtester.Views
{
    /// <summary>
    /// BacktestResultView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BacktestResultView : Window
    {
        public BacktestResultView()
        {
            InitializeComponent();
        }

        public void Init(string symbol, SimpleDealManager dealManager)
        {
            Title = $"{symbol}, BOS: {dealManager.BaseOrderSize}, Income: {dealManager.TotalIncome:N4}";
            ResultDataGrid.ItemsSource = dealManager.Deals;
        }
    }
}
