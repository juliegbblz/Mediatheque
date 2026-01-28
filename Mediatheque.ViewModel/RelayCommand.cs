using System;
using System.Windows.Input;

namespace Mediatheque.ViewModel
{
    /// <summary>
    /// Implémentation simple de ICommand pour relier une action
    /// sans paramètre à un contrôle XAML (bouton, menu, etc.).
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _action;

        public RelayCommand(Action action)
        {
            _action = action;
        }

        /// <summary>
        /// Événement requis par ICommand.
        /// Non utilisé ici car la commande est toujours exécutable.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        /// <summary>
        /// Indique que la commande peut toujours être exécutée.
        /// </summary>
        public bool CanExecute(object? parameter) => true;

        /// <summary>
        /// Exécute l’action associée à la commande.
        /// </summary>
        public void Execute(object? parameter)
        {
            _action();
        }
    }

    /// <summary>
    /// Variante générique de RelayCommand permettant
    /// de transmettre un paramètre typé depuis la vue.
    /// </summary>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _action;

        public RelayCommand(Action<T> action)
        {
            _action = action;
        }

        /// <summary>
        /// Événement requis par ICommand.
        /// Non déclenché car la commande reste toujours valide.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        /// <summary>
        /// La commande est exécutable uniquement si
        /// le paramètre est du type attendu.
        /// </summary>
        public bool CanExecute(object? parameter) => parameter is T;

        /// <summary>
        /// Exécute l’action en castant le paramètre
        /// vers le type générique T.
        /// </summary>
        public void Execute(object? parameter)
        {
            if (parameter is T p)
                _action(p);
        }
    }
}
