using Backtester.Models;
using Backtester.Views;

using Binance.Net.Enums;

using Mercury;
using Mercury.Backtests;
using Mercury.Charts;
using Mercury.Cryptos;
using Mercury.Maths;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Backtester
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		List<SimpleDealManager> dealResult = new();
		List<PrecisionBacktestDealManager> pbDealResult = new();

		public MainWindow()
		{
			InitializeComponent();

			SymbolTextBox.Text = Settings.Default.Symbol;
			StartDateTextBox.Text = Settings.Default.StartDate;
			EndDateTextBox.Text = Settings.Default.EndDate;
			FileNameTextBox.Text = Settings.Default.FileName;
			SymbolTextBoxPB.Text = Settings.Default.SymbolPB;
			StartDateTextBoxPB.Text = Settings.Default.StartDatePB;
			EndDateTextBoxPB.Text = Settings.Default.EndDatePB;
			FileNameTextBoxPB.Text = Settings.Default.FileNamePB;

			BySymbolGrid.Visibility = Visibility.Visible;
			BySymbolRectangle.Visibility = Visibility.Visible;
			PrecisionBacktestGrid.Visibility = Visibility.Hidden;
			PrecisionBacktestRectangle.Visibility = Visibility.Hidden;

			IntervalComboBoxPB.SelectedIndex = 2;
			StrategyComboBoxPB.Items.Clear();
			StrategyComboBoxPB.Items.Add("TS1 All");
			StrategyComboBoxPB.Items.Add("TS1 Single");
			StrategyComboBoxPB.Items.Add("LSMA All");
			StrategyComboBoxPB.Items.Add("LSMA Single");
			StrategyComboBoxPB.Items.Add("TS2 All");
			StrategyComboBoxPB.Items.Add("TS2 Single");
			StrategyComboBoxPB.Items.Add("MACD V3 All");
			StrategyComboBoxPB.Items.Add("MACD V3 Single");
			StrategyComboBoxPB.Items.Add("BB All");
			StrategyComboBoxPB.Items.Add("BB Single");
			StrategyComboBoxPB.Items.Add("SMACD All");
			StrategyComboBoxPB.Items.Add("SMACD Single");
			StrategyComboBoxPB.Items.Add("MACD V2 All");
			StrategyComboBoxPB.Items.Add("MACD V2 Single");
			StrategyComboBoxPB.Items.Add("S2 TEST All");
			StrategyComboBoxPB.Items.Add("Triple RSI All");
			StrategyComboBoxPB.Items.Add("MACD V2 CUDA All");
			StrategyComboBoxPB.SelectedIndex = 16;

#pragma warning disable CS8625 // Null 리터럴을 null을 허용하지 않는 참조 형식으로 변환할 수 없습니다.
			PrecisionBacktestText_MouseLeftButtonDown(null, null);
#pragma warning restore CS8625 // Null 리터럴을 null을 허용하지 않는 참조 형식으로 변환할 수 없습니다.
		}

		private void SymbolTextBoxPB_TextChanged(object sender, TextChangedEventArgs e)
		{
			try
			{
				if (SymbolTextBoxPB.Text == string.Empty)
				{
					return;
				}

				SymbolCountTextPB.Text = SymbolTextBoxPB.Text.Split(';').Length.ToString();
			}
			catch
			{
			}
		}

		private void BacktestButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Settings.Default.Symbol = SymbolTextBox.Text;
				Settings.Default.StartDate = StartDateTextBox.Text;
				Settings.Default.EndDate = EndDateTextBox.Text;
				Settings.Default.FileName = FileNameTextBox.Text;
				Settings.Default.Save();

				var p1 = Parameter1TextBox.Text.ToDecimal();
				var p2 = Parameter2TextBox.Text.ToDecimal();
				var p3 = Parameter3TextBox.Text.ToDecimal();

				dealResult.Clear();
				var symbols = SymbolTextBox.Text.Split(';');
				var interval = ((IntervalComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "5m").ToKlineInterval();

				BacktestProgress.Value = 0;
				BacktestProgress.Maximum = symbols.Length;
				foreach (var symbol in symbols)
				{
					try
					{
						BacktestProgress.Value++;
						var startDate = StartDateTextBox.Text.ToDateTime() > CryptoSymbol.GetStartDate(symbol).AddDays(1) ? StartDateTextBox.Text.ToDateTime() : CryptoSymbol.GetStartDate(symbol).AddDays(1);
						var endDate = EndDateTextBox.Text.ToDateTime();

						if (startDate >= endDate)
						{
							continue;
						}

						// 차트 로드 및 초기화
						if (ChartLoader.GetChartPack(symbol, interval) == null)
						{
							ChartLoader.InitChartsByDate(symbol, interval, startDate, endDate);
						}

						// 차트 진행하면서 매매
						var charts = ChartLoader.GetChartPack(symbol, interval);

						StrategyEv(charts, p1);
					}
					catch (FileNotFoundException)
					{
						continue;
					}
				}

				foreach (var d in dealResult)
				{
					var content = $"{d.ChartInfo.Symbol},{d.TargetRoe},{d.TotalIncome.Round(2)},{d.BacktestDays},{d.IncomePerDay.Round(2)}" + Environment.NewLine;
					File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBox.Text}.csv"), content);

					if (dealResult.Count == 1)
					{
						var resultView = new BacktestResultView();
						resultView.Init(symbols[0], d);
						resultView.Show();
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void StrategyEv(ChartPack charts, decimal p1)
		{
			charts.CalculateIndicatorsEveryonesCoin();

			var dealManager = new SimpleDealManager(charts.Charts[0].DateTime, charts.Charts[^1].DateTime, 100, 1.85m);
			for (int i = 1; i < charts.Charts.Count; i++)
			{
				if (p1 == 0)
				{
					dealManager.EvaluateEveryonesCoinShort(charts.Charts[i], charts.Charts[i - 1]);
				}
				else if (p1 == 1)
				{
					dealManager.EvaluateEveryonesCoinLong(charts.Charts[i], charts.Charts[i - 1]);
				}
			}

			// Set latest chart for UPNL
			dealManager.ChartInfo = charts.Charts[^1];
			dealResult.Add(dealManager);
		}

		private void StrategyStefano(ChartPack charts)
		{
			charts.CalculateIndicatorsStefano();

			var dealManager = new SimpleDealManager(charts.Charts[0].DateTime, charts.Charts[^1].DateTime, 100, null, 2.0m);
			for (int i = 1; i < charts.Charts.Count; i++)
			{
				dealManager.EvaluateStefanoLong(charts.Charts[i], charts.Charts[i - 1], 0.2m, 0.5m);
			}
			dealManager.ChartInfo = charts.Charts[^1];
			dealResult.Add(dealManager);
		}

		private void BySymbolText_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			BySymbolGrid.Visibility = Visibility.Visible;
			BySymbolRectangle.Visibility = Visibility.Visible;
			PrecisionBacktestGrid.Visibility = Visibility.Hidden;
			PrecisionBacktestRectangle.Visibility = Visibility.Hidden;
		}

		private void PrecisionBacktestText_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			BySymbolGrid.Visibility = Visibility.Hidden;
			BySymbolRectangle.Visibility = Visibility.Hidden;
			PrecisionBacktestGrid.Visibility = Visibility.Visible;
			PrecisionBacktestRectangle.Visibility = Visibility.Visible;
		}

		private void FindCheckpointButtonPB_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Settings.Default.SymbolPB = SymbolTextBoxPB.Text;
				Settings.Default.StartDatePB = StartDateTextBoxPB.Text;
				Settings.Default.EndDatePB = EndDateTextBoxPB.Text;
				Settings.Default.FileNamePB = FileNameTextBoxPB.Text;
				Settings.Default.Save();

				var symbols = SymbolTextBoxPB.Text.Split(';');
				var interval = ((IntervalComboBoxPB.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "5m").ToKlineInterval();
				var startDate = StartDateTextBoxPB.Text.ToDateTime();
				var endDate = EndDateTextBoxPB.Text.ToDateTime();
				var maxCandleCount = MaxCandleCountTextBoxPB.Text.ToInt();

				FindCheckpoints(symbols, interval, startDate, endDate, maxCandleCount);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void BacktestButtonPB_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				Settings.Default.SymbolPB = SymbolTextBoxPB.Text;
				Settings.Default.StartDatePB = StartDateTextBoxPB.Text;
				Settings.Default.EndDatePB = EndDateTextBoxPB.Text;
				Settings.Default.FileNamePB = FileNameTextBoxPB.Text;
				Settings.Default.Save();

				pbDealResult.Clear();
				var symbols = SymbolTextBoxPB.Text.Split(';');
				var interval = ((IntervalComboBoxPB.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "5m").ToKlineInterval();
				var startDate = StartDateTextBoxPB.Text.ToDateTime();
				var endDate = EndDateTextBoxPB.Text.ToDateTime();
				var takeProfitRoe = Parameter1TextBoxPB.Text.ToDecimal();
				var p2 = Parameter2TextBoxPB.Text.ToDouble();

				switch (StrategyComboBoxPB.SelectedIndex)
				{
					case 0:
						Strategy1(symbols, interval, startDate, endDate, takeProfitRoe);
						break;

					case 1:
						Strategy2(symbols, interval, startDate, endDate, takeProfitRoe);
						break;

					case 2:
						Strategy3(symbols, interval, startDate, endDate, takeProfitRoe);
						break;

					case 3:
						Strategy4(symbols, interval, startDate, endDate, takeProfitRoe);
						break;

					case 4:
						Strategy5(symbols, interval, startDate, endDate);
						break;

					case 5:
						Strategy6(symbols, interval, startDate, endDate);
						break;

					case 6:
						Strategy7(symbols, interval, startDate, endDate, takeProfitRoe, 22, 48, 11, 24);
						break;

					case 7:
						Strategy8(symbols, interval, startDate, endDate);
						break;

					case 8:
						Strategy9(symbols, interval, startDate, endDate, takeProfitRoe);
						break;

					case 10:
						Strategy11(symbols, interval, startDate, endDate, takeProfitRoe);
						break;

					case 11:
						Strategy12(symbols, interval, startDate, endDate);
						break;

					case 12:
						Strategy13(symbols, interval, startDate, endDate);
						break;

					case 13:
						Strategy14(symbols, interval, startDate, endDate);
						break;

					case 14:
						StrategyS2Test(symbols, interval, startDate, endDate);
						break;

					case 15:
						StrategyTripleRsi(symbols, interval, startDate, endDate, takeProfitRoe);
						break;

					case 16:
						Strategy13_CUDA(symbols, interval, startDate, endDate);
						break;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		#region MACD Golden/Dead Cross
		public bool IsPowerGoldenCross(IList<ChartInfo> charts, int startIndex, int lookback, double? currentMacd = null)
		{
			for (int i = startIndex; i >= startIndex - lookback; i--)
			{
				var c0 = charts[i];
				var c1 = charts[i - 1];

				if (currentMacd == null)
				{
					if (c0.Macd < 0 && c0.Macd > c0.MacdSignal && c1.Macd < c1.MacdSignal && c0.Supertrend1 > 0 && c0.Adx > 30)
					{
						return true;
					}
				}
				else
				{
					if (c0.Macd < currentMacd && c0.Macd < 0 && c0.Macd > c0.MacdSignal && c1.Macd < c1.MacdSignal && c0.Supertrend1 > 0 && c0.Adx > 30)
					{
						return true;
					}
				}
			}
			return false;
		}

		public bool IsPowerGoldenCross2(IList<ChartInfo> charts, int startIndex, int lookback, double? currentMacd = null)
		{
			for (int i = startIndex; i >= startIndex - lookback; i--)
			{
				var c0 = charts[i];
				var c1 = charts[i - 1];

				if (currentMacd == null)
				{
					if (c0.Macd2 < 0 && c0.Macd2 > c0.MacdSignal2 && c1.Macd2 < c1.MacdSignal2 && c0.Supertrend1 > 0 && c0.Adx > 30)
					{
						return true;
					}
				}
				else
				{
					if (c0.Macd2 < currentMacd && c0.Macd2 < 0 && c0.Macd2 > c0.MacdSignal2 && c1.Macd2 < c1.MacdSignal2 && c0.Supertrend1 > 0 && c0.Adx > 30)
					{
						return true;
					}
				}
			}
			return false;
		}

		public bool IsPowerDeadCross(IList<ChartInfo> charts, int startIndex, int lookback, double? currentMacd = null)
		{
			for (int i = startIndex; i >= startIndex - lookback; i--)
			{
				var c0 = charts[i];
				var c1 = charts[i - 1];

				if (currentMacd == null)
				{
					if (c0.Macd > 0 && c0.Macd < c0.MacdSignal && c1.Macd > c1.MacdSignal && c0.Supertrend1 < 0 && c0.Adx > 30)
					{
						return true;
					}
				}
				else
				{
					if (c0.Macd > currentMacd && c0.Macd > 0 && c0.Macd < c0.MacdSignal && c1.Macd > c1.MacdSignal && c0.Supertrend1 < 0 && c0.Adx > 30)
					{
						return true;
					}
				}
			}
			return false;
		}

		public bool IsPowerDeadCross2(IList<ChartInfo> charts, int startIndex, int lookback, double? currentMacd = null)
		{
			for (int i = startIndex; i >= startIndex - lookback; i--)
			{
				var c0 = charts[i];
				var c1 = charts[i - 1];

				if (currentMacd == null)
				{
					if (c0.Macd2 > 0 && c0.Macd2 < c0.MacdSignal2 && c1.Macd2 > c1.MacdSignal2 && c0.Supertrend1 < 0 && c0.Adx > 30)
					{
						return true;
					}
				}
				else
				{
					if (c0.Macd2 > currentMacd && c0.Macd2 > 0 && c0.Macd2 < c0.MacdSignal2 && c1.Macd2 > c1.MacdSignal2 && c0.Supertrend1 < 0 && c0.Adx > 30)
					{
						return true;
					}
				}
			}
			return false;
		}
		#endregion

		#region Triple Supertrend
		private bool IsTwoGreenSignal(ChartInfo info)
		{
			var count = 0;
			count += info.Supertrend1 > 0 ? 1 : 0;
			count += info.Supertrend2 > 0 ? 1 : 0;
			count += info.Supertrend3 > 0 ? 1 : 0;
			return count >= 2;
		}

		private bool IsTwoRedSignal(ChartInfo info)
		{
			var count = 0;
			count += info.Supertrend1 < 0 ? 1 : 0;
			count += info.Supertrend2 < 0 ? 1 : 0;
			count += info.Supertrend3 < 0 ? 1 : 0;
			return count >= 2;
		}

		private bool IsEntryTs2LongBit(IList<ChartInfo> charts, int startIndex)
		{
			int condition = 0;
			for (int i = startIndex; i >= 0; i--) // 이전 봉 기준
			{
				var chart = charts[i];

				switch (condition)
				{
					case 0:
						if (chart.Supertrend1 > 0 && chart.Supertrend2 > 0 && chart.Supertrend3 > 0)
						{
							condition = 1;
						}
						else
						{
							return false;
						}
						break;
					case 1:
						if (chart.Supertrend1 < 0 && chart.Supertrend2 > 0 && chart.Supertrend3 > 0)
						{
							condition = 2;
						}
						else
						{
							return false;
						}
						break;
					case 2:
						if (chart.Supertrend1 < 0 && chart.Supertrend2 > 0 && chart.Supertrend3 > 0)
						{

						}
						else if (chart.Supertrend1 > 0 && chart.Supertrend2 > 0 && chart.Supertrend3 > 0)
						{
							return true;
						}
						else
						{
							return false;
						}
						break;
				}
			}
			return false;
		}
		private bool IsEntryTs2ShortBit(IList<ChartInfo> charts, int startIndex)
		{
			int condition = 0;
			for (int i = startIndex; i >= 0; i--) // 이전 봉 기준
			{
				var chart = charts[i];

				switch (condition)
				{
					case 0:
						if (chart.Supertrend1 < 0 && chart.Supertrend2 < 0 && chart.Supertrend3 < 0)
						{
							condition = 1;
						}
						else
						{
							return false;
						}
						break;
					case 1:
						if (chart.Supertrend1 > 0 && chart.Supertrend2 < 0 && chart.Supertrend3 < 0)
						{
							condition = 2;
						}
						else
						{
							return false;
						}
						break;
					case 2:
						if (chart.Supertrend1 > 0 && chart.Supertrend2 < 0 && chart.Supertrend3 < 0)
						{

						}
						else if (chart.Supertrend1 < 0 && chart.Supertrend2 < 0 && chart.Supertrend3 < 0)
						{
							return true;
						}
						else
						{
							return false;
						}
						break;
				}
			}
			return false;
		}
		#endregion

		/// <summary>
		/// 체크포인트를 모두 찾는다.
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		/// <param name="maxCandleCount"></param>
		private void FindCheckpoints(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate, int maxCandleCount)
		{
			foreach (var symbol in symbols)
			{
				try
				{
					// 차트 로드 및 초기화
					if (ChartLoader.GetChartPack(symbol, interval) == null)
					{
						ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
					}
				}
				catch
				{
				}
			}

			var title = "Triple Supertrend";
			File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), title + Environment.NewLine);
			foreach (var symbol in symbols)
			{
				var result = new List<DealCheckpoints>();
				var activeDeals = new List<DealCheckpoints>();
				var charts = ChartLoader.GetChartPack(symbol, interval).Charts;

				//==================================================================================

				#region Triple RSI
				//var quotes = charts.Select(x => x.Quote);
				//var adx = quotes.GetAdx(14, 14).Select(x => x.Adx);
				//var rsi1 = quotes.GetRsi(7).Select(x => x.Rsi);
				//var rsi2 = quotes.GetRsi(14).Select(x => x.Rsi);
				//var rsi3 = quotes.GetRsi(21).Select(x => x.Rsi);
				//var ema = quotes.GetEma(50).Select(x => x.Ema);

				//for (int i = 0; i < charts.Count; i++)
				//{
				//    var _chart = charts[i];
				//    _chart.Adx = adx.ElementAt(i);
				//    _chart.Rsi1 = rsi1.ElementAt(i);
				//    _chart.Rsi2 = rsi2.ElementAt(i);
				//    _chart.Rsi3 = rsi3.ElementAt(i);
				//    _chart.Ema1 = ema.ElementAt(i);
				//}
				#endregion

				#region Double MACD
				//var quotes = charts.Select(x => x.Quote);
				//var m = quotes.GetMacd(22, 48, 9).Select(x => x.Macd);
				//var m2 = quotes.GetMacd(11, 24, 7).Select(x => x.Macd);
				//var st = quotes.GetSupertrend(10, 1.5).Select(x => x.Supertrend);
				//var adx = quotes.GetAdx(14, 14).Select(x => x.Adx);

				//for (int i = 0; i < charts.Count; i++)
				//{
				//    var _chart = charts[i];
				//    _chart.Macd = m.ElementAt(i);
				//    _chart.Macd2 = m2.ElementAt(i);
				//    _chart.Supertrend1 = st.ElementAt(i);
				//    _chart.Adx = adx.ElementAt(i);
				//}
				#endregion

				#region MACD 4.1
				//var quotes = charts.Select(x => x.Quote);
				//var macd = quotes.GetMacd(12, 26, 9);
				//var m = macd.Select(x => x.Macd);
				//var s = macd.Select(x => x.Signal);
				//var macd2 = quotes.GetMacd(9, 20, 7);
				//var m2 = macd2.Select(x => x.Macd);
				//var s2 = macd2.Select(x => x.Signal);
				//var st = quotes.GetSupertrend(10, 1.5).Select(x => x.Supertrend);
				//var adx = quotes.GetAdx(14, 14).Select(x => x.Adx);

				//for (int i = 0; i < charts.Count; i++)
				//{
				//    var _chart = charts[i];
				//    _chart.Macd = m.ElementAt(i);
				//    _chart.MacdSignal = s.ElementAt(i);
				//    _chart.Macd2 = m2.ElementAt(i);
				//    _chart.MacdSignal2 = s2.ElementAt(i);
				//    _chart.Supertrend1 = st.ElementAt(i);
				//    _chart.Adx = adx.ElementAt(i);
				//}
				#endregion

				#region Triple Supertrend
				var quotes = charts.Select(x => x.Quote);
				var ts = quotes.GetTripleSupertrend(10, 1.2, 10, 3, 10, 10);
				var r1 = ts.Select(x => x.Supertrend1);
				var r2 = ts.Select(x => x.Supertrend2);
				var r3 = ts.Select(x => x.Supertrend3);
				var r4 = quotes.GetEma(200).Select(x => x.Ema);
				var r5 = quotes.GetStochasticRsi(3, 3, 14, 14).Select(x => x.K);

				for (int i = 0; i < charts.Count; i++)
				{
					var _chart = charts[i];
					_chart.Supertrend1 = r1.ElementAt(i);
					_chart.Supertrend2 = r2.ElementAt(i);
					_chart.Supertrend3 = r3.ElementAt(i);
					_chart.Ema1 = r4.ElementAt(i);
					_chart.K = r5.ElementAt(i);
				}
				#endregion

				//==================================================================================

				for (int i = 240; i < charts.Count; i++)
				{
					var c = charts[i];
					var c1 = charts[i - 1];
					var h = c.Quote.High;
					var l = c.Quote.Low;
					var e = c.Quote.Close;

					//==================================================================================

					#region Triple RSI
					//if (c.Rsi3 > 50 && c.Rsi1 > c.Rsi2 && c.Rsi2 > c.Rsi3 && c.Quote.Close > (decimal)c.Ema1 && c.Adx > 20)
					//{
					//    activeDeals.Add(new DealCheckpoints(symbol, c.DateTime, c.Quote.Close, PositionSide.Long));
					//}

					//if (c.Rsi3 < 50 && c.Rsi1 < c.Rsi2 && c.Rsi2 < c.Rsi3 && c.Quote.Close < (decimal)c.Ema1 && c.Adx > 20)
					//{
					//    activeDeals.Add(new DealCheckpoints(symbol, c.DateTime, c.Quote.Close, PositionSide.Short));
					//}
					#endregion

					#region Double MACD
					//if (c.Macd < 0 && c.Macd2 < 0 && c.Macd2 > c.Macd && c1.Macd2 < c1.Macd)
					//{
					//    activeDeals.Add(new DealCheckpoints(symbol, c.DateTime, c.Quote.Close, PositionSide.Long));
					//}
					//if (c.Macd > 0 && c.Macd2 > 0 && c.Macd2 < c.Macd && c1.Macd2 > c1.Macd)
					//{
					//    activeDeals.Add(new DealCheckpoints(symbol, c.DateTime, c.Quote.Close, PositionSide.Short));
					//}
					#endregion

					#region MACD 4.1
					//if (IsPowerGoldenCross(charts, i, 14, c.Macd) && 
					//    IsPowerGoldenCross2(charts, i, 14, c.Macd2))
					//{
					//    activeDeals.Add(new DealCheckpoints(symbol, c.DateTime, c.Quote.Close, PositionSide.Long));
					//}

					//if (IsPowerDeadCross(charts, i, 14, c.Macd) &&
					//    IsPowerDeadCross2(charts, i, 14, c.Macd2))
					//{
					//    activeDeals.Add(new DealCheckpoints(symbol, c.DateTime, c.Quote.Close, PositionSide.Short));
					//}
					#endregion

					#region Triple Supertrend
					//if (IsTwoGreenSignal(c) && e > (decimal)c.Ema1 && c.K > 20 && c1.K < 20)
					//{
					//    activeDeals.Add(new DealCheckpoints(symbol, c.DateTime, c.Quote.Close, PositionSide.Long));
					//}

					//if (IsTwoRedSignal(c) && e < (decimal)c.Ema1 && c.K < 80 && c1.K > 80)
					//{
					//    activeDeals.Add(new DealCheckpoints(symbol, c.DateTime, c.Quote.Close, PositionSide.Short));
					//}
					#endregion

					#region Triple Supertrend2
					//if (c.Supertrend1 > 0 && c1.Supertrend1 < 0 && c.Supertrend2 > 0 && c.Supertrend3 > 0)
					//{
					//    activeDeals.Add(new DealCheckpoints(symbol, c.DateTime, c.Quote.Close, PositionSide.Long));
					//}

					//if (c.Supertrend1 < 0 && c1.Supertrend1 > 0 && c.Supertrend2 < 0 && c.Supertrend3 < 0)
					//{
					//    activeDeals.Add(new DealCheckpoints(symbol, c.DateTime, c.Quote.Close, PositionSide.Short));
					//}
					#endregion

					#region Triple Supertrend3
					if (IsEntryTs2LongBit(charts, i))
					{
						activeDeals.Add(new DealCheckpoints(symbol, c.DateTime, c.Quote.Close, PositionSide.Long));
					}

					if (IsEntryTs2ShortBit(charts, i))
					{
						activeDeals.Add(new DealCheckpoints(symbol, c.DateTime, c.Quote.Close, PositionSide.Short));
					}
					#endregion

					//==================================================================================

					var dealsToRemove = new List<DealCheckpoints>();
					foreach (var activeDeal in activeDeals)
					{
						activeDeal.Life++;

						activeDeal.EvaluateCheckpoint(c);

						// Max Candle Count 까지만 계산
						// 계산이 완료된 Deal은 Result에 적재
						if (activeDeal.Life >= maxCandleCount)
						{
							activeDeal.ArrangeHistories();
							result.Add(activeDeal);
							dealsToRemove.Add(activeDeal);
						}
					}

					// 삭제
					foreach (var dealToRemove in dealsToRemove)
					{
						activeDeals.Remove(dealToRemove);
					}
				}

				// 진입 시그널 레이팅 계산
				var temp = result.Select(x => x.CalculateRoeRangeAverage(0.5, 10.0)).ToArray();
				var rating = 5 * ArrayCalculator.GeometricMean(temp);

				// 목표 수익별 승률 계산
				//var dealResults = new List<DealCheckpointTestResult>();
				//for (decimal targetRoe = 0.5m; targetRoe <= 3.0m; targetRoe += 0.05m)
				//{
				//    var dealResult = new DealCheckpointTestResult
				//    {
				//        TargetRoe = targetRoe
				//    };
				//    foreach (var deal in result)
				//    {
				//        switch (deal.EvaluateDealResult(targetRoe))
				//        {
				//            case 1:
				//                dealResult.Win++;
				//                break;

				//            case -1:
				//                dealResult.Lose++;
				//                break;

				//            case 0:
				//                dealResult.Draw++;
				//                break;
				//        }
				//    }
				//    dealResults.Add(dealResult);
				//}
				//var highestWinRateResult = dealResults.Find(x => x.WinRate == dealResults.Max(y => y.WinRate));

				// 결과 저장
				var builder = new StringBuilder();
				builder.Append($"{symbol},{rating.Round(4)}");
				//foreach(var deal in dealResults)
				//{
				//    builder.AppendLine(deal.ToString());
				//}
				File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), builder.ToString() + Environment.NewLine);
			}
		}

		/// <summary>
		/// 심볼 전체 테스트
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		private void Strategy1(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate, decimal takeProfitRoe)
		{
			foreach (var symbol in symbols)
			{
				try
				{
					// 차트 로드 및 초기화
					if (ChartLoader.GetChartPack(symbol, interval) == null)
					{
						ChartLoader.InitChartsAndTs1IndicatorsByDate(symbol, interval, startDate, endDate);
					}
				}
				catch
				{
				}
			}

			var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 25, takeProfitRoe, takeProfitRoe, 100)
			{
				MonitoringSymbols = symbols.ToList()
			};
			var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

			var headerContent = $"{dealManager.StartDate:yyyy-MM-dd HH:mm:ss}~{dealManager.EndDate:yyyy-MM-dd HH:mm:ss}, {interval.ToIntervalString()}" + Environment.NewLine;
			File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), headerContent);
			ChartLoader.SelectCharts();
			int i = 1;
			for (; i < 5; i++)
			{
				var nextCharts = ChartLoader.NextCharts();
				dealManager.ConcatenateChart(nextCharts);
			}
			for (; i < evaluateCount; i++)
			{
				var nextCharts = ChartLoader.NextCharts();
				dealManager.ConcatenateChart(nextCharts);

				if (dealManager.Charts[symbols[0]].Count >= 220)
				{
					dealManager.RemoveOldChart();
				}

				dealManager.EvaluateTsLongNextCandle();
				dealManager.EvaluateTsShortNextCandle();

				if (i % 96 == 0)
				{
					var content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
					File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), content);
				}
			}

			var _content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine + Environment.NewLine;
			File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);

			foreach (var h in dealManager.PositionHistories)
			{
				File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"),
					$"{h.EntryTime},{h.Symbol},{h.Side},{h.Time},{h.Result}" + Environment.NewLine
					);
			}

			var resultChartView = new BacktestResultChartView();
			resultChartView.Init(dealManager.PositionHistories, interval);
			resultChartView.Show();
		}

		/// <summary>
		/// 심볼 개별 테스트
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		private void Strategy2(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate, decimal takeProfitRoe)
		{
			foreach (var symbol in symbols)
			{
				try
				{
					ChartLoader.Charts.Clear();
					ChartLoader.InitChartsAndTs1IndicatorsByDate(symbol, interval, startDate, endDate);
				}
				catch
				{
				}

				var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 25, takeProfitRoe, takeProfitRoe, 100)
				{
					MonitoringSymbols = new List<string>() { symbol }
				};
				var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

				ChartLoader.SelectCharts();
				int i = 1;
				for (; i < 5; i++)
				{
					var nextCharts = ChartLoader.NextCharts();
					dealManager.ConcatenateChart(nextCharts);
				}
				for (; i < evaluateCount; i++)
				{
					var nextCharts = ChartLoader.NextCharts();
					dealManager.ConcatenateChart(nextCharts);

					if (dealManager.Charts[symbol].Count >= 220)
					{
						dealManager.RemoveOldChart();
					}

					dealManager.EvaluateTsLongNextCandle();
					dealManager.EvaluateTsShortNextCandle();
				}

				var _content = $"{symbol},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
				File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);
			}
		}

		/// <summary>
		/// LSMA 전체 테스트
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		/// <param name="takeProfitRoe"></param>
		private void Strategy3(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate, decimal takeProfitRoe)
		{
			foreach (var symbol in symbols)
			{
				try
				{
					// 차트 로드 및 초기화
					if (ChartLoader.GetChartPack(symbol, interval) == null)
					{
						ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
					}
				}
				catch
				{
				}
			}

			var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 25, takeProfitRoe, takeProfitRoe / -2.0m, 0.2m)
			{
				MonitoringSymbols = symbols.ToList()
			};
			var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

			ChartLoader.SelectCharts();
			int i = 1;
			for (; i < 33; i++)
			{
				var nextCharts = ChartLoader.NextCharts();
				dealManager.ConcatenateChart(nextCharts);
			}
			for (; i < evaluateCount; i++)
			{
				var nextCharts = ChartLoader.NextCharts();
				dealManager.ConcatenateChart(nextCharts);

				if (dealManager.Charts[symbols[0]].Count >= 50)
				{
					dealManager.RemoveOldChart();
				}

				dealManager.CalculateIndicatorsLsma();
				dealManager.EvaluateLsmaLongNextCandle();
				dealManager.EvaluateLsmaShortNextCandle();

				if (i % 96 == 0)
				{
					var content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.SimplePnl.Round(2)}" + Environment.NewLine;
					File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), content);
				}
			}

			var _content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.SimplePnl.Round(2)}" + Environment.NewLine;
			File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);

			foreach (var h in dealManager.PositionHistories)
			{
				File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"),
					$"{h.EntryTime},{h.Symbol},{h.Side},{h.Time},{h.Result}" + Environment.NewLine
					);
			}
		}

		/// <summary>
		/// LSMA 개별 테스트
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		/// <param name="takeProfitRoe"></param>
		private void Strategy4(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate, decimal takeProfitRoe)
		{

		}

		/// <summary>
		/// TS2 전체 테스트
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		private void Strategy5(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate)
		{
			foreach (var symbol in symbols)
			{
				try
				{
					// 차트 로드 및 초기화
					if (ChartLoader.GetChartPack(symbol, interval) == null)
					{
						ChartLoader.InitChartsAndTs2IndicatorsByDate(symbol, interval, startDate, endDate);
					}
				}
				catch
				{
				}
			}

			var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 25, 1.0m, 1.0m, 100)
			{
				MonitoringSymbols = symbols.ToList()
			};
			var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

			var headerContent = $"TS2, {dealManager.StartDate:yyyy-MM-dd}~{dealManager.EndDate:yyyy-MM-dd}, {interval.ToIntervalString()}" + Environment.NewLine;
			File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), headerContent);
			ChartLoader.SelectCharts();
			int i = 1;
			for (; i < 20; i++)
			{
				var nextCharts = ChartLoader.NextCharts();
				dealManager.ConcatenateChart(nextCharts);
			}
			for (; i < evaluateCount; i++)
			{
				var nextCharts = ChartLoader.NextCharts();
				dealManager.ConcatenateChart(nextCharts);

				if (dealManager.Charts[symbols[0]].Count >= 200)
				{
					dealManager.RemoveOldChart();
				}

				dealManager.EvaluateTs2LongBit();
				dealManager.EvaluateTs2ShortBit();

				if (i % 720 == 0)
				{
					var content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
					File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), content);
				}
			}

			var _content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine + Environment.NewLine;
			File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);

			var hi = 1;
			foreach (var h in dealManager.PositionHistories)
			{
				File.AppendAllText(MercuryPath.Desktop.Down($"PH-TS2.csv"),
					$"{hi++},{h.EntryTime},{h.Symbol},{h.Side},{h.Time},{h.Result},{h.Income}" + Environment.NewLine
					);
			}

			var resultChartView = new BacktestResultChartView();
			resultChartView.Init(dealManager.PositionHistories, interval);
			resultChartView.Show();
		}

		/// <summary>
		/// TS2 개별 테스트
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		/// <param name="takeProfitRoe"></param>
		private void Strategy6(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate)
		{
			foreach (var symbol in symbols)
			{
				try
				{
					ChartLoader.Charts.Clear();
					ChartLoader.InitChartsAndTs2IndicatorsByDate(symbol, interval, startDate, endDate);
				}
				catch
				{
				}

				var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 25, 1.0m, 1.0m, 100)
				{
					MonitoringSymbols = new List<string>() { symbol }
				};
				var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

				ChartLoader.SelectCharts();
				int i = 1;
				for (; i < 20; i++)
				{
					var nextCharts = ChartLoader.NextCharts();
					dealManager.ConcatenateChart(nextCharts);
				}
				for (; i < evaluateCount; i++)
				{
					var nextCharts = ChartLoader.NextCharts();
					dealManager.ConcatenateChart(nextCharts);

					if (dealManager.Charts[symbol].Count >= 200)
					{
						dealManager.RemoveOldChart();
					}

					dealManager.EvaluateTs2LongBit();
					dealManager.EvaluateTs2ShortBit();
				}

				var _content = $"{symbol},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
				File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);
			}
		}

		/// <summary>
		/// MACD V3 전체 테스트
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		/// <param name="takeProfitRoe"></param>
		private void Strategy7(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate, decimal takeProfitRoe, int v1, int v2, int v3, int v4)
		{
			decimal min = 99999999;
			decimal max = 0;

			foreach (var symbol in symbols)
			{
				try
				{
					// 차트 로드 및 초기화
					if (ChartLoader.GetChartPack(symbol, interval) == null)
					{
						//ChartLoader.InitChartsByDate(symbol, interval, startDate, endDate);
						ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
					}
				}
				catch
				{
				}
			}

			File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), $"MACD1 ({v1},{v2}) MACD2 ({v3},{v4})" + Environment.NewLine);

			var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 5, takeProfitRoe, takeProfitRoe / -2.0m, 0.2m)
			{
				MonitoringSymbols = symbols.ToList()
			};
			var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

			ChartLoader.SelectCharts();
			int i = 1;
			for (; i < 240; i++)
			{
				var nextCharts = ChartLoader.NextCharts();
				dealManager.ConcatenateChart(nextCharts);
			}
			for (; i < evaluateCount; i++)
			{
				var nextCharts = ChartLoader.NextCharts();
				dealManager.ConcatenateChart(nextCharts);

				if (dealManager.Charts[symbols[0]].Count >= 260)
				{
					dealManager.RemoveOldChart();
				}

				dealManager.CalculateIndicatorsMacd(v1, v2, v3, v4);
				dealManager.EvaluateMacdV3LongNextCandle();
				dealManager.EvaluateMacdV3ShortNextCandle();

				if (i % 288 == 0)
				{
					var content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
					File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), content);

					if (dealManager.EstimatedMoney < min)
					{
						min = dealManager.EstimatedMoney;
					}
					if (dealManager.EstimatedMoney > max)
					{
						max = dealManager.EstimatedMoney;
					}
				}
			}

			if (dealManager.EstimatedMoney < min)
			{
				min = dealManager.EstimatedMoney;
			}
			if (dealManager.EstimatedMoney > max)
			{
				max = dealManager.EstimatedMoney;
			}

			var _content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
			File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);
			File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), $"{dealManager.EstimatedMoney.Round(2)},{min.Round(2)},{max.Round(2)},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)}");
			File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), Environment.NewLine + Environment.NewLine);

			foreach (var h in dealManager.PositionHistories)
			{
				File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"),
					$"{h.EntryTime},{h.Symbol},{h.Side},{h.Time},{h.Result},{Math.Round(h.Income, 4)}" + Environment.NewLine
					);
			}
			File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"), Environment.NewLine + Environment.NewLine);

			//var resultChartView = new BacktestResultChartView();
			//resultChartView.Init(dealManager.PositionHistories, interval);
			//resultChartView.Show();
		}

		/// <summary>
		/// MACD V3 개별 테스트
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		private void Strategy8(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate)
		{
			foreach (var symbol in symbols)
			{
				try
				{
					ChartLoader.Charts.Clear();
					ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
				}
				catch
				{
				}

				var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 3, 1.0m, 1.0m, 0.2m)
				{
					MonitoringSymbols = new List<string>() { symbol }
				};
				var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

				ChartLoader.SelectCharts();
				int i = 1;
				for (; i < 240; i++)
				{
					var nextCharts = ChartLoader.NextCharts();
					dealManager.ConcatenateChart(nextCharts);
				}
				for (; i < evaluateCount; i++)
				{
					var nextCharts = ChartLoader.NextCharts();
					dealManager.ConcatenateChart(nextCharts);

					if (dealManager.Charts[symbol].Count >= 260)
					{
						dealManager.RemoveOldChart();
					}

					dealManager.CalculateIndicatorsMacd(1, 1, 1, 1);
					dealManager.EvaluateMacdV3LongNextCandle();
					dealManager.EvaluateMacdV3ShortNextCandle();
				}

				var _content = $"{symbol},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
				File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);
			}
		}

		/// <summary>
		/// BB 전체 테스트
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		/// <param name="takeProfitRoe"></param>
		private void Strategy9(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate, decimal takeProfitRoe)
		{
			foreach (var symbol in symbols)
			{
				try
				{
					// 차트 로드 및 초기화
					if (ChartLoader.GetChartPack(symbol, interval) == null)
					{
						ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
					}
				}
				catch
				{
				}
			}

			var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 10, takeProfitRoe, takeProfitRoe / -2.0m, 0.2m)
			{
				MonitoringSymbols = symbols.ToList()
			};
			var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

			ChartLoader.SelectCharts();
			int i = 1;
			for (; i < 240; i++)
			{
				var nextCharts = ChartLoader.NextCharts();
				dealManager.ConcatenateChart(nextCharts);
			}
			for (; i < evaluateCount; i++)
			{
				var nextCharts = ChartLoader.NextCharts();
				dealManager.ConcatenateChart(nextCharts);

				if (dealManager.Charts[symbols[0]].Count >= 260)
				{
					dealManager.RemoveOldChart();
				}

				dealManager.CalculateIndicatorsBb();
				dealManager.EvaluateBbLongNextCandle();
				dealManager.EvaluateBbShortNextCandle();

				if (i % 288 == 0)
				{
					var content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
					File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), content);
				}
			}

			var _content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine + Environment.NewLine;
			File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);

			foreach (var h in dealManager.PositionHistories)
			{
				File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"),
					$"{h.EntryTime},{h.Symbol},{h.Side},{h.Time},{h.Result},{Math.Round(h.Income, 4)}" + Environment.NewLine
					);
			}

			var resultChartView = new BacktestResultChartView();
			resultChartView.Init(dealManager.PositionHistories, interval);
			resultChartView.Show();
		}

		/// <summary>
		/// SMACD 전체 테스트
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		/// <param name="takeProfitRoe"></param>
		private void Strategy11(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate, decimal takeProfitRoe)
		{
			foreach (var symbol in symbols)
			{
				try
				{
					// 차트 로드 및 초기화
					if (ChartLoader.GetChartPack(symbol, interval) == null)
					{
						ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
					}
				}
				catch
				{
				}
			}

			var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 10, takeProfitRoe, takeProfitRoe / -2.0m, 0.2m)
			{
				MonitoringSymbols = symbols.ToList()
			};
			var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

			ChartLoader.SelectCharts();
			int i = 1;
			for (; i < 240; i++)
			{
				var nextCharts = ChartLoader.NextCharts();
				dealManager.ConcatenateChart(nextCharts);
			}
			dealManager.CalculateInitSmacd();
			for (; i < evaluateCount; i++)
			{
				var nextCharts = ChartLoader.NextCharts();
				dealManager.ConcatenateChart(nextCharts);

				if (dealManager.Charts[symbols[0]].Count >= 260)
				{
					dealManager.RemoveOldChart();
				}

				dealManager.CalculateIndicatorsSmacd();
				dealManager.EvaluateSmacdLongNextCandle();
				dealManager.EvaluateSmacdShortNextCandle();

				if (i % 288 == 0)
				{
					var content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.PositionHistories.Count},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
					File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), content);
				}
			}

			var _content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.PositionHistories.Count},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine + Environment.NewLine;
			File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);

			foreach (var h in dealManager.PositionHistories)
			{
				File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"),
					$"{h.EntryTime},{h.Symbol},{h.Side},{h.Time},{h.EntryCount},{Math.Round(h.Income, 4)}" + Environment.NewLine
					);
			}

			var resultChartView = new BacktestResultChartView();
			resultChartView.Init(dealManager.PositionHistories, interval);
			resultChartView.Show();
		}

		/// <summary>
		/// SMACD 개별 테스트
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		private void Strategy12(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate)
		{
			foreach (var symbol in symbols)
			{
				try
				{
					ChartLoader.Charts.Clear();
					ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
				}
				catch
				{
				}

				var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 25, 1.0m, 1.0m, 0.2m)
				{
					MonitoringSymbols = new List<string>() { symbol }
				};
				var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

				ChartLoader.SelectCharts();
				int i = 1;
				for (; i < 240; i++)
				{
					var nextCharts = ChartLoader.NextCharts();
					dealManager.ConcatenateChart(nextCharts);
				}
				dealManager.CalculateInitSmacd();
				for (; i < evaluateCount; i++)
				{
					var nextCharts = ChartLoader.NextCharts();
					dealManager.ConcatenateChart(nextCharts);

					if (dealManager.Charts[symbol].Count >= 260)
					{
						dealManager.RemoveOldChart();
					}

					dealManager.CalculateIndicatorsSmacd();
					dealManager.EvaluateSmacdLongNextCandle();
					dealManager.EvaluateSmacdShortNextCandle();
				}

				var _content = $"{symbol},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
				File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);
			}
		}

		/// <summary>
		/// MACD V2 전체 테스트
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		/// <param name="takeProfitRoe"></param>
		private void Strategy13(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate)
		{
			foreach (var symbol in symbols)
			{
				try
				{
					// 차트 로드 및 초기화
					if (ChartLoader.GetChartPack(symbol, interval) == null)
					{
						ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
					}
				}
				catch
				{
				}
			}

			var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 10, 2, 1, 0.2m)
			{
				MonitoringSymbols = symbols.ToList()
			};
			var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

			ChartLoader.SelectCharts();
			int i = 1;
			for (; i < 240; i++)
			{
				var nextCharts = ChartLoader.NextCharts();
				dealManager.ConcatenateChart(nextCharts);
			}
			for (; i < evaluateCount; i++)
			{
				var nextCharts = ChartLoader.NextCharts();
				dealManager.ConcatenateChart(nextCharts);

				if (dealManager.Charts[symbols[0]].Count >= 260)
				{
					dealManager.RemoveOldChart();
				}

				dealManager.CalculateIndicatorsMacd(12, 26, 1, 1);
				dealManager.EvaluateMacdV2LongNextCandle();
				dealManager.EvaluateMacdV2ShortNextCandle();

				if (i % 288 == 0)
				{
					var content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
					File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), content);
				}
			}

			var _content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine + Environment.NewLine;
			File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);

			foreach (var h in dealManager.PositionHistories)
			{
				File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"),
					$"{h.EntryTime},{h.Symbol},{h.Side},{h.Time},{h.Result},{Math.Round(h.Income, 4)}" + Environment.NewLine
					);
			}
			File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"), Environment.NewLine + Environment.NewLine);

			var resultChartView = new BacktestResultChartView();
			resultChartView.Init(dealManager.PositionHistories, interval);
			resultChartView.Show();
		}

		/// <summary>
		/// MACD V2 전체 테스트_CUDA
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		private void Strategy13_CUDA(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate)
		{
			foreach (var symbol in symbols)
			{
				try
				{
					// 차트 로드 및 초기화
					if (ChartLoader.GetChartPack(symbol, interval) == null)
					{
						ChartLoader.InitCharts(symbol, interval, startDate, endDate);
					}
				}
				catch
				{
				}
			}

			var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 10, 2, 1, 0.2m);
			var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

			foreach (var symbol in symbols)
			{
				var chartPack = ChartLoader.GetChartPack(symbol, interval);

				// 지표 계산
				chartPack.UseMacd();
				chartPack.UseAdx();
				chartPack.UseSupertrend(10, 1.5);

				// 매니저에 차트 추가
				dealManager.AddChart(symbol, [.. chartPack.Charts]);
			}

			for (int i = 240; i < evaluateCount; i++)
			{
				foreach(var symbol in symbols)
				{
					dealManager.EvaluateMacdV2LongNextCandleCUDA(symbol, i);
					dealManager.EvaluateMacdV2ShortNextCandleCUDA(symbol, i);
				}

				if (i % 288 == 0)
				{
					var content = $"{dealManager.Charts[symbols[0]][i].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
					File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), content);
				}
			}

			var _content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine + Environment.NewLine;
			File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);

			foreach (var h in dealManager.PositionHistories)
			{
				File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"),
					$"{h.EntryTime},{h.Symbol},{h.Side},{h.Time},{h.Result},{Math.Round(h.Income, 4)}" + Environment.NewLine
					);
			}
			File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"), Environment.NewLine + Environment.NewLine);

			var resultChartView = new BacktestResultChartView();
			resultChartView.Init(dealManager.PositionHistories, interval);
			resultChartView.Show();
		}

		/// <summary>
		/// MACD V2 개별 테스트
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		private void Strategy14(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate)
		{
			foreach (var symbol in symbols)
			{
				try
				{
					ChartLoader.Charts.Clear();
					ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
				}
				catch
				{
				}

				var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 25, 1.0m, 1.0m, 0.2m)
				{
					MonitoringSymbols = new List<string>() { symbol }
				};
				var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

				ChartLoader.SelectCharts();
				int i = 1;
				for (; i < 240; i++)
				{
					var nextCharts = ChartLoader.NextCharts();
					dealManager.ConcatenateChart(nextCharts);
				}
				for (; i < evaluateCount; i++)
				{
					var nextCharts = ChartLoader.NextCharts();
					dealManager.ConcatenateChart(nextCharts);

					if (dealManager.Charts[symbol].Count >= 260)
					{
						dealManager.RemoveOldChart();
					}

					dealManager.CalculateIndicatorsMacd(1, 1, 1, 1);
					dealManager.EvaluateMacdV2LongNextCandle();
					dealManager.EvaluateMacdV2ShortNextCandle();
				}

				var _content = $"{symbol},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
				File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);
			}
		}

		/// <summary>
		/// Season2 Strategy 전체 테스트
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		/// <param name="takeProfitRoe"></param>
		private void StrategyS2Test(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate)
		{
			foreach (var symbol in symbols)
			{
				try
				{
					// 차트 로드 및 초기화
					if (ChartLoader.GetChartPack(symbol, interval) == null)
					{
						ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
					}
				}
				catch
				{
				}
			}

			var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 10, 2, 1, 0.2m)
			{
				MonitoringSymbols = symbols.ToList()
			};
			var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

			ChartLoader.SelectCharts();
			int i = 1;
			for (; i < 240; i++)
			{
				var nextCharts = ChartLoader.NextCharts();
				dealManager.ConcatenateChart(nextCharts);
			}
			for (; i < evaluateCount; i++)
			{
				var nextCharts = ChartLoader.NextCharts();
				dealManager.ConcatenateChart(nextCharts);

				if (dealManager.Charts[symbols[0]].Count >= 260)
				{
					dealManager.RemoveOldChart();
				}

				dealManager.CalculateIndicatorsStochRsiEma();
				dealManager.EvaluateStochRsiEmaLong();
				dealManager.EvaluateStochRsiEmaShort();

				if (i % 288 == 0)
				{
					var content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
					File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), content);
				}
			}

			var _content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine + Environment.NewLine;
			File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content);

			foreach (var h in dealManager.PositionHistories)
			{
				File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"),
					$"{h.EntryTime},{h.Symbol},{h.Side},{h.Time},{h.Result},{Math.Round(h.Income, 4)}" + Environment.NewLine
					);
			}
			File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"), Environment.NewLine + Environment.NewLine);

			var resultChartView = new BacktestResultChartView();
			resultChartView.Init(dealManager.PositionHistories, interval);
			resultChartView.Show();
		}

		/// <summary>
		/// Triple RSI 전체 테스트
		/// </summary>
		/// <param name="symbols"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		/// <param name="takeProfitRoe"></param>
		/// <param name="v1"></param>
		/// <param name="v2"></param>
		/// <param name="v3"></param>
		/// <param name="v4"></param>
		private void StrategyTripleRsi(string[] symbols, KlineInterval interval, DateTime startDate, DateTime endDate, decimal takeProfitRoe)
		{
			foreach (var symbol in symbols)
			{
				try
				{
					// 차트 로드 및 초기화
					if (ChartLoader.GetChartPack(symbol, interval) == null)
					{
						ChartLoader.InitChartsMByDate(symbol, interval, startDate, endDate);
					}
				}
				catch
				{
				}
			}

			File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), $"Triple RSI" + Environment.NewLine);

			var dealManager = new PrecisionBacktestDealManager(startDate, endDate, 5, takeProfitRoe, takeProfitRoe / -2.0m, 0.2m)
			{
				MonitoringSymbols = symbols.ToList()
			};
			var evaluateCount = (int)((endDate - startDate).TotalMinutes / ((int)interval / 60));

			ChartLoader.SelectCharts();
			int i = 1;
			for (; i < 240; i++)
			{
				var nextCharts = ChartLoader.NextCharts();
				dealManager.ConcatenateChart(nextCharts);
			}
			for (; i < evaluateCount; i++)
			{
				var nextCharts = ChartLoader.NextCharts();
				dealManager.ConcatenateChart(nextCharts);

				if (dealManager.Charts[symbols[0]].Count >= 260)
				{
					dealManager.RemoveOldChart();
				}

				dealManager.CalculateIndicatorsTripleRsi();
				dealManager.EvaluateTripleRsiLong();
				dealManager.EvaluateTripleRsiShort();

				if (i % 288 == 0)
				{
					var content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
					File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), content);
				}
			}

			var _content = $"{dealManager.Charts[symbols[0]][^1].DateTime:yyyy-MM-dd HH:mm:ss},{dealManager.Win},{dealManager.Lose},{dealManager.WinRate.Round(2)},{dealManager.LongPositionCount},{dealManager.ShortPositionCount},{dealManager.EstimatedMoney.Round(2)}" + Environment.NewLine;
			File.AppendAllText(MercuryPath.Desktop.Down($"{FileNameTextBoxPB.Text}.csv"), _content + Environment.NewLine + Environment.NewLine);

			foreach (var h in dealManager.PositionHistories)
			{
				File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"),
					$"{h.EntryTime},{h.Symbol},{h.Side},{h.Time},{h.Result},{Math.Round(h.Income, 4)}" + Environment.NewLine
					);
			}
			File.AppendAllText(MercuryPath.Desktop.Down($"positionhistory.csv"), Environment.NewLine + Environment.NewLine);
		}
	}
}
