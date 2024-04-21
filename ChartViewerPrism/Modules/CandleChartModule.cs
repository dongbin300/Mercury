using ChartViewerPrism.Views;

using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;

namespace ChartViewerPrism.Modules
{
	public class CandleChartModule : IModule
	{
		private readonly IRegionManager _regionManager;

		public CandleChartModule(IRegionManager regionManager)
		{
			_regionManager = regionManager;
		}

		public void OnInitialized(IContainerProvider containerProvider)
		{
			_regionManager.RegisterViewWithRegion("ChartRegion", typeof(CandleChartControl));
		}

		public void RegisterTypes(IContainerRegistry containerRegistry)
		{
		}
	}
}
