using Binance.Net.Objects.Models.Spot;

using System;
using System.Collections.Generic;
using System.Linq;

namespace MarinerX.Charts
{
    internal class TradePack
    {
        public string Symbol { get; set; }
        public IList<BinanceAggregatedTrade> Trades { get; set; } = new List<BinanceAggregatedTrade>();
        public int CurrentIndex { get; set; }
        public BinanceAggregatedTrade CurrentTrade => Trades[CurrentIndex];

        public TradePack(string symbol)
        {
            Symbol = symbol;
            CurrentIndex = 0;
        }

        public void AddTrade(BinanceAggregatedTrade trade)
        {
            Trades.Add(trade);
        }

        public BinanceAggregatedTrade Select()
        {
            CurrentIndex = 0;
            return CurrentTrade;
        }

        public BinanceAggregatedTrade Select(int year, int month, int day)
        {
            var trade = Trades.First(x => x.TradeTime.Year == year && x.TradeTime.Month == month && x.TradeTime.Day == day) ?? throw new Exception("No Aggregated Trade");
            CurrentIndex = Trades.IndexOf(trade);
            return CurrentTrade;
        }

        public BinanceAggregatedTrade Next()
        {
            CurrentIndex++;
            return CurrentTrade;
        }
    }
}
