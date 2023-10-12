using MarinerX.Apis;
using MarinerX.Markets;

using MercuryTradingModel.TradingModels;

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
            LocalStorageApi.Init();
            BinanceRestApi.Init();
            BinanceSocketApi.Init();
            TradingModelPath.Init();
            BinanceMarket.Init();
        }
    }
}
