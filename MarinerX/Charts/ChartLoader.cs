using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot;

using MarinaX.Utils;

using MarinerX.Utils;

using Mercury;
using Mercury.Apis;
using Mercury.Charts;
using Mercury.Extensions;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

namespace MarinerX.Charts
{
	internal class ChartLoader
	{
		public static List<ChartPack> Charts { get; set; } = [];
		public static List<TradePack> Trades { get; set; } = [];
		public static List<PricePack> Prices { get; set; } = [];


		/// <summary>
		/// 분봉 초기화
		/// </summary>
		/// <param name="symbol"></param>
		/// <param name="interval"></param>
		/// <param name="worker"></param>
		public static void InitCharts(string symbol, KlineInterval interval, Worker worker)
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
						var files = new DirectoryInfo(MercuryPath.BinanceFuturesData.Down("1m", symbol)).GetFiles("*.csv");

						worker.For(0, files.Length, 1, (i) =>
						{
							var fileName = files[i].FullName;
							var date = SymbolUtil.GetDate(fileName);
							var data = File.ReadAllLines(fileName);

							foreach (var d in data)
							{
								var e = d.Split(',');
								var quote = new Quote
								{
									Date = DateTime.Parse(e[0]),
									Open = decimal.Parse(e[1]),
									High = decimal.Parse(e[2]),
									Low = decimal.Parse(e[3]),
									Close = decimal.Parse(e[4]),
									Volume = decimal.Parse(e[5])
								};
								chartPack.AddChart(new ChartInfo(symbol, quote));
							}
						}, ProgressBarDisplayOptions.Count | ProgressBarDisplayOptions.Percent | ProgressBarDisplayOptions.TimeRemaining);
						break;

					case KlineInterval.OneDay:
					case KlineInterval.ThreeDay:
					case KlineInterval.OneWeek:
					case KlineInterval.OneMonth:
						var path = MercuryPath.BinanceFuturesData.Down("1D", $"{symbol}.csv");
						var data = File.ReadAllLines(path);

						foreach (var d in data)
						{
							var e = d.Split(',');
							var quote = new Quote
							{
								Date = DateTime.Parse(e[0]),
								Open = decimal.Parse(e[1]),
								High = decimal.Parse(e[2]),
								Low = decimal.Parse(e[3]),
								Close = decimal.Parse(e[4]),
								Volume = decimal.Parse(e[5])
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
		/// 분봉 초기화
		/// </summary>
		/// <param name="symbol"></param>
		/// <param name="interval"></param>
		/// <param name="worker"></param>
		/// <param name="startDate"></param>
		/// <param name="endDate"></param>
		public static void InitChartsByDate(string symbol, KlineInterval interval, Worker worker, DateTime startDate, DateTime endDate)
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

						worker.For(0, dayCount, 1, (i) =>
						{
							var _currentDate = startDate.AddDays(i);
							var fileName = MercuryPath.BinanceFuturesData.Down("1m", symbol, $"{symbol}_{_currentDate:yyyy-MM-dd}.csv");
							var data = File.ReadAllLines(fileName);

							foreach (var d in data)
							{
								var e = d.Split(',');
								var quote = new Quote
								{
									Date = DateTime.Parse(e[0]),
									Open = decimal.Parse(e[1]),
									High = decimal.Parse(e[2]),
									Low = decimal.Parse(e[3]),
									Close = decimal.Parse(e[4]),
									Volume = decimal.Parse(e[5])
								};
								chartPack.AddChart(new ChartInfo(symbol, quote));
							}
						}, ProgressBarDisplayOptions.Count | ProgressBarDisplayOptions.Percent | ProgressBarDisplayOptions.TimeRemaining);
						break;

					case KlineInterval.OneDay:
					case KlineInterval.ThreeDay:
					case KlineInterval.OneWeek:
					case KlineInterval.OneMonth:
						var path = MercuryPath.BinanceFuturesData.Down("1D", $"{symbol}.csv");
						var data = File.ReadAllLines(path);

						foreach (var d in data)
						{
							var e = d.Split(',');
							var quote = new Quote
							{
								Date = DateTime.Parse(e[0]),
								Open = decimal.Parse(e[1]),
								High = decimal.Parse(e[2]),
								Low = decimal.Parse(e[3]),
								Close = decimal.Parse(e[4]),
								Volume = decimal.Parse(e[5])
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

		public static void ExtractPricesFromAggregatedTrades(string symbol, Worker worker, DateTime? startDate = null, DateTime? endDate = null)
		{
			try
			{
				var files = Directory.GetFiles(MercuryPath.BinanceFuturesData.Down("trade", symbol)).Where(f => f.GetFileName().StartsWith(symbol)).ToList();

				if (startDate == null && endDate == null)
				{
					foreach (var file in files)
					{
						var f = file.Split('-');
						var currentDateString = $"{f[2]}-{f[3]}-{f[4]}";
						var data = File.ReadAllLines(file);

						var prevPrice = 0m;
						var builder = new StringBuilder();

						worker.For(0, data.Length, 1, (i) =>
						{
							try
							{
								var e = data[i].Split(',');
								var price = decimal.Parse(e[1]);
								var time = long.Parse(e[5]).TimeStampMillisecondsToDateTime();

								if (prevPrice == price)
								{
									return;
								}

								prevPrice = price;
								builder.AppendLine($"{time:yyyy-MM-dd HH:mm:ss.fff},{price}");
							}
							catch
							{
							}
						}, ProgressBarDisplayOptions.Count | ProgressBarDisplayOptions.Percent | ProgressBarDisplayOptions.TimeRemaining);

						File.WriteAllText(MercuryPath.BinanceFuturesData.Down("price", symbol, $"{symbol}-prices-{currentDateString}"), builder.ToString());
					}
				}
				else
				{
					var rangeFiles = files.Where(f => IsFileWithinDateRangeAggTrades(f, startDate, endDate)).ToList();

					foreach (var file in rangeFiles)
					{
						var f = file.Split('-');
						var currentDateString = $"{f[2]}-{f[3]}-{f[4]}";
						var data = File.ReadAllLines(file);

						var raw = new (DateTime time, decimal price)[data.Length];

						worker.For(0, data.Length, 1, (i) =>
						{
							var line = data[i];
							if (line.StartsWith("agg"))
							{
								return;
							}

							var e = data[i].Split(',');
							var price = decimal.Parse(e[1]);
							var time = long.Parse(e[5]).TimeStampMillisecondsToDateTime();

							raw[i] = (time, price);
						}, ProgressBarDisplayOptions.Count | ProgressBarDisplayOptions.Percent | ProgressBarDisplayOptions.TimeRemaining);

						var filtered = raw.Where(x => x.time != default).OrderBy(x => x.time).ToList();

						var timePriceMap = new Dictionary<DateTime, decimal>();
						foreach (var (time, price) in filtered)
						{
							timePriceMap[time] = price;
						}

						var builder = new StringBuilder();
						decimal prevPrice = 0;
						foreach (var (time, price) in timePriceMap.OrderBy(kv => kv.Key))
						{
							if (prevPrice != price)
							{
								builder.AppendLine(price.ToString());
								prevPrice = price;
							}
						}

						File.WriteAllText(MercuryPath.BinanceFuturesData.Down("price", symbol, $"{symbol}-prices-{currentDateString}"), builder.ToString());
					}
				}
			}
			catch
			{
				throw;
			}
		}

		public static void InitTrades(string symbol, Worker worker, DateTime? startDate = null, DateTime? endDate = null)
		{
			try
			{
				var tradePack = new TradePack(symbol);

				var files = Directory.GetFiles(MercuryPath.BinanceFuturesData.Down("trade", symbol)).Where(f => f.GetFileName().StartsWith(symbol)).ToList();

				if (startDate == null && endDate == null)
				{
					worker.For(0, files.Count, 1, (i) =>
					{
						var data = File.ReadAllLines(files[i]);

						foreach (var d in data)
						{
							if (d.StartsWith("agg"))
							{
								continue;
							}
							var e = d.Split(',');
							var trade = new BinanceAggregatedTrade
							{
								Id = long.Parse(e[0]),
								Price = decimal.Parse(e[1]),
								Quantity = decimal.Parse(e[2]),
								FirstTradeId = long.Parse(e[3]),
								LastTradeId = long.Parse(e[4]),
								TradeTime = long.Parse(e[5]).TimeStampMillisecondsToDateTime(),
								BuyerIsMaker = bool.Parse(e[6])
							};
							tradePack.AddTrade(trade);
						}
					}, ProgressBarDisplayOptions.Count | ProgressBarDisplayOptions.Percent | ProgressBarDisplayOptions.TimeRemaining);

					Trades.Add(tradePack);
				}
				else
				{
					var rangeFiles = files.Where(f => IsFileWithinDateRangeAggTrades(f, startDate, endDate)).ToList();

					worker.For(0, rangeFiles.Count, 1, (i) =>
					{
						var data = File.ReadAllLines(rangeFiles[i]);

						foreach (var d in data)
						{
							if (d.StartsWith("agg"))
							{
								continue;
							}
							var e = d.Split(',');
							var trade = new BinanceAggregatedTrade
							{
								Id = long.Parse(e[0]),
								Price = decimal.Parse(e[1]),
								Quantity = decimal.Parse(e[2]),
								FirstTradeId = long.Parse(e[3]),
								LastTradeId = long.Parse(e[4]),
								TradeTime = long.Parse(e[5]).TimeStampMillisecondsToDateTime(),
								BuyerIsMaker = bool.Parse(e[6])
							};
							tradePack.AddTrade(trade);
						}
					}, ProgressBarDisplayOptions.Count | ProgressBarDisplayOptions.Percent | ProgressBarDisplayOptions.TimeRemaining);

					Trades.Add(tradePack);
				}
			}
			catch (FileNotFoundException)
			{
				throw;
			}
		}

		public static void ConvertToPrices(Worker worker)
		{
			try
			{
				if (Trades.Count <= 0)
				{
					return;
				}

				foreach (var tradePack in Trades)
				{
					var currentDate = DateTime.Parse($"{tradePack.Trades[0].TradeTime:yyyy-MM-dd}");
					var data = string.Empty;
					var prevPrice = 0m;

					worker.For(0, tradePack.Trades.Count, 1, (i) =>
					{
						var time = tradePack.Trades[i].TradeTime;
						var price = tradePack.Trades[i].Price;
						if (!(time.Year == currentDate.Year && time.Month == currentDate.Month && time.Day == currentDate.Day))
						{
							File.WriteAllText(MercuryPath.BinanceFuturesData.Down("price", tradePack.Symbol, $"{tradePack.Symbol}-prices-{currentDate:yyyy-MM-dd}.csv"), data);

							currentDate = currentDate.AddDays(1);
							data = string.Empty;
						}

						if (prevPrice == price)
						{
							return;
						}

						prevPrice = price;
						data += $"{time:yyyy-MM-dd HH:mm:ss.fff},{price}" + Environment.NewLine;
					}, ProgressBarDisplayOptions.Count | ProgressBarDisplayOptions.Percent | ProgressBarDisplayOptions.TimeRemaining);

					File.WriteAllText(MercuryPath.BinanceFuturesData.Down("price", tradePack.Symbol, $"{tradePack.Symbol}-prices-{currentDate:yyyy-MM-dd}.csv"), data);
				}
			}
			catch
			{
				throw;
			}
		}

		public static void InitPrices(string symbol, Worker worker)
		{
			try
			{
				var pricePack = new PricePack(symbol);

				var files = new DirectoryInfo(MercuryPath.BinanceFuturesData.Down("price", symbol)).GetFiles("*.csv");

				worker.For(0, files.Length, 1, (i) =>
				{
					var fileName = files[i].FullName;
					var data = File.ReadAllLines(fileName);

					var prices = data.Select(d => decimal.Parse(d)).ToList();
					pricePack.Prices.Add(SymbolUtil.GetDate(fileName), prices);
				}, ProgressBarDisplayOptions.Count | ProgressBarDisplayOptions.Percent | ProgressBarDisplayOptions.TimeRemaining);

				Prices.Add(pricePack);
			}
			catch (FileNotFoundException)
			{
				throw;
			}
		}

		/// <summary>
		/// 1분봉 데이터를 이용해 1일봉 데이터를 파일로 생성
		/// </summary>
		/// <param name="interval"></param>
		/// <param name="worker"></param>
		public static void Extract1DCandle(Worker worker)
		{
			try
			{
				var startTimeTemp = File.Exists(MercuryPath.BinanceFuturesData.Down("1D", "BTCUSDT.csv")) ? SymbolUtil.GetEndDateOf1D("BTCUSDT") : SymbolUtil.GetStartDate("BTCUSDT");
				//var startTimeTemp = new DateTime(2019, 9, 8);
				var symbols = LocalApi.SymbolNames;
				var dayCountTemp = (DateTime.Today - startTimeTemp).Days + 1;
				var csvFileCount = symbols.Count * dayCountTemp;
				worker.SetProgressBar(0, csvFileCount);

				int s = 0;
				foreach (var symbol in symbols)
				{
					var startTime = File.Exists(MercuryPath.BinanceFuturesData.Down("1D", $"{symbol}.csv")) ? SymbolUtil.GetEndDateOf1D(symbol) : SymbolUtil.GetStartDate(symbol);
					var dayCount = (DateTime.Today - startTime).Days + 1;
					var chartPack = new ChartPack(KlineInterval.OneDay);
					var path = MercuryPath.BinanceFuturesData.Down("1D", $"{symbol}.csv");

					for (int i = 0; i < dayCount; i++)
					{
						var date = startTime.AddDays(i);
						var inputFileName = MercuryPath.BinanceFuturesData.Down("1m", symbol, $"{symbol}_{date:yyyy-MM-dd}.csv");

						if (!File.Exists(inputFileName))
						{
							continue;
						}

						var data = File.ReadAllLines(inputFileName);

						worker.Progress(++s);
						worker.ProgressText($"{symbol}, {i} / {dayCount}");

						foreach (var d in data)
						{
							var e = d.Split(',');
							var quote = new Quote
							{
								Date = DateTime.Parse(e[0]),
								Open = decimal.Parse(e[1]),
								High = decimal.Parse(e[2]),
								Low = decimal.Parse(e[3]),
								Close = decimal.Parse(e[4]),
								Volume = decimal.Parse(e[5])
							};
							chartPack.AddChart(new ChartInfo(symbol, quote));
						}
					}

					chartPack.ConvertCandle();

					var newData = chartPack.Charts
						.Select(x => x.Quote)
						.Select(x => string.Join(',', [
							x.Date.ToString("yyyy-MM-dd HH:mm:ss"), x.Open.ToString(), x.High.ToString(), x.Low.ToString(), x.Close.ToString(), x.Volume.ToString()
						]))
						.ToList();

					path.TryCreate();
					var prevData = File.ReadAllLines(path);
					if (prevData.Length < 1)
					{
						File.WriteAllLines(path, newData);
					}
					else
					{
						var currentData = prevData.Take(prevData.Length - 1).ToList();
						currentData.AddRange(newData);
						File.WriteAllLines(path, currentData);
					}
				}
			}
			catch (FileNotFoundException)
			{
				throw;
			}
		}

		/// <summary>
		/// 1분봉 데이터를 이용해 n분봉 데이터를 파일로 생성
		/// </summary>
		/// <param name="worker"></param>
		public static void ExtractCandle(Worker worker, KlineInterval interval)
		{
			try
			{
				var manualStartTime = DateTime.Parse("2019-09-08");
				var symbols = LocalApi.SymbolNames;
				var dayCountTemp = (DateTime.Today - manualStartTime).Days + 1;
				var csvFileCount = symbols.Count * dayCountTemp;
				worker.SetProgressBar(0, csvFileCount);

				int s = 0;
				for (int j = 0; j < symbols.Count; j++)
				{
					var symbol = symbols[j];
					var startTime = manualStartTime;
					var fileName = MercuryPath.BinanceFuturesData.Down(interval.ToIntervalString(), $"{symbol}.csv");

					// 파일이 존재하면 가장 마지막 줄의 시간을 가져옴
					if (File.Exists(fileName))
					{
						startTime = SymbolUtil.GetEndDateTime(symbol, interval);
					}

					var dayCount = (DateTime.Today - startTime).Days + 1;
					var chartPack = new ChartPack(interval);

					for (int i = 0; i < dayCount; i++)
					{
						try
						{
							worker.Progress(++s);
							worker.ProgressText($"{symbol}, {i} / {dayCount}");

							var date = startTime.AddDays(i);
							var inputFileName = MercuryPath.BinanceFuturesData.Down("1m", symbol, $"{symbol}_{date:yyyy-MM-dd}.csv");
							var data = File.ReadAllLines(inputFileName);

							foreach (var d in data)
							{
								var e = d.Split(',');
								var time = e[0].ToDateTime();
								if (time <= startTime)
								{
									continue;
								}

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
						catch (FileNotFoundException)
						{
						}
					}

					chartPack.ConvertCandle();

					var newData = chartPack.Charts
						.Select(x => x.Quote)
						.Select(x => string.Join(',', [
							x.Date.ToString("yyyy-MM-dd HH:mm:ss"), x.Open.ToString(), x.High.ToString(), x.Low.ToString(), x.Close.ToString(), x.Volume.ToString()
						]))
						.ToList()
						.SkipLast(1); // 마지막 데이터는 완성되지 않은 데이터이므로 스킵한다.

					File.AppendAllLines(fileName, newData);

					s = (j + 1) * dayCountTemp;
					worker.Progress(s);
				}
			}
			catch (FileNotFoundException)
			{
				throw;
			}
		}

		/// <summary>
		/// 바이낸스 1분봉 데이터 수집
		/// </summary>
		/// <param name="worker"></param>
		public static void GetCandleDataFromBinance(Worker worker)
		{
			try
			{
				var startTime = SymbolUtil.GetEndDate("ZRXUSDT");
				var EndTime = DateTime.Today;
				var symbols = LocalApi.SymbolNames;
				var csvFileCount = ((EndTime - startTime).Days + 1) * symbols.Count;
				worker.SetProgressBar(0, csvFileCount);

				int p = 0;
				for (int i = 0; i < 400; i++)
				{
					var standardTime = startTime.AddDays(i);

					if (DateTime.Compare(standardTime, EndTime) > 0)
					{
						break;
					}

					foreach (var symbol in symbols)
					{
						var symbolPath = MercuryPath.BinanceFuturesData.Down("1m", symbol);

						if (!Directory.Exists(symbolPath))
						{
							Directory.CreateDirectory(symbolPath);
						}

						worker.Progress(++p);
						worker.ProgressText($"{symbol}, {standardTime:yyyy-MM-dd}");

						BinanceRestApi.GetCandleDataForOneDay(symbol, standardTime);

						Thread.Sleep(500);
					}
				}

				//foreach (var symbol in symbols)
				//{
				//	var startTime = getStartTime;
				//	var count = 400;
				//	var symbolPath = PathUtil.BinanceFuturesData.Down("1m", symbol);

				//	if (!Directory.Exists(symbolPath))
				//	{
				//		Directory.CreateDirectory(symbolPath);
				//	}

				//	for (int i = 0; i < count; i++)
				//	{
				//		var standardTime = startTime.AddDays(i);

				//		if (DateTime.Compare(standardTime, EndTime) > 0)
				//		{
				//			break;
				//		}

				//		worker.Progress(++p);
				//		worker.ProgressText($"{symbol}, {standardTime:yyyy-MM-dd}");

				//		BinanceRestApi.GetCandleDataForOneDay(symbol, standardTime);

				//		Thread.Sleep(500);
				//	}
				//}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		public static void GetCandleDataFromBinanceManual(Worker worker)
		{
			try
			{
				var getStartTime = new DateTime(2024, 1, 4);
				//var symbols = LocalApi.SymbolNames;
				var symbols = new List<string> { "ETHUSDC" };
				var maxCount = 2000;
				var csvFileCount = maxCount * symbols.Count;
				worker.SetProgressBar(0, csvFileCount);

				int p = 0;
				foreach (var symbol in symbols)
				{
					var startTime = getStartTime;
					var symbolPath = MercuryPath.BinanceFuturesData.Down("1m", symbol);

					if (!Directory.Exists(symbolPath))
					{
						Directory.CreateDirectory(symbolPath);
					}

					for (int i = 0; i < maxCount; i++)
					{
						var standardTime = startTime.AddDays(i);

						if (DateTime.Compare(standardTime, DateTime.Today) > 0)
						{
							break;
						}

						worker.Progress(++p);
						worker.ProgressText($"{symbol}, {standardTime:yyyy-MM-dd}");

						BinanceRestApi.GetCandleDataForOneDay(symbol, standardTime);

						Thread.Sleep(500);
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		/// <summary>
		/// 데이터 수집에 실패한 파일 탐색
		/// </summary>
		/// <returns></returns>
		public static List<string> GetInvalidDataFileNames()
		{
			var result = new List<string>();

			try
			{
				var symbols = LocalApi.SymbolNames;
				foreach (var symbol in symbols)
				{
					var symbolPath = MercuryPath.BinanceFuturesData.Down("1m", symbol);
					var files = new DirectoryInfo(symbolPath).GetFiles("*.csv");
					var exceptedFiles = files.Except(new List<FileInfo> { files.OrderBy(f => f.Name).First(), files.OrderByDescending(f => f.Name).First() }); // Except start date & end date
					var fileSizeAverage = exceptedFiles.Average(f => f.Length);
					var invalidFileNames = exceptedFiles.Where(x => Math.Abs(x.Length - fileSizeAverage) > fileSizeAverage * 0.2).Select(x => x.Name).ToList();

					result.AddRange(invalidFileNames);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}

			return result;
		}

		public static List<(string, KlineInterval)> GetLoadedSymbols => Charts.Select(x => (x.Symbol, x.Interval)).ToList();
		public static ChartPack GetChartPack(string symbol, KlineInterval interval) => Charts.Find(x => x.Symbol.Equals(symbol) && x.Interval.Equals(interval)) ?? default!;
		public static TradePack GetTradePack(string symbol) => Trades.Find(x => x.Symbol.Equals(symbol)) ?? default!;
		public static PricePack GetPricePack(string symbol) => Prices.Find(x => x.Symbol.Equals(symbol)) ?? default!;
	}
}
