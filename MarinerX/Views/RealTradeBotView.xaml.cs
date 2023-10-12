using MarinerX.Charts;
using MarinerX.Utils;

using System.Windows;

namespace MarinerX.Views
{
    /// <summary>
    /// RealTradeBotView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RealTradeBotView : Window
    {
        System.Timers.Timer mainTimer = new (2000);

        public RealTradeBotView()
        {
            InitializeComponent();
            mainTimer.Elapsed += MainTimer_Elapsed;
        }

        private void MainTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                DispatcherService.Invoke(() =>
                {
                    QuoteDataGrid.Items.Clear();
                    foreach (var chart in RealtimeChartManager.RealtimeCharts)
                    {
                        QuoteDataGrid.Items.Add(chart);
                    }
                });
            }
            catch
            {
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RealtimeChartManager.Init();
            mainTimer.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainTimer.Stop();
        }
    }
}
