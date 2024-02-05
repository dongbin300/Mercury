using Mercury.Charts;

using System.Linq;
using System.Windows;

namespace Lab
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ChartLoader.InitChartsMByDate("BTCUSDT", Binance.Net.Enums.KlineInterval.FiveMinutes);
            var charts = ChartLoader.GetChartPack("BTCUSDT", Binance.Net.Enums.KlineInterval.FiveMinutes);
            charts.UseRsi();
            var rsi = charts.Charts.Select(x => x.Rsi1);
        }
    }
}
