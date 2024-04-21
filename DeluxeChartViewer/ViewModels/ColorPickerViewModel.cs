using Prism.Mvvm;

namespace DeluxeChartViewer.ViewModels
{
	public class ColorPickerViewModel : BindableBase
    {
		private string _message;
		public string Message
		{
			get { return _message; }
			set { SetProperty(ref _message, value); }
		}

		public ColorPickerViewModel()
		{
			Message = "Hello, ColorPicker!";
		}
	}
}
