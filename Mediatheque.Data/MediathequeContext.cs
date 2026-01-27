using Microsoft.EntityFrameworkCore;
using Mediatheque.Data;

namespace Mediatheque.Data
{
    public class MediathequeContext : DbContext
    {
        // Constructeur nécessaire pour les outils de migration et l'injection de dépendances
        public MediathequeContext(DbContextOptions<MediathequeContext> options) : base(options)
        {
        }

        // Constructeur vide (optionnel mais utile si tu n'utilises pas l'injection)
        public MediathequeContext()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // On ne configure le SQLite ici QUE si les options n'ont pas déjà été définies ailleurs
            if (!options.IsConfigured)
            {
                options.UseSqlite("Data Source=mediatheque.db");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Tes données par défaut (Excellent !)
            modelBuilder.Entity<CategorieActivite>().HasData(
                new CategorieActivite { Id = 1, Nom = "Sport individuel", CouleurHex = "#FF5733" },
                new CategorieActivite { Id = 2, Nom = "Sport Collectif", CouleurHex = "#2ECC71" },
                new CategorieActivite { Id = 3, Nom = "Bien-être", CouleurHex = "#9B59B6" },
                new CategorieActivite { Id = 4, Nom = "Sport de combat", CouleurHex = "#3498DB" },
                new CategorieActivite { Id = 5, Nom = "Compétition/tournoi", CouleurHex = "#FFEB3B" }
            );

            // Optionnel : Assurer que la relation est bien comprise par EF
            modelBuilder.Entity<Entrainement>()
                .HasOne(e => e.Categorie)
                .WithMany()
                .HasForeignKey(e => e.CategorieActiviteId);
        }

        public DbSet<Entrainement> Entrainements { get; set; }
        public DbSet<CategorieActivite> Categories { get; set; }
    }
}