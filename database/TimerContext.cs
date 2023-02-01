using Microsoft.EntityFrameworkCore;

namespace Database
{
    public class TimerContext : DbContext
    {
        /* public TimerContext(DbContextOptions<TimerContext> options)
            : base(options)
        {
        } */

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Timer>().ToTable("Timers");
            modelBuilder.Entity<RepeatTimer>().ToTable("RepeatTimers");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Username=postgres;Password=password;Database=HealthyBot");
            // optionsBuilder.UseSqlServer(@"Server=.\SQLEXPRESS;Database=SchoolDB;Trusted_Connection=True;");
        }

        public DbSet<Timer> Timers { get; set; } = null!;
        public DbSet<RepeatTimer> RepeatTimers { get; set; } = null!;
    }
}