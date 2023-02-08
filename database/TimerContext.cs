using Microsoft.EntityFrameworkCore;

namespace Database
{
    public class TimerContext : DbContext
    {
        public DbSet<Timer> Timers { get; set; } = null!;
        public DbSet<RepeatTimer> RepeatTimers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Timer>().ToTable("Timers");
            modelBuilder.Entity<Timer>().HasIndex(timer => new { timer.Channel, timer.TimerName }).IsUnique();

            modelBuilder.Entity<RepeatTimer>().ToTable("RepeatTimers");
            modelBuilder.Entity<Timer>().HasIndex(timer => new { timer.Channel, timer.TimerName }).IsUnique();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Username=postgres;Password=password;Database=HealthyBot");
        }
    }
}