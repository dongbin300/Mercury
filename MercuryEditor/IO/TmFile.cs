using Mercury;

using Microsoft.Win32;

using System.IO;

namespace MercuryEditor.IO
{
    /// <summary>
    /// Trading Model File
    /// </summary>
    internal class TmFile
    {
        public static string CurrentFilePath = string.Empty;
        public static string TmName => (CurrentFilePath == string.Empty ? string.Empty : CurrentFilePath.GetOnlyFileName()) + (IsSaved ? "" : "*");
        public static bool IsSaved = true;

        public static void Save(string codeText)
        {
            if (CurrentFilePath == string.Empty)
            {
                SaveFileDialog dialog = new()
                {
                    Title = Delegater.CurrentLanguageDictionary["TmFileSave"].ToString(),
                    Filter = "trading model files (*.tm)|*.tm"
                };

                if (dialog.ShowDialog() ?? true)
                {
                    IsSaved = true;
                    CurrentFilePath = dialog.FileName;
                    File.WriteAllText(dialog.FileName, codeText);
                }
            }
            else
            {
                IsSaved = true;
                File.WriteAllText(CurrentFilePath, codeText);
            }
        }

        public static void SaveAs(string codeText)
        {
            SaveFileDialog dialog = new()
            {
                Title = Delegater.CurrentLanguageDictionary["TmFileSaveAs"].ToString(),
                Filter = "trading model files (*.tm)|*.tm"
            };

            if (dialog.ShowDialog() ?? true)
            {
                IsSaved = true;
                CurrentFilePath = dialog.FileName;
                File.WriteAllText(dialog.FileName, codeText);
            }
        }

        public static string Open()
        {
            OpenFileDialog dialog = new()
            {
                Title = Delegater.CurrentLanguageDictionary["TmFileOpen"].ToString(),
                Filter = "trading model files (*.tm)|*.tm"
            };

            if (dialog.ShowDialog() ?? true)
            {
                IsSaved = true;
                CurrentFilePath = dialog.FileName;
                return File.ReadAllText(dialog.FileName);
            }

            return string.Empty;
        }
    }
}
