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
    /// BalanceMonitorView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BalanceMonitorView : Window
    {
        private System.Timers.Timer timer = new (1000);

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

        public BalanceMonitorView()
        {
            InitializeComponent();
            Left = 0;
            Top = 0;

            var hWnd = new WindowInteropHelper(this).Handle;
            SetWindowPos(hWnd, HWND.TopMost, 0, 0, 0, 0, SWP.NOMOVE | SWP.NOSIZE);

            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            var balance = BinanceRestApi.GetFuturesBalance();
            var income = (double)balance - Common.Seed;
            var incomePerDays = income / (DateTime.Now - Common.StartTime).TotalSeconds * 86400;

            DispatcherService.Invoke(() =>
            {
                BalanceText.Text = Math.Round(balance, 2).ToString();
                IncomeText.Text = Math.Round(income, 2).ToString();
                IncomeText.Foreground = income >= 0 ? new SolidColorBrush(Color.FromRgb(59, 207, 134)) : new SolidColorBrush(Color.FromRgb(237, 49, 97));
                IncomePerDaysText.Text = Math.Round(incomePerDays, 2).ToString();
                IncomePerDaysText.Foreground = incomePerDays >= 0 ? new SolidColorBrush(Color.FromRgb(59, 207, 134)) : new SolidColorBrush(Color.FromRgb(237, 49, 97));
            });
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
                BalanceText.FontSize++;
                IncomeText.FontSize++;
                IncomePerDaysText.FontSize++;
            }
            else if (e.Key == Key.OemMinus)
            {
                BalanceText.FontSize--;
                IncomeText.FontSize--;
                IncomePerDaysText.FontSize--;
            }
        }
    }
}
