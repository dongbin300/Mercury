using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.UnityExtensions;

using Prism.Modularity;

using System.ComponentModel;
using System.Windows;

namespace DeluxeChartViewer
{
	public class BootStrapper : UnityBootstrapper
	{
		protected override DependencyObject CreateShell()
		{
			return Container.TryResolve<PrismShell>();
		}

		protected override void InitializeModules()
		{
			base.InitializeModules();
			Application.Current.MainWindow = (PrismShell)Shell;
			Application.Current.MainWindow.Show();
		}
		protected override void ConfigureModuleCatalog()
		{
			base.ConfigureModuleCatalog();
			ModuleCatalog moduleCatalog = (ModuleCatalog)ModuleCatalog;

		}
	}
}