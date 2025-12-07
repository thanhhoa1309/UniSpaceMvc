using Microsoft.EntityFrameworkCore;
using UniSpace.Model.Entities;

namespace UniSpace.Model
{
    public class UniSpaceMvcDbContext : DbContext
    {
        public UniSpaceMvcDbContext() { }

        public UniSpaceMvcDbContext(DbContextOptions<UniSpaceMvcDbContext> options)
            : base(options) { }

        public DbSet<User> User { get; set; }
        public DbSet<Campus> Campuses { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<RoomReport> RoomReports { get; set; }

        public DbSet<Schedule> Schedules { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Connection string mặc định cho design-time (migrations)
                // Port 5433 cho localhost khi chạy Docker Compose
                optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=UnispaceDb;Username=postgres;Password=postgres;Pooling=true");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            modelBuilder.Entity<Room>()
                .Property(r => r.Type)
                .HasConversion<string>();

            modelBuilder.Entity<Room>()
                .Property(r => r.CurrentStatus)
                .HasConversion<string>();

            modelBuilder.Entity<Room>()
                .Property(r => r.RoomStatus)
                .HasConversion<string>();

            modelBuilder.Entity<Booking>()
                .Property(b => b.Status)
                .HasConversion<string>();

            modelBuilder.Entity<RoomReport>()
                .Property(rr => rr.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Schedule>()
                .Property(s => s.ScheduleType)
                .HasConversion<string>();

            modelBuilder.Entity<Room>()
                .HasOne(r => r.Campus)
                .WithMany(c => c.Rooms)
                .HasForeignKey(r => r.CampusId);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Room)
                .WithMany(r => r.Bookings)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomReport>()
                .HasOne(rr => rr.User)
                .WithMany(u => u.Reports)
                .HasForeignKey(rr => rr.UserId);

            modelBuilder.Entity<RoomReport>()
                .HasOne(rr => rr.Room)
                .WithMany(r => r.Reports)
                .HasForeignKey(rr => rr.RoomId);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Room)
                .WithMany(r => r.Schedule)
                .HasForeignKey(s => s.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        }


    }
}
