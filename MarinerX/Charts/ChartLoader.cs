using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot;

using Mercury;

using MarinaX.Utils;

using MarinerX.Apis;
using MarinerX.Utils;

using MercuryTradingModel.Charts;

using System;
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
        public static List<ChartPack> Charts { get; set; } = new();
        public static List<TradePack> Trades { get; set; } = new();
        public static List<PricePack> Prices { get; set; } = new();


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
                        var files = new DirectoryInfo(PathUtil.BinanceFuturesData.Down("1m", symbol)).GetFiles("*.csv");

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
                                chartPack.AddChart(new MercuryChartInfo(symbol, quote));
                            }
                        }, ProgressBarDisplayOptions.Count | ProgressBarDisplayOptions.Percent | ProgressBarDisplayOptions.TimeRemaining);
                        break;

                    case KlineInterval.OneDay:
                    case KlineInterval.ThreeDay:
                    case KlineInterval.OneWeek:
                    case KlineInterval.OneMonth:
                        var path = PathUtil.BinanceFuturesData.Down("1D", $"{symbol}.csv");
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
                            chartPack.AddChart(new MercuryChartInfo(symbol, quote));
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
                            var fileName = PathUtil.BinanceFuturesData.Down("1m", symbol, $"{symbol}_{_currentDate:yyyy-MM-dd}.csv");
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
                                chartPack.AddChart(new MercuryChartInfo(symbol, quote));
                            }
                        }, ProgressBarDisplayOptions.Count | ProgressBarDisplayOptions.Percent | ProgressBarDisplayOptions.TimeRemaining);
                        break;

                    case KlineInterval.OneDay:
                    case KlineInterval.ThreeDay:
                    case KlineInterval.OneWeek:
                    case KlineInterval.OneMonth:
                        var path = PathUtil.BinanceFuturesData.Down("1D", $"{symbol}.csv");
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
                            chartPack.AddChart(new MercuryChartInfo(symbol, quote));
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

        public static void InitTrades(string symbol, Worker worker)
        {
            try
            {
                var tradePack = new TradePack(symbol);

                var files = new DirectoryInfo(PathUtil.BinanceFuturesData.Down("trade", symbol)).GetFiles("*.csv");

                worker.For(0, files.Length, 1, (i) =>
                {
                    var fileName = files[i].FullName;
                    var data = File.ReadAllLines(fileName);

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
            catch (FileNotFoundException)
            {
                throw;
            }
        }

        public static void InitPrices(string symbol, Worker worker)
        {
            try
            {
                var pricePack = new PricePack(symbol);

                var files = new DirectoryInfo(PathUtil.BinanceFuturesData.Down("price", symbol)).GetFiles("*.csv");

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
                var startTimeTemp = File.Exists(PathUtil.BinanceFuturesData.Down("1D", "BTCUSDT.csv")) ? SymbolUtil.GetEndDateOf1D("BTCUSDT") : SymbolUtil.GetStartDate("BTCUSDT");
                //var startTimeTemp = new DateTime(2019, 9, 8);
                var symbols = LocalStorageApi.SymbolNames;
                var dayCountTemp = (DateTime.Today - startTimeTemp).Days + 1;
                var csvFileCount = symbols.Count * dayCountTemp;
                worker.SetProgressBar(0, csvFileCount);

                int s = 0;
                foreach (var symbol in symbols)
                {
                    var startTime = File.Exists(PathUtil.BinanceFuturesData.Down("1D", $"{symbol}.csv")) ? SymbolUtil.GetEndDateOf1D(symbol) : SymbolUtil.GetStartDate(symbol);
                    var dayCount = (DateTime.Today - startTime).Days + 1;
                    var chartPack = new ChartPack(KlineInterval.OneDay);
                    var path = PathUtil.BinanceFuturesData.Down("1D", $"{symbol}.csv");

                    try
                    {
                        for (int i = 0; i < dayCount; i++)
                        {
                            var date = startTime.AddDays(i);
                            var inputFileName = PathUtil.BinanceFuturesData.Down("1m", symbol, $"{symbol}_{date:yyyy-MM-dd}.csv");
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
                                chartPack.AddChart(new MercuryChartInfo(symbol, quote));
                            }
                        }
                    }
                    catch (FileNotFoundException)
                    {
                    }

                    chartPack.ConvertCandle();

                    var newData = chartPack.Charts
                        .Select(x => x.Quote)
                        .Select(x => string.Join(',', new string[] {
                            x.Date.ToString("yyyy-MM-dd HH:mm:ss"), x.Open.ToString(), x.High.ToString(), x.Low.ToString(), x.Close.ToString(), x.Volume.ToString()
                        }))
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
        /// 1분봉 데이터를 이용해 5분봉 데이터를 파일로 생성
        /// </summary>
        /// <param name="worker"></param>
        public static void ExtractCandle(Worker worker, KlineInterval interval, string intervalString)
        {
            try
            {
                var manualStartTime = DateTime.Parse("2019-09-08");
                var symbols = LocalStorageApi.SymbolNames;
                var dayCountTemp = (DateTime.Today - manualStartTime).Days + 1;
                var csvFileCount = symbols.Count * dayCountTemp;
                worker.SetProgressBar(0, csvFileCount);

                int s = 0;
                foreach (var symbol in symbols)
                {
                    var startTime = manualStartTime;
                    var dayCount = (DateTime.Today - startTime).Days + 1;
                    var chartPack = new ChartPack(interval);

                    for (int i = 0; i < dayCount; i++)
                    {
                        try
                        {
                            worker.Progress(++s);
                            worker.ProgressText($"{symbol}, {i} / {dayCount}");

                            var date = startTime.AddDays(i);
                            var inputFileName = PathUtil.BinanceFuturesData.Down("1m", symbol, $"{symbol}_{date:yyyy-MM-dd}.csv");
                            var data = File.ReadAllLines(inputFileName);

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
                                chartPack.AddChart(new MercuryChartInfo(symbol, quote));
                            }
                        }
                        catch (FileNotFoundException)
                        {
                        }
                    }

                    chartPack.ConvertCandle();

                    var newData = chartPack.Charts
                        .Select(x => x.Quote)
                        .Select(x => string.Join(',', new string[] {
                            x.Date.ToString("yyyy-MM-dd HH:mm:ss"), x.Open.ToString(), x.High.ToString(), x.Low.ToString(), x.Close.ToString(), x.Volume.ToString()
                        }))
                        .ToList();

                    var path = PathUtil.BinanceFuturesData.Down(intervalString, $"{symbol}.csv");
                    File.WriteAllLines(path, newData);
                }
            }
            catch (FileNotFoundException)
            {
                throw;
            }
        }

        public static void ExtractIndicatorTs1(Worker worker, KlineInterval interval, string intervalString)
        {
            try
            {
                var manualStartTime = DateTime.Parse("2020-01-01");
                var symbols = LocalStorageApi.SymbolNames;
                var dayCountTemp = (DateTime.Today - manualStartTime).Days + 1;
                var csvFileCount = symbols.Count * dayCountTemp;
                worker.SetProgressBar(0, csvFileCount);

                int s = 0;
                foreach (var symbol in symbols)
                {
                    var startTime = manualStartTime;
                    var dayCount = (DateTime.Today - startTime).Days + 1;
                    var chartPack = new ChartPack(interval);

                    for (int i = 0; i < dayCount; i++)
                    {
                        try
                        {
                            worker.Progress(++s);
                            worker.ProgressText($"{symbol}, {i} / {dayCount}");

                            var date = startTime.AddDays(i);
                            var inputFileName = PathUtil.BinanceFuturesData.Down("1m", symbol, $"{symbol}_{date:yyyy-MM-dd}.csv");
                            var data = File.ReadAllLines(inputFileName);

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
                                chartPack.AddChart(new MercuryChartInfo(symbol, quote));
                            }
                        }
                        catch (FileNotFoundException)
                        {
                        }
                    }

                    chartPack.ConvertCandle();

                    var quotes = chartPack.Charts.Select(x => x.Quote);
                    var srsi = quotes.GetStochasticRsi(3, 3, 14, 14);
                    var ema = quotes.GetEma(200).Select(x => x.Ema);
                    var k = srsi.Select(x => x.K);
                    var _d = srsi.Select(x => x.D);
                    var supertrends = quotes.GetTripleSupertrend(10, 1, 11, 2, 12, 3);
                    var s1 = supertrends.Select(x => x.Supertrend1);
                    var s2 = supertrends.Select(x => x.Supertrend2);
                    var s3 = supertrends.Select(x => x.Supertrend3);

                    var builder = new StringBuilder();
                    for (int i = 0; i < quotes.Count(); i++)
                    {
                        builder.AppendLine($"{quotes.ElementAt(i).Date:yyyy-MM-dd HH:mm:ss},{ema.ElementAt(i)},{k.ElementAt(i)},{_d.ElementAt(i)},{s1.ElementAt(i)},{s2.ElementAt(i)},{s3.ElementAt(i)}");
                    }
                    File.WriteAllText(PathUtil.BinanceFuturesData.Down("idc", "ts1", intervalString, $"{symbol}.csv"), builder.ToString());
                }
            }
            catch (FileNotFoundException)
            {
                throw;
            }
        }

        public static void ExtractIndicatorTs2(Worker worker, KlineInterval interval, string intervalString)
        {
            try
            {
                var manualStartTime = DateTime.Parse("2020-01-01");
                var symbols = LocalStorageApi.SymbolNames;
                var dayCountTemp = (DateTime.Today - manualStartTime).Days + 1;
                var csvFileCount = symbols.Count * dayCountTemp;
                worker.SetProgressBar(0, csvFileCount);

                int s = 0;
                foreach (var symbol in symbols)
                {
                    var startTime = manualStartTime;
                    var dayCount = (DateTime.Today - startTime).Days + 1;
                    var chartPack = new ChartPack(interval);

                    for (int i = 0; i < dayCount; i++)
                    {
                        try
                        {
                            worker.Progress(++s);
                            worker.ProgressText($"{symbol}, {i} / {dayCount}");

                            var date = startTime.AddDays(i);
                            var inputFileName = PathUtil.BinanceFuturesData.Down("1m", symbol, $"{symbol}_{date:yyyy-MM-dd}.csv");
                            var data = File.ReadAllLines(inputFileName);

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
                                chartPack.AddChart(new MercuryChartInfo(symbol, quote));
                            }
                        }
                        catch (FileNotFoundException)
                        {
                        }
                    }

                    chartPack.ConvertCandle();

                    var quotes = chartPack.Charts.Select(x => x.Quote);
                    var supertrends = quotes.GetTripleSupertrend(10, 1.2, 10, 3, 10, 10);
                    var s1 = supertrends.Select(x => x.Supertrend1);
                    var s2 = supertrends.Select(x => x.Supertrend2);
                    var s3 = supertrends.Select(x => x.Supertrend3);

                    var builder = new StringBuilder();
                    for (int i = 0; i < quotes.Count(); i++)
                    {
                        builder.AppendLine($"{quotes.ElementAt(i).Date:yyyy-MM-dd HH:mm:ss},{s1.ElementAt(i)},{s2.ElementAt(i)},{s3.ElementAt(i)}");
                    }
                    File.WriteAllText(PathUtil.BinanceFuturesData.Down("idc", "ts2", intervalString, $"{symbol}.csv"), builder.ToString());
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
                var getStartTime = SymbolUtil.GetEndDate("BTCUSDT");
                var symbols = LocalStorageApi.SymbolNames;
                var csvFileCount = ((DateTime.Today - getStartTime).Days + 1) * symbols.Count;
                worker.SetProgressBar(0, csvFileCount);

                int p = 0;
                foreach (var symbol in symbols)
                {
                    var startTime = getStartTime;
                    var count = 400;
                    var symbolPath = PathUtil.BinanceFuturesData.Down("1m", symbol);

                    if (!Directory.Exists(symbolPath))
                    {
                        Directory.CreateDirectory(symbolPath);
                    }

                    for (int i = 0; i < count; i++)
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

        public static void GetCandleDataFromBinanceManual(Worker worker)
        {
            try
            {
                var getStartTime = new DateTime(2022, 8, 21);
                //var symbols = LocalStorageApi.SymbolNames;
                var symbols = new List<string> { "BNXUSDT" };
                var maxCount = 500;
                var csvFileCount = maxCount * symbols.Count;
                worker.SetProgressBar(0, csvFileCount);

                int p = 0;
                foreach (var symbol in symbols)
                {
                    var startTime = getStartTime;
                    var symbolPath = PathUtil.BinanceFuturesData.Down("1m", symbol);

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
                var symbols = LocalStorageApi.SymbolNames;
                foreach (var symbol in symbols)
                {
                    var symbolPath = PathUtil.BinanceFuturesData.Down("1m", symbol);
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
