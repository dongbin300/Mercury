using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;

using CryptoExchange.Net.Objects;

using System;
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
                var data = System.IO.File.ReadAllLines(Common.BinanceApiKeyPath);
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

		/// <summary>
		/// GTC (Good 'Til Canceled)
        ///		설명: 주문이 명시적으로 취소될 때까지 유효합니다.주문이 전부 체결되거나 취소될 때까지 계속 시장에 남아 있습니다.
        ///		사용 예시: 특정 가격에 도달할 때까지 기다리고 싶은 경우.
        ///	IOC(Immediate or Cancel)
        ///     설명: 주문이 즉시 체결되지 않으면, 즉시 취소됩니다.부분 체결은 가능합니다.체결되지 않은 부분은 취소됩니다.
        ///     사용 예시: 가능한 한 빨리 주문을 체결하고 남은 부분은 취소하고 싶은 경우.
        /// FOK (Fill or Kill)
        ///     설명: 주문이 즉시 완전히 체결되지 않으면, 전부 취소됩니다.부분 체결은 불가능합니다.
        ///     사용 예시: 주문이 완전히 체결되지 않으면 아무 것도 체결하고 싶지 않은 경우.
        /// GTX (Good Till Crossing, Post-Only)
        ///     설명: 이 주문은 항상 메이커 주문으로 남아야 합니다.즉, 주문이 바로 체결되지 않고 주문서에 추가되어야 합니다.만약 주문이 즉시 체결될 상황이라면 취소됩니다.
        ///     사용 예시: 메이커 수수료를 피하고 싶은 경우.
		/// </summary>
		/// <param name="symbol"></param>
		/// <param name="price"></param>
		/// <param name="quantity"></param>
		/// <returns></returns>
		public static async Task<WebCallResult<BinanceUsdFuturesOrder>> OpenBuy(string symbol, decimal price, decimal quantity)
        {
            return await Api.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Buy, FuturesOrderType.Limit, quantity, price, PositionSide.Long, TimeInForce.GoodTillCrossing).ConfigureAwait(false);
        }

        public static async Task<WebCallResult<BinanceUsdFuturesOrder>> OpenSell(string symbol, decimal price, decimal quantity)
        {
            return await Api.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Sell, FuturesOrderType.Limit, quantity, price, PositionSide.Short, TimeInForce.GoodTillCrossing).ConfigureAwait(false);
        }

        public static async Task<WebCallResult<BinanceUsdFuturesOrder>> CloseBuy(string symbol, decimal price, decimal quantity)
        {
            return await Api.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Buy, FuturesOrderType.Limit, quantity, price, PositionSide.Short, TimeInForce.GoodTillCrossing).ConfigureAwait(false);
        }

        public static async Task<WebCallResult<BinanceUsdFuturesOrder>> CloseSell(string symbol, decimal price, decimal quantity)
        {
            return await Api.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Sell, FuturesOrderType.Limit, quantity, price, PositionSide.Long, TimeInForce.GoodTillCrossing).ConfigureAwait(false);
        }

        public static async Task<WebCallResult<BinanceUsdFuturesOrder>> SetLongTakeProfit(string symbol, decimal price, decimal quantity, decimal takePrice)
        {
            return await Api.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Sell, FuturesOrderType.TakeProfit, quantity, price, PositionSide.Long, null, null, null, takePrice).ConfigureAwait(false);
        }

        public static async Task<WebCallResult<BinanceUsdFuturesOrder>> SetLongStopLoss(string symbol, decimal price, decimal quantity, decimal stopPrice)
        {
            return await Api.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Sell, FuturesOrderType.Stop, quantity, price, PositionSide.Long, null, null, null, stopPrice).ConfigureAwait(false);
        }

        public static async Task<WebCallResult<BinanceUsdFuturesOrder>> SetShortTakeProfit(string symbol, decimal price, decimal quantity, decimal takePrice)
        {
            return await Api.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Buy, FuturesOrderType.TakeProfit, quantity, price, PositionSide.Short, null, null, null, takePrice).ConfigureAwait(false);
        }

        public static async Task<WebCallResult<BinanceUsdFuturesOrder>> SetShortStopLoss(string symbol, decimal price, decimal quantity, decimal stopPrice)
        {
            return await Api.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Buy, FuturesOrderType.Stop, quantity, price, PositionSide.Short, null, null, null, stopPrice).ConfigureAwait(false);
        }
    }
}
