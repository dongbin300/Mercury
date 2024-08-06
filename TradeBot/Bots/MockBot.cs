using Binance.Net.Enums;

using Mercury.Backtests;
using Mercury.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using TradeBot.Extensions;
using TradeBot.Systems;

namespace TradeBot.Bots
{
	public class MockBot : Bot
	{
		#region Entry
		public bool IsRunning { get; set; }
		public decimal BaseOrderSize { get; set; }
		public decimal TargetRoe { get; set; }
		public int Leverage { get; set; }
		public int MaxActiveDeals { get; set; }
		public decimal Money { get; set; } = 1_000_000;
		public List<Position> Positions { get; set; } = [];
		public List<Position> PositionHistory { get; set; } = [];
		public List<Position> LongPositions => Positions.Where(x => x.Side == PositionSide.Long).ToList();
		public List<Position> ShortPositions => Positions.Where(x => x.Side == PositionSide.Short).ToList();
		public int LongPositionCount => LongPositions.Count;
		public int ShortPositionCount => ShortPositions.Count;
		public bool IsLongPositioning(string symbol) => LongPositions.Any(x => x.Symbol == symbol);
		public bool IsShortPositioning(string symbol) => ShortPositions.Any(x => x.Symbol == symbol);
		public Position? GetPosition(string symbol, PositionSide side) => Positions.Find(x => x.Symbol == symbol && x.Side == side);

		public MockBot() : this("", "")
		{

		}

		public MockBot(string name) : this(name, "")
		{

		}

		public MockBot(string name, string description)
		{
			Name = name;
			Description = description;
		}
		#endregion

		public async Task Evaluate()
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

					if (c0.Quote.Date.Hour != DateTime.Now.Hour) // 차트 시간과 현재 시간의 동기화 실패
					{
						continue;
					}

					if (!IsLongPositioning(symbol)) // 포지션이 없으면
					{
						if (LongPositionCount + ShortPositionCount >= MaxActiveDeals) // 동시 거래 수 MAX
						{
							continue;
						}

						try
						{
							if (c1.CandlestickType == CandlestickType.Bearish
								&& c2.CandlestickType == CandlestickType.Bearish
								&& c3.CandlestickType == CandlestickType.Bearish
								&& c4.CandlestickType == CandlestickType.Bullish)
							{
								var price = c0.Quote.Close;
								var quantity = (BaseOrderSize / price).ToValidQuantity(symbol);
								if (OpenBuy(symbol, price, quantity))
								{
									if (Common.IsSound)
									{
										Sound.Play("Resources/entry.wav", 0.5);
									}
								}
							}
						}
						catch (Exception ex)
						{
							Logger.Log(nameof(MockBot), MethodBase.GetCurrentMethod()?.Name, ex);
						}
					}
					else // 포지션이 있으면
					{
						if (
							c1.CandlestickType == CandlestickType.Bullish
							&& c2.CandlestickType == CandlestickType.Bullish
							)
						{
							var position = GetPosition(symbol, PositionSide.Long);
							if (position == null)
							{
								continue;
							}

							var price = c0.Quote.Close;
							var quantity = Math.Abs(position.Quantity);
							CloseSell(symbol, price, quantity);
						}
					}


					if (!IsShortPositioning(symbol)) // 포지션이 없으면
					{
						if (LongPositionCount + ShortPositionCount >= MaxActiveDeals) // 동시 거래 수 MAX
						{
							continue;
						}

						try
						{
							if (c1.CandlestickType == CandlestickType.Bullish
								&& c2.CandlestickType == CandlestickType.Bullish
								&& c3.CandlestickType == CandlestickType.Bullish
								&& c4.CandlestickType == CandlestickType.Bearish)
							{
								var price = c0.Quote.Close;
								var quantity = (BaseOrderSize / price).ToValidQuantity(symbol);
								if (OpenSell(symbol, price, quantity))
								{
									if (Common.IsSound)
									{
										Sound.Play("Resources/entry.wav", 0.5);
									}
								}
							}
						}
						catch (Exception ex)
						{
							Logger.Log(nameof(MockBot), MethodBase.GetCurrentMethod()?.Name, ex);
						}
					}
					else // 포지션이 있으면
					{
						if (
							c1.CandlestickType == CandlestickType.Bearish
							&& c2.CandlestickType == CandlestickType.Bearish
							)
						{
							var position = GetPosition(symbol, PositionSide.Short);
							if (position == null)
							{
								continue;
							}

							var price = c0.Quote.Close;
							var quantity = Math.Abs(position.Quantity);
							CloseBuy(symbol, price, quantity);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MockBot), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

		public bool OpenBuy(string symbol, decimal price, decimal quantity)
		{
			try
			{
				var limitPrice = price.ToDownTickPrice(symbol).ToValidPrice(symbol);
				Positions.Add(new Position(DateTime.Now, symbol, PositionSide.Long, limitPrice) { Quantity = quantity, EntryAmount = limitPrice * quantity });
				Common.AddHistory("Mock Bot(Long)", $"Open Buy {symbol}, {limitPrice}, {quantity}");

				return true;
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MockBot), MethodBase.GetCurrentMethod()?.Name, ex);
				return false;
			}
		}

		public void CloseSell(string symbol, decimal price, decimal quantity)
		{
			try
			{
				var limitPrice = price.ToUpTickPrice(symbol).ToValidPrice(symbol);
				var position = GetPosition(symbol, PositionSide.Long);
				if (position == null)
				{
					return;
				}
				position.ExitAmount = limitPrice * position.Quantity;
				PositionHistory.Add(position);
				Positions.Remove(position);
				Common.AddHistory("Mock Bot(Long)", $"Close Sell {symbol}, {limitPrice}, {quantity}");
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MockBot), MethodBase.GetCurrentMethod()?.Name, ex);
			}

		}
		public bool OpenSell(string symbol, decimal price, decimal quantity)
		{
			try
			{
				var limitPrice = price.ToUpTickPrice(symbol).ToValidPrice(symbol);
				Positions.Add(new Position(DateTime.Now, symbol, PositionSide.Short, limitPrice) { Quantity = quantity, EntryAmount = limitPrice * quantity });
				Common.AddHistory("Mock Bot(Short)", $"Open Sell {symbol}, {limitPrice}, {quantity}");
				return true;
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MockBot), MethodBase.GetCurrentMethod()?.Name, ex);
				return false;
			}
		}

		public void CloseBuy(string symbol, decimal price, decimal quantity)
		{
			try
			{
				var limitPrice = price.ToDownTickPrice(symbol).ToValidPrice(symbol);
				var position = GetPosition(symbol, PositionSide.Short);
				if (position == null)
				{
					return;
				}
				position.ExitAmount = limitPrice * position.Quantity;
				PositionHistory.Add(position);
				Positions.Remove(position);
				Common.AddHistory("Mock Bot(Short)", $"Close Buy {symbol}, {limitPrice}, {quantity}");
			}
			catch (Exception ex)
			{
				Logger.Log(nameof(MockBot), MethodBase.GetCurrentMethod()?.Name, ex);
			}
		}

		//public async Task SetTakeProfit(string symbol, decimal price, decimal quantity)
		//{
		//    try
		//    {
		//        var takePrice = price.ToValidPrice(symbol);
		//        var limitPrice = Calculator.TargetPrice(side, price, -1m).ToValidPrice(symbol); // -0 ~ -1%

		//        var result = await BinanceClients.SetLongTakeProfit(symbol, limitPrice, quantity, takePrice).ConfigureAwait(false);
		//        if (result.Success)
		//        {
		//            Common.AddHistory("Long Bot", $"Set Take Profit {symbol}, {price}, {quantity}");
		//        }
		//        else
		//        {
		//            Common.AddHistory("Long Bot", $"Set Take Profit {symbol}, Error: {result.Error?.Message}");
		//        }
		//    }
		//    catch (Exception ex)
		//    {
		//        Logger.Log(nameof(MockBot), MethodBase.GetCurrentMethod()?.Name, ex);
		//    }
		//}

		//public async Task SetStopLoss(string symbol, decimal price, decimal quantity)
		//{
		//    try
		//    {
		//        var stopPrice = price.ToValidPrice(symbol);
		//        var limitPrice = Calculator.TargetPrice(side, price, -1m).ToValidPrice(symbol); // -0 ~ -1%

		//        var result = await BinanceClients.SetLongStopLoss(symbol, limitPrice, quantity, stopPrice).ConfigureAwait(false);
		//        if (result.Success)
		//        {
		//            Common.AddHistory("Long Bot", $"Set Stop Loss {symbol}, {price}, {quantity}");
		//        }
		//        else
		//        {
		//            Common.AddHistory("Long Bot", $"Set Stop Loss {symbol}, Error: {result.Error?.Message}");
		//        }
		//    }
		//    catch (Exception ex)
		//    {
		//        Logger.Log(nameof(MockBot), MethodBase.GetCurrentMethod()?.Name, ex);
		//    }
		//}
	}
}