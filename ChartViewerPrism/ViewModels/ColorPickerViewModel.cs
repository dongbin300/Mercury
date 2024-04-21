using Prism.Mvvm;

namespace ChartViewerPrism.ViewModels
{
	public class ColorPickerViewModel : BindableBase
	{
		private string _text;
		public string Text
		{
			get { return _text; }
			set { SetProperty(ref _text, value); }
		}

		public ColorPickerViewModel()
		{
			Text = "11";
		}
	}
}
