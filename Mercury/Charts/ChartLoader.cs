using Binance.Net.Enums;

using Mercury.Cryptos;
using Mercury.Extensions;

using System.Collections.Concurrent;

namespace Mercury.Charts
{
	public class ChartLoader
	{
		public static List<ChartPack> Charts { get; set; } = [];
		public static List<TradePack> Trades { get; set; } = [];
		public static List<PricePack> Prices { get; set; } = [];

		public static ChartPack GetChartPack(string symbol, KlineInterval interval) => Charts.Find(x => x.Symbol.Equals(symbol) && x.Interval.Equals(interval)) ?? default!;
		public static TradePack GetTradePack(string symbol) => Trades.Find(x => x.Symbol.Equals(symbol)) ?? default!;
		public static PricePack GetPricePack(string symbol) => Prices.Find(x => x.Symbol.Equals(symbol)) ?? default!;

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

		static bool IsFileWithinDateRangeAggTrades(string filePath, DateTime? startDate, DateTime? endDate)
		{
			string fileName = Path.GetFileNameWithoutExtension(filePath);
			string[] fileNameParts = fileName.Split('-');

			if (DateTime.TryParse(fileNameParts[2] + "-" + fileNameParts[3] + "-" + fileNameParts[4], out DateTime fileDate))
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
		public static ChartPack InitCharts(string symbol, KlineInterval interval, DateTime? startDate = null, DateTime? endDate = null)
		{
			try
			{
				var chartPack = new ChartPack(interval);

				var files = Directory.GetFiles(MercuryPath.BinanceFuturesData.Down(
					interval switch
					{
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
					})).Where(f => f.GetFileName().StartsWith(symbol));

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

				return chartPack;
			}
			catch
			{
				throw;
			}
		}

		/// <summary>
		/// 파일에서 통합된 거래 데이터를 불러옵니다.
		/// </summary>
		/// <param name="reportProgressCount"></param>
		/// <param name="symbol"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		/// <returns></returns>
		public static List<Price> GetAggregatedTrades(Action<int, int> reportProgressCount, string symbol, DateTime? startDate = null, DateTime? endDate = null)
		{
			try
			{
				var prices = new List<Price>();

				var files = Directory.GetFiles(MercuryPath.BinanceFuturesData.Down("trade", symbol)).Where(f => f.GetFileName().StartsWith(symbol));

				if (startDate == null && endDate == null)
				{
					for (var i = 0; i < files.Count(); i++)
					{
						reportProgressCount(i, files.Count());

						var file = files.ElementAt(i);
						var data = File.ReadAllLines(file);
						var prevPrice1 = 0m;
						var prevPrice2 = 0m;
						DateTime prevTime = default!;

						foreach (var line in data)
						{
							var e = line.Split(',');
							try
							{
								var _price = e[1].ToDecimal();
								var _time = e[5].ToLong().ToDateTime();
								if ((_time - prevTime).TotalMilliseconds > 10 && 
									_price != prevPrice1 && 
									_price != prevPrice2)
								{
									var price = new Price(_time, _price);
									prices.Add(price);

									prevPrice2 = prevPrice1;
									prevPrice1 = _price;
									prevTime = _time;
								}
							}
							catch
							{
							}
						}
					}
				}
				else
				{
					var rangeFiles = files.Where(f => IsFileWithinDateRangeAggTrades(f, startDate, endDate));

					for (var i = 0; i < rangeFiles.Count(); i++)
					{
						reportProgressCount(i, rangeFiles.Count());

						var file = rangeFiles.ElementAt(i);
						using var reader = new StreamReader(file);
						var prevPrice1 = 0m;
						var prevPrice2 = 0m;
						DateTime prevTime = default!;

						string? line;
						while ((line = reader.ReadLine()) != null)
						{
							var e = line.Split(',');
							try
							{
								var _time = e[5].ToLong().ToDateTime();
								if ((startDate == null || _time >= startDate) && (endDate == null || _time <= endDate))
								{
									var _price = e[1].ToDecimal();
									if ((_time - prevTime).TotalMilliseconds > 10 && 
										_price != prevPrice1 && 
										_price != prevPrice2)
									{
										var price = new Price(_time, _price);
										prices.Add(price);

										prevPrice2 = prevPrice1;
										prevPrice1 = _price;
										prevTime = _time;
									}
								}
							}
							catch
							{
							}
						}
					}
				}

				return prices;
			}
			catch
			{
				throw;
			}
		}

		/// <summary>
		/// 파일에서 가격 데이터를 불러옵니다.
		/// DateTime은 일괄적으로 파일 날짜 0시 0분 0초로 설정한다.
		/// </summary>
		/// <param name="reportProgressCount"></param>
		/// <param name="symbol"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		/// <returns></returns>
		public static List<Price> GetPrices(Action<int, int> reportProgressCount, string symbol, DateTime? startDate = null, DateTime? endDate = null)
		{
			try
			{
				var prices = new List<Price>();
				var files = Directory.GetFiles(MercuryPath.BinanceFuturesData.Down("price", symbol))
									 .Where(f => f.GetFileName().StartsWith(symbol));

				var filteredFiles = (startDate == null && endDate == null)
					? files
					: files.Where(f => IsFileWithinDateRangeAggTrades(f, startDate, endDate));

				foreach (var (file, index) in filteredFiles.Select((file, index) => (file, index)))
				{
					reportProgressCount(index, filteredFiles.Count());

					var date = CryptoSymbol.GetDatePriceCsvFileName(file); // CSV 파일의 날짜
					var data = File.ReadAllLines(file);

					foreach (var line in data)
					{
						if (decimal.TryParse(line, out var _price))
						{
							prices.Add(new Price(date, _price));
						}
					}
				}

				return prices;
			}
			catch
			{
				throw;
			}
		}

		/// <summary>
		/// 파일에서 가격 데이터를 병렬처리로 불러옵니다.
		/// DateTime은 일괄적으로 파일 날짜 0시 0분 0초로 설정한다.
		/// I/O 작업이라 병렬처리로 해도 오히려 더 느린 것 같다.
		/// 받은 데이터가 정렬도 안되어있으니 결론적으로 이게 더 느림.
		/// </summary>
		/// <param name="reportProgressCount"></param>
		/// <param name="symbol"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		/// <returns></returns>
		public static List<Price> GetPricesParallel(Action<int, int> reportProgressCount, string symbol, DateTime? startDate = null, DateTime? endDate = null)
		{
			try
			{
				var prices = new ConcurrentBag<Price>();
				var files = Directory.GetFiles(MercuryPath.BinanceFuturesData.Down("price", symbol))
									 .Where(f => f.GetFileName().StartsWith(symbol));

				var filteredFiles = (startDate == null && endDate == null)
					? files
					: files.Where(f => IsFileWithinDateRangeAggTrades(f, startDate, endDate));

				int progress = 0;

				Parallel.ForEach(filteredFiles, new ParallelOptions { MaxDegreeOfParallelism = 4 }, (file, state, index) =>
				{
					// 진행률(병렬이니 Interlocked 써서 충돌 방지)
					int cur = Interlocked.Increment(ref progress);
					reportProgressCount(cur - 1, filteredFiles.Count());

					var date = CryptoSymbol.GetDatePriceCsvFileName(file);
					var data = File.ReadAllLines(file);

					foreach (var line in data)
					{
						if (decimal.TryParse(line, out var _price))
						{
							prices.Add(new Price(date, _price));
						}
					}
				});

				return [.. prices];
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
						chart.Ema1 = f[1].ToDecimal();
						chart.K = f[2].ToDecimal();
						chart.D = f[3].ToDecimal();
						chart.Supertrend1 = f[4].ToDecimal();
						chart.Supertrend2 = f[5].ToDecimal();
						chart.Supertrend3 = f[6].ToDecimal();

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
						chart.Supertrend1 = f[1].ToDecimal();
						chart.Supertrend2 = f[2].ToDecimal();
						chart.Supertrend3 = f[3].ToDecimal();
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
						chart.Ema1 = e[1].ToDecimal();
						chart.K = e[2].ToDecimal();
						chart.D = e[3].ToDecimal();
						chart.Supertrend1 = e[4].ToDecimal();
						chart.Supertrend2 = e[5].ToDecimal();
						chart.Supertrend3 = e[6].ToDecimal();

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
