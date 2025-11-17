namespace Vectoris.Extensions;

public static class PathExtensions
{
	/// <summary>
	/// 문자열 경로를 기준으로 하위 폴더 또는 파일 경로를 결합합니다.
	/// <br/>ex) <c>BasePath.Down("binance", "data.json")</c>
	/// </summary>
	public static string Down(this string path, params string[] downPaths)
		=> Path.Combine(path, Path.Combine(downPaths));

	/// <summary>
	/// 해당 파일이 없으면 자동 생성합니다.
	/// <br/>ex) <c>"C:\Data\test.txt".TryCreate()</c>
	/// </summary>
	public static void TryCreate(this string path)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		if (!File.Exists(path))
			File.WriteAllText(path, string.Empty);
	}

	/// <summary>
	/// 해당 경로의 디렉터리가 존재하지 않으면 생성합니다.
	/// <br/>ex) <c>"C:\Data\Logs".TryCreateDirectory()</c>
	/// </summary>
	public static void TryCreateDirectory(this string path)
	{
		if (!Directory.Exists(path))
			Directory.CreateDirectory(path);
	}

	/// <summary>
	/// 경로에서 디렉터리 경로만 가져옵니다.
	/// <br/>ex) <c>"C:\Data\test.txt".GetDirectory() → "C:\Data"</c>
	/// </summary>
	public static string GetDirectory(this string path)
		=> Path.GetDirectoryName(path) ?? string.Empty;

	/// <summary>
	/// 전체 경로에서 파일명만 가져옵니다.
	/// <br/>ex) <c>"C:\Data\test.txt".GetFileName() → "test.txt"</c>
	/// </summary>
	public static string GetFileName(this string path)
		=> Path.GetFileName(path);

	/// <summary>
	/// 파일 확장자를 반환합니다.
	/// <br/>ex) <c>"aaa.json".GetExtension() → ".json"</c>
	/// </summary>
	public static string GetExtension(this string path)
		=> Path.GetExtension(path);

	/// <summary>
	/// 파일명에서 확장자를 제외한 순수 파일명을 반환합니다.
	/// <br/>ex) <c>"C:\Data\test.json".GetOnlyFileName() → "test"</c>
	/// </summary>
	public static string GetOnlyFileName(this string path)
		=> Path.GetFileNameWithoutExtension(path);
}
