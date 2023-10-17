using Binance.Net.Enums;

namespace Mercury.Charts
{
    public class ChartLoader
    {
        public static List<ChartPack> Charts { get; set; } = new();
        public static ChartPack GetChartPack(string symbol, KlineInterval interval) => Charts.Find(x => x.Symbol.Equals(symbol) && x.Interval.Equals(interval)) ?? default!;

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
