using Mercury.Apis;
using Mercury.TradingModels;

using System.Windows;

namespace MarinerX
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Initialize();
            BinanceSocketApi.GetBnbMarkPriceUpdatesAsync();
            var trayMenu = new TrayMenu();
        }

        void Initialize()
        {
            LocalApi.Init();
            BinanceRestApi.Init();
            BinanceSocketApi.Init();
            TradingModelPath.Init();
        }
    }
}
