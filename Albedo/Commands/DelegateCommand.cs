using System;
using System.Windows.Input;

namespace Albedo.Commands
{
    public class DelegateCommand : ICommand
    {
#pragma warning disable CS0414
        public event EventHandler? CanExecuteChanged = null;
#pragma warning restore CS0414
        private readonly Action<object?> execute;

        public DelegateCommand(Action<object?> execute)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            execute(parameter);
        }
    }
}
