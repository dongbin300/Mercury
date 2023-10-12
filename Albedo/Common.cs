using Albedo.Enums;
using Albedo.Models;

using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace Albedo
{
    public class Common
    {
        public static readonly decimal NullValue = -39909;

        public static readonly int ChartLoadLimit = 600;
        public static readonly int ChartUpbitLoadLimit = 200;

        public static readonly int ChartItemFullWidth = 100;
        public static readonly float ChartItemMarginPercent = 0.2f;

        public static readonly int CandleTopBottomMargin = 30;
        public static readonly int VolumeTopBottomMargin = 5;

        public static PairMarket SupportedMarket = PairMarket.Favorites | PairMarket.Binance | PairMarket.Upbit | PairMarket.Bithumb;

        public static PairMarketModel CurrentSelectedPairMarket = new(PairMarket.None, "", "");
        public static PairMarketTypeModel CurrentSelectedPairMarketType = new(PairMarketType.None, "");
        public static PairQuoteAssetModel CurrentSelectedPairQuoteAsset = new(PairQuoteAsset.None, "");

        public static Pair Pair = default!;
        public static CandleInterval ChartInterval = CandleInterval.OneMinute;
        public static Action ChartRefresh = default!;
        public static Action ChartAdditionalLoad = default!;
        public static Action ArrangePairs = default!;
        public static Action RefreshAllTickers = default!;
        public static Action CalculateIndicators = default!;

        public static bool ChartAdditionalComplete = false;

        public static string CurrentSettingsMenu = "P1";

        public static List<MaTypeModel> MaTypes = new()
        {
            new MaTypeModel(MaType.Sma, "단순"),
            new MaTypeModel(MaType.Wma, "가중"),
            new MaTypeModel(MaType.Ema, "지수")
        };

        public static List<LineColorModel> MaLineColors = new()
        {
            new LineColorModel(LineColor.Red, new SolidColorBrush(Color.FromRgb(255, 0, 0))),
            new LineColorModel(LineColor.Green, new SolidColorBrush(Color.FromRgb(0, 255, 0))),
            new LineColorModel(LineColor.Blue, new SolidColorBrush(Color.FromRgb(0, 0, 255))),
            new LineColorModel(LineColor.Yellow, new SolidColorBrush(Color.FromRgb(255, 255, 0))),
            new LineColorModel(LineColor.Magenta, new SolidColorBrush(Color.FromRgb(255, 0, 255))),
            new LineColorModel(LineColor.Cyan, new SolidColorBrush(Color.FromRgb(0, 255, 255))),
            new LineColorModel(LineColor.Orange, new SolidColorBrush(Color.FromRgb(255, 165, 0))),
            new LineColorModel(LineColor.Purple, new SolidColorBrush(Color.FromRgb(128, 0, 128))),
            new LineColorModel(LineColor.Teal, new SolidColorBrush(Color.FromRgb(0, 128, 128))),
            new LineColorModel(LineColor.Maroon, new SolidColorBrush(Color.FromRgb(128, 0, 0))),
            new LineColorModel(LineColor.Olive, new SolidColorBrush(Color.FromRgb(0, 128, 0))),
            new LineColorModel(LineColor.Navy, new SolidColorBrush(Color.FromRgb(0, 0, 128))),
            new LineColorModel(LineColor.OliveGreen, new SolidColorBrush(Color.FromRgb(128, 128, 0))),
            new LineColorModel(LineColor.Gray, new SolidColorBrush(Color.FromRgb(128, 128, 128))),
            new LineColorModel(LineColor.Silver, new SolidColorBrush(Color.FromRgb(192, 192, 192))),
            new LineColorModel(LineColor.White, new SolidColorBrush(Color.FromRgb(255, 255, 255))),
            new LineColorModel(LineColor.Black, new SolidColorBrush(Color.FromRgb(0, 0, 0))),
            new LineColorModel(LineColor.Pink, new SolidColorBrush(Color.FromRgb(255, 192, 203))),
            new LineColorModel(LineColor.Peach, new SolidColorBrush(Color.FromRgb(255, 218, 185))),
            new LineColorModel(LineColor.Lime, new SolidColorBrush(Color.FromRgb(0, 255, 127))),
            new LineColorModel(LineColor.DeepPink, new SolidColorBrush(Color.FromRgb(255, 20, 147))),
            new LineColorModel(LineColor.GreenYellow, new SolidColorBrush(Color.FromRgb(173, 255, 47))),
            new LineColorModel(LineColor.LightYellow, new SolidColorBrush(Color.FromRgb(255, 255, 224))),
            new LineColorModel(LineColor.Moccasin, new SolidColorBrush(Color.FromRgb(255, 228, 181))),
            new LineColorModel(LineColor.NavajoWhite, new SolidColorBrush(Color.FromRgb(255, 222, 173))),
            new LineColorModel(LineColor.SandyBrown, new SolidColorBrush(Color.FromRgb(244, 164, 96))),
            new LineColorModel(LineColor.OrangeRed, new SolidColorBrush(Color.FromRgb(255, 69, 0))),
            new LineColorModel(LineColor.Orchid, new SolidColorBrush(Color.FromRgb(218, 112, 214))),
            new LineColorModel(LineColor.Khaki, new SolidColorBrush(Color.FromRgb(240, 230, 140))),
            new LineColorModel(LineColor.CornflowerBlue, new SolidColorBrush(Color.FromRgb(100, 149, 237)))
        };

        public static List<LineWeightModel> MaLineWeights = new()
        {
            new LineWeightModel(LineWeight.Px1, "1px"),
            new LineWeightModel(LineWeight.Px2, "2px"),
            new LineWeightModel(LineWeight.Px3, "3px"),
            new LineWeightModel(LineWeight.Px4, "4px"),
            new LineWeightModel(LineWeight.Px5, "5px"),
            new LineWeightModel(LineWeight.Px6, "6px")
        };
    }
}
