using Microsoft.EntityFrameworkCore;

namespace EventEaseAppOwethuHadebeMVC.Models
{
    public class ApplicationDbContext : DbContext 
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) 
        { 

        }
        public DbSet<Venue> Venues { get; set; }
        public DbSet <Event> Events { get; set; }
        public DbSet <Booking> Bookings { get; set; }

    }
}
