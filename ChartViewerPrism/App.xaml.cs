using ChartViewerPrism.Views;

using Prism.Ioc;
using Prism.Regions;

using System.Windows;
using System.Windows.Controls;

namespace ChartViewerPrism
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		protected override Window CreateShell()
		{
			return Container.Resolve<MainWindow>();
		}

		protected override void RegisterTypes(IContainerRegistry containerRegistry)
		{
		}

		//protected override void ConfigureRegionAdapterMappings(RegionAdapterMappings regionAdapterMappings)
		//{
		//	base.ConfigureRegionAdapterMappings(regionAdapterMappings);
		//	regionAdapterMappings.RegisterMapping<Grid>(Container.Resolve<gridregionadapter>)
		//}

		protected override void InitializeShell(Window shell)
		{
			base.InitializeShell(shell);
			var regionManager = Container.Resolve<IRegionManager>();
			regionManager.AddToRegion("ChartRegion", typeof(CandleChartControl));
		}
	}
}
