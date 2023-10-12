using Albedo.Enums;
using Albedo.Managers;
using Albedo.Mappers;
using Albedo.Utils;
using Albedo.Views;

using Binance.Net.Clients;
using Binance.Net.Objects;

using Bithumb.Net.Clients;
using Bithumb.Net.Enums;

using Bybit.Net.Clients;
using Bybit.Net.Enums;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using Upbit.Net.Clients;

namespace Albedo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// 
    /// 차트 이동 시에 화면 바깥에서 마우스를 놓을 경우 작동이 안되는 버그 수정
    /// 
    /// 라이트/다크 모드(추후)
    /// 현재 캔들 하이라이트 처리(추후)
    /// 
    /// 기능 정리 및 견적 및 사용 매뉴얼 작성
    /// </summary>
    public partial class MainWindow : Window
    {
        int subId = 0;
        BinanceRestClient binanceClient = new();
        BinanceSocketClient binanceSocketClient = new();
        BybitRestClient bybitClient = new();
        BybitSocketClient bybitSocketClient = new();
        BithumbClient bithumbClient = new();
        BithumbSocketClient bithumbSocketClient = new(); // for ticker
        BithumbSocketClient bithumbSocketClient2 = new(); // for transaction
        UpbitClient upbitClient = new();
        DispatcherTimer upbitTimer = new();
        DispatcherTimer upbitCandleTimer = new();

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                InitSettings();

                InitBithumbClient();
                InitUpbitClient();

                InitAction();

                InitBinanceSocketStreams();
                InitBithumbSocketStreams();

                upbitTimer.Interval = TimeSpan.FromSeconds(3);
                upbitCandleTimer.Interval = TimeSpan.FromSeconds(1);
                upbitTimer.Tick += UpbitTimer_Tick;
                upbitCandleTimer.Tick += UpbitCandleTimer_Tick;
                upbitTimer.Start();
                upbitCandleTimer.Start();
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex.ToString());
            }
        }

        #region Window Event
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (Chart.Content is not ChartControl chartControl)
                {
                    return;
                }

                switch (e.Key)
                {
                    case Key.Left:
                        if (chartControl.ViewStartPosition > chartControl.ItemFullWidth)
                        {
                            chartControl.ViewStartPosition -= chartControl.ItemFullWidth;
                            chartControl.ViewEndPosition -= chartControl.ItemFullWidth;
                            chartControl.Render();
                        }
                        break;

                    case Key.Right:
                        if (chartControl.ViewEndPosition + chartControl.ItemFullWidth <= chartControl.ChartWidth)
                        {
                            chartControl.ViewStartPosition += chartControl.ItemFullWidth;
                            chartControl.ViewEndPosition += chartControl.ItemFullWidth;
                            chartControl.Render();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex.ToString());
            }
        }
        #endregion

        #region Timer Event
        private void UpbitTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                TickerMan.UpdateUpbitSpotTicker(upbitClient, Menu);
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex.ToString());
            }
        }

        private void UpbitCandleTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (Chart.Content is not ChartControl chartControl)
                {
                    return;
                }

                if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Favorites)
                {
                    if (Common.Pair.Market == PairMarket.Upbit)
                    {
                        ChartMan.UpdateUpbitSpotChart(upbitClient, chartControl);
                    }
                }
                else if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Upbit)
                {
                    if (Common.Pair.Market == PairMarket.Upbit)
                    {
                        ChartMan.UpdateUpbitSpotChart(upbitClient, chartControl);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex.ToString());
            }
        }
        #endregion

        void InitSettings()
        {
            try
            {
                SettingsMan.Init();

                if (!Directory.Exists("Logs"))
                {
                    Directory.CreateDirectory("Logs");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex.ToString());
            }
        }

        void InitBithumbClient()
        {
            try
            {
                var krwSymbols = bithumbClient.Public.GetAllTickersAsync(BithumbPaymentCurrency.KRW);
                krwSymbols.Wait();
                foreach (var krwSymbol in krwSymbols.Result.data?.coins ?? default!)
                {
                    BithumbSymbolMapper.Add(krwSymbol.currency + "_KRW");
                }
                var btcSymbols = bithumbClient.Public.GetAllTickersAsync(BithumbPaymentCurrency.BTC);
                btcSymbols.Wait();
                foreach (var btcSymbol in btcSymbols.Result.data?.coins ?? default!)
                {
                    BithumbSymbolMapper.Add(btcSymbol.currency + "_BTC");
                }
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex.ToString());
            }
        }

        void InitUpbitClient()
        {
            try
            {
                try
                {
                    // API Key 없으면 안 돌아감
                    upbitClient = new UpbitClient(Settings.Default.UpbitApiKey, Settings.Default.UpbitSecretKey);
                }
                catch
                {
                    MessageBox.Show("업비트 API 오류입니다.\n다시 시도해 주세요.");
                    throw;
                }
                var marketList = upbitClient.QuotationMarketList.GetMarketListAsync();
                marketList.Wait();
                foreach (var market in marketList.Result)
                {
                    UpbitSymbolMapper.Add(market.market, market.korean_name);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex.ToString());
            }
        }

        void InitAction()
        {
            try
            {
                // 차트 새로고침 이벤트
                Common.ChartRefresh = () =>
                {
                    Common.ChartAdditionalComplete = false;

                    switch (Common.CurrentSelectedPairMarket.PairMarket)
                    {
                        case PairMarket.Favorites: // 즐겨찾기
                            switch (Common.Pair.Market)
                            {
                                case PairMarket.Binance:
                                    (var _binanceChartControl, var _binanceNewSubId) = ChartMan.RefreshBinanceChart(binanceClient, binanceSocketClient, subId, Common.Pair.MarketType);
                                    subId = _binanceNewSubId;
                                    Chart.Content = _binanceChartControl;
                                    break;

                                case PairMarket.Bybit:
                                    (var _bybitChartControl, var _bybitNewSubId) = ChartMan.RefreshBybitChart(bybitClient, bybitSocketClient, subId, Common.Pair.MarketType);
                                    subId = _bybitNewSubId;
                                    Chart.Content = _bybitChartControl;
                                    break;

                                case PairMarket.Upbit:
                                    var _upbitChartControl = ChartMan.RefreshUpbitSpotChart(upbitClient);
                                    Chart.Content = _upbitChartControl;
                                    break;

                                case PairMarket.Bithumb:
                                    var _bithumbChartControl = ChartMan.RefreshBithumbSpotChart(bithumbClient, bithumbSocketClient2);
                                    Chart.Content = _bithumbChartControl;
                                    break;
                            }
                            break;

                        case PairMarket.Binance: // 바이낸스
                            (var binanceChartControl, var binanceNewSubId) = ChartMan.RefreshBinanceChart(binanceClient, binanceSocketClient, subId, Common.CurrentSelectedPairMarketType.PairMarketType);
                            subId = binanceNewSubId;
                            Chart.Content = binanceChartControl;
                            break;

                        case PairMarket.Bybit: // 바이비트
                            (var bybitChartControl, var bybitNewSubId) = ChartMan.RefreshBybitChart(bybitClient, bybitSocketClient, subId, Common.CurrentSelectedPairMarketType.PairMarketType);
                            subId = bybitNewSubId;
                            Chart.Content = bybitChartControl;
                            break;

                        case PairMarket.Upbit: // 업비트
                            var upbitChartControl = ChartMan.RefreshUpbitSpotChart(upbitClient);
                            Chart.Content = upbitChartControl;
                            break;

                        case PairMarket.Bithumb: // 빗썸
                            var bithumbChartControl = ChartMan.RefreshBithumbSpotChart(bithumbClient, bithumbSocketClient2);
                            Chart.Content = bithumbChartControl;
                            break;
                    }
                };

                // 차트 추가 로드 이벤트
                Common.ChartAdditionalLoad = () =>
                {
                    if (Chart.Content is not ChartControl chartControl)
                    {
                        return;
                    }

                    if (Common.ChartAdditionalComplete) // 모든 차트가 로드되었을 경우
                    {
                        return;
                    }

                    switch (Common.CurrentSelectedPairMarket.PairMarket)
                    {
                        case PairMarket.Favorites: // 즐겨찾기
                            switch (Common.Pair.Market)
                            {
                                case PairMarket.Binance:
                                    ChartMan.LoadAdditionalBinanceChart(binanceClient, chartControl, Common.Pair.MarketType);
                                    break;

                                case PairMarket.Bybit:
                                    ChartMan.LoadAdditionalBybitChart(bybitClient, chartControl, Common.Pair.MarketType);
                                    break;

                                case PairMarket.Upbit:
                                    ChartMan.LoadAdditionalUpbitSpotChart(upbitClient, chartControl);
                                    break;

                                case PairMarket.Bithumb:
                                    // 빗썸은 차트 추가 로드를 지원하지 않는다.
                                    break;
                            }
                            break;

                        case PairMarket.Binance: // 바이낸스
                            ChartMan.LoadAdditionalBinanceChart(binanceClient, chartControl, Common.CurrentSelectedPairMarketType.PairMarketType);
                            break;

                        case PairMarket.Bybit: // 바이비트
                            ChartMan.LoadAdditionalBybitChart(bybitClient, chartControl, Common.CurrentSelectedPairMarketType.PairMarketType);
                            break;

                        case PairMarket.Upbit: // 업비트
                            ChartMan.LoadAdditionalUpbitSpotChart(upbitClient, chartControl);
                            break;

                        case PairMarket.Bithumb: // 빗썸
                            // 빗썸은 차트 추가 로드를 지원하지 않는다.
                            break;
                    }
                };

                // 코인 정리 이벤트
                Common.ArrangePairs = Menu.viewModel.ArrangePairs;

                // 전체 현재가 최초 로드 이벤트
                Common.RefreshAllTickers = () => TickerMan.InitLoadBithumbSpotTicker(bithumbClient, Menu);
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex.ToString());
            }
        }

        void InitBinanceSocketStreams()
        {
            try
            {
                TickerMan.UpdateBinanceSpotTicker(binanceSocketClient, Menu);
                TickerMan.UpdateBinanceFuturesTicker(binanceSocketClient, Menu);
                TickerMan.UpdateBinanceCoinFuturesTicker(binanceSocketClient, Menu);

                var bybitSpotSymbolResult = bybitClient.V5Api.ExchangeData.GetSpotSymbolsAsync();
                var bybitLinearSymbolResult = bybitClient.V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear);
                var bybitInverseSymbolResult = bybitClient.V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Inverse);
                var bybitOptionSymbolResult = bybitClient.V5Api.ExchangeData.GetOptionSymbolsAsync();
                bybitSpotSymbolResult.Wait();
                bybitLinearSymbolResult.Wait();
                bybitInverseSymbolResult.Wait();
                bybitOptionSymbolResult.Wait();
                var bybitSpotSymbol = bybitSpotSymbolResult.Result.Data.List.Select(x => x.Name);
                var bybitLinearSymbol = bybitLinearSymbolResult.Result.Data.List.Select(x => x.Name);
                var bybitInverseSymbol = bybitInverseSymbolResult.Result.Data.List.Select(x => x.Name);
                var bybitOptionSymbol = bybitOptionSymbolResult.Result.Data.List.Select(x => x.Name);
                TickerMan.UpdateBybitSpotTicker(bybitSocketClient, Menu, bybitSpotSymbol);
                TickerMan.UpdateBybitLinearTicker(bybitSocketClient, Menu, bybitLinearSymbol);
                TickerMan.UpdateBybitInverseTicker(bybitSocketClient, Menu, bybitInverseSymbol);
                TickerMan.UpdateBybitOptionTicker(bybitSocketClient, Menu, bybitOptionSymbol);
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex.ToString());
            }
        }

        void InitBithumbSocketStreams()
        {
            try
            {
                TickerMan.UpdateBithumbSpotTicker(bithumbSocketClient, Menu);
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(MainWindow), MethodBase.GetCurrentMethod()?.Name, ex.ToString());
            }
        }
    }
}
