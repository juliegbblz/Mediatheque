using System;
using System.ComponentModel.DataAnnotations;

namespace Mediatheque.Data
{
    public class Entrainement
    {
        [Key]
        public int Id { get; set; }

        public string Activite { get; set; } = string.Empty;

        public DateTime DateHeure { get; set; }

        public string Lieu { get; set; } = string.Empty;

        //Durée en minutes       
        public int DureeMinutes { get; set; }

        //Clés étrangères
        public int CategorieActiviteId { get; set; }
        public virtual CategorieActivite Categorie { get; set; }
    }
}