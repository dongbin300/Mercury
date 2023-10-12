using MarinerX.Apis;
using MarinerX.Utils;

using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace MarinerX.Views
{
    /// <summary>
    /// PositionMonitorView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PositionMonitorView : Window
    {
        private string symbol = string.Empty;
        private int interval = 15;
        private System.Timers.Timer timer = new ();

        #region Windows API
        /// <summary>
        /// Window handles (HWND) used for hWndInsertAfter
        /// </summary>
        public static class HWND
        {
            public static IntPtr
            NoTopMost = new(-2),
            TopMost = new(-1),
            Top = new(0),
            Bottom = new(1);
        }

        /// <summary>
        /// SetWindowPos Flags
        /// </summary>
        public static class SWP
        {
            public static readonly uint
            NOSIZE = 0x0001,
            NOMOVE = 0x0002,
            NOZORDER = 0x0004,
            NOREDRAW = 0x0008,
            NOACTIVATE = 0x0010,
            DRAWFRAME = 0x0020,
            FRAMECHANGED = 0x0020,
            SHOWWINDOW = 0x0040,
            HIDEWINDOW = 0x0080,
            NOCOPYBITS = 0x0100,
            NOOWNERZORDER = 0x0200,
            NOREPOSITION = 0x0200,
            NOSENDCHANGING = 0x0400,
            DEFERERASE = 0x2000,
            ASYNCWINDOWPOS = 0x4000;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
        #endregion

        public PositionMonitorView()
        {
            InitializeComponent();
            Left = 0;
            Top = 0;

            var hWnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hWnd, HWND.TopMost, 0, 0, 0, 0, SWP.NOMOVE | SWP.NOSIZE);
        }

        public void Init(string symbol, int interval)
        {
            this.symbol = symbol;
            this.interval = interval;

            SymbolText.Text = symbol;
            timer.Interval = interval * 1000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            var info = BinanceRestApi.GetPositioningInformation(symbol);

            if (info.Count == 0)
            {
                DispatcherService.Invoke(() =>
                {
                    PnlText.Foreground = new SolidColorBrush(Colors.Gray);
                    PnlText.Text = "No Position";
                });
                return;
            }

            foreach (var item in info)
            {
                var margin = item.EntryPrice * item.Quantity / item.Leverage;
                var upnlPer = item.UnrealizedPnl / margin * 100;
                DispatcherService.Invoke(() =>
                {
                    SymbolText.Text = item.Symbol;
                    PnlText.Foreground = item.UnrealizedPnl >= 0 ? new SolidColorBrush(Color.FromRgb(59, 207, 134)) : new SolidColorBrush(Color.FromRgb(237, 49, 97));
                    PnlText.Text = (item.UnrealizedPnl >= 0 ? "+" : "") + decimal.Round(item.UnrealizedPnl, 2);
                    //PnlPercentText.Foreground = upnlPer >= 0 ? new SolidColorBrush(Color.FromRgb(59, 207, 134)) : new SolidColorBrush(Color.FromRgb(237, 49, 97));
                    //PnlPercentText.Text = (upnlPer >= 0 ? "+" : "") + decimal.Round(upnlPer, 2) + "%";
                });
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            var window = (Window)sender;
            window.Topmost = true;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.OemPlus)
            {
                SymbolText.FontSize++;
                PnlText.FontSize++;
            }
            else if (e.Key == Key.OemMinus)
            {
                SymbolText.FontSize--;
                PnlText.FontSize--;
            }
        }
    }
}
