﻿using ICSharpCode.AvalonEdit;

using MercuryEditor.IO;

using System;
using System.Windows.Input;

namespace MercuryEditor.Commands
{
    public class NewCommand : ICommand
    {
        private readonly TextEditor editor;
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public NewCommand(TextEditor editor)
        {
            this.editor = editor;
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            if (!Delegater.CheckSave())
            {
                return;
            }
            editor.Clear();
            TmFile.CurrentFilePath = string.Empty;
            TmFile.IsSaved = true;
            Delegater.RefreshFileName();
        }
    }
}
