using Microsoft.EntityFrameworkCore;
using FilmAPI.Model;

namespace FilmAPI.Data;

public class FilmDbContext : DbContext
{
    public FilmDbContext(DbContextOptions<FilmDbContext> options) : base(options)
    {
    }

    public DbSet<Regista> Registi { get; set; } = null!;
    public DbSet<Film> Films { get; set; } = null!;
    public DbSet<Cinema> Cinemas { get; set; } = null!;
    public DbSet<Proiezione> Proiezioni { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Regista>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Cognome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Nazionalita).HasMaxLength(100);
        });

        modelBuilder.Entity<Film>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Titolo).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DataProduzione).IsRequired();
            entity.Property(e => e.Durata).IsRequired();
            entity.Property(e => e.CopertinaPath).HasMaxLength(500);
            entity.Property(e => e.FilmatoPath).HasMaxLength(500);

            entity.HasOne(e => e.Regista)
                .WithMany(r => r.Films)
                .HasForeignKey(e => e.RegistaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Cinema>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Indirizzo).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Citta).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<Proiezione>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Data).IsRequired();
            entity.Property(e => e.Ora).IsRequired();

            entity.HasOne(e => e.Cinema)
                .WithMany(c => c.Proiezioni)
                .HasForeignKey(e => e.CinemaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Film)
                .WithMany(f => f.Proiezioni)
                .HasForeignKey(e => e.FilmId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.CinemaId, e.FilmId, e.Data, e.Ora }).IsUnique();
        });
    }
}
