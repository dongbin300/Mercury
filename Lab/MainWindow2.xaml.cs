using Binance.Net.Enums;

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Vectoris.Charts.Core;
using Vectoris.Charts.Indicators;
using Vectoris.Charts.IO;
using Vectoris.Charts.Series;

namespace Lab;

/// <summary>
/// MainWindow2.xaml에 대한 상호 작용 논리
/// </summary>
public partial class MainWindow2 : Window
{
	public MainWindow2()
	{
		InitializeComponent();

		var startTime = new DateTime(2010, 1, 1);
		var endTime = new DateTime(2025, 3, 1);
		var symbol = "BTCUSDT";
		var interval = KlineInterval.TwoHour;

		var quotes = QuoteLoader.FromCsv(symbol, interval, startTime, endTime);

		//var ema = new Ema(20);
		//ema.AddQuotes(quotes);

		var model = new ChartModel(quotes);
		var ema = model.Get("EMA", 20);
	}
}
