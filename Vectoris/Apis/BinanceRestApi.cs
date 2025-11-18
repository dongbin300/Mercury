using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Spot;

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;

using System.Net.Http.Json;
using System.Text;

using Vectoris.Charts.Core;
using Vectoris.Extensions;
namespace Vectoris.Apis;

public class BinanceRestApi
{
	#region Initialize
	public static BinanceRestClient Client { get; set; } = default!;

	public static void Init()
	{
		var data = File.ReadAllLines(Paths.BinanceApiKey);
		Client = new BinanceRestClient();
		Client.SetApiCredentials(new ApiCredentials(data[0], data[1]));
	}
	#endregion

	public class Futures
	{
		#region Market API
		public static List<string> GetUsdtSymbolNames()
		{
			return [.. Client.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync().Result.Data.Symbols
				.Where(s => s.Name.EndsWith("USDT"))
				.Select(s => s.Name)];
		}

		public static List<string> GetUsdcSymbolNames()
		{
			return [.. Client.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync().Result.Data.Symbols
				.Where(s => s.Name.EndsWith("USDC"))
				.Select(s => s.Name)];
		}

		public static List<BinanceFuturesSymbol> GetUsdtSymbols()
		{
			return [.. Client.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync().Result.Data.Symbols
				.Where(s => s.Name.EndsWith("USDT"))];
		}

		public static List<BinanceFuturesSymbol> GetUsdcSymbols()
		{
			return [.. Client.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync().Result.Data.Symbols
				.Where(s => s.Name.EndsWith("USDC"))];
		}

		public static List<BinancePrice> GetCurrentPrices()
		{
			return [.. Client.UsdFuturesApi.ExchangeData.GetPricesAsync().Result.Data
				.Where(s => s.Symbol.EndsWith("USDT"))];
		}

		public static decimal GetCurrentPrice(string symbol)
		{
			return Client.UsdFuturesApi.ExchangeData.GetPriceAsync(symbol).Result.Data.Price;
		}
		#endregion

		#region Chart API
		public static void GetDailyQuotes(string symbol, DateTime date)
		{
			var start = date;
			var end = date.AddDays(1).AddSeconds(-1);

			var result = Client.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute, start, end, 1500).Result;

			if (!result.Success || result.Data.Length == 0)
			{
				return;
			}

			var builder = new StringBuilder();
			foreach (var data in result.Data)
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

			File.WriteAllText(Paths.BinanceFuturesData.Down("1m", symbol, $"{symbol}_{date:yyyy-MM-dd}.csv"), builder.ToString());
		}

		public static List<Quote> GetQuotes(string symbol, KlineInterval interval, DateTime? startTime, DateTime? endTime, int limit)
		{
			var result = Client.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, interval, startTime, endTime, limit).Result;

			var quotes = new List<Quote>(result.Data.Length);
			foreach (var d in result.Data)
			{
				quotes.Add(new Quote(
					d.OpenTime,
					d.OpenPrice,
					d.HighPrice,
					d.LowPrice,
					d.ClosePrice,
					d.Volume
				));
			}

			return quotes;
		}
		#endregion

		#region Account API
		/// <summary>
		/// 선물 계좌 정보 가져오기
		/// </summary>
		/// <returns></returns>
		public static BinanceFuturesAccountInfoV3 GetAccountInfo()
		{
			var data = Client.UsdFuturesApi.Account.GetAccountInfoV3Async().Result.Data;
			data.Assets = [.. data.Assets.Where(x => x.WalletBalance > 0)];
			//data.Positions = [.. data.Positions.Where(x => x.Symbol.EndsWith("USDT"))];

			return data;
		}

		/// <summary>
		/// 초기 레버리지 배율 바꾸기
		/// </summary>
		/// <param name="symbol"></param>
		/// <param name="leverage"></param>
		/// <returns></returns>
		public static bool ChangeInitialLeverage(string symbol, int leverage)
		{
			return Client.UsdFuturesApi.Account.ChangeInitialLeverageAsync(symbol, leverage).Result.Success;
		}

		/// <summary>
		/// 마진 타입 바꾸기
		/// </summary>
		/// <param name="symbol"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool ChangeMarginType(string symbol, FuturesMarginType type)
		{
			return Client.UsdFuturesApi.Account.ChangeMarginTypeAsync(symbol, type).Result.Success;
		}

		/// <summary>
		/// 전체 포지션 가져오기
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		public static List<BinancePositionDetailsBase> GetPositions(string? symbol = null)
		{
			return [.. Client.UsdFuturesApi.Account.GetPositionInformationAsync(symbol).Result.Data];
		}

		/// <summary>
		/// 현재 포지션 가져오기
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		public static List<BinancePositionDetailsBase> GetCurrentPositions(string? symbol = null)
		{
			var data = Client.UsdFuturesApi.Account.GetPositionInformationAsync(symbol).Result.Data;
			data = [.. data.Where(x => x.Quantity != 0)];
			return [.. data];
		}

		/// <summary>
		/// 현재 잔고 가져오기
		/// </summary>
		/// <returns></returns>
		public static List<BinanceFuturesAccountBalance> GetBalance()
		{
			var data = Client.UsdFuturesApi.Account.GetBalancesAsync().Result.Data;
			data = [.. data.Where(x => x.Asset.Equals("USDT") || x.Asset.Equals("BNB"))];
			return [.. data];
		}

		/// <summary>
		/// 현재 전체 잔고(USDT) 가져오기
		/// </summary>
		/// <returns></returns>
		public static decimal GetTotalBalanceInUsdt()
		{
			var balances = Client.UsdFuturesApi.Account.GetBalancesAsync().Result.Data;
			var bnbPrice = GetCurrentPrice("BNBUSDT");

			var usdt = balances.FirstOrDefault(x => x.Asset.Equals("USDT"))?.WalletBalance ?? 0m;
			var bnb = balances.FirstOrDefault(x => x.Asset.Equals("BNB"))?.WalletBalance ?? 0m;

			return usdt + (bnb * bnbPrice);
		}

		public static string StartUserStream()
		{
			return Client.UsdFuturesApi.Account.StartUserStreamAsync().Result.Data;
		}

		public static WebCallResult StopUserStream(string listenKey)
		{
			return Client.UsdFuturesApi.Account.StopUserStreamAsync(listenKey).Result;
		}
		#endregion

		#region Leverage API
		public static Dictionary<string, int> GetMaxLeverages()
		{
			try
			{
				var data = Client.UsdFuturesApi.Account.GetBracketsAsync().Result.Data;

				return data
					.GroupBy(b => b.Symbol)
					.ToDictionary(
						g => g.Key,
						g => g.SelectMany(b => b.Brackets).Max(x => x.InitialLeverage)
					);
			}
			catch
			{
				return [];
			}
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
		public static BinanceUsdFuturesOrder Order(string symbol, OrderSide side, FuturesOrderType type, decimal quantity, decimal? price = null)
		{
			return Client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, side, type, quantity, price).Result.Data;
		}

		/// <summary>
		/// 매수 주문, Long
		/// 가격을 지정하면 지정가
		/// 지정하지 않으면 시장가
		/// </summary>
		/// <param name="symbol"></param>
		/// <param name="quantity"></param>
		/// <param name="price"></param>
		public static BinanceUsdFuturesOrder Buy(string symbol, decimal quantity, decimal? price = null)
		{
			var side = OrderSide.Buy;
			var type = price == null ? FuturesOrderType.Market : FuturesOrderType.Limit;

			return Order(symbol, side, type, quantity, price);
		}

		/// <summary>
		/// 매도 주문, Short
		/// 가격을 지정하면 지정가
		/// 지정하지 않으면 시장가
		/// </summary>
		/// <param name="symbol"></param>
		/// <param name="quantity"></param>
		/// <param name="price"></param>
		public static BinanceUsdFuturesOrder Sell(string symbol, decimal quantity, decimal? price = null)
		{
			var side = OrderSide.Sell;
			var type = price == null ? FuturesOrderType.Market : FuturesOrderType.Limit;

			return Order(symbol, side, type, quantity, price);
		}

		public static IEnumerable<BinanceFuturesTrade> GetTradeHistory(string[] symbols, DateTime startTime)
		{
			var results = new List<BinanceFuturesTrade>();

			foreach (var symbol in symbols)
			{
				try
				{
					var response = Client.UsdFuturesApi.Trading.GetUserTradesAsync(symbol, startTime).Result;

					if (response.Success && response.Data is { } data)
						results.AddRange(data);
				}
				catch
				{
					continue;
				}
			}

			return results;
		}

		#endregion
	}
}
