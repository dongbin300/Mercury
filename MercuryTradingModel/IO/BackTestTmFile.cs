namespace MercuryTradingModel.IO
{
    public class BackTestTmFile
    {
        public string FileName { get; set; } = string.Empty;
        public string Name => FileName.Split('\\')[^1].Replace(FileName[FileName.LastIndexOf('.')..], "");
        public string MenuString => Name + " 실행";

        public BackTestTmFile(string fileName)
        {
            FileName = fileName;
        }

        public override string ToString()
        {
            return FileName + "|+|" + Name + "|+|" + MenuString;
        }
    }
}
