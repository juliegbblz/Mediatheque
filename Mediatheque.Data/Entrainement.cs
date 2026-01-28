using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mediatheque.Data
{
    public class Entrainement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Activite { get; set; } = string.Empty;

        public DateTime DateHeure { get; set; }

        public string Lieu { get; set; } = string.Empty;
        public int DureeMinutes { get; set; }

        //Clés étrangères
        public int CategorieActiviteId { get; set; }
        public virtual CategorieActivite Categorie { get; set; }
    }
}