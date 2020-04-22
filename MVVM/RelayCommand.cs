using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Autodesk.Revit.Exceptions;
using Hangfire.Annotations;
namespace UpdateLevelByCategory.MVVM
{
   public class RelayCommand : ICommand//инкапсуляция
    {
        readonly Action<object> _execute;
        readonly Predicate<object> _canExecute;
        public RelayCommand(Action<object> execute)
            : this(execute, null)
        {

        }
        private RelayCommand([CanBeNull] Action<object> execute, Predicate<object> canExecute)
        {
            if (execute == null)
            {
                throw new System.ArgumentNullException("execute//выполнить");
            }

            _execute = execute;
            _canExecute = canExecute;
        }
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        [DebuggerStepThrough]
        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
