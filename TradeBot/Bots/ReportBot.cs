using Binance.Net.Enums;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

using TradeBot.Clients;
using TradeBot.Models;
using TradeBot.Systems;

namespace TradeBot.Bots
{
	/// <summary>
	/// 커미션, 레퍼럴, 실현수익 IncomeHistory UsdFuturesApi.Account.GetIncomeHistory()
	/// 거래내역 TradeHistory BinanceRestApi.GetFuturesTradeHistory()
	/// 일별 성과 DailyHistory
	/// </summary>
	public class ReportBot : Bot
	{
		public static string IncomeReportFileName => $"Logs/{DateTime.Today.Year}_income.csv";
		public static string TradeReportFileName => $"Logs/{DateTime.Today.Year}_trade.csv";
		public static string OrderReportFileName => $"Logs/{DateTime.Today.Year}_order.csv";
		public static string DailyReportFileName => $"Logs/daily.csv";
		public static string BotReportFileName => $"Logs/bot_report.log";

		private static readonly char[] botReportSeparator = ['[', ']'];

		public ReportBot() : this("", "")
		{

		}

		public ReportBot(string name) : this(name, "")
		{

		}

		public ReportBot(string name, string description)
		{
			Name = name;
			Description = description;
		}

		public void Init()
		{
			if (!File.Exists(IncomeReportFileName))
			{
				File.WriteAllText(IncomeReportFileName, "");
			}

			if (!File.Exists(TradeReportFileName))
			{
				File.WriteAllText(TradeReportFileName, "");
			}

			if (!File.Exists(OrderReportFileName))
			{
				File.WriteAllText(OrderReportFileName, "");
			}

			if (!File.Exists(DailyReportFileName))
			{
				File.WriteAllText(DailyReportFileName, "");
			}
		}

		public IEnumerable<BinanceIncomeHistory> ReadIncomeReport()
		{
			var result = new List<BinanceIncomeHistory>();
			var files = Directory.GetFiles("Logs", "*_income.csv").OrderBy(x => x);

			foreach (var file in files)
			{
				var lines = File.ReadAllLines(file);
				if (lines == null || lines.Length == 0)
				{
					continue;
				}

				foreach (var line in lines)
				{
					result.Add(new BinanceIncomeHistory(line));
				}
			}

			return result;
		}

		public IEnumerable<BinanceTradeHistory> ReadTradeReport()
		{
			var result = new List<BinanceTradeHistory>();
			var files = Directory.GetFiles("Logs", "*_trade.csv").OrderBy(x => x);

			foreach (var file in files)
			{
				var lines = File.ReadAllLines(file);
				if (lines == null || lines.Length == 0)
				{
					continue;
				}

				foreach (var line in lines)
				{
					result.Add(new BinanceTradeHistory(line));
				}
			}

			return result;
		}

		public IEnumerable<BinanceOrderHistory> ReadOrderReport()
		{
			var result = new List<BinanceOrderHistory>();
			var files = Directory.GetFiles("Logs", "*_order.csv").OrderBy(x => x);

			foreach (var file in files)
			{
				var lines = File.ReadAllLines(file);
				if (lines == null || lines.Length == 0)
				{
					continue;
				}

				foreach (var line in lines)
				{
					result.Add(new BinanceOrderHistory(line));
				}
			}

			return result;
		}

		public IEnumerable<BotReportHistory> ReadBotReportReport()
		{
			var result = new List<BotReportHistory>();
			var lines = File.ReadAllLines(BotReportFileName);
			if (lines == null || lines.Length == 0)
			{
				return result;
			}

			foreach (var line in lines)
			{
				result.Add(new BotReportHistory(line));
			}

			return result;
		}

		public IEnumerable<BinanceDailyHistory> ReadDailyReport()
		{
			var result = new List<BinanceDailyHistory>();
			var lines = File.ReadAllLines(DailyReportFileName);
			if (lines == null || lines.Length == 0)
			{
				return result;
			}

			foreach (var line in lines)
			{
				result.Add(new BinanceDailyHistory(line));
			}

			return result;
		}

		public IEnumerable<BinanceIncomeHistory> GetPnlReport(DateTime startTime, DateTime endTime)
		{
			var result = new List<BinanceIncomeHistory>();
			var files = Directory.GetFiles("Logs", "*_income.csv").OrderBy(x => x);

			foreach (var file in files)
			{
				var lines = File.ReadAllLines(file);
				if (lines == null || lines.Length == 0)
				{
					continue;
				}

				foreach (var line in lines)
				{
					var income = new BinanceIncomeHistory(line);
					if(income.Time >= startTime && income.Time < endTime && income.IncomeType == IncomeType.RealizedPnl)
					{
						result.Add(income);
					}
				}
			}

			return result;
		}

		public async Task WriteIncomeReport(ProgressBar progressBar)
		{
			var utcNow = DateTime.UtcNow;
			var latestFile = Directory.GetFiles("Logs", "*_income.csv").OrderByDescending(x => x).ElementAt(0);
			var lines = File.ReadAllLines(latestFile);

			var latestIncomeTime = lines == null || lines.Length == 0 ? new DateTime(2024, 8, 26) : new BinanceIncomeHistory(lines[^1]).Time.AddSeconds(1);
			//var latestIncomeTransactionId = lines == null || lines.Length == 0 ? "" : new BinanceIncomeHistory(lines[^1]).TransactionId;

			DispatcherService.Invoke(() =>
			{
				var iterationCount = (int)((utcNow - latestIncomeTime).TotalHours / 6 + 1);
				progressBar.Minimum = 0;
				progressBar.Maximum = iterationCount;
				progressBar.Value = 0;
			});

			var builder = new StringBuilder();
			while (latestIncomeTime < utcNow.AddMinutes(-1))
			{
				var startTime = latestIncomeTime;
				var endTime = (latestIncomeTime.AddHours(6) > utcNow) ? utcNow : latestIncomeTime.AddHours(6);

				var result = await BinanceClients.Api.UsdFuturesApi.Account.GetIncomeHistoryAsync(null, null, startTime, endTime, 1000).ConfigureAwait(false);
				var incomes = result.Data.Select(x => new BinanceIncomeHistory(x.Timestamp, x.TransactionId, x.Symbol, x.IncomeType, x.Income, x.Asset, x.Info));

				foreach (var income in incomes)
				{
					builder.AppendLine(income.ToString());
				}

				latestIncomeTime = endTime;
				DispatcherService.Invoke(() =>
				{
					progressBar.Value++;
				});
				await Task.Delay(500);
			}

			File.AppendAllText(IncomeReportFileName, builder.ToString());
		}

		/// <summary>
		/// 거래내역 가져오기
		/// </summary>
		/// <returns></returns>
		public async Task WriteTradeReport(ProgressBar progressBar)
		{
			var utcNow = DateTime.UtcNow;
			var latestFile = Directory.GetFiles("Logs", "*_trade.csv").OrderByDescending(x => x).ElementAt(0);
			var lines = File.ReadAllLines(latestFile);

			var latestTradeTime = lines == null || lines.Length == 0 ? new DateTime(2024, 8, 26) : new BinanceTradeHistory(lines[^1]).Time.AddSeconds(1);
			//var latestIncomeTransactionId = lines == null || lines.Length == 0 ? "" : new BinanceIncomeHistory(lines[^1]).TransactionId;

			DispatcherService.Invoke(() =>
			{
				var iterationCount = (int)((utcNow - latestTradeTime).TotalDays / 7 + 1) * AllSymbols.Count;
				progressBar.Minimum = 0;
				progressBar.Maximum = iterationCount;
				progressBar.Value = 0;
			});
			
			var tradeResult = new List<BinanceTradeHistory>();
			var builder = new StringBuilder();
			while (latestTradeTime < utcNow.AddMinutes(-1))
			{
				var startTime = latestTradeTime;
				var endTime = (latestTradeTime.AddDays(7) > utcNow) ? utcNow : latestTradeTime.AddDays(7);

				foreach (var symbol in AllSymbols)
				{
					var userTrades = await BinanceClients.Api.UsdFuturesApi.Trading.GetUserTradesAsync(symbol, startTime, endTime, 1000).ConfigureAwait(false);
					if (userTrades == null || userTrades.Data == null || !userTrades.Data.Any())
					{
						continue;
					}
					var trades = userTrades.Data.Select(x => new BinanceTradeHistory(
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
					tradeResult.AddRange(trades);
					DispatcherService.Invoke(() =>
					{
						progressBar.Value++;
					});
					await Task.Delay(500);
				}

				latestTradeTime = endTime;
			}

			tradeResult = [.. tradeResult.OrderBy(x => x.Time)];

			foreach (var trade in tradeResult)
			{
				builder.AppendLine(trade.ToString());
			}

			File.AppendAllText(TradeReportFileName, builder.ToString());
		}

		/// <summary>
		/// 주문내역 가져오기
		/// </summary>
		/// <param name="progressBar"></param>
		/// <returns></returns>
		public async Task WriteOrderReport(ProgressBar progressBar)
		{
			var utcNow = DateTime.UtcNow;
			var latestFile = Directory.GetFiles("Logs", "*_order.csv").OrderByDescending(x => x).ElementAt(0);
			var lines = File.ReadAllLines(latestFile);

			var latestOrderTime = lines == null || lines.Length == 0 ? new DateTime(2024, 8, 8) : new BinanceOrderHistory(lines[^1]).Time.AddSeconds(1);
			//var latestIncomeTransactionId = lines == null || lines.Length == 0 ? "" : new BinanceIncomeHistory(lines[^1]).TransactionId;

			DispatcherService.Invoke(() =>
			{
				var iterationCount = (int)((utcNow - latestOrderTime).TotalDays / 7 + 1) * AllSymbols.Count;
				progressBar.Minimum = 0;
				progressBar.Maximum = iterationCount;
				progressBar.Value = 0;
			});

			var orderResult = new List<BinanceOrderHistory>();
			var builder = new StringBuilder();
			while (latestOrderTime < utcNow.AddMinutes(-1))
			{
				var startTime = latestOrderTime;
				var endTime = (latestOrderTime.AddDays(7) > utcNow) ? utcNow : latestOrderTime.AddDays(7);

				foreach (var symbol in AllSymbols)
				{
					var _orders = await BinanceClients.Api.UsdFuturesApi.Trading.GetOrdersAsync(symbol, null, startTime, endTime, 1000).ConfigureAwait(false);
					if (_orders == null || _orders.Data == null || !_orders.Data.Any())
					{
						continue;
					}
					var orders = _orders.Data.Select(x => new BinanceOrderHistory(
						x.CreateTime,
						x.UpdateTime,
						x.Symbol,
						x.PositionSide,
						x.Side,
						x.Price,
						x.Quantity,
						x.QuantityFilled,
						x.QuoteQuantityFilled,
						x.Status
						)
					);
					orderResult.AddRange(orders);
					DispatcherService.Invoke(() =>
					{
						progressBar.Value++;
					});
					await Task.Delay(500);
				}

				latestOrderTime = endTime;
			}

			orderResult = [.. orderResult.OrderBy(x => x.Time)];

			foreach (var order in orderResult)
			{
				builder.AppendLine(order.ToString());
			}

			File.AppendAllText(OrderReportFileName, builder.ToString());
		}

		public void WriteDailyReport()
		{
			var botReports = File.ReadAllLines(BotReportFileName);
			var preEst = 0m;
			var dailys = new List<BinanceDailyHistory>();
			foreach (var line in botReports)
			{
				var parts = line.Split(botReportSeparator, StringSplitOptions.RemoveEmptyEntries);
				var time = DateTime.Parse(parts[0]);

				if (time.Hour == 0)
				{
					var usdtPart = parts[1].Split(',')[0].Trim();
					var est = decimal.Parse(usdtPart.Replace("USDT", "").Trim());

					if (dailys.Count == 0)
					{
						var change = 0;
						var changePer = 0;
						var maxPer = 1;
						var daily = new BinanceDailyHistory(new DateTime(time.Year, time.Month, time.Day, time.Hour, 0, 0), est, change, changePer, maxPer);
						dailys.Add(daily);
					}
					else
					{
						var change = est - preEst;
						var changePer = change / preEst;
						var maxPer = est / Math.Max(est, dailys.Max(x => x.Estimated));
						var daily = new BinanceDailyHistory(new DateTime(time.Year, time.Month, time.Day, time.Hour, 0, 0), est, change, changePer, maxPer);
						dailys.Add(daily);
					}

					preEst = est;
				}
			}

			var builder = new StringBuilder();
			foreach (var daily in dailys)
			{
				builder.AppendLine(daily.ToString());
			}

			File.WriteAllText(DailyReportFileName, builder.ToString());
		}

		/// <summary>
		/// Make Position History from Trade History
		/// </summary>
		/// <param name="trades"></param>
		/// <returns></returns>
		public IEnumerable<BinancePositionHistory> GetPositionHistory(IEnumerable<BinanceTradeHistory> trades)
		{
			var result = new List<BinancePositionHistory>();

			var tradeCount = trades.Count();
			for (int i = 0; i < tradeCount; i++)
			{
				var trade = trades.ElementAt(i);

				var position = result.Find(x => x.Symbol.Equals(trade.Symbol) && x.PositionSide.Equals(trade.PositionSide) && !x.IsClosed);
				if (position == null)
				{
					result.Add(new BinancePositionHistory(trade.Time, null, trade.Symbol, trade.PositionSide, trade.Price, null, trade.Quantity));
				}
				else
				{
					if (trade.PositionSide == PositionSide.Long && trade.Side == OrderSide.Buy ||
						trade.PositionSide == PositionSide.Short && trade.Side == OrderSide.Sell) // Position Entry
					{
						position.EntryQuantity += trade.Quantity;
					}
					else // Position Exit
					{
						position.ExitTime = trade.Time;
						position.ExitPrice = trade.Price;
						position.ExitQuantity += trade.Quantity;
						position.RealizedPnl += trade.RealizedPnl;
					}
				}
			}

			return result;
		}
	}
}
