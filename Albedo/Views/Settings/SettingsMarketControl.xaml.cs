using System.Windows;
using System.Windows.Controls;

namespace Albedo.Views.Settings
{
    /// <summary>
    /// SettingsMarketControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SettingsMarketControl : UserControl
    {
        bool isChanging = false;

        public SettingsMarketControl()
        {
            InitializeComponent();

            //BinanceApiSecretKeyPassword.Visibility = Visibility.Visible;
            UpbitApiSecretKeyPassword.Visibility = Visibility.Visible;
            //BithumbApiSecretKeyPassword.Visibility = Visibility.Visible;
            //BinanceApiSecretKeyText.Visibility = Visibility.Collapsed;
            UpbitApiSecretKeyText.Visibility = Visibility.Collapsed;
            //BithumbApiSecretKeyText.Visibility = Visibility.Collapsed;

            //BinanceApiKeyText.Text = Albedo.Settings.Default.BinanceApiKey;
            //BinanceApiSecretKeyText.Text = BinanceApiSecretKeyPassword.Password = Albedo.Settings.Default.BinanceSecretKey;
            UpbitApiKeyText.Text = Albedo.Settings.Default.UpbitApiKey;
            UpbitApiSecretKeyText.Text = UpbitApiSecretKeyPassword.Password = Albedo.Settings.Default.UpbitSecretKey;
            //BithumbApiKeyText.Text = Albedo.Settings.Default.BithumbApiKey;
            //BithumbApiSecretKeyText.Text = BithumbApiSecretKeyPassword.Password = Albedo.Settings.Default.BithumbSecretKey;
        }

        private void BinanceEyeImage_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //if (BinanceApiSecretKeyPassword.Visibility == Visibility.Visible)
            //{
            //    BinanceApiSecretKeyPassword.Visibility = Visibility.Collapsed;
            //    BinanceApiSecretKeyText.Visibility = Visibility.Visible;
            //}
            //else
            //{
            //    BinanceApiSecretKeyPassword.Visibility = Visibility.Visible;
            //    BinanceApiSecretKeyText.Visibility = Visibility.Collapsed;
            //}
        }

        private void UpbitEyeImage_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (UpbitApiSecretKeyPassword.Visibility == Visibility.Visible)
            {
                UpbitApiSecretKeyPassword.Visibility = Visibility.Collapsed;
                UpbitApiSecretKeyText.Visibility = Visibility.Visible;
            }
            else
            {
                UpbitApiSecretKeyPassword.Visibility = Visibility.Visible;
                UpbitApiSecretKeyText.Visibility = Visibility.Collapsed;
            }
        }

        private void BithumbEyeImage_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //if (BithumbApiSecretKeyPassword.Visibility == Visibility.Visible)
            //{
            //    BithumbApiSecretKeyPassword.Visibility = Visibility.Collapsed;
            //    BithumbApiSecretKeyText.Visibility = Visibility.Visible;
            //}
            //else
            //{
            //    BithumbApiSecretKeyPassword.Visibility = Visibility.Visible;
            //    BithumbApiSecretKeyText.Visibility = Visibility.Collapsed;
            //}
        }

        private void SecretKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isChanging)
            {
                return;
            }

            isChanging = true;
            //BinanceApiSecretKeyPassword.Password = BinanceApiSecretKeyText.Text;
            UpbitApiSecretKeyPassword.Password = UpbitApiSecretKeyText.Text;
            //BithumbApiSecretKeyPassword.Password = BithumbApiSecretKeyText.Text;
            isChanging = false;
        }

        private void SecretKey_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (isChanging)
            {
                return;
            }

            isChanging = true;
            //BinanceApiSecretKeyText.Text = BinanceApiSecretKeyPassword.Password;
            UpbitApiSecretKeyText.Text = UpbitApiSecretKeyPassword.Password;
            //BithumbApiSecretKeyText.Text = BithumbApiSecretKeyPassword.Password;
            isChanging = false;
        }
    }
}
