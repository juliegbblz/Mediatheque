using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Mediatheque.ViewModel
{
    public class RelayCommand : ICommand
    {
        // Action est un délégué vers une fonction void().
        
        private readonly Action _action;

        public RelayCommand(Action action)
        {
            _action = action;
        }

        // Evènement jamais déclenché (CanExecute retourne toujours true).
        // add et remove sont détaillées sans gérer de liste d'abonnés.
       
        public event EventHandler? CanExecuteChanged
        {
           add { } // Abonnement à l'évènement (CanExecuteChanged += ...)
           remove { } // Désabonnement à l'évènement (CanExecuteChanged -= ...)
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            _action();
        }
    }

    public class RelayCommand<T> : ICommand
    {
        // Action est un délégué vers une fonction void(T parameter).
        
        private readonly Action<T> _action;

        public RelayCommand(Action<T> action)
        {
            _action = action;
        }

        // Evènement jamais déclenché (CanExecute retourne toujours true).
        // add et remove sont détaillées sans gérer de liste d'abonnés.
        
        public event EventHandler? CanExecuteChanged
        {
            add { } // Abonnement à l'évènement (CanExecuteChanged += ...)
            remove { } // Désabonnement à l'évènement (CanExecuteChanged -= ...)
        }

        public bool CanExecute(object? parameter) => parameter is T;

        public void Execute(object? parameter)
        {
            if (parameter is T p) _action(p);
        }
    }
}
