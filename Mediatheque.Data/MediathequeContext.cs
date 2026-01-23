using Microsoft.EntityFrameworkCore;

namespace Mediatheque.Data
{
    public class MediathequeContext : DbContext
    {
        public MediathequeContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<Entrainement> Entrainements { get; set; }
    }
}