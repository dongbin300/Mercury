namespace Mercury.IO
{
	public class MtmBacktestTmFile
	{
		public string FileName { get; set; } = string.Empty;
		public string Name => FileName.Split('\\')[^1].Replace(FileName[FileName.LastIndexOf('.')..], "");
		public string MenuString => Name + " 실행";

		public MtmBacktestTmFile(string fileName) => FileName = fileName;

		public override string ToString()
		{
			return FileName + "|+|" + Name + "|+|" + MenuString;
		}
	}
}
