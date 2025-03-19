using Binance.Net.Enums;

using ChartViewerPrism.Events;
using Mercury.Charts;
using Mercury.Extensions;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;

using SkiaSharp.Views.Desktop;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChartViewerPrism.ViewModels
{
    public class MainWindowViewModel : BindableBase
	{
		private string _title = "Chart Viewer Prism";
		public string Title
		{
			get { return _title; }
			set { SetProperty(ref _title, value); }
		}

		private string _symbol;
		public string Symbol
		{
			get { return _symbol; }
			set { SetProperty(ref _symbol, value); }
		}

		private string _date;
		public string Date
		{
			get { return _date; }
			set { SetProperty(ref _date, value); }
		}

		private string _candleCount;
		public string CandleCount
		{
			get { return _candleCount; }
			set { SetProperty(ref _candleCount, value); }
		}

		private ComboBoxItem _intervalSelectedItem;
		public ComboBoxItem IntervalSelectedItem
		{
			get { return _intervalSelectedItem; }
			set { SetProperty(ref _intervalSelectedItem, value); }
		}

		private int _intervalSelectedIndex;
		public int IntervalSelectedIndex
		{
			get { return _intervalSelectedIndex; }
			set { SetProperty(ref _intervalSelectedIndex, value); }
		}

		private bool _isDateFocused;
		public bool IsDateFocused
		{
			get { return _isDateFocused; }
			set { SetProperty(ref _isDateFocused, value); }
		}

		private bool _isCandleCountFocused;
		public bool IsCandleCountFocused
		{
			get { return _isCandleCountFocused; }
			set { SetProperty(ref _isCandleCountFocused, value); }
		}

		private ObservableCollection<ChartInfo> _candleCharts;
		public ObservableCollection<ChartInfo> CandleCharts
		{
			get { return _candleCharts; }
			set { SetProperty(ref _candleCharts, value); }
		}

		public ICommand SymbolTextChangedCommand { get; set; }
		public ICommand DateTextChangedCommand { get; set; }
		public ICommand CandleCountEnterCommand { get; set; }

		private readonly IEventAggregator _eventAggregator;

		public MainWindowViewModel(IEventAggregator eventAggregator)
		{
			_eventAggregator = eventAggregator;

			SymbolTextChangedCommand = new DelegateCommand<object>(SymbolTextChanged);
			DateTextChangedCommand = new DelegateCommand<object>(DateTextChanged);
			CandleCountEnterCommand = new DelegateCommand<object>(CandleCountEnter);

			Symbol = Settings.Default.Symbol;
			Date = Settings.Default.Date;
			CandleCount = Settings.Default.CandleCount;
			IntervalSelectedIndex = Settings.Default.Interval;

			IsCandleCountFocused = true;
		}

		public void SymbolTextChanged(object obj)
		{
			if(obj is not TextBox textBox)
			{
				return;
			}

			if (textBox.Text.EndsWith("USDT"))
			{
				IsDateFocused = false;
				IsDateFocused = true;
			}
		}

		public void DateTextChanged(object obj)
		{
			if (obj is not TextBox textBox)
			{
				return;
			}

			if (textBox.Text.Length == 4)
			{
				textBox.AppendText("-");
				textBox.CaretIndex = textBox.Text.Length;
			}
			else if (textBox.Text.Length == 7)
			{
				textBox.AppendText("-");
				textBox.CaretIndex = textBox.Text.Length;
			}
			else if (textBox.Text.Length == 10)
			{
				IsCandleCountFocused = false;
				IsCandleCountFocused = true;
			}
		}

		public void CandleCountEnter(object obj)
		{
			if (obj is not TextBox textBox)
			{
				return;
			}

			Settings.Default.Symbol = Symbol;
			Settings.Default.Date = Date;
			Settings.Default.CandleCount = textBox.Text;
			Settings.Default.Interval = IntervalSelectedIndex;
			Settings.Default.Save();

			LoadChart(Symbol, Date.ToDateTime(), ToKlineInterval(IntervalSelectedItem.Content.ToString()), textBox.Text.ToInt());
		}

		void LoadChart(string symbol, DateTime startDate, KlineInterval interval, int candleCount)
		{
			var endDate = startDate.AddSeconds((int)interval * candleCount);
			ChartLoader.Charts.Clear();
			ChartLoader.InitCharts(symbol, interval, startDate, endDate);
			CandleCharts = [.. ChartLoader.GetChartPack(symbol, interval).Charts];
		}

		public KlineInterval ToKlineInterval(string intervalString) => intervalString switch
		{
			"1m" => KlineInterval.OneMinute,
			"3m" => KlineInterval.ThreeMinutes,
			"5m" => KlineInterval.FiveMinutes,
			"15m" => KlineInterval.FifteenMinutes,
			"30m" => KlineInterval.ThirtyMinutes,
			"1h" => KlineInterval.OneHour,
			"2h" => KlineInterval.TwoHour,
			"4h" => KlineInterval.FourHour,
			"6h" => KlineInterval.SixHour,
			"8h" => KlineInterval.EightHour,
			"12h" => KlineInterval.TwelveHour,
			"1D" => KlineInterval.OneDay,
			"3D" => KlineInterval.ThreeDay,
			"1W" => KlineInterval.OneWeek,
			"1M" => KlineInterval.OneMonth,
			_ => KlineInterval.OneMinute
		};
	}
}
