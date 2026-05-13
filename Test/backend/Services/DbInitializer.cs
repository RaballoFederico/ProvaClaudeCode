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

        var alreadySeeded =
            await context.Ruoli.AnyAsync() &&
            await context.Categorie.AnyAsync() &&
            await context.Utenti.AnyAsync() &&
            await context.Registi.AnyAsync() &&
            await context.Cinemas.AnyAsync() &&
            await context.Sale.AnyAsync() &&
            await context.Films.AnyAsync() &&
            await context.Shows.AnyAsync();

        if (alreadySeeded)
        {
            // Mantiene allineata la vista legacy "Proiezioni" senza rieseguire tutto il seed.
            await EnsureProiezioniFromShowsAsync(context);
            return;
        }

        var roleMap = await EnsureRuoliAsync(context);
        var categoryMap = await EnsureCategorieAsync(context);
        var userMap = await EnsureUtentiAsync(context, roleMap);
        var directorMap = await EnsureRegistiAsync(context);
        var cinemaMap = await EnsureCinemasAsync(context);
        var saleMap = await EnsureSaleAsync(context, cinemaMap);
        var filmMap = await EnsureFilmsAsync(context, directorMap, categoryMap);
        var showMap = await EnsureShowsAsync(context, saleMap, filmMap);
        await EnsureProiezioniFromShowsAsync(context);

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
            "Romantico", "Sci-Fi", "Animazione", "Documentario", "Avventura", "Storico",
            "Crime", "Biografico", "Musicale", "Mistero", "Guerra", "Famiglia"
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
            new { Username = "giulia.neri", Email = "giulia.neri@email.com", Nome = "Giulia", Cognome = "Neri", Password = "User123!", Ruoli = new[] { "User" } },
            new { Username = "elena.bianchi", Email = "elena.bianchi@email.com", Nome = "Elena", Cognome = "Bianchi", Password = "User123!", Ruoli = new[] { "User" } },
            new { Username = "paolo.riva", Email = "paolo.riva@email.com", Nome = "Paolo", Cognome = "Riva", Password = "User123!", Ruoli = new[] { "User" } },
            new { Username = "sara.gallo", Email = "sara.gallo@email.com", Nome = "Sara", Cognome = "Gallo", Password = "User123!", Ruoli = new[] { "User" } },
            new { Username = "marco.conti", Email = "marco.conti@email.com", Nome = "Marco", Cognome = "Conti", Password = "User123!", Ruoli = new[] { "User" } },
            new { Username = "anna.greco", Email = "anna.greco@email.com", Nome = "Anna", Cognome = "Greco", Password = "User123!", Ruoli = new[] { "User" } },
            new { Username = "davide.mancini", Email = "davide.mancini@email.com", Nome = "Davide", Cognome = "Mancini", Password = "User123!", Ruoli = new[] { "User" } }
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
            new Regista { Nome = "Hayao", Cognome = "Miyazaki", Nazionalita = "Japan" },
            new Regista { Nome = "Martin", Cognome = "Scorsese", Nazionalita = "USA" },
            new Regista { Nome = "Quentin", Cognome = "Tarantino", Nazionalita = "USA" },
            new Regista { Nome = "Patty", Cognome = "Jenkins", Nazionalita = "USA" },
            new Regista { Nome = "Alfonso", Cognome = "Cuaron", Nazionalita = "Mexico" },
            new Regista { Nome = "Sofia", Cognome = "Coppola", Nazionalita = "USA" },
            new Regista { Nome = "Damien", Cognome = "Chazelle", Nazionalita = "USA" },
            new Regista { Nome = "Bong", Cognome = "Joon-ho", Nazionalita = "Korea" },
            new Regista { Nome = "Alejandro", Cognome = "Inarritu", Nazionalita = "Mexico" },
            new Regista { Nome = "James", Cognome = "Cameron", Nazionalita = "Canada" },
            new Regista { Nome = "Jordan", Cognome = "Peele", Nazionalita = "USA" },
            new Regista { Nome = "Sam", Cognome = "Mendes", Nazionalita = "UK" },
            new Regista { Nome = "Kathryn", Cognome = "Bigelow", Nazionalita = "USA" },
            new Regista { Nome = "Pete", Cognome = "Docter", Nazionalita = "USA" }
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
            new Cinema { Nome = "Cinema Centrale", Citta = "Monza", Indirizzo = "Via Italia 45", PostiMassimi = 260, Latitudine = 45.58450000m, Longitudine = 9.27440000m, CodiceLocale = "CTR-MON" },
            new Cinema { Nome = "Arcadia Bellinzago", Citta = "Bellinzago", Indirizzo = "Via XXV Aprile 33", PostiMassimi = 420, Latitudine = 45.57370000m, Longitudine = 8.64320000m, CodiceLocale = "ARC-BEL" },
            new Cinema { Nome = "The Space Vimercate", Citta = "Vimercate", Indirizzo = "Via Torri Bianche 16", PostiMassimi = 360, Latitudine = 45.61770000m, Longitudine = 9.35650000m, CodiceLocale = "TSP-VIM" },
            new Cinema { Nome = "Cinema Ducale", Citta = "Parma", Indirizzo = "Piazzale della Pilotta 1", PostiMassimi = 280, Latitudine = 44.80150000m, Longitudine = 10.32550000m, CodiceLocale = "DUC-PAR" },
            new Cinema { Nome = "UCI Torino Lingotto", Citta = "Torino", Indirizzo = "Via Nizza 280", PostiMassimi = 340, Latitudine = 45.02850000m, Longitudine = 7.66540000m, CodiceLocale = "UCI-TOR" },
            new Cinema { Nome = "Cinema Adriano", Citta = "Roma", Indirizzo = "Piazza Cavour 22", PostiMassimi = 300, Latitudine = 41.90350000m, Longitudine = 12.46800000m, CodiceLocale = "ADR-ROM" },
            new Cinema { Nome = "Cinema Moderno", Citta = "Bologna", Indirizzo = "Via Rizzoli 1", PostiMassimi = 250, Latitudine = 44.49490000m, Longitudine = 11.34640000m, CodiceLocale = "MOD-BOL" },
            new Cinema { Nome = "UCI Roma Parco de' Medici", Citta = "Roma", Indirizzo = "Via Vito G. Galoppini 15", PostiMassimi = 380, Latitudine = 41.86300000m, Longitudine = 12.43300000m, CodiceLocale = "UCI-ROM" },
            new Cinema { Nome = "Cinepolis Galleria", Citta = "Napoli", Indirizzo = "Via Toledo 402", PostiMassimi = 290, Latitudine = 40.83690000m, Longitudine = 14.24840000m, CodiceLocale = "CPS-NAP" },
            new Cinema { Nome = "Cinema Fiume", Citta = "Palermo", Indirizzo = "Via E. F. 88", PostiMassimi = 240, Latitudine = 38.11570000m, Longitudine = 13.35850000m, CodiceLocale = "FLM-PAL" },
            new Cinema { Nome = "Odeon Firenze", Citta = "Firenze", Indirizzo = "Piazza Strozzi 6", PostiMassimi = 310, Latitudine = 43.77370000m, Longitudine = 11.25670000m, CodiceLocale = "ODE-FIR" },
            new Cinema { Nome = "Cinema Republic", Citta = "Genova", Indirizzo = "Via流水 45", PostiMassimi = 230, Latitudine = 44.40760000m, Longitudine = 8.93930000m, CodiceLocale = "REP-GEN" },
            new Cinema { Nome = "UCI Fiumara", Citta = "Genova", Indirizzo = "Via Pammatone 8", PostiMassimi = 350, Latitudine = 44.42400000m, Longitudine = 8.89100000m, CodiceLocale = "UCI-FIU" },
            new Cinema { Nome = "Cinecitta World", Citta = "Roma", Indirizzo = "Via di Settebello 29", PostiMassimi = 400, Latitudine = 41.89200000m, Longitudine = 12.50200000m, CodiceLocale = "CCW-ROM" },
            new Cinema { Nome = "UCI Verona", Citta = "Verona", Indirizzo = "Via Monte Baldo 8", PostiMassimi = 270, Latitudine = 45.44300000m, Longitudine = 10.98700000m, CodiceLocale = "UCI-VER" },
            new Cinema { Nome = "Cinema Azzurro", Citta = "Padova", Indirizzo = "Corso Milano 22", PostiMassimi = 220, Latitudine = 45.40650000m, Longitudine = 11.87370000m, CodiceLocale = "AZZ-PAD" },
            new Cinema { Nome = "UCI Brescia", Citta = "Brescia", Indirizzo = "Via C. B. 14", PostiMassimi = 330, Latitudine = 45.53900000m, Longitudine = 10.22400000m, CodiceLocale = "UCI-BRE" },
            new Cinema { Nome = "Cinema Maestoso", Citta = "Trieste", Indirizzo = "Via Torino 12", PostiMassimi = 200, Latitudine = 45.64950000m, Longitudine = 13.77000000m, CodiceLocale = "MAE-TRI" },
            new Cinema { Nome = "UCI Bari", Citta = "Bari", Indirizzo = "Via J. R. 50", PostiMassimi = 310, Latitudine = 41.06200000m, Longitudine = 16.86500000m, CodiceLocale = "UCI-BAR" },
            new Cinema { Nome = "Cineporti", Citta = "Catania", Indirizzo = "Via S. F. 110", PostiMassimi = 280, Latitudine = 37.50800000m, Longitudine = 15.09300000m, CodiceLocale = "CPR-CAT" },
            new Cinema { Nome = "UCI Bergamo", Citta = "Bergamo", Indirizzo = "Via A. stop 6", PostiMassimi = 260, Latitudine = 45.69400000m, Longitudine = 9.66300000m, CodiceLocale = "UCI-BER" },
            new Cinema { Nome = "Cinema Teatro", Citta = "Varese", Indirizzo = "Via D. M. 18", PostiMassimi = 215, Latitudine = 45.81800000m, Longitudine = 8.82600000m, CodiceLocale = "CTE-VAR" }
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
        foreach (var cinema in cinemaMap.Values)
        {
            var standardLayout = new[]
            {
                new { Num = 1, Nome = "SALA ISENSE", Tipo = TipologiaSala.ISENSE, File = 12, PerFila = 15 },
                new { Num = 2, Nome = "SALA XL", Tipo = TipologiaSala.XL, File = 11, PerFila = 14 },
                new { Num = 3, Nome = "SALA 3D", Tipo = TipologiaSala.TRE_D, File = 10, PerFila = 13 },
                new { Num = 4, Nome = "SALA 2D", Tipo = TipologiaSala.DUE_D, File = 11, PerFila = 12 }
            };

            var compactLayout = new[]
            {
                new { Num = 1, Nome = "SALA PREMIUM", Tipo = TipologiaSala.ISENSE, File = 10, PerFila = 14 },
                new { Num = 2, Nome = "SALA CLASSIC", Tipo = TipologiaSala.DUE_D, File = 9, PerFila = 12 },
                new { Num = 3, Nome = "SALA 3D", Tipo = TipologiaSala.TRE_D, File = 9, PerFila = 11 }
            };

            var layout = cinema.PostiMassimi >= 300 ? standardLayout : compactLayout;

            foreach (var d in layout)
            {
                if (await context.Sale.AnyAsync(s => s.CinemaId == cinema.Id && s.NumeroSala == d.Num)) continue;

                context.Sale.Add(new Sala
                {
                    CinemaId = cinema.Id,
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
            },
            new
            {
                Titolo = "Killers of the Flower Moon",
                RegistaKey = "Martin Scorsese",
                Durata = 206,
                Produzione = new DateTime(2023, 10, 18),
                Rilascio = DateTime.UtcNow.Date.AddDays(16),
                Genere = "Crime",
                Featured = true,
                Copertina = "/media/flower-moon.jpg",
                Filmato = "/media/flower-moon.mp4",
                Descrizione = "Indagine oscura tra avidita e corruzione nell'Oklahoma degli anni '20.",
                Cast = "Leonardo DiCaprio, Lily Gladstone, Robert De Niro",
                Categorie = new[] { "Crime", "Drammatico", "Storico" }
            },
            new
            {
                Titolo = "Once Upon a Time in Hollywood",
                RegistaKey = "Quentin Tarantino",
                Durata = 161,
                Produzione = new DateTime(2019, 8, 1),
                Rilascio = DateTime.UtcNow.Date.AddDays(18),
                Genere = "Commedia",
                Featured = false,
                Copertina = "/media/ouatih.jpg",
                Filmato = "/media/ouatih.mp4",
                Descrizione = "Una lettera d'amore alla Hollywood di fine anni '60.",
                Cast = "Leonardo DiCaprio, Brad Pitt, Margot Robbie",
                Categorie = new[] { "Commedia", "Drammatico" }
            },
            new
            {
                Titolo = "Wonder Woman",
                RegistaKey = "Patty Jenkins",
                Durata = 141,
                Produzione = new DateTime(2017, 6, 2),
                Rilascio = DateTime.UtcNow.Date.AddDays(20),
                Genere = "Azione",
                Featured = false,
                Copertina = "/media/wonder-woman.jpg",
                Filmato = "/media/wonder-woman.mp4",
                Descrizione = "Origini e crescita dell'eroina amazzone nella Grande Guerra.",
                Cast = "Gal Gadot, Chris Pine",
                Categorie = new[] { "Azione", "Avventura", "Fantasy" }
            },
            new
            {
                Titolo = "Roma",
                RegistaKey = "Alfonso Cuaron",
                Durata = 135,
                Produzione = new DateTime(2018, 9, 30),
                Rilascio = DateTime.UtcNow.Date.AddDays(22),
                Genere = "Drammatico",
                Featured = false,
                Copertina = "/media/roma.jpg",
                Filmato = "/media/roma.mp4",
                Descrizione = "Ritratto intimo di una famiglia e della vita domestica nel Messico degli anni '70.",
                Cast = "Yalitza Aparicio, Marina de Tavira",
                Categorie = new[] { "Drammatico", "Storico" }
            },
            new
            {
                Titolo = "Lost in Translation",
                RegistaKey = "Sofia Coppola",
                Durata = 102,
                Produzione = new DateTime(2003, 9, 12),
                Rilascio = DateTime.UtcNow.Date.AddDays(24),
                Genere = "Romantico",
                Featured = false,
                Copertina = "/media/lost-translation.jpg",
                Filmato = "/media/lost-translation.mp4",
                Descrizione = "Due anime sole si incontrano a Tokyo tra silenzi e confidenze.",
                Cast = "Bill Murray, Scarlett Johansson",
                Categorie = new[] { "Romantico", "Commedia", "Drammatico" }
            },
            new
            {
                Titolo = "La La Land",
                RegistaKey = "Damien Chazelle",
                Durata = 128,
                Produzione = new DateTime(2016, 12, 1),
                Rilascio = DateTime.UtcNow.Date.AddDays(26),
                Genere = "Musicale",
                Featured = false,
                Copertina = "/media/lalaland.jpg",
                Filmato = "/media/lalaland.mp4",
                Descrizione = "Sogni, jazz e amore nella Los Angeles delle opportunita.",
                Cast = "Ryan Gosling, Emma Stone",
                Categorie = new[] { "Musicale", "Romantico", "Commedia" }
            },
            new
            {
                Titolo = "Parasite",
                RegistaKey = "Bong Joon-ho",
                Durata = 132,
                Produzione = new DateTime(2019, 5, 30),
                Rilascio = DateTime.UtcNow.Date.AddDays(28),
                Genere = "Thriller",
                Featured = true,
                Copertina = "/media/parasite.jpg",
                Filmato = "/media/parasite.mp4",
                Descrizione = "Satira sociale tagliente che sfocia nel thriller.",
                Cast = "Song Kang-ho, Choi Woo-shik, Park So-dam",
                Categorie = new[] { "Thriller", "Drammatico", "Commedia" }
            },
            new
            {
                Titolo = "Birdman",
                RegistaKey = "Alejandro Inarritu",
                Durata = 119,
                Produzione = new DateTime(2014, 10, 17),
                Rilascio = DateTime.UtcNow.Date.AddDays(30),
                Genere = "Drammatico",
                Featured = false,
                Copertina = "/media/birdman.jpg",
                Filmato = "/media/birdman.mp4",
                Descrizione = "Un attore in crisi cerca redenzione su un palcoscenico di Broadway.",
                Cast = "Michael Keaton, Edward Norton, Emma Stone",
                Categorie = new[] { "Drammatico", "Commedia" }
            },
            new
            {
                Titolo = "Avatar: The Way of Water",
                RegistaKey = "James Cameron",
                Durata = 192,
                Produzione = new DateTime(2022, 12, 16),
                Rilascio = DateTime.UtcNow.Date.AddDays(32),
                Genere = "Sci-Fi",
                Featured = true,
                Copertina = "/media/avatar-way-water.jpg",
                Filmato = "/media/avatar-way-water.mp4",
                Descrizione = "Il ritorno su Pandora tra famiglia, oceani e nuove minacce.",
                Cast = "Sam Worthington, Zoe Saldana, Sigourney Weaver",
                Categorie = new[] { "Sci-Fi", "Avventura", "Azione" }
            },
            new
            {
                Titolo = "Get Out",
                RegistaKey = "Jordan Peele",
                Durata = 104,
                Produzione = new DateTime(2017, 2, 24),
                Rilascio = DateTime.UtcNow.Date.AddDays(34),
                Genere = "Horror",
                Featured = false,
                Copertina = "/media/get-out.jpg",
                Filmato = "/media/get-out.mp4",
                Descrizione = "Un weekend apparentemente tranquillo diventa un incubo psicologico.",
                Cast = "Daniel Kaluuya, Allison Williams",
                Categorie = new[] { "Horror", "Thriller", "Mistero" }
            },
            new
            {
                Titolo = "1917",
                RegistaKey = "Sam Mendes",
                Durata = 119,
                Produzione = new DateTime(2019, 12, 25),
                Rilascio = DateTime.UtcNow.Date.AddDays(36),
                Genere = "Storico",
                Featured = false,
                Copertina = "/media/1917.jpg",
                Filmato = "/media/1917.mp4",
                Descrizione = "Missione impossibile nelle trincee della Prima Guerra Mondiale.",
                Cast = "George MacKay, Dean-Charles Chapman",
                Categorie = new[] { "Storico", "Guerra", "Drammatico" }
            },
            new
            {
                Titolo = "The Hurt Locker",
                RegistaKey = "Kathryn Bigelow",
                Durata = 131,
                Produzione = new DateTime(2008, 10, 10),
                Rilascio = DateTime.UtcNow.Date.AddDays(38),
                Genere = "Thriller",
                Featured = false,
                Copertina = "/media/hurt-locker.jpg",
                Filmato = "/media/hurt-locker.mp4",
                Descrizione = "Tensione e adrenalina nella squadra artificieri in Iraq.",
                Cast = "Jeremy Renner, Anthony Mackie",
                Categorie = new[] { "Thriller", "Guerra", "Drammatico" }
            },
            new
            {
                Titolo = "Inside Out",
                RegistaKey = "Pete Docter",
                Durata = 95,
                Produzione = new DateTime(2015, 6, 19),
                Rilascio = DateTime.UtcNow.Date.AddDays(40),
                Genere = "Animazione",
                Featured = false,
                Copertina = "/media/inside-out.jpg",
                Filmato = "/media/inside-out.mp4",
                Descrizione = "Le emozioni prendono vita nel viaggio di crescita di Riley.",
                Cast = "Amy Poehler, Phyllis Smith",
                Categorie = new[] { "Animazione", "Famiglia", "Commedia" }
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
        return await context.Films
            .GroupBy(f => f.Titolo)
            .Select(g => g.OrderByDescending(x => x.Id).First())
            .ToDictionaryAsync(f => f.Titolo, f => f);
    }

    private static async Task<Dictionary<string, Show>> EnsureShowsAsync(
        FilmDbContext context,
        Dictionary<string, Sala> saleMap,
        Dictionary<string, Film> filmMap)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var filmTitles = filmMap.Keys.OrderBy(t => t).ToArray();
        var sale = saleMap.Values.OrderBy(s => s.CinemaId).ThenBy(s => s.NumeroSala).ToList();

        foreach (var sala in sale)
        {
            var standardSlots = new[] { new TimeOnly(15, 30), new TimeOnly(20, 15) };
            var weekendSlots = new[] { new TimeOnly(11, 30), new TimeOnly(15, 30), new TimeOnly(20, 15) };

            for (var dayOffset = -10; dayOffset <= 60; dayOffset++)
            {
                var data = today.AddDays(dayOffset);
                var isWeekend = data.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                var slots = isWeekend ? weekendSlots : standardSlots;

                for (var slotIndex = 0; slotIndex < slots.Length; slotIndex++)
                {
                    var start = slots[slotIndex];
                    var filmIndex = Math.Abs((sala.Id * 17) + (dayOffset * 7) + slotIndex) % filmTitles.Length;
                    var film = filmMap[filmTitles[filmIndex]];
                    var oraFine = start.AddMinutes(film.Durata);

                    var exists = await context.Shows.AnyAsync(s => s.SalaId == sala.Id && s.Data == data && s.OraInizio == start);
                    if (exists) continue;

                    var stato = dayOffset < 0
                        ? StatoShow.TERMINATO
                        : ((slotIndex == 2 && dayOffset % 9 == 0) ? StatoShow.CANCELLATO : StatoShow.PROGRAMMATO);

                    var price = sala.Tipologia switch
                    {
                        TipologiaSala.ISENSE => 12.00m,
                        TipologiaSala.XL => 10.00m,
                        TipologiaSala.TRE_D => 9.50m,
                        _ => 8.00m
                    };

                    context.Shows.Add(new Show
                    {
                        SalaId = sala.Id,
                        FilmId = film.Id,
                        Data = data,
                        OraInizio = start,
                        OraFine = oraFine,
                        PrezzoBase = price,
                        Stato = stato
                    });
                }
            }
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

    private static async Task EnsureProiezioniFromShowsAsync(FilmDbContext context)
    {
        var showRows = await context.Shows
            .Include(s => s.Sala)
            .Select(s => new
            {
                s.Id,
                s.FilmId,
                s.Data,
                s.OraInizio,
                CinemaId = s.Sala != null ? s.Sala.CinemaId : 0
            })
            .Where(s => s.CinemaId > 0)
            .ToListAsync();

        var showIds = showRows.Select(s => s.Id).ToList();
        var existingShowIds = await context.Proiezioni
            .Where(p => p.ShowId.HasValue && showIds.Contains(p.ShowId.Value))
            .Select(p => p.ShowId!.Value)
            .ToListAsync();
        var existingSet = existingShowIds.ToHashSet();

        var projectionsToAdd = new List<Proiezione>();
        foreach (var s in showRows)
        {
            if (existingSet.Contains(s.Id)) continue;

            projectionsToAdd.Add(new Proiezione
            {
                ShowId = s.Id,
                CinemaId = s.CinemaId,
                FilmId = s.FilmId,
                Data = s.Data.ToDateTime(TimeOnly.MinValue),
                Ora = s.OraInizio.ToTimeSpan()
            });
        }

        if (projectionsToAdd.Count == 0)
        {
            return;
        }

        context.Proiezioni.AddRange(projectionsToAdd);
        await context.SaveChangesAsync();
    }

    private static async Task EnsureCreditiAsync(FilmDbContext context, Dictionary<string, Utente> userMap)
    {
        var credits = new[]
        {
            new { Username = "admin", Saldo = 150m },
            new { Username = "luca.power", Saldo = 90m },
            new { Username = "mario.rossi", Saldo = 25m },
            new { Username = "giulia.neri", Saldo = 40m },
            new { Username = "elena.bianchi", Saldo = 55m },
            new { Username = "paolo.riva", Saldo = 32m },
            new { Username = "sara.gallo", Saldo = 28m },
            new { Username = "marco.conti", Saldo = 18m },
            new { Username = "anna.greco", Saldo = 47m },
            new { Username = "davide.mancini", Saldo = 21m }
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

        var keys = showMap.Keys.OrderBy(k => k).Take(8).ToList();
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

        var extraPurchaseDefs = new[]
        {
            new { User = "elena.bianchi", Show = chosen[2], Qty = 2, Credit = 5m, Stripe = "pi_seed_elena_1", DaysAgo = 2 },
            new { User = "paolo.riva", Show = chosen[3], Qty = 3, Credit = 0m, Stripe = "pi_seed_paolo_1", DaysAgo = 1 },
            new { User = "sara.gallo", Show = chosen[4], Qty = 2, Credit = 4m, Stripe = "pi_seed_sara_1", DaysAgo = 3 },
            new { User = "anna.greco", Show = chosen[5], Qty = 4, Credit = 8m, Stripe = "pi_seed_anna_1", DaysAgo = 4 },
            new { User = "davide.mancini", Show = chosen[6], Qty = 1, Credit = 0m, Stripe = "pi_seed_davide_1", DaysAgo = 1 }
        };

        foreach (var def in extraPurchaseDefs)
        {
            var user = userMap[def.User];
            var acquisto = await context.Acquisti.FirstOrDefaultAsync(a => a.StripeChargeId == def.Stripe);
            if (acquisto is null)
            {
                acquisto = new Acquisto
                {
                    UtenteId = user.Id,
                    ShowId = def.Show.Id,
                    DataAcquisto = DateTime.UtcNow.AddDays(-def.DaysAgo),
                    ImportoTotale = (def.Show.PrezzoBase * def.Qty) - def.Credit,
                    CreditoUsato = def.Credit,
                    StripeChargeId = def.Stripe,
                    Stato = StatoAcquisto.PAGATO,
                    CodiceConferma = Guid.NewGuid().ToString()
                };
                context.Acquisti.Add(acquisto);
                await context.SaveChangesAsync();
            }

            var salaData = await context.Sale
                .Where(s => s.Id == def.Show.SalaId)
                .Select(s => new { s.CinemaId, s.NumeroSala, Tipo = s.Tipologia.ToString() })
                .FirstAsync();

            for (var i = 0; i < def.Qty; i++)
            {
                var posto = $"Fila {5 + (i % 3)}, Posto {9 + i}";
                var exists = await context.Biglietti.AnyAsync(b => b.AcquistoId == acquisto.Id && b.Posto == posto);
                if (exists) continue;

                var t = CreateTicket(acquisto.Id, def.Show.Id, posto, salaData.NumeroSala, salaData.Tipo, def.Show.PrezzoBase, salaData.CinemaId, false);
                context.Biglietti.Add(t);
                await context.SaveChangesAsync();
                t.CodiceHash = GenerateHash(t.Id, t.AcquistoId, t.Posto);
                t.QRCodeUrl = $"https://filmapi.com/validazione/qr/{t.CodiceHash}";
            }

            if (def.Credit > 0m && !await context.TransazioniCredito.AnyAsync(t => t.AcquistoId == acquisto.Id && t.Tipo == TipoTransazione.ACQUISTO))
            {
                context.TransazioniCredito.Add(new TransazioneCredito
                {
                    UtenteId = user.Id,
                    Tipo = TipoTransazione.ACQUISTO,
                    Importo = -def.Credit,
                    SaldoPrecedente = def.Credit + 20m,
                    SaldoSuccessivo = 20m,
                    DataTransazione = DateTime.UtcNow.AddDays(-def.DaysAgo),
                    AcquistoId = acquisto.Id,
                    Descrizione = "Uso credito acquisto"
                });
            }
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
