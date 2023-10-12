using Binance.Net.Clients;
using Binance.Net.Enums;

using CryptoExchange.Net.Authentication;

using MarinerX.Accounts;
using MarinerX.Markets;
using MarinerX.Utils;

using Mercury;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace MarinerX.Apis
{
    internal class BinanceRestApi
    {
        #region Initialize
        static BinanceRestClient binanceClient = new();

        /// <summary>
        /// 바이낸스 클라이언트 초기화
        /// </summary>
        public static void Init()
        {
            try
            {
                var data = File.ReadAllLines(PathUtil.BinanceApiKey);

                binanceClient = new BinanceRestClient();
                binanceClient.SetApiCredentials(new ApiCredentials(data[0], data[1]));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region Market API
        /// <summary>
        /// 선물 심볼이름만 가져오기
        /// </summary>
        /// <returns></returns>
        public static List<string> GetFuturesSymbolNames()
        {
            var usdFuturesSymbolData = binanceClient.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();
            usdFuturesSymbolData.Wait();

            return usdFuturesSymbolData.Result.Data.Symbols
                .Where(s => s.Name.EndsWith("USDT") && !s.Name.Equals("LINKUSDT") && !s.Name.StartsWith("1"))
                .Select(s => s.Name)
                .ToList();
        }

        /// <summary>
        /// 선물 심볼 정보 가져오기
        /// </summary>
        /// <returns></returns>
        public static List<FuturesSymbol> GetFuturesSymbols()
        {
            var usdFuturesSymbolData = binanceClient.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();
            usdFuturesSymbolData.Wait();

            return usdFuturesSymbolData.Result.Data.Symbols
                .Where(s => s.Name.EndsWith("USDT") && !s.Name.Equals("LINKUSDT") && !s.Name.StartsWith("1"))
                .Select(s => new FuturesSymbol(s.Name, s.LiquidationFee, s.ListingDate, s.PriceFilter?.MaxPrice, s.PriceFilter?.MinPrice, s.PriceFilter?.TickSize, s.LotSizeFilter?.MaxQuantity, s.LotSizeFilter?.MinQuantity, s.LotSizeFilter?.StepSize, s.PricePrecision, s.QuantityPrecision, s.UnderlyingType))
                .ToList();
        }
        #endregion

        #region Chart API
        /// <summary>
        /// 하루 동안의 차트 데이터 가져오기
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="startTime"></param>
        public static void GetCandleDataForOneDay(string symbol, DateTime startTime)
        {
            var result = binanceClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(
                symbol,
                KlineInterval.OneMinute,
                startTime,
                startTime.AddMinutes(1439),
                1500);
            result.Wait();

            var builder = new StringBuilder();
            foreach (var data in result.Result.Data)
            {
                builder.AppendLine(string.Join(',', new string[] {
                data.OpenTime.ToString("yyyy-MM-dd HH:mm:ss"),
                data.OpenPrice.ToString(),
                data.HighPrice.ToString(),
                data.LowPrice.ToString(),
                data.ClosePrice.ToString(),
                data.Volume.ToString(),
                data.QuoteVolume.ToString(),
                data.TakerBuyBaseVolume.ToString(),
                data.TakerBuyQuoteVolume.ToString(),
                data.TradeCount.ToString()
            }));
            }

            if (builder.Length < 10)
            {
                return;
            }

            File.WriteAllText(
                path: PathUtil.BinanceFuturesData.Down("1m", symbol, $"{symbol}_{startTime:yyyy-MM-dd}.csv"),
                contents: builder.ToString()
                );
        }

        /// <summary>
        /// 봉 데이터 가져오기
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="interval"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        public static List<Quote> GetQuotes(string symbol, KlineInterval interval, DateTime? startTime, DateTime? endTime, int limit)
        {
            var result = binanceClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, interval, startTime, endTime, limit);
            result.Wait();

            var quotes = new List<Quote>();

            foreach (var data in result.Result.Data)
            {
                quotes.Add(new Quote
                {
                    Date = data.OpenTime,
                    Open = data.OpenPrice,
                    High = data.HighPrice,
                    Low = data.LowPrice,
                    Close = data.ClosePrice,
                    Volume = data.Volume
                });
            }

            return quotes;
        }

        public static double GetCurrentBnbPrice()
        {
            var result = binanceClient.SpotApi.ExchangeData.GetCurrentAvgPriceAsync("BNBUSDT");
            result.Wait();

            return Convert.ToDouble(result.Result.Data.Price);
        }
        #endregion

        #region Account API
        /// <summary>
        /// 선물 계좌 정보 가져오기
        /// </summary>
        /// <returns></returns>
        public static FuturesAccount GetFuturesAccountInfo()
        {
            var accountInfo = binanceClient.UsdFuturesApi.Account.GetAccountInfoAsync();
            accountInfo.Wait();

            var info = accountInfo.Result.Data;

            return new FuturesAccount(
                info.Assets.Where(x => x.WalletBalance > 0).ToList(),
                info.Positions.Where(x => x.Symbol.EndsWith("USDT") && !x.Symbol.Equals("LINKUSDT")).ToList(),
                info.AvailableBalance,
                info.TotalMarginBalance,
                info.TotalUnrealizedProfit,
                info.TotalWalletBalance
                );
        }

        /// <summary>
        /// 초기 레버리지 배율 바꾸기
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="leverage"></param>
        /// <returns></returns>
        public static bool ChangeInitialLeverage(string symbol, int leverage)
        {
            var changeInitialLeverage = binanceClient.UsdFuturesApi.Account.ChangeInitialLeverageAsync(symbol, leverage);
            changeInitialLeverage.Wait();

            return changeInitialLeverage.Result.Success;
        }

        /// <summary>
        /// 마진 타입 바꾸기
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool ChangeMarginType(string symbol, FuturesMarginType type)
        {
            var changeMarginType = binanceClient.UsdFuturesApi.Account.ChangeMarginTypeAsync(symbol, type);
            changeMarginType.Wait();

            return changeMarginType.Result.Success;
        }

        /// <summary>
        /// 전체 포지션 가져오기
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static List<FuturesPosition> GetPositionInformation(string? symbol = null)
        {
            var positionInformation = binanceClient.UsdFuturesApi.Account.GetPositionInformationAsync(symbol);
            positionInformation.Wait();

            return positionInformation.Result.Data
                .Where(x => x.Symbol.EndsWith("USDT") && !x.Symbol.Equals("LINKUSDT"))
                .Select(x => new FuturesPosition(
                    x.Symbol,
                    x.MarginType,
                    x.Leverage,
                    x.PositionSide,
                    x.Quantity,
                    x.EntryPrice,
                    x.MarkPrice,
                    x.UnrealizedPnl,
                    x.LiquidationPrice
                    ))
                .ToList();
        }

        /// <summary>
        /// 현재 포지션 가져오기
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static List<FuturesPosition> GetPositioningInformation(string? symbol = null)
        {
            var positionInformation = binanceClient.UsdFuturesApi.Account.GetPositionInformationAsync(symbol);
            positionInformation.Wait();

            return positionInformation.Result.Data
                .Where(x => x.Symbol.EndsWith("USDT") && !x.Symbol.Equals("LINKUSDT") && x.Quantity != 0)
                .Select(x => new FuturesPosition(
                    x.Symbol,
                    x.MarginType,
                    x.Leverage,
                    x.PositionSide,
                    x.Quantity,
                    x.EntryPrice,
                    x.MarkPrice,
                    x.UnrealizedPnl,
                    x.LiquidationPrice
                    ))
                .ToList();
        }

        /// <summary>
        /// 현재 잔고 가져오기
        /// </summary>
        /// <returns></returns>
        public static List<FuturesBalance> GetBalance()
        {
            var balance = binanceClient.UsdFuturesApi.Account.GetBalancesAsync();
            balance.Wait();

            return balance.Result.Data.Where(x => x.Asset.Equals("USDT") || x.Asset.Equals("BNB"))
                .Select(x => new FuturesBalance(
                     x.Asset,
                     x.WalletBalance,
                     x.AvailableBalance,
                     x.CrossUnrealizedPnl
                    ))
                .ToList();
        }

        public static decimal GetFuturesBalance()
        {
            try
            {
                var result = binanceClient.UsdFuturesApi.Account.GetBalancesAsync();
                result.Wait();
                var balance = result.Result.Data;
                var usdtBalance = balance.First(b => b.Asset.Equals("USDT"));
                var usdt = usdtBalance.WalletBalance + usdtBalance.CrossUnrealizedPnl;
                var bnb = balance.First(b => b.Asset.Equals("BNB")).WalletBalance * (decimal)Common.BnbPrice;
                return usdt + bnb;
            }
            catch
            {
                return 0;
            }
        }

        public static string StartUserStream()
        {
            var listenKey = binanceClient.UsdFuturesApi.Account.StartUserStreamAsync();
            listenKey.Wait();

            return listenKey.Result.Data;
        }

        public static void StopUserStream(string listenKey)
        {
            var result = binanceClient.UsdFuturesApi.Account.StopUserStreamAsync(listenKey);
            result.Wait();
        }
        #endregion

        #region Leverage API
        public static Dictionary<string, int> GetMaxLeverages()
        {
            var results = new Dictionary<string, int>();
            try
            {
                var result = binanceClient.UsdFuturesApi.Account.GetBracketsAsync();
                result.Wait();

                foreach (var d in result.Result.Data)
                {
                    results.Add(d.Symbol, d.Brackets.Max(x => x.InitialLeverage));
                }
            }
            catch
            {
            }
            return results;
        }
        #endregion

        #region Trading API
        /// <summary>
        /// 바이낸스 주문
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="side"></param>
        /// <param name="type"></param>
        /// <param name="quantity"></param>
        /// <param name="price"></param>
        public static void Order(string symbol, OrderSide side, FuturesOrderType type, decimal quantity, decimal? price = null)
        {
            var placeOrder = binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, side, type, quantity, price);
            placeOrder.Wait();
        }

        /// <summary>
        /// 매수 주문, Long
        /// 가격을 지정하면 지정가
        /// 지정하지 않으면 시장가
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="quantity"></param>
        /// <param name="price"></param>
        public static void Buy(string symbol, double quantity, double? price = null)
        {
            var type = price == null ? FuturesOrderType.Market : FuturesOrderType.Limit;
            var placeOrder = binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Buy, type, Convert.ToDecimal(quantity), Convert.ToDecimal(price));
        }

        /// <summary>
        /// 매도 주문, Short
        /// 가격을 지정하면 지정가
        /// 지정하지 않으면 시장가
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="quantity"></param>
        /// <param name="price"></param>
        public static void Sell(string symbol, double quantity, double? price = null)
        {
            var type = price == null ? FuturesOrderType.Market : FuturesOrderType.Limit;
            var placeOrder = binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Sell, type, Convert.ToDecimal(quantity), Convert.ToDecimal(price));
        }
        #endregion
    }
}
