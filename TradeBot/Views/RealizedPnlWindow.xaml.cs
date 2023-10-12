using TradeBot.Models;

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace TradeBot.Views
{
    /// <summary>
    /// RealizedPnlWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RealizedPnlWindow : Window
    {
        public RealizedPnlWindow()
        {
            InitializeComponent();
        }

        public void Init(IEnumerable<BinanceRealizedPnlHistory> histories)
        {
            RealizedPnlDataGrid.ItemsSource = histories;

            DealCountText.Text = $"{histories.Count():#,###}";
            var totalPnl = histories.Sum(x => x.RealizedPnl);
            TotalPnlText.Text = totalPnl >= 0 ? $"+{totalPnl:N}" : $"{totalPnl:N}";
            TotalPnlText.Foreground = totalPnl >= 0 ? new SolidColorBrush(Color.FromRgb(14, 203, 129)) : new SolidColorBrush(Color.FromRgb(246, 70, 93));
        }
    }
}
