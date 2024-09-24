using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

using TradeBot.Bots;

namespace TradeBot.Views
{
	/// <summary>
	/// DebugWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class DebugWindow : Window
	{
		public class DebugItem
		{
			public string Name { get; set; } = string.Empty;
			public string Value { get; set; } = string.Empty;
		}

		DispatcherTimer timer = default!;
		List<DebugItem> items = [];
		public LongBot LongBot = default!;
		public ShortBot ShortBot = default!;

		public DebugWindow()
		{
			InitializeComponent();

			timer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(1000)
			};
			timer.Tick += Timer_Tick;
			timer.Start();
		}

		private void Add(string name, object value)
		{
			items.Add(new DebugItem { Name = name, Value = value.ToString() ?? string.Empty });
		}

		private void Timer_Tick(object? sender, EventArgs e)
		{
			try
			{
				items.Clear();

				Add("Settings.BaseOrderSize", Settings.Default.BaseOrderSize);
				Add("Settings.Leverage", Settings.Default.Leverage);
				Add("Settings.MaxActiveDeals", Settings.Default.MaxActiveDeals);
				Add("Settings.MaxActiveDealsTypeIndex", Settings.Default.MaxActiveDealsTypeIndex);
				Add("Settings.ThemeColor", Settings.Default.ThemeColor);
				Add("Settings.ForegroundColor", Settings.Default.ForegroundColor);
				Add("Settings.BackgroundColor", Settings.Default.BackgroundColor);
				Add("Settings.LongColor", Settings.Default.LongColor);
				Add("Settings.ShortColor", Settings.Default.ShortColor);
				for (int i = 0; i < Common.PairQuotes.Count; i++)
				{
					try
					{
						var pairQuote = Common.PairQuotes[i];
						Add($"Common.PairQuotes[{i}].Symbol", pairQuote.Symbol);
						Add($"Common.PairQuotes[{i}].Chart[^1]", pairQuote.Charts[^1].ToDebugString());
						Add($"Common.PairQuotes[{i}].Chart[^2]", pairQuote.Charts[^2].ToDebugString());
						Add($"Common.PairQuotes[{i}].Chart[^3]", pairQuote.Charts[^3].ToDebugString());
						Add($"Common.PairQuotes[{i}].Chart[^4]", pairQuote.Charts[^4].ToDebugString());
						Add($"Common.PairQuotes[{i}].Chart[^5]", pairQuote.Charts[^5].ToDebugString());
					}
					catch
					{
					}
				}
				for (int i = 0; i < Common.Positions.Count; i++)
				{
					var position = Common.Positions[i];
					Add($"Common.Positions[{i}].Symbol", position.Symbol);
					Add($"Common.Positions[{i}].PositionSide", position.PositionSide);
					Add($"Common.Positions[{i}].Pnl", position.Pnl);
					Add($"Common.Positions[{i}].Quantity", position.Quantity);
					Add($"Common.Positions[{i}].Margin", position.Margin);
				}
				for (int i = 0; i < Common.OpenOrders.Count; i++)
				{
					var openOrder = Common.OpenOrders[i];
					Add($"Common.OpenOrders[{i}].Id", openOrder.Id);
					Add($"Common.OpenOrders[{i}].Symbol", openOrder.Symbol);
					Add($"Common.OpenOrders[{i}].Side", openOrder.Side);
					Add($"Common.OpenOrders[{i}].CreateTime", openOrder.CreateTime);
					Add($"Common.OpenOrders[{i}].Type", openOrder.Type);
					Add($"Common.OpenOrders[{i}].Quantity", openOrder.Quantity);
					Add($"Common.OpenOrders[{i}].Price", openOrder.Price);
					Add($"Common.OpenOrders[{i}].QuantityFilled", openOrder.QuantityFilled);
				}
				for (int i = 0; i < Common.PositionCoolTimes.Count; i++)
				{
					var positionCoolTime = Common.PositionCoolTimes[i];
					Add($"Common.PositionCoolTimes[{i}].Symbol", positionCoolTime.Symbol);
					Add($"Common.PositionCoolTimes[{i}].Side", positionCoolTime.Side);
					Add($"Common.PositionCoolTimes[{i}].LatestEntryTime", positionCoolTime.LatestEntryTime);
				}
				Add("LongBot.Name", LongBot.Name);
				Add("LongBot.Description", LongBot.Description);
				Add("LongBot.IsRunning", LongBot.IsRunning);
				Add("LongBot.BaseOrderSize", LongBot.BaseOrderSize);
				Add("LongBot.Leverage", LongBot.Leverage);
				Add("LongBot.MaxActiveDealsType", LongBot.MaxActiveDealsType);
				Add("LongBot.MaxActiveDeals", LongBot.MaxActiveDeals);

				Add("ShortBot.Name", ShortBot.Name);
				Add("ShortBot.Description", ShortBot.Description);
				Add("ShortBot.IsRunning", ShortBot.IsRunning);
				Add("ShortBot.BaseOrderSize", ShortBot.BaseOrderSize);
				Add("ShortBot.Leverage", ShortBot.Leverage);
				Add("ShortBot.MaxActiveDealsType", ShortBot.MaxActiveDealsType);
				Add("ShortBot.MaxActiveDeals", ShortBot.MaxActiveDeals);
				for (int i = 0; i < Common.BlacklistPositions.Count; i++)
				{
					var blacklistPosition = Common.BlacklistPositions[i];
					Add($"Common.BlacklistPositions[{i}].Symbol", blacklistPosition.Symbol);
					Add($"Common.BlacklistPositions[{i}].Side", blacklistPosition.Side);
					Add($"Common.BlacklistPositions[{i}].TriggerTime", blacklistPosition.TriggerTime);
					Add($"Common.BlacklistPositions[{i}].ReleaseTime", blacklistPosition.ReleaseTime);
				}

				DebugDataGrid.ItemsSource = null;
				DebugDataGrid.ItemsSource = items;
			}
			catch
			{
			}
		}
	}
}
