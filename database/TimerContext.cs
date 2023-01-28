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
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Username=postgres;Password=password;Database=HealthyBot");
            // optionsBuilder.UseSqlServer(@"Server=.\SQLEXPRESS;Database=SchoolDB;Trusted_Connection=True;");
        }

        public DbSet<Timer> timers { get; set; } = null!;
    }
}