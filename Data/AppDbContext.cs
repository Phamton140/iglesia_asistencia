using IglesiaAsistencia.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace IglesiaAsistencia.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Persona> Personas { get; set; }
        public DbSet<Asistencia> Asistencias { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={AppConfig.DatabasePath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Persona>()
                .HasMany(p => p.Asistencias)
                .WithOne(a => a.Persona)
                .HasForeignKey(a => a.PersonaId);
        }
    }
}
