﻿using System.Collections.Generic;

namespace MercuryEditor.Editor
{
    internal class MercuryCompletionDictionary
    {
        public static string[] Data =
        {
            // Basic Keyword
            "asset",
            "period",
            "interval",
            "target",

            // Trading Model
            "scenario",
            "strategy",
            "cue",
            "signal",
            "order",

            // Order
            "long",
            "short",
            "open",
            "close",
            "limit",
            "market",

            // Trade & Position
            "roe",
            "seed",
            "balance",
            "balancesymbol",
            "position",

            // Indicator
            "ma",
            "ema",
            "bb",
            "ri",
            "rsi",
            "macd",
            "hist",
            "upper",
            "lower",
            "sma",

            // Candle
            "candle",
            "high",
            "low",
            "volume",

            // Symbol
            "BTCUSDT",
            "ETHUSDT",
            "BCHUSDT",
            "XRPUSDT",
            "EOSUSDT",
            "LTCUSDT",
            "TRXUSDT",
            "ETCUSDT",
            "XLMUSDT",
            "ADAUSDT",
            "XMRUSDT",
            "DASHUSDT",
            "ZECUSDT",
            "XTZUSDT",
            "BNBUSDT",
            "ATOMUSDT",
            "ONTUSDT",
            "IOTAUSDT",
            "BATUSDT",
            "VETUSDT",
            "NEOUSDT",
            "QTUMUSDT",
            "IOSTUSDT",
            "THETAUSDT",
            "ALGOUSDT",
            "ZILUSDT",
            "KNCUSDT",
            "ZRXUSDT",
            "COMPUSDT",
            "OMGUSDT",
            "DOGEUSDT",
            "SXPUSDT",
            "KAVAUSDT",
            "BANDUSDT",
            "RLCUSDT",
            "WAVESUSDT",
            "MKRUSDT",
            "SNXUSDT",
            "DOTUSDT",
            "DEFIUSDT",
            "YFIUSDT",
            "BALUSDT",
            "CRVUSDT",
            "TRBUSDT",
            "RUNEUSDT",
            "SUSHIUSDT",
            "SRMUSDT",
            "EGLDUSDT",
            "SOLUSDT",
            "ICXUSDT",
            "STORJUSDT",
            "BLZUSDT",
            "UNIUSDT",
            "AVAXUSDT",
            "FTMUSDT",
            "HNTUSDT",
            "ENJUSDT",
            "FLMUSDT",
            "TOMOUSDT",
            "RENUSDT",
            "KSMUSDT",
            "NEARUSDT",
            "AAVEUSDT",
            "FILUSDT",
            "RSRUSDT",
            "LRCUSDT",
            "MATICUSDT",
            "OCEANUSDT",
            "CVCUSDT",
            "BELUSDT",
            "CTKUSDT",
            "AXSUSDT",
            "ALPHAUSDT",
            "ZENUSDT",
            "SKLUSDT",
            "GRTUSDT",
            "CHZUSDT",
            "SANDUSDT",
            "ANKRUSDT",
            "BTSUSDT",
            "LITUSDT",
            "UNFIUSDT",
            "REEFUSDT",
            "RVNUSDT",
            "SFPUSDT",
            "XEMUSDT",
            "BTCSTUSDT",
            "COTIUSDT",
            "CHRUSDT",
            "MANAUSDT",
            "ALICEUSDT",
            "HBARUSDT",
            "ONEUSDT",
            "LINAUSDT",
            "STMXUSDT",
            "DENTUSDT",
            "CELRUSDT",
            "HOTUSDT",
            "MTLUSDT",
            "OGNUSDT",
            "NKNUSDT",
            "SCUSDT",
            "DGBUSDT",
            "BAKEUSDT",
            "GTCUSDT",
            "BTCDOMUSDT",
            "TLMUSDT",
            "IOTXUSDT",
            "AUDIOUSDT",
            "RAYUSDT",
            "C98USDT",
            "MASKUSDT",
            "ATAUSDT",
            "DYDXUSDT",
            "GALAUSDT",
            "CELOUSDT",
            "ARUSDT",
            "KLAYUSDT",
            "ARPAUSDT",
            "CTSIUSDT",
            "LPTUSDT",
            "ENSUSDT",
            "PEOPLEUSDT",
            "ANTUSDT",
            "ROSEUSDT",
            "DUSKUSDT",
            "FLOWUSDT",
            "IMXUSDT",
            "API3USDT",
            "GMTUSDT",
            "APEUSDT",
            "BNXUSDT",
            "WOOUSDT",
            "FTTUSDT",
            "JASMYUSDT",
            "DARUSDT",
            "GALUSDT",
            "OPUSDT",
            "INJUSDT",
            "STGUSDT",
            "FOOTBALLUSDT",
            "SPELLUSDT",
            "LUNA2USDT",
            "LDOUSDT",
            "CVXUSDT",
            "ICPUSDT"
        };

        public static string[] GetSimilarData(string keyword)
        {
            var list = new List<string>();
            foreach (var data in Data)
            {
                var d = data.ToLower();
                if (d.Contains(keyword.ToLower()))
                {
                    list.Add(data);
                }
            }
            return list.ToArray();
        }
    }
}
