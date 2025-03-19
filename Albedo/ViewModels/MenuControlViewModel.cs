using Albedo.Commands;
using Albedo.Enums;
using Albedo.Models;
using Albedo.Views;

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Albedo.ViewModels
{
    public class MenuControlViewModel : INotifyPropertyChanged
    {
        #region Notify Property Changed
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion Notify Property Changed

        private ObservableCollection<PairControl> pairControls = [];
        public ObservableCollection<PairControl> PairControls
        {
            get => pairControls;
            set
            {
                pairControls = value;
                OnPropertyChanged(nameof(PairControls));
            }
        }
		private ObservableCollection<PairControl> resultPairControls = [];
        public ObservableCollection<PairControl> ResultPairControls
        {
            get => resultPairControls;
            set
            {
                resultPairControls = value;
                OnPropertyChanged(nameof(ResultPairControls));
            }
        }
        private ObservableCollection<PairControl> simplePairControls = [];
        public ObservableCollection<PairControl> SimplePairControls
        {
            get => simplePairControls;
            set
            {
                simplePairControls = value;
                OnPropertyChanged(nameof(SimplePairControls));
            }
        }
        private ObservableCollection<PairMarketModel> pairMarkets = [];
        public ObservableCollection<PairMarketModel> PairMarkets
        {
            get => pairMarkets;
            set
            {
                pairMarkets = value;
                OnPropertyChanged(nameof(PairMarkets));
            }
        }
        private ObservableCollection<PairMarketTypeModel> pairMarketTypes = [];
        public ObservableCollection<PairMarketTypeModel> PairMarketTypes
        {
            get => pairMarketTypes;
            set
            {
                pairMarketTypes = value;
                OnPropertyChanged(nameof(PairMarketTypes));
            }
        }
        private ObservableCollection<PairQuoteAssetModel> pairQuoteAssets = [];
        public ObservableCollection<PairQuoteAssetModel> PairQuoteAssets
        {
            get => pairQuoteAssets;
            set
            {
                pairQuoteAssets = value;
                OnPropertyChanged(nameof(PairQuoteAssets));
            }
        }
        private int selectedPairMarketIndex = 0;
        public int SelectedPairMarketIndex
        {
            get => selectedPairMarketIndex;
            set
            {
                selectedPairMarketIndex = value;
                OnPropertyChanged(nameof(SelectedPairMarketIndex));
            }
        }
        private int selectedPairMarketTypeIndex = 0;
        public int SelectedPairMarketTypeIndex
        {
            get => selectedPairMarketTypeIndex;
            set
            {
                selectedPairMarketTypeIndex = value;
                OnPropertyChanged(nameof(SelectedPairMarketTypeIndex));
            }
        }
        private int selectedPairQuoteAssetIndex = 0;
        public int SelectedPairQuoteAssetIndex
        {
            get => selectedPairQuoteAssetIndex;
            set
            {
                selectedPairQuoteAssetIndex = value;
                OnPropertyChanged(nameof(SelectedPairQuoteAssetIndex));
            }
        }
        private string keywordText = string.Empty;
        public string KeywordText
        {
            get => keywordText;
            set
            {
                keywordText = value;
                OnPropertyChanged(nameof(KeywordText));
                Common.ArrangePairs();
            }
        }
        private bool isSimpleList = false;
        public bool IsSimpleList
        {
            get => isSimpleList;
            set
            {
                isSimpleList = value;
                OnPropertyChanged(nameof(IsSimpleList));
                OnPropertyChanged(nameof(PairListBoxRowSpan));
            }
        }
        private bool isAllListView = false;
        public bool IsAllListView
        {
            get => isAllListView;
            set
            {
                isAllListView = value;
                OnPropertyChanged(nameof(IsAllListView));
            }
        }
        private PairSortType changeSortType = PairSortType.None;
        public PairSortType ChangeSortType
        {
            get => changeSortType;
            set
            {
                changeSortType = value;
                OnPropertyChanged(nameof(ChangeSortType));
                OnPropertyChanged(nameof(ChangeSortImage));
                Common.ArrangePairs();
            }
        }
        private PairSortType azSortType = PairSortType.None;
        public PairSortType AzSortType
        {
            get => azSortType;
            set
            {
                azSortType = value;
                OnPropertyChanged(nameof(AzSortType));
                OnPropertyChanged(nameof(AzSortImage));
                Common.ArrangePairs();
            }
        }
        public BitmapImage ChangeSortImage => new(new Uri("pack://application:,,,/Albedo;component/Resources/" + ChangeSortType switch
        {
            PairSortType.Asc => "sort-asc.png",
            PairSortType.Desc => "sort-desc.png",
            _ => "sort.png",
        }));
        public BitmapImage AzSortImage => new(new Uri("pack://application:,,,/Albedo;component/Resources/" + AzSortType switch
        {
            PairSortType.Asc => "az-a.png",
            PairSortType.Desc => "az-z.png",
            _ => "az.png",
        }));
        public BitmapImage SettingsImage => new(new Uri("pack://application:,,,/Albedo;component/Resources/" + "setting.png"));
        public int PairListBoxRowSpan => IsSimpleList ? 1 : 2;

        public ICommand? PairMarketSelectionChanged { get; set; }
        public ICommand? PairMarketTypeSelectionChanged { get; set; }
        public ICommand? PairQuoteAssetSelectionChanged { get; set; }
        public ICommand? PairSelectionChanged { get; set; }
        public ICommand? ChangeSortClick { get; set; }
        public ICommand? AzSortClick { get; set; }
        public ICommand? SettingsClick { get; set; }
        public ICommand? AllListViewClick { get; set; }

        public MenuControlViewModel()
        {
            InitEvent();
            InitMarket();
            InitMarketType();
            InitQuoteAsset();
        }

        /// <summary>
        /// 이벤트 초기화
        /// </summary>
        private void InitEvent()
        {
            // 거래소 변경 이벤트
            PairMarketSelectionChanged = new DelegateCommand((obj) =>
            {
                if (obj is not PairMarketModel market)
                {
                    return;
                }

                Common.CurrentSelectedPairMarket = market;
                PairControls.Clear();
                InitMarketType();
                IsAllListView = false;
            });

            // 타입 변경 이벤트
            PairMarketTypeSelectionChanged = new DelegateCommand((obj) =>
            {
                if (obj is not PairMarketTypeModel marketType)
                {
                    return;
                }

                Common.CurrentSelectedPairMarketType = marketType;
                PairControls.Clear();
                InitQuoteAsset();
                IsAllListView = false;
            });

            // 거래자산 변경 이벤트
            PairQuoteAssetSelectionChanged = new DelegateCommand((obj) =>
            {
                if (obj is not PairQuoteAssetModel quoteAsset)
                {
                    return;
                }

                Common.CurrentSelectedPairQuoteAsset = quoteAsset;
                PairControls.Clear();
                Common.RefreshAllTickers?.Invoke();
                IsAllListView = false;
            });

            // 코인 선택 변경 이벤트
            PairSelectionChanged = new DelegateCommand((obj) =>
            {
                if (obj is not PairControl pairControl)
                {
                    return;
                }

                Common.Pair = pairControl.Pair;
                Common.ChartRefresh();
            });

            // 등락률 정렬 이벤트
            ChangeSortClick = new DelegateCommand((obj) =>
            {
                AzSortType = PairSortType.None;
                ChangeSortType = ChangeSortType switch
                {
                    PairSortType.None => PairSortType.Asc,
                    PairSortType.Asc => PairSortType.Desc,
                    PairSortType.Desc => PairSortType.None,
                    _ => PairSortType.None
                };
            });

            // A-Z 정렬 이벤트
            AzSortClick = new DelegateCommand((obj) =>
            {
                ChangeSortType = PairSortType.None;
                AzSortType = AzSortType switch
                {
                    PairSortType.None => PairSortType.Asc,
                    PairSortType.Asc => PairSortType.Desc,
                    PairSortType.Desc => PairSortType.None,
                    _ => PairSortType.None
                };
            });

            // 설정 클릭 이벤트
            SettingsClick = new DelegateCommand((obj) =>
            {
                var settingsView = new SettingsView();
                settingsView.ShowDialog();
            });

            // 모든 리스트 보기 클릭 이벤트
            AllListViewClick = new DelegateCommand((obj) =>
            {
                IsAllListView = true;
                ArrangePairs();
            });
        }

        /// <summary>
        /// 거래소 초기화
        /// </summary>
        private void InitMarket()
        {
            PairMarkets.Clear();
            if (Common.SupportedMarket.HasFlag(PairMarket.Favorites))
            {
                PairMarkets.Add(new PairMarketModel(PairMarket.Favorites, "즐겨찾기", "Resources/favorites-on.png"));
            }
            if (Common.SupportedMarket.HasFlag(PairMarket.Binance))
            {
                PairMarkets.Add(new PairMarketModel(PairMarket.Binance, "바이낸스", "Resources/binance.png"));
            }
            if (Common.SupportedMarket.HasFlag(PairMarket.Upbit))
            {
                PairMarkets.Add(new PairMarketModel(PairMarket.Upbit, "업비트", "Resources/upbit.png"));
            }
            if (Common.SupportedMarket.HasFlag(PairMarket.Bithumb))
            {
                PairMarkets.Add(new PairMarketModel(PairMarket.Bithumb, "빗썸", "Resources/bithumb.png"));
            }
            if (Common.SupportedMarket.HasFlag(PairMarket.Bybit))
            {
                //PairMarkets.Add(new PairMarketModel(PairMarket.Bybit, "바이비트", "Resources/bybit.png"));
            }
            if (PairMarkets.Count > 0)
            {
                SelectedPairMarketIndex = 0;
                PairMarketSelectionChanged?.Execute(PairMarkets[SelectedPairMarketIndex]);
            }
            else
            {
                SelectedPairMarketIndex = -1;
            }
        }

        /// <summary>
        /// 거래소 타입 초기화
        /// </summary>
        private void InitMarketType()
        {
            PairMarketTypes.Clear();
            switch (Common.CurrentSelectedPairMarket.PairMarket)
            {
                case PairMarket.Binance:
                    PairMarketTypes.Add(new PairMarketTypeModel(PairMarketType.Spot, "현물"));
                    PairMarketTypes.Add(new PairMarketTypeModel(PairMarketType.Futures, "선물"));
                    PairMarketTypes.Add(new PairMarketTypeModel(PairMarketType.CoinFutures, "코인 선물"));
                    break;

                case PairMarket.Bybit:
                    PairMarketTypes.Add(new PairMarketTypeModel(PairMarketType.Spot, "현물"));
                    PairMarketTypes.Add(new PairMarketTypeModel(PairMarketType.Linear, "선물"));
                    PairMarketTypes.Add(new PairMarketTypeModel(PairMarketType.Inverse, "선물 인버스"));
                    PairMarketTypes.Add(new PairMarketTypeModel(PairMarketType.Option, "옵션"));
                    break;

                case PairMarket.Upbit:
                case PairMarket.Bithumb:
                    PairMarketTypes.Add(new PairMarketTypeModel(PairMarketType.Spot, "현물"));
                    break;

                case PairMarket.Favorites:
                    PairQuoteAssets.Clear();
                    Common.RefreshAllTickers?.Invoke();
                    break;
            }
            if (PairMarketTypes.Count > 0)
            {
                SelectedPairMarketTypeIndex = 0;
                PairMarketTypeSelectionChanged?.Execute(PairMarketTypes[SelectedPairMarketTypeIndex]);
            }
            else
            {
                SelectedPairMarketTypeIndex = -1;
            }
        }

        /// <summary>
        /// 거래자산 초기화
        /// </summary>
        private void InitQuoteAsset()
        {
            PairQuoteAssets.Clear();
            switch (Common.CurrentSelectedPairMarket.PairMarket)
            {
                case PairMarket.Binance:
                    switch (Common.CurrentSelectedPairMarketType.PairMarketType)
                    {
                        case PairMarketType.Spot:
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.USDT, "USDT"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.TUSD, "TUSD"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.BUSD, "BUSD"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.BNB, "BNB"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.BTC, "BTC"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.ETH, "ETH"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.DAI, "DAI"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.USDC, "USDC"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.VAI, "VAI"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.XRP, "XRP"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.TRX, "TRX"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.DOGE, "DOGE"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.DOT, "DOT"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.AUD, "AUD"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.BIDR, "BIDR"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.BRL, "BRL"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.EUR, "EUR"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.GBP, "GBP"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.RUB, "RUB"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.TRY, "TRY"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.UAH, "UAH"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.ZAR, "ZAR"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.IDRT, "IDRT"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.NGN, "NGN"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.PLN, "PLN"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.RON, "RON"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.ARS, "ARS"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.USD, "USD"));
                            break;

                        case PairMarketType.Futures:
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.USDT, "USDT"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.BUSD, "BUSD"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.BTC, "BTC"));
                            break;

                        case PairMarketType.CoinFutures:
                            break;
                    }
                    break;

                case PairMarket.Bybit:
                    switch (Common.CurrentSelectedPairMarketType.PairMarketType)
                    {
                        case PairMarketType.Spot:
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.USDT, "USDT"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.USDC, "USDC"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.BTC, "BTC"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.DAI, "DAI"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.EUR, "EUR"));
                            break;

                        case PairMarketType.Linear:
                            break;

                        case PairMarketType.Inverse:
                            break;

                        case PairMarketType.Option:
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.BTC, "BTC"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.ETH, "ETH"));
                            PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.SOL, "SOL"));
                            break;
                    }
                    break;

                case PairMarket.Upbit:
                    PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.KRW, "KRW"));
                    PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.BTC, "BTC"));
                    PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.USDT, "USDT"));
                    break;

                case PairMarket.Bithumb:
                    PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.KRW, "KRW"));
                    PairQuoteAssets.Add(new PairQuoteAssetModel(PairQuoteAsset.BTC, "BTC"));
                    break;

                default:
                    break;
            }
            if (PairQuoteAssets.Count > 0)
            {
                SelectedPairQuoteAssetIndex = 0;
                PairQuoteAssetSelectionChanged?.Execute(pairQuoteAssets[SelectedPairQuoteAssetIndex]);
            }
            else
            {
                SelectedPairQuoteAssetIndex = -1;
            }
        }

        /// <summary>
        /// 코인 정리
        /// </summary>
        public void ArrangePairs()
        {
            /* Searching */
            ResultPairControls = string.IsNullOrEmpty(keywordText)
                ? new ObservableCollection<PairControl>(PairControls)
                : new ObservableCollection<PairControl>(PairControls.Where(p => p.Pair.SymbolKorean.Contains(keywordText)));

            /* Sorting */
            ResultPairControls = ChangeSortType switch
            {
                PairSortType.Asc => new ObservableCollection<PairControl>(ResultPairControls.OrderByDescending(p => p.Pair.PriceChangePercent)),
                PairSortType.Desc => new ObservableCollection<PairControl>(ResultPairControls.OrderBy(p => p.Pair.PriceChangePercent)),
                _ => new ObservableCollection<PairControl>(ResultPairControls)
            };
            ResultPairControls = AzSortType switch
            {
                PairSortType.Asc => new ObservableCollection<PairControl>(ResultPairControls.OrderBy(p => p.Pair.SymbolKorean)),
                PairSortType.Desc => new ObservableCollection<PairControl>(ResultPairControls.OrderByDescending(p => p.Pair.SymbolKorean)),
                _ => new ObservableCollection<PairControl>(ResultPairControls)
            };

            /* Simplify */
            if (!IsAllListView && ResultPairControls.Count > Settings.Default.DefaultPairCount)
            {
                SimplePairControls = new ObservableCollection<PairControl>(ResultPairControls.Take(Settings.Default.DefaultPairCount));
                IsSimpleList = true;
            }
            else
            {
                SimplePairControls = new ObservableCollection<PairControl>(ResultPairControls);
                IsSimpleList = false;
            }

        }

        /// <summary>
        /// 코인 정보(이름, 가격, 등락률) 업데이트
        /// </summary>
        /// <param name="pair"></param>
        public void UpdatePairInfo(Pair pair)
        {
            if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Favorites)
            {
                if (!pair.IsFavorites)
                {
                    return;
                }
            }

            var pairTag = $"{pair.Market}_{pair.MarketType}_{pair.Symbol}";
            var _pair = PairControls.Where(p => p.Tag.Equals(pairTag));

            if (_pair == null || !_pair.Any())
            {
                var pairControl = new PairControl();
                pairControl.Init(pair);
                PairControls.Add(pairControl);
                return;
            }

            _pair.ElementAt(0).Pair.Price = pair.Price;
            _pair.ElementAt(0).Pair.PriceChangePercent = pair.PriceChangePercent;
        }
    }
}
