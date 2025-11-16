using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Backtester2.Models
{
    public class CsvRow : INotifyPropertyChanged
    {
        private Dictionary<string, string> _data = new();

        public Dictionary<string, string> Data
        {
            get => _data;
            set
            {
                _data = value;
                OnPropertyChanged(nameof(Data));

                // Notify property changed for each column
                foreach (var key in value.Keys)
                {
                    OnPropertyChanged(key);
                }
            }
        }

        public string this[string columnName]
        {
            get => _data.TryGetValue(columnName, out var value) ? value : string.Empty;
            set
            {
                _data[columnName] = value;
                OnPropertyChanged(columnName);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static CsvRow FromCsvLine(string csvLine, List<string> headers)
        {
            var parts = csvLine.Split(',');
            var row = new CsvRow();

            for (int i = 0; i < headers.Count && i < parts.Length; i++)
            {
                row[headers[i]] = parts[i].Trim();
            }

            return row;
        }
    }
}