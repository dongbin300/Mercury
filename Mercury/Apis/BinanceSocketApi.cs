using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models;
using Binance.Net.Objects.Models.Futures.Socket;

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects.Sockets;

using Mercury.Charts;

namespace Mercury.Apis
{
	public class BinanceSocketApi
	{
		#region Initialize
		public static BinanceSocketClient BinanceClient = new();

		/// <summary>
		/// 바이낸스 클라이언트 초기화
		/// </summary>
		public static void Init()
		{
			var data = File.ReadAllLines(MercuryPath.BinanceApiKey);

			// BinanceSocketClient 초기화
			var socketClient = new BinanceSocketClient();
			BinanceClient.SetApiCredentials(new ApiCredentials(data[0], data[1]));
		}
		#endregion

		#region Market API
		public static async void GetKlineUpdatesAsync(string symbol, KlineInterval interval)
		{
			var result = await BinanceClient.UsdFuturesApi.ExchangeData.SubscribeToKlineUpdatesAsync(symbol, interval, KlineUpdatesOnMessage);
		}

		public static async void GetKlineUpdatesAsync2(string symbol, KlineInterval interval)
		{
			var result = await BinanceClient.UsdFuturesApi.ExchangeData.SubscribeToKlineUpdatesAsync(symbol, interval, KlineUpdatesOnMessage2);
		}

		public static async void GetContinuousKlineUpdatesAsync(string symbol, KlineInterval interval)
		{
			var result = await BinanceClient.UsdFuturesApi.ExchangeData.SubscribeToContinuousContractKlineUpdatesAsync(symbol, ContractType.Perpetual, interval, ContinuousKlineUpdatesOnMessage);
		}

		public static async void GetBnbMarkPriceUpdatesAsync()
		{
			var result = await BinanceClient.UsdFuturesApi.ExchangeData.SubscribeToMarkPriceUpdatesAsync("BNBUSDT", 1000, BnbMarkPriceUpdatesAsyncOnMessage);
		}

		public static async void GetAllMarketMiniTickersAsync()
		{
			var result = await BinanceClient.UsdFuturesApi.ExchangeData.SubscribeToAllMiniTickerUpdatesAsync(AllMarketMiniTickersOnMessage);
		}

		public static async void SubscribeToUserDataUpdatesAsync()
		{
			var listenKey = BinanceRestApi.StartUserStream();
			var result = await BinanceClient.UsdFuturesApi.Account.SubscribeToUserDataUpdatesAsync(listenKey, null, null, AccountUpdateOnMessage, null, ListenKeyExpiredOnMessage);
		}

		public static void ListenKeyExpiredOnMessage(DataEvent<BinanceFuturesStreamTradeUpdate> obj)
		{
			
		}

		public static void AccountUpdateOnMessage(DataEvent<BinanceFuturesStreamAccountUpdate> obj)
		{

		}

		/// <summary>
		/// 모든 심볼의 24시간 변화 데이터
		/// </summary>
		/// <param name="obj"></param>
		private static void AllMarketMiniTickersOnMessage(DataEvent<IBinanceMiniTick[]> obj)
		{
			var data = obj.Data;
			QuoteFactory.CurrentPrices = [.. data.Select(x => new CurrentPrice(x.Symbol, x.LastPrice))];
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
