using Binance.Net.Enums;

using Mercury;

using TradeBot.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Windows.Media;

namespace TradeBot
{
    public class Common
    {
        public static readonly int NullIntValue = -39909;
        public static readonly double NullDoubleValue = -39909;
        public static readonly decimal NullDecimalValue = -39909;

        public static readonly string BinanceApiKeyPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Down("Gaten", "binance_api.txt");

        public static readonly SolidColorBrush OffColor = new(Color.FromRgb(80, 80, 80));
        public static readonly SolidColorBrush WhiteColor = new(Color.FromRgb(222, 222, 222));
        public static readonly SolidColorBrush LongColor = new(Color.FromRgb(14, 203, 129));
        public static readonly SolidColorBrush ShortColor = new(Color.FromRgb(246, 70, 93));
        public static readonly SolidColorBrush MixColor = new(Color.FromRgb(125, 136, 111));

        public static readonly KlineInterval BaseInterval = KlineInterval.OneHour;
        public static readonly int BaseIntervalNumber = 1;

        public static List<SymbolDetail> SymbolDetails = [];

        public static bool IsSound = false;

        public static void LoadSymbolDetail()
        {
            try
            {
                var data = File.ReadAllLines("Resources/symbol_detail.csv");

                SymbolDetails.Clear();
                for (int i = 1; i < data.Length; i++)
                {
                    var d = data[i].Split(',');
                    SymbolDetails.Add(new SymbolDetail
                    {
                        Symbol = d[0],
                        MaxPrice = decimal.Parse(d[3]),
                        MinPrice = decimal.Parse(d[4]),
                        TickSize = decimal.Parse(d[5]),
                        MaxQuantity = decimal.Parse(d[6]),
                        MinQuantity = decimal.Parse(d[7]),
                        StepSize = decimal.Parse(d[8]),
                        PricePrecision = int.Parse(d[9]),
                        QuantityPrecision = int.Parse(d[10])
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(Common), MethodBase.GetCurrentMethod()?.Name, ex);
            }
        }

        public static List<PairQuote> PairQuotes = [];
        public static List<BinancePosition> Positions = [];
        public static List<BinancePosition> LongPositions => Positions.Where(p => p.PositionSide.Equals("Long")).ToList();
        public static List<BinancePosition> ShortPositions => Positions.Where(p => p.PositionSide.Equals("Short")).ToList();
        public static List<BinanceOrder> OpenOrders = [];
        public static Action<string, string> AddHistory = default!;

        public static bool IsPositioning(string symbol, PositionSide side)
        {
            return Positions.Any(p => p.Symbol.Equals(symbol) && p.PositionSide.Equals(side.ToString()));
        }

        public static bool IsLongPositioning(string symbol)
        {
            return LongPositions.Any(p => p.Symbol.Equals(symbol));
        }

        public static bool IsShortPositioning(string symbol)
        {
            return ShortPositions.Any(p => p.Symbol.Equals(symbol));
        }

        public static BinancePosition? GetPosition(string symbol, PositionSide side)
        {
            return Positions.Find(p => p.Symbol.Equals(symbol) && p.PositionSide.Equals(side.ToString()));
        }

        public static BinanceOrder? GetOrder(string symbol, PositionSide side, FuturesOrderType type)
        {
            return OpenOrders.Find(o => o.Symbol.Equals(symbol) && o.Side.Equals(side) && o.Type.Equals(type));
        }

        public static readonly int PositionCoolTimeSeconds = 60;
        public static List<PositionCoolTime> PositionCoolTimes = new ();
        public static PositionCoolTime? GetPositionCoolTime(string symbol, PositionSide side)
        {
            return PositionCoolTimes.Find(p => p.Symbol.Equals(symbol) && p.Side.Equals(side));
        }

        public static void AddPositionCoolTime(string symbol, PositionSide side)
        {
            var positionCoolTime = GetPositionCoolTime(symbol, side);
            if(positionCoolTime == null)
            {
                PositionCoolTimes.Add(new PositionCoolTime(symbol, side, DateTime.Now));
            }
            else
            {
                positionCoolTime.LatestEntryTime = DateTime.Now;
            }
        }

        public static bool IsCoolTime(string symbol, PositionSide side)
        {
            var positionCoolTime = GetPositionCoolTime(symbol, side);
            return positionCoolTime == null ? false : positionCoolTime.IsCoolTime();
        }
    }
}
