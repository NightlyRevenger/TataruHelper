// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FFXIITataruHelper.ViewModel
{
    public class TataruUICommand : ICommand
    {
        protected Action _action = null;
        protected Action<object> _parameterizedAction = null;

        private bool _canExecute = false;

        /// <summary>
        /// Creates instance of the command handler
        /// </summary>
        /// <param name="action">Action to be executed by the command</param>
        /// <param name="canExecute">A bolean property to containing current permissions to execute the command</param>
        public TataruUICommand(Action action, bool canExecute = true)
        {
            _action = action;
            _canExecute = canExecute;
        }

        public TataruUICommand(Action<object> parameterizedAction, bool canExecute = true)
        {
            //  Set the action.
            _parameterizedAction = parameterizedAction;
            _canExecute = canExecute;
        }

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
            return _canExecute;
        }

        public void Execute(object parameter)
        {
            if (_action != null)
                _action();
            else
                _parameterizedAction(parameter);
        }
    }
}