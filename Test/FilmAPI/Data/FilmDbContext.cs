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
    public DbSet<Utente> Utenti { get; set; } = null!;
    public DbSet<Ruolo> Ruoli { get; set; } = null!;
    public DbSet<UtenteRuolo> UtentiRuoli { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Prenotazione> Prenotazioni { get; set; } = null!;
    public DbSet<Categoria> Categorie { get; set; } = null!;
    public DbSet<FilmCategoria> FilmsCategorie { get; set; } = null!;
    public DbSet<ProiezioneSalvata> ProiezioniSalvate { get; set; } = null!;

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

        modelBuilder.Entity<Utente>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Nome).HasMaxLength(100);
            entity.Property(e => e.Cognome).HasMaxLength(100);
            entity.Property(e => e.Telefono).HasMaxLength(20);
            entity.Property(e => e.DataRegistrazione).IsRequired();
            entity.Property(e => e.Attivo).IsRequired();

            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Ruolo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Descrizione).HasMaxLength(255);

            entity.HasIndex(e => e.Nome).IsUnique();
        });

        modelBuilder.Entity<UtenteRuolo>(entity =>
        {
            entity.HasKey(e => new { e.UtenteId, e.RuoloId });

            entity.HasOne(e => e.Utente)
                .WithMany(u => u.UtentiRuoli)
                .HasForeignKey(e => e.UtenteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Ruolo)
                .WithMany(r => r.UtentiRuoli)
                .HasForeignKey(e => e.RuoloId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(256);
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.HasIndex(e => e.Token).IsUnique();

            entity.HasOne(e => e.Utente)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UtenteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Prenotazione>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DataPrenotazione).IsRequired();
            entity.Property(e => e.NumeroPosti).IsRequired();

            entity.HasOne(e => e.Utente)
                .WithMany(u => u.Prenotazioni)
                .HasForeignKey(e => e.UtenteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Proiezione)
                .WithMany()
                .HasForeignKey(e => e.ProiezioneId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Descrizione).HasMaxLength(500);

            entity.HasIndex(e => e.Nome).IsUnique();
        });

        modelBuilder.Entity<FilmCategoria>(entity =>
        {
            entity.HasKey(e => new { e.FilmId, e.CategoriaId });

            entity.HasOne(e => e.Film)
                .WithMany(f => f.FilmsCategorie)
                .HasForeignKey(e => e.FilmId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Categoria)
                .WithMany(c => c.FilmsCategorie)
                .HasForeignKey(e => e.CategoriaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProiezioneSalvata>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DataSalvataggio).IsRequired();
            entity.Property(e => e.Prenotato).IsRequired();
            entity.Property(e => e.NumeroPosti).IsRequired();

            entity.HasOne(e => e.Utente)
                .WithMany(u => u.ProiezioniSalvate)
                .HasForeignKey(e => e.UtenteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Proiezione)
                .WithMany()
                .HasForeignKey(e => e.ProiezioneId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
