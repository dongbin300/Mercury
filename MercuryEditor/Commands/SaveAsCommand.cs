using ICSharpCode.AvalonEdit;

using MercuryEditor.IO;

using System;
using System.Windows.Input;

namespace MercuryEditor.Commands
{
    public class SaveAsCommand : ICommand
    {
        private readonly TextEditor editor;
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public SaveAsCommand(TextEditor editor)
        {
            this.editor = editor;
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            TmFile.SaveAs(editor.Text);
            Delegater.RefreshFileName();
        }
    }
}
