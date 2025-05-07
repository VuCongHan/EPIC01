using Microsoft.EntityFrameworkCore;

namespace EPIC01.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Document> Documents { get; set; }
    }
}
