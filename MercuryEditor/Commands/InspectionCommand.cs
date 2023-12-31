﻿using ICSharpCode.AvalonEdit;

using MercuryEditor.Inspection;

using System;
using System.Windows.Input;

namespace MercuryEditor.Commands
{
    public class InspectionCommand : ICommand
    {
        private readonly TextEditor editor;
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public InspectionCommand(TextEditor editor)
        {
            this.editor = editor;
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            new SaveCommand(editor).Execute(parameter);

            var inspector = new MercuryInspector();
            var result = inspector.Run(editor.Text);

            if (!result.IsOk)
            {
                Delegater.SetEditorStatusText(result.ErrorMessage);
                return;
            }

            Delegater.SetEditorStatusText(Delegater.CurrentLanguageDictionary["InspectionComplete"].ToString());
        }
    }
}
