using ICSharpCode.AvalonEdit;

using MercuryEditor.IO;

using System;
using System.Windows.Input;

namespace MercuryEditor.Commands
{
    public class SaveCommand : ICommand
    {
        private readonly TextEditor editor;
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public SaveCommand(TextEditor editor)
        {
            this.editor = editor;
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            TmFile.Save(editor.Text);
            Delegater.RefreshFileName();
        }
    }
}
