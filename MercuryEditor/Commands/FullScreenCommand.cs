using System;
using System.Windows.Input;

namespace MercuryEditor.Commands
{
    public class FullScreenCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public FullScreenCommand()
        {

        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            Delegater.SetFullScreen();
        }
    }
}
