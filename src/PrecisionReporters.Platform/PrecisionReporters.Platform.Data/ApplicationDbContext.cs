using Microsoft.EntityFrameworkCore;
using PrecisionReporters.Platform.Data.Entities;

namespace PrecisionReporters.Platform.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Case> Cases { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<VerifyUser> VerifyUsers { get; set; }

        public DbSet<Room> Rooms { get; set; }
        public DbSet<Member> Members { get; set; }

        public ApplicationDbContext(DbContextOptions options) : base(options) { }

        public ApplicationDbContext() { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(u => u.HasIndex(e => e.EmailAddress).IsUnique());
        }
    }
}
