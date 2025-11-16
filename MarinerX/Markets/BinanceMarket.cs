using Binance.Net.Objects.Models.Spot;

using MarinerX.Utils;

using Mercury.Apis;
using Mercury.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MarinerX.Markets
{
	public class BinanceMarket
	{
		public static List<SymbolBenchmark> Benchmarks = [];
		public static List<SymbolBenchmark2> Benchmarks2 = [];

		public static void Init()
		{
			#region Symbol Benchmark Calculate
			var volatilityResult = new Dictionary<string, decimal>();
			var amountResult = new Dictionary<string, decimal>();
			var data = LocalApi.GetAllOneDayQuotes();

			foreach (var d in data)
			{
				if (d.Value.Count <= 0)
				{
					continue;
				}

				var amount = d.Value.Average(x => x.Volume * (x.Low + x.High) / 2);
				amountResult.Add(d.Key, Math.Round(amount));

				var list = d.Value.Select(x => Math.Round((x.High - x.Low) / x.Low * 100, 2)).ToList();
				volatilityResult.Add(d.Key, Math.Round(list.Average(), 4));
			}

			var maxLeverages = BinanceRestApi.GetMaxLeverages();
			var symbolMarketCap = BinanceHttpApi.GetSymbolMarketCap();
			if (symbolMarketCap == null)
			{
				return;
			}

			foreach (var marketCap in symbolMarketCap)
			{
				var key = volatilityResult.Where(x => x.Key.Equals(marketCap.Symbol));
				var leverageKey = maxLeverages.Where(x => x.Key.Equals(marketCap.Symbol));
				if (key.Any())
				{
					var maxLeverage = leverageKey.Any() ? leverageKey.First().Value : 0;
					var listingDate = SymbolUtil.GetStartDate(marketCap.Symbol).ToString("yyyy-MM-dd");
					Benchmarks.Add(new SymbolBenchmark(marketCap.Symbol, key.First().Value, marketCap.marketCapWon, maxLeverage, listingDate));
				}
			}
			#endregion
		}

		public static void Init2()
		{
			var symbols = LocalApi.GetSymbols();
			var currentPrices = GetAllFuturesPricesWithRetry(2);
			var maxLeverages = BinanceRestApi.GetMaxLeverages();

			foreach (var symbol in symbols)
			{
				var name = symbol.Name;
				var listingDate = symbol.ListingDate;
				var _currentPrice = currentPrices.Find(x => x.Symbol.Equals(name));
				var currentPrice = _currentPrice == null ? -1m : _currentPrice.Price;
				var tickSize = symbol.TickSize ?? 1m;
				var tickPer = (tickSize / currentPrice).Round(5);
				var elapsedDays = (int)(DateTime.Today - listingDate).TotalDays;
				if (!maxLeverages.TryGetValue(name, out var maxLeverage))
				{
					maxLeverage = 0;
				}

				var maxDate = new DateTime(2023, 12, 30); // 백테스팅 시작 날짜보다 앞이어야함.
				var dayThreshold = (int)(DateTime.Today - maxDate).TotalDays;
				var tickPerThreshold = 0.0002m; // 한 호가 퍼센트 0.02% 미만
				var pass = elapsedDays >= dayThreshold && tickPer < tickPerThreshold && currentPrice != -1 ? name : string.Empty;

				Benchmarks2.Add(new SymbolBenchmark2(name, listingDate, tickSize, currentPrice, tickPer, elapsedDays, maxLeverage, pass));
			}
		}

        public static List<BinancePrice> GetAllFuturesPricesWithRetry(int retryCount = 3, int delayMs = 2000)
        {
            var allPricesDict = new Dictionary<string, BinancePrice>();
            for (int i = 0; i < retryCount; i++)
            {
                var prices = BinanceRestApi.GetFuturesPrices();
                foreach (var p in prices)
                    allPricesDict[p.Symbol] = p;

                Thread.Sleep(delayMs);
            }
            return [.. allPricesDict.Values];
        }

    }
}
