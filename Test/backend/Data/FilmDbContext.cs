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
    public DbSet<Sala> Sale { get; set; } = null!;
    public DbSet<Show> Shows { get; set; } = null!;
    public DbSet<Utente> Utenti { get; set; } = null!;
    public DbSet<Ruolo> Ruoli { get; set; } = null!;
    public DbSet<UtenteRuolo> UtentiRuoli { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Prenotazione> Prenotazioni { get; set; } = null!;
    public DbSet<Acquisto> Acquisti { get; set; } = null!;
    public DbSet<Biglietto> Biglietti { get; set; } = null!;
    public DbSet<CreditoUtente> CreditiUtente { get; set; } = null!;
    public DbSet<TransazioneCredito> TransazioniCredito { get; set; } = null!;
    public DbSet<PrenotazioneTemporanea> PrenotazioniTemporanee { get; set; } = null!;
    public DbSet<Categoria> Categorie { get; set; } = null!;
    public DbSet<FilmCategoria> FilmsCategorie { get; set; } = null!;
    public DbSet<ProiezioneSalvata> ProiezioniSalvate { get; set; } = null!;
    public DbSet<NotificaUtente> NotificheUtente { get; set; } = null!;

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
            entity.Property(e => e.Descrizione).HasMaxLength(2000);
            entity.Property(e => e.RegistaNome).HasMaxLength(100);
            entity.Property(e => e.Cast).HasMaxLength(1000);
            entity.Property(e => e.Featured).HasDefaultValue(false);
            entity.Property(e => e.Genere).HasMaxLength(50);

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
            entity.Property(e => e.PostiMassimi).IsRequired();
            entity.Property(e => e.Latitudine).HasColumnType("decimal(10,8)");
            entity.Property(e => e.Longitudine).HasColumnType("decimal(11,8)");
            entity.Property(e => e.CodiceLocale).HasMaxLength(20);
        });

        modelBuilder.Entity<Sala>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CinemaId, e.NumeroSala }).IsUnique();

            entity.HasOne(e => e.Cinema)
                .WithMany(c => c.Sale)
                .HasForeignKey(e => e.CinemaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Show>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SalaId, e.Data, e.OraInizio }).IsUnique();

            entity.HasOne(e => e.Sala)
                .WithMany(s => s.Shows)
                .HasForeignKey(e => e.SalaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Film)
                .WithMany(f => f.Shows)
                .HasForeignKey(e => e.FilmId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Proiezione>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Data).IsRequired();
            entity.Property(e => e.Ora).IsRequired();

            entity.HasOne(e => e.Show)
                .WithMany()
                .HasForeignKey(e => e.ShowId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Cinema)
                .WithMany(c => c.Proiezioni)
                .HasForeignKey(e => e.CinemaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Film)
                .WithMany(f => f.Proiezioni)
                .HasForeignKey(e => e.FilmId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.CinemaId, e.FilmId, e.Data, e.Ora }).IsUnique();
            entity.HasIndex(e => e.ShowId).IsUnique();
        });

        modelBuilder.Entity<Utente>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash);
            entity.Property(e => e.ExternalProvider).HasMaxLength(30);
            entity.Property(e => e.ExternalProviderUserId).HasMaxLength(200);
            entity.Property(e => e.Nome).HasMaxLength(100);
            entity.Property(e => e.Cognome).HasMaxLength(100);
            entity.Property(e => e.Telefono).HasMaxLength(20);
            entity.Property(e => e.DataRegistrazione).IsRequired();
            entity.Property(e => e.Attivo).IsRequired();
            entity.Property(e => e.PreferredPaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PreferredPaymentMethodLabel).HasMaxLength(120);

            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => new { e.ExternalProvider, e.ExternalProviderUserId }).IsUnique();

            entity.HasOne(e => e.PreferredCinema)
                .WithMany()
                .HasForeignKey(e => e.PreferredCinemaId)
                .OnDelete(DeleteBehavior.SetNull);
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
            entity.Property(e => e.TokenHash).IsRequired().HasMaxLength(256);
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ReplacedByTokenHash).HasMaxLength(256);
            entity.Property(e => e.CreatedByIp).HasMaxLength(64);
            entity.Property(e => e.CreatedByUserAgent).HasMaxLength(256);
            entity.Property(e => e.RevokedByIp).HasMaxLength(64);
            entity.Property(e => e.RevokedByUserAgent).HasMaxLength(256);
            entity.HasIndex(e => e.TokenHash).IsUnique();

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

        modelBuilder.Entity<Acquisto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MetodoPagamento).HasMaxLength(50);
            entity.Property(e => e.MetodoPagamentoEtichetta).HasMaxLength(120);
            entity.Property(e => e.MetodoPagamentoSalvato).IsRequired();

            entity.HasOne(e => e.Utente)
                .WithMany(u => u.Acquisti)
                .HasForeignKey(e => e.UtenteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Show)
                .WithMany()
                .HasForeignKey(e => e.ShowId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Biglietto>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CodiceUnivoco).IsUnique();
            entity.HasIndex(e => e.CodiceHash).IsUnique();

            entity.HasOne(e => e.Acquisto)
                .WithMany(a => a.Biglietti)
                .HasForeignKey(e => e.AcquistoId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Show)
                .WithMany(s => s.Biglietti)
                .HasForeignKey(e => e.ShowId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CreditoUtente>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UtenteId).IsUnique();

            entity.HasOne(e => e.Utente)
                .WithOne(u => u.Credito)
                .HasForeignKey<CreditoUtente>(e => e.UtenteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TransazioneCredito>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Utente)
                .WithMany()
                .HasForeignKey(e => e.UtenteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Acquisto)
                .WithMany()
                .HasForeignKey(e => e.AcquistoId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PrenotazioneTemporanea>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ShowId, e.Posto });
            entity.HasIndex(e => e.DataScadenza);
            entity.HasIndex(e => e.SessionId);

            entity.HasOne(e => e.Show)
                .WithMany(s => s.PrenotazioniTemporanee)
                .HasForeignKey(e => e.ShowId)
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

        modelBuilder.Entity<NotificaUtente>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UtenteId, e.DataCreazione });
            entity.HasIndex(e => new { e.UtenteId, e.Letta });
            entity.HasIndex(e => new { e.UtenteId, e.DedupeKey }).IsUnique();

            entity.HasOne(e => e.Utente)
                .WithMany(u => u.Notifiche)
                .HasForeignKey(e => e.UtenteId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
