using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Backtester.Views.Controls
{
	/// <summary>
	/// NumericPlusTextBox.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class NumericPlusTextBox : UserControl
	{
		public NumericPlusTextBox()
		{
			InitializeComponent();
			PART_TextBox.PreviewMouseWheel += TextBox_PreviewMouseWheel;
		}

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register("Text", typeof(string), typeof(NumericPlusTextBox), new PropertyMetadata("0"));
		public static readonly DependencyProperty CaretBrushProperty =
			DependencyProperty.Register("CaretBrush", typeof(Brush), typeof(NumericPlusTextBox), new PropertyMetadata(Brushes.White));

		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}
		public Brush CaretBrush
		{
			get { return (Brush)GetValue(CaretBrushProperty); }
			set { SetValue(CaretBrushProperty, value); }
		}

		private void TextBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (int.TryParse(Text, out int value))
			{
				value += e.Delta > 0 ? 1 : -1;
				Text = value.ToString();
			}
			e.Handled = true;
		}

		private void UpButton_Click(object sender, RoutedEventArgs e)
		{
			if (int.TryParse(Text, out int value))
			{
				value++;
				Text = value.ToString();
			}
		}

		private void DownButton_Click(object sender, RoutedEventArgs e)
		{
			if (int.TryParse(Text, out int value))
			{
				value--;
				Text = value.ToString();
			}
		}
	}
}
