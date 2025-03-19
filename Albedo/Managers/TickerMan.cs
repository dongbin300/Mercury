using Albedo.Enums;
using Albedo.Extensions;
using Albedo.Mappers;
using Albedo.Models;
using Albedo.Utils;
using Albedo.Views;

using Binance.Net.Clients;

using Bithumb.Net.Clients;
using Bithumb.Net.Enums;

using Bybit.Net.Clients;

using System.Collections.Generic;

using Upbit.Net.Clients;

namespace Albedo.Managers
{
    public class TickerMan
    {
        #region Binance
        public static void UpdateBinanceSpotTicker(BinanceSocketClient client, MenuControl menu)
        {
            client.SpotApi.ExchangeData.SubscribeToAllTickerUpdatesAsync((obj) =>
            {
                if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Favorites) // 즐겨찾기
                {
                    foreach (var item in obj.Data)
                    {
                        var quoteAsset = BinanceSymbolMapper.GetPairQuoteAsset(item.Symbol);
                        var pairId = $"Binance_Spot_{item.Symbol}";
                        if (SettingsMan.FavoritesList.Contains(pairId))
                        {
                            DispatcherService.Invoke(() =>
                            {
                                menu.viewModel.UpdatePairInfo(new Pair(
                               PairMarket.Binance,
                               PairMarketType.Spot,
                               quoteAsset,
                               item.Symbol, item.LastPrice, item.PriceChangePercent));
                            });
                        }
                    }
                    Common.ArrangePairs();
                }
                else if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Binance && Common.CurrentSelectedPairMarketType.PairMarketType == PairMarketType.Spot) // 바이낸스 현물
                {
                    foreach (var item in obj.Data)
                    {
                        var quoteAsset = BinanceSymbolMapper.GetPairQuoteAsset(item.Symbol);
                        if (quoteAsset == Common.CurrentSelectedPairQuoteAsset.PairQuoteAsset)
                        {
                            DispatcherService.Invoke(() =>
                            {
                                menu.viewModel.UpdatePairInfo(new Pair(
                                PairMarket.Binance,
                                PairMarketType.Spot,
                                quoteAsset,
                                item.Symbol, item.LastPrice, item.PriceChangePercent));
                            });
                        }
                    }
                    Common.ArrangePairs();
                }
            });
        }
        public static void UpdateBinanceFuturesTicker(BinanceSocketClient client, MenuControl menu)
        {
            client.UsdFuturesApi.ExchangeData.SubscribeToAllTickerUpdatesAsync((obj) =>
            {
                if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Favorites) // 즐겨찾기
                {
                    foreach (var item in obj.Data)
                    {
                        var quoteAsset = BinanceSymbolMapper.GetPairQuoteAsset(item.Symbol);
                        var pairId = $"Binance_Futures_{item.Symbol}";
                        if (SettingsMan.FavoritesList.Contains(pairId))
                        {
                            DispatcherService.Invoke(() =>
                            {
                                menu.viewModel.UpdatePairInfo(new Pair(
                                    PairMarket.Binance,
                                    PairMarketType.Futures,
                                    quoteAsset,
                                    item.Symbol, item.LastPrice, item.PriceChangePercent));
                            });
                        }
                    }
                    Common.ArrangePairs();
                }
                else if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Binance && Common.CurrentSelectedPairMarketType.PairMarketType == PairMarketType.Futures) // 바이낸스 선물
                {
                    foreach (var item in obj.Data)
                    {
                        var quoteAsset = BinanceSymbolMapper.GetPairQuoteAsset(item.Symbol);
                        if (quoteAsset == Common.CurrentSelectedPairQuoteAsset.PairQuoteAsset)
                        {
                            DispatcherService.Invoke(() =>
                            {
                                menu.viewModel.UpdatePairInfo(new Pair(
                               PairMarket.Binance,
                               PairMarketType.Futures,
                               quoteAsset,
                               item.Symbol, item.LastPrice, item.PriceChangePercent));
                            });
                        }
                    }
                    Common.ArrangePairs();
                }
            });
        }
        public static void UpdateBinanceCoinFuturesTicker(BinanceSocketClient client, MenuControl menu)
        {
            client.CoinFuturesApi.SubscribeToAllTickerUpdatesAsync((obj) =>
            {
                if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Favorites) // 즐겨찾기
                {
                    foreach (var item in obj.Data)
                    {
                        var quoteAsset = BinanceSymbolMapper.GetPairQuoteAsset(item.Symbol);
                        var pairId = $"Binance_CoinFutures_{item.Symbol}";
                        if (SettingsMan.FavoritesList.Contains(pairId))
                        {
                            DispatcherService.Invoke(() =>
                            {
                                menu.viewModel.UpdatePairInfo(new Pair(
                                    PairMarket.Binance,
                                    PairMarketType.CoinFutures,
                                    PairQuoteAsset.USDT,
                                    item.Symbol, item.LastPrice, item.PriceChangePercent));
                            });
                        }
                    }
                    Common.ArrangePairs();
                }
                else if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Binance && Common.CurrentSelectedPairMarketType.PairMarketType == PairMarketType.CoinFutures) // 바이낸스 코인선물
                {
                    foreach (var item in obj.Data)
                    {
                        DispatcherService.Invoke(() =>
                        {
                            menu.viewModel.UpdatePairInfo(new Pair(
                            PairMarket.Binance,
                            PairMarketType.CoinFutures,
                            PairQuoteAsset.USDT,
                            item.Symbol, item.LastPrice, item.PriceChangePercent));
                        });
                    }
                    Common.ArrangePairs();
                }
            });
        }
        #endregion

        #region Bybit
        public static void UpdateBybitSpotTicker(BybitSocketClient client, MenuControl menu, IEnumerable<string> symbols)
        {
            client.V5SpotApi.SubscribeToTickerUpdatesAsync(symbols, (obj) =>
            {
                var item = obj.Data;
                var quoteAsset = BybitSymbolMapper.GetPairQuoteAsset(item.Symbol);

                if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Favorites) // 즐겨찾기
                {
                    var pairId = $"Bybit_Spot_{item.Symbol}";
                    if (SettingsMan.FavoritesList.Contains(pairId))
                    {
                        DispatcherService.Invoke(() =>
                        {
                            menu.viewModel.UpdatePairInfo(new Pair(
                           PairMarket.Bybit,
                           PairMarketType.Spot,
                           quoteAsset,
                           item.Symbol, item.LastPrice, item.PricePercentage24h));
                        });
                    }
                    Common.ArrangePairs();
                }
                else if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Bybit && Common.CurrentSelectedPairMarketType.PairMarketType == PairMarketType.Spot) // 바이비트 현물
                {
                    if (quoteAsset == Common.CurrentSelectedPairQuoteAsset.PairQuoteAsset)
                    {
                        DispatcherService.Invoke(() =>
                        {
                            menu.viewModel.UpdatePairInfo(new Pair(
                            PairMarket.Bybit,
                            PairMarketType.Spot,
                            quoteAsset,
                            item.Symbol, item.LastPrice, item.PricePercentage24h));
                        });
                    }
                    Common.ArrangePairs();
                }
            });
        }
        public static void UpdateBybitLinearTicker(BybitSocketClient client, MenuControl menu, IEnumerable<string> symbols)
        {
            client.V5LinearApi.SubscribeToTickerUpdatesAsync(symbols, (obj) =>
            {
                var item = obj.Data;
                decimal price = 0;
                decimal change = 0;
                if (item.LastPrice != null && item.PricePercentage24h != null)
                {
                    price = item.LastPrice.Value;
                    change = item.PricePercentage24h.Value;
                }
                else
                {
                    return;
                }
                var quoteAsset = BybitSymbolMapper.GetPairQuoteAsset(item.Symbol);

                if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Favorites) // 즐겨찾기
                {
                    var pairId = $"Bybit_Linear_{item.Symbol}";
                    if (SettingsMan.FavoritesList.Contains(pairId))
                    {
                        DispatcherService.Invoke(() =>
                        {
                            menu.viewModel.UpdatePairInfo(new Pair(
                           PairMarket.Bybit,
                           PairMarketType.Linear,
                           quoteAsset,
                           item.Symbol, price, change));
                        });
                    }
                    Common.ArrangePairs();
                }
                else if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Bybit && Common.CurrentSelectedPairMarketType.PairMarketType == PairMarketType.Linear) // 바이비트 선물
                {
                    if (quoteAsset == Common.CurrentSelectedPairQuoteAsset.PairQuoteAsset)
                    {
                        DispatcherService.Invoke(() =>
                        {
                            menu.viewModel.UpdatePairInfo(new Pair(
                            PairMarket.Bybit,
                            PairMarketType.Linear,
                            quoteAsset,
                            item.Symbol, price, change));
                        });
                    }
                    Common.ArrangePairs();
                }
            });
        }
        public static void UpdateBybitInverseTicker(BybitSocketClient client, MenuControl menu, IEnumerable<string> symbols)
        {
            client.V5InverseApi.SubscribeToTickerUpdatesAsync(symbols, (obj) =>
            {
                var item = obj.Data;
                decimal price = 0;
                decimal change = 0;
                if (item.LastPrice != null && item.PricePercentage24h != null)
                {
                    price = item.LastPrice.Value;
                    change = item.PricePercentage24h.Value;
                }
                else
                {
                    return;
                }
                var quoteAsset = BybitSymbolMapper.GetPairQuoteAsset(item.Symbol);

                if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Favorites) // 즐겨찾기
                {
                    var pairId = $"Bybit_Inverse_{item.Symbol}";
                    if (SettingsMan.FavoritesList.Contains(pairId))
                    {
                        DispatcherService.Invoke(() =>
                        {
                            menu.viewModel.UpdatePairInfo(new Pair(
                           PairMarket.Bybit,
                           PairMarketType.Inverse,
                           quoteAsset,
                           item.Symbol, price, change));
                        });
                    }
                    Common.ArrangePairs();
                }
                else if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Bybit && Common.CurrentSelectedPairMarketType.PairMarketType == PairMarketType.Inverse) // 바이비트 선물 인버스
                {
                    if (quoteAsset == Common.CurrentSelectedPairQuoteAsset.PairQuoteAsset)
                    {
                        DispatcherService.Invoke(() =>
                        {
                            menu.viewModel.UpdatePairInfo(new Pair(
                            PairMarket.Bybit,
                            PairMarketType.Inverse,
                            quoteAsset,
                            item.Symbol, price, change));
                        });
                    }
                    Common.ArrangePairs();
                }
            });
        }
        public static void UpdateBybitOptionTicker(BybitSocketClient client, MenuControl menu, IEnumerable<string> symbols)
        {
            client.V5OptionsApi.SubscribeToTickerUpdatesAsync(symbols, (obj) =>
            {
                if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Favorites) // 즐겨찾기
                {
                    var item = obj.Data;
                    var quoteAsset = BybitSymbolMapper.GetPairQuoteAsset(item.Symbol);
                    var pairId = $"Bybit_Option_{item.Symbol}";
                    if (SettingsMan.FavoritesList.Contains(pairId))
                    {
                        DispatcherService.Invoke(() =>
                        {
                            menu.viewModel.UpdatePairInfo(new Pair(
                           PairMarket.Bybit,
                           PairMarketType.Option,
                           quoteAsset,
                           item.Symbol, item.LastPrice, item.Change24h));
                        });
                    }
                    Common.ArrangePairs();
                }
                else if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Bybit && Common.CurrentSelectedPairMarketType.PairMarketType == PairMarketType.Option) // 바이비트 옵션
                {
                    var item = obj.Data;
                    var quoteAsset = BybitSymbolMapper.GetPairQuoteAsset(item.Symbol);
                    if (quoteAsset == Common.CurrentSelectedPairQuoteAsset.PairQuoteAsset)
                    {
                        DispatcherService.Invoke(() =>
                        {
                            menu.viewModel.UpdatePairInfo(new Pair(
                            PairMarket.Bybit,
                            PairMarketType.Option,
                            quoteAsset,
                            item.Symbol, item.LastPrice, item.Change24h));
                        });
                    }
                    Common.ArrangePairs();
                }
            });
        }
        #endregion

        #region Upbit
        public static void UpdateUpbitSpotTicker(UpbitClient client, MenuControl menu)
        {
            if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Favorites) // 즐겨찾기
            {
                var symbols = UpbitSymbolMapper.Symbols;
                var tickerResult = client.QuotationTickers.GetTickersAsync(symbols);
                tickerResult.Wait();
                foreach (var coin in tickerResult.Result)
                {
                    var quoteAsset = UpbitSymbolMapper.GetPairQuoteAsset(coin.market);
                    var pairId = $"Upbit_Spot_{coin.market}";
                    if (SettingsMan.FavoritesList.Contains(pairId))
                    {
                        menu.viewModel.UpdatePairInfo(new Pair(
                                PairMarket.Upbit,
                                PairMarketType.Spot,
                                quoteAsset,
                                coin.market, coin.trade_price, coin.signed_change_rate * 100));
                    }
                }
                Common.ArrangePairs();
            }
            else if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Upbit) // 업비트 현물
            {
                var symbols = Common.CurrentSelectedPairQuoteAsset.PairQuoteAsset switch
                {
                    PairQuoteAsset.KRW => UpbitSymbolMapper.KrwSymbols,
                    PairQuoteAsset.BTC => UpbitSymbolMapper.BtcSymbols,
                    PairQuoteAsset.USDT => UpbitSymbolMapper.UsdtSymbols,
                    _ => UpbitSymbolMapper.Symbols,
                };
                var tickerResult = client.QuotationTickers.GetTickersAsync(symbols);
                tickerResult.Wait();
                foreach (var coin in tickerResult.Result)
                {
                    menu.viewModel.UpdatePairInfo(new Pair(
                   PairMarket.Upbit,
                   PairMarketType.Spot,
                   Common.CurrentSelectedPairQuoteAsset.PairQuoteAsset,
                   coin.market, coin.trade_price, coin.signed_change_rate * 100));
                }
                Common.ArrangePairs();
            }
        }
        #endregion

        #region Bithumb
        public static void InitLoadBithumbSpotTicker(BithumbClient client, MenuControl menu)
        {
            if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Favorites) // 즐겨찾기
            {
                var tickers = client.Public.GetAllTickersAsync(BithumbPaymentCurrency.KRW);
                tickers.Wait();
                foreach (var coin in tickers.Result.data?.coins ?? default!)
                {
                    var pairId = $"Bithumb_Spot_{coin.currency}_KRW";
                    if (SettingsMan.FavoritesList.Contains(pairId))
                    {
                        DispatcherService.Invoke(() =>
                        {
                            menu.viewModel.UpdatePairInfo(new Pair(
                            PairMarket.Bithumb,
                            PairMarketType.Spot,
                            PairQuoteAsset.KRW,
                            $"{coin.currency}_KRW", coin.closing_price, coin.fluctate_rate_24H));
                        });
                    }
                }
                tickers = client.Public.GetAllTickersAsync(BithumbPaymentCurrency.BTC);
                tickers.Wait();
                foreach (var coin in tickers.Result.data?.coins ?? default!)
                {
                    var pairId = $"Bithumb_Spot_{coin.currency}_BTC";
                    if (SettingsMan.FavoritesList.Contains(pairId))
                    {
                        DispatcherService.Invoke(() =>
                        {
                            menu.viewModel.UpdatePairInfo(new Pair(
                            PairMarket.Bithumb,
                            PairMarketType.Spot,
                            PairQuoteAsset.BTC,
                            $"{coin.currency}_BTC", coin.closing_price, coin.fluctate_rate_24H));
                        });
                    }
                }
            }
            else if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Bithumb && Common.CurrentSelectedPairMarketType.PairMarketType == PairMarketType.Spot) // 빗썸 현물
            {
                var paymentCurrency = Common.CurrentSelectedPairQuoteAsset.PairQuoteAsset.ToBithumbPaymentCurrency();
                var tickers = client.Public.GetAllTickersAsync(paymentCurrency);
                tickers.Wait();
                foreach (var coin in tickers.Result.data?.coins ?? default!)
                {
                    DispatcherService.Invoke(() =>
                    {
                        menu.viewModel.UpdatePairInfo(new Pair(
                        PairMarket.Bithumb,
                        PairMarketType.Spot,
                        Common.CurrentSelectedPairQuoteAsset.PairQuoteAsset,
                        $"{coin.currency}_{paymentCurrency}", coin.closing_price, coin.fluctate_rate_24H));
                    });
                }
            }
        }

        public static void UpdateBithumbSpotTicker(BithumbSocketClient client, MenuControl menu)
        {
            client.Streams.SubscribeToTickerAsync(BithumbSymbolMapper.Symbols, BithumbSocketTickInterval.OneDay, (obj) =>
            {
                var data = obj.content;

                if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Favorites) // 즐겨찾기
                {
                    var pairId = $"Bithumb_Spot_{data.symbol}";
                    var quoteAsset = BithumbSymbolMapper.GetPairQuoteAsset(data.symbol);
                    DispatcherService.Invoke(() =>
                    {
                        if (SettingsMan.FavoritesList.Contains(pairId))
                        {
                            menu.viewModel.UpdatePairInfo(new Pair(
                                PairMarket.Bithumb,
                                PairMarketType.Spot,
                                quoteAsset,
                                data.symbol, data.closePrice, data.chgRate));
                        }
                    });
                    Common.ArrangePairs();
                }
                else if (Common.CurrentSelectedPairMarket.PairMarket == PairMarket.Bithumb) // 빗썸 현물
                {
                    var quoteAsset = BithumbSymbolMapper.GetPairQuoteAsset(data.symbol);
                    if (quoteAsset == Common.CurrentSelectedPairQuoteAsset.PairQuoteAsset)
                    {
                        DispatcherService.Invoke(() =>
                        {
                            menu.viewModel.UpdatePairInfo(new Pair(
                          PairMarket.Bithumb,
                          PairMarketType.Spot,
                          quoteAsset,
                          data.symbol, data.closePrice, data.chgRate));
                        });
                    }
                    Common.ArrangePairs();
                }
            });
        }
        #endregion
    }
}
