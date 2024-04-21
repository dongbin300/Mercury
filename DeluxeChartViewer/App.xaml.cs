using System.Configuration;
using System.Data;
using System.Windows;

namespace DeluxeChartViewer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			base.OnStartup(e);
			BootStrapper bootstrapper = new BootStrapper();
			bootstrapper.Run();
		}
	}

}
