using System.Reflection;

namespace MercuryTradingModel.Extensions
{
    public static class IEnumerableExtension
    {
        public static void SaveCsvFile<T>(this IEnumerable<T> obj, string path)
        {
            var alternativeColonChar = 'ꪪ';
            var type = typeof(T);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetField);
            var fieldNames = fields.Select(x => x.Name.Replace(',', alternativeColonChar).Replace("k__BackingField", "").Replace("<", "").Replace(">", "")).ToList();

            var contents = new List<string>
            {
                string.Join(',', fieldNames)
            };

            foreach (var data in obj)
            {
                var values = new List<string>();
                for (int i = 0; i < fields.Length; i++)
                {
                    var value = type.GetProperty(fieldNames[i])?.GetValue(data, null);
                    values.Add(value?.ToString()?.Replace(',', alternativeColonChar) ?? default!);
                }
                contents.Add(string.Join(',', values.ToArray()));
            }

            File.WriteAllLines(path, contents);
        }
    }
}
