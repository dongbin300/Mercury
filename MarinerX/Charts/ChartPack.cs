using Binance.Net.Enums;

using Mercury;

using MercuryTradingModel.Charts;
using MercuryTradingModel.Elements;
using MercuryTradingModel.Enums;
using MercuryTradingModel.Indicators;

using System;
using System.Collections.Generic;
using System.Linq;

namespace MarinerX.Charts
{
    /// <summary>
    /// 심볼 하나의 모든 차트 정보
    /// </summary>
    internal class ChartPack
    {
        public string Symbol => Charts.First().Symbol;
        public KlineInterval Interval = KlineInterval.OneMinute;
        public IList<MercuryChartInfo> Charts { get; set; } = new List<MercuryChartInfo>();
        public DateTime StartTime => Charts.Min(x => x.DateTime);
        public DateTime EndTime => Charts.Max(x => x.DateTime);
        public MercuryChartInfo? CurrentChart;

        public ChartPack(KlineInterval interval)
        {
            Interval = interval;
            CurrentChart = null;
        }

        public void AddChart(MercuryChartInfo chart)
        {
            Charts.Add(chart);
        }

        public void ConvertCandle()
        {
            if (Interval == KlineInterval.OneMinute)
            {
                return;
            }

            var newQuotes = new List<Quote>();

            int unitCount = Interval switch
            {
                KlineInterval.ThreeMinutes => 3,
                KlineInterval.FiveMinutes => 5,
                KlineInterval.FifteenMinutes => 15,
                KlineInterval.ThirtyMinutes => 30,
                KlineInterval.OneHour => 60,
                KlineInterval.TwoHour => 120,
                KlineInterval.FourHour => 240,
                KlineInterval.SixHour => 360,
                KlineInterval.EightHour => 480,
                KlineInterval.TwelveHour => 720,
                KlineInterval.OneDay => 1440,
                _ => 1
            };

            int i = 0;
            for(; i < Charts.Count; i++)
            {
                if ((Charts[i].DateTime.Hour * 60 + Charts[i].DateTime.Minute) % unitCount == 0)
                {
                    break;
                }
            }

            for (; i < Charts.Count; i += unitCount)
            {
                var targets = Charts.Skip(i).Take(unitCount).Select(x => x.Quote).ToList();

                newQuotes.Add(new Quote
                {
                    Date = targets[0].Date,
                    Open = targets[0].Open,
                    High = targets.Max(t => t.High),
                    Low = targets.Min(t => t.Low),
                    Close = targets[^1].Close,
                    Volume = targets.Sum(t => t.Volume)
                });
            }

            var newChart = newQuotes.Select(candle => new MercuryChartInfo(Symbol, candle)).ToList();
            Charts = newChart;
        }

        public decimal GetChartElementValue(IEnumerable<double> data, int index)
        {
            var value = data.ElementAt(index);
            return (decimal)value;
        }

        public void CalculateCommasIndicatorsRobHoffman()
        {
            var quotes = Charts.Select(x => x.Quote);
            var r1 = quotes.GetSma(5).Select(x => x.Sma);
            var r2 = quotes.GetSma(50).Select(x => x.Sma);
            var r3 = quotes.GetSma(89).Select(x => x.Sma);
            var r4 = quotes.GetEma(18).Select(x => x.Ema);
            var r5 = quotes.GetEma(144).Select(x => x.Ema);
            for (int i = 0; i < Charts.Count; i++)
            {
                var chart = Charts[i];
                chart.ChartElements.Clear();
                chart.ChartElements.Add(new ChartElementResult(
                    ChartElementType.ma,
                    GetChartElementValue(r1, i)));
                chart.ChartElements.Add(new ChartElementResult(
                    ChartElementType.ma2,
                    GetChartElementValue(r2, i)));
                chart.ChartElements.Add(new ChartElementResult(
                    ChartElementType.ma3,
                    GetChartElementValue(r3, i)));
                chart.ChartElements.Add(new ChartElementResult(
                    ChartElementType.ema,
                    GetChartElementValue(r4, i)));
                chart.ChartElements.Add(new ChartElementResult(
                    ChartElementType.ema2,
                    GetChartElementValue(r5, i)));
            }
        }

        public void CalculateCommasIndicatorsEveryonesCoin()
        {
            var quotes = Charts.Select(x => x.Quote);
            var r1 = quotes.GetLsma(10).Select(x => x.Lsma);
            var r2 = quotes.GetLsma(30).Select(x => x.Lsma);
            var r3 = quotes.GetRsi(14).Select(x => x.Rsi);
            for (int i = 0; i < Charts.Count; i++)
            {
                var chart = Charts[i];
                chart.ChartElements.Clear();
                chart.ChartElements.Add(new ChartElementResult(
                    ChartElementType.lsma,
                    GetChartElementValue(r1, i)));
                chart.ChartElements.Add(new ChartElementResult(
                    ChartElementType.lsma2,
                    GetChartElementValue(r2, i)));
                chart.ChartElements.Add(new ChartElementResult(
                    ChartElementType.rsi,
                    GetChartElementValue(r3, i)));
            }
        }

        public void CalculateCommasIndicators()
        {
            var quotes = Charts.Select(x => x.Quote);
            for (int i = 0; i < Charts.Count; i++)
            {
                var chart = Charts[i];
                chart.ChartElements.Clear();
            }
        }

        public void CalculateIndicators(IList<ChartElement> chartElements, IList<NamedElement> namedElements)
        {
            var quotes = Charts.Select(x => x.Quote);

            // calculate chart elements
            var chartElementResults = new List<List<ChartElementResult>>();
            foreach (var chartElement in chartElements)
            {
                IEnumerable<ChartElementResult> result = chartElement.ElementType switch
                {
                    ChartElementType.ma => quotes.GetSma((int)chartElement.Parameters[0]).Select(x => new ChartElementResult(chartElement.ElementType, Convert.ToDecimal(x.Sma))),
                    ChartElementType.ema => quotes.GetEma((int)chartElement.Parameters[0]).Select(x => new ChartElementResult(chartElement.ElementType, Convert.ToDecimal(x.Ema))),
                    ChartElementType.ri => quotes.GetRi((int)chartElement.Parameters[0]).Select(x => new ChartElementResult(chartElement.ElementType, Convert.ToDecimal(x.Ri))),
                    ChartElementType.rsi => quotes.GetRsi((int)chartElement.Parameters[0]).Select(x => new ChartElementResult(chartElement.ElementType, Convert.ToDecimal(x.Rsi))),
                    ChartElementType.macd_macd => quotes.GetMacd((int)chartElement.Parameters[0], (int)chartElement.Parameters[1], (int)chartElement.Parameters[2]).Select(x => new ChartElementResult(chartElement.ElementType, Convert.ToDecimal(x.Macd))),
                    ChartElementType.macd_signal => quotes.GetMacd((int)chartElement.Parameters[0], (int)chartElement.Parameters[1], (int)chartElement.Parameters[2]).Select(x => new ChartElementResult(chartElement.ElementType, Convert.ToDecimal(x.Signal))),
                    ChartElementType.macd_hist => quotes.GetMacd((int)chartElement.Parameters[0], (int)chartElement.Parameters[1], (int)chartElement.Parameters[2]).Select(x => new ChartElementResult(chartElement.ElementType, Convert.ToDecimal(x.Hist))),
                    ChartElementType.bb_sma => quotes.GetBollingerBands((int)chartElement.Parameters[0], (double)chartElement.Parameters[1]).Select(x => new ChartElementResult(chartElement.ElementType, Convert.ToDecimal(x.Sma))),
                    ChartElementType.bb_upper => quotes.GetBollingerBands((int)chartElement.Parameters[0], (double)chartElement.Parameters[1]).Select(x => new ChartElementResult(chartElement.ElementType, Convert.ToDecimal(x.Upper))),
                    ChartElementType.bb_lower => quotes.GetBollingerBands((int)chartElement.Parameters[0], (double)chartElement.Parameters[1]).Select(x => new ChartElementResult(chartElement.ElementType, Convert.ToDecimal(x.Lower))),
                    _ => default!
                };
                chartElementResults.Add(result.ToList());
            }
            for (int j = 0; j < Charts.Count; j++)
            {
                var chart = Charts[j];
                chart.ChartElements.Clear();
                for (int i = 0; i < chartElementResults.Count; i++)
                {
                    chart.ChartElements.Add(chartElementResults[i][j]);
                }
            }

            // calculate named elements
            var namedElementResults = new List<List<NamedElementResult>>();
            foreach (var namedElement in namedElements)
            {
                IEnumerable<NamedElementResult> result = namedElement.ElementType switch
                {
                    ChartElementType.ma => quotes.GetSma((int)namedElement.Parameters[0]).Select(x => new NamedElementResult(namedElement.Name, Convert.ToDecimal(x.Sma))),
                    ChartElementType.ema => quotes.GetEma((int)namedElement.Parameters[0]).Select(x => new NamedElementResult(namedElement.Name, Convert.ToDecimal(x.Ema))),
                    ChartElementType.ri => quotes.GetRi((int)namedElement.Parameters[0]).Select(x => new NamedElementResult(namedElement.Name, Convert.ToDecimal(x.Ri))),
                    ChartElementType.rsi => quotes.GetRsi((int)namedElement.Parameters[0]).Select(x => new NamedElementResult(namedElement.Name, Convert.ToDecimal(x.Rsi))),
                    ChartElementType.macd_macd => quotes.GetMacd((int)namedElement.Parameters[0], (int)namedElement.Parameters[1], (int)namedElement.Parameters[2]).Select(x => new NamedElementResult(namedElement.Name, Convert.ToDecimal(x.Macd))),
                    ChartElementType.macd_signal => quotes.GetMacd((int)namedElement.Parameters[0], (int)namedElement.Parameters[1], (int)namedElement.Parameters[2]).Select(x => new NamedElementResult(namedElement.Name, Convert.ToDecimal(x.Signal))),
                    ChartElementType.macd_hist => quotes.GetMacd((int)namedElement.Parameters[0], (int)namedElement.Parameters[1], (int)namedElement.Parameters[2]).Select(x => new NamedElementResult(namedElement.Name, Convert.ToDecimal(x.Hist))),
                    ChartElementType.bb_sma => quotes.GetBollingerBands((int)namedElement.Parameters[0], (double)namedElement.Parameters[1]).Select(x => new NamedElementResult(namedElement.Name, Convert.ToDecimal(x.Sma))),
                    ChartElementType.bb_upper => quotes.GetBollingerBands((int)namedElement.Parameters[0], (double)namedElement.Parameters[1]).Select(x => new NamedElementResult(namedElement.Name, Convert.ToDecimal(x.Upper))),
                    ChartElementType.bb_lower => quotes.GetBollingerBands((int)namedElement.Parameters[0], (double)namedElement.Parameters[1]).Select(x => new NamedElementResult(namedElement.Name, Convert.ToDecimal(x.Lower))),
                    _ => default!
                };
                namedElementResults.Add(result.ToList());
            }
            for (int j = 0; j < Charts.Count; j++)
            {
                var chart = Charts[j];
                chart.NamedElements.Clear();
                for (int i = 0; i < namedElementResults.Count; i++)
                {
                    chart.NamedElements.Add(namedElementResults[i][j]);
                }
            }
        }

        public MercuryChartInfo Select()
        {
            return CurrentChart = GetChart(StartTime);
        }

        public MercuryChartInfo Select(DateTime time)
        {
            return CurrentChart = GetChart(time);
        }

        public MercuryChartInfo Next() =>
            CurrentChart == null ?
            CurrentChart = default! :
            CurrentChart = GetChart(Interval switch
            {
                KlineInterval.OneMinute => CurrentChart.DateTime.AddMinutes(1),
                KlineInterval.ThreeMinutes => CurrentChart.DateTime.AddMinutes(3),
                KlineInterval.FiveMinutes => CurrentChart.DateTime.AddMinutes(5),
                KlineInterval.FifteenMinutes => CurrentChart.DateTime.AddMinutes(15),
                KlineInterval.ThirtyMinutes => CurrentChart.DateTime.AddMinutes(30),
                KlineInterval.OneHour => CurrentChart.DateTime.AddHours(1),
                KlineInterval.TwoHour => CurrentChart.DateTime.AddHours(2),
                KlineInterval.FourHour => CurrentChart.DateTime.AddHours(4),
                KlineInterval.SixHour => CurrentChart.DateTime.AddHours(6),
                KlineInterval.EightHour => CurrentChart.DateTime.AddHours(8),
                KlineInterval.TwelveHour => CurrentChart.DateTime.AddHours(12),
                KlineInterval.OneDay => CurrentChart.DateTime.AddDays(1),
                _ => CurrentChart.DateTime.AddMinutes(1)
            });

        public MercuryChartInfo GetChart(DateTime dateTime) => Charts.First(x => x.DateTime.Equals(dateTime));
    }
}
