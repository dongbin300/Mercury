using Binance.Net.Enums;

using Mercury.Charts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Backtester
{
	/// <summary>
	/// LabWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class LabWindow : Window
	{
		public LabWindow()
		{
			InitializeComponent();
		}

		public class LabResult_EmaRecross
		{
			public int EmaPeriod { get; set; }
			public int Win { get; set; }
			public int Lose { get; set; }
			public decimal WinRate => (decimal)Win / (Win + Lose) * 100;
		}

		decimal GetMinPrice(List<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Min(x => x.Quote.Low);
		decimal GetMaxPrice(List<ChartInfo> charts, int period, int i) => charts.Skip(i - period).Take(period).Max(x => x.Quote.High);

		private void TestButton_Click(object sender, RoutedEventArgs e)
		{
			var results = new List<LabResult_EmaRecross>();

			var symbol = "ENSUSDT";
			var interval = KlineInterval.FiveMinutes;
			var startDate = new DateTime(2022, 1, 1);
			var endDate = new DateTime(2023, 12, 31);
			ChartLoader.InitCharts(symbol, interval, startDate, endDate);
			var chartPack = ChartLoader.GetChartPack(symbol, interval);

			for (int i = 5; i <= 120; i++)
			{
				chartPack.UseEma(i);
				var charts = chartPack.Charts.ToList();
				int flag = 0;
				int flag2 = 0;
				int win = 0;
				int lose = 0;
				decimal minPrice = 0m;
				decimal maxPrice = 0m;
				for (int j = 480; j < charts.Count; j++)
				{
					var c0 = charts[j];
					var c1 = charts[j - 1];

					switch (flag)
					{
						case 0:
							if (flag2 >= 40) // 20봉이상 ema 위나 아래에 존재하면
							{
								flag = 1;
							}

							if (c1.Quote.Close > (decimal)c1.Ema1 && c0.Quote.Close > (decimal)c0.Ema1
								|| c1.Quote.Close < (decimal)c1.Ema1 && c0.Quote.Close < (decimal)c0.Ema1)
							{
								flag2++;
							}
							else
							{
								flag2 = 0;
							}
							break;

						case 1:
							if (c1.Quote.Close > (decimal)c1.Ema1 && c0.Quote.Close < (decimal)c0.Ema1
								|| c1.Quote.Close < (decimal)c1.Ema1 && c0.Quote.Close > (decimal)c0.Ema1) // cross
							{
								flag = 2;
								flag2 = 0;
							}
							break;

						case 2:
							if (flag2 >= 10) // 10봉이상 ema recross 실패시 초기화
							{
								flag = 0;
							}
							else if (c1.Quote.Close < (decimal)c1.Ema1 && c0.Quote.Close > (decimal)c0.Ema1) // long recross
							{
								flag = 3;
								flag2 = 0;
								minPrice = GetMinPrice(charts, 30, j);
							}
							else if (c1.Quote.Close > (decimal)c1.Ema1 && c0.Quote.Close < (decimal)c0.Ema1) // short recross
							{
								flag = 4;
								flag2 = 0;
								maxPrice = GetMaxPrice(charts, 30, j);
							}
							else
							{
								flag2++;
							}
							break;

						case 3:
							if (flag2 >= 30) // 30봉이상 SL 버티면
							{
								win++;
								flag = 0;
								flag2 = 0;
							}
							else if (c0.Quote.Close < minPrice)
							{
								lose++;
								flag = 0;
								flag2 = 0;
							}
							else
							{
								flag2++;
							}
							break;

						case 4:
							if (flag2 >= 30) // 30봉이상 SL 버티면
							{
								win++;
								flag = 0;
								flag2 = 0;
							}
							else if (c0.Quote.Close > maxPrice)
							{
								lose++;
								flag = 0;
								flag2 = 0;
							}
							else
							{
								flag2++;
							}
							break;
					}
				}

				results.Add(new LabResult_EmaRecross()
				{
					EmaPeriod = i,
					Win = win,
					Lose = lose
				});
			}
		}
	}
}
