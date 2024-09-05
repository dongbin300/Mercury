using System;
using System.Windows;
using System.Windows.Media;

namespace TradeBot.Extensions
{
	public static class DependencyObjectExtension
    {
		public static DependencyObject? FindDescendant(this DependencyObject element, Type type)
		{
			if (element == null)
			{
				return null;
			}

			if (element.GetType() == type)
			{
				return element;
			}

			DependencyObject? foundElement = default!;
			if (element is FrameworkElement frameworkElement)
			{
				frameworkElement.ApplyTemplate();
			}

			var childrenCount = VisualTreeHelper.GetChildrenCount(element);
			for (int i = 0; i < childrenCount; i++)
			{
				var child = VisualTreeHelper.GetChild(element, i);
				foundElement = FindDescendant(child, type);
				if (foundElement != null)
				{
					break;
				}
			}

			return foundElement;
		}
	}
}
