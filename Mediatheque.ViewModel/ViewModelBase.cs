using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mediatheque.ViewModel
{
    public class ViewModelBase : INotifyPropertyChanged // WPF s'abonne à PropertyChanged
    {
        private static readonly Dictionary<string, PropertyChangedEventArgs> PROPERTY_CHANGED_CACHE = new ();

        private static PropertyChangedEventArgs GetPropertyChangedEventArgs(string propertyName)
        {
            if (!PROPERTY_CHANGED_CACHE.TryGetValue(propertyName, out PropertyChangedEventArgs? e))
            {
                e = new PropertyChangedEventArgs(propertyName);
                PROPERTY_CHANGED_CACHE.Add(propertyName, e);
            }

            return e;
        }

       
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, GetPropertyChangedEventArgs(propertyName));
            }
        }

        protected void OnPropertyChanged(params string[] propertyNames)
        {
            if (PropertyChanged != null)
            {
                for (int i = 0; i < propertyNames.Length; i++)
                {
                    PropertyChanged(this, GetPropertyChangedEventArgs(propertyNames[i]));
                }
            }
        }
    }
}
