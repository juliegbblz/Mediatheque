using Microsoft.EntityFrameworkCore;
using Mediatheque.Data;

namespace Mediatheque.Data
{
    /// <summary>
    /// Contexte Entity Framework Core de l’application.
    /// Centralise l’accès à la base de données SQLite
    /// et définit le modèle relationnel.
    /// </summary>
    public class MediathequeContext : DbContext
    {
        /// <summary>
        /// Constructeur utilisé lorsque le DbContext est configuré
        /// depuis l’extérieur (ex : injection de dépendances).
        /// </summary>
        public MediathequeContext(DbContextOptions<MediathequeContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Constructeur sans options, utilisé lors d’une instanciation manuelle.
        /// La configuration est alors fournie dans OnConfiguring.
        /// </summary>
        public MediathequeContext()
        {
        }

        /// <summary>
        /// Définit la configuration minimale du contexte
        /// lorsque aucune option n’a été injectée.
        /// </summary>
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
            {
                options.UseSqlite("Data Source=mediatheque.db");
            }
        }

        /// <summary>
        /// Décrit le modèle de données :
        /// - données initiales (seeding)
        /// - relations entre entités
        /// - contraintes de mapping
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Insertion automatique des catégories lors de la création/migration de la base
            modelBuilder.Entity<CategorieActivite>().HasData(
                new CategorieActivite { Id = 1, Nom = "Sport individuel", CouleurHex = "#FF5733" },
                new CategorieActivite { Id = 2, Nom = "Sport Collectif", CouleurHex = "#2ECC71" },
                new CategorieActivite { Id = 3, Nom = "Bien-être", CouleurHex = "#9B59B6" },
                new CategorieActivite { Id = 4, Nom = "Sport de combat", CouleurHex = "#3498DB" },
                new CategorieActivite { Id = 5, Nom = "Compétition/tournoi", CouleurHex = "#E6B200" }
            );

            // Relation : chaque entraînement est rattaché à une catégorie unique
            modelBuilder.Entity<Entrainement>()
                .HasOne(e => e.Categorie)
                .WithMany()
                .HasForeignKey(e => e.CategorieActiviteId);
        }

        /// <summary>
        /// Représente la table des entraînements.
        /// </summary>
        public DbSet<Entrainement> Entrainements { get; set; }

        /// <summary>
        /// Représente la table des catégories d’activités.
        /// </summary>
        public DbSet<CategorieActivite> Categories { get; set; }
    }
}
