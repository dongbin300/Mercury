using Albedo.Enums;
using Albedo.Models;

using Newtonsoft.Json;

using System.Collections.Generic;

namespace Albedo.Managers
{
    public class SettingsMan
    {
        public static string BinanceApiKey { get; set; } = string.Empty;
        public static string BinanceSecretKey { get; set; } = string.Empty;
        public static string UpbitApiKey { get; set; } = string.Empty;
        public static string UpbitSecretKey { get; set; } = string.Empty;
        public static string BithumbApiKey { get; set; } = string.Empty;
        public static string BithumbSecretKey { get; set; } = string.Empty;
        public static int SimpleListCount { get; set; }
        public static int DefaultCandleCount { get; set; }
        public static IndicatorsModel Indicators { get; set; } = default!;
        public static List<string> FavoritesList { get; set; } = default!;

        public SettingsMan()
        {

        }

        public static void Init()
        {
            Common.ChartInterval = Settings.Default.Interval switch
            {
                "1분" => CandleInterval.OneMinute,
                "3분" => CandleInterval.ThreeMinutes,
                "5분" => CandleInterval.FiveMinutes,
                "10분" => CandleInterval.TenMinutes,
                "15분" => CandleInterval.FifteenMinutes,
                "30분" => CandleInterval.ThirtyMinutes,
                "1시간" => CandleInterval.OneHour,
                "1일" => CandleInterval.OneDay,
                "1주" => CandleInterval.OneWeek,
                "1월" => CandleInterval.OneMonth,
                _ => CandleInterval.OneMinute,
            };

            Load();
        }

        public static void Load()
        {
            //BinanceApiKey = Settings.Default.BinanceApiKey;
            //BinanceSecretKey = Settings.Default.BinanceSecretKey;
            UpbitApiKey = Settings.Default.UpbitApiKey;
            UpbitSecretKey = Settings.Default.UpbitSecretKey;
            //BithumbApiKey = Settings.Default.BithumbApiKey;
            //BithumbSecretKey = Settings.Default.BithumbSecretKey;
            SimpleListCount = Settings.Default.DefaultPairCount;
            DefaultCandleCount = Settings.Default.DefaultCandleCount;
            Indicators = JsonConvert.DeserializeObject<IndicatorsModel>(Settings.Default.IndicatorString) ?? new IndicatorsModel();
            FavoritesList = JsonConvert.DeserializeObject<List<string>>(Settings.Default.FavoritesString) ?? new List<string>();
        }

        public static void Save()
        {
            //Settings.Default.BinanceApiKey = BinanceApiKey;
            //Settings.Default.BinanceSecretKey = BinanceSecretKey;
            Settings.Default.UpbitApiKey = UpbitApiKey;
            Settings.Default.UpbitSecretKey = UpbitSecretKey;
            //Settings.Default.BithumbApiKey = BithumbApiKey;
            //Settings.Default.BithumbSecretKey = BithumbSecretKey;
            Settings.Default.DefaultPairCount = SimpleListCount;
            Settings.Default.DefaultCandleCount = DefaultCandleCount;
            Settings.Default.IndicatorString = JsonConvert.SerializeObject(Indicators);
            Settings.Default.FavoritesString = JsonConvert.SerializeObject(FavoritesList);
            Settings.Default.Save();
        }
    }
}
