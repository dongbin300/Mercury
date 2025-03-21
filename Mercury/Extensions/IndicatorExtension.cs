using Mercury.Indicators;
using Mercury.Maths;

namespace Mercury.Extensions
{
    public enum QuoteType
    {
        Open,
        High,
        Low,
        Close
    }

    public static class IndicatorExtension
    {
        /// <summary>
        /// Convert to Heikin Ashi candle
        /// First Open
        /// = Candle(0).Open
        /// N Open
        /// = (Candle(-1).Open + Candle(-1).Close) / 2
        /// 
        /// Close
        /// = (Candle(0).Open + Candle(0).High + Candle(0).Low + Candle(0).Close) / 4
        /// 
        /// High
        /// = Max(Candle(0).High, Candle(0).Open, Candle(0).Close)
        /// 
        /// Low
        /// = Min(Candle(0).Low, Candle(0).Open, Candle(0).Close)
        /// </summary>
        /// <param name="quotes"></param>
        /// <returns></returns>
        public static IEnumerable<Quote> GetHeikinAshiCandle(this IEnumerable<Quote> quotes)
        {
            var result = new List<Quote>();

            var _q = quotes.ElementAt(0);
            result.Add(new Quote(
                _q.Date,
                (_q.Open + _q.Close) / 2,
                Math.Max(_q.High, Math.Max(_q.Open, _q.Close)),
                Math.Min(_q.Low, Math.Min(_q.Open, _q.Close)),
                (_q.Open + _q.High + _q.Low + _q.Close) / 4
                ));

            for (int i = 1; i < quotes.Count(); i++)
            {
                var q = quotes.ElementAt(i);
                var prevHa = result[i - 1];
                result.Add(new Quote(
                    q.Date,
                    (prevHa.Open + prevHa.Close) / 2,
                    Math.Max(q.High, Math.Max(q.Open, q.Close)),
                    Math.Min(q.Low, Math.Min(q.Open, q.Close)),
                    (q.Open + q.High + q.Low + q.Close) / 4
                ));
            }

            return result;
        }

        public static IEnumerable<RiResult> GetRi(this IEnumerable<Quote> quotes, int period)
        {
            var result = new List<RiResult>();
            var quoteList = quotes.ToList();

            result.Add(new RiResult(quoteList[0].Date, 0));
            for (int i = 1; i < quoteList.Count; i++)
            {
                double sum = 0;
                var _period = Math.Min(period, i);
                for (int j = 0; j < _period; j++)
                {
                    sum += Convert.ToDouble(quoteList[i - j].Close);
                }
                var average = sum / _period;
                var ri = (Convert.ToDouble(quoteList[i].Close) - average) / average * 1000;
                result.Add(new RiResult(quoteList[i].Date, ri));
            }

            return result;
        }

        public static IEnumerable<LsmaResult> GetLsma(this IEnumerable<Quote> quotes, int period)
        {
            var pl = new List<double>();
            var result = new List<LsmaResult>();
            var quoteList = quotes.ToList();

            for (int i = 0; i < quoteList.Count; i++)
            {
                pl.Add((double)quoteList[i].Close);

                if (pl.Count >= period)
                {
                    double sumX = 0;
                    double sumY = 0;
                    double sumXY = 0;
                    double sumXX = 0;
                    double sumYY = 0;

                    for (int a = 1; a <= pl.Count; a++)
                    {
                        sumX += a;
                        sumY += pl[a - 1];
                        sumXY += pl[a - 1] * a;
                        sumXX += a * a;
                        sumYY += pl[a - 1] * pl[a - 1];
                    }

                    double m = (sumXY - sumX * sumY / period) / (sumXX - sumX * sumX / period);
                    double b = sumY / period - m * sumX / period;
                    result.Add(new LsmaResult(quoteList[i].Date, m * period + b));
                    pl = pl.Skip(1).ToList();
                }
                else
                {
                    result.Add(new LsmaResult(quoteList[i].Date, 0));
                }
            }

            return result;
        }

        public static IEnumerable<RsiResult> GetRsi(this IEnumerable<Quote> quotes, int period = 14)
        {
            var result = new List<RsiResult>();

            var values = quotes.Select(x => (double)x.Close).ToArray();
            var rsi = ArrayCalculator.Rsi(values, period);
            for (int i = 0; i < rsi.Length; i++)
            {
                result.Add(new RsiResult(quotes.ElementAt(i).Date, rsi[i]));
            }

            return result;
        }

        public static IEnumerable<StochResult> GetStoch(this IEnumerable<Quote> quotes, int period)
        {
            var result = new List<StochResult>();

            var high = quotes.Select(x => (double)x.High).ToArray().ToNullable();
            var low = quotes.Select(x => (double)x.Low).ToArray().ToNullable();
            var close = quotes.Select(x => (double)x.Close).ToArray().ToNullable();
            var k = ArrayCalculator.Stoch(high, low, close, period);
            for (int i = 0; i < k.Length; i++)
            {
                result.Add(new StochResult(quotes.ElementAt(i).Date, k[i]));
            }

            return result;
        }

        public static IEnumerable<StochasticRsiResult> GetStochasticRsi(this IEnumerable<Quote> quotes, int smoothK, int smoothD, int rsiPeriod, int stochasticPeriod)
        {
            var result = new List<StochasticRsiResult>();

            var values = quotes.Select(x => (double)x.Close).ToArray();
            (var k, var d) = ArrayCalculator.StochasticRsi(values, smoothK, smoothD, rsiPeriod, stochasticPeriod);
            for (int i = 0; i < k.Length; i++)
            {
                result.Add(new StochasticRsiResult(quotes.ElementAt(i).Date, k[i], d[i]));
            }

            return result;
        }

        public static IEnumerable<SmaResult> GetSma(this IEnumerable<Quote> quotes, int period)
        {
            var result = new List<SmaResult>();

            var values = quotes.Select(x => (double)x.Close).ToArray().ToNullable();
            var sma = ArrayCalculator.Sma(values, period);
            for (int i = 0; i < sma.Length; i++)
            {
                result.Add(new SmaResult(quotes.ElementAt(i).Date, sma[i]));
            }

            return result;
        }

        public static IEnumerable<EmaResult> GetEma(this IEnumerable<Quote> quotes, int period)
        {
            var result = new List<EmaResult>();

            var values = quotes.Select(x => (double)x.Close).ToArray().ToNullable();
            var ema = ArrayCalculator.Ema(values, period);
            for (int i = 0; i < ema.Length; i++)
            {
                result.Add(new EmaResult(quotes.ElementAt(i).Date, ema[i]));
            }

            return result;
        }

        public static IEnumerable<SmaResult> GetVolumeSma(this IEnumerable<Quote> quotes, int period)
        {
            var result = new List<SmaResult>();

            var values = quotes.Select(x => (double)x.Volume).ToArray().ToNullable();
            var sma = ArrayCalculator.Sma(values, period);
            for (int i = 0; i < sma.Length; i++)
            {
                result.Add(new SmaResult(quotes.ElementAt(i).Date, sma[i]));
            }

            return result;
        }

        public static IEnumerable<BbResult> GetBollingerBands(this IEnumerable<Quote> quotes, int period = 20, double deviation = 2.0, QuoteType quoteType = QuoteType.Close)
        {
            var result = new List<BbResult>();

            var values = quoteType switch
            {
                QuoteType.Open => quotes.Select(x => (double)x.Open).ToArray().ToNullable(),
                QuoteType.High => quotes.Select(x => (double)x.High).ToArray().ToNullable(),
                QuoteType.Low => quotes.Select(x => (double)x.Low).ToArray().ToNullable(),
                QuoteType.Close or _ => quotes.Select(x => (double)x.Close).ToArray().ToNullable(),
            };

            (var sma, var upper, var lower) = ArrayCalculator.BollingerBands(values, period, deviation);
            for (int i = 0; i < sma.Length; i++)
            {
                result.Add(new BbResult(quotes.ElementAt(i).Date, sma[i], upper[i], lower[i]));
            }

            return result;
        }

        public static IEnumerable<IchimokuCloudResult> GetIchimokuCloud(this IEnumerable<Quote> quotes, int conversionPeriod = 9, int basePeriod = 26, int leadingSpanPeriod = 52)
        {
            var result = new List<IchimokuCloudResult>();

            var interval = quotes.ElementAt(1).Date - quotes.ElementAt(0).Date;
            var high = quotes.Select(x => (double)x.High).ToArray();
            var low = quotes.Select(x => (double)x.Low).ToArray();
            var close = quotes.Select(x => (double)x.Close).ToArray();
            (var conversion, var _base, var trailingSpan, var leadingSpan1, var leadingSpan2) = ArrayCalculator.IchimokuCloud(high, low, close, conversionPeriod, basePeriod, leadingSpanPeriod);
            for (int i = 0; i < high.Length; i++)
            {
                result.Add(new IchimokuCloudResult(quotes.ElementAt(i).Date, conversion[i], _base[i], trailingSpan[i], leadingSpan1[i], leadingSpan2[i]));
            }
            // TODO : value sync to time
            var nextDateTime = result[^1].Date;
            for (int i = high.Length; i < high.Length + basePeriod - 1; i++)
            {
                nextDateTime += interval;
                result.Add(new IchimokuCloudResult(nextDateTime, 0, 0, 0, leadingSpan1[i], leadingSpan2[i]));
            }

            return result;
        }

        public static IEnumerable<MacdResult> GetMacd(this IEnumerable<Quote> quotes, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
        {
            var result = new List<MacdResult>();

            var values = quotes.Select(x => (double)x.Close).ToArray().ToNullable();
            (var macd, var signal, var hist) = ArrayCalculator.Macd(values, fastPeriod, slowPeriod, signalPeriod);
            for (int i = 0; i < macd.Length; i++)
            {
                result.Add(new MacdResult(quotes.ElementAt(i).Date, macd[i], signal[i], hist[i]));
            }

            return result;
        }

        public static IEnumerable<SupertrendResult> GetSupertrend(this IEnumerable<Quote> quotes, int atrPeriod, double factor)
        {
            var result = new List<SupertrendResult>();

            var high = quotes.Select(x => (double)x.High).ToArray();
            var low = quotes.Select(x => (double)x.Low).ToArray();
            var close = quotes.Select(x => (double)x.Close).ToArray();
            (var supertrend, var direction) = ArrayCalculator.Supertrend(high, low, close, factor, atrPeriod);

            for (int i = 0; i < supertrend.Length; i++)
            {
                var st = i >= atrPeriod - 1 ? -direction[i] * supertrend[i] : 0;
                var _supertrend = new SupertrendResult(quotes.ElementAt(i).Date, st);
                result.Add(_supertrend);
            }

            return result;
        }

        public static IEnumerable<SupertrendResult> GetReverseSupertrend(this IEnumerable<Quote> quotes, int atrPeriod, double factor)
        {
            var result = new List<SupertrendResult>();

            var high = quotes.Select(x => (double)x.High).ToArray();
            var low = quotes.Select(x => (double)x.Low).ToArray();
            var close = quotes.Select(x => (double)x.Close).ToArray();
            (var supertrend, var direction) = ArrayCalculator.ReverseSupertrend(high, low, close, factor, atrPeriod);

            for (int i = 0; i < supertrend.Length; i++)
            {
                var st = i >= atrPeriod - 1 ? -direction[i] * supertrend[i] : 0;
                var _supertrend = new SupertrendResult(quotes.ElementAt(i).Date, st);
                result.Add(_supertrend);
            }

            return result;
        }

        public static IEnumerable<TripleSupertrendResult> GetTripleSupertrend(this IEnumerable<Quote> quotes, int atrPeriod1, double factor1, int atrPeriod2, double factor2, int atrPeriod3, double factor3)
        {
            var result = new List<TripleSupertrendResult>();

            var high = quotes.Select(x => (double)x.High).ToArray();
            var low = quotes.Select(x => (double)x.Low).ToArray();
            var close = quotes.Select(x => (double)x.Close).ToArray();
            (var supertrend1, var direction1, var supertrend2, var direction2, var supertrend3, var direction3) = ArrayCalculator.TripleSupertrend(high, low, close, atrPeriod1, factor1, atrPeriod2, factor2, atrPeriod3, factor3);

            for (int i = 0; i < supertrend1.Length; i++)
            {
                var st1 = i >= atrPeriod1 - 1 ? -direction1[i] * supertrend1[i] : 0;
                var st2 = i >= atrPeriod2 - 1 ? -direction2[i] * supertrend2[i] : 0;
                var st3 = i >= atrPeriod3 - 1 ? -direction3[i] * supertrend3[i] : 0;
                var tripleSupertrend = new TripleSupertrendResult(quotes.ElementAt(i).Date, st1, st2, st3);
                result.Add(tripleSupertrend);
            }

            return result;
        }

        public static IEnumerable<AdxResult> GetAdx(this IEnumerable<Quote> quotes, int adxPeriod = 14, int diPeriod = 14)
        {
            var result = new List<AdxResult>();

            var high = quotes.Select(x => (double)x.High).ToArray();
            var low = quotes.Select(x => (double)x.Low).ToArray();
            var close = quotes.Select(x => (double)x.Close).ToArray();
            var adx = ArrayCalculator.Adx(high, low, close, adxPeriod, diPeriod);

            for (int i = 0; i < adx.Length; i++)
            {
                result.Add(new AdxResult(quotes.ElementAt(i).Date, adx[i]));
            }

            return result;
        }

        public static IEnumerable<AtrResult> GetAtr(this IEnumerable<Quote> quotes, int period = 14)
        {
            var result = new List<AtrResult>();

            var high = quotes.Select(x => (double)x.High).ToArray();
            var low = quotes.Select(x => (double)x.Low).ToArray();
            var close = quotes.Select(x => (double)x.Close).ToArray();
            var atr = ArrayCalculator.Atr(high, low, close, period);

            for (int i = 0; i < atr.Length; i++)
            {
                result.Add(new AtrResult(quotes.ElementAt(i).Date, atr[i]));
            }

            return result;
        }

        public static IEnumerable<TsvResult> GetTsv(this IEnumerable<Quote> quotes, int period)
        {
            var result = new List<TsvResult>();

            var close = quotes.Select(x => (double)x.Close).ToArray();
            var volume = quotes.Select(x => (double)x.Volume).ToArray();
            var tsv = ArrayCalculator.TimeSegmentedVolume(close, volume, period);

            for (int i = 0; i < tsv.Length; i++)
            {
                result.Add(new TsvResult(quotes.ElementAt(i).Date, tsv[i]));
            }

            return result;
        }

        public static IEnumerable<CustomResult> GetCustom(this IEnumerable<Quote> quotes, int period)
        {
            var result = new List<CustomResult>();

            var open = quotes.Select(x => (double)x.Open).ToArray();
            var high = quotes.Select(x => (double)x.High).ToArray();
            var low = quotes.Select(x => (double)x.Low).ToArray();
            var close = quotes.Select(x => (double)x.Close).ToArray();
            var volume = quotes.Select(x => (double)x.Volume).ToArray();
            (var upper, var lower, var pioneer, var player) = ArrayCalculator.Custom(open, high, low, close, volume, period);

            for (int i = 0; i < upper.Length; i++)
            {
                result.Add(new CustomResult(quotes.ElementAt(i).Date, upper[i], lower[i], pioneer[i], player[i]));
            }

            return result;
        }

        public static IEnumerable<TrendRiderResult> GetTrendRider(this IEnumerable<Quote> quotes, int atrPeriod = 10, double atrMultiplier = 3.0, int rsiPeriod = 14, int macdFastPeriod = 12, int macdSlowPeriod = 26, int macdSignalPeriod = 9)
        {
            var result = new List<TrendRiderResult>();

            var high = quotes.Select(x => (double)x.High).ToArray();
            var low = quotes.Select(x => (double)x.Low).ToArray();
            var close = quotes.Select(x => (double)x.Close).ToArray();
            (var trend, var supertrend, var supertrendDirection) = ArrayCalculator.TrendRider(high, low, close, atrPeriod, atrMultiplier, rsiPeriod, macdFastPeriod, macdSlowPeriod, macdSignalPeriod);

            for (int i = 0; i < trend.Length; i++)
            {
                var st = i >= atrPeriod - 1 ? -supertrendDirection[i] * supertrend[i] : 0;
                var trendRider = new TrendRiderResult(quotes.ElementAt(i).Date, -trend[i], st);
                result.Add(trendRider);
            }

            return result;
        }

        public static IEnumerable<PredictiveRangesResult> GetPredictiveRanges(this IEnumerable<Quote> quotes, int period = 200, double factor = 6.0)
        {
            var result = new List<PredictiveRangesResult>();

            var high = quotes.Select(x => (double)x.High).ToArray();
            var low = quotes.Select(x => (double)x.Low).ToArray();
            var close = quotes.Select(x => (double)x.Close).ToArray();
            (var upper2, var upper, var average, var lower, var lower2) = ArrayCalculator.PredictiveRanges(high, low, close, period, factor);

            for (int i = 0; i < upper2.Length; i++)
            {
                var predictiveRanges = new PredictiveRangesResult(quotes.ElementAt(i).Date, upper2[i], upper[i], average[i], lower[i], lower2[i]);
                result.Add(predictiveRanges);
            }

            return result;
        }
    }
}
