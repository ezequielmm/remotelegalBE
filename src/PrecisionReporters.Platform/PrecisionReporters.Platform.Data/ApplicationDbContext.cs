using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PrecisionReporters.Platform.Data.Entities;
using PrecisionReporters.Platform.Data.Enums;
using PrecisionReporters.Platform.Data.Seeds;

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
        public DbSet<Document> Documents { get; set; }
        public DbSet<DocumentUserDeposition> DocumentUserDepositions { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserResourceRole> UserResourceRoles { get; set; }
        public DbSet<DepositionEvent> DepositionEvents { get; set; }
        public DbSet<AnnotationEvent> AnnotationEvents { get; set; }
        public DbSet<Transcription> Transcriptions { get; set; }
        public DbSet<BreakRoom> BreakRooms { get; set; }
        public DbSet<BreakRoomAttendee> BreakRoomsAttendees { get; set; }
        public DbSet<ActivityHistory> ActivityHistories { get; set; }
        public DbSet<DeviceInfo> DevicesInfo { get; set; }
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
                .HasConversion(new EnumToStringConverter<ParticipantType>());

            modelBuilder.Entity<Deposition>()
                .Property(x => x.Status)
                .HasConversion(new EnumToStringConverter<DepositionStatus>());

            modelBuilder.Entity<Case>()
                .HasMany(x => x.Depositions)
                .WithOne(x => x.Case);

            modelBuilder.Entity<DepositionEvent>()
                .Property(x => x.EventType)
                .HasConversion(new EnumToStringConverter<EventType>());

            modelBuilder.Entity<UserResourceRole>().Property(x => x.ResourceType).HasConversion(new EnumToStringConverter<ResourceType>());

            modelBuilder.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.Action });
            modelBuilder.Entity<RolePermission>().Property(x => x.Action).HasConversion(new EnumToStringConverter<ResourceAction>());

            modelBuilder.Entity<UserResourceRole>().HasKey(x => new { x.RoleId, x.ResourceId, x.UserId });

            modelBuilder.Entity<Role>().Property(x => x.Name).HasConversion(new EnumToStringConverter<RoleName>());

            modelBuilder.Entity<AnnotationEvent>()
                .Property(x => x.Action)
                .HasConversion(new EnumToStringConverter<AnnotationAction>());

            modelBuilder.Entity<Document>()
                .Property(x => x.DocumentType)
                .HasConversion(new EnumToStringConverter<DocumentType>());

            modelBuilder.Entity<VerifyUser>()
                .Property(x => x.VerificationType)
                .HasConversion(new EnumToStringConverter<VerificationType>());

            modelBuilder.Entity<ActivityHistory>()
                .Property(x => x.Action)
                .HasConversion(new EnumToStringConverter<ActivityHistoryAction>());

            modelBuilder.Entity<BreakRoomAttendee>().HasKey(x => new { x.BreakRoomId, x.UserId });

            modelBuilder.Entity<DeviceInfo>()
                .Property(x => x.CameraStatus)
                .HasConversion(new EnumToStringConverter<CameraStatus>());
            // Seeds
            modelBuilder.SeedRoles();
        }
    }
}
