using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;


using MessageBox = System.Windows.MessageBox;

namespace MAll
{
    public class TrayMenu
    {
        private NotifyIcon trayIcon;
        private static ContextMenuStrip menuStrip = new();
        private string iconFileName = "Resources/Images/chart2.ico";
        private Image iconImage;

        public TrayMenu()
        {
            iconImage = Image.FromFile(iconFileName);

            trayIcon = new NotifyIcon
            {
                Icon = new Icon(iconFileName),
                Text = $"M_ALL By Gaten",
                Visible = true,
            };

            RefreshMenu();
        }

        public void RefreshMenu()
        {
            menuStrip = new ContextMenuStrip();
            menuStrip.Items.Add(new ToolStripMenuItem("M_ALL By Gaten", iconImage));
            menuStrip.Items.Add(new ToolStripSeparator());

            menuStrip.Items.Add(new ToolStripMenuItem("Mariner X", null, MarinerX));
            menuStrip.Items.Add(new ToolStripMenuItem("Trade Bot", null, TradeBot));
            menuStrip.Items.Add(new ToolStripSeparator());

            menuStrip.Items.Add(new ToolStripMenuItem("Alphasquare", null, Alphasquare));
            menuStrip.Items.Add(new ToolStripMenuItem("Kiwoom Manager", null, KiwoomManager));
            menuStrip.Items.Add(new ToolStripSeparator());

            menuStrip.Items.Add(new ToolStripMenuItem("Backtester", null, Backtester));
            menuStrip.Items.Add(new ToolStripMenuItem("Chart Simulator", null, ChartSimulator));
            menuStrip.Items.Add(new ToolStripMenuItem("Chart Viewer", null, ChartViewer));
            menuStrip.Items.Add(new ToolStripSeparator());

            menuStrip.Items.Add(new ToolStripMenuItem("Albedo", null, Albedo));
            menuStrip.Items.Add(new ToolStripMenuItem("Albedo.Trades", null, AlbedoTrades));
            menuStrip.Items.Add(new ToolStripSeparator());

            menuStrip.Items.Add(new ToolStripMenuItem("종료", null, Exit));

            menuStrip.Items[0].Enabled = false;
            trayIcon.ContextMenuStrip = menuStrip;
        }

        void ExecuteSmart(string keyword)
        {
            var mainPath = Environment.CurrentDirectory[..(Environment.CurrentDirectory.IndexOf("\\CS\\Mercury\\") + 3)];
            var files = new DirectoryInfo(mainPath).GetFiles($"*{keyword}*.exe", SearchOption.AllDirectories);

            var filePath = files.Length > 0 ? files[0].FullName : string.Empty;

            ProcessStartInfo info = new()
            {
                FileName = filePath,
                UseShellExecute = true
            };

            Process.Start(info);
        }

        public void MarinerX(object? sender, EventArgs e)
        {
            try
            {
                ExecuteSmart("MarinerX");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void TradeBot(object? sender, EventArgs e)
        {
            try
            {
                ExecuteSmart("TradeBot");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void Alphasquare(object? sender, EventArgs e)
        {
            try
            {
                ExecuteSmart("Alphasquare");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void KiwoomManager(object? sender, EventArgs e)
        {
            try
            {
                ExecuteSmart("KiwoomManager");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void Backtester(object? sender, EventArgs e)
        {
            try
            {
                ExecuteSmart("Backtester");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void ChartSimulator(object? sender, EventArgs e)
        {
            try
            {
                ExecuteSmart("ChartSimulator");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void ChartViewer(object? sender, EventArgs e)
        {
            try
            {
                ExecuteSmart("ChartViewer");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void Albedo(object? sender, EventArgs e)
        {
            try
            {
                ExecuteSmart("Albedo");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public void AlbedoTrades(object? sender, EventArgs e)
        {
            try
            {
                ExecuteSmart("Albedo.Trades");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #region Exit
        private void Exit(object? sender, EventArgs e)
        {
            Environment.Exit(0);
        }
        #endregion
    }
}
