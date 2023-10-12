using Albedo.Managers;
using Albedo.Models;

using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Albedo.Views.Settings
{
    /// <summary>
    /// SettingsChartControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingsChartControl : UserControl
    {
        bool isInit = false;

        public SettingsChartControl()
        {
            InitializeComponent();

            MaTypeCombo1.Items.Clear();
            MaTypeCombo2.Items.Clear();
            MaTypeCombo3.Items.Clear();
            MaTypeCombo4.Items.Clear();
            MaTypeCombo5.Items.Clear();
            foreach (var item in Common.MaTypes)
            {
                MaTypeCombo1.Items.Add(item);
                MaTypeCombo2.Items.Add(item);
                MaTypeCombo3.Items.Add(item);
                MaTypeCombo4.Items.Add(item);
                MaTypeCombo5.Items.Add(item);
            }

            MaLineColorCombo1.Items.Clear();
            MaLineColorCombo2.Items.Clear();
            MaLineColorCombo3.Items.Clear();
            MaLineColorCombo4.Items.Clear();
            MaLineColorCombo5.Items.Clear();
            BbSmaLineColorCombo1.Items.Clear();
            BbUpperLineColorCombo1.Items.Clear();
            BbLowerLineColorCombo1.Items.Clear();
            IcTenkanLineColorCombo.Items.Clear();
            IcKijunLineColorCombo.Items.Clear();
            IcChikouLineColorCombo.Items.Clear();
            IcSenkou1LineColorCombo.Items.Clear();
            IcSenkou2LineColorCombo.Items.Clear();
            RsiLineColorCombo.Items.Clear();
            foreach (var item in Common.MaLineColors)
            {
                MaLineColorCombo1.Items.Add(item);
                MaLineColorCombo2.Items.Add(item);
                MaLineColorCombo3.Items.Add(item);
                MaLineColorCombo4.Items.Add(item);
                MaLineColorCombo5.Items.Add(item);
                BbSmaLineColorCombo1.Items.Add(item);
                BbUpperLineColorCombo1.Items.Add(item);
                BbLowerLineColorCombo1.Items.Add(item);
                IcTenkanLineColorCombo.Items.Add(item);
                IcKijunLineColorCombo.Items.Add(item);
                IcChikouLineColorCombo.Items.Add(item);
                IcSenkou1LineColorCombo.Items.Add(item);
                IcSenkou2LineColorCombo.Items.Add(item);
                RsiLineColorCombo.Items.Add(item);
            }

            MaLineWeightCombo1.Items.Clear();
            MaLineWeightCombo2.Items.Clear();
            MaLineWeightCombo3.Items.Clear();
            MaLineWeightCombo4.Items.Clear();
            MaLineWeightCombo5.Items.Clear();
            BbSmaLineWeightCombo1.Items.Clear();
            BbUpperLineWeightCombo1.Items.Clear();
            BbLowerLineWeightCombo1.Items.Clear();
            IcTenkanLineWeightCombo.Items.Clear();
            IcKijunLineWeightCombo.Items.Clear();
            IcChikouLineWeightCombo.Items.Clear();
            IcSenkou1LineWeightCombo.Items.Clear();
            IcSenkou2LineWeightCombo.Items.Clear();
            RsiLineWeightCombo.Items.Clear();
            foreach (var item in Common.MaLineWeights)
            {
                MaLineWeightCombo1.Items.Add(item);
                MaLineWeightCombo2.Items.Add(item);
                MaLineWeightCombo3.Items.Add(item);
                MaLineWeightCombo4.Items.Add(item);
                MaLineWeightCombo5.Items.Add(item);
                BbSmaLineWeightCombo1.Items.Add(item);
                BbUpperLineWeightCombo1.Items.Add(item);
                BbLowerLineWeightCombo1.Items.Add(item);
                IcTenkanLineWeightCombo.Items.Add(item);
                IcKijunLineWeightCombo.Items.Add(item);
                IcChikouLineWeightCombo.Items.Add(item);
                IcSenkou1LineWeightCombo.Items.Add(item);
                IcSenkou2LineWeightCombo.Items.Add(item);
                RsiLineWeightCombo.Items.Add(item);
            }

            DefaultCandleCountErrorText.Visibility = Visibility.Hidden;
            isInit = true;
            LoadSettings();
            isInit = false;
        }

        private void LoadSettings()
        {
            DefaultCandleCountText.Text = SettingsMan.DefaultCandleCount.ToString();

            // 기본값
            MaPeriodText1.Text = "5";
            MaPeriodText2.Text = "10";
            MaPeriodText3.Text = "20";
            MaPeriodText4.Text = "60";
            MaPeriodText5.Text = "120";
            MaTypeCombo1.SelectedIndex = 0;
            MaTypeCombo2.SelectedIndex = 0;
            MaTypeCombo3.SelectedIndex = 0;
            MaTypeCombo4.SelectedIndex = 0;
            MaTypeCombo5.SelectedIndex = 0;
            MaLineColorCombo1.SelectedIndex = 0;
            MaLineColorCombo2.SelectedIndex = 1;
            MaLineColorCombo3.SelectedIndex = 2;
            MaLineColorCombo4.SelectedIndex = 3;
            MaLineColorCombo5.SelectedIndex = 4;
            MaLineWeightCombo1.SelectedIndex = 0;
            MaLineWeightCombo2.SelectedIndex = 0;
            MaLineWeightCombo3.SelectedIndex = 0;
            MaLineWeightCombo4.SelectedIndex = 0;
            MaLineWeightCombo5.SelectedIndex = 0;

            BbPeriodText1.Text = "20";
            BbDeviationText1.Text = "2";
            BbSmaLineColorCombo1.SelectedIndex = 0;
            BbUpperLineColorCombo1.SelectedIndex = 0;
            BbLowerLineColorCombo1.SelectedIndex = 0;
            BbSmaLineWeightCombo1.SelectedIndex = 0;
            BbUpperLineWeightCombo1.SelectedIndex = 0;
            BbLowerLineWeightCombo1.SelectedIndex = 0;

            IcShortPeriodText.Text = "9";
            IcMidPeriodText.Text = "26";
            IcLongPeriodText.Text = "52";
            IcCloudEnable.IsChecked = false;
            IcTenkanLineColorCombo.SelectedIndex = 0;
            IcKijunLineColorCombo.SelectedIndex = 1;
            IcChikouLineColorCombo.SelectedIndex = 2;
            IcSenkou1LineColorCombo.SelectedIndex = 3;
            IcSenkou2LineColorCombo.SelectedIndex = 4;
            IcTenkanLineWeightCombo.SelectedIndex = 0;
            IcKijunLineWeightCombo.SelectedIndex = 0;
            IcChikouLineWeightCombo.SelectedIndex = 0;
            IcSenkou1LineWeightCombo.SelectedIndex = 0;
            IcSenkou2LineWeightCombo.SelectedIndex = 0;

            RsiPeriodText.Text = "14";
            RsiLineColorCombo.SelectedIndex = 0;
            RsiLineWeightCombo.SelectedIndex = 0;

            #region MA
            // 이평선 1
            if (SettingsMan.Indicators.Mas.Count >= 1)
            {
                var ma = SettingsMan.Indicators.Mas[0];
                MaEnable1.IsChecked = ma.Enable;
                MaPeriodText1.Text = ma.Period.ToString();
                MaTypeCombo1.SelectedItem = MaTypeCombo1.Items.OfType<MaTypeModel>().First(x => x.Type.Equals(ma.Type.Type));
                MaLineColorCombo1.SelectedItem = MaLineColorCombo1.Items.OfType<LineColorModel>().First(x => x.LineColor.Equals(ma.LineColor.LineColor));
                MaLineWeightCombo1.SelectedItem = MaLineWeightCombo1.Items.OfType<LineWeightModel>().First(x => x.LineWeight.Equals(ma.LineWeight.LineWeight));
            }

            // 이평선 2
            if (SettingsMan.Indicators.Mas.Count >= 2)
            {
                var ma = SettingsMan.Indicators.Mas[1];
                MaEnable2.IsChecked = ma.Enable;
                MaPeriodText2.Text = ma.Period.ToString();
                MaTypeCombo2.SelectedItem = MaTypeCombo2.Items.OfType<MaTypeModel>().First(x => x.Type.Equals(ma.Type.Type));
                MaLineColorCombo2.SelectedItem = MaLineColorCombo2.Items.OfType<LineColorModel>().First(x => x.LineColor.Equals(ma.LineColor.LineColor));
                MaLineWeightCombo2.SelectedItem = MaLineWeightCombo2.Items.OfType<LineWeightModel>().First(x => x.LineWeight.Equals(ma.LineWeight.LineWeight));
            }

            // 이평선 3
            if (SettingsMan.Indicators.Mas.Count >= 3)
            {
                var ma = SettingsMan.Indicators.Mas[2];
                MaEnable3.IsChecked = ma.Enable;
                MaPeriodText3.Text = ma.Period.ToString();
                MaTypeCombo3.SelectedItem = MaTypeCombo3.Items.OfType<MaTypeModel>().First(x => x.Type.Equals(ma.Type.Type));
                MaLineColorCombo3.SelectedItem = MaLineColorCombo3.Items.OfType<LineColorModel>().First(x => x.LineColor.Equals(ma.LineColor.LineColor));
                MaLineWeightCombo3.SelectedItem = MaLineWeightCombo3.Items.OfType<LineWeightModel>().First(x => x.LineWeight.Equals(ma.LineWeight.LineWeight));
            }

            // 이평선 4
            if (SettingsMan.Indicators.Mas.Count >= 4)
            {
                var ma = SettingsMan.Indicators.Mas[3];
                MaEnable4.IsChecked = ma.Enable;
                MaPeriodText4.Text = ma.Period.ToString();
                MaTypeCombo4.SelectedItem = MaTypeCombo4.Items.OfType<MaTypeModel>().First(x => x.Type.Equals(ma.Type.Type));
                MaLineColorCombo4.SelectedItem = MaLineColorCombo4.Items.OfType<LineColorModel>().First(x => x.LineColor.Equals(ma.LineColor.LineColor));
                MaLineWeightCombo4.SelectedItem = MaLineWeightCombo4.Items.OfType<LineWeightModel>().First(x => x.LineWeight.Equals(ma.LineWeight.LineWeight));
            }

            // 이평선 5
            if (SettingsMan.Indicators.Mas.Count >= 5)
            {
                var ma = SettingsMan.Indicators.Mas[4];
                MaEnable5.IsChecked = ma.Enable;
                MaPeriodText5.Text = ma.Period.ToString();
                MaTypeCombo5.SelectedItem = MaTypeCombo5.Items.OfType<MaTypeModel>().First(x => x.Type.Equals(ma.Type.Type));
                MaLineColorCombo5.SelectedItem = MaLineColorCombo5.Items.OfType<LineColorModel>().First(x => x.LineColor.Equals(ma.LineColor.LineColor));
                MaLineWeightCombo5.SelectedItem = MaLineWeightCombo5.Items.OfType<LineWeightModel>().First(x => x.LineWeight.Equals(ma.LineWeight.LineWeight));
            }
            #endregion

            #region BB
            // 볼린저밴드 1
            if (SettingsMan.Indicators.Bbs.Count >= 1)
            {
                var bb = SettingsMan.Indicators.Bbs[0];
                BbEnable1.IsChecked = bb.Enable;
                BbPeriodText1.Text = bb.Period.ToString();
                BbDeviationText1.Text = bb.Deviation.ToString();
                BbSmaLineColorCombo1.SelectedItem = BbSmaLineColorCombo1.Items.OfType<LineColorModel>().First(x => x.LineColor.Equals(bb.SmaLineColor.LineColor));
                BbUpperLineColorCombo1.SelectedItem = BbUpperLineColorCombo1.Items.OfType<LineColorModel>().First(x => x.LineColor.Equals(bb.UpperLineColor.LineColor));
                BbLowerLineColorCombo1.SelectedItem = BbLowerLineColorCombo1.Items.OfType<LineColorModel>().First(x => x.LineColor.Equals(bb.LowerLineColor.LineColor));
                BbSmaLineWeightCombo1.SelectedItem = BbSmaLineWeightCombo1.Items.OfType<LineWeightModel>().First(x => x.LineWeight.Equals(bb.SmaLineWeight.LineWeight));
                BbUpperLineWeightCombo1.SelectedItem = BbUpperLineWeightCombo1.Items.OfType<LineWeightModel>().First(x => x.LineWeight.Equals(bb.UpperLineWeight.LineWeight));
                BbLowerLineWeightCombo1.SelectedItem = BbLowerLineWeightCombo1.Items.OfType<LineWeightModel>().First(x => x.LineWeight.Equals(bb.LowerLineWeight.LineWeight));
            }
            #endregion

            #region IC
            // 일목균형표
            if (SettingsMan.Indicators.Ic != null)
            {
                var ic = SettingsMan.Indicators.Ic;
                IcEnable.IsChecked = ic.Enable;
                IcShortPeriodText.Text = ic.ShortPeriod.ToString();
                IcMidPeriodText.Text = ic.MidPeriod.ToString();
                IcLongPeriodText.Text = ic.LongPeriod.ToString();
                IcCloudEnable.IsChecked = ic.CloudEnable;
                IcTenkanLineColorCombo.SelectedItem = IcTenkanLineColorCombo.Items.OfType<LineColorModel>().First(x => x.LineColor.Equals(ic.TenkanLineColor.LineColor));
                IcKijunLineColorCombo.SelectedItem = IcKijunLineColorCombo.Items.OfType<LineColorModel>().First(x => x.LineColor.Equals(ic.KijunLineColor.LineColor));
                IcChikouLineColorCombo.SelectedItem = IcChikouLineColorCombo.Items.OfType<LineColorModel>().First(x => x.LineColor.Equals(ic.ChikouLineColor.LineColor));
                IcSenkou1LineColorCombo.SelectedItem = IcSenkou1LineColorCombo.Items.OfType<LineColorModel>().First(x => x.LineColor.Equals(ic.Senkou1LineColor.LineColor));
                IcSenkou2LineColorCombo.SelectedItem = IcSenkou2LineColorCombo.Items.OfType<LineColorModel>().First(x => x.LineColor.Equals(ic.Senkou2LineColor.LineColor));
                IcTenkanLineWeightCombo.SelectedItem = IcTenkanLineWeightCombo.Items.OfType<LineWeightModel>().First(x => x.LineWeight.Equals(ic.TenkanLineWeight.LineWeight));
                IcKijunLineWeightCombo.SelectedItem = IcKijunLineWeightCombo.Items.OfType<LineWeightModel>().First(x => x.LineWeight.Equals(ic.KijunLineWeight.LineWeight));
                IcChikouLineWeightCombo.SelectedItem = IcChikouLineWeightCombo.Items.OfType<LineWeightModel>().First(x => x.LineWeight.Equals(ic.ChikouLineWeight.LineWeight));
                IcSenkou1LineWeightCombo.SelectedItem = IcSenkou1LineWeightCombo.Items.OfType<LineWeightModel>().First(x => x.LineWeight.Equals(ic.Senkou1LineWeight.LineWeight));
                IcSenkou2LineWeightCombo.SelectedItem = IcSenkou2LineWeightCombo.Items.OfType<LineWeightModel>().First(x => x.LineWeight.Equals(ic.Senkou2LineWeight.LineWeight));
            }
            #endregion

            #region RSI
            // RSI
            if (SettingsMan.Indicators.Rsi != null)
            {
                var rsi = SettingsMan.Indicators.Rsi;
                RsiEnable.IsChecked = rsi.Enable;
                RsiPeriodText.Text = rsi.Period.ToString();
                RsiLineColorCombo.SelectedItem = RsiLineColorCombo.Items.OfType<LineColorModel>().First(x => x.LineColor.Equals(rsi.LineColor.LineColor));
                RsiLineWeightCombo.SelectedItem = RsiLineWeightCombo.Items.OfType<LineWeightModel>().First(x => x.LineWeight.Equals(rsi.LineWeight.LineWeight));
            }
            #endregion
        }

        /// <summary>
        /// 기본 캔들 개수 수정
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DefaultCandleCountText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isInit)
            {
                return;
            }

            if (int.TryParse(DefaultCandleCountText.Text, out var defaultCandleCount))
            {
                if (defaultCandleCount >= 10 && defaultCandleCount <= 1000)
                {
                    DefaultCandleCountErrorText.Visibility = Visibility.Hidden;
                }
                else
                {
                    DefaultCandleCountErrorText.Visibility = Visibility.Visible;
                }
            }
            else
            {
                DefaultCandleCountErrorText.Visibility = Visibility.Visible;
            }
        }
    }
}
