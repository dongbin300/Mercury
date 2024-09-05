using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot;

using CryptoExchange.Net.Authentication;

using Mercury.Cryptos.Binance;

using System.Collections.Generic;
using System.Text;

namespace Mercury.Apis
{
	public class BinanceRestApi
	{
		#region Initialize
		public static BinanceRestClient BinanceClient = new();

		/// <summary>
		/// 바이낸스 클라이언트 초기화
		/// </summary>
		public static void Init()
		{
			var data = File.ReadAllLines(MercuryPath.BinanceApiKey);

			BinanceClient = new BinanceRestClient();
			BinanceClient.SetApiCredentials(new ApiCredentials(data[0], data[1]));
		}
		#endregion

		#region Test
		public static BinanceExchangeInfo Test()
		{
			var exchangeInfo = BinanceClient.SpotApi.ExchangeData.GetExchangeInfoAsync();
			exchangeInfo.Wait();

			return exchangeInfo.Result.Data;
		}
		#endregion

		#region Market API
		/// <summary>
		/// 선물 심볼이름만 가져오기
		/// </summary>
		/// <returns></returns>
		public static List<string> GetFuturesSymbolNames()
		{
			var usdFuturesSymbolData = BinanceClient.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();
			usdFuturesSymbolData.Wait();

			var symbolNames = usdFuturesSymbolData.Result.Data.Symbols
				.Where(s => s.Name.EndsWith("USDT") && !s.Name.Equals("LINKUSDT") && !s.Name.StartsWith('1'))
				.Select(s => s.Name)
				.ToList();

			symbolNames.Insert(0, "ETHUSDC");
			symbolNames.Insert(0, "BTCUSDC");

			return symbolNames;
		}

		/// <summary>
		/// 선물 심볼 정보 가져오기
		/// </summary>
		/// <returns></returns>
		public static List<BinanceFuturesSymbol> GetFuturesSymbols()
		{
			var usdFuturesSymbolData = BinanceClient.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync();
			usdFuturesSymbolData.Wait();

			return usdFuturesSymbolData.Result.Data.Symbols
				.Where(s => s.Name.EndsWith("USDT") && !s.Name.Equals("LINKUSDT") && !s.Name.StartsWith('1'))
				.Select(s => new BinanceFuturesSymbol(s.Name, s.LiquidationFee, s.ListingDate, s.PriceFilter?.MaxPrice, s.PriceFilter?.MinPrice, s.PriceFilter?.TickSize, s.LotSizeFilter?.MaxQuantity, s.LotSizeFilter?.MinQuantity, s.LotSizeFilter?.StepSize, s.PricePrecision, s.QuantityPrecision, s.UnderlyingType))
				.ToList();
		}

		public static List<BinancePrice> GetFuturesPrices()
		{
			return BinanceClient.UsdFuturesApi.ExchangeData.GetPricesAsync().Result.Data.Where(s => s.Symbol.EndsWith("USDT") && !s.Symbol.Equals("LINKUSDT") && !s.Symbol.StartsWith('1')).ToList();
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
			var result = BinanceClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(
				symbol,
				KlineInterval.OneMinute,
				startTime,
				startTime.AddMinutes(1439),
				1500);
			result.Wait();

			var builder = new StringBuilder();
			foreach (var data in result.Result.Data)
			{
				builder.AppendLine(string.Join(',', [
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
			]));
			}

			if (builder.Length < 10)
			{
				return;
			}

			File.WriteAllText(
				path: MercuryPath.BinanceFuturesData.Down("1m", symbol, $"{symbol}_{startTime:yyyy-MM-dd}.csv"),
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
			var result = BinanceClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, interval, startTime, endTime, limit);
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

		public static List<AggregatedTrade> GetAggregatedTradesForOneDay(string symbol, DateTime date)
		{
			return GetAggregatedTrades(symbol, date, date.AddSeconds(86399), null);
		}

		public static List<AggregatedTrade> GetAggregatedTrades(string symbol, DateTime? startTime, DateTime? endTime, int? limit)
		{
			var result = BinanceClient.UsdFuturesApi.ExchangeData.GetAggregatedTradeHistoryAsync(symbol, null, startTime, endTime, limit);
			result.Wait();

			var aggregatedTrades = new List<AggregatedTrade>();

			foreach (var data in result.Result.Data)
			{
				aggregatedTrades.Add(new AggregatedTrade(data.TradeTime, data.Price, data.Quantity));
			}

			return aggregatedTrades;
		}

		public static double GetCurrentBnbPrice()
		{
			var result = BinanceClient.SpotApi.ExchangeData.GetCurrentAvgPriceAsync("BNBUSDT");
			result.Wait();

			return Convert.ToDouble(result.Result.Data.Price);
		}
		#endregion

		#region Account API
		/// <summary>
		/// 선물 계좌 정보 가져오기
		/// </summary>
		/// <returns></returns>
		public static BinanceFuturesAccount GetFuturesAccountInfo()
		{
			var accountInfo = BinanceClient.UsdFuturesApi.Account.GetAccountInfoV3Async();
			accountInfo.Wait();

			var info = accountInfo.Result.Data;

			return new BinanceFuturesAccount(
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
			var changeInitialLeverage = BinanceClient.UsdFuturesApi.Account.ChangeInitialLeverageAsync(symbol, leverage);
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
			var changeMarginType = BinanceClient.UsdFuturesApi.Account.ChangeMarginTypeAsync(symbol, type);
			changeMarginType.Wait();

			return changeMarginType.Result.Success;
		}

		/// <summary>
		/// 전체 포지션 가져오기
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		public static List<BinanceFuturesPosition> GetPositionInformation(string? symbol = null)
		{
			var positionInformation = BinanceClient.UsdFuturesApi.Account.GetPositionInformationAsync(symbol);
			positionInformation.Wait();

			return positionInformation.Result.Data
				.Where(x => x.Symbol.EndsWith("USDT") && !x.Symbol.Equals("LINKUSDT"))
				.Select(x => new BinanceFuturesPosition(
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
		public static List<BinanceFuturesPosition> GetPositioningInformation(string? symbol = null)
		{
			var positionInformation = BinanceClient.UsdFuturesApi.Account.GetPositionInformationAsync(symbol);
			positionInformation.Wait();

			return positionInformation.Result.Data
				.Where(x => x.Symbol.EndsWith("USDT") && !x.Symbol.Equals("LINKUSDT") && x.Quantity != 0)
				.Select(x => new BinanceFuturesPosition(
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
		public static List<BinanceFuturesBalance> GetBalance()
		{
			var balance = BinanceClient.UsdFuturesApi.Account.GetBalancesAsync();
			balance.Wait();

			return balance.Result.Data.Where(x => x.Asset.Equals("USDT") || x.Asset.Equals("BNB"))
				.Select(x => new BinanceFuturesBalance(
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
				var result = BinanceClient.UsdFuturesApi.Account.GetBalancesAsync();
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
			var listenKey = BinanceClient.UsdFuturesApi.Account.StartUserStreamAsync();
			listenKey.Wait();

			return listenKey.Result.Data;
		}

		public static void StopUserStream(string listenKey)
		{
			var result = BinanceClient.UsdFuturesApi.Account.StopUserStreamAsync(listenKey);
			result.Wait();
		}
		#endregion

		#region Leverage API
		public static Dictionary<string, int> GetMaxLeverages()
		{
			var results = new Dictionary<string, int>();
			try
			{
				var result = BinanceClient.UsdFuturesApi.Account.GetBracketsAsync();
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
			var placeOrder = BinanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, side, type, quantity, price);
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
			var placeOrder = BinanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Buy, type, Convert.ToDecimal(quantity), Convert.ToDecimal(price));
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
			var placeOrder = BinanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, OrderSide.Sell, type, Convert.ToDecimal(quantity), Convert.ToDecimal(price));
		}

		public static IEnumerable<BinanceFuturesTrade> GetFuturesTradeHistory(string[] symbols, DateTime startTime)
		{
			var result = new List<BinanceFuturesTrade>();

			foreach (var symbol in symbols)
			{
				var userTrades = BinanceClient.UsdFuturesApi.Trading.GetUserTradesAsync(symbol, startTime).Result;
				var trades = userTrades.Data.Select(x => new BinanceFuturesTrade(
					x.Timestamp,
					x.Symbol,
					x.PositionSide,
					x.Side,
					x.Price,
					x.Quantity,
					x.QuoteQuantity,
					x.Fee,
					x.FeeAsset,
					x.RealizedPnl,
					x.Maker
					)
				);
				result.AddRange(trades);
			}

			return result;
		}
		#endregion
	}
}
