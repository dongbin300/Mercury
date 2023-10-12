using Mercury;

using MercuryEditor.Editor;
using MercuryEditor.Enums;
using MercuryEditor.Inspection.SystemCode;
using MercuryEditor.Inspection.V1;
using MercuryEditor.IO;

using MercuryTradingModel.TradingModels;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MercuryEditor.Inspection
{
    internal class MercuryInspector
    {
        public string InspectedPath { get; set; } = string.Empty;
        private List<TextLine> code = new();
        private int lineNumber = 0;
        private int lineCount => code.Count;

        public MercuryInspector()
        {

        }

        public MercuryInspectionResult Run(string codeText)
        {
            try
            {
                code = codeText.Split(Environment.NewLine, StringSplitOptions.None)
                    .Select((x, i) => new TextLine(i + 1, x.Split(new string[] {"//", "/*", "*/"}, StringSplitOptions.None)[0].Trim())).ToList();
                lineNumber = 0;

                var systemCodes = GetSystemCode();

                switch (systemCodes.CodeVersion)
                {
                    case 1:
                        switch (systemCodes.MarketPlatform)
                        {
                            case MarketPlatform.binancefutures:
                                switch (systemCodes.ModelType)
                                {
                                    case ModelType.backtest:
                                        var inspector = new MercuryBinanceFuturesBackTestInspector(lineNumber);
                                        var backTestResult = inspector.Run(code);
                                        if (backTestResult != string.Empty)
                                        {
                                            throw new Exception(backTestResult);
                                        }

                                        var jsonString = JsonConvert.SerializeObject(inspector.TradingModel, new JsonSerializerSettings
                                        {
                                             NullValueHandling = NullValueHandling.Ignore,
                                             TypeNameHandling = TypeNameHandling.Auto,
                                             Formatting = Formatting.Indented
                                        });
                                        InspectedPath = TradingModelPath.InspectedBackTestDirectory.Down(TmFile.TmName + ".json");
                                        File.WriteAllText(InspectedPath, jsonString);
                                        break;

                                    case ModelType.mocktrade:
                                        break;

                                    case ModelType.realtrade:
                                        break;

                                    default:
                                        throw new Exception(Delegater.CurrentLanguageDictionary["UnknownModelType"].ToString());
                                }
                                break;

                            default:
                                throw new Exception(Delegater.CurrentLanguageDictionary["UnknownMarketPlatform"].ToString());
                        }
                        break;

                    default:
                        throw new Exception(Delegater.CurrentLanguageDictionary["UnknownCodeVersion"].ToString());
                }

                return new MercuryInspectionResult();
            }
            catch (Exception ex)
            {
                return new MercuryInspectionResult(ex.Message);
            }
        }

        private MercurySystemCodeFormat GetSystemCode()
        {
            var systemCodeText = code.FindAll(x => x.Text.StartsWith('#')).ToList();
            var systemCodeParseResult = MercurySystemCodeCollection.Parse(systemCodeText);
            lineNumber = systemCodeText.Max(x => x.LineNumber);

            if (!systemCodeParseResult.IsOk)
            {
                throw new Exception(systemCodeParseResult.ErrorMessage);
            }

            if (systemCodeParseResult.SystemCode == null)
            {
                throw new SystemException(Delegater.CurrentLanguageDictionary["SystemError"].ToString());
            }

            return systemCodeParseResult.SystemCode;
        }
    }
}
