using Binance.Net.Enums;

namespace Mercury.Charts
{
	public class ChartLoader
	{
		public static List<ChartPack> Charts { get; set; } = new();
		public static ChartPack GetChartPack(string symbol, KlineInterval interval) => Charts.Find(x => x.Symbol.Equals(symbol) && x.Interval.Equals(interval)) ?? default!;

		static bool IsFileWithinDateRange(string filePath, DateTime? startDate, DateTime? endDate)
		{
			string fileName = Path.GetFileNameWithoutExtension(filePath);
			string[] fileNameParts = fileName.Split('_');

			if (fileNameParts.Length != 2)
			{
				return false;

			}

			if (DateTime.TryParse(fileNameParts[1], out DateTime fileDate))
			{
				if (startDate == null)
				{
					return fileDate <= endDate;
				}
				else if (endDate == null)
				{
					return fileDate >= startDate;
				}
				else
				{
					return fileDate >= startDate && fileDate <= endDate;
				}
			}

			return false;
		}

		/// <summary>
		/// 파일에서 차트 데이터를 불러옵니다.<br/>
		/// 파일에 기록된 DateTime은 UTC+0 기준입니다.<br/>
		/// <br/>
		/// 1분,3분,5분,15분,30분,1시간,2시간,4시간,6시간,8시간,12시간,1일 캔들 기준은 매일 00:00(UTC)<br/>
		/// 1주 캔들 기준은 월요일 00:00(UTC)<br/>
		/// 1달 캔들 기준은 매월 1일 00:00(UTC)
		/// </summary>
		/// <param name="symbol"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		public static void InitCharts(string symbol, KlineInterval interval, DateTime? startDate = null, DateTime? endDate = null)
		{
			try
			{
				var chartPack = new ChartPack(interval);

				var files = Directory.GetFiles(MercuryPath.BinanceFuturesData.Down(
					interval switch {
						KlineInterval.OneMinute => $"1m\\{symbol}",
						KlineInterval.ThreeMinutes => $"1m\\{symbol}",
						KlineInterval.FiveMinutes => "5m",
						KlineInterval.FifteenMinutes => "5m",
						KlineInterval.ThirtyMinutes => "5m",
						KlineInterval.OneHour => "1h",
						KlineInterval.TwoHour => "1h",
						KlineInterval.FourHour => "1h",
						KlineInterval.SixHour => "1h",
						KlineInterval.EightHour => "1h",
						KlineInterval.TwelveHour => "1h",
						KlineInterval.OneDay => "1D",
						KlineInterval.OneWeek => "1D",
						KlineInterval.OneMonth => "1D",
						_ => $"1m\\{symbol}"
					})).Where(f=>f.GetFileName().StartsWith(symbol));

				if (startDate == null && endDate == null)
				{
					foreach (var file in files)
					{
						var data = File.ReadAllLines(file);

						foreach (var line in data)
						{
							var e = line.Split(',');
							var quote = new Quote
							{
								Date = e[0].ToDateTime(),
								Open = e[1].ToDecimal(),
								High = e[2].ToDecimal(),
								Low = e[3].ToDecimal(),
								Close = e[4].ToDecimal(),
								Volume = e[5].ToDecimal()
							};
							chartPack.AddChart(new ChartInfo(symbol, quote));
						}
					}
				}
				else
				{
					// 1분, 3분봉이면 파일 이름에서 해당되는 기간을 찾아야함.
					var rangeFiles = interval == KlineInterval.OneMinute || interval == KlineInterval.ThreeMinutes ? files.Where(f => IsFileWithinDateRange(f, startDate, endDate)) : files;
					foreach (var file in rangeFiles)
					{
						using var reader = new StreamReader(file);
						string? line;
						while ((line = reader.ReadLine()) != null)
						{
							var e = line.Split(',');
							if (DateTime.TryParse(e[0], out DateTime date))
							{
								if ((startDate == null || date >= startDate) && (endDate == null || date <= endDate))
								{
									var quote = new Quote
									{
										Date = e[0].ToDateTime(),
										Open = e[1].ToDecimal(),
										High = e[2].ToDecimal(),
										Low = e[3].ToDecimal(),
										Close = e[4].ToDecimal(),
										Volume = e[5].ToDecimal()
									};
									chartPack.AddChart(new ChartInfo(symbol, quote));
								}
							}
						}
					}
				}

				chartPack.ConvertCandle();

				Charts.Add(chartPack);
			}
			catch
			{
				throw;
			}
		}

		/// <summary>
		/// 가격 데이터 초기화
		/// </summary>
		/// <param name="symbol"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		public static void InitChartsByDate(string symbol, KlineInterval interval, DateTime startDate, DateTime endDate)
		{
			try
			{
				var chartPack = new ChartPack(interval);

				switch (interval)
				{
					case KlineInterval.OneMinute:
					case KlineInterval.ThreeMinutes:
					case KlineInterval.FiveMinutes:
					case KlineInterval.FifteenMinutes:
					case KlineInterval.ThirtyMinutes:
					case KlineInterval.OneHour:
					case KlineInterval.TwoHour:
					case KlineInterval.FourHour:
					case KlineInterval.SixHour:
					case KlineInterval.EightHour:
					case KlineInterval.TwelveHour:
						var dayCount = (int)(endDate - startDate).TotalDays + 1;

						for (int i = 0; i < dayCount; i++)
						{
							var _currentDate = startDate.AddDays(i);
							var fileName = MercuryPath.BinanceFuturesData.Down("1m", symbol, $"{symbol}_{_currentDate:yyyy-MM-dd}.csv");
							var data = File.ReadAllLines(fileName);

							foreach (var d in data)
							{
								var e = d.Split(',');
								var quote = new Quote
								{
									Date = e[0].ToDateTime(),
									Open = e[1].ToDecimal(),
									High = e[2].ToDecimal(),
									Low = e[3].ToDecimal(),
									Close = e[4].ToDecimal(),
									Volume = e[5].ToDecimal()
								};
								chartPack.AddChart(new ChartInfo(symbol, quote));
							}
						}
						break;

					case KlineInterval.OneDay:
					case KlineInterval.ThreeDay:
					case KlineInterval.OneWeek:
					case KlineInterval.OneMonth:
						var path = MercuryPath.BinanceFuturesData.Down("1D", $"{symbol}.csv");
						var data1 = File.ReadAllLines(path);

						foreach (var d in data1)
						{
							var e = d.Split(',');
							var quote = new Quote
							{
								Date = e[0].ToDateTime(),
								Open = e[1].ToDecimal(),
								High = e[2].ToDecimal(),
								Low = e[3].ToDecimal(),
								Close = e[4].ToDecimal(),
								Volume = e[5].ToDecimal()
							};
							chartPack.AddChart(new ChartInfo(symbol, quote));
						}
						break;

					default:
						break;
				}

				chartPack.ConvertCandle();

				Charts.Add(chartPack);
			}
			catch (FileNotFoundException)
			{
				throw;
			}
		}

		/// <summary>
		/// 가격 데이터와 지표 데이터를 동시에 초기화
		/// </summary>
		/// <param name="symbol"></param>
		/// <param name="interval"></param>
		/// <param name="startTime"></param>
		/// <param name="endTime"></param>
		public static void InitChartsAndTs1IndicatorsByDate(string symbol, KlineInterval interval, DateTime startDate, DateTime endDate)
		{
			try
			{
				var chartPack = new ChartPack(interval);
				var chartFileName = MercuryPath.BinanceFuturesData.Down(interval.ToIntervalString(), $"{symbol}.csv");
				var indicatorFileName = MercuryPath.BinanceFuturesData.Down("idc", "ts1", interval.ToIntervalString(), $"{symbol}.csv");
				var chartData = File.ReadAllLines(chartFileName);
				var indicatorData = File.ReadAllLines(indicatorFileName);

				for (int i = 0; i < chartData.Length; i++)
				{
					var e = chartData[i].Split(',');
					var time = e[0].ToDateTime();
					if (time >= startDate && time <= endDate)
					{
						var quote = new Quote
						{
							Date = time,
							Open = e[1].ToDecimal(),
							High = e[2].ToDecimal(),
							Low = e[3].ToDecimal(),
							Close = e[4].ToDecimal(),
							Volume = e[5].ToDecimal()
						};

						var chart = new ChartInfo(symbol, quote);
						var f = indicatorData[i].Split(',');
						chart.Ema1 = f[1].ToDouble();
						chart.K = f[2].ToDouble();
						chart.D = f[3].ToDouble();
						chart.Supertrend1 = f[4].ToDouble();
						chart.Supertrend2 = f[5].ToDouble();
						chart.Supertrend3 = f[6].ToDouble();

						chartPack.AddChart(chart);
					}

					if (time == endDate)
					{
						break;
					}
				}
				Charts.Add(chartPack);
			}
			catch (FileNotFoundException)
			{
				throw;
			}
		}

		/// <summary>
		/// 가격 데이터와 지표 데이터를 동시에 초기화
		/// </summary>
		/// <param name="symbol"></param>
		/// <param name="interval"></param>
		/// <param name="startTime"></param>
		/// <param name="endTime"></param>
		public static void InitChartsAndTs2IndicatorsByDate(string symbol, KlineInterval interval, DateTime startDate, DateTime endDate)
		{
			try
			{
				var chartPack = new ChartPack(interval);
				var chartFileName = MercuryPath.BinanceFuturesData.Down(interval.ToIntervalString(), $"{symbol}.csv");
				var indicatorFileName = MercuryPath.BinanceFuturesData.Down("idc", "ts2", interval.ToIntervalString(), $"{symbol}.csv");
				var chartData = File.ReadAllLines(chartFileName);
				var indicatorData = File.ReadAllLines(indicatorFileName);

				for (int i = 0; i < chartData.Length; i++)
				{
					var e = chartData[i].Split(',');
					var time = e[0].ToDateTime();
					if (time >= startDate && time <= endDate)
					{
						var quote = new Quote
						{
							Date = time,
							Open = e[1].ToDecimal(),
							High = e[2].ToDecimal(),
							Low = e[3].ToDecimal(),
							Close = e[4].ToDecimal(),
							Volume = e[5].ToDecimal()
						};

						var chart = new ChartInfo(symbol, quote);
						var f = indicatorData[i].Split(',');
						chart.Supertrend1 = f[1].ToDouble();
						chart.Supertrend2 = f[2].ToDouble();
						chart.Supertrend3 = f[3].ToDouble();

						chartPack.AddChart(chart);
					}

					if (time == endDate)
					{
						break;
					}
				}
				Charts.Add(chartPack);
			}
			catch (FileNotFoundException)
			{
				throw;
			}
		}

		/// <summary>
		/// 분봉 초기화
		/// </summary>
		/// <param name="symbol"></param>
		/// <param name="interval"></param>
		public static void InitChartsMByDate(string symbol, KlineInterval interval)
		{
			try
			{
				var chartPack = new ChartPack(interval);
				var fileName = MercuryPath.BinanceFuturesData.Down(interval.ToIntervalString(), $"{symbol}.csv");
				var data = File.ReadAllLines(fileName);

				foreach (var d in data)
				{
					var e = d.Split(',');
					var time = e[0].ToDateTime();
					var quote = new Quote
					{
						Date = time,
						Open = e[1].ToDecimal(),
						High = e[2].ToDecimal(),
						Low = e[3].ToDecimal(),
						Close = e[4].ToDecimal(),
						Volume = e[5].ToDecimal()
					};
					chartPack.AddChart(new ChartInfo(symbol, quote));
				}

				Charts.Add(chartPack);
			}
			catch (FileNotFoundException)
			{
				throw;
			}
		}

		/// <summary>
		/// 분봉 초기화
		/// </summary>
		/// <param name="symbol"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		public static void InitChartsMByDate(string symbol, KlineInterval interval, DateTime startDate, DateTime endDate)
		{
			try
			{
				var chartPack = new ChartPack(interval);
				var fileName = MercuryPath.BinanceFuturesData.Down(interval.ToIntervalString(), $"{symbol}.csv");
				var data = File.ReadAllLines(fileName);

				foreach (var d in data)
				{
					var e = d.Split(',');
					var time = e[0].ToDateTime();
					if (time >= startDate && time <= endDate)
					{
						var quote = new Quote
						{
							Date = time,
							Open = e[1].ToDecimal(),
							High = e[2].ToDecimal(),
							Low = e[3].ToDecimal(),
							Close = e[4].ToDecimal(),
							Volume = e[5].ToDecimal()
						};
						chartPack.AddChart(new ChartInfo(symbol, quote));
					}
				}

				Charts.Add(chartPack);
			}
			catch (FileNotFoundException)
			{
				throw;
			}
		}

		/// <summary>
		/// 지표값 불러오기
		/// 가격 데이터 먼저 불러오고 난 뒤 가능
		/// </summary>
		/// <param name="indicatorId"></param>
		/// <param name="symbol"></param>
		/// <param name="interval"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		public static void LoadIndicatorTs1ByDate(string symbol, KlineInterval interval, DateTime startDate, DateTime endDate)
		{
			try
			{
				var chartPack = GetChartPack(symbol, interval);
				var fileName = MercuryPath.BinanceFuturesData.Down("idc", "ts1", interval.ToIntervalString(), $"{symbol}.csv");
				var data = File.ReadAllLines(fileName);

				var isStart = false;
				foreach (var d in data)
				{
					if (isStart)
					{
						var e = d.Split(',');
						var time = e[0].ToDateTime();
						var chart = chartPack.GetChart(time);
						chart.Ema1 = e[1].ToDouble();
						chart.K = e[2].ToDouble();
						chart.D = e[3].ToDouble();
						chart.Supertrend1 = e[4].ToDouble();
						chart.Supertrend2 = e[5].ToDouble();
						chart.Supertrend3 = e[6].ToDouble();

						if (d.StartsWith($"{endDate:yyyy-MM-dd HH:mm:ss}"))
						{
							break;
						}
					}
					else if (d.StartsWith($"{startDate:yyyy-MM-dd HH:mm:ss}"))
					{
						isStart = true;
					}
					else
					{
						continue;
					}
				}
			}
			catch (FileNotFoundException)
			{
				throw;
			}
		}

		public static void SelectCharts()
		{
			foreach (var chartPack in Charts)
			{
				chartPack.Select();
			}
		}

		public static Dictionary<string, ChartInfo> NextCharts()
		{
			try
			{
				var result = new Dictionary<string, ChartInfo>();

				foreach (var chartPack in Charts)
				{
					result.Add(chartPack.Symbol, chartPack.Next());
				}

				return result;
			}
			catch (FileNotFoundException)
			{
				throw;
			}
		}
	}
}
