using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mediatheque.Data
{
    public class CategorieActivite
    {
        public int Id { get; set; }
        public string Nom { get; set; }
        public string CouleurHex { get; set; } 
        public virtual ICollection<Entrainement> Entrainements { get; set; }
    }

}
