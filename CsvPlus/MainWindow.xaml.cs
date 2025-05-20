using System.Data;
using System.Windows;
using System.Windows.Controls;

namespace CsvPlus;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	QuantityToWidthConverter qtwConverter = default!;
	private DataTable _csvTable;

	public MainWindow()
	{
		InitializeComponent();
	}

	private void CsvTextBox_TextChanged(object sender, TextChangedEventArgs e)
	{
		string csvText = CsvTextBox.Text;

		if (string.IsNullOrWhiteSpace(csvText))
		{
			CsvDataGrid.ItemsSource = null;
			return;
		}

		var lines = csvText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
		if (lines.Length < 1) return;

		bool hasHeader = GuessHasHeader(lines);
		int startIndex = hasHeader ? 1 : 0;

		string[] headers;
		int colCount;

		if (hasHeader)
		{
			headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
			colCount = headers.Length;
		}
		else
		{
			colCount = lines[0].Split(',').Length;
			headers = Enumerable.Range(1, colCount).Select(i => $"C{i}").ToArray();
		}

		// 숫자인지 아닌지 판단
		bool[] isNumericColumn = Enumerable.Repeat(true, colCount).ToArray();

		for (int i = startIndex; i < lines.Length; i++)
		{
			var values = lines[i].Split(',');
			for (int j = 0; j < colCount; j++)
			{
				if (j >= values.Length) continue;

				string v = values[j].Trim();
				if (!IsNumeric(v))
					isNumericColumn[j] = false;
			}
		}

		// DataTable 생성
		DataTable dataTable = new DataTable();

		for (int i = 0; i < colCount; i++)
		{
			if (isNumericColumn[i])
				dataTable.Columns.Add(headers[i], typeof(double));
			else
				dataTable.Columns.Add(headers[i], typeof(string));
		}

		// 데이터 넣기
		for (int i = startIndex; i < lines.Length; i++)
		{
			var values = lines[i].Split(',');
			var row = dataTable.NewRow();

			for (int j = 0; j < colCount; j++)
			{
				if (j >= values.Length)
				{
					row[j] = DBNull.Value;
					continue;
				}

				string raw = values[j].Trim();
				if (isNumericColumn[j] && double.TryParse(raw, out double num))
					row[j] = num;
				else
					row[j] = raw;
			}

			dataTable.Rows.Add(row);
		}

		_csvTable = dataTable;
		CsvDataGrid.ItemsSource = dataTable.DefaultView;

		UpdateComboBoxes(headers);
	}

	private bool GuessHasHeader(string[] lines)
	{
		if (lines.Length < 2) return false;

		var first = lines[0].Split(',');
		var second = lines[1].Split(',');

		bool firstAllText = first.All(s => !IsNumeric(s));
		bool secondHasNumbers = second.Any(s => IsNumeric(s));

		return firstAllText && secondHasNumbers;
	}

	private bool IsNumeric(string value)
	{
		return double.TryParse(value, out _);
	}

	private void UpdateComboBoxes(string[] headers)
	{
		Parameter1ComboBox.ItemsSource = headers;
		Parameter2ComboBox.ItemsSource = headers;

		if (Parameter1ComboBox.Items.Count > 0)
			Parameter1ComboBox.SelectedIndex = 0;

		if (Parameter2ComboBox.Items.Count > 0)
			Parameter2ComboBox.SelectedIndex = 1;

		Parameter1ComboBox.SelectionChanged += ParameterComboBox_SelectionChanged;
		Parameter2ComboBox.SelectionChanged += ParameterComboBox_SelectionChanged;
	}

	private void ParameterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (_csvTable == null) return;
		if (Parameter1ComboBox.SelectedItem == null || Parameter2ComboBox.SelectedItem == null) return;

		string groupColumn = Parameter1ComboBox.SelectedItem.ToString();
		string targetColumn = Parameter2ComboBox.SelectedItem.ToString();

		if (!_csvTable.Columns.Contains(groupColumn) || !_csvTable.Columns.Contains(targetColumn)) return;

		var grouped = _csvTable.AsEnumerable()
			.Where(row => double.TryParse(row[targetColumn]?.ToString(), out _))
			.GroupBy(row => row[groupColumn]?.ToString())
			.Select(g =>
			{
				var values = g.Select(r => double.Parse(r[targetColumn].ToString())).ToList();
				double avg = values.Average();
				double median = values.OrderBy(x => x).ElementAt(values.Count / 2);
				double std = Math.Sqrt(values.Select(x => Math.Pow(x - avg, 2)).Average());
				double min = values.Min();
				double max = values.Max();
				double maxMinRatio = min != 0 ? max / min : double.NaN;

				return new
				{
					Group = g.Key,
					Count = values.Count,
					Average = (int)avg,
					Median = (int)median,
					StdDev = (int)std,
					Min = (int)min,
					Max = (int)max,
					MaxMinRatio = (int)maxMinRatio
				};
			}).ToList();

		AnalysisDataGrid.ItemsSource = grouped;

		if (AnalysisDataGrid.ItemsSource != null)
		{
			qtwConverter = (QuantityToWidthConverter)Application.Current.Resources["qtw"];
			if (qtwConverter != null)
			{
				qtwConverter.MaxQuantity = grouped.Max(o => o.Average);
			}
		}
	}
}