using System.Windows;

using WpfMdi;

namespace MarinerX.Views
{
	/// <summary>
	/// MdiTestView.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MdiTestView : Window
	{
		public MdiTestView()
		{
			InitializeComponent();
		}

		private void SymbolBenchmarkButton_Click(object sender, RoutedEventArgs e)
		{
			container.Children.Add(new MdiChild()
			{
				Title = "Symbol Benchmark",
				Content = new SymbolBenchmarkingView(),
				Width = 400,
				Height = 300,
				Position = new Point(30, 30)
			});
		}
	}
}
