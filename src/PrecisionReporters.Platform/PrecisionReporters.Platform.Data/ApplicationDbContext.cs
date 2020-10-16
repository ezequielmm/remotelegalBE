using Microsoft.EntityFrameworkCore;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Data
{
    public class ApplicationDbContext: DbContext
    {
        public DbSet<Case> Cases { get; set; }

        public ApplicationDbContext(DbContextOptions options) : base(options) { }

        public ApplicationDbContext() { }

    }
}
