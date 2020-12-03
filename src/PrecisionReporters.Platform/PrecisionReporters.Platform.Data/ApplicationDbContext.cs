using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;

namespace PrecisionReporters.Platform.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Case> Cases { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<VerifyUser> VerifyUsers { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Composition> Compositions { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Deposition> Depositions { get; set; }
        public DbSet<DepositionDocument> DepositionDocuments { get; set; }
        public DbSet<Participant> Participants { get; set; }

        public ApplicationDbContext(DbContextOptions options) : base(options) { }

        public ApplicationDbContext() { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(u => u.HasIndex(e => e.EmailAddress).IsUnique());

            modelBuilder.Entity<Room>(x => x.HasIndex(i => i.Name).IsUnique());

            modelBuilder.Entity<Room>()
                .HasOne(x => x.Composition)
                .WithOne(x => x.Room)
                .HasForeignKey<Composition>(c => c.RoomId);

            modelBuilder.Entity<Room>()
                .Property(x => x.Status)
                .HasConversion(new EnumToStringConverter<RoomStatus>());

            modelBuilder.Entity<Composition>()
                .Property(x => x.Status)
                .HasConversion(new EnumToStringConverter<CompositionStatus>());

            modelBuilder.Entity<Participant>()
                .Property(p => p.Role)
                .HasConversion(new EnumToStringConverter<ParticipantRole>());
        }
    }
}
