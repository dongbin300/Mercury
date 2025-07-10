namespace Mercury.IO
{
	public class MtmBacktestTmFile(string fileName)
	{
		public string FileName { get; set; } = fileName;
		public string Name => FileName.Split('\\')[^1].Replace(FileName[FileName.LastIndexOf('.')..], "");
		public string MenuString => Name + " 실행";

		public override string ToString()
		{
			return FileName + "|+|" + Name + "|+|" + MenuString;
		}
	}
}
