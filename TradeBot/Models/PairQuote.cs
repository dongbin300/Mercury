using Binance.Net.Enums;

using Mercury;
using Mercury.Maths;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Media;

namespace TradeBot.Models
{
    public class PairQuote(string symbol, IEnumerable<Quote> quotes)
	{
        private int DecimalCount = 4;

		public string Symbol { get; set; } = symbol;
		public List<ChartInfo> Charts { get; set; } = quotes.Select(quote => new ChartInfo(quote)).ToList();

		public string EntryRate => GetEntryRateString();
        public SolidColorBrush EntryRateColor => GetEntryRateColor();

        //public decimal CurrentPrice => Charts[^1].Quote.Close;
        public double PrevMacd => Math.Round(Charts[^2].Macd * 10000, DecimalCount);
        public double PrevSignal => Math.Round(Charts[^2].Signal * 10000, DecimalCount);
        public double CurrentMacd => Math.Round(Charts[^1].Macd * 10000, DecimalCount);
        public double CurrentSignal => Math.Round(Charts[^1].Signal * 10000, DecimalCount);
        //public double CurrentAdx => Math.Round(Charts[^1].Adx, DecimalCount);
        //public double CurrentSupertrend => Math.Round(Charts[^1].Supertrend, DecimalCount);
        public SolidColorBrush SymbolColor => GetSymbolColor();

		public void UpdateQuote(Quote quote)
        {
            try
            {
				var lastQuote = Charts[^1];
				//if (lastQuote.Quote.Date.Equals(quote.Date) || quote.Date.Minute % Common.BaseIntervalNumber != 0)
				if (lastQuote.Quote.Date.Hour.Equals(quote.Date.Hour))
				{
					lastQuote.Quote.High = quote.High;
					lastQuote.Quote.Low = quote.Low;
					lastQuote.Quote.Close = quote.Close;
					lastQuote.Quote.Volume = quote.Volume;
				}
				else
				{
					Charts.Add(new ChartInfo(quote));
				}
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(PairQuote), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

        public void UpdateIndicators()
        {
            try
            {
                //var quotes = Charts.Select(x => x.Quote);
                //var macd = quotes.GetMacd(12, 26, 9);
                //var m = macd.Select(x => x.Macd);
                //var s = macd.Select(x => x.Signal);
                //var st = quotes.GetSupertrend(10, 1.5).Select(x => x.Supertrend);
                //var adx = quotes.GetAdx(14, 14).Select(x => x.Adx);
                //var stoch = quotes.GetStoch(12).Select(x => x.Stoch);

                //for (int i = 0; i < Charts.Count; i++)
                //{
                //    Charts[i].Macd = m.ElementAt(i);
                //    Charts[i].Signal = s.ElementAt(i);
                //    Charts[i].Supertrend = st.ElementAt(i);
                //    Charts[i].Adx = adx.ElementAt(i);
                //    Charts[i].Stoch = stoch.ElementAt(i);
                //}
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(PairQuote), MethodBase.GetCurrentMethod()?.Name, ex);
            }
        }

        public bool IsPowerGoldenCross(int lookback)
        {
            // Starts at Charts[^2]
            for (int i = 2; i < 2 + lookback; i++)
            {
                var c0 = Charts[^i];
                var c1 = Charts[^(i + 1)];

                if (c0.Macd < 0 && c0.Macd > c0.Signal && c1.Macd < c1.Signal && c0.Supertrend > 0 && c0.Adx > 30)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsPowerDeadCross(int lookback)
        {
            // Starts at Charts[^2]
            for (int i = 2; i < 2 + lookback; i++)
            {
                var c0 = Charts[^i];
                var c1 = Charts[^(i + 1)];

                if (c0.Macd > 0 && c0.Macd < c0.Signal && c1.Macd > c1.Signal && c0.Supertrend < 0 && c0.Adx > 30)
                {
                    return true;
                }
            }
            return false;
        }

        private SolidColorBrush GetSymbolColor()
        {
            if (Common.IsPositioning(Symbol, PositionSide.Long))
            {
                return Common.IsPositioning(Symbol, PositionSide.Short) ? new SolidColorBrush(Colors.Yellow) : Common.LongColor;
            }
            else if (Common.IsPositioning(Symbol, PositionSide.Short))
            {
                return Common.ShortColor;
            }
            else
            {
                return Common.WhiteColor;
            }
        }

        private SolidColorBrush GetCurrentMacdColor()
        {
            return CurrentMacd >= 0
                ? CurrentMacd < CurrentSignal && PrevMacd > PrevSignal ? Common.ShortColor : Common.OffColor
                : CurrentMacd > CurrentSignal && PrevMacd < PrevSignal ? Common.LongColor : Common.OffColor;
        }

        private string GetEntryRateString()
        {
            var result = string.Empty;

            var c0 = Charts[^1]; // 현재 정보
            var c1 = Charts[^2]; // 1봉전 정보

            if (IsPowerGoldenCross(14))
            {
                result += "▲";

                var side = PositionSide.Long;
                var minPrice = Charts.SkipLast(1).TakeLast(24).Min(x => x.Quote.Low);
                var maxPrice = Charts.SkipLast(1).TakeLast(24).Max(x => x.Quote.High);
                var slPer = Calculator.Roe(side, c0.Quote.Open, minPrice) * 1.1m;
                var tpPer = Calculator.Roe(side, c0.Quote.Open, maxPrice) * 0.9m;

                if (c1.Macd < 0)
                {
                    result += "M";
                }
                if (tpPer > 1.0m)
                {
                    result += "T";
                }
                if (c1.Stoch < 20)
                {
                    result += "S";
                }
            }
            else if (IsPowerDeadCross(14))
            {
                result += "▼";

                var side = PositionSide.Short;
                var minPrice = Charts.SkipLast(1).TakeLast(24).Min(x => x.Quote.Low);
                var maxPrice = Charts.SkipLast(1).TakeLast(24).Max(x => x.Quote.High);
                var slPer = Calculator.Roe(side, c0.Quote.Open, maxPrice) * 1.1m;
                var tpPer = Calculator.Roe(side, c0.Quote.Open, minPrice) * 0.9m;

                if (c1.Macd > 0)
                {
                    result += "M";
                }
                if (tpPer > 1.0m)
                {
                    result += "T";
                }
                if (c1.Stoch > 80)
                {
                    result += "S";
                }
            }

            return result;
        }

        private SolidColorBrush GetEntryRateColor()
        {
            if (IsPowerGoldenCross(14))
            {
                return Common.LongColor;
            }
            else if(IsPowerDeadCross(14))
            {
                return Common.ShortColor;
            }

            return Common.OffColor;
        }
    }
}
