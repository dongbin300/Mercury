using TradeBot.Interfaces;

using System.Collections.Generic;

namespace TradeBot.Bots
{
    public class Bot(string name, string description) : IBot
    {
		public string Name { get; set; } = name;
		public string Description { get; set; } = description;
		protected List<string> MonitorSymbols { get; set; } =
		[
			"JASMYUSDT",
            "DOGEUSDT",
            "ZECUSDT",
            "UNIUSDT",
            "XMRUSDT",
            "MASKUSDT",
            "SUSHIUSDT",
            "ATOMUSDT",
            "GRTUSDT",
            "ENSUSDT",
            "CHZUSDT",
            "BNBUSDT",
            "NKNUSDT",
            "OMGUSDT",
            "NEOUSDT",
            "WAVESUSDT",
            "XRPUSDT",
            "DASHUSDT",
            "OCEANUSDT",
            "ROSEUSDT",
            "BALUSDT",
            "SANDUSDT",
            "BANDUSDT",
            "COTIUSDT",
            "EGLDUSDT",
            "IOSTUSDT",
            "LTCUSDT",
            "ADAUSDT",
            "KAVAUSDT",
            "RLCUSDT",
            "MATICUSDT",
            "AAVEUSDT",
            "BELUSDT",
            "FTMUSDT",
            "ALPHAUSDT",
            "XLMUSDT",
            "KNCUSDT",
            "ETCUSDT",
            "OPUSDT",
            "HBARUSDT"
		];

        public Bot() : this("", "")
        {

        }

        public Bot(string name) : this(name, "")
        {

        }
	}
}
