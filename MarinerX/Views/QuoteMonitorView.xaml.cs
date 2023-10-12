using Binance.Net.Enums;

using Mercury;

using MarinerX.Apis;
using MarinerX.Charts;

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
        public double Rsi { get; set; }
        public string Uad { get; set; } = string.Empty;
        public double Volume { get; set; }
        public bool IsLongPosition { get; set; }

        public QuoteMonitorData(string symbol, double rsi)
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
        DispatcherTimer timer2 = new ();
        readonly KlineInterval DefaultInterval = KlineInterval.FiveMinutes;
#pragma warning disable CS0414
        private bool isRunning;
#pragma warning restore CS0414
        readonly List<string> MonitorSymbolNames = new()
        {
            "AAVEUSDT",
            "ALGOUSDT",
            "ALICEUSDT",
            "ALPHAUSDT",
            "ANKRUSDT",
            "ANTUSDT",
            "APEUSDT",
            "API3USDT",
            "APTUSDT",
            "ARPAUSDT",
            "ARUSDT",
            "ATAUSDT",
            "ATOMUSDT",
            "AUDIOUSDT",
            "AVAXUSDT",
            "AXSUSDT",
            "BAKEUSDT",
            "BALUSDT",
            "BANDUSDT",
            "BATUSDT",
            "BELUSDT",
            "BLUEBIRDUSDT",
            "BLZUSDT",
            "BTCDOMUSDT",
            "BTCSTUSDT",
            "C98USDT",
            "CELRUSDT",
            "CHRUSDT",
            "CHZUSDT",
            "COMPUSDT",
            "COTIUSDT",
            "CRVUSDT",
            "CTKUSDT",
            "CTSIUSDT",
            "CVCUSDT",
            "DARUSDT",
            "DASHUSDT",
            "DEFIUSDT",
            "DENTUSDT",
            "DGBUSDT",
            "DOGEUSDT",
            "DOTUSDT",
            "DUSKUSDT",
            "DYDXUSDT",
            "EGLDUSDT",
            "ENJUSDT",
            "ENSUSDT",
            "ETCUSDT",
            "FETUSDT",
            "FILUSDT",
            "FLMUSDT",
            "FOOTBALLUSDT",
            "FTMUSDT",
            "FTTUSDT",
            "FXSUSDT",
            "GALAUSDT",
            "GALUSDT",
            "GMTUSDT",
            "GRTUSDT",
            "GTCUSDT",
            "HBARUSDT",
            "HNTUSDT",
            "HOOKUSDT",
            "HOTUSDT",
            "ICXUSDT",
            "IMXUSDT",
            "INJUSDT",
            "IOSTUSDT",
            "IOTAUSDT",
            "IOTXUSDT",
            "JASMYUSDT",
            "KAVAUSDT",
            "KNCUSDT",
            "KSMUSDT",
            "LDOUSDT",
            "LINAUSDT",
            "LITUSDT",
            "LPTUSDT",
            "LRCUSDT",
            "LUNA2USDT",
            "MAGICUSDT",
            "MANAUSDT",
            "MASKUSDT",
            "MATICUSDT",
            "MKRUSDT",
            "MTLUSDT",
            "NEARUSDT",
            "NKNUSDT",
            "OCEANUSDT",
            "OGNUSDT",
            "OMGUSDT",
            "ONEUSDT",
            "ONTUSDT",
            "OPUSDT",
            "PEOPLEUSDT",
            "QTUMUSDT",
            "REEFUSDT",
            "RENUSDT",
            "RLCUSDT",
            "ROSEUSDT",
            "RSRUSDT",
            "RUNEUSDT",
            "RVNUSDT",
            "SANDUSDT",
            "SFPUSDT",
            "SKLUSDT",
            "SNXUSDT",
            "SOLUSDT",
            "SRMUSDT",
            "STGUSDT",
            "STMXUSDT",
            "STORJUSDT",
            "SUSHIUSDT",
            "SXPUSDT",
            "THETAUSDT",
            "TLMUSDT",
            "TOMOUSDT",
            "TRBUSDT",
            "UNFIUSDT",
            "UNIUSDT",
            "VETUSDT",
            "WAVESUSDT",
            "WOOUSDT",
            "XEMUSDT",
            "XTZUSDT",
            "YFIUSDT",
            "ZECUSDT",
            "ZENUSDT",
            "ZILUSDT",
            "ZRXUSDT"
        };
        private Dictionary<string, double> RsiValues = new();

        public QuoteMonitorView()
        {
            InitializeComponent();

            MonitorStopButton.Visibility = Visibility.Hidden;
            foreach (var symbol in LocalStorageApi.SymbolNames)
            {
                BinanceSocketApi.GetKlineUpdatesAsync(symbol, KlineInterval.FiveMinutes);
            }
            timer.Interval = TimeSpan.FromSeconds(1);
            timer2.Interval = TimeSpan.FromSeconds(30);
            timer.Tick += Timer_Tick;
            timer2.Tick += Timer2_Tick;
        }

        private void Timer2_Tick(object? sender, EventArgs e)
        {
            RsiValues.Clear();
            foreach (var symbol in MonitorSymbolNames)
            {
                var quotes = BinanceRestApi.GetQuotes(symbol, DefaultInterval, null, null, 30);
                RsiValues.Add(symbol, quotes.GetRsi(14).Last().Rsi);
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
                MonitorDataGrid.Items.Clear();

                foreach (var symbol in MonitorSymbolNames)
                {
                    var quote = QuoteFactory.GetQuote(symbol);
                    if (quote == null)
                    {
                        continue;
                    }

                    if (RsiValues[symbol] >= 40 && RsiValues[symbol] <= 45)
                    {
                        MonitorDataGrid.Items.Add(new QuoteMonitorData(quote.Symbol, RsiValues[symbol]));
                    }
                }
            }
            catch { }
        }

        private void MonitorStartButton_Click(object sender, RoutedEventArgs e)
        {
            isRunning = true;
            timer.Start();
            timer2.Start();
            MonitorStartButton.Visibility = Visibility.Hidden;
            MonitorStopButton.Visibility = Visibility.Visible;
        }

        private void MonitorStopButton_Click(object sender, RoutedEventArgs e)
        {
            isRunning = false;
            timer.Stop();
            timer2.Stop();
            MonitorStartButton.Visibility = Visibility.Visible;
            MonitorStopButton.Visibility = Visibility.Hidden;
        }
    }
}
