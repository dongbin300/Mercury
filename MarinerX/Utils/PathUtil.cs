using Mercury;

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace MarinerX.Utils
{
    public class PathUtil
    {
        public static string Desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public static string Base = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Down("Gaten");
        public static string BinanceApiKey = Base.Down("binance_api.txt");
        public static string BinanceFuturesData = Base.Down("BinanceFuturesData");

        public static DataTable ReadCsv(string path)
        {
            var result = new DataTable();
            var items = System.IO.File.ReadAllLines(path);
            var columns = items[0].Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var column in columns)
            {
                result.Columns.Add(column, typeof(string));
            }
            for (int i = 1; i < items.Length; i++)
            {
                var data = items[i].Split(',', StringSplitOptions.RemoveEmptyEntries);
                result.Rows.Add(data);
            }
            return result;
        }

        public static IList<T> ReadCsv<T>(string path)
        {
            IList<T> result = new List<T>();
            var items = System.IO.File.ReadAllLines(path);
            var columns = items[0].Split(',', StringSplitOptions.RemoveEmptyEntries);

            Type type = typeof(T);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetField);

            for (int i = 1; i < items.Length; i++)
            {
                object[] defaultParameters = new object[columns.Length];
                Array.Fill(defaultParameters, default!);
                T instance = (T)(Activator.CreateInstance(type, defaultParameters) ?? default!);
                var segments = items[i].Split(",", StringSplitOptions.RemoveEmptyEntries);
                for (int j = 0; j < segments.Length; j++)
                {
                    var field = fields.First(f => f.Name.Equals(columns[j]) || f.Name.Equals($"<{columns[j]}>k__BackingField"));
                    field.SetValue(instance, segments[j]);
                }
                result.Add(instance);
            }

            return result;
        }
    }
}
