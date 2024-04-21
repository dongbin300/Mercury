using Microsoft.Xaml.Behaviors;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChartViewerPrism.Behaviors
{
	public class TextBoxBehavior : Behavior<TextBox>
	{
		public bool IsSelectAll { get; set; } = true;

		protected override void OnAttached()
		{
			if (IsSelectAll)
			{
				AssociatedObject.GotFocus += AssociatedObject_GotFocus;
			}
			AssociatedObject.TextChanged += AssociatedObject_TextChanged;
			AssociatedObject.KeyDown += AssociatedObject_KeyDown;
		}

		private void AssociatedObject_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (TextChangedCommand != null)
			{
				TextChangedCommand.Execute(sender);
			}
		}

		private void AssociatedObject_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter && EnterCommand != null)
			{
				EnterCommand.Execute(sender);
				e.Handled = true;
			}
		}

		private void AssociatedObject_GotFocus(object sender, System.Windows.RoutedEventArgs e)
		{
			Dispatcher.BeginInvoke(() =>
			{
				AssociatedObject.SelectAll();
			}, null);
		}

		protected override void OnDetaching()
		{
			if (IsSelectAll)
			{
				AssociatedObject.GotFocus -= AssociatedObject_GotFocus;
			}
			AssociatedObject.TextChanged -= AssociatedObject_TextChanged;
			AssociatedObject.KeyDown -= AssociatedObject_KeyDown;
		}

		public ICommand TextChangedCommand
		{
			get { return (ICommand)GetValue(TextChangedCommandProperty); }
			set { SetValue(TextChangedCommandProperty, value); }
		}
		public ICommand EnterCommand
		{
			get { return (ICommand)GetValue(EnterCommandProperty); }
			set { SetValue(EnterCommandProperty, value); }
		}
		public static readonly DependencyProperty TextChangedCommandProperty = DependencyProperty.Register(nameof(TextChangedCommand), typeof(ICommand), typeof(TextBoxBehavior), new PropertyMetadata(null));
		public static readonly DependencyProperty EnterCommandProperty = DependencyProperty.Register(nameof(EnterCommand), typeof(ICommand), typeof(TextBoxBehavior), new PropertyMetadata(null));
	}
}
