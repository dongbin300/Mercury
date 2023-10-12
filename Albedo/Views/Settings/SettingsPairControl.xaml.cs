using System.Windows.Controls;
using System.Windows;

namespace Albedo.Views.Settings
{
    /// <summary>
    /// SettingsPairControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingsPairControl : UserControl
    {
        bool isInit = false;

        public SettingsPairControl()
        {
            InitializeComponent();

            SimpleListCountErrorText.Visibility = Visibility.Hidden;
            isInit = true;
            SimpleListCountText.Text = Albedo.Settings.Default.DefaultPairCount.ToString();
            isInit = false;
        }

        private void SimpleListCountText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isInit)
            {
                return;
            }

            if (int.TryParse(SimpleListCountText.Text, out var simpleListCount))
            {
                if (simpleListCount > 0)
                {
                    SimpleListCountErrorText.Visibility = Visibility.Hidden;
                }
                else
                {
                    SimpleListCountErrorText.Visibility = Visibility.Visible;
                }
            }
            else
            {
                SimpleListCountErrorText.Visibility = Visibility.Visible;
            }
        }
    }
}
