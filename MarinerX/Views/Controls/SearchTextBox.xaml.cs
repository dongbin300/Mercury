using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MarinerX.Views.Controls
{
	/// <summary>
	/// SearchTextBox.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class SearchTextBox : UserControl, INotifyPropertyChanged
	{
		public static readonly DependencyProperty AllPossibleSuggestionsProperty =
			DependencyProperty.Register("AllPossibleSuggestions", typeof(ObservableCollection<string>), typeof(SearchTextBox), new PropertyMetadata(new ObservableCollection<string>()));

		private ObservableCollection<string> _suggestions;
		private string _searchText;

		public ObservableCollection<string> Suggestions
		{
			get { return _suggestions; }
			set { _suggestions = value; OnPropertyChanged(); }
		}

		public string SearchText
		{
			get { return _searchText; }
			set
			{
				_searchText = value;
				OnPropertyChanged();
				FilterSuggestions();
			}
		}

		public ObservableCollection<string> AllPossibleSuggestions
		{
			get { return (ObservableCollection<string>)GetValue(AllPossibleSuggestionsProperty); }
			set { SetValue(AllPossibleSuggestionsProperty, value); }
		}

		public SearchTextBox()
		{
			InitializeComponent();
			DataContext = this;
			Suggestions = [];

			_suggestions = [];
			_searchText = string.Empty;
		}

		private void FilterSuggestions()
		{
			if (string.IsNullOrEmpty(SearchText))
			{
				Suggestions.Clear();
				return;
			}

			var filtered = AllPossibleSuggestions
						   .Where(s => s.Contains(SearchText, System.StringComparison.CurrentCultureIgnoreCase))
						   .ToList();

			Suggestions.Clear();
			foreach (var suggestion in filtered)
			{
				Suggestions.Add(suggestion);
			}

			_SuggestionsListBox.Visibility = Suggestions.Any() ? Visibility.Visible : Visibility.Collapsed;
		}

		private void _SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			SearchText = _SearchTextBox.Text;
		}

		public event PropertyChangedEventHandler? PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void _SearchTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Up:
					_SuggestionsListBox.SelectedIndex = Math.Max(0, --_SuggestionsListBox.SelectedIndex);
					break;

				case Key.Down:
					_SuggestionsListBox.SelectedIndex = Math.Min(_SuggestionsListBox.Items.Count - 1, ++_SuggestionsListBox.SelectedIndex);
					break;

				case Key.Enter:
					_SearchTextBox.Text = _SuggestionsListBox.SelectedItem.ToString();
					_SuggestionsListBox.Visibility = Visibility.Collapsed;
					break;
			}
		}

		private void _SuggestionsListBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			_SearchTextBox.Text = _SuggestionsListBox.SelectedItem.ToString();
			_SuggestionsListBox.Visibility = Visibility.Collapsed;
		}
	}
}
