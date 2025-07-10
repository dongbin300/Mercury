using Binance.Net.Enums;
using Mercury.Apis;
using Mercury.Charts;
using Mercury.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace MarinerX.Views
{
    /// <summary>
    /// QuoteMonitorView.xaml에 대한 상호 작용 논리
    /// </summary>
    public class QuoteMonitorData
    {
        public string Symbol { get; set; } = string.Empty;
        public double? Rsi { get; set; }
        public string Uad { get; set; } = string.Empty;
        public double Volume { get; set; }
        public bool IsLongPosition { get; set; }

        public QuoteMonitorData(string symbol, double? rsi)
        {
            Symbol = symbol;
            Rsi = rsi;
        }

        public QuoteMonitorData(string symbol, string uad, double volume, bool isLongPosition = true)
        {
            Symbol = symbol;
            Uad = uad;
            Volume = volume;
            IsLongPosition = isLongPosition;
        }
    }

    public class QuoteRating
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Ma20 { get; set; }
        public decimal Ema112 { get; set; }
        public decimal Ema224 { get; set; }
        public decimal Volume { get; set; }
    }

    /// <summary>
    /// QuoteMonitorView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class QuoteMonitorView : Window
    {
        DispatcherTimer timer = new ();
        readonly KlineInterval DefaultInterval = KlineInterval.OneDay;
#pragma warning disable CS0414
        private bool isRunning;
#pragma warning restore CS0414
		public List<string> MonitorSymbolNames = [];
        private Dictionary<string, double?> RsiValues = new();

        public QuoteMonitorView()
        {
            InitializeComponent();

			MonitorStopButton.Visibility = Visibility.Hidden;

			//foreach (var symbol in LocalApi.SymbolNames)
			//{
			//    BinanceSocketApi.GetKlineUpdatesAsync(symbol, KlineInterval.FiveMinutes);
			//}
			//timer.Interval = TimeSpan.FromSeconds(30);
			//timer.Tick += Timer_Tick;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				MonitorSymbolNames = BinanceRestApi.GetFuturesSymbolNames();
				MonitorSymbolNames.Remove("BTCSTUSDT");

				ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
				MonitorDataGrid.Items.Clear();

				foreach (var symbol in MonitorSymbolNames)
				{
					try
					{
						var quotes = BinanceRestApi.GetQuotes(symbol, DefaultInterval, null, null, 300);
						var charts = quotes.Select(x => new ChartInfo(symbol, x)).ToList();
						if (charts == null || charts.Count < 300) continue;
						int lastIdx = charts.Count - 1;

						var ema = charts.Select(x => x.Quote).GetEma(120).Select(x => x.Ema);
						var ema2 = charts.Select(x => x.Quote).GetEma(240).Select(x => x.Ema);
						for (int i = 0; i < charts.Count; i++)
						{
							charts[i].Ema1 = ema.ElementAt(i);
							charts[i].Ema2 = ema2.ElementAt(i);
						}

						bool cond1 = IsInEmaRange(charts, lastIdx, 30, 5); // 30봉 이내 EMA1~EMA2 5회 이상
						bool cond2 = IsNearEma120(charts, lastIdx, 30, 0.98m, 1.05m, 3); // 30봉 이내 120EMA 근접 3회 이상
						bool cond3 = HasHighOverPrevClose(charts, lastIdx, 30, 20); // 30봉 이내 전일종가대비 고가 20% 이상

						if (cond1 && cond2 && cond3 &&
							charts[^1].Quote.Close > (decimal)charts[^1].Ema1 && charts[^1].Quote.Close < (decimal)charts[^1].Ema2
							)
						{
							MonitorDataGrid.Items.Add(new QuoteMonitorData(symbol, 0)); // 0대신 원하는 값 넣기
						}
					}
					catch
					{
					}
					
				}
			}
			catch
			{
			}
		}

		private void Timer_Tick(object? sender, EventArgs e)
		{
			
		}


		private void MonitorStartButton_Click(object sender, RoutedEventArgs e)
        {
            isRunning = true;
            timer.Start();
            MonitorStartButton.Visibility = Visibility.Hidden;
            MonitorStopButton.Visibility = Visibility.Visible;
        }

        private void MonitorStopButton_Click(object sender, RoutedEventArgs e)
        {
            isRunning = false;
            timer.Stop();
            MonitorStartButton.Visibility = Visibility.Visible;
            MonitorStopButton.Visibility = Visibility.Hidden;
        }

		private bool IsInEmaRange(List<ChartInfo> charts, int currentIndex, int lookback, int minCount)
		{
			int count = 0;
			int from = Math.Max(0, currentIndex - lookback + 1);
			for (int i = from; i <= currentIndex; i++)
			{
				var c = charts[i];
				if (c.Ema1 == null || c.Ema2 == null) continue;
				if (c.Quote.Close > (decimal)c.Ema1 && c.Quote.Close < (decimal)c.Ema2)
					count++;
			}
			return count >= minCount;
		}

		private bool IsNearEma120(List<ChartInfo> charts, int currentIndex, int lookback, decimal lower, decimal upper, int minCount)
		{
			int count = 0;
			int from = Math.Max(0, currentIndex - lookback + 1);
			for (int i = from; i <= currentIndex; i++)
			{
				var c = charts[i];
				if (c.Ema1 == null) continue;
				var ratio = c.Quote.Close / (decimal)c.Ema1;
				if (ratio >= lower && ratio <= upper)
					count++;
			}
			return count >= minCount;
		}

		private bool HasHighOverPrevClose(List<ChartInfo> charts, int currentIndex, int lookback = 30, decimal percent = 20)
		{
			int from = Math.Max(1, currentIndex - lookback + 1); // 0번째는 전일 종가가 없으니 1부터 시작
			decimal ratio = 1 + (percent / 100m);

			for (int i = from; i <= currentIndex; i++)
			{
				var prevClose = charts[i - 1].Quote.Close;
				var high = charts[i].Quote.High;
				if (prevClose == 0) continue; // 0으로 나누는 경우 방지
				if (high >= prevClose * ratio)
					return true;
			}
			return false;
		}
		
	}
}
