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

            DateTime? startDate = new DateTime(2023, 2, 22, 0, 2, 0);
			DateTime? endDate = new DateTime(2023, 3, 23, 10, 51, 0);

			ChartLoader.InitCharts("BTCUSDT", Binance.Net.Enums.KlineInterval.OneWeek
                , startDate, endDate);
			var charts = ChartLoader.GetChartPack("BTCUSDT", Binance.Net.Enums.KlineInterval.OneWeek).Charts;
            //charts.UseEma(20);
            //var ema = charts.Charts.Select(x => x.Ema1).ToArray();

			//int blockSize = 1;
   //         int threadSize = 1;
   //         var close = charts.Charts.Select(x => (double)x.Quote.Close).ToArray();
   //         var length = close.Length;
   //         var result = new double[length];

			//use_cuda(result, close, length, 20, blockSize, threadSize);
        }
    }
}
