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
			"ENSUSDT",
			"DOGEUSDT",
			"ZECUSDT",
			"MASKUSDT",
			"BANDUSDT",
			"SUSHIUSDT",
			"UNIUSDT",
			"XMRUSDT",
			"APEUSDT",
			"LRCUSDT",
			"ATOMUSDT",
			"DASHUSDT",
			"NKNUSDT",
			"COTIUSDT",
			"ROSEUSDT",
			"VETUSDT",
			"BNBUSDT",
			"GRTUSDT",
			"NEARUSDT",
			"ALPHAUSDT",
			"ANKRUSDT",
			"KAVAUSDT",
			"XRPUSDT",
			"LTCUSDT",
		];

		protected List<string> AllSymbols { get; set; } =
		[
			"AAVEUSDT",
			"ADAUSDT",
			"ALPHAUSDT",
			"ANKRUSDT",
			"APEUSDT",
			"API3USDT",
			"ARPAUSDT",
			"ARUSDT",
			"ATOMUSDT",
			"AVAXUSDT",
			"AXSUSDT",
			"BAKEUSDT",
			"BANDUSDT",
			"BCHUSDT",
			"BELUSDT",
			"BLZUSDT",
			"BNBUSDT",
			"BTCUSDT",
			"CHZUSDT",
			"COMPUSDT",
			"COTIUSDT",
			"DASHUSDT",
			"DOGEUSDT",
			"DOTUSDT",
			"DUSKUSDT",
			"EGLDUSDT",
			"ENJUSDT",
			"ENSUSDT",
			"ETCUSDT",
			"ETHUSDT",
			"FILUSDT",
			"FTMUSDT",
			"GMTUSDT",
			"GRTUSDT",
			"HBARUSDT",
			"IMXUSDT",
			"IOSTUSDT",
			"IOTXUSDT",
			"JASMYUSDT",
			"KAVAUSDT",
			"KNCUSDT",
			"KSMUSDT",
			"LPTUSDT",
			"LRCUSDT",
			"LTCUSDT",
			"MANAUSDT",
			"MASKUSDT",
			"MATICUSDT",
			"MKRUSDT",
			"MTLUSDT",
			"NEARUSDT",
			"NEOUSDT",
			"NKNUSDT",
			"OMGUSDT",
			"PEOPLEUSDT",
			"QTUMUSDT",
			"RENUSDT",
			"RLCUSDT",
			"ROSEUSDT",
			"RSRUSDT",
			"RUNEUSDT",
			"SANDUSDT",
			"SFPUSDT",
			"SKLUSDT",
			"SOLUSDT",
			"STORJUSDT",
			"SUSHIUSDT",
			"THETAUSDT",
			"TRBUSDT",
			"TRXUSDT",
			"UNFIUSDT",
			"UNIUSDT",
			"VETUSDT",
			"WOOUSDT",
			"XLMUSDT",
			"XMRUSDT",
			"XRPUSDT",
			"YFIUSDT",
			"ZECUSDT",
			"ZENUSDT",
			"ZRXUSDT"
		];

		public Bot() : this("", "")
		{

		}

		public Bot(string name) : this(name, "")
		{

		}
	}
}
