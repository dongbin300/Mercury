using Mercury.Charts;

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace Lab
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
    {
        [DllImport("CudaLab.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void use_cuda(double[] r, double[] close, int length, int period, int b_size, int t_size);

        public MainWindow()
        {
            InitializeComponent();

            DateTime? startDate = null;
            DateTime? endDate = null;

			ChartLoader.InitCharts("BTCUSDT", Binance.Net.Enums.KlineInterval.OneMonth
                , startDate, endDate);
			var charts = ChartLoader.GetChartPack("BTCUSDT", Binance.Net.Enums.KlineInterval.OneMonth);
            charts.UseEma(20);
            var ema = charts.Charts.Select(x => x.Ema1).ToArray();

			//int blockSize = 1;
   //         int threadSize = 1;
   //         var close = charts.Charts.Select(x => (double)x.Quote.Close).ToArray();
   //         var length = close.Length;
   //         var result = new double[length];

			//use_cuda(result, close, length, 20, blockSize, threadSize);
        }
    }
}
