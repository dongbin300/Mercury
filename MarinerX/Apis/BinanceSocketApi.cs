using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects;
using Binance.Net.Objects.Models.Futures.Socket;

using CryptoExchange.Net.Sockets;
using CryptoExchange.Net.Authentication;

using Mercury;

using MarinerX.Charts;
using MarinerX.Utils;

using MercuryTradingModel.Charts;

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace MarinerX.Apis
{
    internal class BinanceSocketApi
    {
        #region Initialize
        static BinanceSocketClient binanceClient = new();

        /// <summary>
        /// 바이낸스 클라이언트 초기화
        /// </summary>
        public static void Init()
        {
            try
            {
                var data = File.ReadAllLines(PathUtil.BinanceApiKey);

                binanceClient = new BinanceSocketClient();
                binanceClient.SetApiCredentials(new ApiCredentials(data[0], data[1]));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region Market API
        public static async void GetKlineUpdatesAsync(string symbol, KlineInterval interval)
        {
            var result = await binanceClient.UsdFuturesApi.SubscribeToKlineUpdatesAsync(symbol, interval, KlineUpdatesOnMessage);
        }

        public static async void GetKlineUpdatesAsync2(string symbol, KlineInterval interval)
        {
            var result = await binanceClient.UsdFuturesApi.SubscribeToKlineUpdatesAsync(symbol, interval, KlineUpdatesOnMessage2);
        }

        public static async void GetContinuousKlineUpdatesAsync(string symbol, KlineInterval interval)
        {
            var result = await binanceClient.UsdFuturesApi.SubscribeToContinuousContractKlineUpdatesAsync(symbol, ContractType.Perpetual, interval, ContinuousKlineUpdatesOnMessage);
        }

        public static async void GetBnbMarkPriceUpdatesAsync()
        {
            var result = await binanceClient.UsdFuturesApi.SubscribeToMarkPriceUpdatesAsync("BNBUSDT", 1000, BnbMarkPriceUpdatesAsyncOnMessage);
        }

        public static async void GetAllMarketMiniTickersAsync()
        {
            var result = await binanceClient.UsdFuturesApi.SubscribeToAllMiniTickerUpdatesAsync(AllMarketMiniTickersOnMessage);
        }

        //public static async void SubscribeToUserDataUpdatesAsync()
        //{
        //    var listenKey = BinanceClientApi.StartUserStream();
        //    var result = await binanceClient.UsdFuturesApi.SubscribeToUserDataUpdatesAsync(listenKey, null, null, AccountUpdateOnMessage, null, actionkey);
        //}

        //private static void actionkey(DataEvent<BinanceStreamEvent> obj)
        //{
        //    throw new NotImplementedException();
        //}

        //private static void AccountUpdateOnMessage(DataEvent<BinanceFuturesStreamAccountUpdate> obj)
        //{

        //}

        /// <summary>
        /// 모든 심볼의 24시간 변화 데이터
        /// </summary>
        /// <param name="obj"></param>
        private static void AllMarketMiniTickersOnMessage(DataEvent<IEnumerable<IBinanceMiniTick>> obj)
        {
            var data = obj.Data;
        }

        /// <summary>
        /// BNB의 현재 가격
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private static void BnbMarkPriceUpdatesAsyncOnMessage(DataEvent<BinanceFuturesUsdtStreamMarkPrice> obj)
        {
            Common.BnbPrice = (double)obj.Data.MarkPrice;
        }

        private static void ContinuousKlineUpdatesOnMessage(DataEvent<BinanceStreamContinuousKlineData> obj)
        {
            var data = obj.Data.Data;
        }

        private static void KlineUpdatesOnMessage(DataEvent<IBinanceStreamKlineData> obj)
        {
            var data = obj.Data.Data;
            QuoteFactory.UpdateQuote(new RealtimeQuote()
            {
                Symbol = obj.Data.Symbol,
                OpenTime = data.OpenTime,
                CloseTime = data.CloseTime,
                Open = data.OpenPrice,
                High = data.HighPrice,
                Low = data.LowPrice,
                Close = data.ClosePrice,
                Volume = data.Volume,
                TradeCount = data.TradeCount,
                TakerBuyBaseVolume = data.TakerBuyBaseVolume
            });
        }

        private static void KlineUpdatesOnMessage2(DataEvent<IBinanceStreamKlineData> obj)
        {
            var symbol = obj.Data.Symbol;
            var data = obj.Data.Data;
            RealtimeChartManager.UpdateRealtimeChart(symbol, new Quote
            {
                Date = data.OpenTime,
                Open = data.OpenPrice,
                High = data.HighPrice,
                Low = data.LowPrice,
                Close = data.ClosePrice,
                Volume = data.Volume
            });
        }
        #endregion
    }
}
