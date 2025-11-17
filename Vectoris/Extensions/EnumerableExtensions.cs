using System.Reflection;

namespace Vectoris.Extensions;

/// <summary>
/// IEnumerable 컬렉션 관련 확장 메서드
/// </summary>
public static class EnumerableExtensions
{
	/// <summary>
	/// IEnumerable 컬렉션을 CSV 파일로 저장합니다.
	/// <br/>ex) <c>myList.SaveCsvFile("C:\\data.csv");</c>
	/// </summary>
	/// <typeparam name="T">컬렉션 요소 타입</typeparam>
	/// <param name="collection">CSV로 저장할 컬렉션</param>
	/// <param name="path">저장할 파일 경로</param>
	/// <param name="alternativeCommaChar">CSV 내 쉼표 대체 문자 (기본: ꪪ)</param>
	public static void SaveCsvFile<T>(this IEnumerable<T> collection, string path, char alternativeCommaChar = 'ꪪ')
	{
		ArgumentNullException.ThrowIfNull(collection);

		var type = typeof(T);

		var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

		var headers = properties
			.Select(p => p.Name.Replace(',', alternativeCommaChar))
			.ToArray();

		var csvLines = new List<string> { string.Join(',', headers) };

		foreach (var item in collection)
		{
			var values = properties.Select(p =>
			{
				var value = p.GetValue(item)?.ToString() ?? string.Empty;
				return value.Replace(',', alternativeCommaChar);
			});

			csvLines.Add(string.Join(',', values));
		}

		var directory = Path.GetDirectoryName(path);
		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			Directory.CreateDirectory(directory);

		File.WriteAllLines(path, csvLines);
	}
}
