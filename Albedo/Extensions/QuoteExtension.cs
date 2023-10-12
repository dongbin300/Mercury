using Albedo.Enums;

using Mercury;

using System;
using System.Collections.Generic;

namespace Albedo.Extensions
{
    public static class QuoteExtension
    {
        public static List<Quote> Merge(this List<Quote> quotes, CandleInterval toInterval)
        {
            var newQuotes = new List<Quote>();
            var newQuote = new Quote();
            var isFirst = true;
            MergeIntervalType intervalType = MergeIntervalType.Minutes;

            var num = 1;
            switch (toInterval)
            {
                case CandleInterval.OneWeek:
                    intervalType = MergeIntervalType.Week;
                    break;

                case CandleInterval.OneMonth:
                    intervalType = MergeIntervalType.Month;
                    break;

                default:
                    num = toInterval switch
                    {
                        CandleInterval.ThreeMinutes => 3,
                        CandleInterval.FiveMinutes => 5,
                        CandleInterval.TenMinutes => 10,
                        CandleInterval.FifteenMinutes => 15,
                        CandleInterval.ThirtyMinutes => 30,
                        _ => 1
                    };
                    break;
            }

            for (int i = 0; i < quotes.Count; i++)
            {
                var quote = quotes[i];
                var separationCondition = intervalType switch
                {
                    MergeIntervalType.Minutes => quote.Date.Minute % num == 0,
                    MergeIntervalType.Week => quote.Date.DayOfWeek == DayOfWeek.Sunday,
                    MergeIntervalType.Month => quote.Date.Day == 1,
                    _ => false
                };

                if (separationCondition || i == quotes.Count - 1)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        newQuotes.Add(newQuote);
                    }
                    newQuote = new Quote()
                    {
                        Date = quote.Date,
                        Open = quote.Open,
                        High = quote.High,
                        Low = quote.Low,
                        Close = quote.Close,
                        Volume = quote.Volume
                    };
                }
                else
                {
                    if (isFirst)
                    {
                        continue;
                    }

                    newQuote.High = Math.Max(newQuote.High, quote.High);
                    newQuote.Low = Math.Min(newQuote.Low, quote.Low);
                    newQuote.Close = quote.Close;
                    newQuote.Volume += quote.Volume;
                }
            }

            return newQuotes;
        }
    }
}
