using Microsoft.EntityFrameworkCore;
using Mediatheque.Data;

namespace Mediatheque.Data
{
    public class MediathequeContext : DbContext
    {
        /// <summary>
        /// Initialise le contexte avec des options externes (utile pour l'injection de dépendances).
        /// </summary>
        public MediathequeContext(DbContextOptions<MediathequeContext> options) : base(options)
        {
        }

        /// <summary>
        /// Constructeur par défaut permettant l'instanciation manuelle du contexte.
        /// </summary>
        public MediathequeContext()
        {
        }

        /// <summary>
        /// Définit la source de données SQLite si aucune configuration externe n'est fournie.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
            {
                options.UseSqlite("Data Source=mediatheque.db");
            }
        }

        /// <summary>
        /// Configure le schéma de la base de données, les relations et le jeu de données initial (seeding).
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Initialisation des catégories de sport par défaut
            modelBuilder.Entity<CategorieActivite>().HasData(
                new CategorieActivite { Id = 1, Nom = "Sport individuel", CouleurHex = "#FF5733" },
                new CategorieActivite { Id = 2, Nom = "Sport Collectif", CouleurHex = "#2ECC71" },
                new CategorieActivite { Id = 3, Nom = "Bien-être", CouleurHex = "#9B59B6" },
                new CategorieActivite { Id = 4, Nom = "Sport de combat", CouleurHex = "#3498DB" },
                new CategorieActivite { Id = 5, Nom = "Compétition/tournoi", CouleurHex = "#FFEB3B" }
            );

            // Configuration de la relation un-à-plusieurs : un Entrainement appartient à une Categorie
            modelBuilder.Entity<Entrainement>()
                .HasOne(e => e.Categorie)
                .WithMany()
                .HasForeignKey(e => e.CategorieActiviteId);
        }

        // Tables de la base de données
        public DbSet<Entrainement> Entrainements { get; set; }
        public DbSet<CategorieActivite> Categories { get; set; }
    }
}