using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Mediatheque.ViewModel
{
    /// <summary>
    /// Classe de base pour tous les ViewModels.
    /// Implémente INotifyPropertyChanged afin de notifier automatiquement la vue
    /// lors des changements de propriétés.
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Cache partagé des PropertyChangedEventArgs afin d’éviter
        /// des allocations répétées lors des notifications.
        /// </summary>
        private static readonly Dictionary<string, PropertyChangedEventArgs> PROPERTY_CHANGED_CACHE = new();

        /// <summary>
        /// Récupère (ou crée) une instance de PropertyChangedEventArgs
        /// associée au nom de propriété fourni.
        /// </summary>
        private static PropertyChangedEventArgs GetPropertyChangedEventArgs(string propertyName)
        {
            if (!PROPERTY_CHANGED_CACHE.TryGetValue(propertyName, out PropertyChangedEventArgs? e))
            {
                e = new PropertyChangedEventArgs(propertyName);
                PROPERTY_CHANGED_CACHE.Add(propertyName, e);
            }

            return e;
        }

        /// <summary>
        /// Événement déclenché lorsqu’une propriété change de valeur.
        /// WPF s’y abonne automatiquement pour mettre à jour l’interface.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Notifie un changement pour une seule propriété.
        /// </summary>
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, GetPropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Notifie plusieurs changements de propriétés en une seule fois.
        /// Utile lorsque plusieurs valeurs dépendent d’une même modification.
        /// </summary>
        protected void OnPropertyChanged(params string[] propertyNames)
        {
            if (PropertyChanged == null) return;

            for (int i = 0; i < propertyNames.Length; i++)
            {
                PropertyChanged(this, GetPropertyChangedEventArgs(propertyNames[i]));
            }
        }
    }
}
