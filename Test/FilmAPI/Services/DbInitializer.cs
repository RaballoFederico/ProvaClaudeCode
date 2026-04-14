using System.Security.Cryptography;
using System.Text;
using FilmAPI.Data;
using FilmAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace FilmAPI.Services;

public static class DbInitializer
{
    public static async Task InitializeAsync(FilmDbContext context)
    {
        await context.Database.MigrateAsync();

        var roleMap = await EnsureRuoliAsync(context);
        var categoryMap = await EnsureCategorieAsync(context);
        var userMap = await EnsureUtentiAsync(context, roleMap);
        var directorMap = await EnsureRegistiAsync(context);
        var cinemaMap = await EnsureCinemasAsync(context);
        var saleMap = await EnsureSaleAsync(context, cinemaMap);
        var filmMap = await EnsureFilmsAsync(context, directorMap, categoryMap);
        var showMap = await EnsureShowsAsync(context, saleMap, filmMap);

        await EnsureCreditiAsync(context, userMap);
        await EnsureAcquistiBigliettiAsync(context, userMap, showMap, cinemaMap);
    }

    private static async Task<Dictionary<string, Ruolo>> EnsureRuoliAsync(FilmDbContext context)
    {
        var seeds = new[]
        {
            new Ruolo { Nome = "Admin", Descrizione = "Amministratore con accesso completo" },
            new Ruolo { Nome = "PowerUser", Descrizione = "Utente con privilegi elevati" },
            new Ruolo { Nome = "User", Descrizione = "Utente standard" }
        };

        foreach (var s in seeds)
        {
            if (!await context.Ruoli.AnyAsync(r => r.Nome == s.Nome))
            {
                context.Ruoli.Add(s);
            }
        }

        await context.SaveChangesAsync();
        return await context.Ruoli.ToDictionaryAsync(r => r.Nome, r => r);
    }

    private static async Task<Dictionary<string, Categoria>> EnsureCategorieAsync(FilmDbContext context)
    {
        var names = new[]
        {
            "Fantasy", "Horror", "Drammatico", "Commedia", "Azione", "Thriller",
            "Romantico", "Sci-Fi", "Animazione", "Documentario", "Avventura", "Storico"
        };

        foreach (var name in names)
        {
            if (!await context.Categorie.AnyAsync(c => c.Nome == name))
            {
                context.Categorie.Add(new Categoria { Nome = name, Descrizione = $"Film di genere {name}" });
            }
        }

        await context.SaveChangesAsync();
        return await context.Categorie.ToDictionaryAsync(c => c.Nome, c => c);
    }

    private static async Task<Dictionary<string, Utente>> EnsureUtentiAsync(FilmDbContext context, Dictionary<string, Ruolo> roleMap)
    {
        var users = new[]
        {
            new { Username = "admin", Email = "admin@filmapi.com", Nome = "Admin", Cognome = "System", Password = "Admin123!", Ruoli = new[] { "Admin" } },
            new { Username = "luca.power", Email = "luca.verdi@filmapi.com", Nome = "Luca", Cognome = "Verdi", Password = "Power123!", Ruoli = new[] { "PowerUser" } },
            new { Username = "mario.rossi", Email = "mario.rossi@email.com", Nome = "Mario", Cognome = "Rossi", Password = "User123!", Ruoli = new[] { "User" } },
            new { Username = "giulia.neri", Email = "giulia.neri@email.com", Nome = "Giulia", Cognome = "Neri", Password = "User123!", Ruoli = new[] { "User" } }
        };

        foreach (var u in users)
        {
            var exists = await context.Utenti.Include(x => x.UtentiRuoli).AnyAsync(x => x.Username == u.Username);
            if (exists) continue;

            var entity = new Utente
            {
                Username = u.Username,
                Email = u.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(u.Password),
                Nome = u.Nome,
                Cognome = u.Cognome,
                DataRegistrazione = DateTime.UtcNow,
                Attivo = true
            };

            foreach (var role in u.Ruoli)
            {
                entity.UtentiRuoli.Add(new UtenteRuolo { RuoloId = roleMap[role].Id });
            }

            context.Utenti.Add(entity);
        }

        await context.SaveChangesAsync();
        return await context.Utenti.ToDictionaryAsync(u => u.Username, u => u);
    }

    private static async Task<Dictionary<string, Regista>> EnsureRegistiAsync(FilmDbContext context)
    {
        var directors = new[]
        {
            new Regista { Nome = "Christopher", Cognome = "Nolan", Nazionalita = "UK" },
            new Regista { Nome = "Denis", Cognome = "Villeneuve", Nazionalita = "Canada" },
            new Regista { Nome = "Greta", Cognome = "Gerwig", Nazionalita = "USA" },
            new Regista { Nome = "Ridley", Cognome = "Scott", Nazionalita = "UK" },
            new Regista { Nome = "Hayao", Cognome = "Miyazaki", Nazionalita = "Japan" }
        };

        foreach (var d in directors)
        {
            var exists = await context.Registi.AnyAsync(x => x.Nome == d.Nome && x.Cognome == d.Cognome);
            if (!exists) context.Registi.Add(d);
        }

        await context.SaveChangesAsync();
        return await context.Registi.ToDictionaryAsync(r => $"{r.Nome} {r.Cognome}", r => r);
    }

    private static async Task<Dictionary<string, Cinema>> EnsureCinemasAsync(FilmDbContext context)
    {
        var cinemas = new[]
        {
            new Cinema { Nome = "UCI Lissone", Citta = "Lissone", Indirizzo = "Via Lombardia 12", PostiMassimi = 320, Latitudine = 45.61420000m, Longitudine = 9.23710000m, CodiceLocale = "UCI-LIS" },
            new Cinema { Nome = "UCI Milano", Citta = "Milano", Indirizzo = "Viale Sarca 336", PostiMassimi = 300, Latitudine = 45.51600000m, Longitudine = 9.21360000m, CodiceLocale = "UCI-MIL" },
            new Cinema { Nome = "Cinema Centrale", Citta = "Monza", Indirizzo = "Via Italia 45", PostiMassimi = 260, Latitudine = 45.58450000m, Longitudine = 9.27440000m, CodiceLocale = "CTR-MON" }
        };

        foreach (var c in cinemas)
        {
            if (!await context.Cinemas.AnyAsync(x => x.Nome == c.Nome)) context.Cinemas.Add(c);
        }

        await context.SaveChangesAsync();
        return await context.Cinemas.ToDictionaryAsync(c => c.Nome, c => c);
    }

    private static async Task<Dictionary<string, Sala>> EnsureSaleAsync(FilmDbContext context, Dictionary<string, Cinema> cinemaMap)
    {
        var defs = new[]
        {
            new { Cinema = "UCI Lissone", Num = 1, Nome = "SALA ISENSE", Tipo = TipologiaSala.ISENSE, File = 12, PerFila = 15 },
            new { Cinema = "UCI Lissone", Num = 2, Nome = "SALA XL", Tipo = TipologiaSala.XL, File = 10, PerFila = 14 },
            new { Cinema = "UCI Lissone", Num = 3, Nome = "SALA 3D", Tipo = TipologiaSala.TRE_D, File = 9, PerFila = 12 },
            new { Cinema = "UCI Lissone", Num = 4, Nome = "SALA 2D", Tipo = TipologiaSala.DUE_D, File = 11, PerFila = 13 },

            new { Cinema = "UCI Milano", Num = 1, Nome = "SALA IMAX", Tipo = TipologiaSala.ISENSE, File = 13, PerFila = 16 },
            new { Cinema = "UCI Milano", Num = 2, Nome = "SALA XL", Tipo = TipologiaSala.XL, File = 10, PerFila = 15 },
            new { Cinema = "UCI Milano", Num = 3, Nome = "SALA 3D", Tipo = TipologiaSala.TRE_D, File = 9, PerFila = 13 },
            new { Cinema = "UCI Milano", Num = 4, Nome = "SALA 2D", Tipo = TipologiaSala.DUE_D, File = 10, PerFila = 12 },

            new { Cinema = "Cinema Centrale", Num = 1, Nome = "SALA PREMIUM", Tipo = TipologiaSala.ISENSE, File = 10, PerFila = 14 },
            new { Cinema = "Cinema Centrale", Num = 2, Nome = "SALA CLASSIC", Tipo = TipologiaSala.DUE_D, File = 9, PerFila = 12 }
        };

        foreach (var d in defs)
        {
            var cinemaId = cinemaMap[d.Cinema].Id;
            if (await context.Sale.AnyAsync(s => s.CinemaId == cinemaId && s.NumeroSala == d.Num)) continue;

            context.Sale.Add(new Sala
            {
                CinemaId = cinemaId,
                NumeroSala = d.Num,
                Nome = d.Nome,
                Tipologia = d.Tipo,
                NumeroFile = d.File,
                PostiPerFila = d.PerFila,
                PostiTotali = d.File * d.PerFila,
                ConfigurazionePosti = null,
                Attiva = true
            });
        }

        await context.SaveChangesAsync();

        return await context.Sale.Include(s => s.Cinema)
            .ToDictionaryAsync(s => $"{s.Cinema!.Nome}#{s.NumeroSala}", s => s);
    }

    private static async Task<Dictionary<string, Film>> EnsureFilmsAsync(
        FilmDbContext context,
        Dictionary<string, Regista> directorMap,
        Dictionary<string, Categoria> categoryMap)
    {
        var films = new[]
        {
            new
            {
                Titolo = "Oppenheimer",
                RegistaKey = "Christopher Nolan",
                Durata = 180,
                Produzione = new DateTime(2023, 7, 21),
                Rilascio = new DateTime(2024, 3, 15),
                Genere = "Drammatico",
                Featured = true,
                Copertina = "/media/oppenheimer.jpg",
                Filmato = "/media/oppenheimer.mp4",
                Descrizione = "Il racconto del progetto Manhattan e del conflitto morale di J. Robert Oppenheimer.",
                Cast = "Cillian Murphy, Emily Blunt, Matt Damon, Robert Downey Jr.",
                Categorie = new[] { "Drammatico", "Storico" }
            },
            new
            {
                Titolo = "Dune - Parte Due",
                RegistaKey = "Denis Villeneuve",
                Durata = 166,
                Produzione = new DateTime(2024, 2, 28),
                Rilascio = new DateTime(2024, 4, 20),
                Genere = "Sci-Fi",
                Featured = true,
                Copertina = "/media/dune2.jpg",
                Filmato = "/media/dune2.mp4",
                Descrizione = "Paul Atreides guida la ribellione dei Fremen nella guerra per Arrakis.",
                Cast = "Timothee Chalamet, Zendaya, Rebecca Ferguson",
                Categorie = new[] { "Sci-Fi", "Avventura", "Azione" }
            },
            new
            {
                Titolo = "Barbie",
                RegistaKey = "Greta Gerwig",
                Durata = 114,
                Produzione = new DateTime(2023, 7, 20),
                Rilascio = new DateTime(2024, 4, 25),
                Genere = "Commedia",
                Featured = false,
                Copertina = "/media/barbie.jpg",
                Filmato = "/media/barbie.mp4",
                Descrizione = "Un viaggio ironico e colorato tra realta e mondo perfetto.",
                Cast = "Margot Robbie, Ryan Gosling",
                Categorie = new[] { "Commedia", "Fantasy" }
            },
            new
            {
                Titolo = "Alien: Romulus",
                RegistaKey = "Ridley Scott",
                Durata = 122,
                Produzione = new DateTime(2024, 8, 14),
                Rilascio = DateTime.UtcNow.Date.AddDays(8),
                Genere = "Horror",
                Featured = false,
                Copertina = "/media/alien-romulus.jpg",
                Filmato = "/media/alien-romulus.mp4",
                Descrizione = "Un nuovo capitolo sci-fi horror ambientato nell'universo Alien.",
                Cast = "Cailee Spaeny, Isabela Merced",
                Categorie = new[] { "Horror", "Sci-Fi", "Thriller" }
            },
            new
            {
                Titolo = "Il ragazzo e l'airone",
                RegistaKey = "Hayao Miyazaki",
                Durata = 124,
                Produzione = new DateTime(2023, 12, 1),
                Rilascio = DateTime.UtcNow.Date.AddDays(12),
                Genere = "Animazione",
                Featured = false,
                Copertina = "/media/heron.jpg",
                Filmato = "/media/heron.mp4",
                Descrizione = "Una fiaba onirica su crescita, perdita e immaginazione.",
                Cast = "Soma Santoki, Masaki Suda",
                Categorie = new[] { "Animazione", "Fantasy", "Avventura" }
            }
        };

        foreach (var f in films)
        {
            if (await context.Films.AnyAsync(x => x.Titolo == f.Titolo)) continue;

            var film = new Film
            {
                Titolo = f.Titolo,
                RegistaId = directorMap[f.RegistaKey].Id,
                RegistaNome = f.RegistaKey,
                Durata = f.Durata,
                DataProduzione = f.Produzione,
                DataRilascio = f.Rilascio,
                Genere = f.Genere,
                Featured = f.Featured,
                CopertinaPath = f.Copertina,
                FilmatoPath = f.Filmato,
                Descrizione = f.Descrizione,
                Cast = f.Cast
            };

            foreach (var cat in f.Categorie)
            {
                film.FilmsCategorie.Add(new FilmCategoria { CategoriaId = categoryMap[cat].Id });
            }

            context.Films.Add(film);
        }

        await context.SaveChangesAsync();
        return await context.Films.ToDictionaryAsync(f => f.Titolo, f => f);
    }

    private static async Task<Dictionary<string, Show>> EnsureShowsAsync(
        FilmDbContext context,
        Dictionary<string, Sala> saleMap,
        Dictionary<string, Film> filmMap)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var showDefs = new List<(string SalaKey, string Film, int DayOffset, TimeOnly Start, decimal Price)>();

        foreach (var cinema in new[] { "UCI Lissone", "UCI Milano", "Cinema Centrale" })
        {
            var salaNumbers = cinema == "Cinema Centrale" ? new[] { 1, 2 } : new[] { 1, 2, 3, 4 };
            foreach (var d in Enumerable.Range(0, 6))
            {
                foreach (var n in salaNumbers)
                {
                    var salaKey = $"{cinema}#{n}";
                    var film = n switch
                    {
                        1 => "Oppenheimer",
                        2 => "Dune - Parte Due",
                        3 => "Barbie",
                        _ => d % 2 == 0 ? "Alien: Romulus" : "Il ragazzo e l'airone"
                    };

                    var times = n switch
                    {
                        1 => new[] { new TimeOnly(16, 0), new TimeOnly(19, 30), new TimeOnly(22, 45) },
                        2 => new[] { new TimeOnly(15, 45), new TimeOnly(18, 40), new TimeOnly(21, 35) },
                        3 => new[] { new TimeOnly(17, 0), new TimeOnly(20, 0) },
                        _ => new[] { new TimeOnly(16, 20), new TimeOnly(18, 50), new TimeOnly(21, 10) }
                    };

                    var basePrice = n switch
                    {
                        1 => 12m,
                        2 => 10m,
                        3 => 9.5m,
                        _ => 8m
                    };

                    showDefs.AddRange(times.Select(t => (salaKey, film, d, t, basePrice)));
                }
            }
        }

        foreach (var def in showDefs)
        {
            var sala = saleMap[def.SalaKey];
            var film = filmMap[def.Film];
            var data = today.AddDays(def.DayOffset);
            var oraFine = def.Start.AddMinutes(film.Durata);

            var exists = await context.Shows.AnyAsync(s => s.SalaId == sala.Id && s.Data == data && s.OraInizio == def.Start);
            if (exists) continue;

            context.Shows.Add(new Show
            {
                SalaId = sala.Id,
                FilmId = film.Id,
                Data = data,
                OraInizio = def.Start,
                OraFine = oraFine,
                PrezzoBase = def.Price,
                Stato = StatoShow.PROGRAMMATO
            });
        }

        await context.SaveChangesAsync();

        return await context.Shows
            .Include(s => s.Sala)
            .ThenInclude(s => s!.Cinema)
            .Include(s => s.Film)
            .ToDictionaryAsync(
                s => $"{s.Sala!.Cinema!.Nome}#{s.Sala.NumeroSala}|{s.Film!.Titolo}|{s.Data:yyyy-MM-dd}|{s.OraInizio:hh\\:mm}",
                s => s);
    }

    private static async Task EnsureCreditiAsync(FilmDbContext context, Dictionary<string, Utente> userMap)
    {
        var credits = new[]
        {
            new { Username = "admin", Saldo = 150m },
            new { Username = "luca.power", Saldo = 90m },
            new { Username = "mario.rossi", Saldo = 25m },
            new { Username = "giulia.neri", Saldo = 40m }
        };

        foreach (var c in credits)
        {
            var userId = userMap[c.Username].Id;
            var entity = await context.CreditiUtente.FirstOrDefaultAsync(x => x.UtenteId == userId);

            if (entity is null)
            {
                context.CreditiUtente.Add(new CreditoUtente
                {
                    UtenteId = userId,
                    Saldo = c.Saldo,
                    DataUltimoAggiornamento = DateTime.UtcNow
                });
            }
            else
            {
                entity.Saldo = c.Saldo;
                entity.DataUltimoAggiornamento = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task EnsureAcquistiBigliettiAsync(
        FilmDbContext context,
        Dictionary<string, Utente> userMap,
        Dictionary<string, Show> showMap,
        Dictionary<string, Cinema> cinemaMap)
    {
        var mario = userMap["mario.rossi"];
        var giulia = userMap["giulia.neri"];

        var keys = showMap.Keys.OrderBy(k => k).Take(4).ToList();
        var chosen = keys.Select(k => showMap[k]).ToList();

        var acquistoMario = await context.Acquisti.FirstOrDefaultAsync(a => a.StripeChargeId == "pi_seed_mario_1");
        if (acquistoMario is null)
        {
            acquistoMario = new Acquisto
            {
                UtenteId = mario.Id,
                ShowId = chosen[0].Id,
                DataAcquisto = DateTime.UtcNow.AddDays(-2),
                ImportoTotale = chosen[0].PrezzoBase * 3,
                CreditoUsato = 10m,
                StripeChargeId = "pi_seed_mario_1",
                Stato = StatoAcquisto.PAGATO,
                CodiceConferma = Guid.NewGuid().ToString()
            };
            context.Acquisti.Add(acquistoMario);
        }

        var acquistoGiulia = await context.Acquisti.FirstOrDefaultAsync(a => a.StripeChargeId == "pi_seed_giulia_1");
        if (acquistoGiulia is null)
        {
            acquistoGiulia = new Acquisto
            {
                UtenteId = giulia.Id,
                ShowId = chosen[1].Id,
                DataAcquisto = DateTime.UtcNow.AddDays(-1),
                ImportoTotale = chosen[1].PrezzoBase * 2,
                CreditoUsato = 0m,
                StripeChargeId = "pi_seed_giulia_1",
                Stato = StatoAcquisto.PAGATO,
                CodiceConferma = Guid.NewGuid().ToString()
            };
            context.Acquisti.Add(acquistoGiulia);
        }

        await context.SaveChangesAsync();

        var marioShow = chosen[0];
        var giuliaShow = chosen[1];

        var marioCinema = await context.Sale.Where(s => s.Id == marioShow.SalaId).Select(s => s.CinemaId).FirstAsync();
        var giuliaCinema = await context.Sale.Where(s => s.Id == giuliaShow.SalaId).Select(s => s.CinemaId).FirstAsync();
        var marioSalaNum = await context.Sale.Where(s => s.Id == marioShow.SalaId).Select(s => s.NumeroSala).FirstAsync();
        var giuliaSalaNum = await context.Sale.Where(s => s.Id == giuliaShow.SalaId).Select(s => s.NumeroSala).FirstAsync();
        var marioTipo = await context.Sale.Where(s => s.Id == marioShow.SalaId).Select(s => s.Tipologia).FirstAsync();
        var giuliaTipo = await context.Sale.Where(s => s.Id == giuliaShow.SalaId).Select(s => s.Tipologia).FirstAsync();

        var ticketDefs = new[]
        {
            new { Acq = acquistoMario.Id, Show = marioShow.Id, Posto = "Fila 7, Posto 1", SalaNum = marioSalaNum, Tipo = marioTipo.ToString(), Prezzo = marioShow.PrezzoBase, Cinema = marioCinema, Validato = false },
            new { Acq = acquistoMario.Id, Show = marioShow.Id, Posto = "Fila 7, Posto 2", SalaNum = marioSalaNum, Tipo = marioTipo.ToString(), Prezzo = marioShow.PrezzoBase, Cinema = marioCinema, Validato = false },
            new { Acq = acquistoMario.Id, Show = marioShow.Id, Posto = "Fila 7, Posto 3", SalaNum = marioSalaNum, Tipo = marioTipo.ToString(), Prezzo = marioShow.PrezzoBase, Cinema = marioCinema, Validato = false },
            new { Acq = acquistoGiulia.Id, Show = giuliaShow.Id, Posto = "Fila 5, Posto 11", SalaNum = giuliaSalaNum, Tipo = giuliaTipo.ToString(), Prezzo = giuliaShow.PrezzoBase, Cinema = giuliaCinema, Validato = true },
            new { Acq = acquistoGiulia.Id, Show = giuliaShow.Id, Posto = "Fila 5, Posto 12", SalaNum = giuliaSalaNum, Tipo = giuliaTipo.ToString(), Prezzo = giuliaShow.PrezzoBase, Cinema = giuliaCinema, Validato = false }
        };

        var createdTickets = new List<Biglietto>();
        foreach (var d in ticketDefs)
        {
            var exists = await context.Biglietti.AnyAsync(b => b.AcquistoId == d.Acq && b.Posto == d.Posto);
            if (exists) continue;
            var t = CreateTicket(d.Acq, d.Show, d.Posto, d.SalaNum, d.Tipo, d.Prezzo, d.Cinema, d.Validato);
            context.Biglietti.Add(t);
            createdTickets.Add(t);
        }

        if (createdTickets.Count > 0)
        {
            await context.SaveChangesAsync();

            foreach (var t in createdTickets)
            {
                t.CodiceHash = GenerateHash(t.Id, t.AcquistoId, t.Posto);
                t.QRCodeUrl = $"https://filmapi.com/validazione/qr/{t.CodiceHash}";
                if (t.Validato && t.DataValidazione is null) t.DataValidazione = DateTime.UtcNow.AddHours(-4);
            }
        }

        if (!await context.TransazioniCredito.AnyAsync(t => t.Descrizione == "Ricarica in cassa" && t.UtenteId == mario.Id))
        {
            context.TransazioniCredito.Add(new TransazioneCredito
            {
                UtenteId = mario.Id,
                Tipo = TipoTransazione.RICARICA,
                Importo = 35m,
                SaldoPrecedente = 0m,
                SaldoSuccessivo = 35m,
                DataTransazione = DateTime.UtcNow.AddDays(-3),
                OperatoreId = userMap["luca.power"].Id,
                CinemaId = cinemaMap["UCI Lissone"].Id,
                Descrizione = "Ricarica in cassa"
            });
        }

        if (!await context.TransazioniCredito.AnyAsync(t => t.AcquistoId == acquistoMario.Id && t.Tipo == TipoTransazione.ACQUISTO))
        {
            context.TransazioniCredito.Add(new TransazioneCredito
            {
                UtenteId = mario.Id,
                Tipo = TipoTransazione.ACQUISTO,
                Importo = -10m,
                SaldoPrecedente = 35m,
                SaldoSuccessivo = 25m,
                DataTransazione = DateTime.UtcNow.AddDays(-2),
                AcquistoId = acquistoMario.Id,
                Descrizione = "Uso credito acquisto"
            });
        }

        await context.SaveChangesAsync();
    }

    private static Biglietto CreateTicket(
        int acquistoId,
        int showId,
        string posto,
        int salaNumero,
        string tipologia,
        decimal prezzo,
        int cinemaId,
        bool validato)
    {
        return new Biglietto
        {
            AcquistoId = acquistoId,
            ShowId = showId,
            Posto = posto,
            SalaNumero = salaNumero,
            TipologiaSala = tipologia,
            Prezzo = prezzo,
            CodiceUnivoco = Guid.NewGuid().ToString("N")[..20],
            CodiceHash = Guid.NewGuid().ToString("N"),
            Validato = validato,
            DataValidazione = validato ? DateTime.UtcNow.AddHours(-6) : null,
            CinemaId = cinemaId,
            QRCodeUrl = "https://filmapi.com/validazione/qr/pending"
        };
    }

    private static string GenerateHash(int bigliettoId, int acquistoId, string posto)
    {
        var raw = $"{bigliettoId}|{acquistoId}|{posto}|filmapi";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
