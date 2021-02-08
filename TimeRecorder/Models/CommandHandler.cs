using System;
using System.Windows.Input;

namespace TimeRecorder.Models
{
    public class CommandHandler : CommandHandler<object>
    {
        public CommandHandler(Action action, Func<bool> canExecute) : base(_ => action(), canExecute)
        { }

        public CommandHandler(Action action) : this(action, () => true)
        { }
    }

    public class CommandHandler<T> : ICommand
    {
        private readonly Action<T> _action;
        private readonly Func<bool> _canExecute;

        public CommandHandler(Action<T> action, Func<bool> canExecute)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public CommandHandler(Action<T> action) : this(action, () => true)
        { }

        /// <summary>
        /// Wires CanExecuteChanged event
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Forcess checking if execute is allowed
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute.Invoke();
        }

        public void Execute(object parameter)
        {
            if (parameter == null)
            {
#pragma warning disable CS8604 // Possible null reference argument.
                _action(default);
#pragma warning restore CS8604 // Possible null reference argument.
            }
            else if (parameter is T typedParameter)
            {
                _action(typedParameter);
            }
            //else if (parameter is string strParam)
            //{
            //    if (typeof(T) == typeof(int) && int.TryParse(strParam, out var number))
            //    {
            //        _action((T)number);
            //    }
            //}
        }
    }
}