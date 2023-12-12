using MarinaX.Utils;

using MarinerX.Apis;
using MarinerX.Charts;
using MarinerX.Utils;

using Mercury;

using MercuryTradingModel.Assets;
using MercuryTradingModel.Enums;
using MercuryTradingModel.Extensions;
using MercuryTradingModel.Orders;
using MercuryTradingModel.Trades;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MarinerX.Views
{
    public class GridBotBackTestTradingModel
    {
        public decimal Asset { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan Period { get; set; }
        public string Target { get; set; } = string.Empty;
        public decimal HighPrice { get; set; }
        public decimal LowPrice { get; set; }
        public int Levels { get; set; }
    }

    /// <summary>
    /// GridBotBackTesterView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class GridBotBackTesterView : Window
    {
        List<BackTestTradeInfo> trades = new();
        ChartWindow chartViewer = new();
        Worker worker = new();
        ProgressView progressView = new();

        public GridBotBackTesterView()
        {
            InitializeComponent();

            for (int i = 2019; i <= 2023; i++)
            {
                YearComboBox.Items.Add(i);
            }
            for (int i = 0; i <= 3; i++)
            {
                PeriodYearComboBox.Items.Add(i);
            }
            for (int i = 0; i <= 12; i++)
            {
                MonthComboBox.Items.Add(i);
                PeriodMonthComboBox.Items.Add(i);
            }
            for (int i = 0; i <= 31; i++)
            {
                DayComboBox.Items.Add(i);
                PeriodDayComboBox.Items.Add(i);
            }
            for (int i = 0; i <= 23; i++)
            {
                HourComboBox.Items.Add(i);
                PeriodHourComboBox.Items.Add(i);
            }
            for (int i = 0; i <= 59; i++)
            {
                MinuteComboBox.Items.Add(i);
                PeriodMinuteComboBox.Items.Add(i);
            }
            foreach (var symbol in LocalStorageApi.SymbolNames)
            {
                TargetComboBox.Items.Add(symbol);
            }

            YearComboBox.SelectedIndex = 4;
            MonthComboBox.SelectedIndex = 1;
            DayComboBox.SelectedIndex = 29;
            HourComboBox.SelectedIndex = 0;
            MinuteComboBox.SelectedIndex = 0;
            PeriodYearComboBox.SelectedIndex = 0;
            PeriodMonthComboBox.SelectedIndex = 0;
            PeriodDayComboBox.SelectedIndex = 2;
            PeriodHourComboBox.SelectedIndex = 0;
            PeriodMinuteComboBox.SelectedIndex = 0;
            TargetComboBox.SelectedIndex = 0;
            AssetTextBox.Text = "100000";
            TargetComboBox.SelectedValue = "BTCUSDT";
            LowPriceTextBox.Text = "22500";
            HighPriceTextBox.Text = "24000";
            LevelsTextBox.Text = "100";
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            var tradingModel = new GridBotBackTestTradingModel()
            {
                Asset = decimal.Parse(AssetTextBox.Text),
                Period = new TimeSpan(int.Parse(PeriodYearComboBox.Text) * 365 + int.Parse(PeriodMonthComboBox.Text) * 30 + int.Parse(PeriodDayComboBox.Text), int.Parse(PeriodHourComboBox.Text), int.Parse(PeriodMinuteComboBox.Text), 0),
                StartTime = new DateTime(int.Parse(YearComboBox.Text), int.Parse(MonthComboBox.Text), int.Parse(DayComboBox.Text), int.Parse(HourComboBox.Text), int.Parse(MinuteComboBox.Text), 0),
                Target = TargetComboBox.Text,
                HighPrice = decimal.Parse(HighPriceTextBox.Text),
                LowPrice = decimal.Parse(LowPriceTextBox.Text),
                Levels = int.Parse(LevelsTextBox.Text)
            };

            progressView.Show();
            worker = new Worker
            {
                ProgressBar = progressView.ProgressBar,
                Action = Run,
                Arguments = tradingModel
            };
            worker.Start();
        }

        private void Run(Worker worker, object? arg)
        {
            if (arg is not GridBotBackTestTradingModel model)
            {
                return;
            }

            // Asset init
            Asset asset = new BackTestAsset(model.Asset, new Position());
            var prices = ChartLoader.GetPricePack(model.Target);

            // If you did not load the target chart data yet, at first, load the chart data.
            if (prices == null)
            {
                TrayMenu.LoadPriceDataEvent(null, new EventArgs(), model.Target, true);
                prices = ChartLoader.GetPricePack(model.Target);
            }

            // Back test start!
            var targetDates = Enumerable.Range(0, (int)model.Period.TotalDays).Select(x => model.StartTime.AddDays(x)).ToList();
            var targetPrices = prices.Prices.Where(p => targetDates.Contains(p.Key)).ToList();
            var priceSequences = new List<decimal>();
            foreach (var targetPrice in targetPrices)
            {
                priceSequences.AddRange(targetPrice.Value);
            }
            var tickCount = priceSequences.Count;

            // Calc grid
            var basePrice = priceSequences[0];
            var gridPrices = new List<decimal>();
            for (int p = 0; p < model.Levels; p++)
            {
                var gridPrice = Math.Round(model.LowPrice + p * ((model.HighPrice - model.LowPrice) / (model.Levels - 1)), 4);
                gridPrices.Add(gridPrice);
            }
            var currentIndex = gridPrices.IndexOf(gridPrices.Where(x => x < basePrice).Max());
            var quantity = Math.Round(model.Asset / model.Levels / ((model.HighPrice + model.LowPrice) / 2), 4);

            worker.For(0, tickCount, 1, (i) =>
            {
                //chartViewer.AddChartInfo(info);
                if (priceSequences[i] <= gridPrices[currentIndex - 1]) // Buy
                {
                    var order = new BackTestOrder(OrderType.Limit, PositionSide.Long, new OrderAmount(OrderAmountType.FixedSymbol, quantity), gridPrices[currentIndex - 1]);
                    var tradeInfo = order.Run(asset, model.Target);
                    trades.Add(tradeInfo);

                    currentIndex--;
                }
                else if (priceSequences[i] >= gridPrices[currentIndex + 1]) // Sell
                {
                    var order = new BackTestOrder(OrderType.Limit, PositionSide.Short, new OrderAmount(OrderAmountType.FixedSymbol, quantity), gridPrices[currentIndex + 1]);
                    var tradeInfo = order.Run(asset, model.Target);
                    trades.Add(tradeInfo);

                    currentIndex++;
                }

                // (TODO) 차트에 거래내역 표시
                //chartViewer.AddTradeInfo(new BackTestTrade(
                //    info.DateTime,
                //    strategy,
                //    tradeInfo.PositionSide
                //    ));
            }, ProgressBarDisplayOptions.Count | ProgressBarDisplayOptions.Percent | ProgressBarDisplayOptions.TimeRemaining);


            var path = PathUtil.Base.Down("MarinerX", $"BackTest_{DateTime.Now.ToStandardFileName()}.csv");
            trades.SaveCsvFile(path);

            DispatcherService.Invoke(() =>
            {
                progressView.Hide();

                var historyView = new BackTestTradingHistoryView();
                historyView.Init(trades);
                historyView.Show();
            });

            //if (param.isShowChart)
            //{
            //    DispatcherService.Invoke(bot.ChartViewer.Show);
            //}
            //else
            //{
            //    ProcessUtil.Start(path);
            //}
        }

        private void HighPriceTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                GridWidthText.Text = GetGridWidths(decimal.Parse(HighPriceTextBox.Text), decimal.Parse(LowPriceTextBox.Text), int.Parse(LevelsTextBox.Text));
            }
            catch
            {
                GridWidthText.Text = "";
            }
        }

        private void LowPriceTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                GridWidthText.Text = GetGridWidths(decimal.Parse(HighPriceTextBox.Text), decimal.Parse(LowPriceTextBox.Text), int.Parse(LevelsTextBox.Text));
            }
            catch
            {
                GridWidthText.Text = "";
            }
        }

        private void LevelsTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                GridWidthText.Text = GetGridWidths(decimal.Parse(HighPriceTextBox.Text), decimal.Parse(LowPriceTextBox.Text), int.Parse(LevelsTextBox.Text));
            }
            catch
            {
                GridWidthText.Text = "";
            }
        }

        private string GetGridWidths(decimal highPrice, decimal lowPrice, int levels)
        {
            var minWidth = Math.Round((highPrice / (lowPrice + (highPrice - lowPrice) * (levels - 2) / (levels - 1)) - 1) * 100, 2);
            var maxWidth = Math.Round(((lowPrice + (highPrice - lowPrice) / (levels - 1)) / lowPrice - 1) * 100, 2);
            return $"{minWidth}% ~ {maxWidth}%";
        }
    }
}
