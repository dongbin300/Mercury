using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects;
using Binance.Net.Objects.Models.Futures;

using CryptoExchange.Net.Objects;

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace TradeBot.Clients
{
    public class BinanceClients
    {
        public static BinanceRestClient Api = default!;
        public static BinanceSocketClient Socket = default!;

        string listenKey = string.Empty;

        public BinanceClients()
        {

        }

        public static void Init()
        {
            try
            {
                var data = File.ReadAllLines(Common.BinanceApiKeyPath);
                Api = new BinanceRestClient();
                Api.SetApiCredentials(new CryptoExchange.Net.Authentication.ApiCredentials(data[0], data[1]));
                //RefreshUserStream();

                Socket = new BinanceSocketClient();
                Socket.SetApiCredentials(new CryptoExchange.Net.Authentication.ApiCredentials(data[0], data[1]));
                // socketClient.UsdFuturesStreams.SubscribeToUserDataUpdatesAsync() 이거 죽어도 데이터 안옴(추후)
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(BinanceClients), MethodBase.GetCurrentMethod()?.Name, ex);
            }
        }

        private void RefreshUserStream()
        {
            var userStream = Api.UsdFuturesApi.Account.StartUserStreamAsync();
            userStream.Wait();
            listenKey = userStream.Result.Data;

            KeepAliveUserStream();
        }

        private void KeepAliveUserStream()
        {
            var userStream = Api.UsdFuturesApi.Account.KeepAliveUserStreamAsync(listenKey);
            userStream.Wait();
        }

        public static async Task<WebCallResult<BinanceFuturesPlacedOrder>> OpenBuy(string symbol, decimal price, decimal quantity)
        {
            return await Api.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Buy, FuturesOrderType.Limit, quantity, price, PositionSide.Long, TimeInForce.GoodTillCanceled).ConfigureAwait(false);
        }

        public static async Task<WebCallResult<BinanceFuturesPlacedOrder>> OpenSell(string symbol, decimal price, decimal quantity)
        {
            return await Api.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Sell, FuturesOrderType.Limit, quantity, price, PositionSide.Short, TimeInForce.GoodTillCanceled).ConfigureAwait(false);
        }

        public static async Task<WebCallResult<BinanceFuturesPlacedOrder>> CloseBuy(string symbol, decimal price, decimal quantity)
        {
            return await Api.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Buy, FuturesOrderType.Limit, quantity, price, PositionSide.Short, TimeInForce.GoodTillCanceled).ConfigureAwait(false);
        }

        public static async Task<WebCallResult<BinanceFuturesPlacedOrder>> CloseSell(string symbol, decimal price, decimal quantity)
        {
            return await Api.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Sell, FuturesOrderType.Limit, quantity, price, PositionSide.Long, TimeInForce.GoodTillCanceled).ConfigureAwait(false);
        }

        public static async Task<WebCallResult<BinanceFuturesPlacedOrder>> SetLongTakeProfit(string symbol, decimal price, decimal quantity, decimal takePrice)
        {
            return await Api.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Sell, FuturesOrderType.TakeProfit, quantity, price, PositionSide.Long, null, null, null, takePrice).ConfigureAwait(false);
        }

        public static async Task<WebCallResult<BinanceFuturesPlacedOrder>> SetLongStopLoss(string symbol, decimal price, decimal quantity, decimal stopPrice)
        {
            return await Api.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Sell, FuturesOrderType.Stop, quantity, price, PositionSide.Long, null, null, null, stopPrice).ConfigureAwait(false);
        }

        public static async Task<WebCallResult<BinanceFuturesPlacedOrder>> SetShortTakeProfit(string symbol, decimal price, decimal quantity, decimal takePrice)
        {
            return await Api.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Buy, FuturesOrderType.TakeProfit, quantity, price, PositionSide.Short, null, null, null, takePrice).ConfigureAwait(false);
        }

        public static async Task<WebCallResult<BinanceFuturesPlacedOrder>> SetShortStopLoss(string symbol, decimal price, decimal quantity, decimal stopPrice)
        {
            return await Api.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Buy, FuturesOrderType.Stop, quantity, price, PositionSide.Short, null, null, null, stopPrice).ConfigureAwait(false);
        }
    }
}
