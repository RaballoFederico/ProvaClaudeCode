# Piano di Lavoro - Iterazione 4

## Riepilogo delle Scelte Tecniche

| Aspetto | Scelta |
|---------|--------|
| Geolocalizzazione | API Geolocation browser |
| Piantina posti | Configurazione flessibile per sala |
| Prezzi | Prezzo per tipologia sala |
| QR Code | Codice hash di dati biglietto |

---

## Panoramica dell'Iterazione 4

Questa iterazione introduce un sistema completo per la gestione multi-sala dei cinema, la programmazione film, l'acquisto biglietti con gestione posti, il pagamento misto (credito + Stripe), e la validazione biglietti.

### Obiettivi Principali
1. **Gestione Cinema Multi-Sala**: Ogni cinema ha n sale con tipologie diverse
2. **Programmazione Utente**: Pagina simile a UCI Cinemas con filtri e selezione cinema
3. **Scheda Film**: Dettaglio film con show per data/sala
4. **Acquisto Biglietti**: Selezione posti con piantina interattiva
5. **Pagamento**: Mix credito piattaforma + Stripe
6. **Validazione Biglietti**: QR code con verifica hash

---

## Fase 1: Modello Dati - Nuove Entita

### 1.1 Nuova Entita: Sala

```csharp
public enum TipologiaSala
{
    ISENSE = 0,
    XL = 1,
    TRE_D = 2,  // 3D
    DUE_D = 3   // 2D
}

[Table("sale")]
public class Sala
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int CinemaId { get; set; }

    [ForeignKey(nameof(CinemaId))]
    public Cinema? Cinema { get; set; }

    [Required]
    public int NumeroSala { get; set; }  // 1, 2, 3, 10...

    [MaxLength(100)]
    public string? Nome { get; set; }  // "SALA IMAX"

    [Required]
    public TipologiaSala Tipologia { get; set; }

    [Required]
    public int NumeroFile { get; set; }

    public int? PostiPerFila { get; set; }  // Se null, configurazione per fila

    [Required]
    public int PostiTotali { get; set; }

    [MaxLength(2000)]
    public string? ConfigurazionePosti { get; set; }  // JSON

    public bool Attiva { get; set; } = true;

    public ICollection<Show> Shows { get; set; } = new List<Show>();
}
```

**Schema ConfigurazionePosti JSON:**
```json
{
    "file": [
        { "fila": 1, "posti": 15 },
        { "fila": 2, "posti": 16 },
        { "fila": 3, "posti": 16 },
        { "fila": 4, "posti": 15 },
        { "fila": 5, "posti": 14 }
    ],
    "settori": [
        { "nome": "PLATEA", "file_da": 1, "file_a": 5 }
    ]
}
```

### 1.2 Entita Rinominata: Show (ex Proiezione)

```csharp
public enum StatoShow
{
    PROGRAMMATO = 0,
    IN_CORSO = 1,
    TERMINATO = 2,
    CANCELLATO = 3
}

[Table("shows")]
public class Show
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int SalaId { get; set; }  // CAMBIATO da CinemaId

    [ForeignKey(nameof(SalaId))]
    public Sala? Sala { get; set; }

    [Required]
    public int FilmId { get; set; }

    [ForeignKey(nameof(FilmId))]
    public Film? Film { get; set; }

    [Required]
    public DateOnly Data { get; set; }

    [Required]
    public TimeOnly OraInizio { get; set; }

    [Required]
    public TimeOnly OraFine { get; set; }  // Calculated = OraInizio + Film.Durata

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PrezzoBase { get; set; }  // Prezzo per tipologia sala

    [Required]
    public StatoShow Stato { get; set; } = StatoShow.PROGRAMMATO;

    public ICollection<Biglietto> Biglietti { get; set; } = new List<Biglietto>();
    public ICollection<PrenotazioneTemporanea> PrenotazioniTemporanee { get; set; } = new List<PrenotazioneTemporanea>();
}

// UNIQUE INDEX: (SalaId, Data, OraInizio)
```

### 1.3 Aggiornamento Film

```csharp
// Nuovi campi da aggiungere a Film
[MaxLength(2000)]
public string? Descrizione { get; set; }  // Descrizione testuale del film

[MaxLength(100)]
public string? Regista { get; set; }  // Nome e cognome del regista (campo testuale)

[MaxLength(1000)]
public string? Cast { get; set; }  // JSON array di nomi o testo libero

public bool Featured { get; set; }  // Per tag "in evidenza"

public DateTime? DataRilascio { get; set; }  // Data uscita ufficiale

[MaxLength(50)]
public string? Genere { get; set; }  // Campo testuale per genere
```

### 1.4 Nuova Entita: Acquisto

```csharp
public enum StatoAcquisto
{
    PAGATO = 0,
    CANCELLED = 1,
    REFUNDED = 2
}

[Table("acquisti")]
public class Acquisto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UtenteId { get; set; }

    [ForeignKey(nameof(UtenteId))]
    public Utente Utente { get; set; } = null!;

    [Required]
    public int ShowId { get; set; }

    [ForeignKey(nameof(ShowId))]
    public Show Show { get; set; } = null!;

    [Required]
    public DateTime DataAcquisto { get; set; } = DateTime.UtcNow;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal ImportoTotale { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal CreditoUsato { get; set; }

    [MaxLength(100)]
    public string? StripeChargeId { get; set; }

    [Required]
    public StatoAcquisto Stato { get; set; } = StatoAcquisto.PAGATO;

    [Required]
    [MaxLength(36)]
    public string CodiceConferma { get; set; } = Guid.NewGuid().ToString();

    public ICollection<Biglietto> Biglietti { get; set; } = new List<Biglietto>();
}
```

### 1.5 Nuova Entita: Biglietto

```csharp
[Table("biglietti")]
public class Biglietto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int AcquistoId { get; set; }

    [ForeignKey(nameof(AcquistoId))]
    public Acquisto Acquisto { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Posto { get; set; } = string.Empty;  // "Fila 7, Posto 7"

    [Required]
    public int SalaNumero { get; set; }

    [Required]
    [MaxLength(20)]
    public string TipologiaSala { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Prezzo { get; set; }

    [Required]
    [MaxLength(20)]
    public string CodiceUnivoco { get; set; } = string.Empty;  // UUID per QR

    [Required]
    [MaxLength(64)]
    public string CodiceHash { get; set; } = string.Empty;  // SHA256 per validazione

    public bool Validato { get; set; } = false;

    public DateTime? DataValidazione { get; set; }

    [Required]
    public int CinemaId { get; set; }  // Per verifica validazione

    [ForeignKey(nameof(CinemaId))]
    public Cinema? Cinema { get; set; }

    [Required]
    [MaxLength(500)]
    public string QRCodeUrl { get; set; } = string.Empty;
}
```

### 1.6 Nuove Entita: CreditoUtente e TransazioneCredito

```csharp
public enum TipoTransazione
{
    RICARICA = 0,
    ACQUISTO = 1,
    RIMBORSO = 2
}

[Table("crediti_utente")]
public class CreditoUtente
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UtenteId { get; set; }

    [ForeignKey(nameof(UtenteId))]
    public Utente Utente { get; set; } = null!;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Saldo { get; set; } = 0;

    public DateTime DataUltimoAggiornamento { get; set; } = DateTime.UtcNow;

    public ICollection<TransazioneCredito> Transazioni { get; set; } = new List<TransazioneCredito>();
}

[Table("transazioni_credito")]
public class TransazioneCredito
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UtenteId { get; set; }

    [ForeignKey(nameof(UtenteId))]
    public Utente Utente { get; set; } = null!;

    [Required]
    public TipoTransazione Tipo { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Importo { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal SaldoPrecedente { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal SaldoSuccessivo { get; set; }

    [Required]
    public DateTime DataTransazione { get; set; } = DateTime.UtcNow;

    public int? OperatoreId { get; set; }  // Chi ha effettuato la ricarica

    [ForeignKey(nameof(OperatoreId))]
    public Utente? Operatore { get; set; }

    public int? CinemaId { get; set; }  // Dove e' stata fatta la ricarica

    [ForeignKey(nameof(CinemaId))]
    public Cinema? Cinema { get; set; }

    [MaxLength(500)]
    public string? Descrizione { get; set; }

    public int? AcquistoId { get; set; }  // Se collegato a un acquisto

    [ForeignKey(nameof(AcquistoId))]
    public Acquisto? Acquisto { get; set; }
}
```

### 1.7 Nuova Entita: PrenotazioneTemporanea (Race Condition)

```csharp
public enum StatoPrenotazioneTemp
{
    ATTIVA = 0,
    CONFERMATA = 1,
    SCADUTA = 2,
    CANCELLATA = 3
}

[Table("prenotazioni_temporanee")]
public class PrenotazioneTemporanea
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(36)]
    public string CodiceTemporaneo { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public int ShowId { get; set; }

    [ForeignKey(nameof(ShowId))]
    public Show Show { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Posto { get; set; } = string.Empty;  // "Fila 7, Posto 7"

    [Required]
    public int UtenteId { get; set; }

    [ForeignKey(nameof(UtenteId))]
    public Utente Utente { get; set; } = null!;

    [Required]
    public DateTime DataCreazione { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime DataScadenza { get; set; }  // DataCreazione + 10 minuti

    [Required]
    public StatoPrenotazioneTemp Stato { get; set; } = StatoPrenotazioneTemp.ATTIVA;

    [Required]
    [MaxLength(50)]
    public string SessionId { get; set; } = string.Empty;
}
```

### 1.8 Aggiornamento Cinema

```csharp
// Nuovi campi da aggiungere a Cinema
[Column(TypeName = "decimal(10,8)")]
public decimal? Latitudine { get; set; }

[Column(TypeName = "decimal(11,8)")]
public decimal? Longitudine { get; set; }

[MaxLength(20)]
public string? CodiceLocale { get; set; }  // Per validazione biglietti

public ICollection<Sala> Sale { get; set; } = new List<Sala>();
```

### 1.9 Aggiornamento Utente

```csharp
// Nuovo campo da aggiungere a Utente
public int? PreferredCinemaId { get; set; }

[ForeignKey(nameof(PreferredCinemaId))]
public Cinema? PreferredCinema { get; set; }

public CreditoUtente? Credito { get; set; }

public ICollection<Acquisto> Acquisti { get; set; } = new List<Acquisto>();
```

---

## Fase 2: Modello Dati - Migration

### Task 2.1: Creare Modelli
- [ ] `Model/Sala.cs` con enum `TipologiaSala`
- [ ] `Model/Show.cs` (rinominare da Proiezione.cs)
- [ ] `Model/Acquisto.cs`
- [ ] `Model/Biglietto.cs`
- [ ] `Model/CreditoUtente.cs`
- [ ] `Model/TransazioneCredito.cs`
- [ ] `Model/PrenotazioneTemporanea.cs`
- [ ] Aggiornare `Model/Film.cs`
- [ ] Aggiornare `Model/Cinema.cs`
- [ ] Aggiornare `Model/Utente.cs`
- [ ] Aggiornare `Model/Prenotazione.cs` (diventa legacy o rimosso)

### Task 2.2: Aggiornare DbContext
```csharp
// Nuovi DbSet
public DbSet<Sala> Sale { get; set; } = null!;
public DbSet<Show> Shows { get; set; } = null!;  // Rinominato da Proiezioni
public DbSet<Acquisto> Acquisti { get; set; } = null!;
public DbSet<Biglietto> Biglietti { get; set; } = null!;
public DbSet<CreditoUtente> CreditiUtente { get; set; } = null!;
public DbSet<TransazioneCredito> TransazioniCredito { get; set; } = null!;
public DbSet<PrenotazioneTemporanea> PrenotazioniTemporanee { get; set; } = null!;

// Configurazione Fluent API
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

modelBuilder.Entity<Biglietto>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => e.CodiceUnivoco).IsUnique();
    entity.HasIndex(e => e.CodiceHash).IsUnique();
});

modelBuilder.Entity<CreditoUtente>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => e.UtenteId).IsUnique();
});

modelBuilder.Entity<PrenotazioneTemporanea>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => new { e.ShowId, e.Posto });
    entity.HasIndex(e => e.DataScadenza);
});
```

### Task 2.3: Migration
```bash
dotnet ef migrations add AddMultiSalaAndTickets
dotnet ef database update
```

### Task 2.4: Seed Dati Iniziali
- [ ] Prezzi per tipologia sala
- [ ] Cinema con coordinate (esempi)
- [ ] Sale di esempio per ogni cinema
- [ ] Configurazione piantine

---

## Fase 3: Backend - Servizi

### 3.1 SalaService

```csharp
public interface ISalaService
{
    Task<IEnumerable<SalaDTO>> GetSaleByCinemaAsync(int cinemaId);
    Task<SalaDTO?> GetSalaAsync(int id);
    Task<SalaDTO> CreateSalaAsync(SalaCreateDTO dto);
    Task<SalaDTO?> UpdateSalaAsync(int id, SalaUpdateDTO dto);
    Task<bool> DeleteSalaAsync(int id);
    Task<PiantinaDTO> GetPiantinaAsync(int salaId);
    Task<bool> UpdatePiantinaAsync(int salaId, PiantinaUpdateDTO dto);
    Task<bool> ValidateConfigurazioneAsync(string configurazioneJson);
}

public class SalaService : ISalaService
{
    // CRUD sale
    // Validazione: non eliminare sale con show futuri
    // Calcolo posti totali da configurazione
    // Generazione preview piantina
}
```

### 3.2 ShowService

```csharp
public interface IShowService
{
    Task<IEnumerable<ShowDTO>> GetShowsAsync(ShowFilterDTO? filter = null);
    Task<ShowDTO?> GetShowAsync(int id);
    Task<ShowDTO> CreateShowAsync(ShowCreateDTO dto);
    Task<ShowDTO?> UpdateShowAsync(int id, ShowUpdateDTO dto);
    Task<bool> DeleteShowAsync(int id);
    Task<bool> ValidateOrarioAsync(int salaId, DateOnly data, TimeOnly oraInizio, int durataFilm, int? excludeShowId = null);
    Task<IEnumerable<ShowDTO>> GetShowsByFilmAsync(int filmId, int? cinemaId = null, DateOnly? data = null);
    Task<IEnumerable<ShowDTO>> GetShowsByCinemaAsync(int cinemaId, DateOnly data);
    Task<int> GetPostiDisponibiliAsync(int showId);
    Task<DisponibilitaPostiDTO> GetDisponibilitaPostiAsync(int showId);
}
```

**Validazione Orari:**
```csharp
public async Task<bool> ValidateOrarioAsync(int salaId, DateOnly data, TimeOnly oraInizio, int durataFilm, int? excludeShowId = null)
{
    var oraFine = oraInizio.AddMinutes(durataFilm);
    
    // Verifica show precedente
    var showPrecedente = await context.Shows
        .Where(s => s.SalaId == salaId && s.Data == data && s.OraInizio < oraInizio)
        .Where(s => excludeShowId == null || s.Id != excludeShowId)
        .OrderByDescending(s => s.OraInizio)
        .FirstOrDefaultAsync();
    
    if (showPrecedente != null && oraInizio < showPrecedente.OraFine)
        return false;
    
    // Verifica show successivo
    var showSuccessivo = await context.Shows
        .Where(s => s.SalaId == salaId && s.Data == data && s.OraInizio > oraInizio)
        .Where(s => excludeShowId == null || s.Id != excludeShowId)
        .OrderBy(s => s.OraInizio)
        .FirstOrDefaultAsync();
    
    if (showSuccessivo != null && oraFine > showSuccessivo.OraInizio)
        return false;
    
    return true;
}
```

### 3.3 BigliettoService

```csharp
public interface IBigliettoService
{
    Task<IEnumerable<PostoStatoDTO>> GetPiantinaStatoAsync(int showId);
    Task<PrenotazioneTempDTO> LockPostiAsync(int showId, List<PostoDTO> posti, string sessionId);
    Task<bool> RinnovaLockAsync(string codiceTemporaneo);
    Task<bool> RilasciaLockAsync(string codiceTemporaneo);
    Task<AcquistoResultDTO> ConfermaAcquistoAsync(ConfermaAcquistoDTO dto);
    Task<BigliettoDTO?> GetBigliettoAsync(int id);
    Task<IEnumerable<BigliettoDTO>> GetBigliettiUtenteAsync(int utenteId);
    Task<BigliettoValidazioneDTO?> GetBigliettoPerValidazioneAsync(string codiceHash);
    Task<bool> ValidabigliettoAsync(string codiceHash, int operatoreId, int cinemaId);
    string GeneraCodiceHash(int bigliettoId, int acquistoId, string posto);
    string GeneraQRCodeUrl(string codiceHash);
}
```

### 3.4 PagamentoService

```csharp
public interface IPagamentoService
{
    Task<CalcoloImportoDTO> CalcolaImportoAsync(CalcoloImportoRequestDTO dto);
    Task<PagamentoResultDTO> ProcessaPagamentoAsync(PagamentoRequestDTO dto);
    Task<RimborsbResultDTO> RimborsaAcquistoAsync(int acquistoId);
    Task<StripePaymentIntentDTO> CreaPaymentIntentAsync(decimal importo);
}
```

### 3.5 CreditoService

```csharp
public interface ICreditoService
{
    Task<decimal> GetSaldoAsync(int utenteId);
    Task<TransazioneCreditoDTO> RicaricaAsync(RicaricaCreditoDTO dto);
    Task<IEnumerable<TransazioneCreditoDTO>> GetStoricoAsync(int utenteId);
    Task<IEnumerable<TransazioneCreditoDTO>> GetAllTransazioniAsync(TransazioneFilterDTO? filter = null);
    Task<bool> ScalaCreditoAsync(int utenteId, decimal importo, int acquistoId);
}
```

---

## Fase 4: Backend - Endpoint

### 4.1 SaleEndpoints (Admin)

```
GET    /admin/cinemas/{cinemaId}/sale        - Lista sale cinema
GET    /admin/sale/{id}                      - Dettaglio sala
POST   /admin/cinemas/{cinemaId}/sale       - Crea sala
PUT    /admin/sale/{id}                      - Modifica sala
DELETE /admin/sale/{id}                      - Elimina sala
GET    /admin/sale/{id}/piantina             - Configurazione piantina
PUT    /admin/sale/{id}/piantina              - Aggiorna piantina
```

### 4.2 ShowsEndpoints (Admin/PowerUser)

```
GET    /admin/shows                          - Lista show (con filtri)
GET    /admin/shows/{id}                     - Dettaglio show
POST   /admin/shows                          - Crea show
PUT    /admin/shows/{id}                     - Modifica show
DELETE /admin/shows/{id}                     - Elimina show
POST   /admin/shows/bulk                     - Crea show multipli
GET    /admin/shows/{id}/disponibilita       - Posti disponibili
```

### 4.3 ProgrammazioneEndpoints (Pubblico)

```
GET    /programmazione                       - Film in programmazione (con filtri)
GET    /programmazione/featured              - Film in evidenza
GET    /programmazione/coming-soon           - Film in uscita (entro 2 settimane)
GET    /film/{id}/shows                      - Show per film (con filtri data/cinema)
GET    /cinema/{id}/programmazione           - Programmazione cinema per data
GET    /cinemas/nearby                       - Cinema vicini (geolocalizzazione)
```

### 4.4 AcquistoEndpoints (Autenticato)

```
GET    /acquisto/{showId}/piantina           - Piantina con stato posti
POST   /acquisto/lock-posti                  - Lock temporaneo posti
POST   /acquisto/rinnova-lock                - Rinnova lock
DELETE /acquisto/lock/{codice}              - Rilascia lock
POST   /acquisto/calcola-importo             - Calcola importo totale
POST   /acquisto/conferma                    - Conferma acquisto
POST   /acquisto/pagamento                   - Processa pagamento Stripe
GET    /acquisto/{id}/biglietti              - Biglietti dell'acquisto
GET    /acquisto/{id}/pdf                    - Genera PDF biglietti
```

### 4.5 UserEndpoints (aggiornamento)

```
GET    /user/credito                         - Saldo credito
GET    /user/credito/storico                 - Storico transazioni
GET    /user/acquisti                        - Storico acquisti
GET    /user/biglietti                       - Biglietti utente
PUT    /user/cinema-preferito                - Imposta cinema preferito
GET    /user/cinema-preferito                - Get cinema preferito
```

### 4.6 CreditoEndpoints (PowerUser/Admin)

```
POST   /admin/credito/ricarica               - Ricarica credito utente
GET    /admin/credito/storico/{utenteId}     - Storico ricariche utente
GET    /admin/credito/transazioni            - Tutte transazioni (con filtri)
GET    /admin/credito/ricerca-utente         - Cerca utente per email/id
```

### 4.7 ValidazioneEndpoints (PowerUser/Admin)

```
GET    /validazione                          - Pagina validazione
POST   /validazione/verifica                 - Verifica biglietto (codice)
GET    /validazione/qr/{codiceHash}          - Validazione via QR
POST   /validazione/conferma                 - Conferma validazione
GET    /validazione/{codice}/info            - Info biglietto per validazione
```

---

## Fase 5: Frontend - Pagine Pubbliche

### 5.1 Rifacimento programmazione.html

**Layout ispirato a UCI Cinemas:**

```
+-------------------------------------------------------------+
| [LOGO]                    Film  My Cinemas                   |
+-------------------------------------------------------------+
| Modal Selezione Cinema (overlay)                             |
| +---------------------------------------------------------+ |
| | Seleziona il tuo cinema                                 | |
| | [Ricerca...]                                            | |
| |                                                         | |
| | UCI Cinemas Lissone     Monza Brianza    km 5.2        | |
| |    XL, IMAX, 3D, 2D                   [Seleziona]      | |
| |                                                         | |
| | UCI Cinemas Milano      Milano           km 12.4       | |
| |    ISENSE, 3D, 2D                     [Seleziona]       | |
| +---------------------------------------------------------+ |
+-------------------------------------------------------------+
| Tags: [In Evidenza] [In Uscita] [Tutti i Film]              |
+-------------------------------------------------------------+
| Filtro categoria: [ dropdown ]                               |
| Ricerca: [________________]                                  |
+-------------------------------------------------------------+
| Cinema selezionato: UCI Lissone [Cambia]                    |
+-------------------------------------------------------------+
| Film Card Grid                                               |
| +----------+ +----------+ +----------+                       |
| | [IMG]    | | [IMG]    | | [IMG]    |                       |
| | Titolo   | | Titolo   | | Titolo   |                       |
| | 2h 28m   | | 2h 10m   | | 1h 58m   |                       |
| | 2024     | | 2024     | | 2024     |                       |
| | [checkmark] Nel tuo| | [x] Non    | | [checkmark] Nel tuo|   |
| |   cinema | |   cinema | |   cinema |                       |
| +----------+ +----------+ +----------+                       |
|                                                              |
| ... piu' card ...                                             |
+-------------------------------------------------------------+
```

**Funzionalita:**
1. **Modal selezione cinema**:
   - Geolocalizzazione automatica (browser API)
   - Ordinamento per distanza
   - Salvataggio in localStorage (non loggato) o backend (loggato)
   - Sync cinema preferito frontend <-> backend

2. **Tags/Tab**:
   - **In evidenza**: Film con `featured=true` o piu' show nei prossimi 7 giorni
   - **In uscita**: Film con `DataRilascio` entro 14 giorni da oggi
   - **Tutti**: Tutti i film con almeno uno show programmato

3. **Filtri**:
   - Categoria (dropdown)
   - Ricerca per titolo (input testuale)

4. **Card Film**:
   - Una card per film (non per proiezione)
   - Indicatore [checkmark]/[x] se presente nel cinema selezionato
   - Click -> va a `scheda-film.html?id={id}`

5. **Salvataggio Cinema**:
   - Non loggato: localStorage key `selectedCinema`
   - Loggato: PUT `/user/cinema-preferito`
   - Lettura: GET `/user/cinema-preferito` (loggato) o localStorage (non loggato)

### 5.2 Nuova scheda-film.html

**Layout:**

```
+-------------------------------------------------------------+
|                        [Copertina grande]                    |
| Titolo Film                                                  |
| Durata: 2h 28m | Genere: Fantasy | Data rilascio: 15/03/2024|
+-------------------------------------------------------------+
| Descrizione                                                 |
| Lorem ipsum dolor sit amet... (2000 char)                    |
+-------------------------------------------------------------+
| Regista: Christopher Nolan                                   |
| Cast: Cillian Murphy, Emily Blunt, Matt Damon, Robert...    |
+-------------------------------------------------------------+
| [Vai agli show v]                                            |
+-------------------------------------------------------------+
| Seleziona data:                                              |
| <- [Oggi] [Lun 14] [Mar 15] [Mer 16] [Gio 17] ... ->        |
+-------------------------------------------------------------+
| UCI Cinemas Lissone                                          |
| Monza Brianza - Via Lombardia 12                            |
|                                                              |
| ISENSE                                                       |
| [16:00] [18:30] [21:00]                                      |
|                                                              |
| XL                                                           |
| [16:30] [19:00] [21:30]                                     |
|                                                              |
| 3D                                                           |
| [17:15]                                                      |
|                                                              |
| 2D                                                           |
| [16:15] [17:00] [17:30] [18:00]                             |
+-------------------------------------------------------------+
```

**Funzionalita:**
1. **Carousel date**:
   - Orizzontale, scrollabile
   - Da "Oggi" a +14 giorni
   - Freccia sinistra/destra per navigare
   - Click su data -> carica show per quella data

2. **Sezione show per tipologia sala**:
   - Raggruppati per tipologia (ISENSE, XL, 3D, 2D)
   - Bottoni orario (style: pill/small button)
   - Click orario -> `acquista.html?...` (se loggato) o login con redirect

3. **Gestione cinema**:
   - Mostra cinema selezionato
   - Se film non nel cinema -> messaggio "Non disponibile nel tuo cinema"
   - Link per cambiare cinema

4. **URL Parameters**:
   - `acquista.html?IdCinema={id}&IdFilm={id}&IdSala={id}&IdShow={id}`

### 5.3 Nuova my-cinemas.html

**Layout:**

```
+-------------------------------------------------------------+
| I Nostri Cinema                                              |
+-------------------------------------------------------------+
| Cinema Card Grid                                             |
| +---------------------------------------------------------+ |
| | UCI Cinemas Lissone                                     | |
| | Monza Brianza                                          | |
| | Via Lombardia 12                                        | |
| | Sale: XL, IMAX, 3D, 2D                                 | |
| |                                      [Vedi Programmazione]| |
| +---------------------------------------------------------+ |
| ... altre card ...                                          |
+-------------------------------------------------------------+
| Se cinema selezionato (my-cinemas.html?IdCinema=1):          |
|                                                              |
| Seleziona data:                                              |
| <- [Oggi] [Lun 14] [Mar 15] [Mer 16] ... ->                 |
+-------------------------------------------------------------+
| Film 1                                                       |
| [IMG]  Titolo - Descrizione breve...                        |
|        ISENSE: [16:00] [18:30]                              |
|        3D: [17:15] [20:00]                                  |
|        2D: [15:00] [18:00] [21:00]                          |
|                                                              |
| Film 2                                                       |
| [IMG]  Titolo - Descrizione breve...                        |
|        XL: [14:30] [17:30] [20:30]                         |
|        2D: [16:00] [19:00] [22:00]                         |
+-------------------------------------------------------------+
```

**Funzionalita:**
1. **Lista cinema**:
   - Card con info cinema
   - Tipologie sale presenti
   - Click -> programma di quel cinema

2. **Programmazione cinema**:
   - Carousel date
   - Film con orari per tipologia sala
   - Click orario -> acquista o login

---

## Fase 6: Frontend - Pagine Acquisto

### 6.1 acquista.html

**Layout:**

```
+-------------------------------------------------------------+
| Riepilogo                                                    |
| +---------------------------------------------------------+ |
| | Film: Oppenheimer                                       | |
| | Tipologia: ISENSE                                       | |
| | Data: 14 Aprile 2024                                   | |
| | Ora: 18:30                                              | |
| | Cinema: UCI Cinemas Lissone                             | |
| | Biglietti: 0 | Totale: 0,00 EUR                          | |
| +---------------------------------------------------------+ |
+-------------------------------------------------------------+
| SCHERMO                                                      |
| +---------------------------------------------------------+ |
| | Seleziona i posti (max 10)                              | |
| |                                                         | |
| |     [1] [2] [3] [4] [5] [6] [7] [8] [9] [10] [11] [12] | |
| |   +---------------------------------------------------+ | |
| |   |F 1 [GREEN][GREEN][GREEN][RED][RED][GREEN][GREEN]   | | |
| |   |i 2 [GREEN][GREEN][RED][RED][RED][GREEN][GREEN]     | | |
| |   |l 3 [GREEN][GREEN][GREEN][GREEN][GREEN][GREEN]       | | |
| |   |a 4 [GREEN][RED][GREEN][GREEN][GREEN][GREEN]         | | |
| |   | 5 [GREEN][GREEN][GREEN][RED][RED][GREEN][GREEN]     | | |
| |   | 6 [GREEN][GREEN][GREEN][GREEN][GREEN][GREEN]       | | |
| |   | 7 [YELLOW][YELLOW][YELLOW] <- SELEZIONATI           | | |
| |   +---------------------------------------------------+ | |
| |                                                         | |
| | GREEN Disponibile  RED Occupato  YELLOW Selezionato    | |
| +---------------------------------------------------------+ |
+-------------------------------------------------------------+
| Posti selezionati:                                           |
| Fila 7, Posto 1 | Fila 7, Posto 2 | Fila 7, Posto 3          |
|                                                              |
| Timer: 09:45 rimanenti                                       |
|                              [Continua ->]                    |
+-------------------------------------------------------------+
```

**Funzionalita:**
1. **Piantina interattiva**:
   - Generata da API (`GET /acquisto/{showId}/piantina`)
   - Colori: verde (disponibile), rosso (occupato), giallo (selezionato)
   - Click posto -> lock temporaneo

2. **Lock posti**:
   - POST `/acquisto/lock-posti` con lista posti
   - Timer countdown 10 minuti
   - Aggiornamento real-time stato posti

3. **Max 10 posti** per acquisto

4. **Session ID**:
   - Generato al caricamento pagina
   - Usato per associare lock a sessione

5. **Rinnovo lock**:
   - Auto-rinnovo se utente attivo
   - POST `/acquisto/rinnova-lock`

### 6.2 pagamento.html

**Layout:**

```
+-------------------------------------------------------------+
| Riepilogo Acquisto                                           |
| +---------------------------------------------------------+ |
| | Film: Oppenheimer                                       | |
| | Cinema: UCI Lissone - ISENSE                            | |
| | Data: 14 Aprile 2024, 18:30                             | |
| | Posti: Fila 7 (Posti 1, 2, 3)                           | |
| |                                                         | |
| | Subtotale: 3 x 12,00 EUR = 36,00 EUR                     | |
| +---------------------------------------------------------+ |
+-------------------------------------------------------------+
| Credito Disponibile: 25,00 EUR                               |
|                                                              |
| [x] Usa il mio credito (25,00 EUR)                           |
|                                                              |
| Rimanente da pagare: 11,00 EUR                               |
+-------------------------------------------------------------+
| Pagamento con Carta (Stripe)                                 |
| +---------------------------------------------------------+ |
| | [card element Stripe]                                   | |
| |                                                         | |
| | [Paga 11,00 EUR]                                         | |
| +---------------------------------------------------------+ |
|                                                              |
| [Paga tutto con carta (36,00 EUR)]                          |
+-------------------------------------------------------------+
```

**Funzionalita:**
1. **Calcolo importo**:
   - GET credito utente
   - Checkbox "Usa credito"
   - Calcolo automatico rimanente

2. **Stripe Elements**:
   - Integrazione frontend Stripe.js
   - Card input sicuro
   - 3D Secure support

3. **Opzioni pagamento**:
   - Mix credito + carta
   - Solo carta (ignora credito)
   - Solo credito (se sufficiente)

4. **Conferma**:
   - POST `/acquisto/pagamento`
   - Gestione successo/errore
   - Redirect a pagina conferma

---

## Fase 7: Frontend - Pagine Admin

### 7.1 sale.html (gestione sale)

**Layout:**
```
+-------------------------------------------------------------+
| Gestione Sale                                                |
+-------------------------------------------------------------+
| Seleziona Cinema: [Dropdown]                                |
+-------------------------------------------------------------+
| [Aggiungi Sala]                                              |
+-------------------------------------------------------------+
| Sala # | Nome        | Tipologia | File | Posti | Azioni    |
|-------|-------------|-----------|------|-------|----------|
| 1     | SALA IMAX   | ISENSE    | 12   | 180   | [E][D]   |
| 2     | SALA 2      | XL        | 10   | 150   | [E][D]   |
| 3     | SALA 3      | 3D        | 8    | 120   | [E][D]   |
| 4     | SALA 4      | 2D        | 15   | 225   | [E][D]   |
+-------------------------------------------------------------+
```

**Modal Creazione/Modifica Sala:**
```
+-------------------------------------------------------------+
| Crea/Modifica Sala                                           |
+-------------------------------------------------------------+
| Cinema: [Dropdown - readonly se modifica]                   |
| Numero Sala: [__]                                           |
| Nome: [__________________]                                   |
| Tipologia: [ISENSE v]                                        |
|                                                              |
| Configurazione Piantina:                                     |
| Numero File: [__]                                            |
| [Configurazione manuale / Automatica]                        |
|                                                              |
| Se automatica:                                                |
| Posti per fila: [__]                                         |
|                                                              |
| Se manuale:                                                   |
| Fila 1: [__] posti                                          |
| Fila 2: [__] posti                                          |
| ...                                                          |
|                                                              |
| Preview Piantina:                                            |
| [Visualizzazione grafica]                                    |
|                                                              |
| [Salva] [Annulla]                                            |
+-------------------------------------------------------------+
```

### 7.2 shows.html (ex proiezioni.html aggiornato)

**Layout:**
```
+-------------------------------------------------------------+
| Gestiona Show                                                |
+-------------------------------------------------------------+
| Filtri:                                                      |
| Cinema: [Dropdown]  Sala: [Dropdown]  Data: [Date]          |
| Film: [Dropdown]    Stato: [Dropdown]                       |
+-------------------------------------------------------------+
| [Nuovo Show] [Creazione Multipla]                            |
+-------------------------------------------------------------+
| ID | Film        | Sala       | Data      | Ora  | Stato | Azioni|
|----|-------------|------------|-----------|------|-------|-------|
| 1  | Oppenheimer | SALA 1     | 14/04/24  | 18:30| PROG  | [E][D]|
| 2  | Dune 2      | SALA 2     | 14/04/24  | 20:00| PROG  | [E][D]|
+-------------------------------------------------------------+
```

**Modal Creazione Show Singolo:**
```
+-------------------------------------------------------------+
| Nuovo Show                                                   |
+-------------------------------------------------------------+
| Cinema: [Dropdown] -> popola Sale                           |
| Sala: [Dropdown]                                             |
| Film: [Dropdown] -> popola Durata, Prezzo                    |
| Data: [DatePicker]                                          |
| Ora Inizio: [TimePicker]                                     |
| Prezzo: [__.__] EUR (auto-calcolato per tipologia)          |
|                                                              |
| Validazione:                                                 |
| [x] Nessuna sovrapposizione                                  |
| [x] Durata: 180 min - Fine: 21:30                            |
|                                                              |
| [Salva] [Annulla]                                            |
+-------------------------------------------------------------+
```

**Modal Creazione Multipla:**
```
+-------------------------------------------------------------+
| Creazione Multipla Show                                      |
+-------------------------------------------------------------+
| Cinema: [Dropdown]                                           |
| Film: [Dropdown]                                             |
|                                                              |
| Periodo: [Dal] [Al]                                          |
|                                                              |
| Giorni: [x] Lun [x] Mar [x] Mer [x] Gio [x] Ven [x] Sab [x] Dom|
|                                                              |
| Seleziona Sale e Orari:                                      |
| +---------------------------------------------------------+ |
| | SALA 1 (ISENSE) - Prezzo: 12,00 EUR                    | |
| | [x] Mattina [__:__] [__:__]                            | |
| | [x] Pomeriggio [__:__] [__:__]                        | |
| | [x] Sera [__:__] [__:__]                              | |
| +---------------------------------------------------------+ |
| | SALA 2 (3D) - Prezzo: 10,00 EUR                         | |
| | [ ] Mattina [__:__] [__:__]                            | |
| | [x] Pomeriggio [__:__] [__:__]                        | |
| | [x] Sera [__:__] [__:__]                              | |
| +---------------------------------------------------------+ |
|                                                              |
| [Anteprama] [Crea Show]                                      |
+-------------------------------------------------------------+
```

### 7.3 validazione.html (PowerUser/Admin)

**Layout:**
```
+-------------------------------------------------------------+
| Validazione Biglietti - UCI Lissone                         |
+-------------------------------------------------------------+
| Cinema: [Dropdown - default cinema operatore]               |
+-------------------------------------------------------------+
| [Scannerizza QR Code] o inserisci codice:                   |
| [________________________] [Verifica]                        |
+-------------------------------------------------------------+
| Risultato:                                                   |
| +---------------------------------------------------------+ |
| | [CHECK] BIGLIETTO VALIDO                               | |
| |                                                         | |
| | Film: Oppenheimer                                       | |
| | Sala: 10 (ISENSE)                                       | |
| | Data: 14/04/2024 - 18:30                               | |
| | Posto: Fila 7, Posto 7                                 | |
| | Codice: ABC123DEF456                                    | |
| |                                                         | |
| | [ VALIDA BIGLIETTO ]                                    | |
| +---------------------------------------------------------+ |
+-------------------------------------------------------------+
| Storico Validazioni Oggi: 12                                |
| Ultimi: Fila 7 p7, Fila 5 p12, Fila 3 p8...                |
+-------------------------------------------------------------+
```

**QR Scanner:**
- Usa libreria js QR (es. html5-qrcode)
- Smartphone/tablet camera
- URL QR: `https://filmapi.com/validazione/qr/{codiceHash}`
- Se URL aperto da PowerUser/Admin loggato -> auto-valida

### 7.4 ricarica-credito.html (PowerUser/Admin)

**Layout:**
```
+-------------------------------------------------------------+
| Ricarica Credito Utente                                      |
+-------------------------------------------------------------+
| Email/Codice utente: [____________________] [Cerca]         |
+-------------------------------------------------------------+
| Utente trovato:                                              |
| Nome: Mario Rossi                                            |
| Email: mario.rossi@email.com                                |
| Saldo attuale: 15,00 EUR                                     |
+-------------------------------------------------------------+
| Importo ricarica: EUR [____]                                 |
| Note: [__________________________]                           |
|                                                              |
| [ Conferma Ricarica ]                                        |
+-------------------------------------------------------------+
| Transazione registrata:                                      |
| Operatore: Luca Verdi (PowerUser)                           |
| Data: 14/04/2024 15:30                                      |
| Importo: +20,00 EUR                                          |
| Nuovo saldo: 35,00 EUR                                       |
+-------------------------------------------------------------+
```

---

## Fase 8: Integrazione Stripe

### 8.1 Setup

**Installare pacchetto:**
```bash
dotnet add package Stripe.net
```

**Configurazione .env:**
```
STRIPE_SECRET_KEY=sk_test_...
STRIPE_PUBLISHABLE_KEY=pk_test_...
STRIPE_WEBHOOK_SECRET=whsec_...
```

**Configurazione Program.cs:**
```csharp
builder.Services.AddScoped<IPagamentoService, PagamentoService>();

StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];
```

### 8.2 Flusso Pagamento

1. **Frontend**:
   - Carica Stripe.js
   - Crea card element
   - Ottiene Payment Intent dal backend

2. **Backend** (Crea Payment Intent):
   ```csharp
   public async Task<StripePaymentIntentDTO> CreaPaymentIntentAsync(decimal importo)
   {
       var options = new PaymentIntentCreateOptions
       {
           Amount = (long)(importo * 100),  // Centesimi
           Currency = "eur",
           Metadata = new Dictionary<string, string>
           {
               { "integration", "filmapi" }
           }
       };
       
       var service = new PaymentIntentService();
       var intent = await service.CreateAsync(options);
       
       return new StripePaymentIntentDTO
       {
           ClientSecret = intent.ClientSecret,
           PaymentIntentId = intent.Id
       };
   }
   ```

3. **Frontend** (Conferma):
   ```javascript
   const { paymentIntent, error } = await stripe.confirmCardPayment(clientSecret, {
       payment_method: {
           card: cardElement
       }
   });
   
   if (error) {
       // Gestione errore
   } else if (paymentIntent.status === 'succeeded') {
       // Chiama backend per conferma
       await ApiClient.post('/acquisto/conferma', {
           paymentIntentId: paymentIntent.id,
           ...
       });
   }
   ```

4. **Backend** (Conferma):
   - Verifica payment intent
   - Scala credito se usato
   - Genera biglietti
   - Invia email

---

## Fase 9: Race Condition - Strategia Dettagliata

### 9.1 Algoritmo Lock Posti

**Schema Database:**
```sql
-- Indici per performance
CREATE INDEX IX_PrenotazioniTemporanee_Show_Posto ON PrenotazioniTemporanee(ShowId, Posto);
CREATE INDEX IX_PrenotazioniTemporanee_Scadenza ON PrenotazioniTemporanee(DataScadenza);
CREATE INDEX IX_PrenotazioniTemporanee_Sessione ON PrenotazioniTemporanee(SessionId);
```

**Lock Posto:**
```csharp
public async Task<PrenotazioneTempDTO> LockPostiAsync(int showId, List<PostoDTO> posti, string sessionId)
{
    var scadenza = DateTime.UtcNow.AddMinutes(10);
    var codiceTemporaneo = Guid.NewGuid().ToString();
    
    using var transaction = await context.Database.BeginTransactionAsync();
    
    try
    {
        foreach (var posto in posti)
        {
            // Verifica posto non gia' occupato o prenotato
            var occupato = await context.Biglietti
                .AnyAsync(b => b.ShowId == showId && b.Posto == posto.ToString());
            
            if (occupato)
                throw new PostoOccupatoException(posto);
            
            var prenotato = await context.PrenotazioniTemporanee
                .AnyAsync(p => p.ShowId == showId && 
                              p.Posto == posto.ToString() && 
                              p.Stato == StatoPrenotazioneTemp.ATTIVA && 
                              p.DataScadenza > DateTime.UtcNow);
            
            if (prenotato)
                throw new PostoOccupatoException(posto);
            
            // Crea prenotazione temporanea
            context.PrenotazioniTemporanee.Add(new PrenotazioneTemporanea
            {
                CodiceTemporaneo = codiceTemporaneo,
                ShowId = showId,
                Posto = posto.ToString(),
                UtenteId = utenteId,
                DataCreazione = DateTime.UtcNow,
                DataScadenza = scadenza,
                Stato = StatoPrenotazioneTemp.ATTIVA,
                SessionId = sessionId
            });
        }
        
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        
        return new PrenotazioneTempDTO
        {
            CodiceTemporaneo = codiceTemporaneo,
            Posti = posti,
            DataScadenza = scadenza
        };
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### 9.2 Background Job - Cleanup Lock Scaduti

```csharp
public class PrenotazioneTempCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var scaduti = await context.PrenotazioniTemporanee
                .Where(p => p.Stato == StatoPrenotazioneTemp.ATTIVA && 
                           p.DataScadenza < DateTime.UtcNow)
                .ToListAsync();
            
            foreach (var p in scaduti)
            {
                p.Stato = StatoPrenotazioneTemp.SCADUTA;
            }
            
            await context.SaveChangesAsync();
            
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
```

### 9.3 Frontend Timer

```javascript
class LockTimer {
    constructor(scadenza, onExpire, onUpdate) {
        this.scadenza = new Date(scadenza);
        this.onExpire = onExpire;
        this.onUpdate = onUpdate;
        this.intervalId = null;
    }
    
    start() {
        this.intervalId = setInterval(() => {
            const remaining = this.scadenza - new Date();
            
            if (remaining <= 0) {
                this.stop();
                this.onExpire();
            } else {
                const minutes = Math.floor(remaining / 60000);
                const seconds = Math.floor((remaining % 60000) / 1000);
                this.onUpdate(`${minutes}:${seconds.toString().padStart(2, '0')}`);
            }
        }, 1000);
    }
    
    stop() {
        if (this.intervalId) {
            clearInterval(this.intervalId);
            this.intervalId = null;
        }
    }
    
    async rinnova() {
        const response = await ApiClient.post('/acquisto/rinnova-lock', {
            codiceTemporaneo: this.codiceTemporaneo
        });
        
        if (response.success) {
            this.scadenza = new Date(response.nuovaScadenza);
        }
    }
}
```

---

## Fase 10: Email e PDF

### 10.1 Configurazione Email

**Installare pacchetto:**
```bash
dotnet add package MailKit
```

**Configurazione .env:**
```
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=your-email@gmail.com
SMTP_PASSWORD=your-app-password
SMTP_FROM=noreply@filmapi.com
```

**Servizio Email:**
```csharp
public interface IEmailService
{
    Task InviaConfermaAcquistoAsync(Acquisto acquisto, List<Biglietto> biglietti);
    Task InviaPDFAsync(string toEmail, byte[] pdf, string filename);
}

public class EmailService : IEmailService
{
    public async Task InviaConfermaAcquistoAsync(Acquisto acquisto, List<Biglietto> biglietti)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("FilmAPI", _config["SMTP:From"]));
        message.To.Add(new MailboxAddress(acquisto.Utente.Email, acquisto.Utente.Email));
        message.Subject = $"Conferma Acquisto - {acquisto.Show.Film.Titolo}";
        
        var builder = new BodyBuilder();
        builder.HtmlBody = GeneraEmailHtml(acquisto, biglietti);
        
        // Genera PDF e allega
        var pdf = GeneraPDF(biglietti, acquisto);
        builder.Attachments.Add("biglietti.pdf", pdf);
        
        message.Body = builder.ToMessageBody();
        
        using var client = new SmtpClient();
        await client.ConnectAsync(_config["SMTP:Host"], int.Parse(_config["SMTP:Port"]), SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_config["SMTP:User"], _config["SMTP:Password"]);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
```

### 10.2 Generazione PDF

**Installare pacchetto:**
```bash
dotnet add package QuestPDF
```

**Template PDF:**
```csharp
public byte[] GeneraPDF(List<Biglietto> biglietti, Acquisto acquisto)
{
    var document = Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(20);
            
            page.Content().Column(column =>
            {
                foreach (var biglietto in biglietti)
                {
                    column.Item().Element(e => BigliettoPage(e, biglietto, acquisto));
                    column.Item().PageBreak();
                }
            });
        });
    });
    
    return document.GeneratePdf();
}

private void BigliettoPage(IContainer container, Biglietto biglietto, Acquisto acquisto)
{
    container.Column(column =>
    {
        // Titolo film
        column.Item().Text(acquisto.Show.Film.Titolo)
            .FontSize(24).Bold();
        
        // Data e ora
        column.Item().Text($"Data: {acquisto.Show.Data:dd/MM/yyyy} - {acquisto.Show.OraInizio:HH:mm}")
            .FontSize(14);
        
        // Sala e posto
        column.Item().Text($"Sala: {biglietto.SalaNumero}, {biglietto.TipologiaSala}")
            .FontSize(12);
        column.Item().Text($"Posto: {biglietto.Posto}")
            .FontSize(12);
        
        // Cinema
        column.Item().Text($"Cinema: {biglietto.Cinema.Nome}")
            .FontSize(12);
        column.Item().Text(biglietto.Cinema.Indirizzo)
            .FontSize(10);
        
        // Prezzo
        column.Item().Text($"Prezzo: {biglietto.Prezzo:F2} EUR")
            .FontSize(14).Bold();
        
        // QR Code
        column.Item().Image(GeneraQRCode(biglietto.CodiceHash))
            .Width(150).Height(150);
        
        // Codice univoco
        column.Item().Text($"Codice: {biglietto.CodiceUnivoco}")
            .FontSize(10);
        
        // Codice a barre (opzionale)
        column.Item().Barcode(BarcodeType.Code128, biglietto.CodiceUnivoco)
            .Width(200).Height(50);
    });
}
```

---

## Fase 11: Geolocalizzazione

### 11.1 Frontend - API Geolocation

```javascript
const GeoLocation = {
    async getPosition() {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject(new Error('Geolocation non supportata'));
                return;
            }
            
            navigator.geolocation.getCurrentPosition(
                position => resolve({
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude
                }),
                error => reject(error),
                {
                    enableHighAccuracy: true,
                    timeout: 10000,
                    maximumAge: 300000  // 5 minuti cache
                }
            );
        });
    },
    
    calculateDistance(lat1, lon1, lat2, lon2) {
        // Formula di Haversine
        const R = 6371;  // Raggio Terra in km
        const dLat = this.toRad(lat2 - lat1);
        const dLon = this.toRad(lon2 - lon1);
        const a = Math.sin(dLat/2) * Math.sin(dLat/2) +
                  Math.cos(this.toRad(lat1)) * Math.cos(this.toRad(lat2)) *
                  Math.sin(dLon/2) * Math.sin(dLon/2);
        const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
        return R * c;
    },
    
    toRad(deg) {
        return deg * (Math.PI / 180);
    },
    
    async getNearbyCinemas(userLat, userLon, cinemaList) {
        return cinemaList
            .map(cinema => ({
                ...cinema,
                distance: this.calculateDistance(
                    userLat, userLon, 
                    cinema.latitudine, cinema.longitudine
                )
            }))
            .sort((a, b) => a.distance - b.distance);
    }
};
```

### 11.2 Backend - Endpoint Cinema Vicini

```csharp
// GET /cinemas/nearby?lat=45.4642&lon=9.1900
app.MapGet("/cinemas/nearby", async (double lat, double lon, FilmDbContext db) =>
{
    var cinemas = await db.Cinemas
        .Where(c => c.Latitudine != null && c.Longitudine != null)
        .ToListAsync();
    
    var result = cinemas.Select(c => new
    {
        c.Id,
        c.Nome,
        c.Citta,
        c.Indirizzo,
        c.Latitudine,
        c.Longitudine,
        Distance = CalculateDistance(lat, lon, c.Latitudine!.Value, c.Longitudine!.Value),
        Sale = c.Sale.Select(s => s.Tipologia.ToString()).Distinct()
    })
    .OrderBy(c => c.Distance)
    .ToList();
    
    return Results.Ok(result);
})
.WithName("GetNearbyCinemas")
.AllowAnonymous();
```

---

## Fase 12: Prezzi per Tipologia Sala

### Tabella Prezzi Base (configurabile via DB)

```csharp
[Table("prezzi_tipologia_sala")]
public class PrezzoTipologiaSala
{
    [Key]
    public TipologiaSala Tipologia { get; set; }
    
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal PrezzoBase { get; set; }
}
```

**Dati Seed:**
| Tipologia | Prezzo Base |
|-----------|-------------|
| ISENSE    | 12,00 EUR   |
| XL        | 11,00 EUR   |
| TRE_D (3D)| 10,00 EUR   |
| DUE_D (2D)| 8,00 EUR    |

---

## Fase 13: Task Sequenziali di Sviluppo

### Sprint 1: Modello Dati (2-3 giorni)
- [ ] Creare `Model/Sala.cs` con enum `TipologiaSala`
- [ ] Creare `Model/Show.cs`
- [ ] Creare `Model/Acquisto.cs`
- [ ] Creare `Model/Biglietto.cs`
- [ ] Creare `Model/CreditoUtente.cs`
- [ ] Creare `Model/TransazioneCredito.cs`
- [ ] Creare `Model/PrenotazioneTemporanea.cs`
- [ ] Aggiornare `Model/Film.cs` (nuovi campi)
- [ ] Aggiornare `Model/Cinema.cs` (coordinate)
- [ ] Aggiornare `Model/Utente.cs` (PreferredCinemaId)
- [ ] Aggiornare `Data/FilmDbContext.cs`
- [ ] Creare migration `AddMultiSalaAndTickets`
- [ ] Seed dati iniziali

### Sprint 2: Backend Servizi (3-4 giorni)
- [ ] Creare `Services/SalaService.cs`
- [ ] Creare `Services/ShowService.cs` (con validazione orari)
- [ ] Creare `Services/BigliettoService.cs` (con lock posti)
- [ ] Creare `Services/CreditoService.cs`
- [ ] Creare `Services/PagamentoService.cs`
- [ ] Creare `Services/EmailService.cs`
- [ ] Creare `Services/PDFService.cs`
- [ ] Registrare servizi in Program.cs

### Sprint 3: Backend Endpoint (2-3 giorni)
- [ ] Creare `Endpoints/SaleEndpoints.cs`
- [ ] Aggiornare `Endpoints/ShowsEndpoints.cs` (ex Proiezioni)
- [ ] Creare `Endpoints/ProgrammazioneEndpoints.cs`
- [ ] Creare `Endpoints/AcquistoEndpoints.cs`
- [ ] Aggiornare `Endpoints/UserEndpoints.cs`
- [ ] Creare `Endpoints/CreditoEndpoints.cs`
- [ ] Creare `Endpoints/ValidazioneEndpoints.cs`
- [ ] Configurare policy autorizzazione

### Sprint 4: Frontend Programmazione (3-4 giorni)
- [ ] Rifare `wwwroot/programmazione.html`
- [ ] Creare modal selezione cinema
- [ ] Implementare geolocalizzazione
- [ ] Salvataggio cinema preferito (localStorage + backend)
- [ ] Tags: In evidenza, In uscita, Tutti
- [ ] Filtri categoria e ricerca
- [ ] Creare `wwwroot/scheda-film.html`
- [ ] Carousel date
- [ ] Sezione show per tipologia sala
- [ ] Creare `wwwroot/my-cinemas.html`
- [ ] Aggiornare `js/api-client.js`
- [ ] Creare `js/geo-location.js`

### Sprint 5: Frontend Acquisto (3-4 giorni)
- [ ] Creare `wwwroot/acquista.html`
- [ ] Piantina interattiva
- [ ] Implementare lock posti
- [ ] Timer countdown
- [ ] Creare `wwwroot/pagamento.html`
- [ ] Integrazione Stripe.js
- [ ] Gestione credito + carta
- [ ] Creare `wwwroot/conferma-acquisto.html`
- [ ] Creare `js/stripe-client.js`
- [ ] Creare `js/acquisto.js`

### Sprint 6: Frontend Admin (2-3 giorni)
- [ ] Creare `wwwroot/sale.html`
- [ ] Configurazione piantina visuale
- [ ] Aggiornare `wwwroot/proiezioni.html` -> shows.html
- [ ] Creazione show singolo e multipla
- [ ] Creare `wwwroot/validazione.html`
- [ ] QR scanner
- [ ] Creare `wwwroot/ricarica-credito.html`
- [ ] Aggiornare `wwwroot/components/sidebar.html`
- [ ] Aggiornare `wwwroot/components/navbar.html`

### Sprint 7: Email e PDF (2 giorni)
- [ ] Configurare SMTP
- [ ] Implementare template email
- [ ] Implementare generazione PDF
- [ ] QR code in PDF
- [ ] Codice a barre
- [ ] Test invio email

### Sprint 8: Testing e Rifinitura (2-3 giorni)
- [ ] Test flusso completo acquisto
- [ ] Test race condition (multi-utente)
- [ ] Test validazione biglietti
- [ ] Test pagamento misto
- [ ] Test geolocalizzazione
- [ ] Bug fix
- [ ] Ottimizzazione performance
- [ ] Documentazione API

---

## File da Creare/Modificare

### Backend (Nuovi):
- `Model/Sala.cs`
- `Model/Show.cs`
- `Model/Acquisto.cs`
- `Model/Biglietto.cs`
- `Model/CreditoUtente.cs`
- `Model/TransazioneCredito.cs`
- `Model/PrenotazioneTemporanea.cs`
- `Model/PrezzoTipologiaSala.cs`
- `DTO/SalaDTO.cs`
- `DTO/ShowDTO.cs`
- `DTO/AcquistoDTO.cs`
- `DTO/BigliettoDTO.cs`
- `DTO/CreditoDTO.cs`
- `DTO/PagamentoDTO.cs`
- `Services/SalaService.cs`
- `Services/ShowService.cs`
- `Services/BigliettoService.cs`
- `Services/CreditoService.cs`
- `Services/PagamentoService.cs`
- `Services/EmailService.cs`
- `Services/PDFService.cs`
- `Endpoints/SaleEndpoints.cs`
- `Endpoints/ProgrammazioneEndpoints.cs`
- `Endpoints/AcquistoEndpoints.cs`
- `Endpoints/CreditoEndpoints.cs`
- `Endpoints/ValidazioneEndpoints.cs`
- `Background/PrenotazioneTempCleanupService.cs`

### Backend (Modificare):
- `Model/Film.cs`
- `Model/Cinema.cs`
- `Model/Utente.cs`
- `Data/FilmDbContext.cs`
- `Endpoints/ShowsEndpoints.cs` (ex Proiezioni)
- `Endpoints/UserEndpoints.cs`
- `Program.cs`
- `.env`

### Frontend (Nuovi):
- `wwwroot/scheda-film.html`
- `wwwroot/my-cinemas.html`
- `wwwroot/acquista.html`
- `wwwroot/pagamento.html`
- `wwwroot/conferma-acquisto.html`
- `wwwroot/sale.html`
- `wwwroot/validazione.html`
- `wwwroot/ricarica-credito.html`
- `wwwroot/js/geo-location.js`
- `wwwroot/js/stripe-client.js`
- `wwwroot/js/acquisto.js`
- `wwwroot/js/pages/scheda-film.js`
- `wwwroot/js/pages/my-cinemas.js`
- `wwwroot/js/pages/acquista.js`
- `wwwroot/js/pages/pagamento.js`
- `wwwroot/js/pages/sale.js`
- `wwwroot/js/pages/validazione.js`
- `wwwroot/js/pages/ricarica-credito.js`

### Frontend (Modificare):
- `wwwroot/programmazione.html`
- `wwwroot/proiezioni.html` -> rinominare in shows.html
- `wwwroot/components/sidebar.html`
- `wwwroot/components/navbar.html`
- `wwwroot/js/api-client.js`
- `wwwroot/js/auth.js`
- `wwwroot/js/route-guard.js`

---

## Note Tecniche Importanti

### Sicurezza QR Code
```csharp
public string GeneraCodiceHash(int bigliettoId, int acquistoId, string posto)
{
    var secretKey = _configuration["HashSecretKey"];
    var data = $"{bigliettoId}|{acquistoId}|{posto}|{secretKey}";
    using var sha256 = SHA256.Create();
    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
    return Convert.ToBase64String(hash).Substring(0, 16);
}

public bool VerificaHash(string codiceHash, Biglietto biglietto)
{
    var expectedHash = GeneraCodiceHash(biglietto.Id, biglietto.AcquistoId, biglietto.Posto);
    return codiceHash == expectedHash;
}
```

### Validazione Orari Show (Pseudocodice)
```csharp
public async Task<bool> ValidateOrarioAsync(int salaId, DateOnly data, TimeOnly oraInizio, int durataFilm, int? excludeShowId = null)
{
    var oraFine = oraInizio.AddMinutes(durataFilm);
    
    var showPrecedente = await context.Shows
        .Where(s => s.SalaId == salaId && s.Data == data && s.OraInizio < oraInizio)
        .Where(s => excludeShowId == null || s.Id != excludeShowId)
        .OrderByDescending(s => s.OraInizio)
        .FirstOrDefaultAsync();
    
    if (showPrecedente != null && oraInizio < showPrecedente.OraFine)
        return false;
    
    var showSuccessivo = await context.Shows
        .Where(s => s.SalaId == salaId && s.Data == data && s.OraInizio > oraInizio)
        .Where(s => excludeShowId == null || s.Id != excludeShowId)
        .OrderBy(s => s.OraInizio)
        .FirstOrDefaultAsync();
    
    if (showSuccessivo != null && oraFine > showSuccessivo.OraInizio)
        return false;
    
    return true;
}
```

### Timestamps
- Tutte le date in UTC
- TimeOnly per orari (no fuso orario)
- DateOnly per date

### Gestione Errori
- Codici HTTP coerenti
- Messaggi errore localizzati (IT)
- Logging strutturato

---

## Checklist Finale

### Sicurezza
- [ ] API protette con JWT
- [ ] RBAC funzionante
- [ ] Stripe integration sicura
- [ ] QR code con hash verificabile
- [ ] Lock posti anti-race-condition
- [ ] HTTPS in produzione

### Funzionalita
- [ ] Multi-sala per cinema
- [ ] Programmazione per cinema/film
- [ ] Acquisto biglietti con piantina
- [ ] Pagamento mixto (credito + carta)
- [ ] Validazione biglietti
- [ ] Email con PDF
- [ ] Geolocalizzazione cinema

### Performance
- [ ] Index su query frequenti
- [ ] Background job cleanup
- [ ] Caching configurazione sale
- [ ] Pagination liste lunghe

### Testing
- [ ] Test unitari servizi
- [ ] Test integrazione endpoint
- [ ] Test E2E acquisto
- [ ] Test race condition

---

## Dipendenze Necessarie

### Backend
```xml
<PackageReference Include="Stripe.net" Version="*" />
<PackageReference Include="MailKit" Version="*" />
<PackageReference Include="QuestPDF" Version="*" />
<PackageReference Include="QRCoder" Version="*" />
```

### Frontend
```html
<!-- Stripe.js -->
<script src="https://js.stripe.com/v3/"></script>

<!-- QR Scanner (opzionale) -->
<script src="https://unpkg.com/html5-qrcode"></script>
```