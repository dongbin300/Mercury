using Binance.Net.Enums;

using Mercury.Enums;
using Mercury.Maths;

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using TradeBot.Clients;
using TradeBot.Extensions;
using TradeBot.Systems;

namespace TradeBot.Bots
{
	public class ShortBot : Bot
	{
		#region Entry
		public bool IsRunning { get; set; }
		public decimal BaseOrderSize { get; set; }
		public decimal TargetRoe { get; set; }
		public int Leverage { get; set; }
		public MaxActiveDealsType MaxActiveDealsType { get; set; }
		public int MaxActiveDeals { get; set; }
		private PositionSide side => PositionSide.Short;

		public ShortBot() : this("", "")
		{

		}

		public ShortBot(string name) : this(name, "")
		{

		}

		public ShortBot(string name, string description)
		{
			Name = name;
			Description = description;
		}
		#endregion

		private static readonly SemaphoreSlim semaphore = new(1, 1);
		public async Task EvaluateOpen()
		{
			try
			{
				foreach (var pairQuote in Common.PairQuotes)
				{
					var symbol = pairQuote.Symbol;
					var c0 = pairQuote.Charts[^1]; // 현재 정보
					var c1 = pairQuote.Charts[^2]; // 1봉전 정보
					var c2 = pairQuote.Charts[^3]; // 2봉전 정보
					var c3 = pairQuote.Charts[^4]; // 3봉전 정보
					var c4 = pairQuote.Charts[^5]; // 4봉전 정보

					if (c0.Quote.Date.Hour != DateTime.UtcNow.Hour) // 차트 시간과 현재 시간의 동기화 실패
					{
						continue;
					}

					if (!Common.IsShortPositioning(symbol)) // 포지션이 없으면
					{
						await semaphore.WaitAsync();
						try
						{
							if (MaxActiveDealsType == MaxActiveDealsType.Each && Common.GetShortPositionCount() >= MaxActiveDeals)
							{
								continue;
							}
							if (MaxActiveDealsType == MaxActiveDealsType.Total && Common.GetLongPositionCount() + Common.GetShortPositionCount() >= MaxActiveDeals)
							{
								continue;
							}

							if (c1.CandlestickType == CandlestickType.Bullish
								&& c2.CandlestickType == CandlestickType.Bullish
								&& c3.CandlestickType == CandlestickType.Bullish
								&& c1.BodyLength > 0.05m
								&& c2.BodyLength > 0.05m
								&& c3.BodyLength > 0.05m
								&& !Common.IsBannedPosition(symbol, side, DateTime.UtcNow))
							{
								if (Common.IsCoolTime(symbol, side))
								{
									continue;
								}

								if (!Common.IsAdmin)
								{
									await Task.Delay(1000);
								}

								var price = c0.Quote.Close;
								var quantity = (BaseOrderSize / price).ToValidQuantity(symbol);
								await OpenSell(symbol, price, quantity).ConfigureAwait(false);
								Common.AddPositionCoolTime(symbol, side);
							}
						}
						catch (Exception ex)
						{
							Logger.Log(nameof(ShortBot), MethodBase.GetCurrentMethod()?.Name, ex);
						}
						finally
						{
							semaphore.Release();
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(ShortBot), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

		public async Task EvaluateClose()
		{
			try
			{
				foreach (var pairQuote in Common.PairQuotes)
				{
					var symbol = pairQuote.Symbol;
					var c0 = pairQuote.Charts[^1]; // 현재 정보
					var c1 = pairQuote.Charts[^2]; // 1봉전 정보
					var c2 = pairQuote.Charts[^3]; // 2봉전 정보

					if (c0.Quote.Date.Hour != DateTime.UtcNow.Hour) // 차트 시간과 현재 시간의 동기화 실패
					{
						continue;
					}

					if (Common.IsShortPositioning(symbol))
					{
						await semaphore.WaitAsync();
						try
						{
							if (
								c1.CandlestickType == CandlestickType.Bearish
								&& c2.CandlestickType == CandlestickType.Bearish
								&& c1.BodyLength + c2.BodyLength >= 0.05m // 몸통 2개 합쳐서 0.05%가 넘어야함
								)
							{
								if (Common.IsCoolTime(symbol, side))
								{
									continue;
								}

								if (!Common.IsAdmin)
								{
									await Task.Delay(1000);
								}

								var position = Common.GetPosition(symbol, side);
								if (position == null)
								{
									continue;
								}

								var price = c0.Quote.Close;
								var quantity = Math.Abs(position.Quantity);

								// 손실이 -8%를 넘어가면 24시간 블랙리스트 등록
								// 체결이 되던지 안되던지 상관없이 차트 상에서 8% 이상 움직였으면 블랙리스트 등록
								if (position.Roe / position.Leverage <= -8.0m)
								{
									Common.AddBlacklist(symbol, side, DateTime.UtcNow, DateTime.UtcNow.AddHours(23).AddMinutes(30));
								}

								await CloseBuy(symbol, price, quantity).ConfigureAwait(false);
								Common.AddPositionCoolTime(symbol, side);
							}
						}
						catch (Exception ex)
						{
							Logger.Log(nameof(ShortBot), MethodBase.GetCurrentMethod()?.Name, ex);
						}
						finally
						{
							semaphore.Release();
						}

					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(ShortBot), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

		public async Task MonitorOpenOrderTimeout()
		{
			try
			{
				var openOrderResult = BinanceClients.Api.UsdFuturesApi.Trading.GetOpenOrdersAsync();
				openOrderResult.Wait();
				foreach (var order in openOrderResult.Result.Data)
				{
					if ((DateTime.UtcNow - order.CreateTime) >= TimeSpan.FromMinutes(5)) // 5분이 넘도록 체결이 안되면 주문 취소
					{
						var result = await BinanceClients.Api.UsdFuturesApi.Trading.CancelOrderAsync(order.Symbol, order.Id).ConfigureAwait(false);
						if (result.Success)
						{
							Common.AddHistory("Short Bot", $"Cancel Order {order.Symbol}, {order.Id}");
						}
						else
						{
							Common.AddHistory("Short Bot", $"Cancel Order {order.Symbol}, Error: {result.Error?.Message}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(ShortBot), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

		public async Task<bool> OpenSell(string symbol, decimal price, decimal quantity)
		{
			try
			{
				await BinanceClients.Api.UsdFuturesApi.Account.ChangeInitialLeverageAsync(symbol, Leverage); // 레버리지 설정
				var limitPrice = price;
				var errorString = string.Empty;
				for (int i = 1; i <= 25; i++) // 지정가 주문이 성공할 떄까지 주문금액을 계속 높힘(0.02% ~ 0.5%)
				{
					var result = await BinanceClients.OpenSell(symbol, limitPrice, quantity).ConfigureAwait(false);
					if (result.Success)
					{
						Common.AddHistory("Short Bot", $"Open Sell {symbol}, {limitPrice}, {quantity} ({i})");
						if (Common.IsSound)
						{
							Sound.Play("Resources/entry.wav", 0.5);
						}
						return true;
					}
					else
					{
						errorString = result.Error?.Message ?? string.Empty;
					}

					limitPrice = limitPrice.ToUpTickPricePercent(symbol, 0.02m);
				}
				Common.AddHistory("Short Bot", $"Open Sell {symbol}, {errorString}");
				return false;
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(ShortBot), MethodBase.GetCurrentMethod()?.Name, ex);
				return false;
			}
		}

		public async Task<bool> CloseBuy(string symbol, decimal price, decimal quantity)
		{
			try
			{
				//var limitPrice = price;
				var errorString = string.Empty;

				var result = await BinanceClients.CloseBuyMarket(symbol, quantity).ConfigureAwait(false);
				if (result.Success)
				{
					Common.AddHistory("Short Bot", $"Close Buy Market {symbol}, {quantity}");
					return true;
				}
				else
				{
					errorString = result.Error?.Message ?? string.Empty;
				}

				//for (int i = 1; i <= 25; i++) // 지정가 주문이 성공할 떄까지 주문금액을 계속 높힘(0.02% ~ 0.5%)
				//{
				//	var result = await BinanceClients.CloseBuy(symbol, limitPrice, quantity).ConfigureAwait(false);
				//	if (result.Success)
				//	{
				//		Common.AddHistory("Short Bot", $"Close Buy {symbol}, {limitPrice}, {quantity} ({i})");
				//		return true;
				//	}
				//	else
				//	{
				//		errorString = result.Error?.Message ?? string.Empty;
				//	}

				//	limitPrice = limitPrice.ToDownTickPricePercent(symbol, 0.02m);
				//}
				Common.AddHistory("Short Bot", $"Close Buy Market {symbol}, {errorString}");
				return false;
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(ShortBot), MethodBase.GetCurrentMethod()?.Name, ex);
				return false;
			}
		}

		public async Task SetTakeProfit(string symbol, decimal price, decimal quantity)
		{
			try
			{
				var takePrice = price.ToValidPrice(symbol);
				var limitPrice = Calculator.TargetPrice(side, price, -1m).ToValidPrice(symbol); // +0 ~ +1%

				var result = await BinanceClients.SetShortTakeProfit(symbol, limitPrice, quantity, takePrice).ConfigureAwait(false);
				if (result.Success)
				{
					Common.AddHistory("Short Bot", $"Set Take Profit {symbol}, {price}, {quantity}");
				}
				else
				{
					Common.AddHistory("Short Bot", $"Set Take Profit {symbol}, Error: {result.Error?.Message}");
				}
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(ShortBot), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

		public async Task SetStopLoss(string symbol, decimal price, decimal quantity)
		{
			try
			{
				var stopPrice = price.ToValidPrice(symbol);
				var limitPrice = Calculator.TargetPrice(side, price, -1m).ToValidPrice(symbol); // +0 ~ +1%

				var result = await BinanceClients.SetShortStopLoss(symbol, limitPrice, quantity, stopPrice).ConfigureAwait(false);
				if (result.Success)
				{
					Common.AddHistory("Short Bot", $"Set Stop Loss {symbol}, {price}, {quantity}");
				}
				else
				{
					Common.AddHistory("Short Bot", $"Set Stop Loss {symbol}, Error: {result.Error?.Message}");
				}
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(ShortBot), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}
	}
}
