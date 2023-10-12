using Albedo.Enums;
using Albedo.Managers;
using Albedo.Mappers;
using Albedo.Utils;

using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace Albedo.Models
{
    public class Pair : INotifyPropertyChanged
    {
        #region Notify Property Changed
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion Notify Property Changed

        public PairMarket Market { get; set; }
        public PairMarketType MarketType { get; set; }
        public PairQuoteAsset QuoteAsset { get; set; }
        public string Symbol { get; set; } = string.Empty;
        private decimal price = 0;
        public decimal Price
        {
            get => price;
            set
            {
                price = value;
                OnPropertyChanged(nameof(Price));
                OnPropertyChanged(nameof(PriceString));
            }
        }
        private decimal priceChangePercent = 0;
        public decimal PriceChangePercent
        {
            get => priceChangePercent;
            set
            {
                priceChangePercent = value;
                OnPropertyChanged(nameof(PriceChangePercent));
                OnPropertyChanged(nameof(PriceChangePercentString));
            }
        }
        public bool IsRendered { get; set; }
        public bool IsSelected { get; set; }

        public string Id => Market + "_" + MarketType + "_" + Symbol;
        public string MarketKorean => Market switch
        {
            PairMarket.Binance => "바이낸스",
            PairMarket.Bybit => "바이비트",
            PairMarket.Upbit => "업비트",
            PairMarket.Bithumb => "빗썸",
            _ => ""
        };
        public string MarketTypeKorean => MarketType switch
        {
            PairMarketType.Spot => "현물",
            PairMarketType.Futures => "선물",
            PairMarketType.CoinFutures => "코인 선물",
            PairMarketType.Linear => "선물",
            PairMarketType.Inverse => "선물 인버스",
            PairMarketType.Option => "옵션",
            _ => ""
        };
        public string SymbolKorean => Market switch
        {
            PairMarket.Binance => Symbol,
            PairMarket.Bybit => Symbol,
            PairMarket.Upbit => UpbitSymbolMapper.GetKoreanName(Symbol),
            PairMarket.Bithumb => BithumbSymbolMapper.GetKoreanName(Symbol),
            _ => Symbol
        };
        public string PriceString => NumberUtil.ToRoundedValueString(Price) + " " + QuoteAsset;
        public string PriceChangePercentString => Math.Round(PriceChangePercent, 2) + "%";
        public BitmapImage MarketIcon => new (new Uri("pack://application:,,,/Albedo;component/Resources/" + Market switch
        {
            PairMarket.Binance => "binance.png",
            PairMarket.Bybit => "bybit.png",
            PairMarket.Upbit => "upbit.png",
            PairMarket.Bithumb => "bithumb.png",
            _ => ""
        }));
        public bool IsBullish => PriceChangePercent >= 0;
        public bool IsFavorites => SettingsMan.FavoritesList.Contains(Id);
        public BitmapImage FavoritesImage => new(new Uri("pack://application:,,,/Albedo;component/Resources/" + (IsFavorites ? "favorites-on.png" : "favorites-off.png")));

        public Pair(PairMarket market, PairMarketType marketType, PairQuoteAsset quoteAsset, string symbol, decimal price, decimal priceChangePercent)
        {
            if (market == PairMarket.None || marketType == PairMarketType.None)
            {
                return;
            }

            Market = market;
            MarketType = marketType;
            QuoteAsset = quoteAsset;
            Symbol = symbol;
            Price = price;
            PriceChangePercent = priceChangePercent;
        }
    }
}
