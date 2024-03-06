using Mercury;
using Mercury.Charts;

using System;
using System.Collections.Generic;
using System.IO;
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
        public static extern void use_cuda(double[][] r, double[][] close, int num_arrays, int length, int period, int b_size, int t_size);

        public MainWindow()
		{
			InitializeComponent();

			DateTime? startDate = null;
			DateTime? endDate = null;

			var symbols = GetSymbolNames();
			var symbols2 = symbols.Take(1);
			foreach (var symbol in symbols2)
			{
				ChartLoader.InitCharts(symbol, Binance.Net.Enums.KlineInterval.OneHour, startDate, endDate);
			}

			var chartPacks = new List<ChartPack>();

			foreach (var symbol in symbols2)
			{
				var chartPack = ChartLoader.GetChartPack(symbol, Binance.Net.Enums.KlineInterval.OneHour);
				chartPacks.Add(chartPack);
			}

			foreach (var chartPack in chartPacks)
			{
				chartPack.UseEma(20);
			}

			int blockSize = 1;
			int threadSize = 1;
			var symbolCount = symbols2.Count();
			var close = chartPacks.Select(x => x.Charts.Select(x => (double)x.Quote.Close).ToArray()).ToArray();
			var length = close.Max(x => x.Length);
			//ResizeArray(close, length);
			double[][] result = new double[symbolCount][];
			for (int i = 0; i < symbolCount; i++)
			{
				result[i] = new double[length];
			}

			use_cuda(result, close, symbolCount, length, 20, blockSize, threadSize);
		}

		public List<string> GetSymbolNames()
		{
			var symbolFile = new DirectoryInfo(MercuryPath.BinanceFuturesData).GetFiles("symbol_*.txt").OrderByDescending(x => x.LastAccessTime).FirstOrDefault() ?? default!;
			return File.ReadAllLines(symbolFile.FullName).ToList();
		}

	}
}
