using FitQuest.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FitQuest.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurări suplimentare (dacă ai nevoie)
            // modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        }
    }
}