using TradeBot.Interfaces;

using System.Collections.Generic;

namespace TradeBot.Bots
{
    public class Bot : IBot
    {
        public string Name { get; set; }
        public string Description { get; set; }
        protected List<string> MonitorSymbols { get; set; } = new()
        {
            "RLCUSDT",
            "UNFIUSDT",
            "LPTUSDT",
            "QTUMUSDT",
            "OMGUSDT",
            "CHZUSDT",
            "STORJUSDT",
            "KNCUSDT",
            "BALUSDT",
            "COMPUSDT",
            "GALUSDT",
            "YFIUSDT",
            "MTLUSDT",
            "IMXUSDT",
            "ENSUSDT",
            "DASHUSDT",
            "MANAUSDT",
            "WAVESUSDT",
            "MATICUSDT",
            "BLZUSDT",
            "ZENUSDT",
            "SFPUSDT",
            "SANDUSDT",
            "BCHUSDT",
            "LTCUSDT",
            "TRXUSDT",
            "ALPHAUSDT",
            "ETCUSDT",
            "ENJUSDT",
            "ARUSDT",
            "COTIUSDT",
            "AVAXUSDT",
            "SXPUSDT",
            "AXSUSDT",
            "BANDUSDT",
            "NEOUSDT",
            "OCEANUSDT",
            "ZECUSDT",
            "NKNUSDT",
            "GRTUSDT"
        };

        public Bot() : this("", "")
        {

        }

        public Bot(string name) : this(name, "")
        {

        }

        public Bot(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
