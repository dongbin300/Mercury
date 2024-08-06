using Binance.Net.Enums;

using Mercury;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using TradeBot.Clients;
using TradeBot.Models;
using TradeBot.Systems;

namespace TradeBot.Bots
{
    public class ManagerBot : Bot
    {
        private decimal preTotal = 0;
        private decimal preAvbl = 0;
        private decimal preBnb = 0;

        public ManagerBot() : this("", "")
        {

        }

        public ManagerBot(string name) : this(name, "")
        {

        }

        public ManagerBot(string name, string description)
        {
            Name = name;
            Description = description;
        }

        /// <summary>
        /// Get Total Balance(USDT) and Available Balance(USDT) and BNB Balance(BNB)
        /// </summary>
        /// <returns></returns>
        public async Task<(decimal, decimal, decimal)> GetBinanceBalance()
        {
            try
            {
                var result = await BinanceClients.Api.UsdFuturesApi.Account.GetBalancesAsync().ConfigureAwait(false);
                var balance = result.Data;
                var usdtBalance = balance.First(b => b.Asset.Equals("USDT"));
                var usdt = usdtBalance.WalletBalance + usdtBalance.CrossUnrealizedPnl;
                var availableUsdt = Math.Round(usdtBalance.AvailableBalance, 3);
                var total = Math.Round(usdt, 3);
                var bnb = Math.Round(balance.First(b => b.Asset.Equals("BNB")).WalletBalance, 4);

                preTotal = total;
                preAvbl = availableUsdt;
                preBnb = bnb;

                return (total, availableUsdt, bnb);
            }
            catch
            {
                return (preTotal, preAvbl, preBnb);
            }
        }

        public async Task GetBinancePositions()
        {
            try
            {
                var result = await BinanceClients.Api.UsdFuturesApi.Account.GetPositionInformationAsync().ConfigureAwait(false);
                if (result.Data == null)
                {
                    return;
                }

                var positions = result.Data.Where(r => r.Quantity != 0);
                Common.Positions = [.. positions.Select(p => new BinancePosition(
                    p.Symbol,
                    p.PositionSide.ToString(),
                    p.UnrealizedPnl,
                    p.EntryPrice,
                    p.MarkPrice,
                    p.Quantity,
                    p.Leverage
                    )).OrderByDescending(x => x.Pnl)];
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(ManagerBot), MethodBase.GetCurrentMethod()?.Name, ex);
            }
        }

        public async Task GetBinanceOpenOrders()
        {
            try
            {
                var result = await BinanceClients.Api.UsdFuturesApi.Trading.GetOpenOrdersAsync().ConfigureAwait(false);
                if (result.Data == null)
                {
                    return;
                }

				Common.OpenOrders = result.Data.Select(x => new BinanceOrder(x.Id, x.Symbol, x.PositionSide, x.Type, x.Quantity, x.CreateTime)).ToList();
			}
			catch (Exception ex)
            {
                Logger.Log(nameof(ManagerBot), MethodBase.GetCurrentMethod()?.Name, ex);
            }
        }

        public async Task<IEnumerable<BinanceRealizedPnlHistory>> GetBinanceTodayRealizedPnlHistory()
        {
            try
            {
                var result = await BinanceClients.Api.UsdFuturesApi.Account.GetIncomeHistoryAsync(null, "REALIZED_PNL", DateTime.UtcNow.Date, null, 1000).ConfigureAwait(false);
                var data = result.Data;
                return data.Select(d => new BinanceRealizedPnlHistory(
                    d.Timestamp,
                    d.Symbol ?? string.Empty,
                    d.Income
                    ));
            }
            catch
            {
                return default!;
            }
        }

        public async Task GetAllKlines(int limit)
        {
            try
            {
                foreach (var symbol in MonitorSymbols)
                {
                    var result = await BinanceClients.Api.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, Common.BaseInterval, null, null, limit).ConfigureAwait(false);
                    var quotes = result.Data.Select(x => new Quote(
                        x.OpenTime,
                        x.OpenPrice,
                        x.HighPrice,
                        x.LowPrice,
                        x.ClosePrice,
                        x.Volume
                    ));

                    Common.PairQuotes.Add(new PairQuote(symbol, quotes));
                }

                Common.AddHistory("Manager Bot", "Get All Klines Complete");
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(ManagerBot), MethodBase.GetCurrentMethod()?.Name, ex);
            }
        }

        public async Task StartBinanceFuturesTicker()
        {
            try
            {
                foreach (var symbol in MonitorSymbols)
                {
                    await BinanceClients.Socket.UsdFuturesApi.SubscribeToKlineUpdatesAsync(symbol, Common.BaseInterval, (obj) =>
                    {
                        var data = obj.Data.Data;
                        var pairQuote = Common.PairQuotes.Find(x => x.Symbol.Equals(symbol));
                        var quote = new Quote(
                            data.OpenTime,
                            data.OpenPrice,
                            data.HighPrice,
                            data.LowPrice,
                            data.ClosePrice,
                            data.Volume
                        );

                        pairQuote?.UpdateQuote(quote);
                        pairQuote?.UpdateIndicators();
                    }).ConfigureAwait(false);
                }

                Common.AddHistory("Manager Bot", "Start Binance Futures Ticker Complete");
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(ManagerBot), MethodBase.GetCurrentMethod()?.Name, ex);
            }
        }

        public async Task StopBinanceFuturesSubscriptions()
        {
            try
            {
                await BinanceClients.Socket.UsdFuturesApi.UnsubscribeAllAsync().ConfigureAwait(false);

                Common.AddHistory("Manager Bot", "Stop Binance Futures Ticker Complete");
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(ManagerBot), MethodBase.GetCurrentMethod()?.Name, ex);
            }
        }

  //      public async Task CancelAllOpenOrders()
  //      {
  //          try
  //          {
				
		//	}
		//	catch (Exception ex)
		//	{
		//		Logger.Log(nameof(ManagerBot), MethodBase.GetCurrentMethod()?.Name, ex);
		//	}
		//}

		/// <summary>
		/// 딜이 완료된 포지션에서 주문취소가 안된 주문 찾아서 취소
		/// </summary>
		/// <returns></returns>
		public async Task MonitorOpenOrderClosedDeal()
        {
            try
            {
                var result = await BinanceClients.Api.UsdFuturesApi.Trading.GetOpenOrdersAsync().ConfigureAwait(false);

                if (result.Data == null)
                {
                    return;
                }

                Common.OpenOrders = result.Data.Select(x => new BinanceOrder(x.Id, x.Symbol, x.PositionSide, x.Type, x.Quantity, x.CreateTime)).ToList();

                foreach (var order in Common.OpenOrders)
                {
                    // 해당 주문의 생성시간이 10초가 안 지났으면 스킵
                    if ((DateTime.UtcNow - order.CreateTime).TotalSeconds < 10)
                    {
                        continue;
                    }

                    // 해당 주문이 TP/SL이 아니면 스킵
                    if (order.Type != FuturesOrderType.TakeProfit && order.Type != FuturesOrderType.Stop)
                    {
                        continue;
                    }

                    // 해당 TP/SL 주문의 오리지널 포지션이 존재하면 주문취소하지 않음
                    if (Common.Positions.Any(x => x.Symbol.Equals(order.Symbol) && x.PositionSide.Equals(order.Side.ToString())))
                    {
                        continue;
                    }

                    // 오리지널 포지션이 없으면 주문취소
                    var cancelOrderResult = await BinanceClients.Api.UsdFuturesApi.Trading.CancelOrderAsync(order.Symbol, order.Id).ConfigureAwait(false);
                    if (cancelOrderResult.Success)
                    {
                        // SL 처리되고 TP 남았으면
                        if (order.Type == FuturesOrderType.TakeProfit)
                        {
                            Common.AddHistory("Manager Bot", $"Stop Loss {order.Symbol}");
                            if (Common.IsSound)
                            {
                                Sound.Play("Resources/loss.wav", 0.3);
                            }
                        }
                        // TP 처리되고 SL 남았으면
                        else if (order.Type == FuturesOrderType.Stop)
                        {
                            Common.AddHistory("Manager Bot", $"Take Profit {order.Symbol}");
                            if (Common.IsSound)
                            {
                                Sound.Play("Resources/profit.wav", 0.5);
                            }
                        }
                    }
                    else
                    {
                        Common.AddHistory("Manager Bot", $"Cancel Order {order.Symbol}, Error: {result.Error?.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(nameof(ManagerBot), MethodBase.GetCurrentMethod()?.Name, ex);
            }
        }
    }
}
